using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Linq;
using System.Collections.Generic;

[InitializeOnLoad]
public class BotWheelAnimationSetup
{
    private const string ANIM_DIR = "Assets/Animation/BotWheel";
    private const string CONTROLLER_PATH = ANIM_DIR + "/BotWheel.controller";

    static BotWheelAnimationSetup()
    {
        EditorApplication.delayCall += CheckAndSetup;
    }

    [MenuItem("Vaelstrider/Setup BotWheel Animations")]
    public static void CheckAndSetup()
    {
        if (!Directory.Exists(ANIM_DIR))
        {
            Directory.CreateDirectory(ANIM_DIR);
            AssetDatabase.Refresh();
        }

        if (File.Exists(CONTROLLER_PATH))
        {
            AssignToSceneEnemies();
            return;
        }

        CreateAllAnimationsAndController();
    }

    [MenuItem("Vaelstrider/Force Regenerate BotWheel Animations")]
    public static void ForceRegenerate()
    {
        AssetDatabase.ImportAsset("Assets/ART/BotWheel/charge.png", ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/ART/BotWheel/damaged.png", ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/ART/BotWheel/shoot with FX.png", ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/ART/BotWheel/move with FX.png", ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/ART/BotWheel/death.png", ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/ART/BotWheel/static idle.png", ImportAssetOptions.ForceUpdate);
        CreateAllAnimationsAndController();
    }

    private static void CreateAllAnimationsAndController()
    {
        Debug.Log("<color=cyan>[BOTWHEEL SETUP]</color> Đang tạo Animation và Animator Controller cho BotWheel...");

        // 1. Load Sprites - Su dung 'shoot with FX.png' cho Attack animation ban sung!
        Sprite[] idleSprites = LoadSprites("Assets/ART/BotWheel/static idle.png");
        Sprite[] moveSprites = LoadSprites("Assets/ART/BotWheel/move with FX.png");
        Sprite[] attackSprites = LoadSprites("Assets/ART/BotWheel/shoot with FX.png");
        if (attackSprites == null || attackSprites.Length == 0)
        {
            attackSprites = LoadSprites("Assets/ART/BotWheel/charge.png");
        }
        Sprite[] hurtSprites = LoadSprites("Assets/ART/BotWheel/damaged.png");
        Sprite[] deathSprites = LoadSprites("Assets/ART/BotWheel/death.png");

        // 2. Tao Clips
        AnimationClip idleClip = CreateClip("BotWheel_Idle", idleSprites, 6, true);
        AnimationClip moveClip = CreateClip("BotWheel_Move", moveSprites, 12, true);
        AnimationClip attackClip = CreateClip("BotWheel_Attack", attackSprites, 10, false);
        AnimationClip hurtClip = CreateClip("BotWheel_Hurt", hurtSprites, 10, false);
        AnimationClip deathClip = CreateClip("BotWheel_Death", deathSprites, 10, false);

        // 3. Tao Animator Controller
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);

            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

            var rootStateMachine = controller.layers[0].stateMachine;

            var idleState = rootStateMachine.AddState("Idle");
            idleState.motion = idleClip;

            var moveState = rootStateMachine.AddState("Move");
            moveState.motion = moveClip;

            var attackState = rootStateMachine.AddState("Attack");
            attackState.motion = attackClip;

            var hurtState = rootStateMachine.AddState("Hurt");
            hurtState.motion = hurtClip;

            var dieState = rootStateMachine.AddState("Die");
            dieState.motion = deathClip;

            rootStateMachine.defaultState = idleState;

            // Transitions
            var toMove = idleState.AddTransition(moveState);
            toMove.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
            toMove.hasExitTime = false;
            toMove.duration = 0f;

            var toIdle = moveState.AddTransition(idleState);
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
            toIdle.hasExitTime = false;
            toIdle.duration = 0f;

            var toAttack = rootStateMachine.AddAnyStateTransition(attackState);
            toAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            toAttack.hasExitTime = false;
            toAttack.duration = 0f;

            var fromAttack = attackState.AddTransition(idleState);
            fromAttack.hasExitTime = true;
            fromAttack.exitTime = 1f;
            fromAttack.duration = 0f;

            var toHurt = rootStateMachine.AddAnyStateTransition(hurtState);
            toHurt.AddCondition(AnimatorConditionMode.If, 0, "Hurt");
            toHurt.hasExitTime = false;
            toHurt.duration = 0f;

            var fromHurt = hurtState.AddTransition(idleState);
            fromHurt.hasExitTime = true;
            fromHurt.exitTime = 1f;
            fromHurt.duration = 0f;

            var toDie = rootStateMachine.AddAnyStateTransition(dieState);
            toDie.AddCondition(AnimatorConditionMode.If, 0, "Die");
            toDie.hasExitTime = false;
            toDie.duration = 0f;
        }
        else
        {
            // Update motion on existing state machine
            var rootStateMachine = controller.layers[0].stateMachine;
            foreach (var state in rootStateMachine.states)
            {
                if (state.state.name == "Attack") state.state.motion = attackClip;
                else if (state.state.name == "Hurt") state.state.motion = hurtClip;
                else if (state.state.name == "Move") state.state.motion = moveClip;
                else if (state.state.name == "Idle") state.state.motion = idleClip;
                else if (state.state.name == "Die") state.state.motion = deathClip;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<color=green>[BOTWHEEL SETUP]</color> Hoàn tất cài đặt Animator & Animation bắn súng tại: " + CONTROLLER_PATH);

        AssignToSceneEnemies();
    }

    private static Sprite[] LoadSprites(string path)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(path);
        var sprites = assets.OfType<Sprite>().OrderBy(s => GetSpriteIndex(s.name)).ToArray();
        return sprites;
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

    private static void AssignToSceneEnemies()
    {
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CONTROLLER_PATH);
        if (controller == null) return;

        var bots = Object.FindObjectsByType<BotWheelAI>(FindObjectsSortMode.None);
        foreach (var bot in bots)
        {
            var anim = bot.GetComponent<Animator>();
            if (anim == null) anim = bot.gameObject.AddComponent<Animator>();
            if (anim.runtimeAnimatorController == null)
            {
                anim.runtimeAnimatorController = controller;
                EditorUtility.SetDirty(bot.gameObject);
                Debug.Log("<color=green>[BOTWHEEL SETUP]</color> Đã gắn Controller vào " + bot.gameObject.name);
            }
        }
    }
}
