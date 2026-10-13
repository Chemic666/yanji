from pathlib import Path
import json,shutil,html
root=Path(__file__).resolve().parents[1]
out=root/'artifacts/v021-review';out.mkdir(exist_ok=True)
source=root/'artifacts/v021-implementation'
for phase in ['before','final']:
    (out/phase).mkdir(exist_ok=True)
    for p in (source/phase).glob('*-native.png'):shutil.copy2(p,out/phase/p.name)
rows=[]
for p in sorted((out/'before').glob('*-native.png')):
    assert (out/'final'/p.name).exists(),p.name
    name=p.name;state='detail' if name.startswith('detail') else name.split('-')[0]
    page='detail' if state=='detail' else name.split('-')[1]
    theme='dark' if '-dark-' in name else 'light';size='900x600' if '900x600' in name else '1200x800'
    rows.append(f'<article data-state="{state}" data-page="{page}" data-theme="{theme}" data-size="{size}"><h2>{html.escape(name)}</h2><div class="pair"><figure><a href="before/{name}"><img loading="lazy" src="before/{name}" alt="旧版 {name}"></a><figcaption>Before · 原生 HWND</figcaption></figure><figure><a href="final/{name}"><img loading="lazy" src="final/{name}" alt="新版 {name}"></a><figcaption>V0.2.1 · 原生 HWND</figcaption></figure></div></article>')
head='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>研迹 V0.2.1 原生配对审查</title><style>body{margin:24px;background:#f6f4ee;color:#29352e;font:16px "Segoe UI",sans-serif}header{max-width:1000px}nav{display:flex;gap:12px;flex-wrap:wrap;margin:24px 0}select{padding:10px;border:1px solid #57665a;border-radius:8px;background:#fefdf9}article{margin:32px 0}h2{font-size:16px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:16px}figure{margin:0}img{display:block;width:100%;height:auto}figcaption{padding:8px}a:focus-visible,select:focus-visible{outline:3px solid #456653;outline-offset:3px}@media(max-width:800px){.pair{grid-template-columns:1fr}}</style><header><h1>研迹 V0.2.1 · 静桌</h1><p>60 对真实 WPF HWND 截图。同一合成 fixture，真实192 DPI（200%），窗口尺寸以 metadata 的 DIP 为准。空库与长文库各28张，加4张详情。点击原图放大；本页无需网络或构建。</p><p>毫秒、范围、整次身份、片段与全部表事实一致；数据库生成的时间和修订ID不同，详见 facts-comparison.json。软件渲染截图不是其他DPI或GPU验收。</p></header><nav aria-label="截图筛选">'''
for attr,values in [('page',['today','calendar','plan','weak','statistics','settings','records','detail']),('state',['empty','filled','detail']),('theme',['light','dark']),('size',['900x600','1200x800'])]:
    head+=f'<label>{attr} <select data-filter="{attr}" aria-label="{attr}"><option value="">全部</option>'+''.join(f'<option>{v}</option>' for v in values)+'</select></label>'
tail='''</nav><main>'''+''.join(rows)+'''</main><script>const filters=[...document.querySelectorAll('select')];function update(){document.querySelectorAll('article').forEach(e=>e.hidden=filters.some(f=>f.value&&e.dataset[f.dataset.filter]!==f.value))}filters.forEach(f=>f.addEventListener('change',update));</script></html>'''
(out/'comparison.html').write_text(head+tail,encoding='utf-8')
for name in ['fixture.json','facts-comparison.json','current-skill-match.json']:
    shutil.copy2(source/name,out/name)
for p in (root/'docs').glob('V021-*.md'):shutil.copy2(p,out/p.name)
for phase in ['before','final']:
    shutil.copy2(source/phase/'screenshots.json',out/phase/'screenshots.json')
assert len(rows)==60
print(f'60 pairs; {len(list(out.rglob("*.png")))} complete local image dependencies')
