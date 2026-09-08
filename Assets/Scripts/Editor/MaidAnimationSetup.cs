using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Linq;
using System.Collections.Generic;

[InitializeOnLoad]
public class MaidAnimationSetup
{
    public const string ANIM_DIR = "Assets/Animation/Maid";
    public const string CONTROLLER_PATH = ANIM_DIR + "/Maid.controller";

    static MaidAnimationSetup()
    {
        EditorApplication.delayCall += CheckAndSetup;
    }

    [MenuItem("Vaelstrider/Setup Maid Animations")]
    public static void CheckAndSetup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!Directory.Exists(ANIM_DIR))
        {
            Directory.CreateDirectory(ANIM_DIR);
            AssetDatabase.Refresh();
        }

        if (File.Exists(CONTROLLER_PATH))
        {
            return;
        }

        CreateAllAnimationsAndController();
    }

    [MenuItem("Vaelstrider/Force Regenerate Maid Animations")]
    public static void ForceRegenerate()
    {
        if (!Directory.Exists(ANIM_DIR))
        {
            Directory.CreateDirectory(ANIM_DIR);
            AssetDatabase.Refresh();
        }
        CreateAllAnimationsAndController();
    }

    public static void CreateAllAnimationsAndController()
    {
        Debug.Log("<color=magenta>[MAID SETUP]</color> Đang khởi tạo Animation và Animator Controller cho Maid Chiến Binh...");

        Sprite[] idleSprites = LoadSprites("Assets/ART/Player1/Maid Idle.png");
        Sprite[] runSprites = LoadSprites("Assets/ART/Player1/Maid Run.png");

        if (idleSprites == null || idleSprites.Length < 20 || runSprites == null || runSprites.Length < 40)
        {
            Debug.LogError($"[MAID SETUP] Không đủ sprite! Idle: {idleSprites?.Length ?? 0}, Run: {runSprites?.Length ?? 0}");
            return;
        }

        // 1. Chia nhóm Sprite theo hướng (Idle: 5 frames/hướng; Run: 10 frames/hướng)
        Sprite[] idleUp = idleSprites.Skip(0).Take(5).ToArray();
        Sprite[] idleDown = idleSprites.Skip(5).Take(5).ToArray();
        Sprite[] idleRight = idleSprites.Skip(10).Take(5).ToArray();
        Sprite[] idleLeft = idleSprites.Skip(15).Take(5).ToArray();

        Sprite[] runUp = runSprites.Skip(0).Take(10).ToArray();
        Sprite[] runDown = runSprites.Skip(10).Take(10).ToArray();
        Sprite[] runRight = runSprites.Skip(20).Take(10).ToArray();
        Sprite[] runLeft = runSprites.Skip(30).Take(10).ToArray();

        // 2. Tạo Animation Clips
        AnimationClip clipIdleUp = CreateClip("Maid_Idle_Up", idleUp, 8f, true);
        AnimationClip clipIdleDown = CreateClip("Maid_Idle_Down", idleDown, 8f, true);
        AnimationClip clipIdleRight = CreateClip("Maid_Idle_Right", idleRight, 8f, true);
        AnimationClip clipIdleLeft = CreateClip("Maid_Idle_Left", idleLeft, 8f, true);

        AnimationClip clipRunUp = CreateClip("Maid_Run_Up", runUp, 12f, true);
        AnimationClip clipRunDown = CreateClip("Maid_Run_Down", runDown, 12f, true);
        AnimationClip clipRunRight = CreateClip("Maid_Run_Right", runRight, 12f, true);
        AnimationClip clipRunLeft = CreateClip("Maid_Run_Left", runLeft, 12f, true);

        // Attack Clips (kết hợp các frame tấn công sắc nét)
        AnimationClip clipAttack1 = CreateClip("Maid_Attack1", new Sprite[] { runDown[2], runDown[3], runDown[4], idleDown[0] }, 14f, false);
        AnimationClip clipAttack2 = CreateClip("Maid_Attack2", new Sprite[] { runDown[5], runDown[6], runDown[7], idleDown[1] }, 14f, false);

        // Ulti Clip (Quay 360 độ cực nhanh tạo lốc xoáy)
        List<Sprite> spinFrames = new List<Sprite>
        {
            idleDown[2], idleRight[2], idleUp[2], idleLeft[2],
            runDown[4], runRight[4], runUp[4], runLeft[4],
            idleDown[3], idleRight[3], idleUp[3], idleLeft[3]
        };
        AnimationClip clipUlti = CreateClip("Maid_Ulti", spinFrames.ToArray(), 20f, false);

        // 3. Tạo hoặc lấy Controller
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);
        }

        // Đảm bảo đủ các Parameters giống hệt Player Controller
        EnsureParameter(controller, "MoveX", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "MoveY", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "AttackTrigger", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "ComboStep", AnimatorControllerParameterType.Int);

        var rootSm = controller.layers[0].stateMachine;

        // Xóa states cũ nếu có để tạo mới sạch sẽ
        var existingStates = rootSm.states.Select(s => s.state).ToList();
        foreach (var st in existingStates)
        {
            rootSm.RemoveState(st);
        }

        // --- IDLE BLEND TREE ---
        var idleState = rootSm.AddState("Idle");
        BlendTree idleTree;
        var idleMotion = controller.CreateBlendTreeInController("Idle_BlendTree", out idleTree);
        idleTree.blendType = BlendTreeType.SimpleDirectional2D;
        idleTree.blendParameter = "MoveX";
        idleTree.blendParameterY = "MoveY";
        idleTree.AddChild(clipIdleDown, new Vector2(0, -1));
        idleTree.AddChild(clipIdleUp, new Vector2(0, 1));
        idleTree.AddChild(clipIdleRight, new Vector2(1, 0));
        idleTree.AddChild(clipIdleLeft, new Vector2(-1, 0));
        idleState.motion = idleTree;

        // --- RUN BLEND TREE ---
        var runState = rootSm.AddState("Run");
        BlendTree runTree;
        var runMotion = controller.CreateBlendTreeInController("Run_BlendTree", out runTree);
        runTree.blendType = BlendTreeType.SimpleDirectional2D;
        runTree.blendParameter = "MoveX";
        runTree.blendParameterY = "MoveY";
        runTree.AddChild(clipRunDown, new Vector2(0, -1));
        runTree.AddChild(clipRunUp, new Vector2(0, 1));
        runTree.AddChild(clipRunRight, new Vector2(1, 0));
        runTree.AddChild(clipRunLeft, new Vector2(-1, 0));
        runState.motion = runTree;

        // --- ATTACK STATES ---
        var attack1State = rootSm.AddState("Attack1");
        attack1State.motion = clipAttack1;

        var attack2State = rootSm.AddState("Attack2");
        attack2State.motion = clipAttack2;

        var ultiState = rootSm.AddState("Player_Ulti");
        ultiState.motion = clipUlti;

        // --- TRANSITIONS ---
        rootSm.defaultState = idleState;

        // Idle -> Run
        var toRun = idleState.AddTransition(runState);
        toRun.AddCondition(AnimatorConditionMode.Greater, 0.01f, "Speed");
        toRun.hasExitTime = false;
        toRun.duration = 0.05f;

        // Run -> Idle
        var toIdle = runState.AddTransition(idleState);
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.01f, "Speed");
        toIdle.hasExitTime = false;
        toIdle.duration = 0.05f;

        // AnyState -> Attack1
        var toAtk1 = rootSm.AddAnyStateTransition(attack1State);
        toAtk1.AddCondition(AnimatorConditionMode.If, 0, "AttackTrigger");
        toAtk1.AddCondition(AnimatorConditionMode.Equals, 1, "ComboStep");
        toAtk1.hasExitTime = false;
        toAtk1.duration = 0f;

        // AnyState -> Attack2
        var toAtk2 = rootSm.AddAnyStateTransition(attack2State);
        toAtk2.AddCondition(AnimatorConditionMode.If, 0, "AttackTrigger");
        toAtk2.AddCondition(AnimatorConditionMode.Equals, 2, "ComboStep");
        toAtk2.hasExitTime = false;
        toAtk2.duration = 0f;

        // Return to Idle from Attacks
        var fromAtk1 = attack1State.AddTransition(idleState);
        fromAtk1.hasExitTime = true;
        fromAtk1.exitTime = 0.9f;
        fromAtk1.duration = 0.05f;

        var fromAtk2 = attack2State.AddTransition(idleState);
        fromAtk2.hasExitTime = true;
        fromAtk2.exitTime = 0.9f;
        fromAtk2.duration = 0.05f;

        // Return to Idle from Ulti
        var fromUlti = ultiState.AddTransition(idleState);
        fromUlti.hasExitTime = true;
        fromUlti.exitTime = 0.9f;
        fromUlti.duration = 0.05f;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=green>[MAID SETUP]</color> Hoàn tất cài đặt AnimatorController cho Maid tại: " + CONTROLLER_PATH);
    }

    private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        if (!controller.parameters.Any(p => p.name == name))
        {
            controller.AddParameter(name, type);
        }
    }

    private static Sprite[] LoadSprites(string path)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(path);
        return assets.OfType<Sprite>().OrderBy(s => GetSpriteIndex(s.name)).ToArray();
    }

    private static int GetSpriteIndex(string name)
    {
        int lastUnder = name.LastIndexOf('_');
        if (lastUnder >= 0 && int.TryParse(name.Substring(lastUnder + 1), out int val))
        {
            return val;
        }
        return 0;
    }

    private static AnimationClip CreateClip(string name, Sprite[] sprites, float fps, bool loop)
    {
        string clipPath = $"{ANIM_DIR}/{name}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        clip.frameRate = fps;

        if (sprites != null && sprites.Length > 0)
        {
            EditorCurveBinding binding = new EditorCurveBinding();
            binding.type = typeof(SpriteRenderer);
            binding.path = "";
            binding.propertyName = "m_Sprite";

            ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe();
                keyframes[i].time = i / fps;
                keyframes[i].value = sprites[i];
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
        }

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
        return clip;
    }
}
