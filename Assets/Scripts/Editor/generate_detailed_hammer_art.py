import os
import shutil
from PIL import Image, ImageDraw

# Color ramps
C_OUT = (24, 26, 33, 255)
C_STL_SHD = (45, 52, 65, 255)
C_STL_MID = (78, 90, 112, 255)
C_STL_HI  = (135, 155, 185, 255)
C_STL_SPEC= (215, 230, 250, 255)

C_GLD_SHD = (115, 70, 12, 255)
C_GLD_MID = (205, 145, 25, 255)
C_GLD_HI  = (255, 215, 65, 255)
C_GLD_SPEC= (255, 245, 185, 255)

C_RBY_SHD = (120, 15, 15, 255)
C_RBY_MID = (225, 35, 35, 255)
C_RBY_HI  = (255, 110, 110, 255)
C_RBY_SPEC= (255, 220, 220, 255)

C_WOD_SHD = (38, 26, 18, 255)
C_WOD_MID = (72, 48, 33, 255)
C_WOD_HI  = (108, 74, 52, 255)

C_SKN_SHD = (130, 80, 60, 255)
C_SKN_MID = (195, 135, 105, 255)
C_SKN_HI  = (235, 175, 140, 255)

def render_warhammer_head(draw, cx, cy, style="maul", angle=0):
    """
    Renders an intricately shaded War Hammer Head with beveled metal, gold bands, and glowing ruby core.
    """
    # Base Dimensions: 13 wide, 9 tall
    # Octagonal/beveled shape with smooth corner clipping
    pts = [
        (cx - 5, cy - 4), (cx + 5, cy - 4),
        (cx + 6, cy - 3), (cx + 6, cy + 3),
        (cx + 5, cy + 4), (cx - 5, cy + 4),
        (cx - 6, cy + 3), (cx - 6, cy - 3)
    ]
    
    # 1. Dark Outline
    draw.polygon(pts, fill=C_OUT)
    
    # 2. Main Steel Body
    inner_pts = [
        (cx - 4, cy - 3), (cx + 4, cy - 3),
        (cx + 5, cy - 2), (cx + 5, cy + 2),
        (cx + 4, cy + 3), (cx - 4, cy + 3),
        (cx - 5, cy + 2), (cx - 5, cy - 2)
    ]
    draw.polygon(inner_pts, fill=C_STL_MID)
    
    # 3. Top & Left Highlight Chamfers
    draw.line([(cx - 4, cy - 3), (cx + 4, cy - 3)], fill=C_STL_HI)
    draw.line([(cx - 5, cy - 2), (cx - 5, cy + 2)], fill=C_STL_HI)
    draw.point((cx - 4, cy - 2), fill=C_STL_SPEC)
    
    # Bottom & Right Shadow Chamfers
    draw.line([(cx - 4, cy + 3), (cx + 4, cy + 3)], fill=C_STL_SHD)
    draw.line([(cx + 5, cy - 2), (cx + 5, cy + 2)], fill=C_STL_SHD)
    
    # 4. Central Reinforced Gold Collar & Studs
    draw.rectangle([cx - 2, cy - 4, cx + 2, cy + 4], fill=C_GLD_MID)
    draw.line([(cx - 2, cy - 4), (cx - 2, cy + 4)], fill=C_GLD_SHD)
    draw.line([(cx + 2, cy - 4), (cx + 2, cy + 4)], fill=C_GLD_HI)
    draw.point((cx, cy - 4), fill=C_GLD_SPEC)
    draw.point((cx, cy + 4), fill=C_GLD_SHD)
    
    # 5. Glowing Ruby Titan Core
    draw.rectangle([cx - 1, cy - 1, cx + 1, cy + 1], fill=C_RBY_MID)
    draw.point((cx, cy - 1), fill=C_RBY_HI)
    draw.point((cx, cy), fill=C_RBY_SPEC)
    draw.point((cx, cy + 1), fill=C_RBY_SHD)
    
    if style == "spiked":
        # Add rear armor-piercing curved claw/spike
        draw.line([(cx + 6, cy), (cx + 9, cy - 1)], fill=C_STL_HI)
        draw.line([(cx + 6, cy + 1), (cx + 9, cy)], fill=C_STL_SHD)
        draw.point((cx + 10, cy - 1), fill=C_STL_SPEC)

def render_shaft_with_grip(draw, x0, y0, x1, y1):
    """Renders textured dark ironwood shaft with gold wrapping & pommel."""
    dx = x1 - x0
    dy = y1 - y0
    steps = max(abs(dx), abs(dy), 1)
    for i in range(steps + 1):
        t = i / steps
        px = int(x0 + t * dx)
        py = int(y0 + t * dy)
        draw.point((px, py), fill=C_WOD_MID)
        draw.point((px + 1, py), fill=C_WOD_SHD)
        if i % 4 == 0:
            draw.point((px, py), fill=C_GLD_MID)
            
    # Pommel cap
    draw.rectangle([x1 - 1, y1 - 1, x1 + 2, y1 + 1], fill=C_OUT)
    draw.rectangle([x1, y1, x1 + 1, y1], fill=C_GLD_HI)

def render_armored_fist(draw, x, y, facing="left"):
    """Renders an anatomically contoured armored gauntlet holding the handle."""
    draw.rectangle([x - 2, y - 2, x + 2, y + 2], fill=C_OUT)
    draw.rectangle([x - 1, y - 1, x + 1, y + 1], fill=C_STL_MID)
    # Knuckle plate & gold studs
    draw.line([(x - 1, y - 1), (x + 1, y - 1)], fill=C_STL_HI)
    draw.point((x, y - 1), fill=C_GLD_HI)
    draw.line([(x - 1, y + 1), (x + 1, y + 1)], fill=C_STL_SHD)

def build_refined_preview():
    # Load Player 3 clean unarmed idle and attack base
    p3_idle = Image.open("Assets/ART/Player3/Unarmed_Idle.png").convert("RGBA")
    
    # Let's create a 4-row Showcase Preview Canvas:
    # Row 0: 4 Directions IDLE (Down, Left, Up-Left, Up)
    # Row 1: Attack Windup & Apex (Frames 0, 1, 2, 3)
    # Row 2: Attack Slam, Shockwave & Recovery (Frames 4, 5, 6, 7)
    
    canvas_w = 64 * 4
    canvas_h = 64 * 3
    showcase = Image.new("RGBA", (canvas_w, canvas_h), (22, 24, 30, 255))
    
    # 1. Recolor base hero to Titan Berserker
    # Import recolor function logic
    import importlib.util
    spec = importlib.util.spec_from_file_location("gen", "Assets/Scripts/Editor/generate_player4_sprites.py")
    gen_mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(gen_mod)
    
    titan_idle = gen_mod.recolor_to_titan(p3_idle)
    
    # --- IDLE STANCES ---
    # 1. Down (Front View)
    f_down = titan_idle.crop((0, 0, 64, 64))
    d0 = ImageDraw.Draw(f_down)
    render_warhammer_head(d0, 41, 21, "maul")
    render_shaft_with_grip(d0, 41, 26, 41, 46)
    render_armored_fist(d0, 41, 35)
    showcase.paste(f_down, (0 * 64, 0), f_down)
    
    # 2. Left (Side View - 2-Handed Combat Guard in FRONT)
    f_side = titan_idle.crop((0, 64, 64, 128))
    d1 = ImageDraw.Draw(f_side)
    # Angled shaft across chest into two hands
    render_shaft_with_grip(d1, 21, 23, 26, 45)
    render_warhammer_head(d1, 20, 19, "maul")
    # Upper hand gripping shaft near head
    render_armored_fist(d1, 22, 31)
    # Lower hand gripping shaft near hip
    render_armored_fist(d1, 24, 37)
    showcase.paste(f_side, (1 * 64, 0), f_side)
    
    # 3. Up-Left (Diagonal)
    f_diag = titan_idle.crop((0, 128, 64, 192))
    d2 = ImageDraw.Draw(f_diag)
    render_shaft_with_grip(d2, 23, 22, 27, 44)
    render_warhammer_head(d2, 22, 18, "maul")
    render_armored_fist(d2, 24, 32)
    showcase.paste(f_diag, (2 * 64, 0), f_diag)
    
    # 4. Up (Back View)
    f_up = titan_idle.crop((0, 192, 64, 256))
    d3 = ImageDraw.Draw(f_up)
    render_shaft_with_grip(d3, 25, 23, 27, 45)
    render_warhammer_head(d3, 24, 19, "maul")
    showcase.paste(f_up, (3 * 64, 0), f_up)
    
    # --- ATTACK ANIMATION ROW 1 & 2 ---
    atk_sheet = Image.open("Assets/ART/Player4/Hammer_Attack.png").convert("RGBA")
    
    # Row 1: Frames 0..3 (Windup & Apex)
    for col in range(4):
        f = atk_sheet.crop((col * 64, 64, (col + 1) * 64, 128))
        showcase.paste(f, (col * 64, 64), f)
        
    # Row 2: Frames 4..7 (Slam & Shockwave)
    for col in range(4):
        f = atk_sheet.crop(((col + 4) * 64, 64, (col + 5) * 64, 128))
        showcase.paste(f, (col * 64, 128), f)
        
    # Upscale 3x with NEAREST neighbor for crisp pixel clarity
    showcase_3x = showcase.resize((canvas_w * 3, canvas_h * 3), Image.Resampling.NEAREST)
    
    # Save preview image
    preview_path = "Assets/ART/Player4/titan_hammer_design_preview.png"
    showcase_3x.save(preview_path)
    
    # Also save to artifact directory for markdown embedding
    artifact_dir = "C:/Users/TRUONG HUNG/.gemini/antigravity-ide/brain/953cb4d4-3543-43c3-8946-55fea43b90d1"
    os.makedirs(artifact_dir, exist_ok=True)
    shutil.copy(preview_path, os.path.join(artifact_dir, "titan_hammer_design_preview.png"))
    
    print("Successfully built high-detail preview and saved to artifact dir!")

build_refined_preview()
