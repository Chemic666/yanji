"""Compare locked stable NuGet versions and actual packaged libraries with official advisories.
Downloads public advisory JSON only; does not restore, install, upgrade or transmit code.
"""
from pathlib import Path
import argparse, gzip, hashlib, json, re, subprocess, urllib.request
ROOT=Path(__file__).resolve().parents[1]
def fetch(url):
 assert url.startswith('https://api.nuget.org/'),'Official NuGet endpoint required'
 req=urllib.request.Request(url,headers={'User-Agent':'YanJi-local-publication-audit','Accept-Encoding':'identity'})
 raw=urllib.request.urlopen(req,timeout=40).read()
 if raw.startswith(b'\x1f\x8b'):raw=gzip.decompress(raw)
 return raw
def version(v):
 m=re.fullmatch(r'(\d+(?:\.\d+){0,3})(?:-([0-9A-Za-z.-]+))?(?:\+[0-9A-Za-z.-]+)?',v)
 if not m:raise ValueError('Unsupported version/range; manual review required')
 numeric=tuple((list(map(int,m[1].split('.')))+[0]*4)[:4])
 pre=tuple((0,int(s)) if s.isdigit() else (1,s.lower()) for s in m[2].split('.')) if m[2] else ()
 return numeric,0 if m[2] else 1,pre
def contains(v,range_):
 v=version(v)
 if range_[0] in '[(' and range_[-1] in '])':
  inner=range_[1:-1]
  if ',' not in inner:return v==version(inner)
  low,high=map(str.strip,inner.split(',',1))
  return (not low or v>version(low) or v==version(low) and range_[0]=='[') and (not high or v<version(high) or v==version(high) and range_[-1]==']')
 # NuGet unbracketed versions are inclusive lower bounds.
 return v>=version(range_)
def run(out,deps):
 assert not out.exists(),'Preserve previous advisory evidence'
 out.mkdir(parents=True)
 index=fetch('https://api.nuget.org/v3/vulnerabilities/index.json');(out/'index.json').write_bytes(index)
 advisories={};feeds=[]
 for entry in json.loads(index):
  raw=fetch(entry['@id']);(out/(entry['@name']+'.json')).write_bytes(raw)
  feeds.append(dict(Url=entry['@id'],Updated=entry['@updated'],Sha256=hashlib.sha256(raw).hexdigest()))
  for name,items in json.loads(raw).items():
   existing=advisories.setdefault(name.lower(),[])
   for item in items:
    if item not in existing:existing.append(item)
 packages={}
 def add(name,v,source):packages.setdefault((name.lower(),v),[]).append(source)
 files=subprocess.check_output(['git','ls-files','--','*packages.lock.json'],cwd=ROOT).decode().splitlines()
 for name in files:
  j=json.loads((ROOT/name).read_text(encoding='utf-8-sig'))
  for framework,items in j['dependencies'].items():
   for package,item in items.items():
    if 'resolved' in item:add(package,item['resolved'],name+'#'+framework)
 if deps:
  j=json.loads(deps.read_text(encoding='utf-8-sig'))
  for name,item in j['libraries'].items():
   if item.get('type') in ('package','runtimepack'):
    package,v=name.rsplit('/',1);package=package.removeprefix('runtimepack.');add(package,v,'actual-candidate-deps')
 matches=[];unknown=[]
 for (name,v),sources in sorted(packages.items()):
  for advisory in advisories.get(name,[]):
   try:affected=contains(v,advisory['versions'])
   except ValueError:unknown.append(dict(Package=name,Version=v,Advisory=advisory));continue
   if affected:matches.append(dict(Package=name,Version=v,Sources=sources,**advisory))
 result=dict(Status='ADVISORIES FOUND' if matches else 'NOT TESTED - unsupported range' if unknown else 'NO MATCHES IN CURRENT FEED',Packages=[dict(Id=n,Version=v,Sources=s) for (n,v),s in sorted(packages.items())],Matched=matches,Unsupported=unknown,Feeds=feeds,Limits='NuGet feed package-version matching only, not an exploitability proof or complete native/OS audit. Actual shared runtime components require separate Microsoft announcement review.')
 (out/'result.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
 print(json.dumps(dict(Status=result['Status'],PackageVersionPairs=len(packages),Matched=matches,Unsupported=unknown),ensure_ascii=False,indent=2))
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);p.add_argument('--deps',type=Path);a=p.parse_args();run(a.output,a.deps)
