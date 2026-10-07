import os
import shutil
from PIL import Image, ImageDraw

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
                lum = (r * 0.299 + g * 0.587 + b * 0.114) / 255.0
                if lum > 0.6:
                    out_pix[x, y] = C_STL_SPEC
                elif lum > 0.35:
                    out_pix[x, y] = C_STL_HI
                else:
                    out_pix[x, y] = C_STL_MID
            elif is_skin and cy in range(18, 25):
                out_pix[x, y] = C_BRD_MID
            elif is_blue:
                lum = (r * 0.299 + g * 0.587 + b * 0.114) / 255.0
                if cy in range(17, 24):
                    if lum > 0.6:
                        out_pix[x, y] = C_FUR_HI
                    elif lum > 0.35:
                        out_pix[x, y] = C_FUR_MID
                    else:
                        out_pix[x, y] = C_FUR_SHD
                elif cy in range(35, 41):
                    if lum > 0.5:
                        out_pix[x, y] = C_RED_HI
                    elif lum > 0.3:
                        out_pix[x, y] = C_RED_MID
                    else:
                        out_pix[x, y] = C_RED_SHD
                else:
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
    for r in range(rows):
        for c in range(cols):
            x0 = c * 64
            y0 = r * 64
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

def get_frame_body_metrics(pix, x0, y0):
    """Finds exact Head Y (top of body) and Center X of the character in a 64x64 cell."""
    xs = [x for x in range(x0, x0 + 64) for y in range(y0, y0 + 64) if pix[x, y][3] > 40]
    ys = [y for x in range(x0, x0 + 64) for y in range(y0, y0 + 64) if pix[x, y][3] > 40]
    if not xs or not ys:
        return y0 + 21, x0 + 30
    min_y = min(ys)
    center_x = (min(xs) + max(xs)) // 2
    return min_y, center_x

def draw_straight_shaft(draw, x, y0, y1):
    for y in range(y0, y1 + 1):
        draw.point((x, y), fill=C_WOD_MID)
        draw.point((x + 1, y), fill=C_WOD_SHD)
        if y % 4 == 0:
            draw.point((x, y), fill=C_GLD_MID)
    draw.rectangle([x - 1, y1, x + 2, y1 + 1], fill=C_OUT)
    draw.rectangle([x, y1, x + 1, y1], fill=C_GLD_HI)

def draw_hammer_head(draw, cx, cy):
    draw.rectangle([cx - 5, cy - 3, cx + 5, cy + 3], fill=C_OUT)
    draw.rectangle([cx - 4, cy - 2, cx + 4, cy + 2], fill=C_STL_MID)
    draw.line([(cx - 4, cy - 2), (cx + 4, cy - 2)], fill=C_STL_HI)
    draw.line([(cx - 4, cy - 2), (cx - 4, cy + 2)], fill=C_STL_HI)
    draw.point((cx - 4, cy - 2), fill=C_STL_SPEC)
    draw.line([(cx - 4, cy + 2), (cx + 4, cy + 2)], fill=C_STL_SHD)
    draw.line([(cx + 4, cy - 2), (cx + 4, cy + 2)], fill=C_STL_SHD)
    draw.rectangle([cx - 1, cy - 3, cx + 1, cy + 3], fill=C_GLD_MID)
    draw.point((cx, cy - 3), fill=C_GLD_SPEC)
    draw.point((cx, cy), fill=C_RBY_HI)

def draw_hands_grip(draw, x, y):
    draw.rectangle([x - 1, y - 1, x + 2, y + 1], fill=C_STL_MID)
    draw.line([(x - 1, y - 1), (x + 2, y - 1)], fill=C_STL_HI)
    draw.point((x + 1, y - 1), fill=C_GLD_HI)

# 1. GENERATE IDLE (Dynamic per-frame body tracking)
def generate_idle():
    clean = load_clean_clothed_sheet("Sword_Idle.png", "Sword_Idle")
    titan = transform_to_viking_berserker(clean)
    draw = ImageDraw.Draw(titan)
    pix = titan.load()
    
    for r in range(4):
        for c in range(12):
            x0 = c * 64
            y0 = r * 64
            head_y, center_x = get_frame_body_metrics(pix, x0, y0)
            
            if r == 0: # Down
                hx = center_x + 9
                draw_straight_shaft(draw, hx, head_y + 1, head_y + 24)
                draw_hammer_head(draw, hx, head_y - 2)
                draw_hands_grip(draw, hx - 1, head_y + 12)
            elif r == 1: # Left (Side view)
                hx = center_x - 3
                draw_straight_shaft(draw, hx, head_y - 1, head_y + 23)
                draw_hammer_head(draw, hx, head_y - 4)
                draw_hands_grip(draw, hx, head_y + 12)
            elif r == 2: # Up-Left
                hx = center_x - 4
                draw_straight_shaft(draw, hx, head_y - 1, head_y + 23)
                draw_hammer_head(draw, hx, head_y - 4)
                draw_hands_grip(draw, hx, head_y + 12)
            elif r == 3: # Up
                hx = center_x - 5
                draw_straight_shaft(draw, hx, head_y - 1, head_y + 24)
                draw_hammer_head(draw, hx, head_y - 4)
                
    titan.save(os.path.join(p4_dir, "Hammer_Idle.png"))
    print("Generated dynamic Hammer_Idle.png")

# 2. GENERATE WALK (Dynamic per-frame body stride tracking)
def generate_walk():
    clean = load_clean_clothed_sheet("Sword_Walk.png", "Sword_Walk")
    titan = transform_to_viking_berserker(clean)
    draw = ImageDraw.Draw(titan)
    pix = titan.load()
    
    for r in range(4):
        for c in range(6):
            x0 = c * 64
            y0 = r * 64
            head_y, center_x = get_frame_body_metrics(pix, x0, y0)
            
            if r == 0: # Down
                hx = center_x + 9
                draw_straight_shaft(draw, hx, head_y + 1, head_y + 24)
                draw_hammer_head(draw, hx, head_y - 2)
                draw_hands_grip(draw, hx - 1, head_y + 12)
            elif r == 1: # Left
                hx = center_x - 3
                draw_straight_shaft(draw, hx, head_y - 1, head_y + 23)
                draw_hammer_head(draw, hx, head_y - 4)
                draw_hands_grip(draw, hx, head_y + 12)
            elif r == 2: # Up-Left
                hx = center_x - 4
                draw_straight_shaft(draw, hx, head_y - 1, head_y + 23)
                draw_hammer_head(draw, hx, head_y - 4)
            elif r == 3: # Up
                hx = center_x - 5
                draw_straight_shaft(draw, hx, head_y - 1, head_y + 24)
                draw_hammer_head(draw, hx, head_y - 4)
                
    titan.save(os.path.join(p4_dir, "Hammer_Walk.png"))
    print("Generated dynamic Hammer_Walk.png")

# 3. GENERATE RUN (Dynamic per-frame sprint leap & dip tracking)
def generate_run():
    clean = load_clean_clothed_sheet("Sword_Run.png", "Sword_Run")
    titan = transform_to_viking_berserker(clean)
    draw = ImageDraw.Draw(titan)
    pix = titan.load()
    
    for r in range(4):
        for c in range(8):
            x0 = c * 64
            y0 = r * 64
            head_y, center_x = get_frame_body_metrics(pix, x0, y0)
            
            if r == 0: # Down Run
                hx = center_x + 9
                draw_straight_shaft(draw, hx, head_y + 1, head_y + 24)
                draw_hammer_head(draw, hx, head_y - 2)
                draw_hands_grip(draw, hx - 1, head_y + 12)
            elif r == 1: # Left Run (Locks tightly to hand as body dips 4px and leaps 2px!)
                hx = center_x - 3
                draw_straight_shaft(draw, hx, head_y - 1, head_y + 23)
                draw_hammer_head(draw, hx, head_y - 4)
                draw_hands_grip(draw, hx, head_y + 12)
            elif r == 2: # Up-Left Run
                hx = center_x - 4
                draw_straight_shaft(draw, hx, head_y - 1, head_y + 23)
                draw_hammer_head(draw, hx, head_y - 4)
            elif r == 3: # Up Run
                hx = center_x - 5
                draw_straight_shaft(draw, hx, head_y - 1, head_y + 24)
                draw_hammer_head(draw, hx, head_y - 4)
                
    titan.save(os.path.join(p4_dir, "Hammer_Run.png"))
    print("Generated dynamic Hammer_Run.png with locked stride tracking!")

# 4. GENERATE ATTACK & SKILL
def generate_attack():
    clean = load_clean_clothed_sheet("Sword_Attack.png", "Sword_Attack")
    titan = transform_to_viking_berserker(clean)
    draw = ImageDraw.Draw(titan)
    pix = titan.load()
    
    for r in range(4):
        for c in range(8):
            x0 = c * 64
            y0 = r * 64
            head_y, center_x = get_frame_body_metrics(pix, x0, y0)
            
            if r in (1, 2): # Side Attack
                if c == 0:
                    draw_straight_shaft(draw, center_x - 2, head_y, head_y + 20)
                    draw_hammer_head(draw, center_x - 2, head_y - 4)
                    draw_hands_grip(draw, center_x - 2, head_y + 12)
                elif c == 1:
                    draw_straight_shaft(draw, center_x + 1, head_y - 5, head_y + 15)
                    draw_hammer_head(draw, center_x + 1, head_y - 9)
                    draw_hands_grip(draw, center_x + 1, head_y + 8)
                elif c in (2, 3):
                    # Overhead Apex
                    draw_straight_shaft(draw, center_x + 3, head_y - 8, head_y + 12)
                    draw_hammer_head(draw, center_x + 3, head_y - 12)
                    draw_hands_grip(draw, center_x + 3, head_y + 5)
                    draw.point((center_x - 2, head_y - 14), fill=C_GLD_HI)
                    draw.point((center_x + 8, head_y - 14), fill=C_GLD_HI)
                elif c == 4:
                    # Strike Arc
                    draw_straight_shaft(draw, center_x - 5, head_y + 6, head_y + 20)
                    draw_hammer_head(draw, center_x - 5, head_y + 2)
                    draw_hands_grip(draw, center_x - 5, head_y + 14)
                elif c in (5, 6):
                    # Ground Smash
                    draw_straight_shaft(draw, center_x - 10, head_y + 13, head_y + 25)
                    draw_hammer_head(draw, center_x - 10, head_y + 23)
                    draw_hands_grip(draw, center_x - 10, head_y + 15)
                    draw.line([x0 + 8, y0 + 49, x0 + 32, y0 + 49], fill=C_GLD_HI)
                    draw.point((x0 + 12, y0 + 46), fill=C_RBY_HI)
                    draw.point((x0 + 28, y0 + 46), fill=C_RBY_HI)
                elif c == 7:
                    draw_straight_shaft(draw, center_x - 6, head_y + 3, head_y + 21)
                    draw_hammer_head(draw, center_x - 6, head_y - 1)
                    draw_hands_grip(draw, center_x - 6, head_y + 13)
            elif r == 0: # Down Attack
                if c in (0, 1):
                    draw_straight_shaft(draw, center_x + 4, head_y - 4, head_y + 16)
                    draw_hammer_head(draw, center_x + 4, head_y - 8)
                    draw_hands_grip(draw, center_x + 4, head_y + 8)
                elif c in (2, 3):
                    draw_straight_shaft(draw, center_x + 4, head_y - 8, head_y + 12)
                    draw_hammer_head(draw, center_x + 4, head_y - 12)
                    draw_hands_grip(draw, center_x + 4, head_y + 4)
                    draw.point((center_x, head_y - 14), fill=C_GLD_HI)
                    draw.point((center_x + 8, head_y - 14), fill=C_GLD_HI)
                elif c in (4, 5, 6):
                    draw_straight_shaft(draw, center_x + 4, head_y + 12, head_y + 26)
                    draw_hammer_head(draw, center_x + 4, head_y + 24)
                    draw_hands_grip(draw, center_x + 4, head_y + 14)
                    draw.line([x0 + 18, y0 + 49, x0 + 46, y0 + 49], fill=C_GLD_HI)
                    draw.point((x0 + 22, y0 + 46), fill=C_RBY_HI)
                    draw.point((x0 + 42, y0 + 46), fill=C_RBY_HI)
                else:
                    draw_straight_shaft(draw, center_x + 8, head_y + 3, head_y + 23)
                    draw_hammer_head(draw, center_x + 8, head_y - 1)
                    draw_hands_grip(draw, center_x + 8, head_y + 13)
            elif r == 3: # Up Attack
                if c in (0, 1, 2, 3):
                    draw_straight_shaft(draw, center_x + 1, head_y - 6, head_y + 14)
                    draw_hammer_head(draw, center_x + 1, head_y - 10)
                else:
                    draw_straight_shaft(draw, center_x + 1, head_y + 4, head_y + 22)
                    draw_hammer_head(draw, center_x + 1, head_y)
                    
    titan.save(os.path.join(p4_dir, "Hammer_Attack.png"))
    
    # Area Slam Skill
    skill = titan.copy()
    d_sk = ImageDraw.Draw(skill)
    for r in range(4):
        for c in range(8):
            x0 = c * 64
            y0 = r * 64
            if c in (4, 5, 6):
                d_sk.ellipse([x0 + 6, y0 + 38, x0 + 58, y0 + 56], outline=C_GLD_HI)
                d_sk.point((x0 + 12, y0 + 40), fill=C_RBY_HI)
                d_sk.point((x0 + 52, y0 + 40), fill=C_RBY_HI)
                d_sk.point((x0 + 32, y0 + 54), fill=C_GLD_SPEC)
    skill.save(os.path.join(p4_dir, "Hammer_Skill.png"))
    print("Generated dynamic Hammer_Attack.png & Hammer_Skill.png")

# 5. GENERATE HURT & DEATH
def generate_hurt_death():
    clean_hurt = load_clean_clothed_sheet("Sword_Hurt.png", "Sword_Hurt")
    titan_hurt = transform_to_viking_berserker(clean_hurt)
    titan_hurt.save(os.path.join(p4_dir, "Hammer_Hurt.png"))
    
    clean_death = load_clean_clothed_sheet("Sword_Death.png", "Sword_Death")
    titan_death = transform_to_viking_berserker(clean_death)
    titan_death.save(os.path.join(p4_dir, "Hammer_Death.png"))
    print("Generated dynamic Hammer_Hurt.png & Hammer_Death.png")

# 6. GENERATE PORTRAIT
def generate_portrait():
    idle = Image.open(os.path.join(p4_dir, "Hammer_Idle.png"))
    f0 = idle.crop((0, 0, 64, 64))
    portrait = f0.resize((128, 128), Image.Resampling.NEAREST)
    portrait.save(os.path.join(p4_ui_dir, "Hero_Player4_Portrait.png"))
    print("Generated Hero_Player4_Portrait.png")

generate_idle()
generate_walk()
generate_run()
generate_attack()
generate_hurt_death()
generate_portrait()

# Build 4x Zoom Comparison Preview
run = Image.open(os.path.join(p4_dir, "Hammer_Run.png"))
canvas = Image.new("RGBA", (64 * 8, 64 * 4), (20, 22, 28, 255))
for r in range(4):
    for c in range(8):
        f = run.crop((c * 64, r * 64, (c + 1) * 64, (r + 1) * 64))
        canvas.paste(f, (c * 64, r * 64))

preview_2x = canvas.resize((canvas.width * 2, canvas.height * 2), Image.Resampling.NEAREST)
preview_path = "Assets/ART/Player4/dynamic_run_stride_preview.png"
preview_2x.save(preview_path)

artifact_dir = "C:/Users/TRUONG HUNG/.gemini/antigravity-ide/brain/953cb4d4-3543-43c3-8946-55fea43b90d1"
shutil.copy(preview_path, os.path.join(artifact_dir, "dynamic_run_stride_preview.png"))
print("Saved dynamic_run_stride_preview.png successfully!")
