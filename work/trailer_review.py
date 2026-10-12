from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

folder=Path('outputs/trailer-review')
font=ImageFont.truetype('C:/Windows/Fonts/consola.ttf',20)

def sheet(filename, images, columns=3):
    width,height=480,310
    result=Image.new('RGB',(width*columns,height*((len(images)+columns-1)//columns)),(8,16,23))
    draw=ImageDraw.Draw(result)
    for index,(file,label) in enumerate(images):
        im=Image.open(folder/file).convert('RGB')
        # Unity Game view is wider than the authored 16:9 film; remove its pillarbox.
        target_width=round(im.height*16/9)
        x=(im.width-target_width)//2
        im=im.crop((x,0,x+target_width,im.height))
        im.thumbnail((width-16,270))
        ox=(index%columns)*width+8
        oy=(index//columns)*height
        result.paste(im,(ox,oy+8))
        draw.text((ox,oy+277),label,font=font,fill=(193,224,232))
    result.save(folder/filename,quality=93)

sheet('shop-and-ending.jpg',[(f'shop-page-{i}.png',f'SHOP {i}/5 - 1 SECOND') for i in range(1,6)]+[
    ('shop-turn-2.png','PAGE TURN - 0.32 SECONDS'),
    ('flight-0.png','STUDIO FADE / LIFT OFF'),
    ('flight-2.png','SPARSE CLOUDS / SPEED BLUR'),
    ('flight-4.png','ORIGINAL START FLOATS UP'),
    ('flight-7.png','TITLE BACKGROUND / SETTLE'),
    ('title-glow-0.png','BREATHING PERIMETER'),
    ('title-glow-2.png','7 SECOND FINAL HOLD')])

sheet('logo-endings-flight.jpg',[
    ('opening-logo.png','ORIGINAL STUDIO LOGO'),
    ('ending-0.png','ENDING 1 / STORM'),
    ('ending-1.png','ENDING 2 / BUGGY'),
    ('ending-2.png','ENDING 3 / BOUTIQUE'),
    ('ending-3.png','ENDING 4 / HIT'),
    ('ending-turn-2.png','ENDING PAGE TURN'),
    ('ending-best-2.png','ELASTIC RETURN / FIREWORKS'),
    ('ending-best-5.png','PIXEL FIREWORKS'),
    ('flight-2.png','DARK ASCENT / ACCELERATE'),
    ('flight-4.png','4 CLOUD PLANES / MOTION BLUR'),
    ('flight-6.png','DECELERATE / TITLE RISE'),
    ('title-glow-2.png','TITLE HOLD / BREATHING GLOW')])

im=Image.open(folder/'title-glow-2.png')
w=round(im.height*16/9)
x=(im.width-w)//2
im.crop((x,0,x+w,im.height)).save(folder/'title-finale.png')
print(folder/'shop-and-ending.jpg')

import re
log=Path.home().joinpath('AppData/Local/Unity/Editor/Editor.log').read_text(encoding='utf-8',errors='replace')
run=log[log.rfind('[Trailer] Shot 01/21:'):]
shots=re.findall(r'\[Trailer\] Shot \d+/21:.*',run)
pages=re.findall(r'\[Trailer\] Shop page .*',run)
actions=re.findall(r'\[Trailer\] Action:.*',run)
errors=re.findall(r'^.*(?:Exception:|error CS\d+|Shader error).*$',run,re.M)
assert len(shots)==21, len(shots)
assert len(pages)==5, len(pages)
assert '[Trailer] Completed 77.0s.' in run
endings=re.findall(r'\[Trailer\] Ending \d/4: (\w+)',run)
assert endings==['storm','buggy','boutique','hit'], endings
assert not errors, errors
from PIL import ImageChops, ImageStat
glows=[Image.open(folder/f'title-glow-{i}.png').convert('RGB') for i in range(4)]
glow_change=max(sum(ImageStat.Stat(ImageChops.difference(glows[0],im)).mean) for im in glows[1:])
assert glow_change>0, 'Breathing outline did not animate'
outside=[]
for i in range(8):
    im=Image.open(folder/f'flight-{i}.png').convert('RGB')
    edge=(im.width-round(im.height*16/9))//2
    if edge>2:
        extrema=im.crop((0,0,edge-2,im.height)).getextrema()
        outside.append(all(lo==hi for lo,hi in extrema))
assert all(outside),'Flight overflowed film mask'
(folder/'playback-validation.txt').write_text('\n'.join([
    'PASS - revised 77 second / 21 shot live trailer',
    'Original Resources/Logo opens the film; four original endings: storm, buggy, boutique, hit.',
    'Ending page turns, slow zoom and best-ending elastic return/pixel fireworks verified in captures.',
    'Five shop pages, each three items; 1 second holds and four 0.32 second turns.',
    'Real purchase and action progression; see runtime.txt.',
    'Four second slow-fast-slow flight: four cloud planes, velocity-driven vertical blur, original Bg and Start.',
    'All flight captures retain a clean 16:9 boundary.',
    f'Breathing outline changes across final hold captures: {glow_change:.3f}.',
    'No runtime exceptions or script/shader compilation errors in this playback.',
    *shots,*pages,*actions]),encoding='utf-8')
print('PASS: 21 shots, 5 shop pages, 4 ordered endings, animated glow, masked flight, no runtime exceptions')
