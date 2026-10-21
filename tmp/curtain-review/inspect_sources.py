import json
from pathlib import Path
from PIL import Image, ImageDraw
import numpy as np

root=Path('E:/Unity/DecorIt')
entries=json.loads((root/'ArtSource/Curtains/rebuild-manifest.json').read_text())['entries']
for part in range(4):
    sheet=Image.new('RGB',(1400,920),'#dedede')
    draw=ImageDraw.Draw(sheet)
    for i,e in enumerate(entries[part*14:(part+1)*14]):
        im=Image.open(root/e['source']).convert('RGBA')
        a=np.array(im)[:,:,3]
        if e['coverageFromAlpha']:
            print(e['id'],im.size,'alpha mean',round(float(a.mean()/255),3),'zero fraction',round(float((a<10).mean()),3))
        back=Image.new('RGBA',im.size,'#344650')
        back.alpha_composite(im)
        back.thumbnail((190,380))
        x=(i%7)*200;y=(i//7)*460
        sheet.paste(back.convert('RGB'),(x,y+35))
        name=e['id']
        draw.text((x+3,y+3),name[:25],fill='black')
        draw.text((x+3,y+18),name[25:],fill='black')
        crop=im.crop((im.width//2-95,im.height//2-95,im.width//2+95,im.height//2+95))
        close=Image.new('RGBA',crop.size,'#344650');close.alpha_composite(crop)
        sheet.paste(close.convert('RGB'),(x,y+240))
    sheet.save(root/f'tmp/curtain-review/new-sources-{part+1}.jpg',quality=95)
