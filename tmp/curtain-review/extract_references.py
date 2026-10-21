import json
from pathlib import Path
from pypdf import PdfReader
from PIL import Image, ImageDraw

root = Path('E:/Unity/DecorIt')
out = root / 'tmp/curtain-review/references'
out.mkdir(parents=True, exist_ok=True)
catalog = json.loads((root / 'Assets/DecorItCurtains/catalog.json').read_text('utf-8-sig'))
reader = PdfReader('E:/Locais/Downloads/CATALOGO CORTINAS E CAPSULA.pdf')
manifest = []
for f in catalog['families']:
    page = reader.pages[f['referencePage']-1]
    images = list(page.images)
    candidates = [i for i in images if i.image.width > 250 and i.image.height > 250]
    largest = max(candidates or images, key=lambda i:i.image.width*i.image.height)
    dest = out / (f['id'] + Path(largest.name).suffix)
    dest.write_bytes(largest.data)
    manifest.append(dict(id=f['id'], name=f['name'], page=f['referencePage'], path=str(dest), size=largest.image.size, mode=f['mode']))
(out/'manifest.json').write_text(json.dumps(manifest,indent=2,ensure_ascii=False),'utf-8')
for batch in range(4):
    rows = manifest[batch*14:(batch+1)*14]
    sheet = Image.new('RGB',(1400,1100),'#20242a')
    draw = ImageDraw.Draw(sheet)
    for j,f in enumerate(rows):
        img = Image.open(f['path']).convert('RGB')
        img.thumbnail((320,230))
        x=(j%4)*350; y=(j//4)*275
        sheet.paste(img,(x,y+30))
        draw.text((x+3,y+5),f"{batch*14+j+1}. {f['id']} / p{f['page']}",fill='white')
    sheet.save(out/f'contact-{batch+1}.jpg')
print(json.dumps(manifest,ensure_ascii=False))
