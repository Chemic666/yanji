"""Local publication candidate sealing. No build, network, Git mutation or user data.
Requires reviewed local commit. Always preserves previous archives and diagnostics.
"""
from pathlib import Path,PurePosixPath
import hashlib,importlib.util,io,json,subprocess,zipfile
ROOT=Path(__file__).resolve().parents[1];ART=ROOT/'artifacts/github-release-prep'
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def sha(b):return hashlib.sha256(b).hexdigest().upper()
def git(*a):return subprocess.check_output(['git',*a],cwd=ROOT)
def dump(p,v):
 with p.open('x',encoding='utf-8') as f:json.dump(v,f,ensure_ascii=False,indent=2)
def main():
 head=git('rev-parse','HEAD').decode().strip();inputs=read(ART/'source-inputs.json');stage=ART/'publish'
 assert not git('diff','HEAD','--'),'Commit only reviewed publication files first'
 assert sha(json.dumps(inputs['Files'],sort_keys=True,separators=(',',':')).encode())==inputs['WorktreeInputsSha256']
 normalized=[]
 for name,row in inputs['Files'].items():
  raw=(ROOT/name).read_bytes();assert sha(raw)==row['Sha256'] and len(raw)==row['Bytes'],name+' build input changed'
  blob=git('show',head+':'+name)
  if blob!=raw:
   attr=git('check-attr','text','--',name).decode().rsplit(':',1)[-1].strip()
   assert attr in ('set','auto') and b'\0' not in raw and blob==raw.replace(b'\r\n',b'\n'),name+' Git mismatch'
   normalized.append(name)
 for folder in ('src/YanJi.Core','src/YanJi.Data','resources/knowledge-packages'):
  assert not git('diff',inputs['BaselineCommit'],head,'--',folder),folder+' business source changed'
 config=read(stage/'YanJi.runtimeconfig.json');frameworks=config['runtimeOptions']['includedFrameworks']
 assert {x['version'] for x in frameworks}=={'10.0.13'} and not config['runtimeOptions'].get('frameworks')
 provenance=read(ART/'timer-smoke/provenance.json');screens=read(ART/'screens/provenance.json');timer=read(ART/'timer-smoke/visual-timer.json');perf=read(ART/'runtime-short-perf/result.json')
 dllsha=sha((stage/'YanJi.dll').read_bytes());assert provenance['DllSha256']==screens['DllSha256']==perf['BinarySha256']==dllsha
 assert provenance['Assembly']==inputs['InformationalVersion']
 assert read(ART/'timer-smoke/result.json')['Status']==read(ART/'screens/result.json')['Status']==perf['Status']=='PASS'
 assert timer['SavedMs']+timer['FixtureMs']==timer['TodayMs'] and perf['ExactSavedMilliseconds'] and perf['MainMiniSynchronized']
 assert read(ART/'dependencies-patched/result.json')['Status']=='NO MATCHES IN CURRENT FEED'
 entries={p.relative_to(stage).as_posix():p.read_bytes() for p in stage.rglob('*') if p.is_file()}
 assert not any(n.endswith(('.db','.sqlite','.yanjibak','.pdb','-wal','-shm','.log')) or n.startswith('data-') for n in entries)
 assert not any('StabilityProbe' in n for n in entries),'Never carry earlier diagnostic host DLLs into a release'
 entries['SECURITY.md']=(ROOT/'SECURITY.md').read_bytes();entries['YanJi.ico']=(ROOT/'src/YanJi/Assets/Icons/YanJi.ico').read_bytes()
 entries['source-inputs.json']=(ART/'source-inputs.json').read_bytes()
 info=dict(Status='BLOCKED FOR PUBLICATION - LOCAL REVIEW ONLY',SourceCommit=head,VisualBaseline=inputs['BaselineCommit'],Assembly=inputs['InformationalVersion'],Runtime='10.0.13',DllSha256=dllsha,BuildInputSha256=inputs['WorktreeInputsSha256'],InputCount=len(inputs['Files']),GitLfNormalizationOnly=normalized,Build='one self-contained publish from exact worktree snapshot; no invented embedded final Git hash',PublicVersionAndTag='not selected or created',UserDataIncluded=False,OpenGates=['GitHub target remote not specified','icon and original concept redistribution provenance confirmation','historical author email and local path privacy review'])
 entries['BUILD-INFO.json']=json.dumps(info,ensure_ascii=False,indent=2).encode()
 spec=importlib.util.spec_from_file_location('pack_helpers',ROOT/'tools/Package-V04Final.py');pkg=importlib.util.module_from_spec(spec);spec.loader.exec_module(pkg)
 release=ROOT/('releases/YanJi-V040-publication-candidate-'+head[:8]);release.mkdir(exist_ok=False)
 win=pkg.pack(release/'YanJi-V0.4.0-Publication-Candidate-windows-x64.zip',sorted(entries.items()),head,head,'binary')
 # Re-extract the sealed archive into this same owned, data-free staging directory.
 # All existing files must already be identical; no second full runtime copy.
 with zipfile.ZipFile(release/win['File']) as z:
  for p in stage.rglob('*'):
   if p.is_file():assert p.read_bytes()==entries[p.relative_to(stage).as_posix()]
  for name in z.namelist():assert stage.joinpath(*PurePosixPath(name).parts).resolve().is_relative_to(stage.resolve())
  z.extractall(stage)
  for name,row in read(stage/'manifest-sha256.json')['Files'].items():assert sha((stage/name).read_bytes())==row['Sha256']
 with zipfile.ZipFile(io.BytesIO(git('archive','--format=zip',head))) as z:source_entries=[(n,z.read(n)) for n in z.namelist() if not n.endswith('/')]
 source=pkg.pack(release/'YanJi-V0.4.0-Publication-Candidate-source.zip',source_entries,head,head,'source')
 evidence={}
 for folder in ['screens','timer-smoke','runtime-short-perf']:
  for p in (ART/folder).glob('*.json'):evidence[folder+'/'+p.name]=p.read_bytes()
 for rel in ['history-initial/git-history-scan.json','history-final/git-history-scan.json','dependencies-with-runtime/result.json','dependencies-patched/result.json','source-inputs.json']:
  evidence[rel]=(ART/rel).read_bytes()
 for name in ['GITHUB-RELEASE-AUDIT.md','ASSET-RIGHTS.md','RELEASE-V040.md','V04-FINAL-STABILITY-REPORT.md','V04-FINAL-STABILITY-MANUAL.md']:evidence['docs/'+name]=(ROOT/'docs'/name).read_bytes()
 for p in (ROOT/'docs/screenshots').iterdir():
  if p.is_file():evidence['screenshots/'+p.name]=p.read_bytes()
 evidence['EVIDENCE-SCOPE.txt']=b'Includes all raw JSON of targeted new-runtime checks and four reviewed README PNGs. Full screenshot originals referenced by raw screenshot manifests remain in local artifacts/github-release-prep/screens and timer-smoke; this compact ZIP intentionally contains only selected images. No databases or backups. Publication remains blocked; no remote operations.\n'
 ev=pkg.pack(release/'YanJi-V0.4.0-Publication-Review.zip',sorted(evidence.items()),head,head)
 sums=''.join(x['Sha256']+'  '+x['File']+'\n' for x in [win,source,ev]);(release/'SHA256SUMS.txt').write_text(sums,encoding='ascii')
 result=dict(Status='BLOCKED',Commit=head,ReleaseDirectory=str(release),RunDirectory=str(stage),BuildInfo=info,Archives=[win,source,ev],ZipExtraction='PASS - actual verified in-place extraction, fresh native startup still required',Notes='Source ZIP contains complete Git tree of final commit; not Git history. Reachable history audited separately. Existing releases/tags preserved.')
 dump(ART/'candidate.json',result);dump(release/'PUBLICATION-INDEX.json',result)
 print(json.dumps(dict(Status=result['Status'],Commit=head,Release=str(release),Archives=result['Archives']),ensure_ascii=False,indent=2))
if __name__=='__main__':main()
