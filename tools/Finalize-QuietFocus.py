"""Freeze an already reviewed WPF snapshot: verify Git inputs, repackage runtime.
No compile, Git mutation, user data access, cleanup or network operation.
"""
from pathlib import Path, PurePosixPath
import argparse, hashlib, importlib.util, json, subprocess, zipfile, io
ROOT=Path(__file__).resolve().parents[1]
ART=ROOT/'artifacts/quiet-focus-2-freeze'
MANIFEST=ROOT/'docs/quiet-focus-2/frozen-build-inputs.json'
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def sha(b):return hashlib.sha256(b).hexdigest().upper()
def git(*args):return subprocess.check_output(['git',*args],cwd=ROOT)
def dump(p,v):
 with p.open('x',encoding='utf-8') as f:json.dump(v,f,ensure_ascii=False,indent=2)
def verify(ref):
 ref=git('rev-parse','--verify',ref+'^{commit}').decode().strip();m=read(MANIFEST)
 assert sha(json.dumps(m['Files'],sort_keys=True,separators=(',',':')).encode())==m['WorktreeInputsSha256']
 exact=[];lf=[]
 for name,row in m['Files'].items():
  p=(ROOT/name).resolve();assert p.is_relative_to(ROOT) and '..' not in PurePosixPath(name).parts
  raw=p.read_bytes();assert sha(raw)==row['Sha256'] and len(raw)==row['Bytes'],name+' working bytes drifted'
  blob=git('show',ref+':'+name)
  if blob==raw:exact.append(name)
  else:
   attr=git('check-attr','text','--',name).decode().strip().rsplit(':',1)[-1].strip()
   assert attr in ['set','auto'] and b'\0' not in raw and blob==raw.replace(b'\r\n',b'\n'),name+' Git differs beyond reviewed LF normalization'
   lf.append({'File':name,'BuildBytesSha256':sha(raw),'GitBlobBytesSha256':sha(blob),'Reason':'CRLF to LF only, existing .gitattributes'})
 pro=read(ROOT/'artifacts/chengjing-polish/verified/provenance.json')
 binary=ROOT/'src/YanJi.App/bin/Release/net10.0-windows'
 assert sha((binary/'YanJi.dll').read_bytes())==pro['DllSha256']
 assert pro['Assembly']==m['InformationalVersion']
 return {'Status':'PASS','Commit':ref,'BuildInputSha256':m['WorktreeInputsSha256'],'Assembly':pro['Assembly'],'DllSha256':pro['DllSha256'],'InputCount':len(m['Files']),'ExactGitByteMatches':len(exact),'LfOnlyCount':len(lf),'LfOnly':lf,'SourceRecompiled':False}
def package(ref):
 match=verify(ref);ref=match['Commit'];old=read(ROOT/'artifacts/chengjing-restoration/windows-candidate.json');oldzip=Path(old['ReleaseDirectory'])/old['Archive']['File']
 assert sha(oldzip.read_bytes())==old['Archive']['Sha256']
 spec=importlib.util.spec_from_file_location('package_helpers',ROOT/'tools/Package-V04Final.py');pkg=importlib.util.module_from_spec(spec);spec.loader.exec_module(pkg)
 with zipfile.ZipFile(oldzip) as z:
  assert z.testzip() is None
  original=read_zip=z.read('manifest-sha256.json');listed=json.loads(original)['Files']
  for name,row in listed.items():assert sha(z.read(name))==row['Sha256'] and len(z.read(name))==row['Bytes']
  entries={n:z.read(n) for n in z.namelist() if n not in ['manifest-sha256.json','BUILD-INFO.json','source-inputs.json']}
 assert not any(n.startswith('data-') or n.endswith(('.db','.sqlite','.yanjibak','-wal','-shm','.pdb')) for n in entries)
 binary=ROOT/'src/YanJi.App/bin/Release/net10.0-windows'
 overlays={}
 for name in ['YanJi.dll','YanJi.Core.dll','YanJi.Data.dll']:
  data=(binary/name).read_bytes();overlays[name]={'PreviousSha256':sha(entries[name]),'FrozenSha256':sha(data)};entries[name]=data
 # These referenced dependency bytes have no change; retain the verified RID-specific deps/runtimeconfig.
 for name in ['CommunityToolkit.Mvvm.dll','Microsoft.Data.Sqlite.dll','SQLitePCLRaw.core.dll','SQLitePCLRaw.batteries_v2.dll','SQLitePCLRaw.provider.e_sqlite3.dll']:
  assert (binary/name).read_bytes()==entries[name],name+' dependency mismatch'
 config=json.loads(entries['YanJi.runtimeconfig.json']);assert config['runtimeOptions'].get('includedFrameworks') and not config['runtimeOptions'].get('frameworks')
 guide=(ROOT/'docs/QUIET-FOCUS-2-FROZEN.md').read_bytes()
 for name in ['START-HERE.md','USER-GUIDE.md','RELEASE-NOTES.md']:entries[name]=guide
 entries['source-inputs.json']=MANIFEST.read_bytes()
 info={'Version':'0.4.0-rc.quiet.2','VisualState':'Quiet Focus 2.0 Chengjing frozen 2026-10-18','SourceCommit':ref,**{k:match[k] for k in ['BuildInputSha256','Assembly','DllSha256']},'Recompiled':False,'ManagedOverlays':overlays,'NativeHost':{'SourceZip':old['Archive'],'Sha256':sha(entries['YanJi.exe']),'Metadata':'RC2 native host reused unchanged; actual UI assembly carries visual-polish snapshot version'},'RuntimeConfig':config,'UserDataIncluded':False}
 entries['BUILD-INFO.json']=json.dumps(info,ensure_ascii=False,indent=2).encode()
 release=ROOT/('releases/YanJi-0.4.0-rc.quiet.2-frozen-'+ref[:8]);release.mkdir(exist_ok=False)
 zpath=release/'YanJi-QuietFocus2-Frozen-windows-x64.zip'
 win=pkg.pack(zpath,sorted(entries.items()),ref,ref,'binary')
 stage=ART/'run';stage.mkdir(exist_ok=False)
 with zipfile.ZipFile(zpath) as z:
  for name in z.namelist():assert stage.joinpath(*PurePosixPath(name).parts).resolve().is_relative_to(stage.resolve())
  z.extractall(stage)
  for name,row in json.loads(z.read('manifest-sha256.json'))['Files'].items():assert sha((stage/name).read_bytes())==row['Sha256']
 sourcebytes=io.BytesIO(git('archive','--format=zip',ref))
 with zipfile.ZipFile(sourcebytes) as z:
  sourceentries=[(n,z.read(n)) for n in z.namelist() if not n.endswith('/')]
 # Preserve upstream license bytes, not arbitrary text normalization.
 source=pkg.pack(release/'YanJi-QuietFocus2-Frozen-source.zip',sourceentries,ref,ref,'source')
 result={'Status':'READY FOR MINIMAL RUNTIME VALIDATION','ReleaseDirectory':str(release),'RunDirectory':str(stage),'SourceMatch':match,'Windows':win,'Source':source,'Packaging':'old verified self-contained runtime + exact reviewed managed assemblies; no compiler','NewFullRuntimeDirectories':1}
 dump(ART/'candidate.json',result);dump(release/'CANDIDATE-INDEX.json',result);print(json.dumps(result,ensure_ascii=False,indent=2))
if __name__=='__main__':
 parser=argparse.ArgumentParser();parser.add_argument('mode',choices=['verify','package']);parser.add_argument('--commit',default='HEAD');args=parser.parse_args()
 if args.mode=='verify':print(json.dumps(verify(args.commit),ensure_ascii=False,indent=2))
 else:package(args.commit)
