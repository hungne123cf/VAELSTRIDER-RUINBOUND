import os
from PIL import Image

p4_dir = "Assets/ART/Player4"
p4_ui_dir = "Assets/ART/Player4/UI"
os.makedirs(p4_ui_dir, exist_ok=True)

def clean_frame(im):
    im = im.copy()
    w, h = im.size
    pix = im.load()
    # Clean bottom platform stand bar (20, 12, 28)
    for y in range(h - 8, h):
        for x in range(w):
            r, g, b, a = pix[x, y]
            if a > 0 and (r, g, b) == (20, 12, 28):
                pix[x, y] = (0, 0, 0, 0)
    return im

def recolor_blue_to_crimson(im):
    im = im.convert('RGBA')
    pix = im.load()
    w, h = im.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = pix[x, y]
            if a > 0:
                # Blue armor plates -> Crimson Titan plates
                if b > 150 and r < 60 and g < 150: # Bright blue
                    pix[x, y] = (255, 51, 51, a)
                elif b > 100 and r < 50 and g < 100: # Medium blue
                    pix[x, y] = (179, 18, 38, a)
                elif b > 60 and r < 40 and g < 60: # Dark blue
                    pix[x, y] = (68, 36, 52, a)
    return im

def get_crop(path, recolor=False):
    im = Image.open(path)
    cleaned = clean_frame(im)
    if recolor:
        cleaned = recolor_blue_to_crimson(cleaned)
    return cleaned.crop((24, 32, 152, 160)) # 128x128 standard cell

def build_sheet(crops, out_name):
    cell_size = 128
    cols = 4
    rows = 4
    sheet = Image.new('RGBA', (cols * cell_size, rows * cell_size), (0,0,0,0))
    for r in range(rows):
        for c in range(cols):
            sheet.paste(crops[c], (c * cell_size, r * cell_size))
    sheet.save(os.path.join(p4_dir, out_name))
    print(f"Generated {out_name} - {sheet.size}")

# 1. Idle (Axe Defence4)
idle_crops = [get_crop(f"Assets/ART/Player4/Player4/Axe/Defence4/Player_Idle_Axe_Defence4_{i}.png") for i in range(4)]
build_sheet(idle_crops, "Hammer_Idle.png")

# 2. Walk
walk_crops = [get_crop(f"Assets/ART/Player4/Player4/Axe/Defence4/Player_Walk_Axe_Defence4_{i}.png") for i in range(4)]
build_sheet(walk_crops, "Hammer_Walk.png")

# 3. Run
build_sheet(walk_crops, "Hammer_Run.png")

# 4. Attack
atk_crops = [get_crop(f"Assets/ART/Player4/Player4/Axe/Defence4/Player_Attack_Axe_Defence4_{i}.png") for i in range(4)]
build_sheet(atk_crops, "Hammer_Attack.png")

# 5. Skill (Special Attack recolored to crimson armor)
skill_crops = [get_crop(f"Assets/ART/Player4/Player4/Special/Player_Attack_Special_{i}.png", recolor=True) for i in range(4)]
build_sheet(skill_crops, "Hammer_Skill.png")

# 6. Death (Recolored to crimson armor)
death_crops = [get_crop(f"Assets/ART/Player4/Player4/Axe/Player_Death_Axe_{i}.png", recolor=True) for i in range(4)]
death_sheet = Image.new('RGBA', (4 * 128, 128), (0,0,0,0))
for i, c in enumerate(death_crops):
    death_sheet.paste(c, (i * 128, 0))
death_sheet.save(os.path.join(p4_dir, "Hammer_Death.png"))
print("Generated Hammer_Death.png")

# 7. Hurt
hurt_crops = [death_crops[0], idle_crops[0], death_crops[0], idle_crops[0]]
hurt_sheet = Image.new('RGBA', (4 * 128, 128), (0,0,0,0))
for i, c in enumerate(hurt_crops):
    hurt_sheet.paste(c, (i * 128, 0))
hurt_sheet.save(os.path.join(p4_dir, "Hammer_Hurt.png"))
print("Generated Hammer_Hurt.png")

# 8. UI Avatar Portrait
idle_d4_0 = idle_crops[0]
portrait = Image.new('RGBA', (128, 128), (28, 20, 36, 255))
portrait.paste(idle_d4_0.resize((160, 160), Image.NEAREST), (-16, -10), idle_d4_0.resize((160, 160), Image.NEAREST))
portrait.save(os.path.join(p4_ui_dir, "Hero_Player4_Portrait.png"))
print("Generated Hero_Player4_Portrait.png")

# 9. Overview Preview Montage
preview = Image.new('RGBA', (128 * 4, 128 * 5), (18, 14, 24, 255))
actions = [
    ("Idle", idle_crops),
    ("Walk / Run", walk_crops),
    ("Attack", atk_crops),
    ("Special Skill", skill_crops),
    ("Death", death_crops)
]

for row_idx, (act_name, crops) in enumerate(actions):
    for col_idx, c in enumerate(crops):
        preview.paste(c, (col_idx * 128, row_idx * 128), c)

preview.save(os.path.join(p4_dir, "player4_complete_pack_preview.png"))
print("Generated player4_complete_pack_preview.png successfully!")
