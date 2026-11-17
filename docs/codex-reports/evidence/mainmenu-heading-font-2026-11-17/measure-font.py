"""Measure real rendered pixels. PNG boxes are half-open; glyph coordinates are inclusive.

Historical screenshots use their original heading rectangles. Only panel-local
coordinates are compared; screenshots are never warped, recolored or relaid out.
Ink: min(RGB)>=80 and max(RGB)-min(RGB)<=45 in the title search window
x=18..179, y=15..panelHeight-16, excluding the luminous capsule edges.
Bright: luminance>=200.
Stroke width proxy: 2*ink area / four-connected digital perimeter, in pixels.
"""
from pathlib import Path
import json
import sys
import numpy as np
from PIL import Image

HERE = Path(__file__).resolve().parent

def pixels(path):
    return np.asarray(Image.open(path).convert('RGB'))

def measure(path, box):
    x0,y0,x1,y1=box
    rgb=pixels(path)[y0:y1,x0:x1].astype(float)
    mask=(rgb.min(2)>=80)&((rgb.max(2)-rgb.min(2))<=45)
    window=np.zeros(mask.shape,dtype=bool)
    window[15:mask.shape[0]-15,18:180]=True
    mask &= window
    yy,xx=np.where(mask)
    assert len(xx), str(path)
    left,right,top,bottom=map(int,(xx.min(),xx.max(),yy.min(),yy.max()))
    ink=int(mask.sum())
    lum=rgb@np.array([.2126,.7152,.0722])
    bright=int(((lum>=200)&mask).sum())
    padded=np.pad(mask.astype(np.int32),1)
    perimeter=int(np.abs(np.diff(padded,axis=0)).sum()+np.abs(np.diff(padded,axis=1)).sum())
    return dict(path=str(path.relative_to(HERE.parent.parent.parent.parent)) if path.is_relative_to(HERE.parent.parent.parent.parent) else str(path),
        panel_box=box, glyph_bbox_local=[left,top,right,bottom],width=right-left+1,height=bottom-top+1,
        left_inset=left,right_inset=x1-x0-1-right,ink_pixels=ink,bright_pixels=bright,
        bright_fraction_of_glyph_bbox=round(bright/((right-left+1)*(bottom-top+1)),6),
        bright_fraction_of_panel=round(bright/mask.size,6),
        mean_ink_luminance=round(float(lum[mask].mean()),4),
        mean_bright_luminance=round(float(lum[(lum>=200)&mask].mean()),4),
        stroke_width_proxy=round(2*ink/perimeter,6))

def main():
    refs=HERE.parent
    cases={
        'accepted-2026-11-15':(refs/'mainmenu-heading-border-2026-11-15/Screen_MainMenu.png',[1360,343,1750,413]),
        'original-2026-11-12':(refs/'ui-chinese-copy-2026-11-12/gallery/Screen_MainMenu.png',[1360,215,1750,285]),
        'reported-2026-11-16':(refs/'mainmenu-heading-restore-2026-11-16/Screen_MainMenu.png',[1360,215,1750,275]),
    }
    for name in ('baseline-9a1c012','before','probe-normal','after'):
        f=HERE/name/'Screen_MainMenu.png'
        if f.exists(): cases[name]=(f,[1360,343,1750,413] if name=='baseline-9a1c012' else [1360,215,1750,275])
    result={name:measure(path,box) for name,(path,box) in cases.items()}
    (HERE/'font-measurements.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    for name,v in result.items(): print(name, {k:v[k] for k in ('width','height','left_inset','right_inset','ink_pixels','bright_pixels','bright_fraction_of_glyph_bbox','mean_ink_luminance','stroke_width_proxy')})
    if '--check' in sys.argv:
        a,b=result['baseline-9a1c012'],result['after' if 'after' in result else 'before']
        for key in ('height','left_inset','right_inset'): assert abs(a[key]-b[key])<=1,(key,a[key],b[key])

if __name__=='__main__': main()
