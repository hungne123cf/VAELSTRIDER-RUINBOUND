import os
import shutil
from PIL import Image, ImageDraw, ImageEnhance

p3_dir = "Assets/ART/Player3"
p4_dir = "Assets/ART/Player4"
p4_ui_dir = "Assets/ART/Player4/UI"

os.makedirs(p4_dir, exist_ok=True)
os.makedirs(p4_ui_dir, exist_ok=True)

# 4-Tone Shading Palette for Viking Titan Berserker
C_OUT      = (20, 22, 28, 255)

# Titanium Steel Armor & Helmet
C_STL_DEEP = (28, 33, 42, 255)
C_STL_SHD  = (48, 56, 70, 255)
C_STL_MID  = (80, 92, 114, 255)
C_STL_HI   = (140, 160, 190, 255)
C_STL_SPEC = (220, 235, 255, 255)

# Gold Trims & Horns
C_GLD_SHD  = (120, 75, 15, 255)
C_GLD_MID  = (210, 150, 30, 255)
C_GLD_HI   = (255, 215, 65, 255)
C_GLD_SPEC = (255, 245, 185, 255)

# Ruby Rune Core & Spark FX
C_RBY_SHD  = (125, 15, 15, 255)
C_RBY_MID  = (225, 35, 35, 255)
C_RBY_HI   = (255, 110, 110, 255)
C_RBY_SPEC = (255, 220, 220, 255)

# White/Grey Fur Mantle Collar
C_FUR_DEEP = (50, 45, 40, 255)
C_FUR_SHD  = (110, 100, 90, 255)
C_FUR_MID  = (175, 165, 155, 255)
C_FUR_HI   = (235, 230, 220, 255)
C_FUR_SPEC = (255, 252, 245, 255)

# Crimson Loincloth / War Sash
C_RED_SHD  = (110, 12, 12, 255)
C_RED_MID  = (185, 22, 22, 255)
C_RED_HI   = (240, 50, 50, 255)

# Nordic Beard & Hair
C_BRD_SHD  = (90, 45, 20, 255)
C_BRD_MID  = (150, 80, 40, 255)
C_BRD_HI   = (205, 120, 65, 255)

# Shaft Wood & Leather
C_WOD_SHD  = (38, 26, 18, 255)
C_WOD_MID  = (72, 48, 33, 255)
C_WOD_HI   = (108, 74, 52, 255)

def is_sword_mask_file(filename):
    fn = filename.lower()
    return ('sword' in fn or 'swing' in fn) and not ('body' in fn or 'head' in fn or 'full' in fn or 'meta' in fn)

def load_clean_clothed_sheet(sheet_filename, folder_name):
    """
    Extracts the fully-clothed and animated warrior base from Player 3,
    masking out sword pixels so all animated body gestures (walk strides,
    run sprint, attack windup/slam, hurt recoil, death fall) remain 100% intact.
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

def transform_to_viking_berserker(img):
    """
    Repaints the animated character body into the Viking Titan Berserker:
    - Viking Horned/Steel Helmet & Braided Beard
    - White Fur Mantle Collar across shoulders
    - Dark Titanium Armor with Radiant Gold Trim
    - Crimson War Sash at waist
    - Broad muscular silhouette
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
            
            is_blue = (b > r + 8 and b > 50) or (g > r + 8 and b > 45)
            is_gold = (r > 120 and g > 90 and b < 80)
            is_white = (r > 175 and g > 175 and b > 175)
            is_skin = (r > 145 and g > 95 and b > 65 and r > g and g > b)
            is_hair = (r < 90 and g < 70 and b < 60)
            
            cy = y % 64
            
            if is_hair and cy < 22:
                # Viking Helmet & Horns
                lum = (r * 0.299 + g * 0.587 + b * 0.114) / 255.0
                if lum > 0.6:
                    out_pix[x, y] = C_STL_SPEC
                elif lum > 0.35:
                    out_pix[x, y] = C_STL_HI
                else:
                    out_pix[x, y] = C_STL_MID
            elif is_skin and cy in range(18, 25):
                # Nordic Braided Beard on lower face
                out_pix[x, y] = C_BRD_MID
            elif is_blue:
                lum = (r * 0.299 + g * 0.587 + b * 0.114) / 255.0
                if cy in range(17, 24):
                    # White Fur Mantle across shoulders
                    if lum > 0.6:
                        out_pix[x, y] = C_FUR_HI
                    elif lum > 0.35:
                        out_pix[x, y] = C_FUR_MID
                    else:
                        out_pix[x, y] = C_FUR_SHD
                elif cy in range(35, 41):
                    # Crimson Loincloth / Sash
                    if lum > 0.5:
                        out_pix[x, y] = C_RED_HI
                    elif lum > 0.3:
                        out_pix[x, y] = C_RED_MID
                    else:
                        out_pix[x, y] = C_RED_SHD
                else:
                    # Titanium Armor Plate with Gold Trim
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

    # Add helmet horns and fur mantle volume
    for r in range(rows):
        for c in range(cols):
            x0 = c * 64
            y0 = r * 64
            # Horns on sides of helmet (cy 12..15)
            if out_pix[x0 + 24, y0 + 13][3] > 50:
                out_pix[x0 + 23, y0 + 12] = C_GLD_HI
                out_pix[x0 + 22, y0 + 11] = C_GLD_SPEC
            if out_pix[x0 + 39, y0 + 13][3] > 50:
                out_pix[x0 + 40, y0 + 12] = C_GLD_HI
                out_pix[x0 + 41, y0 + 11] = C_GLD_SPEC
                
            # Broaden shoulders with fur mantle (cy 18..24)
            for cy in range(y0 + 18, y0 + 25):
                xs = [cx for cx in range(x0 + 10, x0 + 54) if out_pix[cx, cy][3] > 50]
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
    """Renders a solid straight 2px ironwood shaft with gold pommel cap."""
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
            
    draw.rectangle([x1 - 1, y1 - 1, x1 + 2, y1 + 1], fill=C_OUT)
    draw.rectangle([x1, y1, x1 + 1, y1], fill=C_GLD_HI)

def draw_hammer_head(draw, cx, cy, orientation="horiz"):
    """Renders the heavy 13x9 beveled War Hammer head with glowing core."""
    if orientation == "horiz":
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
        
        # Gold collar & core
        draw.rectangle([cx - 2, cy - 4, cx + 2, cy + 4], fill=C_GLD_MID)
        draw.line([(cx - 2, cy - 4), (cx - 2, cy + 4)], fill=C_GLD_SHD)
        draw.line([(cx + 2, cy - 4), (cx + 2, cy + 4)], fill=C_GLD_HI)
        draw.point((cx, cy - 4), fill=C_GLD_SPEC)
        
        draw.rectangle([cx - 1, cy - 1, cx + 1, cy + 1], fill=C_RBY_MID)
        draw.point((cx, cy - 1), fill=C_RBY_HI)
        draw.point((cx, cy), fill=C_RBY_SPEC)
    else:
        draw.rectangle([cx - 4, cy - 5, cx + 4, cy + 5], fill=C_OUT)
        draw.rectangle([cx - 3, cy - 4, cx + 3, cy + 4], fill=C_STL_MID)
        draw.line([(cx - 3, cy - 4), (cx + 3, cy - 4)], fill=C_STL_HI)
        draw.line([(cx - 3, cy - 4), (cx - 3, cy + 4)], fill=C_STL_HI)
        draw.point((cx - 3, cy - 4), fill=C_STL_SPEC)
        draw.rectangle([cx - 4, cy - 2, cx + 4, cy + 2], fill=C_GLD_MID)
        draw.point((cx, cy), fill=C_RBY_HI)

def draw_gauntlets(draw, x, y):
    """Renders armored gauntlets holding the shaft."""
    draw.rectangle([x - 1, y - 1, x + 2, y + 1], fill=C_STL_MID)
    draw.line([(x - 1, y - 1), (x + 2, y - 1)], fill=C_STL_HI)
    draw.point((x + 1, y - 1), fill=C_GLD_HI)
    draw.line([(x - 1, y + 1), (x + 2, y + 1)], fill=C_STL_SHD)

# 1. GENERATE IDLE
def generate_idle():
    clean = load_clean_clothed_sheet("Sword_Idle.png", "Sword_Idle")
    titan = transform_to_viking_berserker(clean)
    draw = ImageDraw.Draw(titan)
    
    for r in range(4):
        for c in range(12):
            x0 = c * 64
            y0 = r * 64
            bob = (1 if (c % 4) in (1, 2) else 0)
            
            if r == 0: # Down (Front)
                draw_straight_shaft(draw, x0 + 41, y0 + 26 + bob, x0 + 41, y0 + 46 + bob)
                draw_hammer_head(draw, x0 + 41, y0 + 21 + bob, "horiz")
                draw_gauntlets(draw, x0 + 40, y0 + 34 + bob)
            elif r == 1: # Left (Side View - 2-Handed Front Combat Guard)
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
                
    titan.save(os.path.join(p4_dir, "Hammer_Idle.png"))
    print("Generated Hammer_Idle.png (4 directions)")

# 2. GENERATE WALK (6 frames per direction)
def generate_walk():
    clean = load_clean_clothed_sheet("Sword_Walk.png", "Sword_Walk")
    titan = transform_to_viking_berserker(clean)
    draw = ImageDraw.Draw(titan)
    
    for r in range(4):
        for c in range(6):
            x0 = c * 64
            y0 = r * 64
            step = (1 if c in (1, 4) else 0)
            
            if r == 0: # Down Walk
                draw_straight_shaft(draw, x0 + 41, y0 + 25 + step, x0 + 41, y0 + 45 + step)
                draw_hammer_head(draw, x0 + 41, y0 + 20 + step, "horiz")
                draw_gauntlets(draw, x0 + 40, y0 + 33 + step)
            elif r == 1: # Left Walk
                draw_straight_shaft(draw, x0 + 21, y0 + 23 + step, x0 + 24, y0 + 44 + step)
                draw_hammer_head(draw, x0 + 20, y0 + 19 + step, "horiz")
                draw_gauntlets(draw, x0 + 21, y0 + 31 + step)
                draw_gauntlets(draw, x0 + 22, y0 + 36 + step)
            elif r == 2: # Up-Left Walk
                draw_straight_shaft(draw, x0 + 22, y0 + 22 + step, x0 + 25, y0 + 43 + step)
                draw_hammer_head(draw, x0 + 21, y0 + 18 + step, "horiz")
            elif r == 3: # Up Walk
                draw_straight_shaft(draw, x0 + 24, y0 + 23 + step, x0 + 26, y0 + 44 + step)
                draw_hammer_head(draw, x0 + 23, y0 + 19 + step, "horiz")
                
    titan.save(os.path.join(p4_dir, "Hammer_Walk.png"))
    print("Generated Hammer_Walk.png (4 directions)")

# 3. GENERATE RUN (8 frames per direction)
def generate_run():
    clean = load_clean_clothed_sheet("Sword_Run.png", "Sword_Run")
    titan = transform_to_viking_berserker(clean)
    draw = ImageDraw.Draw(titan)
    
    for r in range(4):
        for c in range(8):
            x0 = c * 64
            y0 = r * 64
            step = (1 if c in (1, 3, 5, 7) else 0)
            
            if r == 0: # Down Run
                draw_straight_shaft(draw, x0 + 41, y0 + 25 + step, x0 + 41, y0 + 45 + step)
                draw_hammer_head(draw, x0 + 41, y0 + 20 + step, "horiz")
                draw_gauntlets(draw, x0 + 40, y0 + 33 + step)
            elif r == 1: # Left Run
                draw_straight_shaft(draw, x0 + 21, y0 + 23 + step, x0 + 24, y0 + 44 + step)
                draw_hammer_head(draw, x0 + 20, y0 + 19 + step, "horiz")
                draw_gauntlets(draw, x0 + 21, y0 + 31 + step)
                draw_gauntlets(draw, x0 + 22, y0 + 36 + step)
            elif r == 2: # Up-Left Run
                draw_straight_shaft(draw, x0 + 22, y0 + 22 + step, x0 + 25, y0 + 43 + step)
                draw_hammer_head(draw, x0 + 21, y0 + 18 + step, "horiz")
            elif r == 3: # Up Run
                draw_straight_shaft(draw, x0 + 24, y0 + 23 + step, x0 + 26, y0 + 44 + step)
                draw_hammer_head(draw, x0 + 23, y0 + 19 + step, "horiz")
                
    titan.save(os.path.join(p4_dir, "Hammer_Run.png"))
    print("Generated Hammer_Run.png (4 directions)")

# 4. GENERATE ATTACK & SKILL (8 frames per direction)
def generate_attack():
    clean = load_clean_clothed_sheet("Sword_Attack.png", "Sword_Attack")
    titan = transform_to_viking_berserker(clean)
    draw = ImageDraw.Draw(titan)
    
    for r in range(4):
        for c in range(8):
            x0 = c * 64
            y0 = r * 64
            
            if r in (1, 2): # Side Attack
                if c == 0:
                    draw_straight_shaft(draw, x0 + 27, y0 + 21, x0 + 30, y0 + 38)
                    draw_hammer_head(draw, x0 + 26, y0 + 16, "horiz")
                    draw_gauntlets(draw, x0 + 28, y0 + 31)
                elif c == 1:
                    draw_straight_shaft(draw, x0 + 32, y0 + 16, x0 + 32, y0 + 34)
                    draw_hammer_head(draw, x0 + 32, y0 + 10, "horiz")
                    draw_gauntlets(draw, x0 + 31, y0 + 27)
                elif c in (2, 3):
                    # Apex High Overhead
                    draw_straight_shaft(draw, x0 + 35, y0 + 14, x0 + 32, y0 + 32)
                    draw_hammer_head(draw, x0 + 36, y0 + 8, "horiz")
                    draw_gauntlets(draw, x0 + 33, y0 + 25)
                    draw.point((x0 + 32, y0 + 6), fill=C_GLD_HI)
                    draw.point((x0 + 40, y0 + 6), fill=C_GLD_HI)
                elif c == 4:
                    # Downward Strike Arc
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
                    # Recovery
                    draw_straight_shaft(draw, x0 + 20, y0 + 34, x0 + 28, y0 + 38)
                    draw_hammer_head(draw, x0 + 18, y0 + 32, "horiz")
                    draw_gauntlets(draw, x0 + 24, y0 + 36)
            elif r == 0: # Down Attack
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
            elif r == 3: # Up Attack
                if c in (0, 1, 2, 3):
                    draw_straight_shaft(draw, x0 + 32, y0 + 16, x0 + 32, y0 + 34)
                    draw_hammer_head(draw, x0 + 32, y0 + 10, "horiz")
                else:
                    draw_straight_shaft(draw, x0 + 32, y0 + 29, x0 + 32, y0 + 42)
                    draw_hammer_head(draw, x0 + 32, y0 + 24, "horiz")
                    
    titan.save(os.path.join(p4_dir, "Hammer_Attack.png"))
    
    # Area Slam Skill
    skill = titan.copy()
    d_sk = ImageDraw.Draw(skill)
    for r in range(4):
        for c in range(8):
            x0 = c * 64
            y0 = r * 64
            if c in (4, 5, 6):
                d_sk.ellipse([x0 + 6, y0 + 40, x0 + 58, y0 + 58], outline=C_GLD_HI)
                d_sk.point((x0 + 12, y0 + 42), fill=C_RBY_HI)
                d_sk.point((x0 + 52, y0 + 42), fill=C_RBY_HI)
                d_sk.point((x0 + 32, y0 + 56), fill=C_GLD_SPEC)
    skill.save(os.path.join(p4_dir, "Hammer_Skill.png"))
    print("Generated Hammer_Attack.png and Hammer_Skill.png (4 directions)")

# 5. GENERATE HURT & DEATH (True hurt recoil & death collapse)
def generate_hurt_death():
    # Hurt (5 frames x 4 directions)
    clean_hurt = load_clean_clothed_sheet("Sword_Hurt.png", "Sword_Hurt")
    titan_hurt = transform_to_viking_berserker(clean_hurt)
    titan_hurt.save(os.path.join(p4_dir, "Hammer_Hurt.png"))
    
    # Death (7 frames x 4 directions)
    clean_death = load_clean_clothed_sheet("Sword_Death.png", "Sword_Death")
    titan_death = transform_to_viking_berserker(clean_death)
    titan_death.save(os.path.join(p4_dir, "Hammer_Death.png"))
    print("Generated Hammer_Hurt.png and Hammer_Death.png (4 directions)")

# 6. GENERATE PORTRAIT
def generate_portrait():
    idle = Image.open(os.path.join(p4_dir, "Hammer_Idle.png"))
    f0 = idle.crop((0, 0, 64, 64))
    portrait = f0.resize((128, 128), Image.Resampling.NEAREST)
    portrait.save(os.path.join(p4_ui_dir, "Hero_Player4_Portrait.png"))
    print("Generated Hero_Player4_Portrait.png")

# Run all generator functions
generate_idle()
generate_walk()
generate_run()
generate_attack()
generate_hurt_death()
generate_portrait()

# Build Showcase Comparison Preview
idle = Image.open(os.path.join(p4_dir, "Hammer_Idle.png"))
walk = Image.open(os.path.join(p4_dir, "Hammer_Walk.png"))
atk = Image.open(os.path.join(p4_dir, "Hammer_Attack.png"))
hurt = Image.open(os.path.join(p4_dir, "Hammer_Hurt.png"))
dead = Image.open(os.path.join(p4_dir, "Hammer_Death.png"))

canvas = Image.new("RGBA", (64 * 6, 64 * 4), (20, 22, 28, 255))

# Row 0: Down (Idle, Walk, Run, Attack Slam, Hurt, Death)
canvas.paste(idle.crop((0, 0, 64, 64)), (0 * 64, 0))
canvas.paste(walk.crop((64, 0, 128, 64)), (1 * 64, 0))
canvas.paste(atk.crop((4 * 64, 0, 5 * 64, 64)), (2 * 64, 0))
canvas.paste(atk.crop((5 * 64, 0, 6 * 64, 64)), (3 * 64, 0))
canvas.paste(hurt.crop((64, 0, 128, 64)), (4 * 64, 0))
canvas.paste(dead.crop((5 * 64, 0, 6 * 64, 64)), (5 * 64, 0))

# Row 1: Left / Side (Idle, Walk, Run, Attack Slam, Hurt, Death)
canvas.paste(idle.crop((0, 64, 64, 128)), (0 * 64, 64))
canvas.paste(walk.crop((64, 64, 128, 128)), (1 * 64, 64))
canvas.paste(atk.crop((4 * 64, 64, 5 * 64, 128)), (2 * 64, 64))
canvas.paste(atk.crop((5 * 64, 64, 6 * 64, 128)), (3 * 64, 64))
canvas.paste(hurt.crop((64, 64, 128, 128)), (4 * 64, 64))
canvas.paste(dead.crop((5 * 64, 64, 6 * 64, 128)), (5 * 64, 64))

# Row 2: Right / Side (Flipped Left)
f_idle_r = idle.crop((0, 64, 64, 128)).transpose(Image.FLIP_LEFT_RIGHT)
f_walk_r = walk.crop((64, 64, 128, 128)).transpose(Image.FLIP_LEFT_RIGHT)
f_atk1_r = atk.crop((4 * 64, 64, 5 * 64, 128)).transpose(Image.FLIP_LEFT_RIGHT)
f_atk2_r = atk.crop((5 * 64, 64, 6 * 64, 128)).transpose(Image.FLIP_LEFT_RIGHT)
f_hurt_r = hurt.crop((64, 64, 128, 128)).transpose(Image.FLIP_LEFT_RIGHT)
f_dead_r = dead.crop((5 * 64, 64, 6 * 64, 128)).transpose(Image.FLIP_LEFT_RIGHT)

canvas.paste(f_idle_r, (0 * 64, 128))
canvas.paste(f_walk_r, (1 * 64, 128))
canvas.paste(f_atk1_r, (2 * 64, 128))
canvas.paste(f_atk2_r, (3 * 64, 128))
canvas.paste(f_hurt_r, (4 * 64, 128))
canvas.paste(f_dead_r, (5 * 64, 128))

# Row 3: Up / Back (Idle, Walk, Attack Slam, Hurt, Death)
canvas.paste(idle.crop((0, 192, 64, 256)), (0 * 64, 192))
canvas.paste(walk.crop((64, 192, 128, 256)), (1 * 64, 192))
canvas.paste(atk.crop((4 * 64, 192, 5 * 64, 256)), (2 * 64, 192))
canvas.paste(atk.crop((5 * 64, 192, 6 * 64, 256)), (3 * 64, 192))
canvas.paste(hurt.crop((64, 192, 128, 256)), (4 * 64, 192))
canvas.paste(dead.crop((5 * 64, 192, 6 * 64, 256)), (5 * 64, 192))

preview_3x = canvas.resize((canvas.width * 3, canvas.height * 3), Image.Resampling.NEAREST)
preview_path = "Assets/ART/Player4/viking_true_4dir_actions_preview.png"
preview_3x.save(preview_path)

artifact_dir = "C:/Users/TRUONG HUNG/.gemini/antigravity-ide/brain/953cb4d4-3543-43c3-8946-55fea43b90d1"
shutil.copy(preview_path, os.path.join(artifact_dir, "viking_true_4dir_actions_preview.png"))
print("Saved viking_true_4dir_actions_preview.png successfully!")
