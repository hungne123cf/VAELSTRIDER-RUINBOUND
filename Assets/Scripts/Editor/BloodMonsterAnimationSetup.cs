using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Linq;
using System.Collections.Generic;

[InitializeOnLoad]
public class BloodMonsterAnimationSetup
{
    private const string ANIM_DIR = "Assets/Animation/BloodMonster";
    private const string CONTROLLER_PATH = ANIM_DIR + "/BloodMonster.controller";
    private const string SPRITESHEET_PATH = "Assets/ART/Blood Monster_A with shadows/Blood Monster_A.png";

    static BloodMonsterAnimationSetup()
    {
        EditorApplication.delayCall += CheckAndSetup;
    }

    [MenuItem("Vaelstrider/Setup BloodMonster Animations")]
    public static void CheckAndSetup()
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
        Debug.Log("<color=red>[BLOOD MONSTER SETUP]</color> Đang tạo Animation và Animator Controller cho Blood Monster...");

        // Load all sprites from sliced texture
        var allAssets = AssetDatabase.LoadAllAssetsAtPath(SPRITESHEET_PATH);
        var sprites = allAssets.OfType<Sprite>().OrderBy(s => GetSpriteIndex(s.name)).ToArray();

        if (sprites.Length < 41)
        {
            Debug.LogWarning($"[BLOOD MONSTER SETUP] Chỉ tìm thấy {sprites.Length} sprites, cần 41 sprites. Hãy đảm bảo file đã được import.");
            return;
        }

        // Chia theo cac hang hoat anh:
        // Hang 1: Idle (0-5)
        // Hang 2: Move (6-13)
        // Hang 3: Attack 1 (14-21)
        // Hang 4: Attack 2 / Spit (22-32)
        // Hang 5: Hurt (33-36)
        // Hang 6: Die (37-40)
        Sprite[] idleSprites = sprites.Skip(0).Take(6).ToArray();
        Sprite[] moveSprites = sprites.Skip(6).Take(8).ToArray();
        Sprite[] attackSprites = sprites.Skip(14).Take(8).ToArray();
        Sprite[] specialAttackSprites = sprites.Skip(22).Take(11).ToArray();
        Sprite[] hurtSprites = sprites.Skip(33).Take(4).ToArray();
        Sprite[] deathSprites = sprites.Skip(37).Take(4).ToArray();

        AnimationClip idleClip = CreateClip("BloodMonster_Idle", idleSprites, 8, true);
        AnimationClip moveClip = CreateClip("BloodMonster_Move", moveSprites, 10, true);
        AnimationClip attackClip = CreateClip("BloodMonster_Attack", attackSprites, 10, false);
        AnimationClip specialAttackClip = CreateClip("BloodMonster_SpecialAttack", specialAttackSprites, 12, false);
        AnimationClip hurtClip = CreateClip("BloodMonster_Hurt", hurtSprites, 10, false);
        AnimationClip deathClip = CreateClip("BloodMonster_Death", deathSprites, 8, false);

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);

            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("SpecialAttack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

            var rootStateMachine = controller.layers[0].stateMachine;

            var idleState = rootStateMachine.AddState("Idle");
            idleState.motion = idleClip;

            var moveState = rootStateMachine.AddState("Move");
            moveState.motion = moveClip;

            var attackState = rootStateMachine.AddState("Attack");
            attackState.motion = attackClip;

            var specialAttackState = rootStateMachine.AddState("SpecialAttack");
            specialAttackState.motion = specialAttackClip;

            var hurtState = rootStateMachine.AddState("Hurt");
            hurtState.motion = hurtClip;

            var dieState = rootStateMachine.AddState("Die");
            dieState.motion = deathClip;

            rootStateMachine.defaultState = idleState;

            // Transitions:
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

            var toSpecial = rootStateMachine.AddAnyStateTransition(specialAttackState);
            toSpecial.AddCondition(AnimatorConditionMode.If, 0, "SpecialAttack");
            toSpecial.hasExitTime = false;
            toSpecial.duration = 0f;

            var fromSpecial = specialAttackState.AddTransition(idleState);
            fromSpecial.hasExitTime = true;
            fromSpecial.exitTime = 1f;
            fromSpecial.duration = 0f;

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
            var rootStateMachine = controller.layers[0].stateMachine;
            foreach (var state in rootStateMachine.states)
            {
                if (state.state.name == "Attack") state.state.motion = attackClip;
                else if (state.state.name == "SpecialAttack") state.state.motion = specialAttackClip;
                else if (state.state.name == "Hurt") state.state.motion = hurtClip;
                else if (state.state.name == "Move") state.state.motion = moveClip;
                else if (state.state.name == "Idle") state.state.motion = idleClip;
                else if (state.state.name == "Die") state.state.motion = deathClip;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<color=green>[BLOOD MONSTER SETUP]</color> Hoàn tất cài đặt Animator & Animation cho Blood Monster tại: " + CONTROLLER_PATH);
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
            clip = new AnimationClip { name = name };
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        clip.frameRate = fps;

        if (sprites != null && sprites.Length > 0)
        {
            EditorCurveBinding binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite"
            };

            ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Length];
            float interval = 1f / fps;
            for (int i = 0; i < sprites.Length; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe();
                keyframes[i].time = i * interval;
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
