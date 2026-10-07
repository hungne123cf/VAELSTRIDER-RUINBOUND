import os
import hashlib
from PIL import Image

def generate_file_id(name):
    h = hashlib.sha1(name.encode('utf-8')).digest()
    return int.from_bytes(h[:8], byteorder='big', signed=True)

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

def create_texture_meta(png_path, cols, rows, frame_w=128, frame_h=128, ppu=28, pivot_x=0.5, pivot_y=0.2):
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
      alignment: 9
      pivot: {{x: {pivot_x}, y: {pivot_y}}}
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
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 0
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
  alignment: 9
  spritePivot: {{x: {pivot_x}, y: {pivot_y}}}
  spritePixelsToUnits: {ppu}
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Standalone
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Android
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: WebGL
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites:
{sprites_str}
  spritePackingTag: 
  pSDRemoveMatte: 0
  pSDShowRemoveMatteOption: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
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
        tangentMode: 136
        weightedMode: 0
        inWeight: 0.33333334
        outWeight: 0.33333334
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
      attribute: 555744692
      script: {{fileID: 0}}
      typeID: 212
      customType: 0
      isPPtrCurve: 0
      isIntCurve: 0
      isSerializeReferenceCurve: 0
    - serializedVersion: 2
      path: 0
      attribute: 0
      script: {{fileID: 0}}
      typeID: 212
      customType: 23
      isPPtrCurve: 1
      isIntCurve: 0
      isSerializeReferenceCurve: 0
    pptrCurveMapping:
{keyframes_block.replace("    - time:", "    -").replace("      value:", "")}
  m_AnimationClipSettings:
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
    m_LoopBlend: {loop_val}
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
    src_path = 'Assets/ART/Player4/Minotaur - Sprite Sheet.png'
    if not os.path.exists(src_path):
        src_path = 'C:/Users/TRUONG HUNG/.gemini/antigravity-ide/brain/11bc86cd-0200-4f80-9a3f-a0f197c9f08b/.user_uploaded/media_1791310307044.png'
    
    im = Image.open(src_path).convert('RGBA')

    p4_dir = "Assets/ART/Player4"
    anim_dir = "Assets/Animation/Player4"
    os.makedirs(p4_dir, exist_ok=True)
    os.makedirs(os.path.join(p4_dir, "UI"), exist_ok=True)
    os.makedirs(anim_dir, exist_ok=True)

    row_slices = [
        (22, 64),    # 0: Idle R (5 frames)
        (116, 160),  # 1: Walk R (8 frames)
        (215, 256),  # 2: Guard Charge / Sprint R (5 frames)
        (293, 358),  # 3: Attack Slash R (9 frames)
        (406, 448),  # 4: Bash R (5 frames)
        (490, 544),  # 5: Slam R (6 frames)
        (599, 646),  # 6: Spin R (9 frames)
        (695, 736),  # 7: Block R (3 frames)
        (791, 832),  # 8: Hurt R (3 frames)
        (887, 929),  # 9: Death R (6 frames)
        (982, 1024), # 10: Idle L (5 frames)
        (1076, 1120),# 11: Walk L (8 frames)
        (1175, 1216),# 12: Guard Charge / Sprint L (5 frames)
        (1253, 1318),# 13: Attack Slash L (9 frames)
        (1366, 1408),# 14: Bash L (5 frames)
        (1450, 1504),# 15: Slam L (6 frames)
        (1559, 1606),# 16: Spin L (8 frames)
        (1655, 1696),# 17: Block L (3 frames)
        (1751, 1792),# 18: Hurt L (3 frames)
        (1847, 1889),# 19: Death L (6 frames)
    ]

    def extract_row(r_idx):
        y0, y1 = row_slices[r_idx]
        crop = im.crop((0, y0, im.width, y1))
        col_has_px = [any(crop.getpixel((x, y))[3] > 10 for y in range(y1-y0)) for x in range(im.width)]
        cols = []
        in_c = False
        sx = 0
        for x, hp in enumerate(col_has_px):
            if hp and not in_c:
                in_c = True
                sx = x
            elif not hp and in_c:
                in_c = False
                cols.append((sx, x))
        if in_c: cols.append((sx, im.width))
        
        merged = []
        for c in cols:
            if not merged:
                merged.append(c)
            else:
                prev_w = merged[-1][1] - merged[-1][0]
                curr_w = c[1] - c[0]
                gap = c[0] - merged[-1][1]
                if gap <= 3 or (gap <= 6 and (prev_w < 5 or curr_w < 5)):
                    merged[-1] = (merged[-1][0], c[1])
                else:
                    merged.append(c)
        
        frames = []
        for x0, x1 in merged:
            if (x1 - x0) <= 5: continue
            f_crop = im.crop((x0, y0, x1, y1))
            bb = f_crop.getbbox()
            if bb:
                raw_frame = f_crop.crop(bb)
                armored_frame = apply_silver_knight_armor(raw_frame)
                frames.append(armored_frame)
        return frames

    def apply_silver_knight_armor(frame):
        # Frame already contains high-definition silver armor plates, royal red cape, and gold accents
        return frame.copy()

    # Cell size 128x128 with grounded feet at bottom_padding = 24px
    CELL_W, CELL_H = 128, 128
    BOTTOM_PAD = 24
    PIVOT_Y = BOTTOM_PAD / CELL_H # 24 / 128 = 0.1875
    PPU = 28 # Perfectly scaled to match Player 1 in world units

    def fit_to_cell(frame, cell_size=(CELL_W, CELL_H), bottom_padding=BOTTOM_PAD):
        cw, ch = cell_size
        tw, th = frame.size
        canvas = Image.new('RGBA', (cw, ch), (0, 0, 0, 0))
        fx = (cw - tw) // 2
        fy = ch - th - bottom_padding
        if fy < 2: fy = 2
        canvas.paste(frame, (fx, fy), frame)
        return canvas

    def make_sheet(row_list, cols_count, cell_size=(CELL_W, CELL_H)):
        rows_count = len(row_list)
        sheet = Image.new('RGBA', (cols_count * cell_size[0], rows_count * cell_size[1]), (0, 0, 0, 0))
        for r_idx, frames in enumerate(row_list):
            for c_idx in range(cols_count):
                if c_idx < len(frames):
                    cell = fit_to_cell(frames[c_idx], cell_size)
                else:
                    cell = fit_to_cell(frames[-1], cell_size)
                sheet.paste(cell, (c_idx * cell_size[0], r_idx * cell_size[1]), cell)
        return sheet

    # Extract rows
    idle_r = extract_row(0)
    idle_l = extract_row(10)
    walk_r = extract_row(1)
    walk_l = extract_row(11)
    run_r = extract_row(2)
    run_l = extract_row(12)
    atk_r = extract_row(3)
    atk_l = extract_row(13)
    bash_r = extract_row(4)
    bash_l = extract_row(14)
    slam_r = extract_row(5)
    slam_l = extract_row(15)
    spin_r = extract_row(6)
    spin_l = extract_row(16)
    block_r = extract_row(7)
    block_l = extract_row(17)
    hurt_r = extract_row(8)
    death_r = extract_row(9)

    back_frame = spin_r[4]

    # 1. Idle (Row 0: Down->R, Row 1: L, Row 2: R, Row 3: Up->R)
    idle_sheet = make_sheet([idle_r, idle_l, idle_r, idle_r], 5)
    safe_save_image(idle_sheet, f"{p4_dir}/Hammer_Idle.png")
    create_texture_meta(f"{p4_dir}/Hammer_Idle.png", 5, 4, CELL_W, CELL_H, PPU, 0.5, PIVOT_Y)

    # 2. Walk
    walk_sheet = make_sheet([walk_r, walk_l, walk_r, walk_r], 8)
    safe_save_image(walk_sheet, f"{p4_dir}/Hammer_Walk.png")
    create_texture_meta(f"{p4_dir}/Hammer_Walk.png", 8, 4, CELL_W, CELL_H, PPU, 0.5, PIVOT_Y)

    # 3. Run
    run_sheet = make_sheet([run_r, run_l, run_r, run_r], 5)
    safe_save_image(run_sheet, f"{p4_dir}/Hammer_Run.png")
    create_texture_meta(f"{p4_dir}/Hammer_Run.png", 5, 4, CELL_W, CELL_H, PPU, 0.5, PIVOT_Y)

    # 4. Attack 1
    atk_sheet = make_sheet([atk_r, atk_l, atk_r, atk_r], 9)
    safe_save_image(atk_sheet, f"{p4_dir}/Hammer_Attack.png")
    create_texture_meta(f"{p4_dir}/Hammer_Attack.png", 9, 4, CELL_W, CELL_H, PPU, 0.5, PIVOT_Y)

    # 5. Attack 2 / Bash
    bash_sheet = make_sheet([bash_r, bash_l, bash_r, bash_r], 5)
    safe_save_image(bash_sheet, f"{p4_dir}/Hammer_Attack2.png")
    create_texture_meta(f"{p4_dir}/Hammer_Attack2.png", 5, 4, CELL_W, CELL_H, PPU, 0.5, PIVOT_Y)

    # 6. Skill
    slam_sheet = make_sheet([slam_r, slam_l, slam_r, slam_r], 6)
    safe_save_image(slam_sheet, f"{p4_dir}/Hammer_Skill.png")
    create_texture_meta(f"{p4_dir}/Hammer_Skill.png", 6, 4, CELL_W, CELL_H, PPU, 0.5, PIVOT_Y)

    # 7. Spin Whirlwind (9 frames)
    spin_sheet = make_sheet([spin_r, spin_l, spin_r, spin_r], 9)
    safe_save_image(spin_sheet, f"{p4_dir}/Hammer_Spin.png")
    create_texture_meta(f"{p4_dir}/Hammer_Spin.png", 9, 4, CELL_W, CELL_H, PPU, 0.5, PIVOT_Y)

    # 8. Block / Guard
    block_sheet = make_sheet([block_r, block_l, block_r, block_r], 3)
    safe_save_image(block_sheet, f"{p4_dir}/Hammer_Block.png")
    create_texture_meta(f"{p4_dir}/Hammer_Block.png", 3, 4, CELL_W, CELL_H, PPU, 0.5, PIVOT_Y)

    # 9. Hurt
    hurt_sheet = make_sheet([hurt_r], 3)
    safe_save_image(hurt_sheet, f"{p4_dir}/Hammer_Hurt.png")
    create_texture_meta(f"{p4_dir}/Hammer_Hurt.png", 3, 1, CELL_W, CELL_H, PPU, 0.5, PIVOT_Y)

    # 10. Death
    death_sheet = make_sheet([death_r], 6)
    safe_save_image(death_sheet, f"{p4_dir}/Hammer_Death.png")
    create_texture_meta(f"{p4_dir}/Hammer_Death.png", 6, 1, CELL_W, CELL_H, PPU, 0.5, PIVOT_Y)

    # 11. Portrait
    portrait_raw = idle_r[0]
    p_bbox = portrait_raw.getbbox()
    p_cropped = portrait_raw.crop(p_bbox) if p_bbox else portrait_raw
    pw, ph = p_cropped.size
    target_max = 92.0
    scale = min(target_max / pw, target_max / ph)
    nw, nh = int(round(pw * scale)), int(round(ph * scale))
    scaled_portrait = p_cropped.resize((nw, nh), Image.Resampling.NEAREST)
    portrait_canvas = Image.new('RGBA', (128, 128), (0, 0, 0, 0))
    portrait_canvas.paste(scaled_portrait, ((128 - nw) // 2, (128 - nh) // 2), scaled_portrait)
    safe_save_image(portrait_canvas, f"{p4_dir}/UI/Hero_Player4_Portrait.png")
    create_texture_meta(f"{p4_dir}/UI/Hero_Player4_Portrait.png", 1, 1, 128, 128, 100)

    # GUIDs
    guid_idle = hashlib.md5(f"{p4_dir}/Hammer_Idle.png".lower().encode('utf-8')).hexdigest()
    guid_walk = hashlib.md5(f"{p4_dir}/Hammer_Walk.png".lower().encode('utf-8')).hexdigest()
    guid_run = hashlib.md5(f"{p4_dir}/Hammer_Run.png".lower().encode('utf-8')).hexdigest()
    guid_attack = hashlib.md5(f"{p4_dir}/Hammer_Attack.png".lower().encode('utf-8')).hexdigest()
    guid_attack2 = hashlib.md5(f"{p4_dir}/Hammer_Attack2.png".lower().encode('utf-8')).hexdigest()
    guid_skill = hashlib.md5(f"{p4_dir}/Hammer_Skill.png".lower().encode('utf-8')).hexdigest()
    guid_spin = hashlib.md5(f"{p4_dir}/Hammer_Spin.png".lower().encode('utf-8')).hexdigest()
    guid_block = hashlib.md5(f"{p4_dir}/Hammer_Block.png".lower().encode('utf-8')).hexdigest()
    guid_hurt = hashlib.md5(f"{p4_dir}/Hammer_Hurt.png".lower().encode('utf-8')).hexdigest()
    guid_death = hashlib.md5(f"{p4_dir}/Hammer_Death.png".lower().encode('utf-8')).hexdigest()

    def get_refs(guid, stem, row_idx, count):
        refs = []
        for col_idx in range(count):
            sname = f"{stem}_{row_idx}_{col_idx}"
            fid = generate_file_id(sname)
            refs.append((fid, guid))
        return refs

    anims = {
        "Player4_Idle_Down": (get_refs(guid_idle, "Hammer_Idle", 2, 5), 6.0, True, False),
        "Player4_Idle_Left": (get_refs(guid_idle, "Hammer_Idle", 1, 5), 6.0, True, False),
        "Player4_Idle_Right": (get_refs(guid_idle, "Hammer_Idle", 2, 5), 6.0, True, False),
        "Player4_Idle_Up": (get_refs(guid_idle, "Hammer_Idle", 2, 5), 6.0, True, False),

        "Player4_Walk_Down": (get_refs(guid_walk, "Hammer_Walk", 2, 8), 12.0, True, False),
        "Player4_Walk_Left": (get_refs(guid_walk, "Hammer_Walk", 1, 8), 12.0, True, False),
        "Player4_Walk_Right": (get_refs(guid_walk, "Hammer_Walk", 2, 8), 12.0, True, False),
        "Player4_Walk_Up": (get_refs(guid_walk, "Hammer_Walk", 2, 8), 12.0, True, False),

        "Player4_Run_Down": (get_refs(guid_run, "Hammer_Run", 2, 5), 10.0, True, False),
        "Player4_Run_Left": (get_refs(guid_run, "Hammer_Run", 1, 5), 10.0, True, False),
        "Player4_Run_Right": (get_refs(guid_run, "Hammer_Run", 2, 5), 10.0, True, False),
        "Player4_Run_Up": (get_refs(guid_run, "Hammer_Run", 2, 5), 10.0, True, False),

        "Player4_Attack_Down": (get_refs(guid_attack, "Hammer_Attack", 2, 9), 12.0, False, False),
        "Player4_Attack_Left": (get_refs(guid_attack, "Hammer_Attack", 1, 9), 12.0, False, False),
        "Player4_Attack_Right": (get_refs(guid_attack, "Hammer_Attack", 2, 9), 12.0, False, False),
        "Player4_Attack_Up": (get_refs(guid_attack, "Hammer_Attack", 2, 9), 12.0, False, False),

        "Player4_Attack2_Down": (get_refs(guid_attack2, "Hammer_Attack2", 2, 5), 10.0, False, False),
        "Player4_Attack2_Left": (get_refs(guid_attack2, "Hammer_Attack2", 1, 5), 10.0, False, False),
        "Player4_Attack2_Right": (get_refs(guid_attack2, "Hammer_Attack2", 2, 5), 10.0, False, False),
        "Player4_Attack2_Up": (get_refs(guid_attack2, "Hammer_Attack2", 2, 5), 10.0, False, False),

        "Player4_Skill_Down": (get_refs(guid_skill, "Hammer_Skill", 2, 6), 7.5, False, False),
        "Player4_Skill_Left": (get_refs(guid_skill, "Hammer_Skill", 1, 6), 7.5, False, False),
        "Player4_Skill_Right": (get_refs(guid_skill, "Hammer_Skill", 2, 6), 7.5, False, False),
        "Player4_Skill_Up": (get_refs(guid_skill, "Hammer_Skill", 2, 6), 7.5, False, False),

        "Player4_Spin_Down": (get_refs(guid_spin, "Hammer_Spin", 2, 9), 14.0, True, False),
        "Player4_Spin_Left": (get_refs(guid_spin, "Hammer_Spin", 1, 8), 14.0, True, False),
        "Player4_Spin_Right": (get_refs(guid_spin, "Hammer_Spin", 2, 9), 14.0, True, False),
        "Player4_Spin_Up": (get_refs(guid_spin, "Hammer_Spin", 2, 9), 14.0, True, False),

        "Player4_Block_Down": (get_refs(guid_block, "Hammer_Block", 2, 3), 6.0, True, False),
        "Player4_Block_Left": (get_refs(guid_block, "Hammer_Block", 1, 3), 6.0, True, False),
        "Player4_Block_Right": (get_refs(guid_block, "Hammer_Block", 2, 3), 6.0, True, False),
        "Player4_Block_Up": (get_refs(guid_block, "Hammer_Block", 2, 3), 6.0, True, False),

        "Player4_Hurt": (get_refs(guid_hurt, "Hammer_Hurt", 0, 3), 8.0, False, False),
        "Player4_Death": (get_refs(guid_death, "Hammer_Death", 0, 6), 8.0, False, False),

        "Player4_Hurt_Down": (get_refs(guid_hurt, "Hammer_Hurt", 0, 3), 8.0, False, False),
        "Player4_Hurt_Left": (get_refs(guid_hurt, "Hammer_Hurt", 0, 3), 8.0, False, True),
        "Player4_Hurt_Right": (get_refs(guid_hurt, "Hammer_Hurt", 0, 3), 8.0, False, False),
        "Player4_Hurt_Up": (get_refs(guid_hurt, "Hammer_Hurt", 0, 3), 8.0, False, False),

        "Player4_Death_Down": (get_refs(guid_death, "Hammer_Death", 0, 6), 8.0, False, False),
        "Player4_Death_Left": (get_refs(guid_death, "Hammer_Death", 0, 6), 8.0, False, True),
        "Player4_Death_Right": (get_refs(guid_death, "Hammer_Death", 0, 6), 8.0, False, False),
        "Player4_Death_Up": (get_refs(guid_death, "Hammer_Death", 0, 6), 8.0, False, False),
    }

    clip_guids = {}
    for anim_name, (refs, fps, loop, flip) in anims.items():
        anim_path = f"{anim_dir}/{anim_name}.anim"
        yaml_data = make_clip_yaml(anim_name, refs, fps, loop, flip)
        with open(anim_path, "w", encoding="utf-8") as f:
            f.write(yaml_data)
        guid = hashlib.md5(anim_path.lower().encode('utf-8')).hexdigest()
        clip_guids[anim_name] = guid
        meta_path = anim_path + ".meta"
        with open(meta_path, "w", encoding="utf-8") as f:
            f.write(f"fileFormatVersion: 2\nguid: {guid}\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")

    print(f"Generated {len(anims)} distinct animation clips successfully with PPU={PPU} and Cell={CELL_W}x{CELL_H}!")

if __name__ == "__main__":
    main()
