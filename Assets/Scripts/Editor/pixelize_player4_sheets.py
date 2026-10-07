import os
import shutil
from PIL import Image, ImageEnhance

p4_dir = "Assets/ART/Player4"
p4_ui_dir = "Assets/ART/Player4/UI"

def convert_to_true_pixel_art(img, n_colors=32):
    """
    Transforms smooth painted images into true crisp pixel art:
    1. Binary alpha mask (no blurry edge halos)
    2. 1px Dark Contour Outline
    3. Strict Color Palette Quantization (32 colors)
    """
    img = img.convert("RGBA")
    w, h = img.size
    pixels = img.load()
    
    # Step 1: Clean binary alpha
    binary_img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    b_pix = binary_img.load()
    
    for y in range(h):
        for x in range(w):
            r, g, b, a = pixels[x, y]
            if a > 70 and max(r, g, b) > 20:
                b_pix[x, y] = (r, g, b, 255)
            else:
                b_pix[x, y] = (0, 0, 0, 0)
                
    # Step 2: Add 1px dark pixel-art outline around foreground pixels
    outlined = binary_img.copy()
    out_pix = outlined.load()
    OUTLINE_COLOR = (24, 26, 32, 255)
    
    for y in range(h):
        for x in range(w):
            if b_pix[x, y][3] == 0:
                # Check 4 neighbors
                has_opaque_neighbor = False
                for dx, dy in [(-1, 0), (1, 0), (0, -1), (0, 1)]:
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < w and 0 <= ny < h:
                        if b_pix[nx, ny][3] > 0 and b_pix[nx, ny] != OUTLINE_COLOR:
                            has_opaque_neighbor = True
                            break
                if has_opaque_neighbor:
                    out_pix[x, y] = OUTLINE_COLOR
                    
    # Step 3: Quantize colors to true retro pixel art palette
    r, g, b, a = outlined.split()
    rgb = outlined.convert("RGB")
    quant = rgb.quantize(colors=n_colors, method=Image.Quantize.MEDIANCUT).convert("RGBA")
    q_pix = quant.load()
    a_pix = a.load()
    
    final_img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    f_pix = final_img.load()
    
    for y in range(h):
        for x in range(w):
            if a_pix[x, y] > 0:
                qr, qg, qb, _ = q_pix[x, y]
                f_pix[x, y] = (qr, qg, qb, 255)
                
    return final_img

# Process all Player 4 sheets
sheet_names = [
    "Hammer_Idle.png",
    "Hammer_Walk.png",
    "Hammer_Run.png",
    "Hammer_Attack.png",
    "Hammer_Skill.png",
    "Hammer_Hurt.png",
    "Hammer_Death.png"
]

for name in sheet_names:
    path = os.path.join(p4_dir, name)
    if os.path.exists(path):
        im = Image.open(path)
        px_im = convert_to_true_pixel_art(im, n_colors=36)
        px_im.save(path)
        n_c = len(px_im.getcolors(maxcolors=5000) or [])
        print(f"Pixelized {name}: {im.size} -> {n_c} colors")

# Build a crisp 4x preview
idle = Image.open(os.path.join(p4_dir, "Hammer_Idle.png"))
atk = Image.open(os.path.join(p4_dir, "Hammer_Attack.png"))
skill = Image.open(os.path.join(p4_dir, "Hammer_Skill.png"))

canvas = Image.new("RGBA", (64 * 4, 64 * 3), (20, 22, 28, 255))
# Row 0: Idle 4 directions
for i, r in enumerate([0, 1, 2, 3]):
    f = idle.crop((0, r * 64, 64, (r + 1) * 64))
    canvas.paste(f, (i * 64, 0), f)
    
# Row 1: Attack Down & Left Frames
for i, c in enumerate([0, 2, 4, 6]):
    f = atk.crop((c * 64, 64, (c + 1) * 64, 128))
    canvas.paste(f, (i * 64, 64), f)
    
# Row 2: Skill Area Slam Frames
for i, c in enumerate([0, 2, 4, 6]):
    f = skill.crop((c * 64, 0, (c + 1) * 64, 64))
    canvas.paste(f, (i * 64, 128), f)

preview_4x = canvas.resize((canvas.width * 4, canvas.height * 4), Image.Resampling.NEAREST)
preview_path = "Assets/ART/Player4/true_pixel_art_preview.png"
preview_4x.save(preview_path)

# Copy to artifact folder
artifact_dir = "C:/Users/TRUONG HUNG/.gemini/antigravity-ide/brain/953cb4d4-3543-43c3-8946-55fea43b90d1"
shutil.copy(preview_path, os.path.join(artifact_dir, "true_pixel_art_preview.png"))
print("Saved true_pixel_art_preview.png!")
