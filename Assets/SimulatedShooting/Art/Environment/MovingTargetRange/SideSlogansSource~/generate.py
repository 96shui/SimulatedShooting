"""Deterministic typography and five-point star artwork, no external images."""
from pathlib import Path
from math import sin, cos, pi
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parent.parent
font = "C:/Windows/Fonts/simhei.ttf"
for side, groups in [("Left", ["争头功", "争任务", "争第一"]), ("Right", ["老实", "朴实", "扎实"])]:
    im = Image.new("RGBA", (4096, 820), (255, 255, 255, 0))
    draw = ImageDraw.Draw(im)
    white = (255, 255, 255, 255)
    points = []
    for i in range(10):
        a = -pi/2 + i*pi/5
        r = 105 if i % 2 == 0 else 42
        points.append((170 + r*cos(a), 145 + r*sin(a)))
    draw.polygon(points, fill=white)
    draw.text((300, 78), "八一", font=ImageFont.truetype(font, 130), fill=white)
    size = 310 if side == "Left" else 335
    f = ImageFont.truetype(font, size)
    for center, text in zip((740, 2048, 3356), groups):
        bounds = draw.textbbox((0, 0), text, font=f)
        draw.text((center - (bounds[2]-bounds[0])/2, 360-bounds[1]), text, font=f, fill=white, stroke_width=3)
    draw.rectangle((150, 745, 3946, 757), fill=white)
    im.save(root / ("RangeSideSlogan" + side + ".png"))
