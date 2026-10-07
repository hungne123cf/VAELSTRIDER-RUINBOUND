import os
import shutil
from PIL import Image, ImageDraw

p3_dir = "Assets/ART/Player3"
p4_dir = "Assets/ART/Player4"
p4_ui_dir = "Assets/ART/Player4/UI"

os.makedirs(p4_dir, exist_ok=True)
os.makedirs(p4_ui_dir, exist_ok=True)

# Precise Pixel Art Palette based on the user's reference image
# Skin / Muscle
C_SKIN_SPEC = (255, 225, 185, 255)
C_SKIN_HI   = (245, 185, 140, 255)
C_SKIN_MID  = (210, 135, 95, 255)
C_SKIN_SHD  = (155, 80, 55, 255)
C_SKIN_DEEP = (95, 45, 30, 255)

# White/Grey Fur Mantle
C_FUR_SPEC  = (250, 248, 242, 255)
C_FUR_HI    = (225, 215, 200, 255)
C_FUR_MID   = (175, 160, 145, 255)
C_FUR_SHD   = (110, 98, 88, 255)
C_FUR_DEEP  = (55, 48, 42, 255)

# Crimson Loincloth / War Skirt
C_RED_HI    = (240, 55, 55, 255)
C_RED_MID   = (190, 25, 25, 255)
C_RED_SHD   = (125, 12, 12, 255)
C_RED_DEEP  = (65, 5, 5, 255)

# Belt & Gold Buckle
C_GLD_HI    = (255, 220, 75, 255)
C_GLD_MID   = (210, 155, 30, 255)
C_GLD_SHD   = (125, 80, 15, 255)

# Dark Combat Pants & Boots
C_PANTS_HI  = (75, 85, 105, 255)
C_PANTS_MID = (48, 55, 70, 255)
C_PANTS_SHD = (30, 35, 45, 255)
C_PANTS_DEEP= (18, 20, 26, 255)

# Colossal War Hammer (Stone & Iron with Runic Engraving)
C_HAM_SPEC  = (220, 235, 255, 255)
C_HAM_HI    = (150, 170, 200, 255)
C_HAM_MID   = (90, 105, 130, 255)
C_HAM_SHD   = (55, 65, 82, 255)
C_HAM_DEEP  = (28, 33, 42, 255)
C_HAM_RUNE  = (190, 220, 255, 255)

C_WOD_HI    = (115, 78, 52, 255)
C_WOD_MID   = (75, 50, 32, 255)
C_WOD_SHD   = (42, 28, 18, 255)

C_OUTLINE   = (20, 22, 28, 255)

def repaint_to_barbarian_berserker(img):
    """
    Transforms character sprite sheet into the reference Barbarian Berserker:
    - Bare muscular chest with ab & pec definition
    - White fur mantle draped over shoulders and neck
    - Crimson loincloth / war sash with gold belt buckle
    - Dark combat trousers & iron boots
    - Studded bracers
    """
    img = img.convert("RGBA")
    w, h = img.size
    pixels = img.load()
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    out_pix = out.load()
    
    cols = w // 64
    rows = h // 64
    
    for y in range(h):
        for x in range(w):
            r, g, b, a = pixels[x, y]
            if a < 15:
                continue
            
            # Determine body region
            # Blue armor from original -> convert to muscular skin on chest, fur mantle on shoulders, red loincloth at waist, dark pants below
            is_blue = (b > r + 8 and b > 50) or (g > r + 8 and b > 45)
            is_skin = (r > 135 and g > 85 and b > 55 and r > g and g > b)
            is_hair = (r < 90 and g < 70 and b < 60 and a > 100)
            is_white = (r > 165 and g > 165 and b > 165)
            is_gold = (r > 115 and g > 85 and b < 80)
            
            # Local cell Y (0..63)
            cy = y % 64
            
            if is_blue:
                lum = (r * 0.299 + g * 0.587 + b * 0.114) / 255.0
                if cy in range(16, 26):
                    # Upper Torso / Shoulders -> White Fur Mantle
                    if lum > 0.6:
                        out_pix[x, y] = C_FUR_SPEC
                    elif lum > 0.4:
                        out_pix[x, y] = C_FUR_HI
                    elif lum > 0.25:
                        out_pix[x, y] = C_FUR_MID
                    else:
                        out_pix[x, y] = C_FUR_SHD
                elif cy in range(26, 36):
                    # Mid Torso -> Bare Muscular Chest / Abs
                    if lum > 0.55:
                        out_pix[x, y] = C_SKIN_HI
                    elif lum > 0.35:
                        out_pix[x, y] = C_SKIN_MID
                    elif lum > 0.2:
                        out_pix[x, y] = C_SKIN_SHD
                    else:
                        out_pix[x, y] = C_SKIN_DEEP
                elif cy in range(36, 43):
                    # Waist -> Crimson Loincloth / Sash
                    if lum > 0.5:
                        out_pix[x, y] = C_RED_HI
                    elif lum > 0.3:
                        out_pix[x, y] = C_RED_MID
                    else:
                        out_pix[x, y] = C_RED_SHD
                else:
                    # Legs / Boots -> Dark Pants & Greaves
                    if lum > 0.45:
                        out_pix[x, y] = C_PANTS_HI
                    elif lum > 0.25:
                        out_pix[x, y] = C_PANTS_MID
                    else:
                        out_pix[x, y] = C_PANTS_SHD
            elif is_skin:
                # Tanned muscular warrior skin
                lum = (r * 0.299 + g * 0.587 + b * 0.114) / 255.0
                if lum > 0.7:
                    out_pix[x, y] = C_SKIN_SPEC
                elif lum > 0.5:
                    out_pix[x, y] = C_SKIN_HI
                elif lum > 0.3:
                    out_pix[x, y] = C_SKIN_MID
                else:
                    out_pix[x, y] = C_SKIN_SHD
            elif is_gold:
                out_pix[x, y] = C_GLD_MID
            elif is_white:
                out_pix[x, y] = C_FUR_SPEC
            else:
                out_pix[x, y] = (int(r * 0.85), int(g * 0.85), int(b * 0.9), a)

    # Enhance Fur Mantle volume on shoulders and Gold Belt Buckle
    for r in range(rows):
        for c in range(cols):
            x0 = c * 64
            y0 = r * 64
            # Add gold belt buckle at waist center (x: 31..33, y: 36)
            out_pix[x0 + 31, y0 + 36] = C_GLD_HI
            out_pix[x0 + 32, y0 + 36] = C_GLD_HI
            out_pix[x0 + 31, y0 + 37] = C_GLD_MID
            out_pix[x0 + 32, y0 + 37] = C_GLD_MID
            
            # Broaden fur mantle silhouette on shoulders (cy 18..24)
            for cy in range(y0 + 18, y0 + 25):
                xs = [cx for cx in range(x0 + 10, x0 + 54) if out_pix[cx, cy][3] > 50]
                if xs:
                    min_x = min(xs)
                    max_x = max(xs)
                    if min_x > x0 + 8:
                        out_pix[min_x - 1, cy] = C_FUR_HI
                        out_pix[min_x - 2, cy] = C_OUTLINE
                    if max_x < x0 + 56:
                        out_pix[max_x + 1, cy] = C_FUR_HI
                        out_pix[max_x + 2, cy] = C_OUTLINE

    return out

def draw_runic_warhammer(draw, head_x, head_y, shaft_x0, shaft_y0, shaft_x1, shaft_y1):
    """
    Renders the exact Colossal Stone/Iron War Hammer with beveled block head,
    Runic circular emblem in center, and ironwood shaft.
    """
    # 1. Shaft with gold cross-bindings
    dx = shaft_x1 - shaft_x0
    dy = shaft_y1 - shaft_y0
    steps = max(abs(dx), abs(dy), 1)
    for i in range(steps + 1):
        t = i / steps
        sx = int(shaft_x0 + t * dx)
        sy = int(shaft_y0 + t * dy)
        draw.point((sx, sy), fill=C_WOD_MID)
        draw.point((sx + 1, sy), fill=C_WOD_SHD)
        if i % 3 == 0:
            draw.point((sx, sy), fill=C_GLD_MID)
            
    # Pommel cap
    draw.rectangle([shaft_x1 - 1, shaft_y1 - 1, shaft_x1 + 2, shaft_y1 + 1], fill=C_OUTLINE)
    draw.rectangle([shaft_x1, shaft_y1, shaft_x1 + 1, shaft_y1], fill=C_GLD_HI)

    # 2. Heavy Beveled Block Hammer Head (13 wide, 9 tall)
    # Outline
    draw.rectangle([head_x - 6, head_y - 4, head_x + 6, head_y + 4], fill=C_OUTLINE)
    # Inner metal/stone
    draw.rectangle([head_x - 5, head_y - 3, head_x + 5, head_y + 3], fill=C_HAM_MID)
    # Top/Left Highlights
    draw.line([(head_x - 5, head_y - 3), (head_x + 5, head_y - 3)], fill=C_HAM_HI)
    draw.line([(head_x - 5, head_y - 3), (head_x - 5, head_y + 3)], fill=C_HAM_HI)
    draw.point((head_x - 5, head_y - 3), fill=C_HAM_SPEC)
    
    # Bottom/Right Shadows
    draw.line([(head_x - 5, head_y + 3), (head_x + 5, head_y + 3)], fill=C_HAM_SHD)
    draw.line([(head_x + 5, head_y - 3), (head_x + 5, head_y + 3)], fill=C_HAM_SHD)
    
    # Runic Circular Emblem in center
    draw.rectangle([head_x - 2, head_y - 2, head_x + 2, head_y + 2], fill=C_HAM_DEEP)
    draw.point((head_x, head_y - 1), fill=C_HAM_RUNE)
    draw.point((head_x - 1, head_y), fill=C_HAM_RUNE)
    draw.point((head_x + 1, head_y), fill=C_HAM_RUNE)
    draw.point((head_x, head_y + 1), fill=C_HAM_RUNE)
    draw.point((head_x, head_y), fill=C_HAM_SPEC)

def draw_armored_hands(draw, hx, hy):
    """Draws iron-studded gauntlets gripping the hammer haft."""
    draw.rectangle([hx - 2, hy - 2, hx + 2, hy + 2], fill=C_OUTLINE)
    draw.rectangle([hx - 1, hy - 1, hx + 1, hy + 1], fill=C_SKIN_MID)
    draw.line([(hx - 1, hy - 1), (hx + 1, hy - 1)], fill=C_SKIN_HI)
    draw.point((hx, hy - 1), fill=C_GLD_HI)

def build_idle():
    base = Image.open(os.path.join(p3_dir, "Unarmed_Idle.png"))
    barb = repaint_to_barbarian_berserker(base)
    draw = ImageDraw.Draw(barb)
    
    for r in range(4):
        for c in range(12):
            x0 = c * 64
            y0 = r * 64
            bob = (1 if (c % 4) in (1, 2) else 0)
            
            if r == 0: # Down (Front)
                draw_runic_warhammer(draw, x0 + 41, y0 + 20 + bob, x0 + 41, y0 + 25 + bob, x0 + 41, y0 + 45 + bob)
                draw_armored_hands(draw, x0 + 41, y0 + 34 + bob)
            elif r == 1: # Left (Side - 2-Handed Front Combat Guard)
                draw_runic_warhammer(draw, x0 + 20, y0 + 19 + bob, x0 + 21, y0 + 23 + bob, x0 + 26, y0 + 44 + bob)
                draw_armored_hands(draw, x0 + 22, y0 + 31 + bob)
                draw_armored_hands(draw, x0 + 24, y0 + 37 + bob)
            elif r == 2: # Up-Left
                draw_runic_warhammer(draw, x0 + 22, y0 + 18 + bob, x0 + 23, y0 + 22 + bob, x0 + 27, y0 + 43 + bob)
                draw_armored_hands(draw, x0 + 24, y0 + 32 + bob)
            elif r == 3: # Up
                draw_runic_warhammer(draw, x0 + 24, y0 + 19 + bob, x0 + 25, y0 + 23 + bob, x0 + 27, y0 + 44 + bob)
                
    barb.save(os.path.join(p4_dir, "Hammer_Idle.png"))
    print("Saved Hammer_Idle.png (Barbarian Berserker)")

def build_movement(src, dst, cols):
    base = Image.open(os.path.join(p3_dir, src))
    barb = repaint_to_barbarian_berserker(base)
    draw = ImageDraw.Draw(barb)
    
    for r in range(4):
        for c in range(cols):
            x0 = c * 64
            y0 = r * 64
            step = (1 if c % 2 == 1 else 0)
            if r == 0: # Down
                draw_runic_warhammer(draw, x0 + 41, y0 + 20 + step, x0 + 41, y0 + 25 + step, x0 + 41, y0 + 44 + step)
                draw_armored_hands(draw, x0 + 41, y0 + 34 + step)
            elif r == 1: # Left
                draw_runic_warhammer(draw, x0 + 20, y0 + 19 + step, x0 + 21, y0 + 23 + step, x0 + 26, y0 + 43 + step)
                draw_armored_hands(draw, x0 + 22, y0 + 31 + step)
                draw_armored_hands(draw, x0 + 24, y0 + 36 + step)
            elif r == 2: # Up-Left
                draw_runic_warhammer(draw, x0 + 22, y0 + 18 + step, x0 + 23, y0 + 22 + step, x0 + 27, y0 + 42 + step)
            elif r == 3: # Up
                draw_runic_warhammer(draw, x0 + 24, y0 + 19 + step, x0 + 25, y0 + 23 + step, x0 + 27, y0 + 43 + step)
                
    barb.save(os.path.join(p4_dir, dst))
    print(f"Saved {dst}")

def build_attack_and_skill():
    # Remove sword pixels from Sword_Attack.png first
    full = Image.open(os.path.join(p3_dir, "Sword_Attack.png")).convert("RGBA")
    s_back = Image.open(os.path.join(p3_dir, "PNG/Sword_Attack/Sword_attack2_sword_back.png")).convert("RGBA")
    s_front = Image.open(os.path.join(p3_dir, "PNG/Sword_Attack/Sword_attack4_sword_front.png")).convert("RGBA")
    s_swing = Image.open(os.path.join(p3_dir, "PNG/Sword_Attack/Sword_attack6_swing.png")).convert("RGBA")

    w, h = full.size
    clean_body = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    full_p = full.load()
    sb_p = s_back.load()
    sf_p = s_front.load()
    ss_p = s_swing.load()
    cb_p = clean_body.load()

    for y in range(h):
        for x in range(w):
            if sb_p[x, y][3] > 20 or sf_p[x, y][3] > 20 or ss_p[x, y][3] > 20:
                continue
            cb_p[x, y] = full_p[x, y]

    barb_atk = repaint_to_barbarian_berserker(clean_body)
    draw = ImageDraw.Draw(barb_atk)
    
    # Attack Animation: 8 Frames Overhead Slam
    for r in range(4):
        for c in range(8):
            x0 = c * 64
            y0 = r * 64
            
            if r in (1, 2): # Side
                if c == 0:
                    draw_runic_warhammer(draw, x0 + 26, y0 + 16, x0 + 27, y0 + 21, x0 + 30, y0 + 38)
                    draw_armored_hands(draw, x0 + 29, y0 + 33)
                elif c == 1:
                    draw_runic_warhammer(draw, x0 + 32, y0 + 10, x0 + 32, y0 + 16, x0 + 32, y0 + 34)
                    draw_armored_hands(draw, x0 + 32, y0 + 28)
                elif c in (2, 3):
                    draw_runic_warhammer(draw, x0 + 36, y0 + 8, x0 + 35, y0 + 14, x0 + 32, y0 + 32)
                    draw_armored_hands(draw, x0 + 33, y0 + 26)
                    draw.point((x0 + 32, y0 + 6), fill=C_HAM_RUNE)
                    draw.point((x0 + 40, y0 + 6), fill=C_HAM_RUNE)
                elif c == 4:
                    draw_runic_warhammer(draw, x0 + 24, y0 + 28, x0 + 27, y0 + 31, x0 + 34, y0 + 36)
                    draw_armored_hands(draw, x0 + 32, y0 + 35)
                elif c in (5, 6):
                    # Ground Smash Impact
                    draw_runic_warhammer(draw, x0 + 14, y0 + 44, x0 + 19, y0 + 41, x0 + 30, y0 + 35)
                    draw_armored_hands(draw, x0 + 26, y0 + 37)
                    # Ground fractures & sparks
                    draw.line([x0 + 4, y0 + 50, x0 + 26, y0 + 50], fill=C_GLD_HI)
                    draw.line([x0 + 2, y0 + 51, x0 + 28, y0 + 51], fill=C_GLD_MID)
                    draw.point((x0 + 8, y0 + 47), fill=C_RED_HI)
                    draw.point((x0 + 12, y0 + 45), fill=C_HAM_RUNE)
                    draw.point((x0 + 20, y0 + 46), fill=C_RED_HI)
                elif c == 7:
                    draw_runic_warhammer(draw, x0 + 18, y0 + 32, x0 + 20, y0 + 34, x0 + 28, y0 + 38)
                    draw_armored_hands(draw, x0 + 25, y0 + 36)
            elif r == 0: # Down
                if c in (0, 1):
                    draw_runic_warhammer(draw, x0 + 32, y0 + 12, x0 + 32, y0 + 18, x0 + 32, y0 + 36)
                    draw_armored_hands(draw, x0 + 32, y0 + 28)
                elif c in (2, 3):
                    draw_runic_warhammer(draw, x0 + 32, y0 + 8, x0 + 32, y0 + 14, x0 + 32, y0 + 34)
                    draw_armored_hands(draw, x0 + 32, y0 + 26)
                elif c in (4, 5, 6):
                    draw_runic_warhammer(draw, x0 + 32, y0 + 45, x0 + 32, y0 + 39, x0 + 32, y0 + 30)
                    draw_armored_hands(draw, x0 + 32, y0 + 33)
                    draw.line([x0 + 18, y0 + 51, x0 + 46, y0 + 51], fill=C_GLD_HI)
                    draw.point((x0 + 22, y0 + 48), fill=C_RED_HI)
                    draw.point((x0 + 42, y0 + 48), fill=C_RED_HI)
                else:
                    draw_runic_warhammer(draw, x0 + 36, y0 + 28, x0 + 36, y0 + 33, x0 + 36, y0 + 44)
                    draw_armored_hands(draw, x0 + 36, y0 + 38)
            elif r == 3: # Up
                if c in (0, 1, 2, 3):
                    draw_runic_warhammer(draw, x0 + 32, y0 + 10, x0 + 32, y0 + 16, x0 + 32, y0 + 34)
                else:
                    draw_runic_warhammer(draw, x0 + 32, y0 + 24, x0 + 32, y0 + 29, x0 + 32, y0 + 42)
                    
    barb_atk.save(os.path.join(p4_dir, "Hammer_Attack.png"))
    
    # Skill Area Slam: reuse attack with enhanced 360 degree shockwaves
    barb_skill = barb_atk.copy()
    d_sk = ImageDraw.Draw(barb_skill)
    for r in range(4):
        for c in range(8):
            x0 = c * 64
            y0 = r * 64
            if c in (4, 5, 6):
                # Radial rocks and ground rupture
                d_sk.ellipse([x0 + 6, y0 + 40, x0 + 58, y0 + 58], outline=C_GLD_HI)
                d_sk.point((x0 + 12, y0 + 42), fill=C_HAM_RUNE)
                d_sk.point((x0 + 52, y0 + 42), fill=C_HAM_RUNE)
                d_sk.point((x0 + 32, y0 + 56), fill=C_RED_HI)
    barb_skill.save(os.path.join(p4_dir, "Hammer_Skill.png"))
    print("Saved Hammer_Attack.png and Hammer_Skill.png")

def build_hurt_death():
    for name, cols in [("Unarmed_Hurt.png", 5), ("Unarmed_Death.png", 7)]:
        base = Image.open(os.path.join(p3_dir, name))
        barb = repaint_to_barbarian_berserker(base)
        dst = "Hammer_Hurt.png" if "Hurt" in name else "Hammer_Death.png"
        barb.save(os.path.join(p4_dir, dst))
        print(f"Saved {dst}")

def build_portrait():
    # Build 128x128 Pixel Art Avatar from Idle Down Frame
    idle = Image.open(os.path.join(p4_dir, "Hammer_Idle.png"))
    f0 = idle.crop((0, 0, 64, 64))
    portrait = f0.resize((128, 128), Image.Resampling.NEAREST)
    portrait.save(os.path.join(p4_ui_dir, "Hero_Player4_Portrait.png"))
    print("Saved Hero_Player4_Portrait.png")

build_idle()
build_movement("Unarmed_Walk.png", "Hammer_Walk.png", 6)
build_movement("Unarmed_Run.png", "Hammer_Run.png", 8)
build_attack_and_skill()
build_hurt_death()
build_portrait()

# Build Showcase Preview
idle = Image.open(os.path.join(p4_dir, "Hammer_Idle.png"))
atk = Image.open(os.path.join(p4_dir, "Hammer_Attack.png"))
skill = Image.open(os.path.join(p4_dir, "Hammer_Skill.png"))

canvas = Image.new("RGBA", (64 * 4, 64 * 3), (20, 22, 28, 255))
for i, r in enumerate([0, 1, 2, 3]):
    f = idle.crop((0, r * 64, 64, (r + 1) * 64))
    canvas.paste(f, (i * 64, 0), f)
for i, c in enumerate([0, 2, 4, 6]):
    f = atk.crop((c * 64, 64, (c + 1) * 64, 128))
    canvas.paste(f, (i * 64, 64), f)
for i, c in enumerate([0, 2, 4, 6]):
    f = skill.crop((c * 64, 0, (c + 1) * 64, 64))
    canvas.paste(f, (i * 64, 128), f)

preview_4x = canvas.resize((canvas.width * 4, canvas.height * 4), Image.Resampling.NEAREST)
preview_path = "Assets/ART/Player4/barbarian_berserker_handcrafted_preview.png"
preview_4x.save(preview_path)

artifact_dir = "C:/Users/TRUONG HUNG/.gemini/antigravity-ide/brain/953cb4d4-3543-43c3-8946-55fea43b90d1"
shutil.copy(preview_path, os.path.join(artifact_dir, "barbarian_berserker_handcrafted_preview.png"))
print("Saved barbarian_berserker_handcrafted_preview.png!")
