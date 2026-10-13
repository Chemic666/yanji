from pathlib import Path
import re,json
root=Path(__file__).resolve().parents[1]
text=(root/'src/YanJi.App/ThemeService.cs').read_text(encoding='utf-8-sig')
arrays=re.findall(r'new\[\]\{([^}]+)\}',text)
dark,light,keys=[re.findall(r'"([^"]+)"',s) for s in arrays[:3]]
def luminance(color):
    channels=[int(color[i:i+2],16)/255 for i in [1,3,5]]
    linear=[v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4 for v in channels]
    return sum(v*w for v,w in zip(linear,[.2126,.7152,.0722]))
pairs=[('Ink','Canvas'),('Muted','Canvas'),('Ink','CardBackground'),('Muted','CardBackground'),('Ink','Soft'),('Muted','Soft'),('Accent','CardBackground'),('AccentInk','Accent'),('Danger','DangerSoft'),('Warning','WarningSoft')]
rows=[]
for name,colors in [('light',light),('dark',dark)]:
    tokens=dict(zip(keys,colors))
    for foreground,background in pairs:
        a,b=sorted([luminance(tokens[foreground]),luminance(tokens[background])]);ratio=(b+.05)/(a+.05)
        rows.append({'Theme':name,'Foreground':foreground,'Background':background,'Ratio':ratio,'Status':'PASS' if ratio>=4.5 else 'FAIL'})
assert all(r['Status']=='PASS' for r in rows)
(root/'artifacts/v021-implementation/contrast-current.json').write_text(json.dumps(rows,indent=2),encoding='utf-8')
print('20 current token pairs PASS; minimum',round(min(r['Ratio'] for r in rows),2))
