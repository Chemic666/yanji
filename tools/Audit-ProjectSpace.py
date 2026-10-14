import os, csv, json, ctypes, time, argparse, sys, math
from pathlib import Path
from ctypes import wintypes as W
parser=argparse.ArgumentParser(description='Read-only Windows/NTFS project space audit. Never deletes files or reads database contents.')
parser.add_argument('--root',type=Path,default=Path(__file__).resolve().parent.parent)
parser.add_argument('--output',type=Path,help='New output directory only; existing directories are rejected.')
parser.add_argument('--warn-gib',type=float,default=3.0)
parser.add_argument('--fail-over-budget',action='store_true',help='Return 2 after writing reports when over budget; never clean anything.')
args=parser.parse_args()
ROOT=args.root.resolve()
if not ROOT.is_dir():parser.error('Root must be an existing directory')
if not math.isfinite(args.warn_gib) or args.warn_gib<=0:parser.error('Budget must be finite and positive')
OUT=(args.output or ROOT/'artifacts'/'space-audits'/time.strftime('%Y%m%d-%H%M%S',time.gmtime())).resolve()
if OUT.exists():parser.error('Output already exists; use a new path to preserve prior evidence')
if os.name!='nt':parser.error('This tool uses Windows NTFS APIs')
OUT.mkdir(parents=True,exist_ok=False)
k=ctypes.WinDLL('kernel32',use_last_error=True)
k.GetCompressedFileSizeW.argtypes=[W.LPCWSTR,ctypes.POINTER(W.DWORD)];k.GetCompressedFileSizeW.restype=W.DWORD
k.CreateFileW.argtypes=[W.LPCWSTR,W.DWORD,W.DWORD,ctypes.c_void_p,W.DWORD,W.DWORD,W.HANDLE];k.CreateFileW.restype=W.HANDLE
k.CloseHandle.argtypes=[W.HANDLE]
class Info(ctypes.Structure):
    _fields_=[('attrs',W.DWORD),('created',W.FILETIME),('accessed',W.FILETIME),('written',W.FILETIME),('volume',W.DWORD),('sizeHigh',W.DWORD),('sizeLow',W.DWORD),('links',W.DWORD),('indexHigh',W.DWORD),('indexLow',W.DWORD)]
k.GetFileInformationByHandle.argtypes=[W.HANDLE,ctypes.POINTER(Info)];k.GetFileInformationByHandle.restype=W.BOOL
class Standard(ctypes.Structure):
    _fields_=[('allocation',ctypes.c_longlong),('end',ctypes.c_longlong),('links',W.DWORD),('deletePending',ctypes.c_byte),('directory',ctypes.c_byte)]
k.GetFileInformationByHandleEx.argtypes=[W.HANDLE,ctypes.c_int,ctypes.c_void_p,W.DWORD];k.GetFileInformationByHandleEx.restype=W.BOOL
FILE_FIELDS=['Path','Relative','LogicalBytes','AllocatedBytes','HardLinks','FileId','ModifiedNs']
files=[];errors=[];skipped=[];folders={'.':{'LogicalBytes':0,'AllocatedPathBytes':0,'Files':0}};unique={};start=time.time()
stack=[ROOT]
while stack:
    folder=stack.pop()
    try: entries=list(os.scandir(folder))
    except OSError as e: errors.append({'Path':str(folder),'Error':str(e)});continue
    for entry in entries:
        p=Path(entry.path);rel=str(p.relative_to(ROOT)).replace('\\','/')
        try:
            st=entry.stat(follow_symlinks=False)
            if getattr(st,'st_file_attributes',0)&0x400: skipped.append({'Path':str(p),'Reason':'reparse point not followed'});continue
            if entry.is_dir(follow_symlinks=False):
                folders.setdefault(rel,{'LogicalBytes':0,'AllocatedPathBytes':0,'Files':0});stack.append(p);continue
            hi=W.DWORD();ctypes.set_last_error(0);lo=k.GetCompressedFileSizeW(str(p),ctypes.byref(hi));err=ctypes.get_last_error()
            allocated=(hi.value<<32)|lo
            if lo==0xFFFFFFFF and err: allocated=st.st_size;errors.append({'Path':str(p),'Error':'allocation query failed '+str(err)})
            h=k.CreateFileW(str(p),0,7,None,3,0,None);fid='';links=1
            if h not in (None,ctypes.c_void_p(-1).value):
                info=Info();standard=Standard()
                if k.GetFileInformationByHandleEx(h,1,ctypes.byref(standard),ctypes.sizeof(standard)): allocated=standard.allocation
                else: errors.append({'Path':str(p),'Error':'FileStandardInfo allocation query failed'})
                if k.GetFileInformationByHandle(h,ctypes.byref(info)):
                    fid=f'{info.volume}:{info.indexHigh}:{info.indexLow}';links=info.links
                else: errors.append({'Path':str(p),'Error':'File identity query failed '+str(ctypes.get_last_error())})
                k.CloseHandle(h)
            else: errors.append({'Path':str(p),'Error':'Metadata handle unavailable '+str(ctypes.get_last_error())})
            key=fid or rel;unique.setdefault(key,allocated)
            files.append({'Path':str(p),'Relative':rel,'LogicalBytes':st.st_size,'AllocatedBytes':allocated,'HardLinks':links,'FileId':fid,'ModifiedNs':st.st_mtime_ns})
            parts=rel.split('/')
            for name in ['.']+['/'.join(parts[:i]) for i in range(1,len(parts))]:
                agg=folders.setdefault(name,{'LogicalBytes':0,'AllocatedPathBytes':0,'Files':0});agg['LogicalBytes']+=st.st_size;agg['AllocatedPathBytes']+=allocated;agg['Files']+=1
        except OSError as e: errors.append({'Path':str(p),'Error':str(e)})
def csvwrite(name,rows,fields):
    with (OUT/name).open('w',newline='',encoding='utf-8-sig') as f:
        w=csv.DictWriter(f,fields);w.writeheader();w.writerows(rows)
csvwrite('files.csv',files,FILE_FIELDS)
dirs=[{'Relative':name,**data} for name,data in folders.items()]
csvwrite('directories.csv',sorted(dirs,key=lambda x:x['LogicalBytes'],reverse=True),list(dirs[0]))
big=[f for f in files if f['LogicalBytes']>100*1024**2]
csvwrite('files-over-100MiB.csv',big,FILE_FIELDS)
summary={'Root':str(ROOT),'UTC':time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime()),'LogicalBytes':sum(x['LogicalBytes'] for x in files),'AllocatedPathBytes':sum(x['AllocatedBytes'] for x in files),'UniqueAllocatedBytes':sum(unique.values()),'Files':len(files),'Directories':len(folders),'HardlinkedPaths':sum(x['HardLinks']>1 for x in files),'Errors':errors,'SkippedReparsePoints':skipped,'ElapsedSeconds':round(time.time()-start,2),'FileIdUnavailable':sum(not f['FileId'] for f in files),'BudgetGiB':args.warn_gib,'BudgetExceeded':sum(unique.values())>args.warn_gib*1024**3,'AllocationNote':'NTFS FileStandardInfo AllocationSize; unique file IDs deduplicate hardlinks. Directory/MFT/alternate streams not included.'}
(OUT/'summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(summary,ensure_ascii=False,indent=2))
print('TOP LEVEL')
for d in sorted([d for d in dirs if d['Relative']=='.' or '/' not in d['Relative']],key=lambda x:x['LogicalBytes'],reverse=True):print(d)


if summary['BudgetExceeded']:print('WARNING: space budget exceeded; review duplicates and pinned evidence. Nothing deleted.')
if args.fail_over_budget and summary['BudgetExceeded']:sys.exit(2)
