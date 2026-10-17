"""Local calendar candidate packaging. Reuses one staging, validates closed ZIPs, never cleans files."""
from pathlib import Path, PurePosixPath
import argparse, importlib.util, io, json, subprocess, zipfile, html
ROOT=Path(__file__).resolve().parents[1]
ART=ROOT/'artifacts/calendar-restore'
STAGE=ROOT/'artifacts/v04-polish2/publish'
VERSION='0.4.0-rc.calendar.1'
TAG='v'+VERSION
spec=importlib.util.spec_from_file_location('pack_base',ROOT/'tools/Package-V04Final.py')
base=importlib.util.module_from_spec(spec);spec.loader.exec_module(base)
git=base.git;sha=base.sha;read=base.read;dump=base.dump;pack=base.pack

def safe_files(folder):
    # Inspect names before reading. No data/backup payload can enter a release.
    paths=sorted(p for p in folder.rglob('*') if p.is_file())
    for p in paths:
        key=p.relative_to(folder).as_posix()
        assert not p.is_symlink() and p.resolve().is_relative_to(folder.resolve())
        assert not any(part.endswith('-data') or part.startswith('data-') for part in PurePosixPath(key).parts)
        assert not key.lower().endswith(('.db','.sqlite','.sqlite3','.yanjibak','-wal','-shm','.dll.config.user'))
    return [(p.relative_to(folder).as_posix(),p.read_bytes()) for p in paths]

def windows():
    assert not git('status','--porcelain'),'Commit reviewed code first'
    code=git('rev-parse','HEAD')
    assert git('rev-parse',TAG+'^{commit}')==code
    assert not base.licenses.check(STAGE,'binary')
    spec=importlib.util.spec_from_file_location('icons',ROOT/'tools/Verify-AppIcon.py')
    icons=importlib.util.module_from_spec(spec);spec.loader.exec_module(icons)
    dump(ART/'native-icon-final.json',icons.verify(STAGE/'YanJi.exe'))
    entries=[(n,b) for n,b in safe_files(STAGE) if n not in {'BUILD-INFO.json','manifest-sha256.json'}]
    release=ROOT/('releases/YanJi-v'+VERSION+'-'+code[:8]);release.mkdir(exist_ok=False)
    info={'Version':VERSION,'CodeHead':code,'RecoveryTag':TAG,'ExpectedAssembly':VERSION+'+'+code,'DllSha256':sha((STAGE/'YanJi.dll').read_bytes()),'UserDataIncluded':False,'DefaultData':'data-rc-calendar','IconCommit':git('rev-parse','31fe8ee')}
    archive=pack(release/('YanJi-v'+VERSION+'-windows-x64.zip'),entries+[('BUILD-INFO.json',json.dumps(info,indent=2).encode())],code,code,'binary')
    with zipfile.ZipFile(release/archive['File']) as z:
        index=json.loads(z.read('manifest-sha256.json'))['Files']
        for name,data in entries:assert sha(data)==index[name]['Sha256']
        for name in z.namelist():assert STAGE.joinpath(*PurePosixPath(name).parts).resolve().is_relative_to(STAGE.resolve())
        z.extractall(STAGE) # Exact verified current bytes; no user data or previous runtime directory removed.
        for name,row in index.items():assert sha((STAGE/name).read_bytes())==row['Sha256']
    dump(ART/'windows-candidate.json',{'BuildInfo':info,'Archive':archive,'ReleaseDirectory':str(release),'ExtractedAndRehashed':True})
    print(json.dumps({'Release':str(release),'Archive':archive},indent=2))

def final():
    assert not git('status','--porcelain')
    c=read(ART/'windows-candidate.json');info=c['BuildInfo'];code=info['CodeHead'];head=git('rev-parse','HEAD');release=Path(c['ReleaseDirectory'])
    assert git('rev-parse',TAG+'^{commit}')==code
    assert not git('diff',code,head,'--','src','resources','Directory.Build.props','Directory.Packages.props','global.json')
    native=ART/'final';result=read(native/'result.json');prov=read(native/'provenance.json');perf=read(native/'performance.json')
    assert result['Status']=='PASS' and perf['ObservedMs']>=125000 and perf['MainMiniSynchronized']
    assert prov['Assembly']==info['ExpectedAssembly'] and prov['DllSha256']==info['DllSha256']
    assert perf['RefreshCount']==1 and perf['Checkpoints']>=7
    photos=read(native/'screenshots.json')
    for row in photos:
        p=native/row['File'];assert p.name==row['File'] and sha(p.read_bytes())==row['Sha256']
    gallery=['<!doctype html><html lang="zh-CN"><meta charset="utf-8"><title>研迹 · 日历恢复验收</title><style>body{font:16px system-ui;max-width:1200px;margin:auto;padding:32px;background:#f5f3ec;color:#28372e}img{width:100%;height:auto;border-radius:8px}figure{margin:32px 0}a{color:#436851}</style><h1>学习数据 · 日历恢复</h1><p>真实WPF窗口，实际200%DPI；所有数据为隔离合成数据。连续页面截图按滚动位置展示完整内容。旧图为空数据，因此前后比较仅用于导航与布局，不比较学习总量。</p>']
    before=ART/'external/before-data-0.jpg'
    if before.is_file():gallery+=['<h2>改造前：日历隐藏在更多功能</h2><img src="external/before-data-0.jpg">']
    for row in photos:
        gallery.append('<figure><figcaption>'+html.escape(row['File'])+f" · {row['Width']}×{row['Height']} DIP · 滚动 {row['Scroll']}"+'</figcaption><img src="final/'+row['File']+'"></figure>')
    gallery.append('<p>外部鼠标点击记录、系统栏截图限制和性能细节请看报告及 external 目录。未实测事项不作为通过结论。</p></html>')
    gallery_bytes='\n'.join(gallery).encode()
    if (ART/'comparison.html').exists():assert (ART/'comparison.html').read_bytes()==gallery_bytes
    else:base.new(ART/'comparison.html',gallery_bytes)
    archives=[c['Archive']]
    raw=io.BytesIO(subprocess.check_output(['git','archive','--format=zip',head],cwd=ROOT))
    with zipfile.ZipFile(raw) as z:
        source=[(n,z.read(n)) for n in z.namelist() if not n.endswith('/')]
    required=['design/branding/yanji-icon-v1.png','src/YanJi/Assets/Icons/YanJi.ico','src/YanJi/Assets/Icons/YanJi.png','tools/Generate-AppIcon.py']
    source_map=dict(source)
    for n in required:
        actual=source_map[n];expected=subprocess.check_output(['git','show',head+':'+n],cwd=ROOT)
        if n.endswith('.py'):assert actual.replace(b'\r\n',b'\n')==expected.replace(b'\r\n',b'\n')  # Git archive honors Windows text checkout.
        else:assert actual==expected  # Never normalize PNG/ICO binary resources.
    assert not any(n.lower().endswith(('.exe','.dll','.pdb','.db','.sqlite','.yanjibak')) or len(b)>10*1024*1024 for n,b in source)
    archives.append(pack(release/('YanJi-v'+VERSION+'-source.zip'),source,code,head,'source'))
    evidence=[]
    for p in sorted(ART.rglob('*')):
        if not p.is_file():continue
        n=p.relative_to(ART).as_posix()
        if any(part.endswith('-data') for part in PurePosixPath(n).parts):continue
        if p.suffix.lower() in {'.db','.sqlite','.sqlite3','.yanjibak','.dll','.exe','.pdb'} or n.endswith(('-wal','-shm')):continue
        evidence.append((n,p.read_bytes()))
    for name in ['CALENDAR-RESTORE.md','CALENDAR-VALIDATION.md','APP-ICON-INTEGRATION.md']:
        evidence.append(('docs/'+name,(ROOT/'docs'/name).read_bytes()))
    archives.append(pack(release/'YanJi-Calendar-Review.zip',evidence,code,head))
    for a in archives:
        assert sha((release/a['File']).read_bytes())==a['Sha256']
        with zipfile.ZipFile(release/a['File']) as z:
            assert z.testzip() is None
            for n,r in json.loads(z.read('manifest-sha256.json'))['Files'].items():
                b=z.read(n);assert sha(b)==r['Sha256'] and len(b)==r['Bytes']
    dump(release/'release-manifest.json',{'Status':'READY FOR MANUAL ACCEPTANCE','CodeHead':code,'DocumentationHead':head,'Tag':TAG,'IconCommit':info['IconCommit'],'Assembly':prov['Assembly'],'DllSha256':prov['DllSha256'],'Archives':archives,'NativeChecks':result['Checks'],'Screenshots':len(photos),'Performance':{k:perf[k] for k in ['ObservedMs','Checkpoints','RefreshCount','MaxGapMs','P95GapMs','Over50ms']},'OriginalIconSourceInSourceZip':True,'AllClosedZipHashes':'PASS','NoRealUserData':True,'NotTested':['physical IME','other system DPI','real sleep/hibernate','taskbar/tray shell-slot screenshot']})
    base.new(release/'SHA256SUMS.txt',''.join(a['Sha256']+'  '+a['File']+'\n' for a in archives).encode())
    print(json.dumps({'Release':str(release),'Archives':archives,'CodeHead':code,'DocumentationHead':head},indent=2))
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('step',choices=['windows','final']);globals()[p.parse_args().step]()
