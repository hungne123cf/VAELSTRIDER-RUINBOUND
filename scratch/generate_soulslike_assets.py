import os
import math
import uuid
from PIL import Image, ImageDraw

OUT_DIR = r"d:\Unity\VAELSTRIDER-RUINBOUND\Assets\Sprites\UI\Soulslike"
os.makedirs(OUT_DIR, exist_ok=True)

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

def save_with_meta(name, img, border=(0, 0, 0, 0)):
    png_path = os.path.join(OUT_DIR, name + ".png")
    meta_path = png_path + ".meta"
    img.save(png_path)
    
    # Keep guid if meta already exists
    guid = uuid.uuid4().hex
    if os.path.exists(meta_path):
        with open(meta_path, "r", encoding="utf-8") as f:
            for line in f:
                if line.startswith("guid:"):
                    guid = line.split("guid:")[1].strip()
                    break
    with open(meta_path, "w", encoding="utf-8") as f:
        f.write(generate_meta(guid, border[0], border[1], border[2], border[3]))
    print(f"Saved {name}.png with guid {guid}")

# 1. Ruby Fill (32x32)
def make_ruby_fill():
    w, h = 32, 32
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    for y in range(h):
        # In PIL, y=0 is top, y=h-1 is bottom
        v = (h - 1 - y) / (h - 1.0) # 1 at top, 0 at bottom
        if v > 0.90:
            # Specular highlight
            t = (v - 0.90) / 0.10
            r = int(242 + (255 - 242) * t)
            g = int(70 + (170 - 70) * t)
            b = int(90 + (185 - 90) * t)
        elif v > 0.45:
            # Ruby body
            t = (v - 0.45) / 0.45
            r = int(185 + (235 - 185) * t)
            g = int(20 + (45 - 20) * t)
            b = int(35 + (60 - 35) * t)
        elif v > 0.10:
            # Deep crimson body
            t = (v - 0.10) / 0.35
            r = int(95 + (185 - 95) * t)
            g = int(10 + (20 - 10) * t)
            b = int(20 + (35 - 20) * t)
        else:
            # Dark shadowed bottom
            r, g, b = 50, 5, 10

        for x in range(w):
            alpha = 255
            # soft corners
            if (x == 0 or x == w - 1) and (y == 0 or y == h - 1):
                alpha = 140
            img.putpixel((x, y), (r, g, b, alpha))
    save_with_meta("Bar_Ruby_Fill", img, (4, 4, 4, 4))

# 2. Sapphire Fill (32x32)
def make_sapphire_fill():
    w, h = 32, 32
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    for y in range(h):
        v = (h - 1 - y) / (h - 1.0)
        if v > 0.88:
            t = (v - 0.88) / 0.12
            r = int(90 + (215 - 90) * t)
            g = int(200 + (245 - 200) * t)
            b = int(255)
        elif v > 0.40:
            t = (v - 0.40) / 0.48
            r = int(25 + (55 - 25) * t)
            g = int(110 + (165 - 110) * t)
            b = int(190 + (245 - 190) * t)
        else:
            t = v / 0.40
            r = int(10 + (25 - 10) * t)
            g = int(40 + (110 - 40) * t)
            b = int(90 + (190 - 90) * t)

        for x in range(w):
            alpha = 255
            if (x == 0 or x == w - 1) and (y == 0 or y == h - 1):
                alpha = 140
            img.putpixel((x, y), (r, g, b, alpha))
    save_with_meta("Bar_Sapphire_Fill", img, (4, 4, 4, 4))

# 3. Amber Fill (32x32)
def make_amber_fill():
    w, h = 32, 32
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    for y in range(h):
        v = (h - 1 - y) / (h - 1.0)
        if v > 0.88:
            t = (v - 0.88) / 0.12
            r = int(250 + (255 - 250) * t)
            g = int(190 + (242 - 190) * t)
            b = int(50 + (150 - 50) * t)
        elif v > 0.40:
            t = (v - 0.40) / 0.48
            r = int(180 + (235 - 180) * t)
            g = int(105 + (160 - 105) * t)
            b = int(12 + (25 - 12) * t)
        else:
            t = v / 0.40
            r = int(75 + (180 - 75) * t)
            g = int(40 + (105 - 40) * t)
            b = int(5 + (12 - 5) * t)

        for x in range(w):
            alpha = 255
            if (x == 0 or x == w - 1) and (y == 0 or y == h - 1):
                alpha = 140
            img.putpixel((x, y), (r, g, b, alpha))
    save_with_meta("Bar_Amber_Fill", img, (4, 4, 4, 4))

# 4. Amethyst Fill (32x16)
def make_amethyst_fill():
    w, h = 32, 16
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    for y in range(h):
        v = (h - 1 - y) / (h - 1.0)
        r = int(90 + (175 - 90) * v)
        g = int(65 + (140 - 65) * v)
        b = int(165 + (245 - 165) * v)
        if v > 0.80:
            t = (v - 0.80) / 0.20
            r = int(r + (235 - r) * t)
            g = int(g + (225 - g) * t)
            b = int(b + (255 - b) * t)

        for x in range(w):
            img.putpixel((x, y), (r, g, b, 255))
    save_with_meta("Bar_Amethyst_Fill", img, (4, 2, 4, 2))

# 5. Obsidian Trough Background (32x32)
def make_obsidian_trough():
    w, h = 32, 32
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    bronze_top = (185, 142, 72, 245)
    bronze_bot = (96, 70, 35, 235)
    obsidian_base = (15, 18, 26, 245)

    for y in range(h):
        # y=0 is top in PIL
        if y == 0:
            row = bronze_top
        elif y == 1:
            row = (int(bronze_top[0]*0.5 + obsidian_base[0]*0.5), int(bronze_top[1]*0.5 + obsidian_base[1]*0.5), int(bronze_top[2]*0.5 + obsidian_base[2]*0.5), 245)
        elif y == h - 1:
            row = bronze_bot
        elif y == h - 2:
            row = (int(bronze_bot[0]*0.5 + obsidian_base[0]*0.5), int(bronze_bot[1]*0.5 + obsidian_base[1]*0.5), int(bronze_bot[2]*0.5 + obsidian_base[2]*0.5), 245)
        else:
            t = y / (h - 1.0)
            mid = math.sin(t * math.pi)
            r = int(10 + 6 * mid)
            g = int(12 + 7 * mid)
            b = int(18 + 9 * mid)
            row = (r, g, b, 245)

        for x in range(w):
            c = row
            if x == 0 or x == w - 1:
                c = (int(c[0]*0.4 + bronze_top[0]*0.6), int(c[1]*0.4 + bronze_top[1]*0.6), int(c[2]*0.4 + bronze_top[2]*0.6), 245)
            img.putpixel((x, y), c)
    save_with_meta("Bar_Obsidian_Trough", img, (4, 4, 4, 4))

# 6. Tarnished Bronze Dual Bezel for Avatar (128x128)
def make_bronze_bezel():
    size = 128
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    center = (size - 1) * 0.5
    outer_r = center - 0.5       # ~63.0
    groove_r = outer_r - 4.5     # ~58.5
    inner_r = groove_r - 5.5     # ~53.0

    gold_light = (225, 184, 98)
    gold_dark = (122, 88, 40)
    groove_col = (30, 22, 12)

    for y in range(size):
        for x in range(size):
            dx = x - center
            dy = y - center
            dist = math.sqrt(dx*dx + dy*dy)
            if dist > outer_r + 0.5 or dist < inner_r - 0.5:
                continue

            angle = math.atan2(dy, dx)
            # light from top-left (10 o'clock)
            light_factor = (math.sin(angle - 2.2) + 1.0) * 0.5

            r_base = int(gold_dark[0] + (gold_light[0] - gold_dark[0]) * light_factor)
            g_base = int(gold_dark[1] + (gold_light[1] - gold_dark[1]) * light_factor)
            b_base = int(gold_dark[2] + (gold_light[2] - gold_dark[2]) * light_factor)

            if abs(dist - groove_r) < 1.3:
                r, g, b = groove_col
            elif dist > groove_r:
                # outer rim
                ed = min(outer_r - dist, dist - groove_r)
                eb = max(0.0, min(1.0, ed + 0.5))
                r = int(r_base * (0.7 + 0.3 * eb))
                g = int(g_base * (0.7 + 0.3 * eb))
                b = int(b_base * (0.7 + 0.3 * eb))
            else:
                # inner rim
                ed = min(groove_r - dist, dist - inner_r)
                eb = max(0.0, min(1.0, ed + 0.5))
                r = min(255, int(r_base * (0.8 + 0.3 * eb)))
                g = min(255, int(g_base * (0.8 + 0.3 * eb)))
                b = min(255, int(b_base * (0.8 + 0.3 * eb)))

            alpha_out = max(0.0, min(1.0, outer_r - dist + 0.5))
            alpha_in = max(0.0, min(1.0, dist - inner_r + 0.5))
            final_alpha = int(min(alpha_out, alpha_in) * 255)

            if final_alpha > 0:
                img.putpixel((x, y), (r, g, b, final_alpha))
    save_with_meta("Avatar_Bronze_Bezel", img, (0, 0, 0, 0))

# 7. Runic Stone Plaque for Shards (64x32)
def make_runic_plaque():
    w, h = 64, 32
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    stone = (16, 20, 30, 245)
    bronze = (175, 134, 65, 245)
    groove = (6, 8, 12, 250)

    for y in range(h):
        for x in range(w):
            is_extreme = (x == 0 or x == w - 1) and (y == 0 or y == h - 1)
            if is_extreme:
                continue

            is_border = (x <= 1 or x >= w - 2 or y <= 1 or y >= h - 2)
            is_groove = (x == 2 or x == w - 3 or y == 2 or y == h - 3)

            if is_border:
                img.putpixel((x, y), bronze)
            elif is_groove:
                img.putpixel((x, y), groove)
            else:
                img.putpixel((x, y), stone)
    save_with_meta("Runic_Plaque_Bg", img, (6, 6, 6, 6))

if __name__ == "__main__":
    make_ruby_fill()
    make_sapphire_fill()
    make_amber_fill()
    make_amethyst_fill()
    make_obsidian_trough()
    make_bronze_bezel()
    make_runic_plaque()
    print("ALL SOULSLIKE TEXTURES CREATED SUCCESSFULLY!")
