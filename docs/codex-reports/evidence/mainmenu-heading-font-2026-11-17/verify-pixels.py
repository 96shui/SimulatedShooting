"""Reproduce the evidence audit without changing any raw Unity screenshot."""
from pathlib import Path
import hashlib
import json
import shutil
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from importlib.util import spec_from_file_location, module_from_spec

HERE=Path(__file__).resolve().parent
spec=spec_from_file_location('measure', HERE/'measure-font.py')
measure=module_from_spec(spec)
spec.loader.exec_module(measure)

def array(name): return np.asarray(Image.open(HERE/name).convert('RGB'))
def diff(a,b):
    delta=np.abs(a.astype(int)-b.astype(int))
    mask=delta.max(2)>0
    yy,xx=np.where(mask)
    return {'changed_pixels':int(mask.sum()),'max_channel_delta':int(delta.max()),
        'bbox_inclusive':list(map(int,(xx.min(),yy.min(),xx.max(),yy.max()))) if len(xx) else None}

before=array('before/Screen_MainMenu.png')
after=array('after/Screen_MainMenu.png')
outside=np.ones(before.shape[:2],dtype=bool)
outside[215:275,1360:1750]=False
assert np.array_equal(before[outside],after[outside])
checks={'mainmenu_total':diff(before,after),
    'mainmenu_outside_heading_changed_pixels':int(np.any(before!=after,2)[outside].sum())}
for name,box in {'entry01':[1360,330,1750,415], 'entry02':[1360,460,1750,545],
                 'entry03':[1360,590,1750,675], 'entry04':[1360,720,1750,805],
                 'bottom_status':[120,965,1800,1025]}.items():
    x,y,r,b=box
    checks[name]=diff(before[y:b,x:r],after[y:b,x:r])
    assert checks[name]['changed_pixels']==0
for name in ('Screen_MainMenu_WithoutHeadingText','Screen_ModeIntro_Zeroing',
             'Screen_ModeIntro_MovingTarget','Screen_ModeIntro_Trench','Screen_ModeIntro_Urban'):
    checks[name]=diff(array('before/'+name+'.png'),array('after/'+name+'.png'))
    assert checks[name]['changed_pixels']==0
base=array('baseline-9a1c012/HeadingTextOnly.png')[343:413,1360:1750]
final=array('after/HeadingTextOnly.png')[215:275,1360:1750]
final=np.pad(final,((5,5),(0,0),(0,0)))
checks['baseline_vs_final_isolated_center_aligned_390x70']=diff(base,final)
assert np.array_equal(base,final)
checks['isolated_font_metrics']={}
for folder,box in [('baseline-9a1c012',[1360,343,1750,413]),
                   ('before',[1360,215,1750,275]),('after',[1360,215,1750,275])]:
    checks['isolated_font_metrics'][folder]=measure.measure(HERE/folder/'HeadingTextOnly.png',box)
# Retain original screenshots plus clearly labeled, unscaled diagnostic crops.
for source,dest in [('after/Screen_MainMenu.png','Screen_MainMenu.png'),
                    ('after/Screen_MainMenu_Closeup.png','Screen_MainMenu_Closeup.png'),
                    ('baseline-9a1c012/Screen_MainMenu.png','Screen_MainMenu_9a1c012.png')]:
    shutil.copy2(HERE/source,HERE/dest)
font=ImageFont.truetype('C:/Windows/Fonts/consola.ttf',18)
panel=Image.new('RGB',(800,380),(22,25,30))
draw=ImageDraw.Draw(panel)
rows=[('9a1c012 (raw crop; original 390x70)', 'baseline-9a1c012/Screen_MainMenu.png',(1360,343,1750,413)),
      ('Before (raw crop; 390x60)','before/Screen_MainMenu.png',(1360,215,1750,275)),
      ('After (raw crop; 390x60)','after/Screen_MainMenu.png',(1360,215,1750,275))]
for i,(title,name,box) in enumerate(rows):
    y=i*120
    draw.text((12,y+10),title,font=font,fill='white')
    panel.paste(Image.open(HERE/name).crop(box),(12,y+42))
panel.save(HERE/'Heading_Font_Comparison.png')
manifest={p.relative_to(HERE).as_posix():{'size':list(Image.open(p).size),
    'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in HERE.rglob('*.png')}
(HERE/'screenshot-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
(HERE/'pixel-verification.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2),encoding='utf8')
print(json.dumps({k:v for k,v in checks.items() if k!='isolated_font_metrics'},indent=2))
