import os
import shutil
from PIL import Image, ImageEnhance

raw_path = "C:/Users/TRUONG HUNG/.gemini/antigravity-ide/brain/953cb4d4-3543-43c3-8946-55fea43b90d1/.user_uploaded/media_1791303381214.jpg"
p4_dir = "Assets/ART/Player4"
p4_ui_dir = "Assets/ART/Player4/UI"

os.makedirs(p4_dir, exist_ok=True)
os.makedirs(p4_ui_dir, exist_ok=True)

raw_img = Image.open(raw_path).convert("RGBA")
raw_pix = raw_img.load()
w, h = raw_img.size

def extract_sprite(box, target_size=(64, 64), offset=(0, 0)):
    """
    Crops box (x0, y0, x1, y1) from raw_img, removes black background,
    and places cleanly centered in target_size transparent canvas.
    """
    crop = raw_img.crop(box)
    cw, ch = crop.size
    cpix = crop.load()
    
    # 1. Clean background removal
    clean = Image.new("RGBA", (cw, ch), (0, 0, 0, 0))
    cl_pix = clean.load()
    
    for y in range(ch):
        for x in range(cw):
            r, g, b, a = cpix[x, y]
            brightness = max(r, g, b)
            # Remove dark background and gray text/lines
            if brightness < 24:
                cl_pix[x, y] = (0, 0, 0, 0)
            elif brightness < 42:
                alpha = int(((brightness - 24) / 18.0) * 255)
                cl_pix[x, y] = (r, g, b, alpha)
            else:
                cl_pix[x, y] = (r, g, b, 255)
                
    # 2. Trim excess empty transparent borders
    bbox = clean.getbbox()
    if bbox:
        trimmed = clean.crop(bbox)
    else:
        trimmed = clean
        
    tw, th = trimmed.size
    
    # 3. Fit into 64x64 top-down canvas
    # Keep scale natural (if sprite is larger than 60px height, downscale slightly)
    max_h = 58
    if th > max_h or tw > 58:
        scale = min(max_h / float(th), 58 / float(tw))
        nw = max(1, int(tw * scale))
        nh = max(1, int(th * scale))
        trimmed = trimmed.resize((nw, nh), Image.Resampling.BILINEAR)
        tw, th = nw, nh
        
    cell = Image.new("RGBA", target_size, (0, 0, 0, 0))
    paste_x = (target_size[0] - tw) // 2 + offset[0]
    paste_y = (target_size[1] - th) // 2 + offset[1]
    cell.paste(trimmed, (paste_x, paste_y), trimmed)
    
    # 4. Pixel-art sharpening & contrast enhancement
    enh = ImageEnhance.Contrast(cell)
    cell = enh.enhance(1.25)
    
    return cell

def build_idle_sheet():
    # IDLE: 4 rows (Down, Left, Up-Left, Up) x 12 cols
    sheet = Image.new("RGBA", (768, 256), (0, 0, 0, 0))
    
    # Coordinates of IDLE frames in raw sheet:
    # Down: (15, 48, 65, 110), (75, 48, 130, 110)
    # Left: (755, 48, 810, 110), (810, 48, 865, 110)
    # Up-Left: (635, 48, 690, 110), (690, 48, 745, 110)
    # Up: (515, 48, 570, 110), (570, 48, 625, 110)
    
    dir_boxes = {
        0: [(15, 48, 65, 110), (75, 48, 130, 110)],       # Down
        1: [(755, 48, 810, 110), (810, 48, 865, 110)],    # Left
        2: [(635, 48, 690, 110), (690, 48, 745, 110)],    # Up-Left
        3: [(515, 48, 570, 110), (570, 48, 625, 110)]     # Up
    }
    
    for r in range(4):
        boxes = dir_boxes[r]
        f0 = extract_sprite(boxes[0])
        f1 = extract_sprite(boxes[1])
        # Loop 12 frames (breathing loop: 0 0 1 1 0 0 1 1 0 0 1 1)
        frames = [f0, f0, f1, f1, f0, f0, f1, f1, f0, f0, f1, f1]
        for c in range(12):
            sheet.paste(frames[c], (c * 64, r * 64), frames[c])
            
    sheet.save(os.path.join(p4_dir, "Hammer_Idle.png"))
    print("Saved Hammer_Idle.png from raw berserker sheet!")

def build_walk_sheet():
    # WALK: 4 rows x 6 cols (384x256)
    sheet = Image.new("RGBA", (384, 256), (0, 0, 0, 0))
    
    # Down column x: 15..110, y: 150..340
    # Left column x: 760..855, y: 150..340
    # Up-Left column x: 645..740, y: 150..340
    # Up column x: 520..610, y: 150..340
    walk_cols = {
        0: (15, 110),
        1: (760, 855),
        2: (645, 740),
        3: (520, 610)
    }
    
    y_steps = [150, 200, 250, 300, 350]
    
    for r in range(4):
        x0, x1 = walk_cols[r]
        extracted = []
        for i in range(4):
            b = (x0, y_steps[i], x1, y_steps[i+1])
            extracted.append(extract_sprite(b))
            
        # 6 frames cycle: 0, 1, 2, 3, 2, 1
        cycle = [extracted[0], extracted[1], extracted[2], extracted[3], extracted[2], extracted[1]]
        for c in range(6):
            sheet.paste(cycle[c], (c * 64, r * 64), cycle[c])
            
    sheet.save(os.path.join(p4_dir, "Hammer_Walk.png"))
    print("Saved Hammer_Walk.png from raw berserker sheet!")

def build_run_sheet():
    # RUN: 4 rows x 8 cols (512x256)
    sheet = Image.new("RGBA", (512, 256), (0, 0, 0, 0))
    
    run_cols = {
        0: (15, 110),
        1: (760, 855),
        2: (645, 740),
        3: (520, 610)
    }
    y_steps = [380, 430, 480, 530, 580]
    
    for r in range(4):
        x0, x1 = run_cols[r]
        extracted = []
        for i in range(4):
            b = (x0, y_steps[i], x1, y_steps[i+1])
            extracted.append(extract_sprite(b))
            
        # 8 frames cycle: 0, 1, 2, 3, 0, 1, 2, 3
        cycle = extracted * 2
        for c in range(8):
            sheet.paste(cycle[c], (c * 64, r * 64), cycle[c])
            
    sheet.save(os.path.join(p4_dir, "Hammer_Run.png"))
    print("Saved Hammer_Run.png from raw berserker sheet!")

def build_attack_and_skill_sheets():
    # ATTACK: 4 rows x 8 cols (512x256)
    sheet_atk = Image.new("RGBA", (512, 256), (0, 0, 0, 0))
    
    atk_cols = {
        0: (15, 90),    # Down
        1: (200, 275),  # Right/Side
        2: (450, 525),  # Up-Left/Up-Right
        3: (370, 445)   # Up
    }
    y_steps = [610, 665, 720, 775, 830]
    
    for r in range(4):
        x0, x1 = atk_cols[r]
        extracted = []
        for i in range(4):
            b = (x0, y_steps[i], x1, y_steps[i+1])
            extracted.append(extract_sprite(b))
            
        # 8 frames: Windup, Windup, Swing, Slam, Impact, Impact, Recovery, Recovery
        cycle = [extracted[0], extracted[0], extracted[1], extracted[1], extracted[2], extracted[3], extracted[3], extracted[0]]
        for c in range(8):
            sheet_atk.paste(cycle[c], (c * 64, r * 64), cycle[c])
            
    sheet_atk.save(os.path.join(p4_dir, "Hammer_Attack.png"))
    print("Saved Hammer_Attack.png from raw berserker sheet!")
    
    # SKILL (Area Slam): (630..1024, 590..840)
    sheet_skill = Image.new("RGBA", (512, 256), (0, 0, 0, 0))
    skill_cols = {
        0: (630, 720),  # Down Area Slam
        1: (720, 810),  # Left Area Slam
        2: (810, 900),  # Up Area Slam
        3: (900, 1000)  # Right Area Slam
    }
    
    for r in range(4):
        x0, x1 = skill_cols[r]
        extracted = []
        for i in range(4):
            b = (x0, y_steps[i], x1, y_steps[i+1])
            extracted.append(extract_sprite(b, offset=(0, 2)))
            
        cycle = [extracted[0], extracted[0], extracted[1], extracted[2], extracted[3], extracted[3], extracted[3], extracted[0]]
        for c in range(8):
            sheet_skill.paste(cycle[c], (c * 64, r * 64), cycle[c])
            
    sheet_skill.save(os.path.join(p4_dir, "Hammer_Skill.png"))
    print("Saved Hammer_Skill.png from raw berserker sheet!")

def build_hurt_death_extra():
    # HURT: 4 rows x 5 cols
    sheet_hurt = Image.new("RGBA", (320, 256), (0, 0, 0, 0))
    hurt_boxes = [
        (15, 870, 75, 960),   # Down
        (75, 870, 135, 960),  # Right
        (135, 870, 195, 960), # Up
        (195, 870, 255, 960)  # Left
    ]
    for r in range(4):
        f = extract_sprite(hurt_boxes[r])
        for c in range(5):
            sheet_hurt.paste(f, (c * 64, r * 64), f)
    sheet_hurt.save(os.path.join(p4_dir, "Hammer_Hurt.png"))
    
    # DEATH: 4 rows x 7 cols
    sheet_death = Image.new("RGBA", (448, 256), (0, 0, 0, 0))
    death_boxes = [
        (265, 890, 335, 960), # Down
        (335, 890, 400, 960), # Right
        (400, 890, 460, 960), # Up
        (460, 890, 530, 960)  # Left
    ]
    for r in range(4):
        f = extract_sprite(death_boxes[r])
        for c in range(7):
            sheet_death.paste(f, (c * 64, r * 64), f)
    sheet_death.save(os.path.join(p4_dir, "Hammer_Death.png"))
    
    # EXTRA: Stand / Preview Portrait (128x128)
    extra_crop = extract_sprite((775, 860, 955, 990), target_size=(128, 128))
    extra_crop.save(os.path.join(p4_ui_dir, "Hero_Player4_Portrait.png"))
    print("Saved Hero_Player4_Portrait.png!")

def build_topdown_preview():
    # Build high resolution preview
    idle = Image.open(os.path.join(p4_dir, "Hammer_Idle.png"))
    atk = Image.open(os.path.join(p4_dir, "Hammer_Attack.png"))
    skill = Image.open(os.path.join(p4_dir, "Hammer_Skill.png"))
    
    canvas = Image.new("RGBA", (64 * 4, 64 * 3), (24, 26, 32, 255))
    
    # Row 0: Idle 4 directions (Down, Left, Up-Left, Up)
    for i, r in enumerate([0, 1, 2, 3]):
        f = idle.crop((0, r * 64, 64, (r + 1) * 64))
        canvas.paste(f, (i * 64, 0), f)
        
    # Row 1: Attack Down & Left Frames
    for i, c in enumerate([0, 2, 4, 6]):
        f = atk.crop((c * 64, 64, (c + 1) * 64, 128))
        canvas.paste(f, (i * 64, 64), f)
        
    # Row 2: Skill Area Slam Down Frames
    for i, c in enumerate([0, 2, 4, 6]):
        f = skill.crop((c * 64, 0, (c + 1) * 64, 64))
        canvas.paste(f, (i * 64, 128), f)
        
    canvas_3x = canvas.resize((canvas.width * 3, canvas.height * 3), Image.Resampling.NEAREST)
    preview_path = "Assets/ART/Player4/berserker_topdown_preview.png"
    canvas_3x.save(preview_path)
    
    # Copy to artifact folder
    artifact_dir = "C:/Users/TRUONG HUNG/.gemini/antigravity-ide/brain/953cb4d4-3543-43c3-8946-55fea43b90d1"
    shutil.copy(preview_path, os.path.join(artifact_dir, "berserker_topdown_preview.png"))
    print("Saved berserker_topdown_preview.png!")

build_idle_sheet()
build_walk_sheet()
build_run_sheet()
build_attack_and_skill_sheets()
build_hurt_death_extra()
build_topdown_preview()
print("All Berserker Top-Down Pixel Art Sprite Sheets processed and saved successfully!")
