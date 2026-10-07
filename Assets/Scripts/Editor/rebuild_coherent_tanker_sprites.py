import os
import hashlib
from collections import deque
from PIL import Image

def clean_frame_bg(crop):
    w, h = crop.size
    pix = crop.load()

    def is_bg_pixel(x, y):
        r, g, b = pix[x, y][:3]
        # Pure white / off-white background
        if r >= 210 and g >= 210 and b >= 210:
            return True
        # Checkerboard tiles (neutral light gray / mid gray / dark gray tiles)
        if abs(r - g) <= 14 and abs(g - b) <= 14 and abs(r - b) <= 14 and max(r, g, b) >= 80:
            return True
        return False

    visited = [[False]*w for _ in range(h)]
    is_bg_map = [[False]*w for _ in range(h)]
    queue = deque()

    # Border seeds from all 4 borders (5px depth)
    for x in range(w):
        for y in range(min(6, h)):
            if is_bg_pixel(x, y) and not visited[y][x]:
                visited[y][x] = True
                queue.append((x, y))
            if is_bg_pixel(x, h - 1 - y) and not visited[h - 1 - y][x]:
                visited[h - 1 - y][x] = True
                queue.append((x, h - 1 - y))
    for y in range(h):
        for x in range(min(6, w)):
            if is_bg_pixel(x, y) and not visited[y][x]:
                visited[y][x] = True
                queue.append((x, y))
            if is_bg_pixel(w - 1 - x, y) and not visited[y][w - 1 - x]:
                visited[y][w - 1 - x] = True
                queue.append((w - 1 - x, y))

    while queue:
        cx, cy = queue.popleft()
        is_bg_map[cy][cx] = True
        for nx, ny in [(cx+1, cy), (cx-1, cy), (cx, cy+1), (cx, cy-1)]:
            if 0 <= nx < w and 0 <= ny < h and not visited[ny][nx]:
                if is_bg_pixel(nx, ny):
                    visited[ny][nx] = True
                    queue.append((nx, ny))

    out = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    opix = out.load()
    for y in range(h):
        for x in range(w):
            if not is_bg_map[y][x]:
                opix[x, y] = (pix[x, y][0], pix[x, y][1], pix[x, y][2], 255)
    return out

def extract_frame(img, r, c, target_size=(128, 128), flip_x=False):
    cell_w = img.size[0] // 4
    cell_h = img.size[1] // 4
    x0 = c * cell_w
    y0 = r * cell_h
    crop = img.crop((x0, y0, x0 + cell_w, y0 + cell_h))

    cleaned = clean_frame_bg(crop)
    bbox = cleaned.getbbox()
    if not bbox:
        return Image.new('RGBA', target_size, (0, 0, 0, 0))

    char = cleaned.crop(bbox)
    if flip_x:
        char = char.transpose(Image.FLIP_LEFT_RIGHT)

    tw, th = char.size
    scale = min(114.0 / tw, 114.0 / th, 0.50)
    nw = max(1, int(tw * scale))
    nh = max(1, int(th * scale))
    scaled = char.resize((nw, nh), Image.Resampling.NEAREST)

    canvas = Image.new('RGBA', target_size, (0, 0, 0, 0))
    fx = (target_size[0] - nw) // 2
    fy = target_size[1] - nh - 6
    if fy < 4: fy = 4
    canvas.paste(scaled, (fx, fy), scaled)
    return canvas

def safe_save_image(img, filepath):
    filepath = filepath.replace('\\', '/')
    os.makedirs(os.path.dirname(filepath), exist_ok=True)
    tmp_path = filepath + '.tmp.png'
    img.save(tmp_path)
    if os.path.exists(filepath):
        try:
            os.remove(filepath)
        except:
            pass
    os.replace(tmp_path, filepath)
    print(f"Saved: {filepath} ({img.size[0]}x{img.size[1]})")

def build_sheet(grid_rows, out_path, cell_size=(128, 128)):
    rows = len(grid_rows)
    cols = max(len(r) for r in grid_rows)
    sheet = Image.new('RGBA', (cols * cell_size[0], rows * cell_size[1]), (0, 0, 0, 0))
    for r_idx, row in enumerate(grid_rows):
        for c_idx, frame in enumerate(row):
            sheet.paste(frame, (c_idx * cell_size[0], r_idx * cell_size[1]), frame)
    safe_save_image(sheet, out_path)

def generate_file_id(name):
    h = hashlib.sha1(name.encode('utf-8')).digest()
    return int.from_bytes(h[:8], byteorder='big', signed=True)

def create_texture_meta(png_path, cols, rows, frame_w=128, frame_h=128, ppu=64):
    png_path = png_path.replace('\\', '/')
    filename = os.path.basename(png_path)
    stem = os.path.splitext(filename)[0]
    guid = hashlib.md5(png_path.lower().encode('utf-8')).hexdigest()
    meta_path = png_path + ".meta"
    
    sprites_meta = []
    internal_ids = []
    
    for r in range(rows):
        unity_y = (rows - 1 - r) * frame_h
        for c in range(cols):
            sprite_name = f"{stem}_{r}_{c}"
            fid = generate_file_id(sprite_name)
            internal_ids.append((fid, sprite_name))
            
            s_entry = f"""    - serializedVersion: 2
      name: {sprite_name}
      rect:
        serializedVersion: 2
        x: {c * frame_w}
        y: {unity_y}
        width: {frame_w}
        height: {frame_h}
      alignment: 0
      pivot: {{x: 0.5, y: 0.5}}
      border: {{x: 0, y: 0, z: 0, w: 0}}
      outline: []
      physicsShape: []
      tessellationDetail: 0
      bones: []
      spriteID: {hashlib.md5(sprite_name.encode('utf-8')).hexdigest()}
      internalID: {fid}
      vertices: []
      indices: 
      edges: []
      weights: []"""
            sprites_meta.append(s_entry)
            
    id_table_str = "\n".join([f"  - first:\n      213: {fid}\n    second: {name}" for fid, name in internal_ids])
    sprites_str = "\n".join(sprites_meta)
    
    meta_content = f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable:
{id_table_str}
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableBMColorScale: 0
    blendSubBlobs: 0
    matchRootImage: 0
    exactThreshold: 0
    srcColorSpace: 1
    defaultAlphaChannel: 0
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 1
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMasterTextureLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSizeSet: 0
  compressionQuality: 50
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbook: 0
  ignorePngGamma: 0
  cookieLightType: 0
  platformSettings: []
  spriteMode: 2
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: {ppu}
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  spritePackingTag: 
  pSDRemoveMatte: 0
  pSDShowRemoveMatteOption: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
  sprites:
{sprites_str}
"""
    with open(meta_path, "w", encoding="utf-8") as f:
        f.write(meta_content)

def make_clip_yaml(clip_name, sprite_refs, frame_rate=8.0, loop=True, flip_x=False):
    n_frames = len(sprite_refs)
    duration = n_frames / frame_rate
    
    keyframes_str = []
    for i, (fid, guid) in enumerate(sprite_refs):
        t = i / frame_rate
        keyframes_str.append(f"""    - time: {t:.7g}
      value: {{fileID: {fid}, guid: {guid}, type: 3}}""")
        
    keyframes_block = "\n".join(keyframes_str)
    
    flip_val = 1 if flip_x else 0
    loop_val = 1 if loop else 0
    
    yaml_content = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!74 &7400000
AnimationClip:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {clip_name}
  serializedVersion: 7
  m_Legacy: 0
  m_Compressed: 0
  m_UseHighQualityCurve: 1
  m_RotationCurves: []
  m_CompressedRotationCurves: []
  m_EulerCurves: []
  m_PositionCurves: []
  m_ScaleCurves: []
  m_FloatCurves:
  - serializedVersion: 2
    curve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: {flip_val}
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      - serializedVersion: 3
        time: {duration:.7g}
        value: {flip_val}
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
    attribute: m_FlipX
    path: 
    classID: 212
    script: {{fileID: 0}}
    flags: 0
  m_PPtrCurves:
  - serializedVersion: 2
    curve:
{keyframes_block}
    attribute: m_Sprite
    path: 
    classID: 212
    script: {{fileID: 0}}
    flags: 2
  m_SampleRate: {frame_rate}
  m_WrapMode: 0
  m_Bounds:
    m_Center: {{x: 0, y: 0, z: 0}}
    m_Extent: {{x: 0, y: 0, z: 0}}
  m_ClipBindingConstant:
    genericBindings:
    - serializedVersion: 2
      path: 0
      attribute: 0
      script: {{fileID: 0}}
      typeID: 212
      customType: 23
      isPPtrCurve: 1
      isIntCurve: 0
      isSerializeReferenceCurve: 0
    - serializedVersion: 2
      path: 0
      attribute: 555744692
      script: {{fileID: 0}}
      typeID: 212
      customType: 0
      isPPtrCurve: 0
      isIntCurve: 0
      isSerializeReferenceCurve: 0
    pptrCurveMapping:
"""
    for fid, guid in sprite_refs:
        yaml_content += f"    - {{fileID: {fid}, guid: {guid}, type: 3}}\n"
        
    yaml_content += f"""  m_AnimationClipSettings:
    serializedVersion: 2
    m_AdditiveReferencePoseClip: {{fileID: 0}}
    m_AdditiveReferencePoseTime: 0
    m_StartTime: 0
    m_StopTime: {duration:.7g}
    m_OrientationOffsetY: 0
    m_Level: 0
    m_CycleOffset: 0
    m_HasAdditiveReferencePose: 0
    m_LoopTime: {loop_val}
    m_LoopBlend: 0
    m_LoopBlendOrientation: 0
    m_LoopBlendPositionY: 0
    m_LoopBlendPositionXZ: 0
    m_KeepOriginalOrientation: 0
    m_KeepOriginalPositionY: 1
    m_KeepOriginalPositionXZ: 0
    m_HeightFromFeet: 0
    m_Mirror: 0
  m_EditorCurves: []
  m_EulerEditorCurves: []
  m_HasGenericRootTransform: 0
  m_HasMotionFloatCurves: 0
  m_Events: []
"""
    return yaml_content

def main():
    art_path = "C:/Users/TRUONG HUNG/.gemini/antigravity-ide/brain/11bc86cd-0200-4f80-9a3f-a0f197c9f08b"
    p4_dir = "Assets/ART/Player4"
    anim_dir = "Assets/Animation/Player4"
    os.makedirs(p4_dir, exist_ok=True)
    os.makedirs(os.path.join(p4_dir, "UI"), exist_ok=True)
    os.makedirs(anim_dir, exist_ok=True)

    src_walk = Image.open(f"{art_path}/tanker_4dir_complete_intact_1791308334479.jpg")
    src_actions = Image.open(f"{art_path}/tanker_actions_skills_death_1791308351781.jpg")
    src_attack = Image.open(f"{art_path}/tanker_coherent_attack_sheet_1791309370718.jpg")

    # 1. HAMMER_ATTACK (4 directions x 4 coherent continuous frames)
    atk_down = [extract_frame(src_attack, 0, c) for c in range(4)]
    atk_left = [extract_frame(src_attack, 1, c, flip_x=True) for c in range(4)]
    atk_right = [extract_frame(src_attack, 1, c, flip_x=False) for c in range(4)]
    atk_up = [extract_frame(src_attack, 2, c) for c in range(4)]
    build_sheet([atk_down, atk_left, atk_right, atk_up], f"{p4_dir}/Hammer_Attack.png")
    create_texture_meta(f"{p4_dir}/Hammer_Attack.png", 4, 4)

    # 2. HAMMER_SKILL (Earthquake Slam / Cataclysm Slam)
    skill_down = [extract_frame(src_actions, 1, c) for c in range(4)]
    skill_left = [extract_frame(src_actions, 0, c, flip_x=True) for c in range(4)]
    skill_right = [extract_frame(src_actions, 0, c, flip_x=False) for c in range(4)]
    skill_up = [extract_frame(src_attack, 3, c) for c in range(4)]
    build_sheet([skill_down, skill_left, skill_right, skill_up], f"{p4_dir}/Hammer_Skill.png")
    create_texture_meta(f"{p4_dir}/Hammer_Skill.png", 4, 4)

    # 3. HAMMER_WALK
    walk_down = [extract_frame(src_walk, 0, c) for c in range(4)]
    walk_left = [extract_frame(src_walk, 1, c, flip_x=True) for c in range(4)]
    walk_right = [extract_frame(src_walk, 1, c, flip_x=False) for c in range(4)]
    walk_up = [extract_frame(src_walk, 2, c) for c in range(4)]
    build_sheet([walk_down, walk_left, walk_right, walk_up], f"{p4_dir}/Hammer_Walk.png")
    create_texture_meta(f"{p4_dir}/Hammer_Walk.png", 4, 4)

    # 4. HAMMER_RUN
    build_sheet([walk_down, walk_left, walk_right, walk_up], f"{p4_dir}/Hammer_Run.png")
    create_texture_meta(f"{p4_dir}/Hammer_Run.png", 4, 4)

    # 5. HAMMER_IDLE
    idle_down = [extract_frame(src_walk, 0, 0), extract_frame(src_walk, 0, 1), extract_frame(src_walk, 0, 0), extract_frame(src_walk, 0, 2)]
    idle_left = [extract_frame(src_walk, 1, 0, flip_x=True), extract_frame(src_walk, 1, 1, flip_x=True), extract_frame(src_walk, 1, 0, flip_x=True), extract_frame(src_walk, 1, 2, flip_x=True)]
    idle_right = [extract_frame(src_walk, 1, 0), extract_frame(src_walk, 1, 1), extract_frame(src_walk, 1, 0), extract_frame(src_walk, 1, 2)]
    idle_up = [extract_frame(src_walk, 2, 0), extract_frame(src_walk, 2, 1), extract_frame(src_walk, 2, 0), extract_frame(src_walk, 2, 2)]
    build_sheet([idle_down, idle_left, idle_right, idle_up], f"{p4_dir}/Hammer_Idle.png")
    create_texture_meta(f"{p4_dir}/Hammer_Idle.png", 4, 4)

    # 6. HAMMER_HURT (1 row x 4 frames)
    hurt_row = [extract_frame(src_actions, 2, c) for c in range(4)]
    build_sheet([hurt_row], f"{p4_dir}/Hammer_Hurt.png")
    create_texture_meta(f"{p4_dir}/Hammer_Hurt.png", 4, 1)

    # 7. HAMMER_DEATH (1 row x 4 frames)
    death_row = [extract_frame(src_actions, 3, c) for c in range(4)]
    build_sheet([death_row], f"{p4_dir}/Hammer_Death.png")
    create_texture_meta(f"{p4_dir}/Hammer_Death.png", 4, 1)

    # 8. HERO_PLAYER4_PORTRAIT.PNG (256x256 UI Portrait)
    portrait_frame = extract_frame(src_walk, 0, 0, target_size=(256, 256))
    safe_save_image(portrait_frame, f"{p4_dir}/UI/Hero_Player4_Portrait.png")
    create_texture_meta(f"{p4_dir}/UI/Hero_Player4_Portrait.png", 1, 1, frame_w=256, frame_h=256, ppu=100)

    # Generate GUIDs
    guid_idle = hashlib.md5(f"{p4_dir}/Hammer_Idle.png".lower().encode('utf-8')).hexdigest()
    guid_walk = hashlib.md5(f"{p4_dir}/Hammer_Walk.png".lower().encode('utf-8')).hexdigest()
    guid_run = hashlib.md5(f"{p4_dir}/Hammer_Run.png".lower().encode('utf-8')).hexdigest()
    guid_attack = hashlib.md5(f"{p4_dir}/Hammer_Attack.png".lower().encode('utf-8')).hexdigest()
    guid_skill = hashlib.md5(f"{p4_dir}/Hammer_Skill.png".lower().encode('utf-8')).hexdigest()
    guid_hurt = hashlib.md5(f"{p4_dir}/Hammer_Hurt.png".lower().encode('utf-8')).hexdigest()
    guid_death = hashlib.md5(f"{p4_dir}/Hammer_Death.png".lower().encode('utf-8')).hexdigest()

    def get_4x4_refs(guid, stem, row_idx):
        refs = []
        for col_idx in range(4):
            sname = f"{stem}_{row_idx}_{col_idx}"
            fid = generate_file_id(sname)
            refs.append((fid, guid))
        return refs

    def get_1x4_refs(guid, stem):
        refs = []
        for col_idx in range(4):
            sname = f"{stem}_0_{col_idx}"
            fid = generate_file_id(sname)
            refs.append((fid, guid))
        return refs

    anims = {
        "Player4_Idle_Down": (get_4x4_refs(guid_idle, "Hammer_Idle", 0), 6.0, True, False),
        "Player4_Idle_Left": (get_4x4_refs(guid_idle, "Hammer_Idle", 1), 6.0, True, False),
        "Player4_Idle_Right": (get_4x4_refs(guid_idle, "Hammer_Idle", 2), 6.0, True, False),
        "Player4_Idle_Up": (get_4x4_refs(guid_idle, "Hammer_Idle", 3), 6.0, True, False),

        "Player4_Walk_Down": (get_4x4_refs(guid_walk, "Hammer_Walk", 0), 8.0, True, False),
        "Player4_Walk_Left": (get_4x4_refs(guid_walk, "Hammer_Walk", 1), 8.0, True, False),
        "Player4_Walk_Right": (get_4x4_refs(guid_walk, "Hammer_Walk", 2), 8.0, True, False),
        "Player4_Walk_Up": (get_4x4_refs(guid_walk, "Hammer_Walk", 3), 8.0, True, False),

        "Player4_Run_Down": (get_4x4_refs(guid_run, "Hammer_Run", 0), 10.0, True, False),
        "Player4_Run_Left": (get_4x4_refs(guid_run, "Hammer_Run", 1), 10.0, True, False),
        "Player4_Run_Right": (get_4x4_refs(guid_run, "Hammer_Run", 2), 10.0, True, False),
        "Player4_Run_Up": (get_4x4_refs(guid_run, "Hammer_Run", 3), 10.0, True, False),

        "Player4_Attack_Down": (get_4x4_refs(guid_attack, "Hammer_Attack", 0), 8.0, False, False),
        "Player4_Attack_Left": (get_4x4_refs(guid_attack, "Hammer_Attack", 1), 8.0, False, False),
        "Player4_Attack_Right": (get_4x4_refs(guid_attack, "Hammer_Attack", 2), 8.0, False, False),
        "Player4_Attack_Up": (get_4x4_refs(guid_attack, "Hammer_Attack", 3), 8.0, False, False),

        "Player4_Skill_Down": (get_4x4_refs(guid_skill, "Hammer_Skill", 0), 7.0, False, False),
        "Player4_Skill_Left": (get_4x4_refs(guid_skill, "Hammer_Skill", 1), 7.0, False, False),
        "Player4_Skill_Right": (get_4x4_refs(guid_skill, "Hammer_Skill", 2), 7.0, False, False),
        "Player4_Skill_Up": (get_4x4_refs(guid_skill, "Hammer_Skill", 3), 7.0, False, False),

        "Player4_Hurt": (get_1x4_refs(guid_hurt, "Hammer_Hurt"), 8.0, False, False),
        "Player4_Death": (get_1x4_refs(guid_death, "Hammer_Death"), 4.0, False, False),
    }

    for clip_name, (refs, fps, loop, flip) in anims.items():
        anim_yaml = make_clip_yaml(clip_name, refs, fps, loop, flip)
        anim_path = f"{anim_dir}/{clip_name}.anim"
        with open(anim_path, "w", encoding="utf-8") as f:
            f.write(anim_yaml)
        
        guid = hashlib.md5(anim_path.lower().encode('utf-8')).hexdigest()
        meta_path = anim_path + ".meta"
        with open(meta_path, "w", encoding="utf-8") as f:
            f.write(f"""fileFormatVersion: 2
guid: {guid}
AnimationClipImporter:
  serializedVersion: 1
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""")

    print("All perfectly coherent sheets, portraits, and animation clips generated successfully!")

if __name__ == "__main__":
    main()
