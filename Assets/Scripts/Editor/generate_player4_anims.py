import os
import hashlib

def generate_file_id(name):
    h = hashlib.sha1(name.encode('utf-8')).digest()
    return int.from_bytes(h[:8], byteorder='big', signed=True)

def make_clip_yaml(clip_name, sprite_refs, frame_rate=8.0, loop=True, flip_x=False):
    # sprite_refs is list of (file_id, guid)
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
    anim_dir = "Assets/Animation/Player4"
    os.makedirs(anim_dir, exist_ok=True)
    
    # GUIDs matching generate_player4_meta.py
    guid_idle = hashlib.md5("Assets/ART/Player4/Hammer_Idle.png".lower().encode('utf-8')).hexdigest()
    guid_walk = hashlib.md5("Assets/ART/Player4/Hammer_Walk.png".lower().encode('utf-8')).hexdigest()
    guid_run = hashlib.md5("Assets/ART/Player4/Hammer_Run.png".lower().encode('utf-8')).hexdigest()
    guid_attack = hashlib.md5("Assets/ART/Player4/Hammer_Attack.png".lower().encode('utf-8')).hexdigest()
    guid_skill = hashlib.md5("Assets/ART/Player4/Hammer_Skill.png".lower().encode('utf-8')).hexdigest()
    guid_hurt = hashlib.md5("Assets/ART/Player4/Hammer_Hurt.png".lower().encode('utf-8')).hexdigest()
    guid_death = hashlib.md5("Assets/ART/Player4/Hammer_Death.png".lower().encode('utf-8')).hexdigest()
    
    # Helper to get row sprites (row 0 = Down, row 1 = Left, row 2 = Right, row 3 = Up)
    def get_4x4_refs(guid, prefix, row):
        return [(generate_file_id(f"{prefix}_{row}_{c}"), guid) for c in range(4)]
        
    def get_1x4_refs(guid, prefix):
        return [(generate_file_id(f"{prefix}_0_{c}"), guid) for c in range(4)]
        
    clips = {
        # Idle
        "Player4_Idle_Down": (get_4x4_refs(guid_idle, "Hammer_Idle", 0), 6.0, True, False),
        "Player4_Idle_Left": (get_4x4_refs(guid_idle, "Hammer_Idle", 1), 6.0, True, False),
        "Player4_Idle_Right": (get_4x4_refs(guid_idle, "Hammer_Idle", 2), 6.0, True, False),
        "Player4_Idle_Up": (get_4x4_refs(guid_idle, "Hammer_Idle", 3), 6.0, True, False),
        
        # Walk
        "Player4_Walk_Down": (get_4x4_refs(guid_walk, "Hammer_Walk", 0), 8.0, True, False),
        "Player4_Walk_Left": (get_4x4_refs(guid_walk, "Hammer_Walk", 1), 8.0, True, False),
        "Player4_Walk_Right": (get_4x4_refs(guid_walk, "Hammer_Walk", 2), 8.0, True, False),
        "Player4_Walk_Up": (get_4x4_refs(guid_walk, "Hammer_Walk", 3), 8.0, True, False),

        # Run
        "Player4_Run_Down": (get_4x4_refs(guid_run, "Hammer_Run", 0), 10.0, True, False),
        "Player4_Run_Left": (get_4x4_refs(guid_run, "Hammer_Run", 1), 10.0, True, False),
        "Player4_Run_Right": (get_4x4_refs(guid_run, "Hammer_Run", 2), 10.0, True, False),
        "Player4_Run_Up": (get_4x4_refs(guid_run, "Hammer_Run", 3), 10.0, True, False),

        # Attack
        "Player4_Attack_Down": (get_4x4_refs(guid_attack, "Hammer_Attack", 0), 10.0, False, False),
        "Player4_Attack_Left": (get_4x4_refs(guid_attack, "Hammer_Attack", 1), 10.0, False, False),
        "Player4_Attack_Right": (get_4x4_refs(guid_attack, "Hammer_Attack", 2), 10.0, False, False),
        "Player4_Attack_Up": (get_4x4_refs(guid_attack, "Hammer_Attack", 3), 10.0, False, False),

        # Skill (Shield Stance / Slam)
        "Player4_Skill_Down": (get_4x4_refs(guid_skill, "Hammer_Skill", 0), 10.0, False, False),
        "Player4_Skill_Left": (get_4x4_refs(guid_skill, "Hammer_Skill", 1), 10.0, False, False),
        "Player4_Skill_Right": (get_4x4_refs(guid_skill, "Hammer_Skill", 2), 10.0, False, False),
        "Player4_Skill_Up": (get_4x4_refs(guid_skill, "Hammer_Skill", 3), 10.0, False, False),

        # Hurt & Death
        "Player4_Hurt": (get_1x4_refs(guid_hurt, "Hammer_Hurt"), 8.0, False, False),
        "Player4_Death": (get_1x4_refs(guid_death, "Hammer_Death"), 6.0, False, False),
    }

    for name, (refs, fps, loop, flip) in clips.items():
        clip_path = os.path.join(anim_dir, f"{name}.anim")
        yaml = make_clip_yaml(name, refs, fps, loop, flip)
        with open(clip_path, "w", encoding="utf-8") as f:
            f.write(yaml)
        print(f"Generated animation clip: {clip_path}")

if __name__ == "__main__":
    main()
