"""Verify the reported HUD symptoms from Unity RectTransforms and TMP glyph bounds.
Run from repository root: python docs/codex-reports/evidence/round6-2026-11-14/gallery-fix/verify-layout.py
"""
import json
from pathlib import Path
from PIL import Image

FOLDER = Path(__file__).resolve().parent
OLD = FOLDER.parents[1] / 'ui-chinese-copy-2026-11-12' / 'gallery'
checks = []

def check(name, verdict, **detail):
    checks.append(dict(check=name, passed=bool(verdict), **detail))

def end(box, axis):
    return box[axis] + box['width' if axis == 'x' else 'height']

def inside(box, outer):
    return all(box[a] >= outer[a]-.5 and end(box,a) <= end(outer,a)+.5 for a in ('x','y'))

def gap(above, below):
    return below['y'] - end(above,'y')

for page,prefix in [('TrenchHud','Trench'),('UrbanStreetHud','UrbanStreet'),('UrbanBuildingHud','UrbanBuilding')]:
    name='Screen_'+page
    data=json.loads((FOLDER/(name+'.layout.json')).read_text(encoding='utf-8-sig'))
    items={i['name']:i for i in data['items']}
    frame=dict(x=0,y=0,width=1920,height=1080)
    check(name+' resolution',Image.open(FOLDER/(name+'.png')).size==(1920,1080))
    panel='Panel_'+page+('_Rooms' if prefix=='UrbanBuilding' else '_Left')
    check(name+' left panel in viewport',inside(items[panel]['rect'],frame),rect=items[panel]['rect'])
    prompt=items['Hud_'+prefix+'_Prompt']
    state=items['Hud_'+prefix+('_State' if prefix=='Trench' else '_TaskState')]
    for index,line in enumerate(prompt['lines']):
        check(name+f' prompt line {index+1} in viewport',inside(line,frame),glyphs=line)
    for index in range(len(prompt['lines'])-1):
        separation=gap(prompt['lines'][index],prompt['lines'][index+1])
        check(name+' prompt lines do not overlap',separation>0,gap_px=separation)
    if prompt['lines']:
        separation=gap(prompt['lines'][-1],state['lines'][0])
        check(name+' prompt and task state do not overlap',separation>0,gap_px=separation)
    buttons=['Button_UrbanBuildingHud_OpenDoor','Button_UrbanBuildingHud_CheckRoom'] if prefix=='UrbanBuilding' else ['Button_UrbanStreetHud_EnterBuilding'] if prefix=='UrbanStreet' else []
    for button in buttons:
        separation=gap(state['rect'],items[button]['rect'])
        check(name+' task state clear of '+button,separation>0,gap_px=separation,active=items[button]['active'])
    if len(buttons)==2:
        a,b=[items[k]['rect'] for k in buttons]
        check(name+' door and check rectangles separate',end(a,'x')<=b['x'],gap_px=b['x']-end(a,'x'))
    # Cyan artwork edges give an independent pixel comparison against the historical gallery.
    old=Image.open(OLD/(name+'.png')).convert('RGB')
    new=Image.open(FOLDER/(name+'.png')).convert('RGB')
    box=items[panel]['rect'];x,y,w,h=[round(box[k]) for k in ('x','y','width','height')]
    def cyan(a):
        return [r<100 and g>120 and b>160 for r,g,b in a.crop((x,y,x+w,y+h)).getdata()]
    a,b=cyan(old),cyan(new)
    check(name+' old/new left frame cyan pixels identical',a==b,changed_pixels=sum(x!=y for x,y in zip(a,b)),pixels=sum(a))
    def yellow(image):
        return [r>170 and 80<g<220 and b<150 for r,g,b in image.crop((650,750,1270,825)).getdata()]
    a,b=yellow(old),yellow(new)
    check(name+' old/new prompt glyph pixels identical',a==b,changed_pixels=sum(x!=y for x,y in zip(a,b)),pixels=sum(a))

for suffix in ['', '_Floor2', '_Floor3']:
    data=json.loads((FOLDER/('Screen_UrbanBuildingHud'+suffix+'.layout.json')).read_text(encoding='utf-8-sig'))
    items={i['name']:i for i in data['items']}
    for key in ['Hud_UrbanBuilding_Floor','Hud_UrbanBuilding_Rooms']:
        item=items[key]
        check(suffix+' '+key+' complete',not item['overflow'] and bool(item['lines']) and all(inside(line,item['rect']) for line in item['lines']),text=item['text'],glyphs=item['lines'])

result=dict(total=len(checks),passed=sum(c['passed'] for c in checks),failed=sum(not c['passed'] for c in checks),checks=checks)
(FOLDER/'layout-verification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(result,ensure_ascii=False,indent=2))
raise SystemExit(bool(result['failed']))
