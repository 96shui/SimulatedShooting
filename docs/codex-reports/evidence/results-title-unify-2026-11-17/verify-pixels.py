"""Measure glyph ink from unmodified Unity UI PNGs, without redrawing images."""
from pathlib import Path
import hashlib
import json
import math
import numpy as np
from PIL import Image

HERE = Path(__file__).resolve().parent
FOLDER = HERE / 'final'
measurements = json.loads((FOLDER / 'title-measurements.json').read_text(encoding='utf8'))['titles']
rows = []
for m in measurements[:2]:
    filename = 'Screen_' + m['source'].removeprefix('runtime-') + '.png'
    path = FOLDER / filename
    image = Image.open(path).convert('RGB')
    assert image.size == (1920, 1080)
    x0, x1 = math.ceil(m['rectMin']['x']), math.floor(m['rectMax']['x'])
    y0, y1 = math.ceil(1080 - m['rectMax']['y']), math.floor(1080 - m['rectMin']['y'])
    pixels = np.array(image)[y0:y1, x0:x1].astype(np.int16)
    # Warm white title ink; excludes the blue/cyan hologram and dark background.
    mask = (pixels.min(axis=2) >= 140) & (pixels.max(axis=2) - pixels.min(axis=2) <= 40)
    y, x = np.where(mask)
    assert len(x), filename
    left, top, right, bottom = int(x.min() + x0), int(y.min() + y0), int(x.max() + x0 + 1), int(y.max() + y0 + 1)
    row = dict(file=filename, image_size=list(image.size),
               title_roi_exclusive=[x0, y0, x1, y1], ink_bbox_exclusive=[left, top, right, bottom],
               ink_width=right-left, ink_height=bottom-top,
               left_rect_whitespace=left-m['rectMin']['x'], right_rect_whitespace=m['rectMax']['x']-right,
               left_panel_whitespace=left-m['panelMin']['x'], right_panel_whitespace=m['panelMax']['x']-right,
               ink_pixels=len(x), sha256=hashlib.sha256(path.read_bytes()).hexdigest())
    assert row['left_rect_whitespace'] > 0 and row['right_rect_whitespace'] > 0
    assert m['allCharactersVisible'] and not m['overflowing'] and not m['truncated'] and m['lines'] == 1
    rows.append(row)

closeup = FOLDER / 'Screen_ZeroingFinalRating_Title_Closeup.png'
assert Image.open(closeup).size == (1920, 1080)
result = dict(render='Unity Camera.Render / TacticalUIGallery preview scene / RGB24',
              coordinate_system='PNG top-left origin; rectangles have exclusive right/bottom',
              ink_rule='min(R,G,B)>=140 and max(R,G,B)-min(R,G,B)<=40 inside actual title RectTransform',
              screenshots=rows, closeup_sha256=hashlib.sha256(closeup.read_bytes()).hexdigest())
(HERE / 'pixel-verification.json').write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf8')
for row in rows:
    print(row['file'], 'ink', row['ink_width'], 'x', row['ink_height'],
          'text-box whitespace', row['left_rect_whitespace'], '/', row['right_rect_whitespace'])
