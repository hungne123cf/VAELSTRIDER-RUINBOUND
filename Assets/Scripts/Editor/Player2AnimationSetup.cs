using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Linq;
using System.Collections.Generic;

[InitializeOnLoad]
public class Player2AnimationSetup
{
    public const string ART_DIR = "Assets/ART/Player2";
    public const string ANIM_DIR = "Assets/Animation/Player2";
    public const string CONTROLLER_PATH = ANIM_DIR + "/Player2.controller";

    static Player2AnimationSetup()
    {
        EditorApplication.delayCall += CheckAndSetup;
    }

    [MenuItem("Vaelstrider/Setup Player 2 (Gale Ronin) Animations")]
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

    [MenuItem("Vaelstrider/Force Regenerate Player 2 Animations")]
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

    public static Sprite[] LoadOrderedSprites(string path, int maxFrames = -1)
    {
        var objects = AssetDatabase.LoadAllAssetsAtPath(path);
        if (objects == null) return new Sprite[0];

        List<Sprite> sprites = new List<Sprite>();
        foreach (var obj in objects)
        {
            if (obj is Sprite s) sprites.Add(s);
        }

        // Sort by row (top to bottom) then column (left to right)
        var ordered = sprites.OrderByDescending(s => s.rect.y).ThenBy(s => s.rect.x);
        if (maxFrames > 0)
        {
            return ordered.Take(maxFrames).ToArray();
        }
        return ordered.ToArray();
    }

    public static void CreateAllAnimationsAndController()
    {
        Debug.Log("<color=cyan>[PLAYER 2 SETUP]</color> Đang khởi tạo Animation và Animator Controller cho Phong Lôi Lãng Khách (Gale Ronin)...");

        string downIdlePath = ART_DIR + "/_down idle.png";
        string sideIdlePath = ART_DIR + "/_side idle.png";
        string upIdlePath = ART_DIR + "/_up idle.png";

        string downWalkPath = ART_DIR + "/_down walk.png";
        string sideWalkPath = ART_DIR + "/_side walk.png";
        string upWalkPath = ART_DIR + "/_up walk.png";

        string downAttackPath = ART_DIR + "/_down attack.png";
        string sideAttackPath = ART_DIR + "/_side attack.png";
        string upAttackPath = ART_DIR + "/_up attack.png";

        string pickupPath = ART_DIR + "/_pick up.png";

        // Step 1: Slice all sprite sheets
        SliceSpriteSheet(downIdlePath, 64, 64);
        SliceSpriteSheet(sideIdlePath, 64, 64);
        SliceSpriteSheet(upIdlePath, 64, 64);

        SliceSpriteSheet(downWalkPath, 64, 64);
        SliceSpriteSheet(sideWalkPath, 64, 64);
        SliceSpriteSheet(upWalkPath, 64, 64);

        SliceSpriteSheet(downAttackPath, 64, 64);
        SliceSpriteSheet(sideAttackPath, 64, 64);
        SliceSpriteSheet(upAttackPath, 64, 64);

        SliceSpriteSheet(pickupPath, 64, 64);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Step 2: Load sprites
        Sprite[] downIdleSprites = LoadOrderedSprites(downIdlePath, 5);
        Sprite[] sideIdleSprites = LoadOrderedSprites(sideIdlePath, 5);
        Sprite[] upIdleSprites = LoadOrderedSprites(upIdlePath, 5);

        Sprite[] downWalkSprites = LoadOrderedSprites(downWalkPath, 6);
        Sprite[] sideWalkSprites = LoadOrderedSprites(sideWalkPath, 6);
        Sprite[] upWalkSprites = LoadOrderedSprites(upWalkPath, 6);

        Sprite[] downAttackSprites = LoadOrderedSprites(downAttackPath, 3);
        Sprite[] sideAttackSprites = LoadOrderedSprites(sideAttackPath, 3);
        Sprite[] upAttackSprites = LoadOrderedSprites(upAttackPath, 3);

        Sprite[] pickupSprites = LoadOrderedSprites(pickupPath, 5);

        // Step 3: Create Animation Clips
        AnimationClip clipIdleDown = CreateClip("Player2_Idle_Down", downIdleSprites, 8f, true, false);
        AnimationClip clipIdleLeft = CreateClip("Player2_Idle_Left", sideIdleSprites, 8f, true, false);
        AnimationClip clipIdleRight = CreateClip("Player2_Idle_Right", sideIdleSprites, 8f, true, true);
        AnimationClip clipIdleUp = CreateClip("Player2_Idle_Up", upIdleSprites, 8f, true, false);

        AnimationClip clipWalkDown = CreateClip("Player2_Walk_Down", downWalkSprites, 10f, true, false);
        AnimationClip clipWalkLeft = CreateClip("Player2_Walk_Left", sideWalkSprites, 10f, true, false);
        AnimationClip clipWalkRight = CreateClip("Player2_Walk_Right", sideWalkSprites, 10f, true, true);
        AnimationClip clipWalkUp = CreateClip("Player2_Walk_Up", upWalkSprites, 10f, true, false);

        AnimationClip clipAttackDown = CreateClip("Player2_Attack_Down", downAttackSprites, 12f, false, false);
        AnimationClip clipAttackLeft = CreateClip("Player2_Attack_Left", sideAttackSprites, 12f, false, false);
        AnimationClip clipAttackRight = CreateClip("Player2_Attack_Right", sideAttackSprites, 12f, false, true);
        AnimationClip clipAttackUp = CreateClip("Player2_Attack_Up", upAttackSprites, 12f, false, false);

        AnimationClip clipPickup = CreateClip("Player2_Pickup", pickupSprites, 8f, false, false);

        // Step 4: Create Controller
        if (File.Exists(CONTROLLER_PATH))
        {
            AssetDatabase.DeleteAsset(CONTROLLER_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        var controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);

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

        // 2. Blend Tree Move / Walk
        var moveTree = new BlendTree { name = "MoveTree", blendType = BlendTreeType.SimpleDirectional2D };
        moveTree.blendParameter = "MoveX";
        moveTree.blendParameterY = "MoveY";
        moveTree.AddChild(clipWalkDown, new Vector2(0, -1));
        moveTree.AddChild(clipWalkUp, new Vector2(0, 1));
        moveTree.AddChild(clipWalkRight, new Vector2(1, 0));
        moveTree.AddChild(clipWalkLeft, new Vector2(-1, 0));
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

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=green>[PLAYER 2 SETUP]</color> Hoàn tất cài đặt Player 2 (Gale Ronin) Animator Controller sạch 100%!");
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
