from pathlib import Path
import json, subprocess, tempfile
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter


ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'Art/Codex/Iconography'
CACHE=tempfile.TemporaryDirectory(prefix='rimworld-iconography-')
WORK=Path(CACHE.name)
SIZE=1024
EXTENT=896
inventory={}
manifest=[]

def layer(name,index):
    source=ROOT/f'Art/Gene_{name}.xcf'
    if source.name not in inventory:
        result=subprocess.run(['magick','identify','-format','%s|%l|%wx%h|%X,%Y\n',str(source)],check=True,capture_output=True,text=True)
        inventory[source.name]=result.stdout.splitlines()
    path=WORK/f'{name}-{index}.png'
    if not path.exists():
        subprocess.run(['magick',str(ROOT/f'Art/Gene_{name}.xcf')+f'[{index}]','+repage',str(path)],check=True,capture_output=True)
    return Image.open(path).convert('RGBA')

def composed(name,indices):
    ims=[layer(name,i) for i in indices]
    positions=[]
    for i in indices:
        row=inventory[f'Gene_{name}.xcf'][i]
        positions.append(tuple(map(int,row.rsplit('|',1)[1].split(','))))
    left=min(p[0] for p in positions); top=min(p[1] for p in positions)
    right=max(p[0]+im.width for p,im in zip(positions,ims)); bottom=max(p[1]+im.height for p,im in zip(positions,ims))
    result=Image.new('RGBA',(right-left,bottom-top))
    for p,im in zip(positions,ims): result.alpha_composite(im,(p[0]-left,p[1]-top))
    return result

def remove_black(im):
    # Recover coverage against the baked black outline using nearby interior color.
    a=np.asarray(im).astype(float)
    rgb=a[:,:,:3]
    foreground=(rgb.max(axis=2)>3)&(a[:,:,3]>0)
    eroded=np.asarray(Image.fromarray(np.uint8(foreground)*255).filter(ImageFilter.MinFilter(5)))>0
    interior=eroded&(rgb.max(axis=2)>70)
    if not interior.any(): return im
    reference=rgb.copy()
    points=np.argwhere(foreground & ~eroded)
    seeds=np.argwhere(interior)
    for start in range(0,len(points),64):
        chunk=points[start:start+64]
        nearest=((chunk[:,None,:]-seeds[None,:,:])**2).sum(2).argmin(1)
        ref=seeds[nearest]
        reference[chunk[:,0],chunk[:,1]]=rgb[ref[:,0],ref[:,1]]
    coverage=np.clip((rgb*reference).sum(2)/np.maximum((reference*reference).sum(2),1),0,1)
    boundary=~eroded
    a[boundary,:3]=reference[boundary]
    a[boundary,3]*=coverage[boundary]
    a[~foreground,3]=0
    return Image.fromarray(np.uint8(np.clip(a,0,255)))

def clip_to_alpha(im,base):
    a=np.asarray(im).copy();a[:,:,3]=np.asarray(base)[:,:,3]
    return Image.fromarray(a)

def white(im):
    a=np.asarray(im).copy();a[:,:,:3]=255
    return Image.fromarray(a)

def select_color(im,color,background=(0,0,0),tolerance=5):
    a=np.asarray(im).astype(float); rgb=a[:,:,:3]
    base=np.array(background); diff=np.array(color)-base
    amount=np.clip(((rgb-base)*diff).sum(2)/float((diff*diff).sum()),0,1)
    expected=base+amount[:,:,None]*diff
    error=np.linalg.norm(rgb-expected,axis=2)
    a[:,:,:3]=255
    amount[amount<0.08]=0
    valid=(error<tolerance)
    if background==(255,255,255): valid=np.ptp(rgb,axis=2)<3
    a[:,:,3]*=amount*valid
    if max(background)>0 and sum(diff)<0:
        interior=np.asarray(im.getchannel('A').filter(ImageFilter.MinFilter(11)))>250
        a[:,:,3]*=interior
    # Reject detached antialias specks from other shapes with similar hues.
    occupied=a[:,:,3]>2
    seen=np.zeros(occupied.shape,dtype=bool)
    minimum=max(10,im.width*im.height//10000)
    for y,x in np.argwhere(occupied):
        if seen[y,x]: continue
        todo=[(y,x)];seen[y,x]=True;component=[]
        while todo:
            cy,cx=todo.pop();component.append((cy,cx))
            for dy,dx in ((0,1),(0,-1),(1,0),(-1,0),(1,1),(1,-1),(-1,1),(-1,-1)):
                ny,nx=cy+dy,cx+dx
                if 0<=ny<im.height and 0<=nx<im.width and occupied[ny,nx] and not seen[ny,nx]:
                    seen[ny,nx]=True;todo.append((ny,nx))
        if len(component)<minimum:
            for cy,cx in component: a[cy,cx,3]=0
    return Image.fromarray(np.uint8(np.clip(a,0,255)))

def save(category,name,im,sources,mode='white',note=''):
    if mode=='white': im=white(im)
    a=np.asarray(im).copy(); a[a[:,:,3]<3,3]=0; im=Image.fromarray(a)
    bbox=im.getbbox()
    if not bbox: raise ValueError(name+' is empty')
    im=im.crop(bbox); native_size=im.size
    scale=EXTENT/max(im.size)
    im=im.resize((round(im.width*scale),round(im.height*scale)),Image.Resampling.LANCZOS)
    if mode=='white': im=white(im)
    canvas=Image.new('RGBA',(SIZE,SIZE),(255,255,255,0))
    canvas.alpha_composite(im,((SIZE-im.width)//2,(SIZE-im.height)//2))
    target=OUT/category/f'{name}.png';target.parent.mkdir(exist_ok=True)
    canvas.save(target)
    manifest.append({'file':target.relative_to(OUT).as_posix(),'color':mode,'source':sources,'source_content_size':list(native_size),'note':note})

def native(category,name,source,indices,mode='white',note='',transform=None):
    im=composed(source,indices)
    if transform: im=transform(im)
    save(category,name,im,[{'path':f'Art/Gene_{source}.xcf','layers':[{'index':i,'name':inventory[f'Gene_{source}.xcf'][i].split('|')[1]} for i in indices]}],mode,note)

def vanilla(category,name,source,mode='original',transform=None,note=''):
    im=Image.open(ROOT/f'Art/Rimworld art/Gene_{source}.png').convert('RGBA')
    im=transform(im) if transform else remove_black(im)
    save(category,name,im,[{'path':f'Art/Rimworld art/Gene_{source}.png'}],mode,'128 px vanilla source; enlarged for consistent framing. '+note)

# Repeated modifiers first.
native('01_Modifiers','arrow-up','KeenEars',[6])
native('01_Modifiers','arrow-down','DrugResistant',[7])
native('01_Modifiers','cross-no','Blind',[6])
native('01_Modifiers','arrow-curved','RockToss',[5])
native('01_Modifiers','sound-waves-pair','Echolocation',[3])
native('01_Modifiers','sound-waves-single','Echolocation',[3],transform=lambda im:im.crop((0,0,im.width//2,im.height)))
native('01_Modifiers','motion-lines','Giant',[4])
native('01_Modifiers','anger-mark','Rage',[5])
vanilla('01_Modifiers','aggression-burst','Aggressive','white')
native('01_Modifiers','medical-cross','EmergencyReserves_firsttry',[8],note='Clean standalone medical cross layer, whitened; replaces the low-resolution color selection.')

# Body shapes and anatomical building blocks, without feature overlays.
native('02_Anatomy','head-round','Head_Trog',[2])
native('02_Anatomy','head-broad','Head_Trog',[3])
native('02_Anatomy','head-pawn','Docile',[8])
native('02_Anatomy','body-pawn','Docile',[4])
native('02_Anatomy','pawn','Echolocation',[1])
native('02_Anatomy','pawn-giant','Giant',[3])
native('02_Anatomy','pawn-group-member','HerdInstinct',[3])
native('02_Anatomy','head-horned','LargeHorns',[2])
native('02_Anatomy','head-bull','Rage',[3])
native('02_Anatomy','eye-almond','Precognition',[1])
native('02_Anatomy','iris','Blind',[4])
vanilla('02_Anatomy','eye-gray','GrayEyes')
native('02_Anatomy','ear-human','KeenEars',[3,4],'original')
native('02_Anatomy','ear-cow','CowEars',[2,3],'original')
native('02_Anatomy','ear-fin','FinEars',[2,3],'original')
native('02_Anatomy','ear-pointed','EarSmallPointed',[1],'original',note='Removed residual black matte pixels at the source layer edge.',transform=remove_black)
native('02_Anatomy','stomach','HerbivoreStomach',[1])
vanilla('02_Anatomy','stomach-shaded','ArchiteMetabolism')
native('02_Anatomy','heart','InsectPheromones',[2],'original')
native('02_Anatomy','heart-solid','PainReversal',[4])
native('02_Anatomy','broken-bone','PainReversal',[5])
native('02_Anatomy','clawed-hand','RetractableClaws',[2])
native('02_Anatomy','bat-wings','BatWings',[2,3,4],'original')
native('02_Anatomy','tail-beaver','BeaverTail',[1,2],'original')
native('02_Anatomy','tail-demon','DemonTail',[1,2],'original')

# Expressions separate from heads, plus original multicolor mood disks.
native('03_Expressions','eyes-heavy-brow','Head_Trog',[4])
native('03_Expressions','eyes-angry','Rage',[4])
native('03_Expressions','sad-features','Melancholy',[3])
vanilla('03_Expressions','mood-happy','Sanguine')
vanilla('03_Expressions','mood-sad','Depressive')
vanilla('03_Expressions','happy-features','Sanguine','white',lambda im:select_color(im,(70,117,60),(119,173,111),3))

# Symbolic components reused in gene compositions.
native('04_Symbols','female','AlwaysFemale',[1])
native('04_Symbols','male','AlwaysMale',[1])
native('04_Symbols','dna-two-colors','GeneticAtavism',[2],'original')
vanilla('04_Symbols','dna-inbred','Inbred','white')
native('04_Symbols','gear','Industrious',[1])
native('04_Symbols','broken-gear','BioRejection',[1])
native('04_Symbols','chess-knight','Joyless',[2])
native('04_Symbols','insect','InsectPheromones',[4])
native('04_Symbols','moon','Nocturnal',[2])
native('04_Symbols','cloud','Nocturnal',[4])
vanilla('04_Symbols','sleep-z','Sleepy','white',lambda im:select_color(im,(136,136,91),tolerance=2))
vanilla('04_Symbols','sleep-zz','Sleepy')
vanilla('04_Symbols','sleep-zzz','VerySleepy')
vanilla('04_Symbols','radiation','PartialToxicityResistance','white',lambda im:select_color(im,(136,136,91),tolerance=2))
vanilla('04_Symbols','sun-uv','IntenseUVSensitivity')
save('04_Symbols','die',Image.open(OUT/'_sources/die-original.png').convert('RGBA'),[{'path':'Art/Codex/Iconography/_sources/die-original.png','method':'Built-in imagegen; new randomness symbol'}],note='Generated white die with transparent pips and face separations; prompt recorded in generation-prompts.md.')

# Objects with their internal colors retained.
native('05_Objects','flask','DrugResistant',[2,3,4,5],'original',note='Highlight, fluid and bubbles clipped to the base flask silhouette.',transform=lambda im:clip_to_alpha(im,layer('DrugResistant',2)))
native('05_Objects','flask-silhouette','DrugResistant',[2])
native('05_Objects','bubbles','DrugResistant',[5])
native('05_Objects','paintbrush','Melancholy',[5])
native('05_Objects','mushroom','MushroomEater',[3])
native('05_Objects','corn','HerbivoreStomach',[3],'original')
native('05_Objects','meat','MeatDependence',[1],'original')
native('05_Objects','rock','RockToss',[2,3],'original',transform=lambda im:clip_to_alpha(im,layer('RockToss',2)))
native('05_Objects','mountains','EvenTemper',[3,4,5],'original')
native('05_Objects','anvil','Stoic',[1])
native('05_Objects','running-cat','FastReflexes',[1],'original')
vanilla('05_Objects','weight','NakedSpeed','white',lambda im:select_color(im,(61,61,61),tolerance=2),note='Isolated the gray weight and handle; converted to white, with the handle opening transparent.')
save('05_Objects','shirt',Image.open(OUT/'_sources/shirt-original.png').convert('RGBA'),[{'path':'Art/Rimworld art/Gene_NakedSpeed.png'},{'path':'Art/Codex/Iconography/_sources/shirt-original.png','method':'Built-in imagegen; faithful extraction and reconstruction'}],note='White shirt component; torso hidden by the weight faithfully reconstructed from the source outline and visible hem. Prompt recorded in generation-prompts.md.')

# Recover the obscured thermometer from its unobscured left half.
for source,name in [('MaxTemperatureSmallDecrease','thermometer-hot'),('MinTemperatureSmallDecrease','thermometer-cold')]:
    def thermometer(im):
        left=im.crop((0,0,53,128))
        if name=='thermometer-hot':
            a=np.asarray(left).copy();a[27:84,46:53]=(178,96,96,255);left=Image.fromarray(a)
        reconstructed=Image.new('RGBA',(106,128))
        reconstructed.paste(left,(0,0));reconstructed.paste(left.transpose(Image.Transpose.FLIP_LEFT_RIGHT),(53,0))
        return remove_black(reconstructed)
    vanilla('05_Objects',name,source,'original',thermometer,'Occluded right half restored by reflecting the visible left half around x=52.5; arrow omitted. Hot shaft occlusion filled with the original fluid color.')

native('06_Patterns','mineral-patches','MineralizedSkin',[4])
native('06_Patterns','scale-mesh','Scaleskin',[3])
native('06_Patterns','facial-stripes','FacialStripes',[1],transform=lambda im:select_color(im,(170,170,170),(255,255,255),8))

# A labeled catalog is a preview only; component files always retain transparency.
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',15)
heading=ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf',22)
cols=7; cellw=190; cellh=172
height=sum(52+((sum(r['file'].startswith(c+'/') for r in manifest)+cols-1)//cols)*cellh for c in sorted({r['file'].split('/')[0] for r in manifest}))+30
sheet=Image.new('RGB',(cols*cellw+32,height),'#343b45');draw=ImageDraw.Draw(sheet)
y=12
for category in sorted({r['file'].split('/')[0] for r in manifest}):
    draw.text((16,y+8),category.replace('_','  '),font=heading,fill='white');y+=52
    items=[r for r in manifest if r['file'].startswith(category+'/')]
    for i,item in enumerate(items):
        x=16+i%cols*cellw;cy=y+i//cols*cellh
        draw.rounded_rectangle((x,cy,x+cellw-10,cy+cellh-9),radius=8,fill='#535b67')
        im=Image.open(OUT/item['file']);im.thumbnail((132,132))
        sheet.paste(im,(x+(cellw-10-im.width)//2,cy+2),im)
        text=Path(item['file']).stem
        draw.text((x+(cellw-10-draw.textlength(text,font=font))//2,cy+139),text,font=font,fill='white')
    y+=((len(items)+cols-1)//cols)*cellh
sheet.save(OUT/'Catalog.jpg',quality=94)
with (OUT/'sources.json').open('w',encoding='utf-8',newline='\r\n') as f: json.dump({'canvas':[SIZE,SIZE],'maximum_content_extent':EXTENT,'components':manifest},f,indent=2);f.write('\n')
CACHE.cleanup()
print(f'Created {len(manifest)} components')
