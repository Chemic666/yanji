"""Fresh local packaging only. Verification is a separate closed-ZIP reader."""
import argparse,hashlib,html,json,pathlib,shutil,subprocess,zipfile
P=pathlib.Path
def sha(data):return hashlib.sha256(data).hexdigest().upper()
def dump(path,value):path.write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf-8')
def manifest(root,commit):
 files=[{'Path':p.relative_to(root).as_posix(),'Bytes':p.stat().st_size,'Sha256':sha(p.read_bytes())} for p in sorted(root.rglob('*')) if p.is_file() and p.name!='file-manifest.json']
 dump(root/'file-manifest.json',{'Version':'0.2.2','SourceCommit':commit,'Files':files})
def zipdir(root,out):
 with zipfile.ZipFile(out,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
  for p in sorted(root.rglob('*')):
   if p.is_file():z.write(p,p.relative_to(root).as_posix())
def source_expected(commit,destination=None):
 entries=[]
 raw=subprocess.check_output(['git','ls-tree','-r','-z',commit])
 for row in raw.split(b'\0'):
  if row:
   meta,path=row.split(b'\t',1);mode,kind,oid=meta.split();assert kind==b'blob';entries.append((path.decode('utf-8'),oid))
 output=subprocess.check_output(['git','cat-file','--batch'],input=b'\n'.join(oid for _,oid in entries)+b'\n');at=0;result={};contents={}
 for name,oid in entries:
  end=output.index(b'\n',at);header=output[at:end].split();assert header[0]==oid and header[1]==b'blob';size=int(header[2]);at=end+1;data=output[at:at+size];at+=size+1
  result[name]={'Bytes':size,'Sha256':sha(data)}
  contents[name]=data
 if destination is not None:
  # git archive on Windows applies checkout EOL conversion, including explicit PS1 CRLF.
  # Source delivery is canonical commit content, so write original blobs without filters.
  with zipfile.ZipFile(destination,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
   for name,data in contents.items():z.writestr(name,data)
 return result
def main():
 ap=argparse.ArgumentParser();ap.add_argument('--commit',required=True);ap.add_argument('--publish',required=True);ap.add_argument('--capture',required=True);ap.add_argument('--native',required=True);ap.add_argument('--trx',required=True);a=ap.parse_args()
 assert subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip()==a.commit
 stage=P('artifacts/v022/package-stage-'+a.commit[:8]);assert not stage.exists();stage.mkdir()
 output=P('releases/v0.2.2-delivery-'+a.commit[:8]);assert not output.exists();output.mkdir()
 capture=P(a.capture);provenance=json.loads((capture/'provenance.json').read_text(encoding='utf-8'));assert a.commit in provenance['Source'];assert json.loads((capture/'result.json').read_text())['Status']=='PASS'
 images=json.loads((capture/'screenshots.json').read_text());assert len(images)==len(list(capture.glob('*.png')));assert all(sha((capture/i['File']).read_bytes())==i['Sha256'] for i in images)
 runtime=P('releases/YanJi-v0.2.2-win-x64');assert not runtime.exists();shutil.copytree(a.publish,runtime)
 for name in ['V022-DELIVERY.md','V022-DESIGN.md','V022-VALIDATION.md','V022-SKILL-USAGE.md']:shutil.copy2(P('docs')/name,runtime/name)
 shutil.copy2('THIRD-PARTY-NOTICES.md',runtime/'THIRD-PARTY-NOTICES.md');shutil.copytree('releases/YanJi-v0.2.1-win-x64/licenses',runtime/'licenses')
 (runtime/'开始使用.txt').write_text('研迹 V0.2.2 · Windows x64 自包含\n完整解压后双击 YanJi.exe。升级前在旧版导出并校验完整备份，明确退出旧版。默认数据目录仍为 %LOCALAPPDATA%\\YanJi。\n本轮仅验证了独立合成目录，未打开真实用户库。限制见 V022-VALIDATION.md。\n',encoding='utf-8')
 assert not any(p.suffix in ['.sqlite','.db','.yanjibak'] for p in runtime.rglob('*'))
 manifest(runtime,a.commit)
 review=stage/'review';review.mkdir();selected=[i for i in images if '-stats-' in i['File'] and '-lower-' not in i['File'] or i['File'].startswith(('detail-','long-range-','pending-','donut-hover','edge-'))]
 (review/'images').mkdir()
 for item in selected:shutil.copy2(capture/item['File'],review/'images'/item['File'])
 dump(review/'screenshots.json',selected);shutil.copy2(capture/'provenance.json',review/'provenance.json');shutil.copy2(capture/'result.json',review/'batch-result.json')
 for name in ['V022-DELIVERY.md','V022-DESIGN.md','V022-VALIDATION.md','V022-SKILL-USAGE.md']:shutil.copy2(P('docs')/name,review/name)
 cards=''.join(f'<article><h2>{html.escape(i["File"])}</h2><a href="images/{i["File"]}"><img src="images/{i["File"]}" loading="lazy"></a><p>{i["PixelWidth"]}×{i["PixelHeight"]} · actual DPI {i["DpiX"]} · PID {i["OwnerProcessId"]}</p></article>' for i in selected)
 (review/'index.html').write_text('<!doctype html><html lang="zh-CN"><meta charset="utf-8"><title>研迹 V0.2.2 原生审阅</title><style>body{background:#f6f4ee;color:#29352e;font:16px "Microsoft YaHei UI",sans-serif;margin:32px}main{display:grid;grid-template-columns:repeat(auto-fit,minmax(380px,1fr));gap:24px}article{background:#fefdf9;padding:16px;border-radius:12px}h2{font-size:14px;overflow-wrap:anywhere}img{width:100%;height:auto}p{font-size:13px}</style><h1>研迹 V0.2.2 原生窗口</h1><p>同一最终发布程序、owned synthetic 数据、实际 DPI 192。此审阅选择矩阵及详情/边界图片；完整批次见 TestEvidence/capture。没有使用旧截图生成器。</p><p>Source '+a.commit+'</p><main>'+cards+'</main></html>',encoding='utf-8')
 manifest(review,a.commit)
 evidence=stage/'evidence';evidence.mkdir();shutil.copytree(capture,evidence/'capture');shutil.copytree('artifacts/v022/skills',evidence/'skills');shutil.copytree('artifacts/v022/tests/red',evidence/'tdd-red');shutil.copy2(a.trx,evidence/'core-final.trx');shutil.copytree('artifacts/v022/compatibility',evidence/'compatibility',ignore=shutil.ignore_patterns('data','*.sqlite','*.sqlite-*'))
 (evidence/'native').mkdir()
 for p in P(a.native).iterdir():
  if p.is_file() and p.suffix in ['.json','.txt','.csv']:shutil.copy2(p,evidence/'native'/p.name)
 for p in P('artifacts/v022').glob('startup-final-*/*.json'):shutil.copy2(p,evidence/('startup-'+p.parent.name+'-'+p.name))
 shutil.copy2('artifacts/v022-completion.json',evidence/'completion-before-zip-verification.json');manifest(evidence,a.commit)
 packages={}
 for kind,folder in [('win-x64',runtime),('Review',review),('TestEvidence',evidence)]:
  name='YanJi-v0.2.2-'+kind+'.zip';zipdir(folder,output/name);packages[name]={p.relative_to(folder).as_posix():{'Bytes':p.stat().st_size,'Sha256':sha(p.read_bytes())} for p in folder.rglob('*') if p.is_file()}
 source=output/'YanJi-v0.2.2-source.zip';packages[source.name]=source_expected(a.commit,source)
 dump(output/'ZIP-EXPECTED.json',{'SourceCommit':a.commit,'CaptureProvenance':provenance,'Packages':packages})
 print(output)
if __name__=='__main__':main()
