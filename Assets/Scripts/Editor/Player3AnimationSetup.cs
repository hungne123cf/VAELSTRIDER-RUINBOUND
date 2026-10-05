using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Linq;
using System.Collections.Generic;

[InitializeOnLoad]
public class Player3AnimationSetup
{
    public const string ART_DIR = "Assets/ART/Player3";
    public const string ANIM_DIR = "Assets/Animation/Player3";
    public const string CONTROLLER_PATH = ANIM_DIR + "/Player3.controller";

    static Player3AnimationSetup()
    {
        EditorApplication.delayCall += CheckAndSetup;
    }

    [MenuItem("Vaelstrider/Setup Player 3 (Azure Blade) Animations")]
    public static void CheckAndSetup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        if (!Directory.Exists(ANIM_DIR))
        {
            Directory.CreateDirectory(ANIM_DIR);
            AssetDatabase.Refresh();
        }

        CreateAllAnimationsAndController();
    }

    [MenuItem("Vaelstrider/Force Regenerate Player 3 Animations")]
    public static void ForceRegenerate()
    {
        if (!Directory.Exists(ANIM_DIR))
        {
            Directory.CreateDirectory(ANIM_DIR);
            AssetDatabase.Refresh();
        }
        CreateAllAnimationsAndController();
    }

    public static void SliceSpriteSheet(string path, int frameWidth = 64, int frameHeight = 64)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.spritePixelsPerUnit = 32;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.isReadable = true;

        // Load texture to get dimensions
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture != null)
        {
            int texW = texture.width;
            int texH = texture.height;
            int cols = texW / frameWidth;
            int rows = texH / frameHeight;

            List<SpriteMetaData> metas = new List<SpriteMetaData>();
            for (int r = 0; r < rows; r++)
            {
                // In Unity texture coordinates, y=0 is bottom
                int unityY = (rows - 1 - r) * frameHeight;
                for (int c = 0; c < cols; c++)
                {
                    SpriteMetaData meta = new SpriteMetaData();
                    meta.rect = new Rect(c * frameWidth, unityY, frameWidth, frameHeight);
                    meta.name = $"{Path.GetFileNameWithoutExtension(path)}_{r}_{c}";
                    meta.alignment = (int)SpriteAlignment.Center;
                    meta.pivot = new Vector2(0.5f, 0.5f);
                    metas.Add(meta);
                }
            }
            importer.spritesheet = metas.ToArray();
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }
    }

    public static Sprite[] LoadOrderedSprites(string path)
    {
        var objects = AssetDatabase.LoadAllAssetsAtPath(path);
        if (objects == null) return new Sprite[0];

        List<Sprite> sprites = new List<Sprite>();
        foreach (var obj in objects)
        {
            if (obj is Sprite s) sprites.Add(s);
        }

        // Sort by row (top to bottom) then column (left to right)
        return sprites.OrderByDescending(s => s.rect.y).ThenBy(s => s.rect.x).ToArray();
    }

    public static void CreateAllAnimationsAndController()
    {
        Debug.Log("<color=cyan>[PLAYER 3 SETUP]</color> Đang khởi tạo Animation và Animator Controller cho Lam Kiếm Sĩ (Azure Blade)...");

        string idlePath = ART_DIR + "/Sword_Idle.png";
        string runPath = ART_DIR + "/Sword_Run.png";
        string attackPath = ART_DIR + "/Sword_Attack.png";
        string hurtPath = ART_DIR + "/Sword_Hurt.png";
        string deathPath = ART_DIR + "/Sword_Death.png";
        string walkPath = ART_DIR + "/Sword_Walk.png";
        string runAttackPath = ART_DIR + "/Sword_Run_attack.png";
        string walkAttackPath = ART_DIR + "/Sword_Walk_attack.png";
        string unarmedIdlePath = ART_DIR + "/Unarmed_Idle.png";
        string unarmedRunPath = ART_DIR + "/Unarmed_Run.png";
        string unarmedWalkPath = ART_DIR + "/Unarmed_Walk.png";
        string unarmedHurtPath = ART_DIR + "/Unarmed_Hurt.png";
        string unarmedDeathPath = ART_DIR + "/Unarmed_Death.png";

        // Step 1: Slice all sprite sheets first
        SliceSpriteSheet(idlePath, 64, 64);
        SliceSpriteSheet(runPath, 64, 64);
        SliceSpriteSheet(walkPath, 64, 64);
        SliceSpriteSheet(attackPath, 64, 64);
        SliceSpriteSheet(runAttackPath, 64, 64);
        SliceSpriteSheet(walkAttackPath, 64, 64);
        SliceSpriteSheet(hurtPath, 64, 64);
        SliceSpriteSheet(deathPath, 64, 64);
        SliceSpriteSheet(unarmedIdlePath, 64, 64);
        SliceSpriteSheet(unarmedRunPath, 64, 64);
        SliceSpriteSheet(unarmedWalkPath, 64, 64);
        SliceSpriteSheet(unarmedHurtPath, 64, 64);
        SliceSpriteSheet(unarmedDeathPath, 64, 64);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Step 2: Load slices
        Sprite[] idleSprites = LoadOrderedSprites(idlePath);
        Sprite[] runSprites = LoadOrderedSprites(runPath);
        Sprite[] attackSprites = LoadOrderedSprites(attackPath);
        Sprite[] hurtSprites = LoadOrderedSprites(hurtPath);
        Sprite[] deathSprites = LoadOrderedSprites(deathPath);

        if (idleSprites.Length < 48 || runSprites.Length < 32 || attackSprites.Length < 32)
        {
            Debug.LogError($"[PLAYER 3 SETUP] Số lượng sprite không đủ! Idle={idleSprites.Length}, Run={runSprites.Length}, Attack={attackSprites.Length}");
            return;
        }

        // 4 hướng trong asset pack: 0=Down, 1=Left, 2=Up-Left, 3=Up
        // 1. Idle (12 frames/hướng; riêng hướng Up gốc chỉ có 4 frames hợp lệ, lặp 3 lần để đủ 12 frames chuẩn nhịp)
        Sprite[] idleDown = idleSprites.Skip(0 * 12).Take(12).ToArray();
        Sprite[] idleLeft = idleSprites.Skip(1 * 12).Take(12).ToArray();
        Sprite[] idleUpLeft = idleSprites.Skip(2 * 12).Take(12).ToArray();
        Sprite[] idleUp4 = idleSprites.Skip(3 * 12).Take(4).ToArray();
        Sprite[] idleUp = idleUp4.Concat(idleUp4).Concat(idleUp4).ToArray();

        // 2. Run (8 frames/hướng)
        Sprite[] runDown = runSprites.Skip(0 * 8).Take(8).ToArray();
        Sprite[] runLeft = runSprites.Skip(1 * 8).Take(8).ToArray();
        Sprite[] runUpLeft = runSprites.Skip(2 * 8).Take(8).ToArray();
        Sprite[] runUp = runSprites.Skip(3 * 8).Take(8).ToArray();

        // 3. Attack (8 frames/hướng)
        Sprite[] attackDown = attackSprites.Skip(0 * 8).Take(8).ToArray();
        Sprite[] attackLeft = attackSprites.Skip(1 * 8).Take(8).ToArray();
        Sprite[] attackUpLeft = attackSprites.Skip(2 * 8).Take(8).ToArray();
        Sprite[] attackUp = attackSprites.Skip(3 * 8).Take(8).ToArray();

        // Tạo Clips (flipX = true khi sang Right để lật hình chuẩn xác)
        AnimationClip clipIdleDown = CreateClip("Player3_Idle_Down", idleDown, 8f, true, false);
        AnimationClip clipIdleLeft = CreateClip("Player3_Idle_Left", idleLeft, 8f, true, false);
        AnimationClip clipIdleRight = CreateClip("Player3_Idle_Right", idleLeft, 8f, true, true);
        AnimationClip clipIdleUp = CreateClip("Player3_Idle_Up", idleUp, 8f, true, false);

        AnimationClip clipRunDown = CreateClip("Player3_Run_Down", runDown, 12f, true, false);
        AnimationClip clipRunLeft = CreateClip("Player3_Run_Left", runLeft, 12f, true, false);
        AnimationClip clipRunRight = CreateClip("Player3_Run_Right", runLeft, 12f, true, true);
        AnimationClip clipRunUp = CreateClip("Player3_Run_Up", runUp, 12f, true, false);

        AnimationClip clipAttackDown = CreateClip("Player3_Attack_Down", attackDown, 10f, false, false);
        AnimationClip clipAttackLeft = CreateClip("Player3_Attack_Left", attackLeft, 10f, false, false);
        AnimationClip clipAttackRight = CreateClip("Player3_Attack_Right", attackLeft, 10f, false, true);
        AnimationClip clipAttackUp = CreateClip("Player3_Attack_Up", attackUp, 10f, false, false);

        AnimationClip clipHurt = CreateClip("Player3_Hurt", hurtSprites.Take(5).ToArray(), 10f, false, false);
        AnimationClip clipDeath = CreateClip("Player3_Death", deathSprites.Take(7).ToArray(), 8f, false, false);

        // Tạo Controller sạch hoàn toàn
        if (File.Exists(CONTROLLER_PATH))
        {
            AssetDatabase.DeleteAsset(CONTROLLER_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        var controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);

        // Setup Parameters
        EnsureParameter(controller, "MoveX", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "MoveY", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "ComboStep", AnimatorControllerParameterType.Int);
        EnsureParameter(controller, "AttackTrigger", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "HurtTrigger", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "IsDead", AnimatorControllerParameterType.Bool);

        var rootSm = controller.layers[0].stateMachine;
        rootSm.anyStateTransitions = new AnimatorStateTransition[0];
        rootSm.entryTransitions = new AnimatorTransition[0];

        // 1. Blend Tree Idle
        var idleTree = new BlendTree { name = "IdleTree", blendType = BlendTreeType.SimpleDirectional2D };
        idleTree.blendParameter = "MoveX";
        idleTree.blendParameterY = "MoveY";
        idleTree.AddChild(clipIdleDown, new Vector2(0, -1));
        idleTree.AddChild(clipIdleUp, new Vector2(0, 1));
        idleTree.AddChild(clipIdleRight, new Vector2(1, 0));
        idleTree.AddChild(clipIdleLeft, new Vector2(-1, 0));
        AssetDatabase.AddObjectToAsset(idleTree, controller);

        var idleState = rootSm.AddState("Idle");
        idleState.motion = idleTree;
        rootSm.defaultState = idleState;

        // 2. Blend Tree Move / Run
        var moveTree = new BlendTree { name = "MoveTree", blendType = BlendTreeType.SimpleDirectional2D };
        moveTree.blendParameter = "MoveX";
        moveTree.blendParameterY = "MoveY";
        moveTree.AddChild(clipRunDown, new Vector2(0, -1));
        moveTree.AddChild(clipRunUp, new Vector2(0, 1));
        moveTree.AddChild(clipRunRight, new Vector2(1, 0));
        moveTree.AddChild(clipRunLeft, new Vector2(-1, 0));
        AssetDatabase.AddObjectToAsset(moveTree, controller);

        var moveState = rootSm.AddState("Move");
        moveState.motion = moveTree;

        // 3. Blend Tree Attack
        var attackTree = new BlendTree { name = "AttackTree", blendType = BlendTreeType.SimpleDirectional2D };
        attackTree.blendParameter = "MoveX";
        attackTree.blendParameterY = "MoveY";
        attackTree.AddChild(clipAttackDown, new Vector2(0, -1));
        attackTree.AddChild(clipAttackUp, new Vector2(0, 1));
        attackTree.AddChild(clipAttackRight, new Vector2(1, 0));
        attackTree.AddChild(clipAttackLeft, new Vector2(-1, 0));
        AssetDatabase.AddObjectToAsset(attackTree, controller);

        var attackState = rootSm.AddState("Attack");
        attackState.motion = attackTree;

        var attack1State = rootSm.AddState("Attack1");
        attack1State.motion = attackTree;

        var attack2State = rootSm.AddState("Attack2");
        attack2State.motion = attackTree;

        // 4. Hurt State
        var hurtState = rootSm.AddState("Hurt");
        hurtState.motion = clipHurt;

        // 5. Death State
        var deathState = rootSm.AddState("Death");
        deathState.motion = clipDeath;

        // Transitions Idle <-> Move
        var toMove = idleState.AddTransition(moveState);
        toMove.AddCondition(AnimatorConditionMode.Greater, 0.05f, "Speed");
        toMove.hasExitTime = false;
        toMove.duration = 0.05f;

        var toIdle = moveState.AddTransition(idleState);
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.05f, "Speed");
        toIdle.hasExitTime = false;
        toIdle.duration = 0.05f;

        // AnyState -> Attack
        var anyToAttack = rootSm.AddAnyStateTransition(attackState);
        anyToAttack.AddCondition(AnimatorConditionMode.If, 0, "AttackTrigger");
        anyToAttack.hasExitTime = false;
        anyToAttack.duration = 0f;

        var attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 0.85f;
        attackToIdle.duration = 0.05f;

        var attack1ToIdle = attack1State.AddTransition(idleState);
        attack1ToIdle.hasExitTime = true;
        attack1ToIdle.exitTime = 0.85f;
        attack1ToIdle.duration = 0.05f;

        var attack2ToIdle = attack2State.AddTransition(idleState);
        attack2ToIdle.hasExitTime = true;
        attack2ToIdle.exitTime = 0.85f;
        attack2ToIdle.duration = 0.05f;

        // AnyState -> Hurt
        var anyToHurt = rootSm.AddAnyStateTransition(hurtState);
        anyToHurt.AddCondition(AnimatorConditionMode.If, 0, "HurtTrigger");
        anyToHurt.hasExitTime = false;
        anyToHurt.duration = 0f;

        var hurtToIdle = hurtState.AddTransition(idleState);
        hurtToIdle.hasExitTime = true;
        hurtToIdle.exitTime = 0.80f;
        hurtToIdle.duration = 0.05f;

        // AnyState -> Death
        var anyToDeath = rootSm.AddAnyStateTransition(deathState);
        anyToDeath.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
        anyToDeath.hasExitTime = false;
        anyToDeath.duration = 0f;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=green>[PLAYER 3 SETUP]</color> Hoàn tất cài đặt Player 3 Animator Controller sạch 100%!");
    }

    private static AnimationClip CreateClip(string name, Sprite[] frames, float frameRate, bool loop, bool flipX = false)
    {
        string path = $"{ANIM_DIR}/{name}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }

        clip.frameRate = frameRate;
        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / frameRate,
                value = frames[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        // Bind m_FlipX: lật ngang nếu là animation hướng phải
        EditorCurveBinding flipBinding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_FlipX"
        };
        AnimationCurve flipCurve = AnimationCurve.Constant(0f, Mathf.Max(0.01f, (float)(frames.Length) / frameRate), flipX ? 1f : 0f);
        AnimationUtility.SetEditorCurve(clip, flipBinding, flipCurve);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        if (!controller.parameters.Any(p => p.name == name))
        {
            controller.AddParameter(name, type);
        }
    }
}
