"""Seal explicit visual evidence and a source snapshot. Never reads a database or mutates Git."""
from pathlib import Path
import hashlib, html, importlib.util, json, subprocess, zipfile
ROOT=Path(__file__).resolve().parents[1]
ART=ROOT/'artifacts/chengjing-restoration'
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,v):p.write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
def sha(b):return hashlib.sha256(b).hexdigest().upper()
def main():
 spec=importlib.util.spec_from_file_location('pkg',ROOT/'tools/Package-V04Final.py');pkg=importlib.util.module_from_spec(spec);spec.loader.exec_module(pkg)
 win=read(ART/'windows-candidate.json');inputs=read(ART/'source-inputs.json');release=Path(win['ReleaseDirectory']);final=ART/'final';result=read(final/'result.json')
 assert result['Status']=='PASS' and not result['Preview']
 assert read(final/'provenance.json')['DllSha256']==win['BuildInfo']['DllSha256']
 assert sha((release/win['Archive']['File']).read_bytes())==win['Archive']['Sha256']
 for name,row in inputs['Files'].items():assert sha((ROOT/name).read_bytes())==row['Sha256'],name
 assert not subprocess.check_output(['git','diff','--name-only','HEAD','--','src/YanJi.Core','src/YanJi.Data','resources/knowledge-packages','resources/legal','LICENSE','NOTICE','src/YanJi/Assets/Icons'],cwd=ROOT).strip()
 states=read(final/'actual-button-states.json');assert {'normal','hover','pressed','focused','disabled'}<={r['State'] for r in states}
 assert any(r['State']=='pressed' and r['IsPressed'] for r in states)
 assert (final/'mini-desktop-wgc.jpg').exists() and (final/'main-desktop-wgc.jpg').exists()
 for row in read(final/'screenshots.json'):
  b=(final/row['File']).read_bytes();assert len(b)==row['Bytes'] and sha(b)==row['Sha256']
 review=ART/'review';review.mkdir(exist_ok=False)
 # Explicit top-level capture results only; never recurse into data/work/backups.
 for folder in ['before','final','tests']:
  (review/folder).mkdir()
  for src in (ART/folder).iterdir():
   if src.is_file() and src.suffix.lower() in {'.png','.jpg','.json','.trx','.txt'} and src.name!='continue.request':(review/folder/src.name).write_bytes(src.read_bytes())
 for name in ['windows-candidate.json','source-inputs.json','before-assessment.json']:(review/name).write_bytes((ART/name).read_bytes())
 for src,dest in [(ROOT/'design/quiet-focus-2/concepts/A-inspiration.png','original-A-inspiration.png'),(ROOT/'docs/CHENGJING-RESTORATION.md','DESIGN.md'),(ROOT/'src/YanJi.App/Assets/Atmosphere/README.md','ASSET-SOURCE.md'),(ART/'DELIVERY.md','DELIVERY.md'),(ART/'VALIDATION.md','VALIDATION.md')]: (review/dest).write_bytes(src.read_bytes())
 def fig(path,label):return '<figure><a href="'+path+'"><img src="'+path+'" alt="'+html.escape(label)+'"></a><figcaption>'+html.escape(label)+'</figcaption></figure>'
 g=['<!doctype html><html lang="zh-CN"><meta charset="utf-8"><title>澄境原始设计 · WPF还原审核</title><style>body{font:15px/1.7 system-ui;background:#f3f2e9;color:#283f32;padding:24px;margin:auto;max-width:1800px}h2{margin-top:40px}.compare{display:grid;grid-template-columns:repeat(3,minmax(280px,1fr));gap:16px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:20px}img{display:block;width:100%;height:auto;border-radius:9px}figure{margin:12px 0}figcaption{padding:8px;color:#526353}a{color:#345d49}.detail{height:300px;overflow:auto;background:#e8eadf}.detail img{width:1200px;max-width:none}@media(max-width:1000px){.compare,.pair{grid-template-columns:1fr}}</style><h1>澄境 · 原始设计与真实WPF</h1><p>原始A为用户选定的AI设计概念图，不是实机截图。其余为实际Windows WPF HWND或Windows.Graphics.Capture。原图1536×1024与WPF2400×1600均以3:2比例展示；实际窗口1200×800 DIP，本机200%缩放。点击图片查看原件。</p><p><a href="DELIVERY.md">交付说明</a> · <a href="VALIDATION.md">验证报告</a> · <a href="DESIGN.md">设计规范</a> · <a href="image-hashes.json">图片哈希</a></p><div class="compare">']
 for path,label in [('original-A-inspiration.png','原始A「澄境」· AI设计参考'),('before/today-light-1200.png','修复前RC quiet.1 · 真实WPF（仅增加诊断入口）'),('final/today-light-1200.png','修复后RC quiet.2 · 候选包真实WPF')]:g.append(fig(path,label))
 g.append('</div><h2>双主题、小窗口与空状态</h2><div class="pair">')
 for name in ['today-dark-1200','today-light-900','today-dark-900','empty-light-1200']:g.append(fig('final/'+name+'.png',name))
 g.append('</div><h2>同宽标题/窗景/计时局部（可滚动）</h2><div class="pair">')
 for path in ['before/today-light-1200.png','final/today-light-1200.png']:g.append('<div class="detail">'+fig(path,path)+'</div>')
 g.append('</div><h2>今日页下方内容 · 连续滚动帧</h2><div class="pair">')
 for i in range(3):g.append(fig('final/today-scroll-'+str(i)+'.png','真实滚动位置'+str(i+1)))
 g.append('</div><h2>迷你窗：修复前后真实桌面合成</h2><div class="pair">'+fig('before/mini-desktop-wgc.jpg','修复前：提示文字仍位于工具条下')+fig('final/mini-desktop-wgc.jpg','修复后：提示与工具条互斥')+'</div><div class="pair">')
 for theme in ['light','dark']:
  for state in ['idle','focused']:g.append(fig('final/mini-'+theme+'-'+state+'.png','真实HWND '+theme+' '+state+'；透明最终效果以桌面合成为准'))
 g.append('</div><h2>主窗口桌面捕获与主按钮实际交互状态</h2>'+fig('final/main-desktop-wgc.jpg','Windows.Graphics.Capture主窗口')+'<div class="pair">')
 for row in states:g.append(fig('final/'+row['File'],row['State']+' · 无业务样本使用正式AtelierPrimary；仅样本延迟释放140ms采集Pressed'))
 g.append('</div><h2>保存完成、模式、焦点和菜单</h2><div class="pair">')
 for name in ['completed-study','mode-1-900','mode-2-900','primary-focused','surface-menu']:g.append(fig('final/'+name+'.png',name))
 g.append('</div><p>物理IME、其他系统DPI、真实睡眠/休眠未测试。所有记录均为隔离合成数据。</p></html>')
 (review/'comparison.html').write_text(''.join(g),encoding='utf-8')
 images={p.relative_to(review).as_posix():{'Bytes':p.stat().st_size,'Sha256':sha(p.read_bytes())} for p in sorted(review.rglob('*')) if p.is_file() and p.suffix.lower() in {'.png','.jpg'}};write(review/'image-hashes.json',{'Files':images,'OriginalEvidencePreserved':True})
 tracked=subprocess.check_output(['git','ls-files','-z'],cwd=ROOT).decode().split('\0');new=['docs/CHENGJING-RESTORATION.md','src/YanJi.App/Atelier.xaml','src/YanJi.App/MainWindow.Atelier.cs','src/YanJi.App/AtelierValidation.cs','src/YanJi.App/AtelierValidation.Review.cs','src/YanJi.App/Assets/Atmosphere/window-light-v1.png','src/YanJi.App/Assets/Atmosphere/README.md','tools/Package-ChengjingRestoration.py']
 sources=[(name,(ROOT/name).read_bytes()) for name in sorted(set(n for n in tracked+new if n))];sources += [('BUILD-WORKTREE.json',(ART/'source-inputs.json').read_bytes())]
 source=pkg.pack(release/'YanJi-0.4.0-rc.quiet.2-source.zip',sources,inputs['BaseCommit'],inputs['BaseCommit'],'source')
 audit=pkg.pack(release/'YanJi-Chengjing-Restoration-Review.zip',list(pkg.files(review)),inputs['BaseCommit'],inputs['BaseCommit'])
 with zipfile.ZipFile(release/audit['File']) as z:
  for name,row in images.items():assert sha(z.read(name))==row['Sha256'] and len(z.read(name))==row['Bytes']
 info={'Status':'READY FOR VISUAL REVIEW','Windows':win['Archive'],'Source':source,'Review':audit,'ImageCount':len(images),'ImageManifestFromZip':'PASS','CodeBaseline':inputs['BaseCommit'],'CompiledWorktree':inputs['WorktreeInputsSha256'],'Committed':False,'NoUserData':True}
 write(release/'RELEASE-INDEX.json',info);write(ART/'final-packaging.json',info);print(json.dumps(info,ensure_ascii=False,indent=2))
if __name__=='__main__':main()
