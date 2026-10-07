import os
import math
from PIL import Image, ImageDraw

p4_dir = "Assets/ART/Player4"
p4_ui_dir = "Assets/ART/Player4/UI"
os.makedirs(p4_ui_dir, exist_ok=True)

# Color Palette: Obsidian Dreadnought with Hellfire Runes & Steel Spikes
C_TRANSPARENT = (0, 0, 0, 0)
C_DARK_SHADOW = (14, 12, 22, 255)
C_OBSIDIAN_DEEP = (26, 26, 40, 255)
C_OBSIDIAN_MID = (46, 52, 78, 255)
C_OBSIDIAN_LIGHT = (76, 88, 120, 255)
C_STEEL_HIGHLIGHT = (145, 160, 192, 255)
C_SPIKE_METAL = (210, 220, 240, 255)

# Hellfire & Rune Glow Colors
C_HELL_DARK = (150, 20, 40, 255)
C_HELL_FIRE = (245, 45, 45, 255)
C_HELL_ORANGE = (255, 130, 25, 255)
C_HELL_GOLD = (255, 220, 70, 255)
C_HELL_CORE = (255, 255, 220, 255)

CELL_SIZE = 128
SCALE = 2 # 2x pixel size for chunky crisp 64x64 grid inside 128 canvas

COLORS = {
    '.': None,
    'K': C_DARK_SHADOW,
    'D': C_OBSIDIAN_DEEP,
    'M': C_OBSIDIAN_MID,
    'L': C_OBSIDIAN_LIGHT,
    'H': C_STEEL_HIGHLIGHT,
    'S': C_SPIKE_METAL,
    'R': C_HELL_FIRE,
    'O': C_HELL_ORANGE,
    'G': C_HELL_GOLD,
    'W': C_HELL_CORE,
    'E': C_HELL_DARK
}

def draw_block(draw, px, py, color, sz=SCALE):
    if color is not None:
        draw.rectangle([px * sz, py * sz, (px + 1) * sz - 1, (py + 1) * sz - 1], fill=color)

def draw_matrix(draw, ox, oy, matrix, flip_x=False):
    h = len(matrix)
    w = max(len(r) for r in matrix) if h > 0 else 0
    for r_idx, row in enumerate(matrix):
        for c_idx, char in enumerate(row):
            col = COLORS.get(char)
            if col:
                target_x = ox + (w - 1 - c_idx if flip_x else c_idx)
                draw_block(draw, target_x, oy + r_idx, col)

# 1. Horned Dreadnought Helm (Front / 3/4)
HELM_BASE = [
    "....SH....HS....",
    "...SMH....HMS...",
    "..SMDD....DDMS..",
    "..MDDDHHHHMDDM..",
    ".MDDDKKKKKKDDM..",
    ".MDDDRROORRDDM..",
    ".MDDDGGWWGGDDM..",
    "..MDDKKKKKKDM...",
    "..MMDDDDDDDDM...",
    "...MMDDDDDDM...."
]

HELM_BACK = [
    "....SH....HS....",
    "...SMH....HMS...",
    "..SMDD....DDMS..",
    "..MDDDHHHHMDDM..",
    ".MDDDDDDDDDDDM..",
    ".MDDDDDMMDDDDM..",
    ".MDDDDDMMDDDDM..",
    "..MDDDDDDDDDM...",
    "..MMDDDDDDDDM...",
    "...MMDDDDDDM...."
]

# 2. Heavy Obsidian Spiked Shoulders & Chestplate
CHEST_BASE = [
    ".SSSHMDDDDDMHSSS.",
    "SSMMMDDDDDDDMMMMS",
    "SMMMDDDEEEDDDMMMS",
    ".MDDDDERREEDDDM..",
    ".MDDDERGOREDDDM..",
    ".MDDDDEWWEDDDDM..",
    "..MDDDERRREDDM...",
    "..MDDDDEEEDDDM...",
    "...MMMDDDDDMM....",
    "....MDDDDDDM.....",
    "....MDDMMDDM....."
]

CHEST_BACK = [
    ".SSSHMDDDDDMHSSS.",
    "SSMMMDDDDDDDMMMMS",
    "SMMMDDDDDDDDDMMMS",
    ".MDDDDDMMDDDDDM..",
    ".MDDDDDMMDDDDDM..",
    ".MDDDDDMMDDDDDM..",
    "..MDDDDDDDDDDM...",
    "..MDDDDDDDDDDM...",
    "...MMMDDDDDMM....",
    "....MDDDDDDM.....",
    "....MDDMMDDM....."
]

# 3. Spiked Demon Tower Shield (Detailed)
SHIELD_TOWER = [
    "..SHHS..",
    ".SHMMHS.",
    "SHMDDMHS",
    "SMDEEDMS",
    "SMEREMS.",
    "SMEGREMS",
    "SMEWWEMS",
    "SMEGREMS",
    "SMEREMS.",
    "SMDEEDMS",
    "SHMDDMHS",
    ".SHMMHS.",
    "..SHHS..",
    "...SS...",
    "...SS..."
]

# 4. Heavy Armored Legs / Boots
LEGS_IDLE = [
    "....MDDDDDM.....",
    "...MDDD.DDDM....",
    "...MDDM.MDDM....",
    "...MDDM.MDDM....",
    "...MDDM.MDDM....",
    "..SSMDM.MDMSS...",
    "..SHHDM.MDHHS..."
]

def render_frame(direction="down", action="idle", frame_idx=0):
    img = Image.new("RGBA", (CELL_SIZE, CELL_SIZE), C_TRANSPARENT)
    draw = ImageDraw.Draw(img)

    cx = 32
    cy = 38

    body_oy = 0
    left_shield_pos = (0, 0)
    right_shield_pos = (0, 0)
    fx = None
    is_back = (direction == "up")

    # Animation Logics
    if action == "idle":
        body_oy = 0 if frame_idx in [0, 2] else 1
        left_shield_pos = (-2, body_oy)
        right_shield_pos = (2, body_oy)
        leg_type = "idle"
    elif action == "walk":
        body_oy = -1 if frame_idx in [1, 3] else 0
        left_shield_pos = (-2 + (1 if frame_idx in [0, 2] else -1), body_oy)
        right_shield_pos = (2 + (-1 if frame_idx in [0, 2] else 1), body_oy)
        leg_type = "walk"
    elif action == "run":
        body_oy = 1 if frame_idx % 2 == 0 else -1
        # Aggressive forward charge: bring shields together to form front ram
        left_shield_pos = (1, body_oy + 2)
        right_shield_pos = (-1, body_oy + 2)
        leg_type = "run"
        if frame_idx % 2 == 1:
            fx = "charge_sparks"
    elif action == "attack":
        leg_type = "idle"
        if frame_idx == 0:
            # Windup: pull both shields back
            body_oy = -1
            left_shield_pos = (-5, -2)
            right_shield_pos = (5, -2)
        elif frame_idx == 1:
            # Shield bash left
            body_oy = 1
            left_shield_pos = (3, 2)
            right_shield_pos = (4, 0)
            fx = "bash_spark"
        elif frame_idx == 2:
            # Dual scissor smash clash
            body_oy = 2
            left_shield_pos = (1, 3)
            right_shield_pos = (-1, 3)
            fx = "clash_shockwave"
        elif frame_idx == 3:
            # Recoil recovery
            body_oy = 0
            left_shield_pos = (-1, 1)
            right_shield_pos = (1, 1)
    elif action == "skill":
        leg_type = "idle"
        if frame_idx == 0:
            # Surge power / lift shields
            body_oy = -3
            left_shield_pos = (-4, -6)
            right_shield_pos = (4, -6)
        elif frame_idx == 1:
            # Peak jump / roar
            body_oy = -5
            left_shield_pos = (-3, -8)
            right_shield_pos = (3, -8)
            fx = "barrier_prep"
        elif frame_idx == 2:
            # Ground smash!
            body_oy = 3
            left_shield_pos = (-1, 6)
            right_shield_pos = (1, 6)
            fx = "ground_slam"
        elif frame_idx == 3:
            # Radiant fortress barrier active
            body_oy = 1
            left_shield_pos = (-2, 4)
            right_shield_pos = (2, 4)
            fx = "fortress_barrier"
    elif action == "hurt":
        leg_type = "idle"
        body_oy = 1
        left_shield_pos = (1, 1)
        right_shield_pos = (-1, 1)
        fx = "hurt_deflect"
    elif action == "death":
        if frame_idx == 0:
            body_oy = 1
            left_shield_pos = (1, 2)
            right_shield_pos = (-1, 2)
            leg_type = "idle"
        elif frame_idx == 1:
            body_oy = 3
            left_shield_pos = (-2, 4)
            right_shield_pos = (2, 4)
            leg_type = "idle"
        elif frame_idx == 2:
            body_oy = 5
            left_shield_pos = (-4, 8)
            right_shield_pos = (4, 8)
            leg_type = "kneel"
        elif frame_idx == 3:
            body_oy = 7
            left_shield_pos = (-5, 10)
            right_shield_pos = (5, 10)
            leg_type = "kneel"

    # Draw Components
    # 1. Legs
    if leg_type == "idle":
        draw_matrix(draw, cx - 8, cy + 12 + body_oy, LEGS_IDLE)
    elif leg_type == "walk":
        shift = int(math.sin(frame_idx * math.pi / 2.0) * 3)
        draw_matrix(draw, cx - 8 + shift, cy + 12 + body_oy, LEGS_IDLE)
    elif leg_type == "run":
        shift = 2 if frame_idx % 2 == 0 else -2
        draw_matrix(draw, cx - 8 + shift, cy + 13 + body_oy, LEGS_IDLE)
    elif leg_type == "kneel":
        leg_kneel = [
            "..MDDDDDDDDDM...",
            ".MDDDDDDDDDDDM..",
            "SSMDMDDDDDMDMSS.",
            "SHHDMMMMMMMDHHS."
        ]
        draw_matrix(draw, cx - 8, cy + 15 + body_oy, leg_kneel)

    # 2. Chest / Torso
    draw_matrix(draw, cx - 8, cy + body_oy, CHEST_BACK if is_back else CHEST_BASE)

    # 3. Helm
    draw_matrix(draw, cx - 8, cy - 9 + body_oy, HELM_BACK if is_back else HELM_BASE)

    # 4. Shields (Left & Right)
    lx = cx - 14 + left_shield_pos[0]
    ly = cy - 2 + body_oy + left_shield_pos[1]
    rx = cx + 8 + right_shield_pos[0]
    ry = cy - 2 + body_oy + right_shield_pos[1]

    draw_matrix(draw, lx, ly, SHIELD_TOWER)
    draw_matrix(draw, rx, ry, SHIELD_TOWER, flip_x=True)

    # 5. Visual FX
    if fx == "charge_sparks":
        for spk in range(4):
            sx = (cx - 10 + spk * 7) * SCALE
            sy = (cy + 22) * SCALE
            draw.rectangle([sx, sy, sx + 3, sy + 3], fill=C_HELL_GOLD)
    elif fx == "bash_spark":
        draw.arc([lx * SCALE - 8, ly * SCALE - 8, (lx + 24) * SCALE, (ly + 28) * SCALE], start=30, end=150, fill=C_HELL_FIRE, width=4)
        draw.arc([lx * SCALE - 4, ly * SCALE - 4, (lx + 20) * SCALE, (ly + 24) * SCALE], start=40, end=140, fill=C_HELL_GOLD, width=3)
    elif fx == "clash_shockwave":
        draw.ellipse([(cx - 18) * SCALE, (cy - 4) * SCALE, (cx + 18) * SCALE, (cy + 24) * SCALE], outline=C_HELL_GOLD, width=3)
        draw.arc([(cx - 24) * SCALE, (cy - 10) * SCALE, (cx + 24) * SCALE, (cy + 30) * SCALE], start=0, end=360, fill=C_HELL_FIRE, width=2)
    elif fx == "ground_slam":
        # Magma cracks & spikes
        for ox in range(-20, 21, 5):
            h_spk = 8 + (abs(ox) % 4) * 3
            draw.polygon([
                (cx * SCALE + ox * 2, (cy + 24) * SCALE),
                (cx * SCALE + ox * 2 + 2, (cy + 24 - h_spk) * SCALE),
                (cx * SCALE + ox * 2 + 5, (cy + 24) * SCALE)
            ], fill=C_SPIKE_METAL)
            draw.line([(cx * SCALE + ox * 2, (cy + 24) * SCALE), (cx * SCALE + ox * 2 + 5, (cy + 24) * SCALE)], fill=C_HELL_GOLD, width=3)
    elif fx == "fortress_barrier":
        # Radiant hexagonal barrier
        pts = [
            (cx * SCALE, (cy - 22) * SCALE),
            ((cx + 28) * SCALE, (cy - 7) * SCALE),
            ((cx + 28) * SCALE, (cy + 18) * SCALE),
            (cx * SCALE, (cy + 28) * SCALE),
            ((cx - 28) * SCALE, (cy + 18) * SCALE),
            ((cx - 28) * SCALE, (cy - 7) * SCALE)
        ]
        draw.polygon(pts, outline=C_HELL_GOLD, width=3)
        draw.polygon([(x*0.92 + cx*SCALE*0.08, y*0.92 + cy*SCALE*0.08) for x,y in pts], outline=C_HELL_FIRE, width=2)
    elif fx == "hurt_deflect":
        # Shield ricochet spark
        draw.line([((cx - 2) * SCALE, (cy + 2) * SCALE), ((cx - 8) * SCALE, (cy - 4) * SCALE)], fill=C_HELL_GOLD, width=3)
        draw.line([((cx + 2) * SCALE, (cy + 2) * SCALE), ((cx + 8) * SCALE, (cy - 4) * SCALE)], fill=C_HELL_CORE, width=3)

    return img

def build_4dir_sheet(action_name, out_filename):
    cols = 4
    rows = 4
    sheet = Image.new("RGBA", (cols * CELL_SIZE, rows * CELL_SIZE), C_TRANSPARENT)
    dirs = ["down", "left", "left", "up"]
    for r_idx, d in enumerate(dirs):
        for c_idx in range(cols):
            frame = render_frame(direction=d, action=action_name, frame_idx=c_idx)
            sheet.paste(frame, (c_idx * CELL_SIZE, r_idx * CELL_SIZE))
    sheet.save(os.path.join(p4_dir, out_filename))
    print(f"Generated {out_filename} (512x512)")

# Generate all action sheets
build_4dir_sheet("idle", "Hammer_Idle.png")
build_4dir_sheet("walk", "Hammer_Walk.png")
build_4dir_sheet("run", "Hammer_Run.png")
build_4dir_sheet("attack", "Hammer_Attack.png")
build_4dir_sheet("skill", "Hammer_Skill.png")

# Generate Death & Hurt
death_sheet = Image.new("RGBA", (4 * CELL_SIZE, CELL_SIZE), C_TRANSPARENT)
hurt_sheet = Image.new("RGBA", (4 * CELL_SIZE, CELL_SIZE), C_TRANSPARENT)

for c_idx in range(4):
    d_frame = render_frame(direction="down", action="death", frame_idx=c_idx)
    h_frame = render_frame(direction="down", action="hurt", frame_idx=c_idx)
    death_sheet.paste(d_frame, (c_idx * CELL_SIZE, 0))
    hurt_sheet.paste(h_frame, (c_idx * CELL_SIZE, 0))

death_sheet.save(os.path.join(p4_dir, "Hammer_Death.png"))
hurt_sheet.save(os.path.join(p4_dir, "Hammer_Hurt.png"))
print("Generated Hammer_Death.png and Hammer_Hurt.png")

# Generate Hero_Player4_Portrait.png
portrait = Image.new("RGBA", (128, 128), (20, 16, 28, 255))
p_draw = ImageDraw.Draw(portrait)
# Frame border
p_draw.rectangle([2, 2, 125, 125], outline=C_HELL_GOLD, width=2)
p_draw.rectangle([5, 5, 122, 122], outline=C_OBSIDIAN_LIGHT, width=1)

base_idle = render_frame(direction="down", action="idle", frame_idx=0)
# Zoom in on head and spiked shoulders
portrait.paste(base_idle.resize((170, 170), Image.NEAREST), (-21, -12), base_idle.resize((170, 170), Image.NEAREST))
portrait.save(os.path.join(p4_ui_dir, "Hero_Player4_Portrait.png"))
print("Generated Hero_Player4_Portrait.png")

# Generate Montage Preview
preview = Image.new("RGBA", (CELL_SIZE * 4, CELL_SIZE * 5), (14, 12, 20, 255))
actions = ["idle", "walk", "run", "attack", "skill"]
for r_idx, act in enumerate(actions):
    for c_idx in range(4):
        f = render_frame(direction="down", action=act, frame_idx=c_idx)
        preview.paste(f, (c_idx * CELL_SIZE, r_idx * CELL_SIZE), f)

preview.save(os.path.join(p4_dir, "player4_obsidian_dreadnought_preview.png"))
print("Generated player4_obsidian_dreadnought_preview.png successfully!")
