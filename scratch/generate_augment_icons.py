import os
import math
from PIL import Image, ImageDraw, ImageFilter

OUTPUT_DIR = "Assets/Sprites/UI/Augments"
os.makedirs(OUTPUT_DIR, exist_ok=True)

SIZE = 128

def create_base():
    return Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))

def draw_sword_slash():
    img = create_base()
    draw = ImageDraw.Draw(img)
    # Crossed curved blades / jagged edge
    # Blade 1 (diagonal)
    draw.polygon([(24, 104), (28, 96), (96, 28), (104, 24), (100, 36), (36, 100)], fill=(255, 255, 255, 255))
    draw.polygon([(18, 110), (28, 100), (24, 96), (14, 106)], fill=(240, 240, 240, 255)) # hilt
    draw.line([(14, 114), (28, 100)], fill=(200, 200, 200, 255), width=3)
    # Blade 2 (crossing)
    draw.polygon([(104, 104), (100, 96), (32, 28), (24, 24), (28, 36), (92, 100)], fill=(255, 255, 255, 240))
    # Serrated teeth on edges
    for i in range(4):
        offset = 40 + i * 14
        draw.polygon([(offset, offset-6), (offset+8, offset), (offset, offset+6)], fill=(255, 255, 255, 255))
    return img

def draw_shield():
    img = create_base()
    draw = ImageDraw.Draw(img)
    # Classic kite / heater shield with cross or gem
    pts = [
        (64, 18), (102, 26), (96, 78), (64, 112), (32, 78), (26, 26)
    ]
    draw.polygon(pts, fill=(255, 255, 255, 255))
    inner = [
        (64, 26), (92, 32), (86, 74), (64, 102), (42, 74), (36, 32)
    ]
    draw.polygon(inner, fill=(180, 180, 180, 255))
    # Central cross in shield
    draw.rectangle([60, 36, 68, 88], fill=(255, 255, 255, 255))
    draw.rectangle([46, 52, 82, 60], fill=(255, 255, 255, 255))
    return img

def draw_divine_shield():
    img = create_base()
    draw = ImageDraw.Draw(img)
    # Angelic radiant shield with wings
    # Outer rays
    for deg in range(0, 360, 30):
        rad = math.radians(deg)
        x1 = 64 + math.cos(rad) * 44
        y1 = 64 + math.sin(rad) * 44
        x2 = 64 + math.cos(rad) * 58
        y2 = 64 + math.sin(rad) * 58
        draw.line([(x1, y1), (x2, y2)], fill=(255, 255, 255, 200), width=4)
    # Shield body
    pts = [(64, 24), (98, 34), (92, 82), (64, 108), (36, 82), (30, 34)]
    draw.polygon(pts, fill=(255, 255, 255, 255))
    # Center holy diamond
    draw.polygon([(64, 44), (78, 64), (64, 84), (50, 64)], fill=(80, 80, 80, 255))
    draw.polygon([(64, 50), (74, 64), (64, 78), (54, 64)], fill=(255, 255, 255, 255))
    return img

def draw_heart():
    img = create_base()
    draw = ImageDraw.Draw(img)
    # Gothic anatomical/gem heart
    draw.pieslice([28, 28, 68, 68], 180, 360, fill=(255, 255, 255, 255))
    draw.pieslice([60, 28, 100, 68], 180, 360, fill=(255, 255, 255, 255))
    draw.polygon([(28, 48), (100, 48), (64, 108)], fill=(255, 255, 255, 255))
    # Inner pulse / veins
    draw.line([(64, 38), (64, 80)], fill=(200, 200, 200, 255), width=4)
    draw.line([(48, 56), (80, 56)], fill=(200, 200, 200, 255), width=3)
    draw.line([(52, 70), (76, 70)], fill=(200, 200, 200, 255), width=2)
    return img

def draw_blood_drop():
    img = create_base()
    draw = ImageDraw.Draw(img)
    # Sacred tear / blood drop
    draw.polygon([(64, 18), (92, 68), (64, 110), (36, 68)], fill=(255, 255, 255, 255))
    draw.ellipse([36, 52, 92, 108], fill=(255, 255, 255, 255))
    # Inner blood core
    draw.ellipse([46, 64, 82, 100], fill=(200, 200, 200, 255))
    draw.ellipse([60, 68, 74, 82], fill=(255, 255, 255, 255))
    # Small satellite droplets
    draw.ellipse([26, 88, 34, 96], fill=(255, 255, 255, 220))
    draw.ellipse([94, 88, 102, 96], fill=(255, 255, 255, 220))
    return img

def draw_wings():
    img = create_base()
    draw = ImageDraw.Draw(img)
    # Swift winged feathers
    # Left wing
    pts_l = [(64, 76), (42, 64), (16, 34), (28, 56), (18, 72), (36, 82), (28, 92), (54, 96), (64, 92)]
    draw.polygon(pts_l, fill=(255, 255, 255, 255))
    # Right wing
    pts_r = [(64, 76), (86, 64), (112, 34), (100, 56), (110, 72), (92, 82), (100, 92), (74, 96), (64, 92)]
    draw.polygon(pts_r, fill=(255, 255, 255, 255))
    # Central jewel
    draw.ellipse([58, 68, 70, 84], fill=(200, 200, 200, 255))
    return img

def draw_flame():
    img = create_base()
    draw = ImageDraw.Draw(img)
    # Roaring fire flame
    pts = [
        (64, 16), (78, 38), (88, 34), (82, 58), (102, 68), (96, 92),
        (82, 108), (64, 114), (46, 108), (32, 92), (26, 68), (46, 58),
        (40, 34), (50, 38)
    ]
    draw.polygon(pts, fill=(255, 255, 255, 255))
    # Inner flame
    inner = [
        (64, 44), (74, 62), (80, 80), (64, 98), (48, 80), (54, 62)
    ]
    draw.polygon(inner, fill=(180, 180, 180, 255))
    draw.ellipse([58, 72, 70, 90], fill=(255, 255, 255, 255))
    return img

def draw_lightning():
    img = create_base()
    draw = ImageDraw.Draw(img)
    # Sharp jagged double lightning
    pts = [
        (76, 14), (42, 58), (62, 58), (36, 114), (88, 52), (66, 52), (92, 14)
    ]
    draw.polygon(pts, fill=(255, 255, 255, 255))
    # Secondary spark
    draw.polygon([(26, 42), (18, 56), (28, 56), (16, 76), (36, 52), (28, 52)], fill=(240, 240, 240, 220))
    return img

def draw_eye():
    img = create_base()
    draw = ImageDraw.Draw(img)
    # Predator eye / focus
    # Outer almond shape
    draw.arc([16, 32, 112, 96], 0, 180, fill=(255, 255, 255, 255), width=6)
    draw.arc([16, 32, 112, 96], 180, 360, fill=(255, 255, 255, 255), width=6)
    draw.pieslice([16, 32, 112, 96], 0, 360, fill=(255, 255, 255, 180))
    # Iris
    draw.ellipse([46, 46, 82, 82], fill=(60, 60, 60, 255))
    # Slit pupil
    draw.polygon([(64, 48), (68, 64), (64, 80), (60, 64)], fill=(255, 255, 255, 255))
    # Crosshairs / focus reticle
    draw.line([(64, 20), (64, 42)], fill=(255, 255, 255, 255), width=3)
    draw.line([(64, 86), (64, 108)], fill=(255, 255, 255, 255), width=3)
    draw.line([(10, 64), (38, 64)], fill=(255, 255, 255, 255), width=3)
    draw.line([(90, 64), (118, 64)], fill=(255, 255, 255, 255), width=3)
    return img

def draw_skull():
    img = create_base()
    draw = ImageDraw.Draw(img)
    # Grim reaper skull
    draw.ellipse([34, 24, 94, 84], fill=(255, 255, 255, 255)) # Cranium
    draw.rectangle([46, 76, 82, 104], fill=(255, 255, 255, 255)) # Jaw
    # Eye sockets
    draw.ellipse([44, 48, 58, 68], fill=(40, 40, 40, 255))
    draw.ellipse([70, 48, 84, 68], fill=(40, 40, 40, 255))
    # Nose cavity
    draw.polygon([(64, 66), (68, 76), (60, 76)], fill=(40, 40, 40, 255))
    # Teeth
    for x in (52, 60, 68, 76):
        draw.line([(x, 90), (x, 102)], fill=(60, 60, 60, 255), width=2)
    return img

def draw_meteor():
    img = create_base()
    draw = ImageDraw.Draw(img)
    # Celestial Cascade meteor crashing down
    # Flaming tail
    draw.polygon([(96, 20), (108, 28), (56, 80), (46, 70)], fill=(255, 255, 255, 200))
    draw.polygon([(112, 16), (116, 22), (68, 72), (62, 66)], fill=(255, 255, 255, 160))
    draw.polygon([(82, 34), (86, 40), (48, 80), (44, 74)], fill=(255, 255, 255, 180))
    # Core star / meteor
    draw.ellipse([26, 68, 66, 108], fill=(255, 255, 255, 255))
    # Starburst
    draw.polygon([(46, 54), (54, 88), (46, 122), (38, 88)], fill=(255, 255, 255, 255))
    draw.polygon([(12, 88), (46, 80), (80, 88), (46, 96)], fill=(255, 255, 255, 255))
    return img

def draw_crown():
    img = create_base()
    draw = ImageDraw.Draw(img)
    # Undying sovereign thorny crown
    pts = [
        (22, 50), (38, 86), (90, 86), (106, 50),
        (88, 64), (64, 30), (40, 64)
    ]
    draw.polygon(pts, fill=(255, 255, 255, 255))
    # Base circlet band
    draw.rectangle([34, 84, 94, 94], fill=(220, 220, 220, 255))
    # Jewels on points
    draw.ellipse([18, 44, 26, 52], fill=(255, 255, 255, 255))
    draw.ellipse([60, 24, 68, 32], fill=(255, 255, 255, 255))
    draw.ellipse([102, 44, 110, 52], fill=(255, 255, 255, 255))
    # Center emblem
    draw.polygon([(64, 52), (72, 66), (64, 80), (56, 66)], fill=(60, 60, 60, 255))
    return img

def draw_demon():
    img = create_base()
    draw = ImageDraw.Draw(img)
    # Demonic pact horns & sigil
    # Horn Left
    draw.polygon([(44, 76), (28, 44), (20, 20), (38, 28), (52, 58)], fill=(255, 255, 255, 255))
    # Horn Right
    draw.polygon([(84, 76), (100, 44), (108, 20), (90, 28), (76, 58)], fill=(255, 255, 255, 255))
    # Center pentagram / inverted triangle
    draw.polygon([(64, 108), (34, 58), (94, 58)], fill=(255, 255, 255, 240))
    draw.polygon([(64, 98), (42, 64), (86, 64)], fill=(60, 60, 60, 255))
    draw.polygon([(64, 60), (74, 80), (54, 80)], fill=(255, 255, 255, 255))
    return img

def draw_cyclone():
    img = create_base()
    draw = ImageDraw.Draw(img)
    # Storm dancer whirlwind with blades
    for r in range(16, 54, 12):
        draw.arc([64-r, 64-r, 64+r, 64+r], 45, 225, fill=(255, 255, 255, 255), width=5)
        draw.arc([64-r, 64-r, 64+r, 64+r], 225, 45, fill=(255, 255, 255, 200), width=4)
    # 4 curved spinning blades
    for deg in (0, 90, 180, 270):
        rad = math.radians(deg)
        x = 64 + math.cos(rad) * 44
        y = 64 + math.sin(rad) * 44
        draw.polygon([(64, 64), (x, y), (x + math.sin(rad)*14, y - math.cos(rad)*14)], fill=(255, 255, 255, 255))
    return img

def draw_potion():
    img = create_base()
    draw = ImageDraw.Draw(img)
    # Rejuvenation flask
    draw.rectangle([56, 18, 72, 34], fill=(255, 255, 255, 255)) # Neck
    draw.rectangle([50, 14, 78, 20], fill=(255, 255, 255, 255)) # Rim
    draw.ellipse([32, 38, 96, 108], fill=(255, 255, 255, 255)) # Body
    draw.ellipse([40, 48, 88, 100], fill=(160, 160, 160, 255)) # Liquid
    # Bubbles
    draw.ellipse([50, 76, 58, 84], fill=(255, 255, 255, 255))
    draw.ellipse([68, 62, 74, 68], fill=(255, 255, 255, 255))
    draw.ellipse([64, 82, 72, 90], fill=(255, 255, 255, 255))
    return img

def draw_scythe():
    img = create_base()
    draw = ImageDraw.Draw(img)
    # Soul harvest scythe
    # Handle (diagonal)
    draw.line([(24, 112), (92, 28)], fill=(255, 255, 255, 255), width=5)
    # Curved blade at top
    draw.polygon([(92, 28), (116, 26), (104, 46), (72, 60), (84, 46)], fill=(255, 255, 255, 255))
    # Soul wisps
    draw.ellipse([34, 48, 48, 62], fill=(255, 255, 255, 220))
    draw.ellipse([46, 32, 56, 42], fill=(255, 255, 255, 200))
    draw.ellipse([26, 70, 36, 80], fill=(255, 255, 255, 220))
    return img

def draw_energy_crystal():
    img = create_base()
    draw = ImageDraw.Draw(img)
    # Surging pulse crystal / mana gem
    pts = [(64, 16), (92, 54), (64, 112), (36, 54)]
    draw.polygon(pts, fill=(255, 255, 255, 255))
    # Crystal facets
    draw.line([(64, 16), (64, 112)], fill=(180, 180, 180, 255), width=3)
    draw.line([(36, 54), (92, 54)], fill=(180, 180, 180, 255), width=3)
    draw.polygon([(64, 16), (74, 54), (64, 94), (54, 54)], fill=(220, 220, 220, 255))
    # Pulsing rings
    draw.arc([20, 30, 108, 98], 150, 330, fill=(255, 255, 255, 180), width=3)
    draw.arc([20, 30, 108, 98], 330, 150, fill=(255, 255, 255, 180), width=3)
    return img

def draw_godforged():
    img = create_base()
    draw = ImageDraw.Draw(img)
    # Godforged Arsenal: Divine golden halo + crossed sword & scepter
    # Halo
    draw.arc([22, 22, 106, 106], 0, 360, fill=(255, 255, 255, 255), width=4)
    # Crossed sword 1
    draw.polygon([(30, 98), (98, 30), (102, 34), (34, 102)], fill=(255, 255, 255, 255))
    # Crossed staff 2
    draw.polygon([(98, 98), (30, 30), (34, 26), (102, 94)], fill=(255, 255, 255, 255))
    # Center radiant sun
    draw.ellipse([50, 50, 78, 78], fill=(255, 255, 255, 255))
    draw.ellipse([56, 56, 72, 72], fill=(60, 60, 60, 255))
    return img

def draw_hourglass():
    img = create_base()
    draw = ImageDraw.Draw(img)
    # CDR Hourglass of chronos
    draw.rectangle([34, 18, 94, 26], fill=(255, 255, 255, 255)) # Top bar
    draw.rectangle([34, 102, 94, 110], fill=(255, 255, 255, 255)) # Bottom bar
    # Upper bulb
    draw.polygon([(40, 26), (88, 26), (64, 62)], fill=(255, 255, 255, 255))
    # Lower bulb
    draw.polygon([(64, 66), (88, 102), (40, 102)], fill=(255, 255, 255, 255))
    # Flowing sand
    draw.line([(64, 58), (64, 88)], fill=(120, 120, 120, 255), width=3)
    return img

icons = {
    "sword_slash.png": draw_sword_slash,
    "shield_aegis.png": draw_shield,
    "divine_shield.png": draw_divine_shield,
    "heart_vitality.png": draw_heart,
    "blood_drop.png": draw_blood_drop,
    "wings_fleet.png": draw_wings,
    "flame_fire.png": draw_flame,
    "lightning_bolt.png": draw_lightning,
    "eye_focus.png": draw_eye,
    "skull_death.png": draw_skull,
    "meteor_cascade.png": draw_meteor,
    "crown_sovereign.png": draw_crown,
    "demon_pact.png": draw_demon,
    "cyclone_storm.png": draw_cyclone,
    "potion_regen.png": draw_potion,
    "scythe_harvest.png": draw_scythe,
    "energy_pulse.png": draw_energy_crystal,
    "godforged_arsenal.png": draw_godforged,
    "hourglass_cdr.png": draw_hourglass
}

for name, func in icons.items():
    img = func()
    path = os.path.join(OUTPUT_DIR, name)
    img.save(path, "PNG")
    print(f"Generated {name}")

print(f"All {len(icons)} distinct augment icons generated successfully!")
