"""Offline read-only Git publication audit. No secret values in output.
Scans every unique object reachable from local refs, not only the current tree.
Heuristics complement review; they do not prove absence of all possible secrets.
"""
from pathlib import Path
import argparse, collections, hashlib, json, re, subprocess
ROOT=Path(__file__).resolve().parents[1]
def git(*args):return subprocess.check_output(['git',*args],cwd=ROOT)
RULES={
 'private-key':rb'-----BEGIN (?:RSA |EC |DSA |OPENSSH |ENCRYPTED )?PRIVATE KEY-----',
 'github-token':rb'\b(?:gh[pousr]_[A-Za-z0-9]{36,255}|github_pat_[A-Za-z0-9_]{40,255})\b',
 'aws-access-id':rb'\b(?:AKIA|ASIA)[A-Z0-9]{16}\b',
 'service-secret':rb'\b(?:sk-(?:proj-|svcacct-)?[A-Za-z0-9_-]{24,255}|sk_live_[A-Za-z0-9]{20,255})\b',
 'credential-url':rb'https?://[A-Za-z0-9_.%+-]+:[A-Za-z0-9_.%+!$&*=~-]{6,}@',
 'literal-secret':rb'(?i)\b(?:api[_-]?key|access[_-]?token|password|client[_-]?secret)\s*[=:]\s*[\x22\x27]([A-Za-z0-9_+./=-]{12,160})[\x22\x27]',
}
PLACEHOLDER=re.compile(rb'(?i)(example|placeholder|dummy|your|test|x{4,}|change.?me|redacted|not.?a.?secret)')
PATHS=re.compile(rb'(?i)[A-Z]:[\\/]+Users[\\/]+[^\\/\r\n\s\x22\x27<>]+')
RISK_NAME=re.compile(r'(?i)(?:^|/)(?:\.env(?:\..*)?|id_rsa|id_ed25519|credentials(?:\..*)?)$|\.(?:pfx|p12|key|sqlite(?:3)?|db|yanjibak|dmp|zip|7z|exe|dll|log)$|(?:^|/)(?:bin|obj|TestResults|releases|artifacts)/')
def audit(output):
 assert not output.exists(),'New evidence output required'
 output.mkdir(parents=True)
 objects={}
 for line in git('rev-list','--objects','--all').splitlines():
  oid,_,name=line.partition(b' ');objects[oid.decode()]=name.decode('utf-8','replace')
 # Read only Git object contents; never enumerate/open runtime databases.
 proc=subprocess.Popen(['git','cat-file','--batch'],cwd=ROOT,stdin=subprocess.PIPE,stdout=subprocess.PIPE)
 findings=[];risks=[];localpaths=[];large=[];counts=collections.Counter();total=0
 headpaths={line.split(b' ',3)[2].split(b'\t')[0].decode() for line in git('ls-tree','-r','HEAD').splitlines()}
 headfiles=git('ls-tree','-r','--name-only','HEAD').decode().splitlines()
 for oid,name in objects.items():
  proc.stdin.write((oid+'\n').encode());proc.stdin.flush();header=proc.stdout.readline().decode().split();kind=header[1];size=int(header[2]);raw=proc.stdout.read(size);assert len(raw)==size;assert proc.stdout.read(1)==b'\n'
  counts[kind]+=1;total+=size
  if kind not in ('blob','commit','tag'):continue
  if kind=='blob' and size>1024*1024:large.append(dict(Object=oid,PathExample=name,Bytes=size,InHead=oid in headpaths))
  if kind=='blob' and (RISK_NAME.search(name) or raw.startswith(b'SQLite format 3\0')):risks.append(dict(Object=oid,PathExample=name,Bytes=size,SQLiteHeader=raw.startswith(b'SQLite format 3\0'),InHead=oid in headpaths))
  binary=b'\0' in raw[:8192]
  for label,pattern in RULES.items():
   if binary and label=='literal-secret':continue
   for m in re.finditer(pattern,raw):
    value=m.group(0)
    if label=='literal-secret' and PLACEHOLDER.search(value):continue
    findings.append(dict(Rule=label,Object=oid,PathExample=name or '<commit-or-tag>',Line=raw.count(b'\n',0,m.start())+1,ValueFingerprint=hashlib.sha256(value).hexdigest()[:16],InHead=oid in headpaths))
  if not binary and PATHS.search(raw):localpaths.append(dict(Object=oid,PathExample=name or '<commit-or-tag>',InHead=oid in headpaths,Occurrences=len(PATHS.findall(raw))))
 proc.stdin.close();assert proc.wait()==0
 authors={}
 for line in git('log','--all','--format=%ae%x09%ce').splitlines():
  for email in line.split(b'\t'):
   digest=hashlib.sha256(email).hexdigest()[:16];authors[digest]={'Fingerprint':digest,'Kind':'GitHub-noreply' if b'noreply.github.com' in email else 'local-example' if email.endswith((b'.local',b'.invalid',b'@example.com')) else 'email-needs-owner-review'}
 result={'Head':git('rev-parse','HEAD').decode().strip(),'Commits':int(git('rev-list','--count','--all')),'ObjectCounts':dict(counts),'TotalUncompressedObjectBytes':total,'AllReachableObjectsScanned':len(objects),'TextMatches':findings,'RiskPaths':risks,'LargeBlobsOver1MiB':large,'LocalUserPathBlobs':localpaths,'AuthorEmailCategories':list(authors.values()),'HeadTrackedFiles':len(headfiles),'HeadRiskNames':[n for n in headfiles if RISK_NAME.search(n)],'Scope':'All objects reachable from --all local refs; each distinct blob once. Reflogs, unreachable objects, LFS remote objects and untracked files are not scanned. Binary OCR/steganography not assessed. No credential validation or external transmission. PathExample is one Git-provided path, not every occurrence. Text values intentionally redacted.'}
 (output/'git-history-scan.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
 print(json.dumps({k:v for k,v in result.items() if k not in ['LocalUserPathBlobs','RiskPaths','LargeBlobsOver1MiB','TextMatches']},ensure_ascii=False,indent=2))
 print('Redacted matches:',len(findings),'risk paths:',len(risks),'local user path blobs:',len(localpaths))
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--output',required=True);args=p.parse_args();audit(Path(args.output).resolve())
