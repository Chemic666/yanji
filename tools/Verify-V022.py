"""Independent verification from CLOSED ZIP bytes; no extraction or CRC-only acceptance."""
import hashlib,json,pathlib,re,sys,zipfile
root=pathlib.Path(sys.argv[1]);expected=json.loads((root/'ZIP-EXPECTED.json').read_text(encoding='utf-8'));reports=[];allfiles={}
for name,entries in expected['Packages'].items():
 with zipfile.ZipFile(root/name,'r') as z:
  names=[i.filename for i in z.infolist() if not i.is_dir()];assert len(names)==len(set(names));assert set(names)==set(entries),(name,'entry set')
  observed={}
  for n in names:
   data=z.read(n);actual={'Bytes':len(data),'Sha256':hashlib.sha256(data).hexdigest().upper()};assert actual==entries[n],(name,n);observed[n]=actual
  if 'file-manifest.json' in names:
   manifest=json.loads(z.read('file-manifest.json'));assert manifest['SourceCommit']==expected['SourceCommit']
   for f in manifest['Files']:assert observed[f['Path']]=={'Bytes':f['Bytes'],'Sha256':f['Sha256']}
  if name.endswith('Review.zip'):
   index=z.read('index.html').decode('utf-8');refs=set(re.findall(r'(?:src|href)="(images/[^\"]+)"',index));metadata=json.loads(z.read('screenshots.json'))
   assert refs=={'images/'+i['File'] for i in metadata};assert set(n for n in names if n.endswith('.png'))==refs
   for i in metadata:assert observed['images/'+i['File']]['Sha256']==i['Sha256']
   assert expected['SourceCommit'] in index;assert json.loads(z.read('provenance.json'))==expected['CaptureProvenance']
  if name.endswith('TestEvidence.zip'):
   shots=json.loads(z.read('capture/screenshots.json'));assert set('capture/'+i['File'] for i in shots)==set(n for n in names if n.startswith('capture/') and n.endswith('.png'))
   for i in shots:assert observed['capture/'+i['File']]['Sha256']==i['Sha256']
   assert json.loads(z.read('capture/provenance.json'))==expected['CaptureProvenance']
  reports.append({'Package':name,'Status':'PASS','Entries':len(names),'Bytes':(root/name).stat().st_size,'Sha256':hashlib.sha256((root/name).read_bytes()).hexdigest().upper()});allfiles[name]=observed
(root/'ZIP-VERIFICATION.json').write_text(json.dumps({'Status':'PASS','SourceCommit':expected['SourceCommit'],'Method':'Closed ZIP independent entry byte length and SHA256; Git blob source baseline; manifest/HTML/image/metadata/provenance cross-check','Packages':reports},indent=2),encoding='utf-8')
(root/'ZIP-ENTRY-SHA256.json').write_text(json.dumps(allfiles,indent=2),encoding='utf-8');(root/'sha256.txt').write_text('\n'.join(i['Sha256']+'  '+i['Package'] for i in reports)+'\n',encoding='utf-8');print(json.dumps(reports,indent=2))
