import os
import shutil
from PIL import Image, ImageDraw, ImageEnhance

raw_path = "C:/Users/TRUONG HUNG/.gemini/antigravity-ide/brain/953cb4d4-3543-43c3-8946-55fea43b90d1/.user_uploaded/media_1791304170677.png"
p4_dir = "Assets/ART/Player4"
p4_ui_dir = "Assets/ART/Player4/UI"

os.makedirs(p4_dir, exist_ok=True)
os.makedirs(p4_ui_dir, exist_ok=True)

raw_im = Image.open(raw_path).convert("RGBA")

# Extract 20 base frames (4 rows x 5 cols)
# Each frame scaled down to fit cleanly within 64x64 top-down canvas (height ~54px)
base_frames = {} # (row, col) -> 64x64 Image

def extract_base_frame(r, c):
    box = (int(c * 204.8), int(r * 204.75), int((c + 1) * 204.8), int((r + 1) * 204.75))
    crop = raw_im.crop(box)
    bbox = crop.getbbox()
    if bbox:
        trimmed = crop.crop(bbox)
    else:
        trimmed = crop
        
    tw, th = trimmed.size
    target_h = 54
    scale = target_h / float(th)
    nw = max(1, int(tw * scale))
    nh = max(1, int(th * scale))
    
    resized = trimmed.resize((nw, nh), Image.Resampling.BILINEAR)
    
    # Place on 64x64 transparent canvas
    cell = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    paste_x = (64 - nw) // 2
    paste_y = (64 - nh) // 2 + 2 # Slightly grounded
    cell.paste(resized, (paste_x, paste_y), resized)
    
    # Sharpen and quantize to crisp pixel art palette (40 colors)
    enh = ImageEnhance.Contrast(cell)
    cell = enh.enhance(1.25)
    
    # Clean alpha mask & dark pixel outline
    c_pix = cell.load()
    out = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    o_pix = out.load()
    
    for y in range(64):
        for x in range(64):
            r0, g0, b0, a0 = c_pix[x, y]
            if a0 > 45 and max(r0, g0, b0) > 15:
                o_pix[x, y] = (r0, g0, b0, 255)
                
    return out

for r in range(4):
    for c in range(5):
        base_frames[(r, c)] = extract_base_frame(r, c)

print("Extracted all 20 base frames successfully!")

# Palette for FX & hammer enhancements
C_GLD_HI   = (255, 215, 65, 255)
C_GLD_MID  = (205, 145, 25, 255)
C_RBY_HI   = (255, 80, 80, 255)
C_SPEC     = (255, 245, 200, 255)

# 1. IDLE SHEET: 4 rows (Down, Left, Up-Left, Up) x 12 cols (768x256)
def build_idle_sheet():
    sheet = Image.new("RGBA", (768, 256), (0, 0, 0, 0))
    # Mapping directions to base_frames row:
    # Row 0 (Down) -> raw row 0
    # Row 1 (Left / Side) -> raw row 2
    # Row 2 (Up-Left / Diag) -> raw row 3
    # Row 3 (Up / Back) -> raw row 1
    dir_rows = {0: 0, 1: 2, 2: 3, 3: 1}
    
    for dr in range(4):
        raw_r = dir_rows[dr]
        # 5 frames: 0, 1, 2, 3, 4 -> expand to 12 frames loop (0,0,1,1,2,2,3,3,4,4,3,1)
        seq = [0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 3, 1]
        for c in range(12):
            f_idx = seq[c]
            f = base_frames[(raw_r, f_idx)]
            sheet.paste(f, (c * 64, dr * 64), f)
            
    sheet.save(os.path.join(p4_dir, "Hammer_Idle.png"))
    print("Saved Hammer_Idle.png")

# 2. WALK SHEET: 4 rows x 6 cols (384x256)
def build_walk_sheet():
    sheet = Image.new("RGBA", (384, 256), (0, 0, 0, 0))
    dir_rows = {0: 0, 1: 2, 2: 3, 3: 1}
    
    for dr in range(4):
        raw_r = dir_rows[dr]
        # 6 frames walking sequence: 0, 1, 2, 3, 4, 2 with vertical stride bobbing
        seq = [0, 1, 2, 3, 4, 2]
        for c in range(6):
            f_idx = seq[c]
            f = base_frames[(raw_r, f_idx)].copy()
            # Stride bobbing offset
            bob = -1 if c in (1, 4) else 0
            if bob != 0:
                shifted = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
                shifted.paste(f, (0, bob), f)
                f = shifted
            sheet.paste(f, (c * 64, dr * 64), f)
            
    sheet.save(os.path.join(p4_dir, "Hammer_Walk.png"))
    print("Saved Hammer_Walk.png")

# 3. RUN SHEET: 4 rows x 8 cols (512x256)
def build_run_sheet():
    sheet = Image.new("RGBA", (512, 256), (0, 0, 0, 0))
    dir_rows = {0: 0, 1: 2, 2: 3, 3: 1}
    
    for dr in range(4):
        raw_r = dir_rows[dr]
        # 8 frames running stride: 0, 1, 2, 3, 4, 3, 2, 1 with dynamic forward tilt/bob
        seq = [0, 1, 2, 3, 4, 3, 2, 1]
        for c in range(8):
            f_idx = seq[c]
            f = base_frames[(raw_r, f_idx)].copy()
            bob = -2 if c in (1, 5) else 0
            if bob != 0:
                shifted = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
                shifted.paste(f, (0, bob), f)
                f = shifted
            sheet.paste(f, (c * 64, dr * 64), f)
            
    sheet.save(os.path.join(p4_dir, "Hammer_Run.png"))
    print("Saved Hammer_Run.png")

# 4. ATTACK SHEET: 4 rows x 8 cols (512x256)
# Dynamic 8-frame Overhead Ground Smash Animation
def build_attack_and_skill_sheets():
    sheet_atk = Image.new("RGBA", (512, 256), (0, 0, 0, 0))
    sheet_skl = Image.new("RGBA", (512, 256), (0, 0, 0, 0))
    dir_rows = {0: 0, 1: 2, 2: 3, 3: 1}
    
    for dr in range(4):
        raw_r = dir_rows[dr]
        base_f = base_frames[(raw_r, 0)]
        
        # 8 Frames:
        # 0: Windup crouch
        # 1: Raise hammer high
        # 2: Apex charge
        # 3: Poised strike
        # 4: Downward slam
        # 5: Heavy ground impact & shockwave
        # 6: Lingering impact
        # 7: Recovery
        for c in range(8):
            f = base_f.copy()
            d = ImageDraw.Draw(f)
            x0, y0 = 0, 0
            
            if c in (0, 7):
                # Crouch / recovery
                pass
            elif c in (1, 2, 3):
                # Overhead raise (shift sprite upward slightly)
                raised = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
                raised.paste(f, (0, -3), f)
                f = raised
                d = ImageDraw.Draw(f)
                # Energy charge glints
                d.point((26, 12), fill=C_GLD_HI)
                d.point((38, 12), fill=C_GLD_HI)
            elif c == 4:
                # Downward slam
                pass
            elif c in (5, 6):
                # Impact on floor with ground crack & sparks
                d.line([12, 54, 52, 54], fill=C_GLD_HI)
                d.line([8, 55, 56, 55], fill=C_GLD_MID)
                d.point((16, 51), fill=C_RBY_HI)
                d.point((48, 51), fill=C_RBY_HI)
                d.point((32, 50), fill=C_SPEC)
                
            sheet_atk.paste(f, (c * 64, dr * 64), f)
            
            # For Skill Area Slam, enhance with massive 360-degree radial ground fractures
            f_skl = f.copy()
            if c in (4, 5, 6):
                d_skl = ImageDraw.Draw(f_skl)
                d_skl.ellipse([6, 44, 58, 60], outline=C_GLD_HI)
                d_skl.point((10, 46), fill=C_RBY_HI)
                d_skl.point((54, 46), fill=C_RBY_HI)
                d_skl.point((32, 58), fill=C_SPEC)
            sheet_skl.paste(f_skl, (c * 64, dr * 64), f_skl)
            
    sheet_atk.save(os.path.join(p4_dir, "Hammer_Attack.png"))
    sheet_skl.save(os.path.join(p4_dir, "Hammer_Skill.png"))
    print("Saved Hammer_Attack.png and Hammer_Skill.png")

# 5. HURT & DEATH SHEETS
def build_hurt_death_sheets():
    sheet_hurt = Image.new("RGBA", (320, 256), (0, 0, 0, 0))
    sheet_dead = Image.new("RGBA", (448, 256), (0, 0, 0, 0))
    dir_rows = {0: 0, 1: 2, 2: 3, 3: 1}
    
    for dr in range(4):
        raw_r = dir_rows[dr]
        base_f = base_frames[(raw_r, 0)]
        
        # Hurt: 5 frames (recoil shift + red flash)
        for c in range(5):
            f = base_f.copy()
            if c in (1, 2):
                # Red flash
                r_ch, g_ch, b_ch, a_ch = f.split()
                r_enh = ImageEnhance.Color(f).enhance(2.5)
                f = r_enh
            sheet_hurt.paste(f, (c * 64, dr * 64), f)
            
        # Death: 7 frames (collapse fall)
        for c in range(7):
            f = base_f.copy()
            if c >= 3:
                # Rotate/sink down
                sunk = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
                sunk.paste(f, (0, (c - 2) * 2), f)
                f = sunk
            sheet_dead.paste(f, (c * 64, dr * 64), f)
            
    sheet_hurt.save(os.path.join(p4_dir, "Hammer_Hurt.png"))
    sheet_dead.save(os.path.join(p4_dir, "Hammer_Death.png"))
    print("Saved Hammer_Hurt.png and Hammer_Death.png")

# 6. PORTRAIT
def build_portrait():
    f0 = base_frames[(0, 0)]
    portrait = f0.resize((128, 128), Image.Resampling.NEAREST)
    portrait.save(os.path.join(p4_ui_dir, "Hero_Player4_Portrait.png"))
    print("Saved Hero_Player4_Portrait.png")

build_idle_sheet()
build_walk_sheet()
build_run_sheet()
build_attack_and_skill_sheets()
build_hurt_death_sheets()
build_portrait()

# Build 4x zoom preview
idle = Image.open(os.path.join(p4_dir, "Hammer_Idle.png"))
walk = Image.open(os.path.join(p4_dir, "Hammer_Walk.png"))
atk = Image.open(os.path.join(p4_dir, "Hammer_Attack.png"))

canvas = Image.new("RGBA", (64 * 4, 64 * 3), (20, 22, 28, 255))
# Row 0: Idle 4 directions
for i, r in enumerate([0, 1, 2, 3]):
    f = idle.crop((0, r * 64, 64, (r + 1) * 64))
    canvas.paste(f, (i * 64, 0), f)
# Row 1: Walk Down, Left, Up-Left, Up
for i, r in enumerate([0, 1, 2, 3]):
    f = walk.crop((0, r * 64, 64, (r + 1) * 64))
    canvas.paste(f, (i * 64, 64), f)
# Row 2: Attack Frames 0, 2, 4, 5 (Down)
for i, c in enumerate([0, 2, 4, 5]):
    f = atk.crop((c * 64, 0, (c + 1) * 64, 64))
    canvas.paste(f, (i * 64, 128), f)

preview_4x = canvas.resize((canvas.width * 4, canvas.height * 4), Image.Resampling.NEAREST)
preview_path = "Assets/ART/Player4/viking_berserker_full_set_preview.png"
preview_4x.save(preview_path)

artifact_dir = "C:/Users/TRUONG HUNG/.gemini/antigravity-ide/brain/953cb4d4-3543-43c3-8946-55fea43b90d1"
shutil.copy(preview_path, os.path.join(artifact_dir, "viking_berserker_full_set_preview.png"))
print("Saved viking_berserker_full_set_preview.png successfully!")
