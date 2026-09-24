#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
public static class Map3AnimationSetup
{
    private const string ANIM_BASE_PATH = "Assets/Animation";
    private const string MONSTERS_PATH = "Assets/ART/Monsters";
    private const string BOSSES_PATH = "Assets/ART/Bosses";

    [MenuItem("Vaelstrider/Map 3/1. Setup Map 3 Monster & Boss Animations", false, 1)]
    public static void SetupAllAnimations()
    {
        Debug.Log("<color=cyan>[MAP 3 ANIM]</color> Bat dau cau hinh toan bo bo Animation & Controller chuan xac...");

        SetupGoblinAnimations();
        SetupSkeletonAnimations();
        SetupSlimeAnimations();
        SetupSkeletonKingAnimations();
        SetupDungeonMasterAnimations();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=green>[MAP 3 ANIM]</color> Hoan tat tao Animation Clips (Idle, Walk, Attack, Hurt, Death) & Controllers!");
    }

    // ===== GOBLIN SETUP =====
    private static void SetupGoblinAnimations()
    {
        string dir = $"{ANIM_BASE_PATH}/Goblin";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        Sprite walk0 = AssetDatabase.LoadAssetAtPath<Sprite>($"{MONSTERS_PATH}/Goblin/Variant0/Goblin_Walk_0.png");
        Sprite walk1 = AssetDatabase.LoadAssetAtPath<Sprite>($"{MONSTERS_PATH}/Goblin/Variant0/Goblin_Walk_1.png");
        Sprite walk2 = AssetDatabase.LoadAssetAtPath<Sprite>($"{MONSTERS_PATH}/Goblin/Variant0/Goblin_Walk_2.png");
        Sprite walk3 = AssetDatabase.LoadAssetAtPath<Sprite>($"{MONSTERS_PATH}/Goblin/Variant0/Goblin_Walk_3.png");

        Sprite death0 = AssetDatabase.LoadAssetAtPath<Sprite>($"{MONSTERS_PATH}/Goblin/Variant0/Goblin_Death_0.png");
        Sprite death1 = AssetDatabase.LoadAssetAtPath<Sprite>($"{MONSTERS_PATH}/Goblin/Variant0/Goblin_Death_1.png");
        Sprite death2 = AssetDatabase.LoadAssetAtPath<Sprite>($"{MONSTERS_PATH}/Goblin/Variant0/Goblin_Death_2.png");
        Sprite death3 = AssetDatabase.LoadAssetAtPath<Sprite>($"{MONSTERS_PATH}/Goblin/Variant0/Goblin_Death_3.png");

        AnimationClip idleClip = CreateClip(dir, "Goblin_Idle", new Sprite[] { walk0, walk2 }, 3, true);
        AnimationClip walkClip = CreateClip(dir, "Goblin_Walk", new Sprite[] { walk0, walk2 }, 6, true);
        AnimationClip attackClip = CreateClip(dir, "Goblin_Attack", new Sprite[] { walk0, walk1, walk3, walk0 }, 10, false);
        AnimationClip hurtClip = CreateClip(dir, "Goblin_Hurt", new Sprite[] { death0, walk0 }, 10, false);
        AnimationClip deathClip = CreateClip(dir, "Goblin_Death", new Sprite[] { death0, death1, death2, death3 }, 6, false);

        BuildMeleeAnimatorController($"{dir}/Goblin.controller", idleClip, walkClip, attackClip, hurtClip, deathClip);
    }

    // ===== SKELETON SETUP =====
    private static void SetupSkeletonAnimations()
    {
        string dir = $"{ANIM_BASE_PATH}/Skeleton";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        Sprite walk0 = AssetDatabase.LoadAssetAtPath<Sprite>($"{MONSTERS_PATH}/Skeleton/Variant0/Skeleton_Walk_0.png");
        Sprite walk1 = AssetDatabase.LoadAssetAtPath<Sprite>($"{MONSTERS_PATH}/Skeleton/Variant0/Skeleton_Walk_1.png");
        Sprite walk2 = AssetDatabase.LoadAssetAtPath<Sprite>($"{MONSTERS_PATH}/Skeleton/Variant0/Skeleton_Walk_2.png");
        Sprite walk3 = AssetDatabase.LoadAssetAtPath<Sprite>($"{MONSTERS_PATH}/Skeleton/Variant0/Skeleton_Walk_3.png");

        Sprite death0 = AssetDatabase.LoadAssetAtPath<Sprite>($"{MONSTERS_PATH}/Skeleton/Variant0/Skeleton_Death_0.png");
        Sprite death1 = AssetDatabase.LoadAssetAtPath<Sprite>($"{MONSTERS_PATH}/Skeleton/Variant0/Skeleton_Death_1.png");
        Sprite death2 = AssetDatabase.LoadAssetAtPath<Sprite>($"{MONSTERS_PATH}/Skeleton/Variant0/Skeleton_Death_2.png");
        Sprite death3 = AssetDatabase.LoadAssetAtPath<Sprite>($"{MONSTERS_PATH}/Skeleton/Variant0/Skeleton_Death_3.png");

        AnimationClip idleClip = CreateClip(dir, "Skeleton_Idle", new Sprite[] { walk0, walk2 }, 2, true);
        AnimationClip walkClip = CreateClip(dir, "Skeleton_Walk", new Sprite[] { walk0, walk2 }, 5, true);
        AnimationClip attackClip = CreateClip(dir, "Skeleton_Attack", new Sprite[] { walk0, walk1, walk3, walk0 }, 10, false);
        AnimationClip hurtClip = CreateClip(dir, "Skeleton_Hurt", new Sprite[] { death0, walk0 }, 10, false);
        AnimationClip deathClip = CreateClip(dir, "Skeleton_Death", new Sprite[] { death0, death1, death2, death3 }, 6, false);

        BuildMeleeAnimatorController($"{dir}/Skeleton.controller", idleClip, walkClip, attackClip, hurtClip, deathClip);
    }

    // ===== SLIME SETUP =====
    private static void SetupSlimeAnimations()
    {
        string dir = $"{ANIM_BASE_PATH}/Slime";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        Sprite walk0 = AssetDatabase.LoadAssetAtPath<Sprite>($"{MONSTERS_PATH}/Slime/Variant0/Slime_Walk_0.png");
        Sprite walk1 = AssetDatabase.LoadAssetAtPath<Sprite>($"{MONSTERS_PATH}/Slime/Variant0/Slime_Walk_1.png");
        Sprite walk2 = AssetDatabase.LoadAssetAtPath<Sprite>($"{MONSTERS_PATH}/Slime/Variant0/Slime_Walk_2.png");
        Sprite walk3 = AssetDatabase.LoadAssetAtPath<Sprite>($"{MONSTERS_PATH}/Slime/Variant0/Slime_Walk_3.png");

        Sprite death0 = AssetDatabase.LoadAssetAtPath<Sprite>($"{MONSTERS_PATH}/Slime/Variant0/Slime_Death_0.png");
        Sprite death1 = AssetDatabase.LoadAssetAtPath<Sprite>($"{MONSTERS_PATH}/Slime/Variant0/Slime_Death_1.png");
        Sprite death2 = AssetDatabase.LoadAssetAtPath<Sprite>($"{MONSTERS_PATH}/Slime/Variant0/Slime_Death_2.png");
        Sprite death3 = AssetDatabase.LoadAssetAtPath<Sprite>($"{MONSTERS_PATH}/Slime/Variant0/Slime_Death_3.png");

        AnimationClip idleClip = CreateClip(dir, "Slime_Idle", new Sprite[] { walk0, walk2 }, 3, true);
        AnimationClip walkClip = CreateClip(dir, "Slime_Walk", new Sprite[] { walk2, walk3, walk0 }, 5, true);
        AnimationClip attackClip = CreateClip(dir, "Slime_Attack", new Sprite[] { walk0, walk1, walk1, walk0 }, 8, false);
        AnimationClip hurtClip = CreateClip(dir, "Slime_Hurt", new Sprite[] { death0, walk0 }, 10, false);
        AnimationClip deathClip = CreateClip(dir, "Slime_Death", new Sprite[] { death0, death1, death2, death3 }, 6, false);

        BuildMeleeAnimatorController($"{dir}/Slime.controller", idleClip, walkClip, attackClip, hurtClip, deathClip);
    }

    // ===== SKELETON KING (BOSS) SETUP =====
    private static void SetupSkeletonKingAnimations()
    {
        string dir = $"{ANIM_BASE_PATH}/SkeletonKing";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        Sprite walk0 = AssetDatabase.LoadAssetAtPath<Sprite>($"{BOSSES_PATH}/Skeleton King/SkeletonKing_Walk_0.png");
        Sprite walk1 = AssetDatabase.LoadAssetAtPath<Sprite>($"{BOSSES_PATH}/Skeleton King/SkeletonKing_Walk_1.png");
        Sprite walk2 = AssetDatabase.LoadAssetAtPath<Sprite>($"{BOSSES_PATH}/Skeleton King/SkeletonKing_Walk_2.png");
        Sprite walk3 = AssetDatabase.LoadAssetAtPath<Sprite>($"{BOSSES_PATH}/Skeleton King/SkeletonKing_Walk_3.png");

        Sprite death0 = AssetDatabase.LoadAssetAtPath<Sprite>($"{BOSSES_PATH}/Skeleton King/SkeletonKing_Death_0.png");
        Sprite death1 = AssetDatabase.LoadAssetAtPath<Sprite>($"{BOSSES_PATH}/Skeleton King/SkeletonKing_Death_1.png");
        Sprite death2 = AssetDatabase.LoadAssetAtPath<Sprite>($"{BOSSES_PATH}/Skeleton King/SkeletonKing_Death_2.png");
        Sprite death3 = AssetDatabase.LoadAssetAtPath<Sprite>($"{BOSSES_PATH}/Skeleton King/SkeletonKing_Death_3.png");

        Sprite[] obstacleSprites = LoadSprites($"{BOSSES_PATH}/Skeleton King/SkeletonKing_Obstacle", 4);
        Sprite[] obstacleDeathSprites = LoadSprites($"{BOSSES_PATH}/Skeleton King/SkeletonKing_Ostacle_Death", 4);

        AnimationClip idleClip = CreateClip(dir, "SkeletonKing_Idle", new Sprite[] { walk0, walk2 }, 2, true);
        AnimationClip walkClip = CreateClip(dir, "SkeletonKing_Walk", new Sprite[] { walk2, walk3, walk0 }, 4, true);
        AnimationClip attackClip = CreateClip(dir, "SkeletonKing_Attack", new Sprite[] { walk0, walk1, walk1, walk0 }, 6, false);
        AnimationClip hurtClip = CreateClip(dir, "SkeletonKing_Hurt", new Sprite[] { death0, walk0 }, 8, false);
        AnimationClip deathClip = CreateClip(dir, "SkeletonKing_Death", new Sprite[] { death0, death1, death2, death3 }, 4, false);

        CreateClip(dir, "SkeletonKing_Obstacle_Rise", obstacleSprites, 8, false);
        CreateClip(dir, "SkeletonKing_Obstacle_Break", obstacleDeathSprites, 8, false);

        BuildMeleeAnimatorController($"{dir}/SkeletonKing.controller", idleClip, walkClip, attackClip, hurtClip, deathClip);
    }

    // ===== DUNGEON MASTER SETUP =====
    private static void SetupDungeonMasterAnimations()
    {
        string dir = $"{ANIM_BASE_PATH}/DungeonMaster";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        Sprite walk0 = AssetDatabase.LoadAssetAtPath<Sprite>($"{BOSSES_PATH}/Dungeon Master/DungeonMaster_Walk_0.png");
        Sprite walk1 = AssetDatabase.LoadAssetAtPath<Sprite>($"{BOSSES_PATH}/Dungeon Master/DungeonMaster_Walk_1.png");
        Sprite walk2 = AssetDatabase.LoadAssetAtPath<Sprite>($"{BOSSES_PATH}/Dungeon Master/DungeonMaster_Walk_2.png");
        Sprite walk3 = AssetDatabase.LoadAssetAtPath<Sprite>($"{BOSSES_PATH}/Dungeon Master/DungeonMaster_Walk_3.png");

        Sprite[] deathSprites = new Sprite[12];
        for (int i = 0; i < 12; i++)
        {
            string numStr = i < 10 ? $"0{i}" : $"{i}";
            deathSprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"{BOSSES_PATH}/Dungeon Master/DungeonMaster_Death_{numStr}.png");
        }

        AnimationClip idleClip = CreateClip(dir, "DungeonMaster_Idle", new Sprite[] { walk0, walk2 }, 3, true);
        AnimationClip walkClip = CreateClip(dir, "DungeonMaster_Walk", new Sprite[] { walk0, walk1, walk2, walk3 }, 6, true);
        AnimationClip attackClip = CreateClip(dir, "DungeonMaster_Attack", new Sprite[] { walk0, walk1, walk3, walk0 }, 8, false);
        AnimationClip hurtClip = CreateClip(dir, "DungeonMaster_Hurt", new Sprite[] { deathSprites[1], walk0 }, 8, false);
        AnimationClip deathClip = CreateClip(dir, "DungeonMaster_Death", deathSprites, 8, false);

        BuildMeleeAnimatorController($"{dir}/DungeonMaster.controller", idleClip, walkClip, attackClip, hurtClip, deathClip);
    }

    private static Sprite[] LoadSprites(string basePath, int count)
    {
        Sprite[] sprites = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"{basePath}_{i}.png");
        }
        return sprites;
    }

    private static AnimationClip CreateClip(string folder, string name, Sprite[] sprites, float fps, bool loop)
    {
        string clipPath = $"{folder}/{name}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            clip = new AnimationClip { name = name };
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        clip.frameRate = fps;

        if (sprites != null && sprites.Length > 0 && sprites.All(s => s != null))
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
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i * interval,
                    value = sprites[i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
        }

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void BuildMeleeAnimatorController(string controllerPath, AnimationClip idleClip, AnimationClip moveClip, AnimationClip attackClip, AnimationClip hurtClip, AnimationClip dieClip)
    {
        // Xoa file controller cu neu co de dam bao reset sach se trang thai
        if (File.Exists(controllerPath))
        {
            AssetDatabase.DeleteAsset(controllerPath);
        }

        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        // Them du cac Parameters
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("SpecialAttack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Skill", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

        var rootStateMachine = controller.layers[0].stateMachine;

        // 1. Idle State (Default)
        var idleState = rootStateMachine.AddState("Idle");
        idleState.motion = idleClip;
        rootStateMachine.defaultState = idleState;

        // 2. Move State
        var moveState = rootStateMachine.AddState("Move");
        moveState.motion = moveClip;

        // 3. Attack State
        var attackState = rootStateMachine.AddState("Attack");
        attackState.motion = attackClip;

        // 4. Hurt State
        var hurtState = rootStateMachine.AddState("Hurt");
        hurtState.motion = hurtClip;

        // 5. Die State
        var dieState = rootStateMachine.AddState("Die");
        dieState.motion = dieClip;

        // ===== TRANSITIONS =====
        // Idle <-> Move
        var toMove = idleState.AddTransition(moveState);
        toMove.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
        toMove.hasExitTime = false;
        toMove.duration = 0.05f;

        var toIdle = moveState.AddTransition(idleState);
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
        toIdle.hasExitTime = false;
        toIdle.duration = 0.05f;

        // AnyState -> Attack
        var anyToAttack = rootStateMachine.AddAnyStateTransition(attackState);
        anyToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        anyToAttack.hasExitTime = false;
        anyToAttack.duration = 0.02f;

        // Attack -> Idle khi danh xong
        var fromAttack = attackState.AddTransition(idleState);
        fromAttack.hasExitTime = true;
        fromAttack.exitTime = 1f;
        fromAttack.duration = 0.05f;

        // AnyState -> Hurt (ho tro ca trigger Hurt va Hit)
        var anyToHurt1 = rootStateMachine.AddAnyStateTransition(hurtState);
        anyToHurt1.AddCondition(AnimatorConditionMode.If, 0, "Hurt");
        anyToHurt1.hasExitTime = false;
        anyToHurt1.duration = 0.02f;

        var anyToHurt2 = rootStateMachine.AddAnyStateTransition(hurtState);
        anyToHurt2.AddCondition(AnimatorConditionMode.If, 0, "Hit");
        anyToHurt2.hasExitTime = false;
        anyToHurt2.duration = 0.02f;

        // Hurt -> Idle khi giat minh xong
        var fromHurt = hurtState.AddTransition(idleState);
        fromHurt.hasExitTime = true;
        fromHurt.exitTime = 1f;
        fromHurt.duration = 0.05f;

        // AnyState -> Die
        var toDie = rootStateMachine.AddAnyStateTransition(dieState);
        toDie.AddCondition(AnimatorConditionMode.If, 0, "Die");
        toDie.hasExitTime = false;
        toDie.duration = 0f;

        EditorUtility.SetDirty(controller);
    }
}
#endif
