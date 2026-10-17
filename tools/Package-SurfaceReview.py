"""Local surface candidate: verified working-tree snapshot, no Git mutation or user data."""
from pathlib import Path, PurePosixPath
import hashlib,html,importlib.util,json,re,struct,subprocess,zipfile,sys
ROOT=Path(__file__).resolve().parents[1]
ART=ROOT/'artifacts/surface-refinement'
STAGE=ROOT/'artifacts/v04-polish2/publish'
spec=importlib.util.spec_from_file_location('base',ROOT/'tools/Package-V04Final.py')
base=importlib.util.module_from_spec(spec);spec.loader.exec_module(base)
sha=base.sha
read=base.read
head=base.git('rev-parse','HEAD')
state=read(ART/'sealed-source-state.json')
assert head==state['Head']
for row in state['Files']:
 data=(ROOT/row['Path']).read_bytes()
 assert len(data)==row['Bytes'] and sha(data)==row['Sha256'],row['Path']
provenance=read(ART/'sealed/provenance.json')
assert sha((STAGE/'YanJi.dll').read_bytes())==provenance['DllSha256']
assert read(ART/'sealed/result.json')['Status']=='PASS'
assert read(ART/'after/timer-check.json')['Status']=='PASS'
for folder in ['before','sealed']:
 for row in read(ART/folder/'screenshots.json'):
  data=(ART/folder/row['File']).read_bytes()
  assert len(data)==row['Bytes'] and sha(data)==row['Sha256']
  assert struct.unpack('>II',data[16:24])==(row['Width']*2,row['Height']*2)
for folder in ['before','sealed']:
 states=read(ART/folder/'actual-button-states.json')
 assert len(states)==6
 for row in states:
  if row['State']=='normal':assert not row['IsPressed'] and not row['IsMouseOver']
  elif row['State']=='hover':assert row['IsMouseOver'] and not row['IsPressed']
  else:assert row['IsPressed'] and row['FaceOffsetY']==1.5
allowed={'src/YanJi.App/App.xaml.cs','src/YanJi.App/KnowledgeTreeStyles.xaml','src/YanJi.App/MainViewModel.cs','src/YanJi.App/MainWindow.xaml','src/YanJi.App/MainWindow.xaml.cs','src/YanJi.App/Polish2Validation.cs','src/YanJi.App/Styles.xaml','src/YanJi.App/ThemeService.cs','src/YanJi.App/SurfaceValidation.cs','docs/SOFT-SURFACES.md','docs/SOFT-SURFACES-VALIDATION.md','tools/Package-SurfaceReview.py'}
changed=set(base.git('diff','--name-only').splitlines())|set(base.git('ls-files','--others','--exclude-standard').splitlines())
assert changed<=allowed,changed-allowed
assert not base.git('diff','--cached','--name-only')
release=ROOT/('releases/YanJi-soft-surfaces-'+head[:8]+'-'+state['CompileInputFingerprint'][:8].lower())
resume='--resume' in sys.argv
release.mkdir(exist_ok=resume)
review=ART/'review';review.mkdir(exist_ok=resume)
def dump_once(path,value):
 if path.exists():assert read(path)==value
 else:base.dump(path,value)
for folder in ['before','sealed']:
 for name,data in base.files(ART/folder):
  assert PurePosixPath(name).suffix.lower() in {'.png','.json'}
  dest=review/folder/name;dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(data)
for name in ['timer-check.json','checks.json','provenance.json','result.json']:
 dest=review/'timing'/name;dest.parent.mkdir(exist_ok=True);dest.write_bytes((ART/'after'/name).read_bytes())
for name,data in base.files(ART/'external'):
 dest=review/'external'/name;dest.parent.mkdir(exist_ok=True);dest.write_bytes(data)
for name in ['SOFT-SURFACES.md','SOFT-SURFACES-VALIDATION.md']:(review/name).write_bytes((ROOT/'docs'/name).read_bytes())
for name in ['sealed-source-state.json','publish-menu-final.log']:(review/name).write_bytes((ART/name).read_bytes())
# Retained historical timer evidence is explicitly separate, never relabelled as rerun.
for name in ['performance.json','checks.json','provenance.json']:
 dest=review/'reused-calendar-timing'/name;dest.parent.mkdir(exist_ok=True);dest.write_bytes((ROOT/'artifacts/calendar-restore/final'/name).read_bytes())
parts=['<!doctype html><html lang="zh-CN"><meta charset="utf-8"><title>研迹 · 自然分层验收</title>',
'<style>body{font:16px/1.6 system-ui;max-width:1500px;margin:30px auto;padding:24px;background:#f5f4ed;color:#283b31}h2{margin-top:48px}figure{margin:12px 0}img{display:block;width:100%;height:auto;border-radius:8px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:20px}figcaption{padding:8px 0}.states{display:grid;grid-template-columns:repeat(3,1fr);gap:14px}a{color:#345e47}@media(max-width:800px){.pair,.states{grid-template-columns:1fr}}</style>',
'<h1>研迹 · 自然分层</h1><p>全部图片来自真实 WPF 窗口。浅色/深色、1200×800 / 900×600 DIP，本机200% DPI。左：基线；右：本轮。仅调整共用视觉边界，日历、数据入口及页面结构保留。</p>',
'<p><a href="SOFT-SURFACES.md">设计规范</a> · <a href="SOFT-SURFACES-VALIDATION.md">验证与限制</a> · <a href="sealed-source-state.json">工作区源码指纹</a></p>']
def figure(name,caption):return '<figure><figcaption>'+html.escape(caption)+'</figcaption><img loading="lazy" src="'+html.escape(name,quote=True)+'" alt="'+html.escape(caption,quote=True)+'"></figure>'
def pair(name,title):
 parts.append('<h2>'+html.escape(title)+'</h2><div class="pair">'+figure('before/'+name,'优化前')+figure('sealed/'+name,'优化后')+'</div>')
for theme,label in [('light','浅色'),('dark','深色')]:
 for width in [1200,900]:
  for page,title in [('today','今日'),('progress','学习进度'),('data','学习数据 · 日历直接可见')]:pair(f'{page}-{theme}-{width}.png',f'{title} · {label} · {width}×{800 if width==1200 else 600}')
for name,title in [('input-default.png','输入框默认态'),('input-focused.png','输入框真实焦点'),('context-menu.png','菜单 · 移除默认图标沟槽'),('combo-popup.png','选择器原生弹出层')]:pair(name,title)
for theme,label in [('light','浅色'),('dark','深色')]:
 for state_name,title in [('normal','Normal'),('hover','Hover'),('pressed','Pressed')]:pair(f'{theme}-{state_name}.png',f'典型按钮 · {label} · {title}')
parts+=['<h2>日历近景</h2><p>真实原图像素裁切；原图与crop.json一并保留。</p>',figure('sealed/calendar-detail.png','学习日、今天与选中日期采用背景及文字区分'),figure('external/menu-keyboard-editor.jpg','真实键盘从记录菜单打开编辑界面；取消未保存'),'</html>']
gallery='\n'.join(parts);(review/'comparison.html').write_text(gallery,encoding='utf-8')
for link in re.findall(r'(?:src|href)="([^"]+)"',gallery):assert (review/link).is_file(),link
images={k:{'Bytes':len(v),'Sha256':sha(v)} for k,v in base.files(review) if k.lower().endswith(('.png','.jpg'))}

info={**state,'Version':provenance['Assembly'],'DllSha256':provenance['DllSha256'],'UserDataIncluded':False,'Purpose':'local visual candidate; current HEAD plus uncommitted reviewed working tree','NoPublicRelease':True}
dump_once(review/'BUILD-INFO.json',info)
entries=[]
for name,data in base.files(STAGE):
 if name in ('BUILD-INFO.json','manifest-sha256.json'):continue
 assert not any(s.startswith('data-') for s in PurePosixPath(name).parts)
 assert not name.lower().endswith(('.db','.sqlite','.sqlite3','.yanjibak','-wal','-shm'))
 entries.append((name,data))
windows=release/'YanJi-soft-surfaces-windows-x64.zip'
if resume:
 with zipfile.ZipFile(windows) as z:
  assert z.testzip() is None
  manifest=json.loads(z.read('manifest-sha256.json'))['Files']
  for name,row in manifest.items():assert sha(z.read(name))==row['Sha256'] and len(z.read(name))==row['Bytes']
  assert json.loads(z.read('BUILD-INFO.json'))==info and not base.licenses.check(windows,'binary',True)
 archives=[{'File':windows.name,'Bytes':windows.stat().st_size,'Sha256':sha(windows.read_bytes()),'Entries':len(manifest),'ZipCRC':'PASS','AllEntrySha256':'PASS','LicenseGate':'PASS'}]
else:archives=[base.pack(windows,entries+[('BUILD-INFO.json',json.dumps(info,ensure_ascii=False,indent=2).encode())],head,head,'binary')]
# Actually extract the ZIP's program files over byte-identical, owned staging only.
# Previous staging metadata is retained; new package metadata remains in the ZIP/review.
with zipfile.ZipFile(windows) as z:
 for name,data in entries:
  dest=STAGE.joinpath(*PurePosixPath(name).parts)
  assert dest.resolve().is_relative_to(STAGE.resolve()) and sha(dest.read_bytes())==sha(data)
  assert z.read(name)==data
  z.extract(name,STAGE)
  assert sha(dest.read_bytes())==sha(data)
probe=ART/'extracted-startup';data_dir=ART/'extracted-startup-data'
if not resume:
 assert not probe.exists() and not data_dir.exists()
 process=subprocess.run([str(STAGE/'YanJi.exe'),'--startup-probe',str(probe),'--data-dir',str(data_dir)],cwd=ROOT,timeout=35)
 assert process.returncode==0
assert read(probe/'startup.json')['Status']=='PASS'
for name,data in base.files(probe):
 dest=review/'extracted-startup'/name;dest.parent.mkdir(exist_ok=True);dest.write_bytes(data)
dump_once(review/'zip-program-extraction.json',{'ProgramFiles':len(entries),'Status':'PASS','Method':'actual closed-ZIP program extraction into byte-identical owned staging; no extra runtime copy','Metadata':'new metadata stays in ZIP and review; earlier staging metadata retained','Startup':'PASS; fresh isolated database'})
source_names=set(base.git('ls-files').splitlines())|changed
source=[];repairs=[]
for name in sorted(source_names):
 p=ROOT/name
 if not p.is_file():continue
 assert not any(s in ('bin','obj','artifacts','releases','TestResults','.git') for s in PurePosixPath(name).parts)
 assert not name.lower().endswith(('.db','.sqlite','.sqlite3','.yanjibak','-wal','-shm','.exe','.dll','.pdb'))
 data=p.read_bytes();assert len(data)<10*1024*1024
 source.append((name,data))
assert {'design/branding/yanji-icon-v1.png','src/YanJi/Assets/Icons/YanJi.ico','src/YanJi/Assets/Icons/YanJi.png'}<=set(k for k,_ in source)
source += [('WORKTREE-BUILD-INFO.json',json.dumps(info,ensure_ascii=False,indent=2).encode())]
archives.append(base.pack(release/'YanJi-soft-surfaces-source-snapshot.zip',source,head,head,'source'))
dump_once(review/'source-snapshot-notes.json',{'UncommittedWorkingTree':True,'LicenseBytesPreserved':True,'CompilationInputsIdenticalToFrozenHashes':True,'GitChanged':False})
images={k:{'Bytes':len(v),'Sha256':sha(v)} for k,v in base.files(review) if k.lower().endswith(('.png','.jpg'))}
image_index=review/'image-hashes.json'
if image_index.exists() and read(image_index)!=images:
 old=ART/'packaging-failure/image-hashes-before-startup.json'
 if not old.exists():old.write_bytes(image_index.read_bytes())
image_index.write_text(json.dumps(images,ensure_ascii=False,indent=2),encoding='utf-8')
archives.append(base.pack(release/'YanJi-soft-surfaces-review.zip',base.files(review),head,head))
with zipfile.ZipFile(release/'YanJi-soft-surfaces-review.zip') as z:
 original=json.loads(z.read('image-hashes.json'))
 for name,row in original.items():assert sha(z.read(name))==row['Sha256'] and len(z.read(name))==row['Bytes']
 for link in re.findall(r'(?:src|href)="([^"]+)"',z.read('comparison.html').decode()):assert link in z.namelist()
base.dump(release/'release-manifest.json',{'Status':'READY FOR HUMAN VISUAL REVIEW','Build':info,'Archives':archives,'Images':len(images),'PhysicalIME':'NOT TESTED','OtherSystemDpi':'NOT TESTED','GitCommitted':False,'UserDataIncluded':False})
base.new(release/'SHA256SUMS.txt',''.join(r['Sha256']+'  '+r['File']+'\n' for r in archives).encode())
print(json.dumps({'Release':str(release),'Images':len(images),'Archives':archives,'Startup':'PASS','LicenseBytesPreserved':True},ensure_ascii=False,indent=2))
