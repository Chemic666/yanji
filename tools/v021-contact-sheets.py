from pathlib import Path
from PIL import Image,ImageDraw
root=Path(__file__).resolve().parents[1]/'artifacts/v021-implementation'
for phase in ['before','after1','after2','final']:
    folder=root/phase
    if not folder.exists():continue
    for state in ['filled','empty']:
        for size in ['900x600','1200x800']:
            for theme in ['light','dark']:
                files=[folder/f'{state}-{page}-{theme}-{size}-native.png' for page in ['calendar','plan','weak','settings','records']]
                if not all(p.exists() for p in files):continue
                sheet=Image.new('RGB',(1500,1100),'#e5e5e5');draw=ImageDraw.Draw(sheet)
                for i,p in enumerate(files):
                    im=Image.open(p);im.thumbnail((750,345));x=(i%2)*750;y=(i//2)*365
                    sheet.paste(im,(x,y+20));draw.text((x+8,y+2),p.name,fill='#222222')
                sheet.save(folder/f'sheet-{state}-{theme}-{size}.jpg')


# Primary surface index, full native originals remain separate.
for phase in ['before','final']:
    folder=root/phase
    for state in ['empty','filled']:
        for theme in ['light','dark']:
            files=[folder/f'{state}-{page}-{theme}-{size}-native.png' for page in ['today','statistics'] for size in ['900x600','1200x800']]
            sheet=Image.new('RGB',(1800,1500),'#e5e5e5');draw=ImageDraw.Draw(sheet)
            for i,p in enumerate(files):
                im=Image.open(p);im.thumbnail((900,700));x=(i%2)*900;y=(i//2)*750
                sheet.paste(im,(x,y+24));draw.text((x+8,y+2),p.name,fill='#222222')
            sheet.save(folder/f'primary-{state}-{theme}.jpg')

