import sys
from PIL import Image, ImageDraw
n=int(sys.argv[1]); cols=int(sys.argv[2]); labels=sys.argv[3].split('|')
w,h=300,320
o=Image.new('RGB',(cols*w,((n+cols-1)//cols)*h),'white'); d=ImageDraw.Draw(o)
for i in range(n):
    o.paste(Image.open(f'g_{i}.png').resize((w,h)),((i%cols)*w,(i//cols)*h)); d.text(((i%cols)*w+6,(i//cols)*h+6),labels[i],fill='black')
o.save(sys.argv[4])
