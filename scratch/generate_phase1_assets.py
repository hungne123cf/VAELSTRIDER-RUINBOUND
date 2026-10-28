import os
import math
import uuid
from PIL import Image, ImageDraw

OUT_DIR = r"d:\Unity\VAELSTRIDER-RUINBOUND\Assets\Sprites\UI\Soulslike"
RES_DIR = r"d:\Unity\VAELSTRIDER-RUINBOUND\Assets\Resources\UI\Soulslike"
os.makedirs(OUT_DIR, exist_ok=True)
os.makedirs(RES_DIR, exist_ok=True)

def generate_meta(guid, border_x=0, border_y=0, border_z=0, border_w=0):
    template = """fileFormatVersion: 2
guid: __GUID__
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 1
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    wrapU: 1
    wrapV: 1
    wrapW: 1
  spriteMode: 1
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 100
  spriteBorder: {x: __BX__, y: __BY__, z: __BZ__, w: __BW__}
  alphaUsage: 1
  alphaIsTransparency: 1
  textureType: 8
"""
    return template.replace("__GUID__", str(guid))\
                   .replace("__BX__", str(border_x))\
                   .replace("__BY__", str(border_y))\
                   .replace("__BZ__", str(border_z))\
                   .replace("__BW__", str(border_w))

def save_asset(name, img, border=(0, 0, 0, 0)):
    for directory in [OUT_DIR, RES_DIR]:
        png_path = os.path.join(directory, name + ".png")
        meta_path = png_path + ".meta"
        img.save(png_path)
        
        guid = uuid.uuid4().hex
        if os.path.exists(meta_path):
            with open(meta_path, "r", encoding="utf-8") as f:
                for line in f:
                    if line.startswith("guid:"):
                        guid = line.split("guid:")[1].strip()
                        break
        with open(meta_path, "w", encoding="utf-8") as f:
            f.write(generate_meta(guid, border[0], border[1], border[2], border[3]))
    print(f"Saved {name}.png to both folders.")

# 1. Dark Fantasy Panel Background (64x64, 9-slice: 12, 12, 12, 12)
# For all modal frames, center box, inventory cards
def make_panel_bg():
    w, h = 64, 64
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    # Fill dark obsidian stone center
    for y in range(h):
        for x in range(w):
            # Subtle stone noise / radial vignette
            dx = (x - 31.5) / 32.0
            dy = (y - 31.5) / 32.0
            dist = math.sqrt(dx*dx + dy*dy)
            val = int(14 - dist * 4)
            img.putpixel((x, y), (val, int(val * 1.1), int(val * 1.3), 245))
            
    # Outer dark border
    draw.rectangle([0, 0, w - 1, h - 1], outline=(10, 8, 6, 255))
    
    # Double Antique Bronze Filigree Border
    # Outer bronze line
    draw.rectangle([1, 1, w - 2, h - 2], outline=(120, 95, 55, 255))
    draw.rectangle([2, 2, w - 3, h - 3], outline=(190, 155, 90, 255)) # Gold highlight
    draw.rectangle([3, 3, w - 4, h - 4], outline=(100, 80, 45, 255))
    
    # Inner dark trough line
    draw.rectangle([4, 4, w - 5, h - 5], outline=(20, 16, 12, 255))
    
    # Thin inner gold pinstripe
    draw.rectangle([6, 6, w - 7, h - 7], outline=(145, 115, 65, 180))
    
    # Corner rosettes / runic accents (in 12x12 corner zones)
    corners = [(2, 2), (w - 7, 2), (2, h - 7), (w - 7, h - 7)]
    for cx, cy in corners:
        draw.rectangle([cx, cy, cx + 4, cy + 4], fill=(225, 185, 105, 255), outline=(90, 70, 35, 255))
        draw.point((cx + 2, cy + 2), fill=(255, 235, 175, 255))
        
    save_asset("Dark_Fantasy_Panel_Bg", img, (12, 12, 12, 12))

# 2. Soulslike Button Stone (64x32, 9-slice: 8, 8, 8, 8)
# For all clickable interactive buttons (Respawn, Restart, MainMenu, Shop, Play)
def make_stone_button():
    w, h = 64, 32
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    for y in range(h):
        # Vertical gradient (lighter on top, dark at bottom)
        v = (h - 1 - y) / (h - 1.0)
        r = int(28 + v * 14)
        g = int(26 + v * 12)
        b = int(28 + v * 12)
        for x in range(w):
            img.putpixel((x, y), (r, g, b, 255))
            
    # Outer dark outline
    draw.rectangle([0, 0, w - 1, h - 1], outline=(12, 10, 8, 255))
    
    # Antique bronze border with top bevel
    draw.line([(1, 1), (w - 2, 1)], fill=(215, 175, 100, 255)) # Top highlight gold
    draw.line([(1, 1), (1, h - 2)], fill=(160, 130, 75, 255))  # Left
    draw.line([(w - 2, 1), (w - 2, h - 2)], fill=(130, 105, 60, 255)) # Right
    draw.line([(1, h - 2), (w - 2, h - 2)], fill=(75, 60, 35, 255))   # Bottom shadow
    
    # Second inner border
    draw.rectangle([2, 2, w - 3, h - 3], outline=(20, 18, 16, 255))
    draw.rectangle([3, 3, w - 4, h - 4], outline=(100, 80, 45, 140))
    
    # Corner rivets
    for cx, cy in [(3, 3), (w - 5, 3), (3, h - 5), (w - 5, h - 5)]:
        draw.point((cx, cy), fill=(245, 205, 125, 255))
        draw.point((cx + 1, cy), fill=(160, 125, 65, 255))
        
    save_asset("Soulslike_Button_Stone", img, (8, 8, 8, 8))

# 3. Action Button Runic Ring (128x128 circular ring)
# For MobileActionButton (Attack, Dash, Skill)
def make_action_button_ring():
    size = 128
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    center = 63.5
    outer_r = 62.0
    inner_r = 46.0
    
    for y in range(size):
        for x in range(size):
            dx = x - center
            dy = y - center
            dist = math.sqrt(dx*dx + dy*dy)
            
            if dist <= outer_r and dist >= inner_r:
                # Ring region
                t = (dist - inner_r) / (outer_r - inner_r) # 0 at inner, 1 at outer
                angle = math.atan2(dy, dx)
                
                # Lighting direction from top-left (angle around -2.35 rad)
                light = math.cos(angle - (-2.35)) * 0.25
                
                # Base bronze color
                if t < 0.12 or t > 0.88:
                    # Dark borders of the ring
                    r, g, b = int(40 + light * 20), int(30 + light * 15), int(20 + light * 10)
                elif t >= 0.40 and t <= 0.60:
                    # Central raised filigree ridge with gold highlight
                    r = int(220 + light * 35)
                    g = int(180 + light * 30)
                    b = int(105 + light * 25)
                else:
                    # Bronze body
                    r = int(155 + light * 30)
                    g = int(125 + light * 25)
                    b = int(75 + light * 20)
                    
                # Cardinal notches (at 0, 90, 180, 270 deg)
                is_notch = (abs(dx) < 3.0 and dist > inner_r + 2) or (abs(dy) < 3.0 and dist > inner_r + 2)
                if is_notch:
                    r = int(r * 0.6)
                    g = int(g * 0.6)
                    b = int(b * 0.6)
                    
                # Anti-aliasing edges
                alpha = 255
                if dist > outer_r - 1.0:
                    alpha = int(255 * (outer_r - dist))
                elif dist < inner_r + 1.0:
                    alpha = int(255 * (dist - inner_r))
                    
                img.putpixel((x, y), (max(0, min(255, r)), max(0, min(255, g)), max(0, min(255, b)), max(0, min(255, alpha))))
                
    save_asset("Action_Button_Runic_Ring", img, (0, 0, 0, 0))

# 4. Action Button Obsidian Core (128x128 circular disc)
def make_action_button_core():
    size = 128
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    center = 63.5
    radius = 48.0 # slightly overlaps ring inner edge
    
    for y in range(size):
        for x in range(size):
            dx = x - center
            dy = y - center
            dist = math.sqrt(dx*dx + dy*dy)
            
            if dist <= radius:
                # Radial gradient: darker at center, subtle stone rim
                norm = dist / radius
                r = int(14 + norm * 12)
                g = int(16 + norm * 14)
                b = int(22 + norm * 18)
                
                alpha = 255
                if dist > radius - 1.2:
                    alpha = int(255 * (radius - dist) / 1.2)
                img.putpixel((x, y), (r, g, b, max(0, min(255, alpha))))
                
    save_asset("Action_Button_Obsidian_Core", img, (0, 0, 0, 0))

# 5. Boss Bar Trough Frame (128x32, 9-slice: 16, 8, 16, 8)
# Wide ornate frame for BossHealthBarUI
def make_boss_bar_frame():
    w, h = 128, 32
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    # Fill deep obsidian interior
    draw.rectangle([0, 0, w - 1, h - 1], fill=(12, 14, 18, 250))
    
    # Outer dark outline
    draw.rectangle([0, 0, w - 1, h - 1], outline=(8, 6, 4, 255))
    
    # Antique bronze filigree top and bottom rails
    draw.line([(0, 1), (w - 1, 1)], fill=(195, 160, 90, 255)) # Top gold rail
    draw.line([(0, 2), (w - 1, 2)], fill=(125, 100, 55, 255))
    draw.line([(0, h - 2), (w - 1, h - 2)], fill=(125, 100, 55, 255))
    draw.line([(0, h - 1), (w - 1, h - 1)], fill=(75, 60, 30, 255))
    
    # Inner trough line
    draw.rectangle([3, 3, w - 4, h - 4], outline=(22, 18, 14, 255))
    
    # Left and Right Gothic Endcap Ornaments (within 16px border)
    for side in [0, 1]:
        base_x = 2 if side == 0 else w - 15
        # Diamond finial
        draw.polygon([(base_x + 6, 6), (base_x + 10, 15), (base_x + 6, 25), (base_x + 2, 15)], fill=(185, 150, 85, 255), outline=(60, 45, 25, 255))
        draw.point((base_x + 6, 15), fill=(255, 225, 150, 255)) # Center spark
        
    save_asset("Boss_Bar_Frame", img, (16, 8, 16, 8))

# 6. Gothic Filigree Divider (256x16)
def make_gothic_divider():
    w, h = 256, 16
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    mid_x = w // 2
    mid_y = h // 2
    
    # Central Diamond (8x8)
    draw.polygon([(mid_x, mid_y - 6), (mid_x + 6, mid_y), (mid_x, mid_y + 6), (mid_x - 6, mid_y)], fill=(235, 195, 115, 255), outline=(95, 75, 40, 255))
    draw.point((mid_x, mid_y), fill=(255, 240, 180, 255))
    
    # Flanking smaller diamonds
    for dx in [-14, 14]:
        draw.polygon([(mid_x + dx, mid_y - 3), (mid_x + dx + 3, mid_y), (mid_x + dx, mid_y + 3), (mid_x + dx - 3, mid_y)], fill=(195, 160, 90, 255))
        
    # Tapered horizontal filigree lines extending left and right
    for x in range(w):
        dist_from_center = abs(x - mid_x)
        if dist_from_center > 18:
            t = (dist_from_center - 18) / float(mid_x - 18) # 0 to 1
            if t <= 1.0:
                alpha = int(220 * (1.0 - t * t))
                r = int(210 * (1.0 - t * 0.3))
                g = int(170 * (1.0 - t * 0.3))
                b = int(95 * (1.0 - t * 0.3))
                img.putpixel((x, mid_y), (r, g, b, alpha))
                if t < 0.6:
                    img.putpixel((x, mid_y - 1), (int(r * 0.6), int(g * 0.6), int(b * 0.6), int(alpha * 0.7)))
                    img.putpixel((x, mid_y + 1), (int(r * 0.4), int(g * 0.4), int(b * 0.4), int(alpha * 0.7)))
                    
    save_asset("Gothic_Filigree_Divider", img, (0, 0, 0, 0))

# 7. Potion Slot Runic Frame (64x64, 9-slice: 10, 10, 10, 10)
def make_potion_slot_frame():
    w, h = 64, 64
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    # Dark obsidian interior
    draw.rectangle([0, 0, w - 1, h - 1], fill=(12, 15, 20, 250))
    
    # Outer dark border
    draw.rectangle([0, 0, w - 1, h - 1], outline=(8, 6, 4, 255))
    
    # Bronze outer frame
    draw.rectangle([1, 1, w - 2, h - 2], outline=(150, 120, 70, 255))
    draw.rectangle([2, 2, w - 3, h - 3], outline=(205, 170, 100, 255)) # Gold edge
    draw.rectangle([3, 3, w - 4, h - 4], outline=(100, 80, 45, 255))
    
    # Inner dark trough
    draw.rectangle([4, 4, w - 5, h - 5], outline=(18, 14, 10, 255))
    
    # Corner Claws / Brackets
    for cx, cy in [(1, 1), (w - 7, 1), (1, h - 7), (w - 7, h - 7)]:
        draw.rectangle([cx, cy, cx + 5, cy + 5], fill=(225, 185, 110, 255), outline=(75, 55, 30, 255))
        draw.point((cx + 2, cy + 2), fill=(255, 240, 180, 255))
        
    save_asset("Potion_Slot_Runic_Frame", img, (10, 10, 10, 10))

if __name__ == "__main__":
    make_panel_bg()
    make_stone_button()
    make_action_button_ring()
    make_action_button_core()
    make_boss_bar_frame()
    make_gothic_divider()
    make_potion_slot_frame()
    print("All Phase 1 assets generated successfully!")
