"""Read-only UI-copy inventory. Diagnostic strings are classified, not silently removed.
XAML attributes and C# literals are review candidates, not a proof of reachability.
"""
from pathlib import Path
import argparse, hashlib, html, json, re

ROOT=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser()
parser.add_argument('output',type=Path)
args=parser.parse_args()
assert not args.output.exists(), 'Preserve previous audit evidence'
args.output.mkdir(parents=True)
rows=[];files=[]
diagnostic=re.compile(r'Validation|Runner|Capture|Checks|Probe|Performance|VisualLab|PerfTrace')
attention=re.compile(r'隔离候选|RC2|RC 验证|测试构建|本轮候选|尚未授权|事实摘要|原子保存|软删除|Schema4|占位|调试|模拟|验收|隔离测试')
for folder in ('src/YanJi.App','src/YanJi.Core','src/YanJi.Data'):
    for p in sorted((ROOT/folder).glob('*')):
        if p.suffix not in ('.cs','.xaml'):continue
        raw=p.read_bytes();text=raw.decode('utf-8-sig');scope='diagnostic-only' if diagnostic.search(p.stem) else 'product-or-data-safety'
        files.append(dict(File=p.relative_to(ROOT).as_posix(),Sha256=hashlib.sha256(raw).hexdigest().upper(),Scope=scope))
        pattern=r'\b(?:Text|Content|Header|Title|ToolTip|AutomationProperties.Name)="([^"]*)"' if p.suffix=='.xaml' else r'(?<!\\)"((?:\\.|[^"\\])*)"'
        for match in re.finditer(pattern,text):
            value=html.unescape(match.group(1))
            if not re.search('[\u3400-\u9fff]',value) or value.startswith('{Binding'):continue
            rows.append(dict(File=p.relative_to(ROOT).as_posix(),Line=text.count('\n',0,match.start())+1,Text=value,Scope=scope,Attention=bool(attention.search(value))))
(args.output/'inventory.json').write_text(json.dumps(dict(Files=files,Strings=rows),ensure_ascii=False,indent=2),encoding='utf-8')
summary=dict(Files=len(files),LiteralCandidates=len(rows),ProductAttention=[r for r in rows if r['Attention'] and r['Scope']=='product-or-data-safety'])
(args.output/'summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(dict(Files=len(files),LiteralCandidates=len(rows),ProductAttention=len(summary['ProductAttention'])),ensure_ascii=False))
