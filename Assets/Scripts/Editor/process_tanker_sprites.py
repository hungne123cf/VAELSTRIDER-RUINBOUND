import os
import shutil
from PIL import Image

def is_bg(r, g, b):
    # Check if pixel is part of the gray checkerboard
    if abs(r - g) <= 8 and abs(g - b) <= 8 and abs(r - b) <= 8:
        if (115 <= r <= 150) or (165 <= r <= 200):
            return True
    return False

def clean_sprite_box(img, box, target_size=(128, 128), flip_x=False):
    """
    Crops box (x0, y0, x1, y1) from img, removes background,
    and places centered in target_size (128x128) transparent canvas.
    """
    crop = img.crop(box)
    cw, ch = crop.size
    cpix = crop.load()
    
    # 1. Clean background removal
    clean = Image.new("RGBA", (cw, ch), (0, 0, 0, 0))
    cl_pix = clean.load()
    
    for y in range(ch):
        for x in range(cw):
            r, g, b, a = cpix[x, y]
            if is_bg(r, g, b):
                cl_pix[x, y] = (0, 0, 0, 0)
            else:
                cl_pix[x, y] = (r, g, b, 255)
                
    # 2. Trim excess empty transparent borders
    bbox = clean.getbbox()
    if bbox:
        trimmed = clean.crop(bbox)
    else:
        trimmed = clean
        
    if flip_x:
        trimmed = trimmed.transpose(Image.FLIP_LEFT_RIGHT)
        
    tw, th = trimmed.size
    
    # 3. Fit nicely into 128x128 canvas
    canvas = Image.new("RGBA", target_size, (0, 0, 0, 0))
    # Place bottom-centered so feet align naturally across animation frames
    offset_x = (target_size[0] - tw) // 2
    offset_y = target_size[1] - th - 16  # 16px margin from bottom
    if offset_y < 4:
        offset_y = 4
    canvas.paste(trimmed, (offset_x, offset_y), trimmed)
    return canvas

def build_grid(frames_by_row, out_path, cell_size=(128, 128)):
    rows = len(frames_by_row)
    cols = max(len(r) for r in frames_by_row)
    
    grid = Image.new("RGBA", (cols * cell_size[0], rows * cell_size[1]), (0, 0, 0, 0))
    for r_idx, row in enumerate(frames_by_row):
        for c_idx, frame in enumerate(row):
            grid.paste(frame, (c_idx * cell_size[0], r_idx * cell_size[1]), frame)
            
    grid.save(out_path)
    print(f"Saved: {out_path} ({grid.size[0]}x{grid.size[1]})")

def main():
    img1_path = "C:/Users/TRUONG HUNG/.gemini/antigravity-ide/brain/11bc86cd-0200-4f80-9a3f-a0f197c9f08b/tanker_topdown_spritesheet_1791307901726.jpg"
    img2_path = "C:/Users/TRUONG HUNG/.gemini/antigravity-ide/brain/11bc86cd-0200-4f80-9a3f-a0f197c9f08b/tanker_4directions_movement_1791307919168.jpg"

    img1 = Image.open(img1_path).convert("RGBA")
    img2 = Image.open(img2_path).convert("RGBA")

    out_dir = "Assets/ART/Player4"
    ui_dir = "Assets/ART/Player4/UI"
    os.makedirs(out_dir, exist_ok=True)
    os.makedirs(ui_dir, exist_ok=True)

    # In img2: 6 rows, ~4 cols per row (1024x1024 / 6 = ~170.6 height, 1024/4 = 256 width)
    # Let's define grid boxes for img2 (w=1024, h=1024)
    # Row heights: ~170 each. Col widths: ~256 each.
    
    def get_img2_cell(r, c):
        x0 = int(c * (1024 / 4.0))
        x1 = int((c + 1) * (1024 / 4.0))
        y0 = int(r * (1024 / 6.0))
        y1 = int((r + 1) * (1024 / 6.0))
        return (x0, y0, x1, y1)

    def get_img1_cell(r, c, cols=5, rows=6):
        x0 = int(c * (1024 / float(cols)))
        x1 = int((c + 1) * (1024 / float(cols)))
        y0 = int(r * (1024 / float(rows)))
        y1 = int((r + 1) * (1024 / float(rows)))
        return (x0, y0, x1, y1)

    # 1. Walk / Movement
    # Row 0: Down, Row 1: Up, Row 2: Side
    walk_down = [clean_sprite_box(img2, get_img2_cell(0, c)) for c in range(4)]
    walk_up = [clean_sprite_box(img2, get_img2_cell(1, c)) for c in range(4)]
    walk_left = [clean_sprite_box(img2, get_img2_cell(2, c), flip_x=True) for c in range(4)]
    walk_right = [clean_sprite_box(img2, get_img2_cell(2, c), flip_x=False) for c in range(4)]

    # Standard 4x4 Grid (Row 0: Down, Row 1: Left, Row 2: Right, Row 3: Up)
    walk_grid = [walk_down, walk_left, walk_right, walk_up]
    build_grid(walk_grid, os.path.join(out_dir, "Hammer_Walk.png"))
    build_grid(walk_grid, os.path.join(out_dir, "Hammer_Run.png"))

    # 2. Idle (using first frame / breathing poses)
    idle_down = [walk_down[0], walk_down[1], walk_down[0], walk_down[2]]
    idle_left = [walk_left[0], walk_left[1], walk_left[0], walk_left[2]]
    idle_right = [walk_right[0], walk_right[1], walk_right[0], walk_right[2]]
    idle_up = [walk_up[0], walk_up[1], walk_up[0], walk_up[2]]
    idle_grid = [idle_down, idle_left, idle_right, idle_up]
    build_grid(idle_grid, os.path.join(out_dir, "Hammer_Idle.png"))

    # 3. Attack (Down: Row 3, Up/Side: Row 4)
    atk_down = [clean_sprite_box(img2, get_img2_cell(3, c)) for c in range(4)]
    atk_side_frames = [clean_sprite_box(img2, get_img2_cell(4, c)) for c in range(4)]
    atk_left = [clean_sprite_box(img2, get_img2_cell(4, c), flip_x=True) for c in range(4)]
    atk_right = [clean_sprite_box(img2, get_img2_cell(4, c), flip_x=False) for c in range(4)]
    atk_up = [clean_sprite_box(img2, get_img2_cell(4, 3)), clean_sprite_box(img2, get_img2_cell(1, 1)), clean_sprite_box(img2, get_img2_cell(1, 2)), clean_sprite_box(img2, get_img2_cell(1, 3))]
    atk_grid = [atk_down, atk_left, atk_right, atk_up]
    build_grid(atk_grid, os.path.join(out_dir, "Hammer_Attack.png"))

    # 4. Skill (Shield Slam / Ground Stomp - Row 5 of img2 & War Cry of img1)
    skill_down = [clean_sprite_box(img2, get_img2_cell(5, c)) for c in range(4)]
    skill_left = [clean_sprite_box(img2, get_img2_cell(5, c), flip_x=True) for c in range(4)]
    skill_right = [clean_sprite_box(img2, get_img2_cell(5, c), flip_x=False) for c in range(4)]
    skill_up = [clean_sprite_box(img2, get_img2_cell(5, 3)), clean_sprite_box(img2, get_img2_cell(5, 2)), clean_sprite_box(img2, get_img2_cell(5, 1)), clean_sprite_box(img2, get_img2_cell(5, 0))]
    skill_grid = [skill_down, skill_left, skill_right, skill_up]
    build_grid(skill_grid, os.path.join(out_dir, "Hammer_Skill.png"))

    # 5. Hurt (img1 Row 5)
    hurt_frames = [
        clean_sprite_box(img1, get_img1_cell(5, 0)),
        clean_sprite_box(img1, get_img1_cell(5, 1)),
        clean_sprite_box(img1, get_img1_cell(5, 0)),
        clean_sprite_box(img1, get_img1_cell(5, 1))
    ]
    build_grid([hurt_frames], os.path.join(out_dir, "Hammer_Hurt.png"))

    # 6. Death (img1 Row 5 col 2, 3, 4)
    death_frames = [
        clean_sprite_box(img1, get_img1_cell(5, 1)),
        clean_sprite_box(img1, get_img1_cell(5, 2)),
        clean_sprite_box(img1, get_img1_cell(5, 3)),
        clean_sprite_box(img1, get_img1_cell(5, 4))
    ]
    build_grid([death_frames], os.path.join(out_dir, "Hammer_Death.png"))

    # 7. Avatar UI
    avatar = clean_sprite_box(img2, get_img2_cell(0, 0), target_size=(128, 128))
    avatar.save(os.path.join(ui_dir, "avatar.png"))
    print("Avatar UI saved!")

if __name__ == "__main__":
    main()
