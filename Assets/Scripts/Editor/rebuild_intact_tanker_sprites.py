import os
import hashlib
from collections import deque
from PIL import Image

def floodfill_remove_bg(crop):
    w, h = crop.size
    pix = crop.load()
    
    def is_outer_bg(x, y):
        r, g, b = pix[x, y][:3]
        # Light gray / white background squares
        if r > 180 and g > 180 and b > 180 and abs(r - g) < 14 and abs(g - b) < 14:
            return True
        # Mid-gray squares if any
        if 105 <= r <= 165 and abs(r - g) < 10 and abs(g - b) < 10:
            return True
        return False

    visited = [[False]*w for _ in range(h)]
    is_bg_map = [[False]*w for _ in range(h)]
    queue = deque()

    # Border seed pixels
    for x in range(w):
        for y in range(4):
            if is_outer_bg(x, y) and not visited[y][x]:
                visited[y][x] = True
                queue.append((x, y))
            if is_outer_bg(x, h - 1 - y) and not visited[h - 1 - y][x]:
                visited[h - 1 - y][x] = True
                queue.append((x, h - 1 - y))
                
    for y in range(h):
        for x in range(4):
            if is_outer_bg(x, y) and not visited[y][x]:
                visited[y][x] = True
                queue.append((x, y))
            if is_outer_bg(w - 1 - x, y) and not visited[y][w - 1 - x]:
                visited[y][w - 1 - x] = True
                queue.append((w - 1 - x, y))

    while queue:
        cx, cy = queue.popleft()
        is_bg_map[cy][cx] = True
        for nx, ny in [(cx+1, cy), (cx-1, cy), (cx, cy+1), (cx, cy-1)]:
            if 0 <= nx < w and 0 <= ny < h and not visited[ny][nx]:
                if is_outer_bg(nx, ny):
                    visited[ny][nx] = True
                    queue.append((nx, ny))

    out = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    opix = out.load()
    for y in range(h):
        for x in range(w):
            if not is_bg_map[y][x]:
                opix[x, y] = (pix[x, y][0], pix[x, y][1], pix[x, y][2], 255)
    return out

def extract_intact_sprite(img, row, col, target_size=(128, 128), flip_x=False):
    x0 = col * 256
    y0 = row * 256
    crop = img.crop((x0, y0, x0 + 256, y0 + 256))
    
    cleaned = floodfill_remove_bg(crop)
    bbox = cleaned.getbbox()
    if not bbox:
        return Image.new('RGBA', target_size, (0, 0, 0, 0))
        
    char_crop = cleaned.crop(bbox)
    if flip_x:
        char_crop = char_crop.transpose(Image.FLIP_LEFT_RIGHT)
        
    tw, th = char_crop.size
    
    # Scale down proportionately so it fits comfortably within 116x116 inside 128x128
    scale = min(116.0 / tw, 116.0 / th, 0.50)
    nw = max(1, int(tw * scale))
    nh = max(1, int(th * scale))
    scaled = char_crop.resize((nw, nh), Image.Resampling.NEAREST)
    
    canvas = Image.new('RGBA', target_size, (0, 0, 0, 0))
    # Center horizontally, place near bottom with 6px safety margin so feet and horns have plenty of room
    fx = (target_size[0] - nw) // 2
    fy = target_size[1] - nh - 6
    if fy < 4:
        fy = 4
    canvas.paste(scaled, (fx, fy), scaled)
    return canvas

def build_grid(frames_by_row, out_path, cell_size=(128, 128)):
    rows = len(frames_by_row)
    cols = max(len(r) for r in frames_by_row)
    grid = Image.new("RGBA", (cols * cell_size[0], rows * cell_size[1]), (0, 0, 0, 0))
    for r_idx, row in enumerate(frames_by_row):
        for c_idx, frame in enumerate(row):
            grid.paste(frame, (c_idx * cell_size[0], r_idx * cell_size[1]), frame)
    grid.save(out_path)
    print(f"Saved sprite sheet: {out_path} ({grid.size[0]}x{grid.size[1]})")

def generate_file_id(name):
    h = hashlib.sha1(name.encode('utf-8')).digest()
    return int.from_bytes(h[:8], byteorder='big', signed=True)

def create_texture_meta(png_path, cols, rows, frame_w=128, frame_h=128, ppu=64):
    filename = os.path.basename(png_path)
    stem = os.path.splitext(filename)[0]
    guid = hashlib.md5(png_path.replace('\\', '/').lower().encode('utf-8')).hexdigest()
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
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpMap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 1
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vramBudgetMB: 1024
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 2
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: {ppu}
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaIsTransparency: 1
  spriteColliderType: 1
  tracksAlpha: 0
  padFiles: 0
  psdSettings:
    layers: []
    layerId: 
  spritesheet:
    serializedVersion: 2
    sprites:
{sprites_str}
    outline: []
    physicsShape: []
    bones: []
    spriteID: 
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
  spritePackingTag: 
  pSDRemoveMatte: 0
  pSDShowRemoveMatteOption: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    with open(meta_path, "w", encoding="utf-8") as f:
        f.write(meta_content)
    return guid, internal_ids

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
    img1_path = "C:/Users/TRUONG HUNG/.gemini/antigravity-ide/brain/11bc86cd-0200-4f80-9a3f-a0f197c9f08b/tanker_4dir_complete_intact_1791308334479.jpg"
    img2_path = "C:/Users/TRUONG HUNG/.gemini/antigravity-ide/brain/11bc86cd-0200-4f80-9a3f-a0f197c9f08b/tanker_actions_skills_death_1791308351781.jpg"

    img1 = Image.open(img1_path).convert("RGBA")
    img2 = Image.open(img2_path).convert("RGBA")

    out_dir = "Assets/ART/Player4"
    ui_dir = "Assets/ART/Player4/UI"
    anim_dir = "Assets/Animation/Player4"
    os.makedirs(out_dir, exist_ok=True)
    os.makedirs(ui_dir, exist_ok=True)
    os.makedirs(anim_dir, exist_ok=True)

    # 1. Extract Walk frames (Down, Left, Right, Up)
    walk_down = [extract_intact_sprite(img1, 0, c) for c in range(4)]
    walk_up = [extract_intact_sprite(img1, 1, c) for c in range(4)]
    walk_left = [extract_intact_sprite(img1, 2, c, flip_x=True) for c in range(4)]
    walk_right = [extract_intact_sprite(img1, 2, c, flip_x=False) for c in range(4)]
    walk_grid = [walk_down, walk_left, walk_right, walk_up]
    build_grid(walk_grid, os.path.join(out_dir, "Hammer_Walk.png"))
    build_grid(walk_grid, os.path.join(out_dir, "Hammer_Run.png"))

    # 2. Extract Idle frames
    idle_down = [walk_down[0], walk_down[1], walk_down[0], walk_down[2]]
    idle_left = [walk_left[0], walk_left[1], walk_left[0], walk_left[2]]
    idle_right = [walk_right[0], walk_right[1], walk_right[0], walk_right[2]]
    idle_up = [walk_up[0], walk_up[1], walk_up[0], walk_up[2]]
    idle_grid = [idle_down, idle_left, idle_right, idle_up]
    build_grid(idle_grid, os.path.join(out_dir, "Hammer_Idle.png"))

    # 3. Extract Attack frames
    atk_down = [extract_intact_sprite(img1, 3, 0), extract_intact_sprite(img1, 3, 1), extract_intact_sprite(img1, 3, 2), extract_intact_sprite(img1, 3, 3)]
    atk_left = [extract_intact_sprite(img1, 3, 2, flip_x=True), extract_intact_sprite(img1, 3, 1, flip_x=True), extract_intact_sprite(img1, 3, 0, flip_x=True), extract_intact_sprite(img1, 3, 3, flip_x=True)]
    atk_right = [extract_intact_sprite(img1, 3, 2, flip_x=False), extract_intact_sprite(img1, 3, 1, flip_x=False), extract_intact_sprite(img1, 3, 0, flip_x=False), extract_intact_sprite(img1, 3, 3, flip_x=False)]
    atk_up = [walk_up[0], extract_intact_sprite(img1, 3, 0), walk_up[2], extract_intact_sprite(img1, 3, 1)]
    atk_grid = [atk_down, atk_left, atk_right, atk_up]
    build_grid(atk_grid, os.path.join(out_dir, "Hammer_Attack.png"))

    # 4. Extract Skill frames (Shield Stance & Ground Roar from img2)
    skill_down = [extract_intact_sprite(img2, 0, c) for c in range(4)]
    skill_left = [extract_intact_sprite(img2, 1, c, flip_x=True) for c in range(4)]
    skill_right = [extract_intact_sprite(img2, 1, c, flip_x=False) for c in range(4)]
    skill_up = [extract_intact_sprite(img2, 0, 3), extract_intact_sprite(img2, 0, 2), extract_intact_sprite(img2, 0, 1), extract_intact_sprite(img2, 0, 0)]
    skill_grid = [skill_down, skill_left, skill_right, skill_up]
    build_grid(skill_grid, os.path.join(out_dir, "Hammer_Skill.png"))

    # 5. Extract Hurt frames (img2 row 2)
    hurt_frames = [extract_intact_sprite(img2, 2, c) for c in range(4)]
    build_grid([hurt_frames], os.path.join(out_dir, "Hammer_Hurt.png"))

    # 6. Extract Death frames (img2 row 3)
    death_frames = [extract_intact_sprite(img2, 3, c) for c in range(4)]
    build_grid([death_frames], os.path.join(out_dir, "Hammer_Death.png"))

    # 7. Avatar Icon
    avatar = extract_intact_sprite(img1, 0, 0, target_size=(128, 128))
    avatar.save(os.path.join(ui_dir, "avatar.png"))
    print("Avatar icon saved!")

    # 8. Rebuild Unity Meta files
    sheets_4x4 = ["Hammer_Idle.png", "Hammer_Walk.png", "Hammer_Run.png", "Hammer_Attack.png", "Hammer_Skill.png"]
    sheets_1x4 = ["Hammer_Hurt.png", "Hammer_Death.png"]
    
    for s in sheets_4x4:
        create_texture_meta(os.path.join(out_dir, s), cols=4, rows=4)
    for s in sheets_1x4:
        create_texture_meta(os.path.join(out_dir, s), cols=4, rows=1)
    create_texture_meta(os.path.join(ui_dir, "avatar.png"), cols=1, rows=1)
    print("Meta files created!")

    # 9. Rebuild Animation clips
    guid_idle = hashlib.md5("Assets/ART/Player4/Hammer_Idle.png".lower().encode('utf-8')).hexdigest()
    guid_walk = hashlib.md5("Assets/ART/Player4/Hammer_Walk.png".lower().encode('utf-8')).hexdigest()
    guid_run = hashlib.md5("Assets/ART/Player4/Hammer_Run.png".lower().encode('utf-8')).hexdigest()
    guid_attack = hashlib.md5("Assets/ART/Player4/Hammer_Attack.png".lower().encode('utf-8')).hexdigest()
    guid_skill = hashlib.md5("Assets/ART/Player4/Hammer_Skill.png".lower().encode('utf-8')).hexdigest()
    guid_hurt = hashlib.md5("Assets/ART/Player4/Hammer_Hurt.png".lower().encode('utf-8')).hexdigest()
    guid_death = hashlib.md5("Assets/ART/Player4/Hammer_Death.png".lower().encode('utf-8')).hexdigest()

    def get_4x4_refs(guid, prefix, row):
        return [(generate_file_id(f"{prefix}_{row}_{c}"), guid) for c in range(4)]
    def get_1x4_refs(guid, prefix):
        return [(generate_file_id(f"{prefix}_0_{c}"), guid) for c in range(4)]

    clips = {
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

        "Player4_Attack_Down": (get_4x4_refs(guid_attack, "Hammer_Attack", 0), 10.0, False, False),
        "Player4_Attack_Left": (get_4x4_refs(guid_attack, "Hammer_Attack", 1), 10.0, False, False),
        "Player4_Attack_Right": (get_4x4_refs(guid_attack, "Hammer_Attack", 2), 10.0, False, False),
        "Player4_Attack_Up": (get_4x4_refs(guid_attack, "Hammer_Attack", 3), 10.0, False, False),

        "Player4_Skill_Down": (get_4x4_refs(guid_skill, "Hammer_Skill", 0), 10.0, False, False),
        "Player4_Skill_Left": (get_4x4_refs(guid_skill, "Hammer_Skill", 1), 10.0, False, False),
        "Player4_Skill_Right": (get_4x4_refs(guid_skill, "Hammer_Skill", 2), 10.0, False, False),
        "Player4_Skill_Up": (get_4x4_refs(guid_skill, "Hammer_Skill", 3), 10.0, False, False),

        "Player4_Hurt": (get_1x4_refs(guid_hurt, "Hammer_Hurt"), 8.0, False, False),
        "Player4_Death": (get_1x4_refs(guid_death, "Hammer_Death"), 6.0, False, False),
    }

    for name, (refs, fps, loop, flip) in clips.items():
        clip_path = os.path.join(anim_dir, f"{name}.anim")
        yaml = make_clip_yaml(name, refs, fps, loop, flip)
        with open(clip_path, "w", encoding="utf-8") as f:
            f.write(yaml)

    print("All intact animation clips regenerated successfully!")

if __name__ == "__main__":
    main()
