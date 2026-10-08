import imageio_ffmpeg, subprocess, numpy as np
from PIL import Image
from pathlib import Path
src=Path(r'C:\Users\Administrator\Downloads\Blood_Mist_1_0700.mov')
p=subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-loglevel','error','-i',str(src),'-vf','fps=30,scale=1024:540','-frames:v','100','-f','rawvideo','-pix_fmt','rgb24','-'],stdout=subprocess.PIPE,check=True)
f=np.frombuffer(p.stdout,np.uint8).reshape(-1,540,1024,3)
a=1-f.min(axis=3)/255
energy=a.sum(axis=(1,2)); start=int(np.where(energy>300)[0][0]); frames=f[start:start+64]
atlas=Image.new('RGBA',(2048,2048))
for i,rgb in enumerate(frames):
 x=rgb.astype(np.float32)/255; alpha=1-x.min(axis=2)
 color=np.clip((x-(1-alpha[...,None]))/np.maximum(alpha[...,None],.001),0,1)
 alpha=np.where(alpha>.015,np.clip(alpha*1.25,0,.8),0)
 out=np.dstack((color,np.maximum(alpha,0)))
 im=Image.fromarray((out*255).astype('uint8'),'RGBA').resize((256,256),Image.Resampling.LANCZOS)
 atlas.paste(im,((i%8)*256,(i//8)*256))
atlas.save('Assets/Resources/Combat/Vfx/BloodMist/ActionVFX_BloodMist1.png')
atlas.crop((768,256,1024,512)).save('Temp/blood-converted.png')
print('Source frames',len(f),'start',start,'atlas frames',len(frames),'size',Path('Assets/Resources/Combat/Vfx/BloodMist/ActionVFX_BloodMist1.png').stat().st_size)
