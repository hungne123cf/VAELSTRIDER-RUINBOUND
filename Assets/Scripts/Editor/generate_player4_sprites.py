import os
import shutil
from PIL import Image, ImageDraw

p3_dir = "Assets/ART/Player3"
p4_dir = "Assets/ART/Player4"
p4_ui_dir = "Assets/ART/Player4/UI"

os.makedirs(p4_dir, exist_ok=True)
os.makedirs(p4_ui_dir, exist_ok=True)

# 4-Tone Shading Palette for Titan Berserker
C_OUT      = (24, 26, 33, 255)
C_STL_SHD  = (45, 52, 65, 255)
C_STL_MID  = (78, 90, 112, 255)
C_STL_HI   = (135, 155, 185, 255)
C_STL_SPEC = (215, 230, 250, 255)

C_GLD_SHD  = (115, 70, 12, 255)
C_GLD_MID  = (205, 145, 25, 255)
C_GLD_HI   = (255, 215, 65, 255)
C_GLD_SPEC = (255, 245, 185, 255)

C_RBY_SHD  = (120, 15, 15, 255)
C_RBY_MID  = (225, 35, 35, 255)
C_RBY_HI   = (255, 110, 110, 255)
C_RBY_SPEC = (255, 220, 220, 255)

C_WOD_SHD  = (38, 26, 18, 255)
C_WOD_MID  = (72, 48, 33, 255)
C_WOD_HI   = (108, 74, 52, 255)

# Fur Mantle (White/Grey) & Red Sash
C_FUR_HI   = (235, 230, 220, 255)
C_FUR_MID  = (180, 170, 160, 255)
C_FUR_SHD  = (115, 105, 95, 255)

C_RED_HI   = (235, 45, 45, 255)
C_RED_MID  = (180, 20, 20, 255)
C_RED_SHD  = (110, 10, 10, 255)

def is_sword_mask_file(filename):
    fn = filename.lower()
    return ('sword' in fn or 'swing' in fn) and not ('body' in fn or 'head' in fn or 'full' in fn or 'meta' in fn)

def load_clean_clothed_sheet(sheet_filename, folder_name):
    """
    Takes the fully-clothed warrior sheet (e.g. Sword_Idle.png) and subtracts
    the sword layers so the warrior keeps 100% of their clothes, armor, and hair.
    """
    full_path = os.path.join(p3_dir, sheet_filename)
    full = Image.open(full_path).convert("RGBA")
    w, h = full.size
    clean = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    
    full_p = full.load()
    clean_p = clean.load()
    
    png_folder = os.path.join(p3_dir, "PNG", folder_name)
    sword_masks = []
    if os.path.exists(png_folder):
        for f in os.listdir(png_folder):
            if is_sword_mask_file(f):
                p = os.path.join(png_folder, f)
                im = Image.open(p).convert("RGBA")
                if im.size == (w, h):
                    sword_masks.append(im.load())
                    
    for y in range(h):
        for x in range(w):
            is_sword = False
            for mask in sword_masks:
                if mask[x, y][3] > 20:
                    is_sword = True
                    break
            if not is_sword:
                clean_p[x, y] = full_p[x, y]
                
    return clean

def transform_to_titan_berserker(img):
    """
    Uniformly recolors clothed warrior sprites into Titan Berserker:
    - Titanium steel armor with radiant gold trim
    - White fur mantle around shoulders / neck
    - Red loincloth / sash at waist
    - Tanned skin
    """
    img = img.convert("RGBA")
    w, h = img.size
    pixels = img.load()
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    out_pix = out.load()
    
    for y in range(h):
        for x in range(w):
            r, g, b, a = pixels[x, y]
            if a < 15:
                continue
            
            is_blue = (b > r + 8 and b > 50) or (g > r + 8 and b > 45)
            is_gold = (r > 120 and g > 90 and b < 80)
            is_white = (r > 175 and g > 175 and b > 175)
            is_skin = (r > 145 and g > 95 and b > 65 and r > g and g > b)
            
            cy = y % 64
            
            if is_blue:
                lum = (r * 0.299 + g * 0.587 + b * 0.114) / 255.0
                if cy in range(17, 24):
                    # Shoulder collar -> White Fur Mantle
                    if lum > 0.6:
                        out_pix[x, y] = C_FUR_HI
                    elif lum > 0.35:
                        out_pix[x, y] = C_FUR_MID
                    else:
                        out_pix[x, y] = C_FUR_SHD
                elif cy in range(35, 41):
                    # Belt area -> Crimson Loincloth / Sash
                    if lum > 0.5:
                        out_pix[x, y] = C_RED_HI
                    elif lum > 0.3:
                        out_pix[x, y] = C_RED_MID
                    else:
                        out_pix[x, y] = C_RED_SHD
                else:
                    # Titanium armor plate with Gold Trim
                    if lum > 0.65:
                        out_pix[x, y] = (int(245 * lum), int(190 * lum), int(50 * lum), 255)
                    elif lum > 0.35:
                        out_pix[x, y] = (int(95 * lum * 1.5), int(110 * lum * 1.5), int(135 * lum * 1.5), 255)
                    else:
                        out_pix[x, y] = (int(40 * lum * 1.8), int(45 * lum * 1.8), int(55 * lum * 1.8), 255)
            elif is_gold:
                out_pix[x, y] = (min(255, int(r * 1.15)), min(255, int(g * 1.1)), min(255, int(b * 0.6)), 255)
            elif is_white:
                out_pix[x, y] = C_FUR_HI
            elif is_skin:
                out_pix[x, y] = (min(255, int(r * 0.95)), min(255, int(g * 0.85)), min(255, int(b * 0.75)), a)
            else:
                out_pix[x, y] = (int(r * 0.88), int(g * 0.88), int(b * 0.92), a)

    # Broaden shoulders with fur mantle
    cols = w // 64
    rows = h // 64
    for r in range(rows):
        for c in range(cols):
            x0 = c * 64
            y0 = r * 64
            for cy in range(y0 + 18, y0 + 35):
                xs = [cx for cx in range(x0 + 12, x0 + 52) if out_pix[cx, cy][3] > 50]
                if xs:
                    min_x = min(xs)
                    max_x = max(xs)
                    if min_x > x0 + 8:
                        out_pix[min_x - 1, cy] = C_FUR_MID
                        out_pix[min_x - 2, cy] = C_OUT
                    if max_x < x0 + 56:
                        out_pix[max_x + 1, cy] = C_FUR_MID
                        out_pix[max_x + 2, cy] = C_OUT

    return out

def draw_straight_shaft(draw, x0, y0, x1, y1):
    """
    Renders a perfectly straight, solid 2px thick wooden/iron shaft with pommel cap.
    """
    dx = x1 - x0
    dy = y1 - y0
    steps = max(abs(dx), abs(dy), 1)
    
    for i in range(steps + 1):
        t = i / float(steps)
        px = int(round(x0 + t * dx))
        py = int(round(y0 + t * dy))
        draw.point((px, py), fill=C_WOD_MID)
        draw.point((px + 1, py), fill=C_WOD_SHD)
        if i % 4 == 0:
            draw.point((px, py), fill=C_GLD_MID)
            
    # Pommel cap
    draw.rectangle([x1 - 1, y1 - 1, x1 + 2, y1 + 1], fill=C_OUT)
    draw.rectangle([x1, y1, x1 + 1, y1], fill=C_GLD_HI)

def draw_hammer_head(draw, cx, cy, orientation="horiz"):
    """
    Renders a massive, symmetrical beveled Titan War Hammer head (13x9).
    """
    if orientation == "horiz":
        # 13 wide, 9 tall octagonal beveled head
        pts = [
            (cx - 5, cy - 4), (cx + 5, cy - 4),
            (cx + 6, cy - 3), (cx + 6, cy + 3),
            (cx + 5, cy + 4), (cx - 5, cy + 4),
            (cx - 6, cy + 3), (cx - 6, cy - 3)
        ]
        draw.polygon(pts, fill=C_OUT)
        
        inner_pts = [
            (cx - 4, cy - 3), (cx + 4, cy - 3),
            (cx + 5, cy - 2), (cx + 5, cy + 2),
            (cx + 4, cy + 3), (cx - 4, cy + 3),
            (cx - 5, cy + 2), (cx - 5, cy - 2)
        ]
        draw.polygon(inner_pts, fill=C_STL_MID)
        
        draw.line([(cx - 4, cy - 3), (cx + 4, cy - 3)], fill=C_STL_HI)
        draw.line([(cx - 5, cy - 2), (cx - 5, cy + 2)], fill=C_STL_HI)
        draw.point((cx - 4, cy - 2), fill=C_STL_SPEC)
        
        draw.line([(cx - 4, cy + 3), (cx + 4, cy + 3)], fill=C_STL_SHD)
        draw.line([(cx + 5, cy - 2), (cx + 5, cy + 2)], fill=C_STL_SHD)
        
        # Central Gold Collar
        draw.rectangle([cx - 2, cy - 4, cx + 2, cy + 4], fill=C_GLD_MID)
        draw.line([(cx - 2, cy - 4), (cx - 2, cy + 4)], fill=C_GLD_SHD)
        draw.line([(cx + 2, cy - 4), (cx + 2, cy + 4)], fill=C_GLD_HI)
        draw.point((cx, cy - 4), fill=C_GLD_SPEC)
        
        # Ruby Rune Core
        draw.rectangle([cx - 1, cy - 1, cx + 1, cy + 1], fill=C_RBY_MID)
        draw.point((cx, cy - 1), fill=C_RBY_HI)
        draw.point((cx, cy), fill=C_RBY_SPEC)
    else:
        # Vertical Head (9 wide, 13 tall)
        draw.rectangle([cx - 4, cy - 5, cx + 4, cy + 5], fill=C_OUT)
        draw.rectangle([cx - 3, cy - 4, cx + 3, cy + 4], fill=C_STL_MID)
        draw.line([(cx - 3, cy - 4), (cx + 3, cy - 4)], fill=C_STL_HI)
        draw.line([(cx - 3, cy - 4), (cx - 3, cy + 4)], fill=C_STL_HI)
        draw.point((cx - 3, cy - 4), fill=C_STL_SPEC)
        draw.rectangle([cx - 4, cy - 2, cx + 4, cy + 2], fill=C_GLD_MID)
        draw.point((cx, cy), fill=C_RBY_HI)

def draw_gauntlets(draw, x, y):
    """Renders armored gauntlets wrapping cleanly around the handle."""
    draw.rectangle([x - 1, y - 1, x + 2, y + 1], fill=C_STL_MID)
    draw.line([(x - 1, y - 1), (x + 2, y - 1)], fill=C_STL_HI)
    draw.point((x + 1, y - 1), fill=C_GLD_HI)
    draw.line([(x - 1, y + 1), (x + 2, y + 1)], fill=C_STL_SHD)

# 1. PROCESS IDLE SHEET
def process_idle():
    clean_base = load_clean_clothed_sheet("Sword_Idle.png", "Sword_Idle")
    titan_idle = transform_to_titan_berserker(clean_base)
    draw = ImageDraw.Draw(titan_idle)
    
    for r in range(4):
        for c in range(12):
            x0 = c * 64
            y0 = r * 64
            bob = (1 if (c % 4) in (1, 2) else 0)
            
            if r == 0: # Down (Front)
                draw_straight_shaft(draw, x0 + 41, y0 + 26 + bob, x0 + 41, y0 + 46 + bob)
                draw_hammer_head(draw, x0 + 41, y0 + 21 + bob, "horiz")
                draw_gauntlets(draw, x0 + 40, y0 + 34 + bob)
            elif r == 1: # Left (Side View - 2-Handed Front Guard)
                draw_straight_shaft(draw, x0 + 21, y0 + 23 + bob, x0 + 24, y0 + 45 + bob)
                draw_hammer_head(draw, x0 + 20, y0 + 19 + bob, "horiz")
                draw_gauntlets(draw, x0 + 21, y0 + 31 + bob)
                draw_gauntlets(draw, x0 + 22, y0 + 37 + bob)
            elif r == 2: # Up-Left (Diagonal)
                draw_straight_shaft(draw, x0 + 22, y0 + 22 + bob, x0 + 25, y0 + 44 + bob)
                draw_hammer_head(draw, x0 + 21, y0 + 18 + bob, "horiz")
                draw_gauntlets(draw, x0 + 22, y0 + 32 + bob)
            elif r == 3: # Up (Back View)
                draw_straight_shaft(draw, x0 + 24, y0 + 23 + bob, x0 + 26, y0 + 45 + bob)
                draw_hammer_head(draw, x0 + 23, y0 + 19 + bob, "horiz")
                
    titan_idle.save(os.path.join(p4_dir, "Hammer_Idle.png"))
    print("Saved Hammer_Idle.png (Clothed Titan Berserker)")

# 2. PROCESS WALK & RUN
def process_movement(sheet_name, folder_name, dst_name, cols):
    clean_base = load_clean_clothed_sheet(sheet_name, folder_name)
    titan_mov = transform_to_titan_berserker(clean_base)
    draw = ImageDraw.Draw(titan_mov)
    
    for r in range(4):
        for c in range(cols):
            x0 = c * 64
            y0 = r * 64
            step = (1 if c % 2 == 1 else 0)
            
            if r == 0: # Down
                draw_straight_shaft(draw, x0 + 41, y0 + 25 + step, x0 + 41, y0 + 45 + step)
                draw_hammer_head(draw, x0 + 41, y0 + 20 + step, "horiz")
                draw_gauntlets(draw, x0 + 40, y0 + 33 + step)
            elif r == 1: # Left
                draw_straight_shaft(draw, x0 + 21, y0 + 23 + step, x0 + 24, y0 + 44 + step)
                draw_hammer_head(draw, x0 + 20, y0 + 19 + step, "horiz")
                draw_gauntlets(draw, x0 + 21, y0 + 31 + step)
                draw_gauntlets(draw, x0 + 22, y0 + 36 + step)
            elif r == 2: # Up-Left
                draw_straight_shaft(draw, x0 + 22, y0 + 22 + step, x0 + 25, y0 + 43 + step)
                draw_hammer_head(draw, x0 + 21, y0 + 18 + step, "horiz")
            elif r == 3: # Up
                draw_straight_shaft(draw, x0 + 24, y0 + 23 + step, x0 + 26, y0 + 44 + step)
                draw_hammer_head(draw, x0 + 23, y0 + 19 + step, "horiz")
                
    titan_mov.save(os.path.join(p4_dir, dst_name))
    print(f"Saved {dst_name} (Clothed Titan Berserker)")

# 3. PROCESS ATTACK & SKILL
def process_attack():
    clean_base = load_clean_clothed_sheet("Sword_Attack.png", "Sword_Attack")
    titan_atk = transform_to_titan_berserker(clean_base)
    draw = ImageDraw.Draw(titan_atk)
    
    for r in range(4):
        for c in range(8):
            x0 = c * 64
            y0 = r * 64
            
            if r in (1, 2): # Side Strike
                if c == 0:
                    draw_straight_shaft(draw, x0 + 27, y0 + 21, x0 + 30, y0 + 38)
                    draw_hammer_head(draw, x0 + 26, y0 + 16, "horiz")
                    draw_gauntlets(draw, x0 + 28, y0 + 31)
                elif c == 1:
                    draw_straight_shaft(draw, x0 + 32, y0 + 16, x0 + 32, y0 + 34)
                    draw_hammer_head(draw, x0 + 32, y0 + 10, "horiz")
                    draw_gauntlets(draw, x0 + 31, y0 + 27)
                elif c in (2, 3):
                    draw_straight_shaft(draw, x0 + 35, y0 + 14, x0 + 32, y0 + 32)
                    draw_hammer_head(draw, x0 + 36, y0 + 8, "horiz")
                    draw_gauntlets(draw, x0 + 33, y0 + 25)
                    draw.point((x0 + 32, y0 + 6), fill=C_GLD_HI)
                    draw.point((x0 + 40, y0 + 6), fill=C_GLD_HI)
                elif c == 4:
                    draw_straight_shaft(draw, x0 + 27, y0 + 31, x0 + 34, y0 + 36)
                    draw_hammer_head(draw, x0 + 24, y0 + 28, "horiz")
                    draw_gauntlets(draw, x0 + 30, y0 + 34)
                elif c in (5, 6):
                    # Ground Smash Impact
                    draw_straight_shaft(draw, x0 + 19, y0 + 41, x0 + 30, y0 + 35)
                    draw_hammer_head(draw, x0 + 14, y0 + 44, "horiz")
                    draw_gauntlets(draw, x0 + 25, y0 + 37)
                    draw.line([x0 + 4, y0 + 50, x0 + 26, y0 + 50], fill=C_GLD_HI)
                    draw.line([x0 + 2, y0 + 51, x0 + 28, y0 + 51], fill=C_GLD_MID)
                    draw.point((x0 + 8, y0 + 47), fill=C_RBY_HI)
                    draw.point((x0 + 12, y0 + 45), fill=C_GLD_SPEC)
                    draw.point((x0 + 20, y0 + 46), fill=C_RBY_HI)
                elif c == 7:
                    draw_straight_shaft(draw, x0 + 20, y0 + 34, x0 + 28, y0 + 38)
                    draw_hammer_head(draw, x0 + 18, y0 + 32, "horiz")
                    draw_gauntlets(draw, x0 + 24, y0 + 36)
            elif r == 0: # Down
                if c in (0, 1):
                    draw_straight_shaft(draw, x0 + 32, y0 + 18, x0 + 32, y0 + 36)
                    draw_hammer_head(draw, x0 + 32, y0 + 12, "horiz")
                    draw_gauntlets(draw, x0 + 31, y0 + 28)
                elif c in (2, 3):
                    draw_straight_shaft(draw, x0 + 32, y0 + 14, x0 + 32, y0 + 34)
                    draw_hammer_head(draw, x0 + 32, y0 + 8, "horiz")
                    draw_gauntlets(draw, x0 + 31, y0 + 26)
                    draw.point((x0 + 28, y0 + 6), fill=C_GLD_HI)
                    draw.point((x0 + 36, y0 + 6), fill=C_GLD_HI)
                elif c in (4, 5, 6):
                    draw_straight_shaft(draw, x0 + 32, y0 + 39, x0 + 32, y0 + 30)
                    draw_hammer_head(draw, x0 + 32, y0 + 45, "horiz")
                    draw_gauntlets(draw, x0 + 31, y0 + 33)
                    draw.line([x0 + 18, y0 + 51, x0 + 46, y0 + 51], fill=C_GLD_HI)
                    draw.point((x0 + 22, y0 + 48), fill=C_RBY_HI)
                    draw.point((x0 + 42, y0 + 48), fill=C_RBY_HI)
                else:
                    draw_straight_shaft(draw, x0 + 36, y0 + 33, x0 + 36, y0 + 44)
                    draw_hammer_head(draw, x0 + 36, y0 + 28, "horiz")
                    draw_gauntlets(draw, x0 + 35, y0 + 38)
            elif r == 3: # Up
                if c in (0, 1, 2, 3):
                    draw_straight_shaft(draw, x0 + 32, y0 + 16, x0 + 32, y0 + 34)
                    draw_hammer_head(draw, x0 + 32, y0 + 10, "horiz")
                else:
                    draw_straight_shaft(draw, x0 + 32, y0 + 29, x0 + 32, y0 + 42)
                    draw_hammer_head(draw, x0 + 32, y0 + 24, "horiz")
                    
    titan_atk.save(os.path.join(p4_dir, "Hammer_Attack.png"))
    
    # Skill Area Slam
    titan_skill = titan_atk.copy()
    d_sk = ImageDraw.Draw(titan_skill)
    for r in range(4):
        for c in range(8):
            x0 = c * 64
            y0 = r * 64
            if c in (4, 5, 6):
                d_sk.ellipse([x0 + 6, y0 + 40, x0 + 58, y0 + 58], outline=C_GLD_HI)
                d_sk.point((x0 + 12, y0 + 42), fill=C_RBY_HI)
                d_sk.point((x0 + 52, y0 + 42), fill=C_RBY_HI)
                d_sk.point((x0 + 32, y0 + 56), fill=C_GLD_SPEC)
    titan_skill.save(os.path.join(p4_dir, "Hammer_Skill.png"))
    print("Saved Hammer_Attack.png & Hammer_Skill.png (Clothed Titan Berserker)")

# 4. PROCESS HURT & DEATH
def process_hurt_death():
    for sheet_name, folder, dst_name in [("Sword_Hurt.png", "Sword_Hurt", "Hammer_Hurt.png"), ("Sword_Death.png", "Sword_Death", "Hammer_Death.png")]:
        clean = load_clean_clothed_sheet(sheet_name, folder)
        titan = transform_to_titan_berserker(clean)
        titan.save(os.path.join(p4_dir, dst_name))
        print(f"Saved {dst_name} (Clothed Titan Berserker)")

# 5. PROCESS PORTRAIT
def process_portrait():
    idle = Image.open(os.path.join(p4_dir, "Hammer_Idle.png"))
    f0 = idle.crop((0, 0, 64, 64))
    portrait = f0.resize((128, 128), Image.Resampling.NEAREST)
    portrait.save(os.path.join(p4_ui_dir, "Hero_Player4_Portrait.png"))
    print("Saved Hero_Player4_Portrait.png")

process_idle()
process_movement("Sword_Walk.png", "Sword_Walk", "Hammer_Walk.png", 6)
process_movement("Sword_Run.png", "Sword_Run", "Hammer_Run.png", 8)
process_attack()
process_hurt_death()
process_portrait()
print("All 6 Player 4 Sprite Sheets successfully generated with 100% unified clothed warrior armor & straight heavy war hammer!")
