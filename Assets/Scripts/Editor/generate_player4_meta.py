import os
import uuid
import hashlib

def generate_file_id(name):
    # Generates a stable deterministic 64-bit int fileID from sprite name
    h = hashlib.sha1(name.encode('utf-8')).digest()
    val = int.from_bytes(h[:8], byteorder='big', signed=True)
    return val

def create_texture_meta(png_path, cols, rows, frame_w=128, frame_h=128, ppu=64):
    filename = os.path.basename(png_path)
    stem = os.path.splitext(filename)[0]
    
    # Deterministic GUID based on relative path
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
    print(f"Generated meta: {meta_path} (guid: {guid})")
    return guid, internal_ids

def main():
    p4_dir = "Assets/ART/Player4"
    sheets_4x4 = ["Hammer_Idle.png", "Hammer_Walk.png", "Hammer_Run.png", "Hammer_Attack.png", "Hammer_Skill.png"]
    sheets_1x4 = ["Hammer_Hurt.png", "Hammer_Death.png"]
    
    data = {}
    for s in sheets_4x4:
        path = os.path.join(p4_dir, s)
        guid, ids = create_texture_meta(path, cols=4, rows=4)
        data[s] = (guid, ids)
        
    for s in sheets_1x4:
        path = os.path.join(p4_dir, s)
        guid, ids = create_texture_meta(path, cols=4, rows=1)
        data[s] = (guid, ids)
        
    # Avatar icon
    avatar_path = "Assets/ART/Player4/UI/avatar.png"
    guid, ids = create_texture_meta(avatar_path, cols=1, rows=1)
    data["avatar.png"] = (guid, ids)
    
    print("All meta files generated successfully!")

if __name__ == "__main__":
    main()
