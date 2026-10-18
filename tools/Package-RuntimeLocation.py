"""Seal only the reviewed runtime-location repair; no network, build or Git mutation."""
from pathlib import Path
import argparse, hashlib, importlib.util, io, json, subprocess, sys, zipfile

ROOT=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser()
parser.add_argument('--kind',choices=['runtime-location','ui-copy'],default='runtime-location')
parser.add_argument('--snapshot',action='store_true')
parser.add_argument('--run-name',default='native-final')
parser.add_argument('--input-name',default='build-inputs.json')
options=parser.parse_args()
assert Path(options.run_name).name==options.run_name and Path(options.input_name).name==options.input_name
ART=ROOT/'artifacts'/options.kind
INPUT=ART/options.input_name
def git(*args): return subprocess.check_output(['git',*args],cwd=ROOT)
def sha(data): return hashlib.sha256(data).hexdigest().upper()
def read(path): return json.loads(path.read_text(encoding='utf-8-sig'))
def dump(path,value):
    with path.open('x',encoding='utf-8') as f: json.dump(value,f,ensure_ascii=False,indent=2)

if options.snapshot:
    paths=['src','resources','Directory.Build.props','Directory.Packages.props','global.json','NuGet.Config',
           'LICENSE','NOTICE','COPYRIGHT.md','THIRD-PARTY-NOTICES.md','docs/USER-GUIDE.md',
           'docs/UPGRADE-ROLLBACK.md','docs/V04-RCFINAL-MANUAL.md','docs/V04-SIMPLE-REFERENCES.md',
           'docs/RELEASE-V040.md','docs/ASSET-RIGHTS.md']
    names=set(git('ls-files','--cached','--others','--exclude-standard','-z','--',*paths).decode().split('\0'))-{''}
    dump(INPUT,dict(Files={n:dict(Bytes=(ROOT/n).stat().st_size,Sha256=sha((ROOT/n).read_bytes())) for n in sorted(names)}))
    print('Build input snapshot saved');sys.exit(0)

assert not git('diff','HEAD','--'),'Commit reviewed files before sealing'
head=git('rev-parse','HEAD').decode().strip()
inputs=read(INPUT)
for name,row in inputs['Files'].items():
    raw=(ROOT/name).read_bytes()
    assert sha(raw)==row['Sha256'] and len(raw)==row['Bytes'],name+' changed after snapshot'
    blob=git('show',head+':'+name)
    assert blob==raw or b'\0' not in raw and blob==raw.replace(b'\r\n',b'\n'),name+' differs from committed source'
stage=ART/'publish'
native=read(ART/options.run_name/'first/result.json');reopen=read(ART/options.run_name/'reopen/result.json')
assert native['Status']==reopen['Status']=='PASS' and reopen['Reopened']
dllsha=sha((stage/'YanJi.dll').read_bytes())
assert native['DllSha256']==reopen['DllSha256']==dllsha
assert native['RawMs']==reopen['RawMs']
spec=importlib.util.spec_from_file_location('pack_helpers',ROOT/'tools/Package-V04Final.py')
pkg=importlib.util.module_from_spec(spec);spec.loader.exec_module(pkg)
prefix='UI-Copy' if options.kind=='ui-copy' else 'Runtime-Isolation'
release=ROOT/('releases/YanJi-V040-'+('ui-copy' if options.kind=='ui-copy' else 'runtime-isolation')+'-'+head[:8]);release.mkdir(exist_ok=False)
entries={p.relative_to(stage).as_posix():p.read_bytes() for p in stage.rglob('*') if p.is_file() and p.suffix.lower()!='.pdb'}
assert not any(n.endswith(('.sqlite','.db','-wal','-shm','.yanjibak','.log')) or n.startswith('data-') for n in entries)
assert 'manifest-sha256.json' not in entries and 'BUILD-INFO.json' not in entries
info=dict(SourceCommit=head,Assembly=native['Source'],DllSha256=dllsha,Runtime='10.0.13',
          NormalDataDirectory='%LOCALAPPDATA%/YanJi',TestArguments='--test-mode --data-dir <independent absolute directory>',
          ExistingData='read-only schema4 preflight; no automatic migration or candidate-data import',
          RealUserDataAccess=False,PublicReleaseAuthorized=False,BuildInputsSha256=sha(INPUT.read_bytes()))
entries['BUILD-INFO.json']=json.dumps(info,ensure_ascii=False,indent=2).encode()
entries['build-inputs.json']=INPUT.read_bytes()
entries['RUNTIME-ISOLATION.md']=(ROOT/'docs/RUNTIME-ISOLATION.md').read_bytes()
if options.kind=='ui-copy':
    assert read(ART/options.run_name/'first/copy-result.json')['Status']=='PASS'
    entries['UI-COPY-REVIEW.md']=(ROOT/'docs/UI-COPY-REVIEW.md').read_bytes()
    entries['UI-COPY-CHANGES.json']=(ROOT/'docs/UI-COPY-CHANGES.json').read_bytes()
win=pkg.pack(release/('YanJi-V0.4.0-'+prefix+'-windows-x64.zip'),sorted(entries.items()),head,head,'binary')
with zipfile.ZipFile(io.BytesIO(git('archive','--format=zip',head))) as z:
    source=pkg.pack(release/('YanJi-V0.4.0-'+prefix+'-source.zip'),[(n,z.read(n)) for n in z.namelist() if not n.endswith('/')],head,head,'source')
evidence={}
for folder in (options.run_name+'/first',options.run_name+'/reopen','test-results'):
    for p in (ART/folder).iterdir():
        if p.suffix in ('.json','.png','.trx'): evidence[folder+'/'+p.name]=p.read_bytes()
evidence['RUNTIME-ISOLATION.md']=(ROOT/'docs/RUNTIME-ISOLATION.md').read_bytes()
if options.kind=='ui-copy':
    for name in ('UI-COPY-REVIEW.md','UI-COPY-CHANGES.json'):evidence[name]=(ROOT/'docs'/name).read_bytes()
    evidence['change-scope-final.json']=(ART/'change-scope-final.json').read_bytes()
    for phase in ('before','after-final'):
        for p in (ART/phase).glob('*.json'):evidence['copy-inventory/'+phase+'/'+p.name]=p.read_bytes()
review=pkg.pack(release/('YanJi-V0.4.0-'+prefix+'-Review.zip'),sorted(evidence.items()),head,head)
(release/'SHA256SUMS.txt').write_text(''.join(x['Sha256']+'  '+x['File']+'\n' for x in (win,source,review)),encoding='ascii')
dump(release/'PACKAGE-INFO.json',dict(BuildInfo=info,Archives=[win,source,review],ZipNativeSmoke='pending; see subsequent zip-smoke evidence'))
dump(ART/'candidate.json',dict(Release=str(release),SourceCommit=head,Archives=[win,source,review]))
print(json.dumps(dict(Release=str(release),Commit=head,Archives=[win,source,review]),ensure_ascii=False,indent=2))
