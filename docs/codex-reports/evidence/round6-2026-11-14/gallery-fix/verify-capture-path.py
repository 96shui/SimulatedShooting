"""Controlled render comparisons for the misleading round6 screenshots; no image editing."""
from pathlib import Path
from PIL import Image
import hashlib
import json

folder=Path(__file__).resolve().parent
replay=folder/'replay'
checks=[]
def check(name,verdict,**details):
    checks.append(dict(check=name,passed=bool(verdict),**details))
for label,page,panel in [('trench','Screen_TrenchHud','Panel_TrenchHud_Left'),('urban','Screen_UrbanBuildingHud','Panel_UrbanBuildingHud_Rooms')]:
    gallery=json.loads((folder/(page+'.layout.json')).read_text())
    capture=json.loads((replay/(label+'-hud-30-120-converted-'+page+'.layout.json')).read_text())
    before={i['name']:i for i in gallery['items']}
    after={i['name']:i for i in capture['items']}
    ids=[panel,'Hud_'+('Trench' if label=='trench' else 'UrbanBuilding')+'_Prompt']
    if label=='urban':ids+=['Hud_UrbanBuilding_TaskState','Button_UrbanBuildingHud_OpenDoor','Button_UrbanBuildingHud_CheckRoom']
    delta=max(abs(before[k]['rect'][a]-after[k]['rect'][a]) for k in ids for a in ('x','y','width','height'))
    check(label+' gallery/converted coordinates unchanged',delta<.1,max_delta_px=delta)
    check(label+' source FOV retained',capture['fieldOfView']==60,field_of_view=capture['fieldOfView'])
    check(label+' target resolution correct',capture['width']==1920 and capture['height']==1080)

full=Image.open(replay/'trench-hud-30-120.png').convert('RGB')
ui=Image.open(replay/'trench-hud-30-120-ui-only.png').convert('RGB')
def cyan(p):
    r,g,b=p;return r<100 and g>120 and b>160
crop=(50,475,560,685)
f=list(full.crop(crop).getdata());u=list(ui.crop(crop).getdata())
missing=sum(cyan(a) and not cyan(b) for a,b in zip(u,f))
check('trench terrain hides existing panel artwork only in scene capture',missing>1000,cyan_pixels_hidden=missing,cyan_pixels_without_terrain=sum(map(cyan,u)))
check('trench original screenshot reproduced byte for byte',hashlib.sha256((folder.parent/'trench-hud-30-120.png').read_bytes()).digest()==hashlib.sha256((replay/'trench-hud-30-120.png').read_bytes()).digest())
scene=Image.open(replay/'urban-hud-30-120-scene-only.png').convert('RGB')
rows=[]
for y in range(750,825):
    count=sum(r>120 and g>70 and r>g*1.1 and b<80 for r,g,b in scene.crop((0,y,1920,y+1)).getdata())
    if count>=1700:rows.append(y)
check('urban long yellow stripes exist without any HUD',bool(rows),scene_only_yellow_stripe_rows=rows)
result=dict(total=len(checks),passed=sum(c['passed'] for c in checks),failed=sum(not c['passed'] for c in checks),checks=checks)
(folder/'capture-path-verification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(result,ensure_ascii=False,indent=2))
assert result['failed']==0
