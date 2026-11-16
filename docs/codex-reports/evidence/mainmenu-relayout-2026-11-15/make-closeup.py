from pathlib import Path
from PIL import Image

folder = Path(__file__).resolve().parent
source = Image.open(folder/'Screen_MainMenu.png').convert('RGB')
assert source.size == (1920,1080)
# Exact Frame bounds in screenshot coordinates (origin top-left).
box = (1300,160,1810,950)
cropped = source.crop(box)
scale = min(1920/cropped.width,1080/cropped.height)
size = (round(cropped.width*scale),round(cropped.height*scale))
zoom = cropped.resize(size,Image.Resampling.LANCZOS)
output = Image.new('RGB',(1920,1080),(7,15,25))
output.paste(zoom,((1920-size[0])//2,(1080-size[1])//2))
output.save(folder/'Screen_MainMenu_Closeup.png')
print('Closeup:',box,'scale:',scale,'content:',size,'output:',output.size)
