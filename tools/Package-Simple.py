"""Local RC-Simple packaging; one staging copy, closed ZIP verification, no cleanup."""
from pathlib import Path, PurePosixPath
import argparse
import importlib.util
import io
import json
import subprocess
import zipfile
import html
import struct
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[1]
ART=ROOT/'artifacts/v04-rc-simple'
VERSION='0.4.0-rc.simple.1'
TAG='v'+VERSION
spec=importlib.util.spec_from_file_location('sealed_pack',ROOT/'tools/Package-V04Final.py')
base=importlib.util.module_from_spec(spec);spec.loader.exec_module(base)
git=base.git;sha=base.sha;read=base.read;new=base.new;dump=base.dump;files=base.files;pack=base.pack

def windows():
    assert not git('status','--porcelain'),'Freeze reviewed code before publishing'
    code=git('rev-parse','HEAD');stage=ART/'publish'
    assert not base.licenses.check(stage,'binary')
    reserved={'BUILD-INFO.json','manifest-sha256.json'}
    entries=[(name,data) for name,data in files(stage) if name not in reserved]
    assert not any(k.lower().endswith(('.db','.sqlite','.yanjibak','-wal','-shm')) or k.startswith('data-') for k,_ in entries)
    release=ROOT/('releases/YanJi-v'+VERSION+'-'+code[:8]);release.mkdir(exist_ok=False)
    info={'Version':VERSION,'CodeHead':code,'Tag':TAG,'ExpectedAssembly':VERSION+'+'+code,'DllSha256':sha((stage/'YanJi.dll').read_bytes()),'UserDataIncluded':False}
    path=release/('YanJi-v'+VERSION+'-windows-x64.zip')
    archive=pack(path,entries+[('BUILD-INFO.json',json.dumps(info,indent=2).encode())],code,code,'binary')
    with zipfile.ZipFile(path) as z:
        index=json.loads(z.read('manifest-sha256.json'))['Files'];current={name:data for name,data in files(stage) if name not in reserved}
        assert set(current)==set(index)-{'BUILD-INFO.json'}
        for name,data in current.items():assert sha(data)==index[name]['Sha256']
        for name in z.namelist():assert (stage/Path(*PurePosixPath(name).parts)).resolve().is_relative_to(stage.resolve())
        z.extractall(stage)
        for name,row in index.items():assert sha((stage/name).read_bytes())==row['Sha256']
    dump(ART/('windows-candidate-'+code[:8]+'.json'),{'CodeHead':code,'ReleaseDirectory':str(release),'Archive':archive,'BuildInfo':info,'ActualZipExtraction':True,'SelfContainedCopies':1})
    print(json.dumps({'Release':str(release),'Archive':archive},indent=2))

def final():
    assert not git('status','--porcelain')
    code=git('rev-parse',TAG+'^{commit}');candidates=[read(p) for p in ART.glob('windows-candidate*.json') if read(p)['CodeHead']==code];assert len(candidates)==1
    candidate=candidates[0];head=git('rev-parse','HEAD');release=Path(candidate['ReleaseDirectory'])
    assert git('rev-parse',TAG+'^{commit}')==code
    assert not git('diff',code,head,'--','src','resources','Directory.Build.props','Directory.Packages.props','global.json')
    native_matches=[p.parent for p in ART.glob('native-*/provenance.json') if read(p)['Assembly']==VERSION+'+'+code and (p.parent/'result.json').is_file() and read(p.parent/'result.json')['Status']=='PASS']
    assert len(native_matches)==1,'Select exactly one successful native run of the tagged program'
    native=native_matches[0];result=read(native/'result.json');provenance=read(native/'provenance.json')
    assert result['Status']=='PASS' and result['Samples']==9 and result['Images']==8
    assert provenance['Assembly']==VERSION+'+'+code and provenance['BinarySha256']==candidate['BuildInfo']['DllSha256']
    counters=next(e for e in ET.parse(ART/'core/core-simple.trx').iter() if e.tag.endswith('Counters'))
    assert counters.attrib['passed']=='216' and counters.attrib['failed']=='0'
    assert read(ART/'final-data-and-retention.json')['Status']=='READY FOR FINAL APPROVAL'
    shots=read(native/'screenshots.json');assert len(shots)==8 and len({r['File'].casefold() for r in shots})==8
    gallery=['<!doctype html><html lang="zh-CN"><meta charset="utf-8"><title>RC-Simple · 真实WPF</title><style>body{font:16px system-ui;max-width:1200px;margin:32px auto;background:#f5f3ec;color:#28372e;padding:20px}img{max-width:100%;height:auto}figure{margin:24px 0}</style><h1>YanJi RC-Simple · 原生窗口</h1><p>本候选ZIP解压程序；实际200%DPI。不是网页模拟。物理IME、其他DPI与电源实验未执行。</p>']
    for row in shots:
        name=row['File'];assert PurePosixPath(name).name==name and '\\' not in name
        data=(native/name).read_bytes();assert len(data)==row['Bytes'] and sha(data)==row['Sha256']
        assert data[:8]==b'\x89PNG\r\n\x1a\n' and struct.unpack('>II',data[16:24])==(row['PixelWidth'],row['PixelHeight'])
        assert abs(row['PixelWidth']-row['Width']*row['Dpi']/96)<=2 and abs(row['PixelHeight']-row['Height']*row['Dpi']/96)<=2
        caption=f"{name} · {row['Width']}×{row['Height']} DIP · {row['PixelWidth']}×{row['PixelHeight']} px · DPI {row['Dpi']}"
        gallery.append('<figure><figcaption>'+html.escape(caption)+'</figcaption><img src="'+name+'" alt="'+html.escape(caption,quote=True)+'"></figure>')
    new(native/'comparison.html','\n'.join(gallery+['</html>']).encode())
    raw=io.BytesIO(subprocess.check_output(['git','archive','--format=zip',head],cwd=ROOT))
    def source():
        with zipfile.ZipFile(raw) as z:
            for name in z.namelist():
                if name.endswith('/'):continue
                data=z.read(name);assert len(data)<10*1024*1024 and not name.lower().endswith(('.exe','.dll','.pdb','.db','.sqlite','.yanjibak'))
                yield name,data
    archives=[candidate['Archive']]
    assert sha((release/archives[0]['File']).read_bytes())==archives[0]['Sha256']
    archives.append(pack(release/('YanJi-v'+VERSION+'-source.zip'),source(),code,head,'source'))
    def evidence():
        for name,data in files(ART):
            if PurePosixPath(name).parts[0]=='publish' or PurePosixPath(name).parts[0].endswith('-data'):continue
            yield 'evidence/'+name,data
        for p in sorted((ROOT/'docs').glob('V04-SIMPLE-*.md')):yield 'docs/'+p.name,p.read_bytes()
        yield 'resources/legal/third-party-licenses.json',(ROOT/'resources/legal/third-party-licenses.json').read_bytes()
    archives.append(pack(release/'YanJi-V04-RC-Simple-Review.zip',evidence(),code,head))
    for archive in archives:
        path=release/archive['File'];assert sha(path.read_bytes())==archive['Sha256']
        with zipfile.ZipFile(path) as z:
            assert z.testzip() is None
            index=json.loads(z.read('manifest-sha256.json'))['Files'];assert set(z.namelist())==set(index)|{'manifest-sha256.json'}
            for name,row in index.items():data=z.read(name);assert len(data)==row['Bytes'] and sha(data)==row['Sha256']
    dump(release/'release-manifest.json',{'Status':'READY FOR FINAL APPROVAL','Version':VERSION,'Branch':git('branch','--show-current'),'CodeHead':code,'DocumentationHead':head,'Tag':TAG,'Assembly':provenance['Assembly'],'BinarySha256':provenance['BinarySha256'],'Archives':archives,'CoreTests':216,'NativeChecks':result['Checks'],'NativeImages':8,'NativeTimerSamples':9,'RealUserDataAccessed':False,'NoPublicRelease':True,'NotTested':result['NotTested']})
    new(release/'SHA256SUMS.txt',''.join(r['Sha256']+'  '+r['File']+'\n' for r in archives).encode())
    new(release/'MANUAL-SIMPLE.md',(ROOT/'docs/V04-SIMPLE-MANUAL.md').read_bytes())
    dump(release/'space-summary.json',{'ReleaseBytes':sum(p.stat().st_size for p in release.rglob('*') if p.is_file()),'ArtifactsBytes':sum(p.stat().st_size for p in ART.rglob('*') if p.is_file()),'SelfContainedCopiesCreated':1,'HistoricalFilesDeleted':0})
    print((release/'release-manifest.json').read_text())

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('stage',choices=['windows','final']);args=parser.parse_args()
    windows() if args.stage=='windows' else final()
