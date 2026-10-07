using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Linq;
using System.Collections.Generic;

[InitializeOnLoad]
public class Player4AnimationSetup
{
    public const string ART_DIR = "Assets/ART/Player4";
    public const string ANIM_DIR = "Assets/Animation/Player4";
    public const string CONTROLLER_PATH = ANIM_DIR + "/Player4.controller";

    static Player4AnimationSetup()
    {
        EditorApplication.delayCall += CheckAndSetup;
    }

    [MenuItem("Vaelstrider/Setup Player 4 (Titan Berserker) Animations")]
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

    [MenuItem("Vaelstrider/Force Regenerate Player 4 Animations")]
    public static void ForceRegenerate()
    {
        if (!Directory.Exists(ANIM_DIR))
        {
            Directory.CreateDirectory(ANIM_DIR);
            AssetDatabase.Refresh();
        }
        CreateAllAnimationsAndController();
    }

    public static void SliceSpriteSheet(string path, int frameWidth = 128, int frameHeight = 128, float ppu = 28f)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.spritePixelsPerUnit = ppu;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.isReadable = true;

        if (File.Exists(path))
        {
            byte[] fileData = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2);
            if (texture.LoadImage(fileData))
            {
                int texW = texture.width;
                int texH = texture.height;
                int cols = Mathf.Max(1, texW / frameWidth);
                int rows = Mathf.Max(1, texH / frameHeight);

                List<SpriteMetaData> metas = new List<SpriteMetaData>();
                for (int r = 0; r < rows; r++)
                {
                    int unityY = (rows - 1 - r) * frameHeight;
                    for (int c = 0; c < cols; c++)
                    {
                        SpriteMetaData meta = new SpriteMetaData();
                        meta.rect = new Rect(c * frameWidth, unityY, frameWidth, frameHeight);
                        meta.name = $"{Path.GetFileNameWithoutExtension(path)}_{r}_{c}";
                        meta.alignment = (int)SpriteAlignment.Custom;
                        meta.pivot = new Vector2(0.5f, 0.1875f);
                        metas.Add(meta);
                    }
                }
                importer.spritesheet = metas.ToArray();
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
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

        return sprites.OrderByDescending(s => s.rect.y).ThenBy(s => s.rect.x).ToArray();
    }

    public static void CreateAllAnimationsAndController()
    {
        Debug.Log("<color=cyan>[PLAYER 4 SETUP]</color> Đang khởi tạo Animation và Animator Controller cho Cuồng Chiến Binh (Titan Berserker)...");

        string idlePath = ART_DIR + "/Hammer_Idle.png";
        string walkPath = ART_DIR + "/Hammer_Walk.png";
        string runPath = ART_DIR + "/Hammer_Run.png";
        string attackPath = ART_DIR + "/Hammer_Attack.png";
        string attack2Path = ART_DIR + "/Hammer_Attack2.png";
        string skillPath = ART_DIR + "/Hammer_Skill.png";
        string spinPath = ART_DIR + "/Hammer_Spin.png";
        string hurtPath = ART_DIR + "/Hammer_Hurt.png";
        string deathPath = ART_DIR + "/Hammer_Death.png";

        SliceSpriteSheet(idlePath, 128, 128, 28f);
        SliceSpriteSheet(walkPath, 128, 128, 28f);
        SliceSpriteSheet(runPath, 128, 128, 28f);
        SliceSpriteSheet(attackPath, 128, 128, 28f);
        SliceSpriteSheet(attack2Path, 128, 128, 28f);
        SliceSpriteSheet(skillPath, 128, 128, 28f);
        SliceSpriteSheet(spinPath, 128, 128, 28f);
        SliceSpriteSheet(hurtPath, 128, 128, 28f);
        SliceSpriteSheet(deathPath, 128, 128, 28f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Sprite[] idleSprites = LoadOrderedSprites(idlePath);
        Sprite[] walkSprites = LoadOrderedSprites(walkPath);
        Sprite[] runSprites = LoadOrderedSprites(runPath);
        Sprite[] attackSprites = LoadOrderedSprites(attackPath);
        Sprite[] attack2Sprites = LoadOrderedSprites(attack2Path);
        Sprite[] skillSprites = LoadOrderedSprites(skillPath);
        Sprite[] spinSprites = LoadOrderedSprites(spinPath);
        Sprite[] hurtSprites = LoadOrderedSprites(hurtPath);
        Sprite[] deathSprites = LoadOrderedSprites(deathPath);

        if (idleSprites.Length < 4 || walkSprites.Length < 4 || attackSprites.Length < 4)
        {
            Debug.LogError($"[PLAYER 4 SETUP] Số lượng sprite không đủ! Idle={idleSprites.Length}, Walk={walkSprites.Length}, Attack={attackSprites.Length}");
            return;
        }

        // 1. Idle (5 frames/hướng, 4 hàng)
        Sprite[] idleDown = idleSprites.Take(5).ToArray();
        Sprite[] idleLeft = (idleSprites.Length >= 10) ? idleSprites.Skip(5).Take(5).ToArray() : idleDown;
        Sprite[] idleRight = (idleSprites.Length >= 15) ? idleSprites.Skip(10).Take(5).ToArray() : idleDown;
        Sprite[] idleUp = (idleSprites.Length >= 20) ? idleSprites.Skip(15).Take(5).ToArray() : idleDown;

        // 2. Walk (8 frames/hướng, 4 hàng)
        Sprite[] walkDown = walkSprites.Take(8).ToArray();
        Sprite[] walkLeft = (walkSprites.Length >= 16) ? walkSprites.Skip(8).Take(8).ToArray() : walkDown;
        Sprite[] walkRight = (walkSprites.Length >= 24) ? walkSprites.Skip(16).Take(8).ToArray() : walkDown;
        Sprite[] walkUp = (walkSprites.Length >= 32) ? walkSprites.Skip(24).Take(8).ToArray() : walkDown;

        // 3. Run (5 frames/hướng, 4 hàng - Guard Sprint / Charge)
        Sprite[] runDown = runSprites.Take(5).ToArray();
        Sprite[] runLeft = (runSprites.Length >= 10) ? runSprites.Skip(5).Take(5).ToArray() : runDown;
        Sprite[] runRight = (runSprites.Length >= 15) ? runSprites.Skip(10).Take(5).ToArray() : runDown;
        Sprite[] runUp = (runSprites.Length >= 20) ? runSprites.Skip(15).Take(5).ToArray() : runDown;

        // 4. Attack 1 (9 frames/hướng, 4 hàng - Crescent Slash)
        Sprite[] attackDown = attackSprites.Take(9).ToArray();
        Sprite[] attackLeft = (attackSprites.Length >= 18) ? attackSprites.Skip(9).Take(9).ToArray() : attackDown;
        Sprite[] attackRight = (attackSprites.Length >= 27) ? attackSprites.Skip(18).Take(9).ToArray() : attackDown;
        Sprite[] attackUp = (attackSprites.Length >= 36) ? attackSprites.Skip(27).Take(9).ToArray() : attackDown;

        // 5. Attack 2 / Bash (5 frames/hướng, 4 hàng - Heavy Thrust)
        Sprite[] attack2Down = (attack2Sprites.Length >= 5) ? attack2Sprites.Take(5).ToArray() : attackDown;
        Sprite[] attack2Left = (attack2Sprites.Length >= 10) ? attack2Sprites.Skip(5).Take(5).ToArray() : attack2Down;
        Sprite[] attack2Right = (attack2Sprites.Length >= 15) ? attack2Sprites.Skip(10).Take(5).ToArray() : attack2Down;
        Sprite[] attack2Up = (attack2Sprites.Length >= 20) ? attack2Sprites.Skip(15).Take(5).ToArray() : attack2Down;

        // 6. Skill (6 frames/hướng, 4 hàng - Leap & Ground Slam)
        Sprite[] skillDown = skillSprites.Take(6).ToArray();
        Sprite[] skillLeft = (skillSprites.Length >= 12) ? skillSprites.Skip(6).Take(6).ToArray() : skillDown;
        Sprite[] skillRight = (skillSprites.Length >= 18) ? skillSprites.Skip(12).Take(6).ToArray() : skillDown;
        Sprite[] skillUp = (skillSprites.Length >= 24) ? skillSprites.Skip(18).Take(6).ToArray() : skillDown;

        // 7. Spin Whirlwind (9 frames/hướng, 4 hàng - 360 Spin)
        Sprite[] spinDown = (spinSprites.Length >= 9) ? spinSprites.Take(9).ToArray() : attackDown;
        Sprite[] spinLeft = (spinSprites.Length >= 18) ? spinSprites.Skip(9).Take(9).ToArray() : spinDown;
        Sprite[] spinRight = (spinSprites.Length >= 27) ? spinSprites.Skip(18).Take(9).ToArray() : spinDown;
        Sprite[] spinUp = (spinSprites.Length >= 36) ? spinSprites.Skip(27).Take(9).ToArray() : spinDown;

        // Clips Creation
        AnimationClip clipIdleDown = CreateClip("Player4_Idle_Down", idleDown, 6f, true, false);
        AnimationClip clipIdleLeft = CreateClip("Player4_Idle_Left", idleLeft, 6f, true, false);
        AnimationClip clipIdleRight = CreateClip("Player4_Idle_Right", idleRight, 6f, true, false);
        AnimationClip clipIdleUp = CreateClip("Player4_Idle_Up", idleUp, 6f, true, false);

        AnimationClip clipWalkDown = CreateClip("Player4_Walk_Down", walkDown, 10f, true, false);
        AnimationClip clipWalkLeft = CreateClip("Player4_Walk_Left", walkLeft, 10f, true, false);
        AnimationClip clipWalkRight = CreateClip("Player4_Walk_Right", walkRight, 10f, true, false);
        AnimationClip clipWalkUp = CreateClip("Player4_Walk_Up", walkUp, 10f, true, false);

        AnimationClip clipRunDown = CreateClip("Player4_Run_Down", runDown, 12f, true, false);
        AnimationClip clipRunLeft = CreateClip("Player4_Run_Left", runLeft, 12f, true, false);
        AnimationClip clipRunRight = CreateClip("Player4_Run_Right", runRight, 12f, true, false);
        AnimationClip clipRunUp = CreateClip("Player4_Run_Up", runUp, 12f, true, false);

        AnimationClip clipAttackDown = CreateClip("Player4_Attack_Down", attackDown, 12f, false, false);
        AnimationClip clipAttackLeft = CreateClip("Player4_Attack_Left", attackLeft, 12f, false, false);
        AnimationClip clipAttackRight = CreateClip("Player4_Attack_Right", attackRight, 12f, false, false);
        AnimationClip clipAttackUp = CreateClip("Player4_Attack_Up", attackUp, 12f, false, false);

        AnimationClip clipAttack2Down = CreateClip("Player4_Attack2_Down", attack2Down, 12f, false, false);
        AnimationClip clipAttack2Left = CreateClip("Player4_Attack2_Left", attack2Left, 12f, false, false);
        AnimationClip clipAttack2Right = CreateClip("Player4_Attack2_Right", attack2Right, 12f, false, false);
        AnimationClip clipAttack2Up = CreateClip("Player4_Attack2_Up", attack2Up, 12f, false, false);

        AnimationClip clipSkillDown = CreateClip("Player4_Skill_Down", skillDown, 10f, false, false);
        AnimationClip clipSkillLeft = CreateClip("Player4_Skill_Left", skillLeft, 10f, false, false);
        AnimationClip clipSkillRight = CreateClip("Player4_Skill_Right", skillRight, 10f, false, false);
        AnimationClip clipSkillUp = CreateClip("Player4_Skill_Up", skillUp, 10f, false, false);

        AnimationClip clipSpinDown = CreateClip("Player4_Spin_Down", spinDown, 12f, true, false);
        AnimationClip clipSpinLeft = CreateClip("Player4_Spin_Left", spinLeft, 12f, true, false);
        AnimationClip clipSpinRight = CreateClip("Player4_Spin_Right", spinRight, 12f, true, false);
        AnimationClip clipSpinUp = CreateClip("Player4_Spin_Up", spinUp, 12f, true, false);

        AnimationClip clipHurt = CreateClip("Player4_Hurt", hurtSprites.Take(3).ToArray(), 8f, false, false);
        AnimationClip clipDeath = CreateClip("Player4_Death", deathSprites.Take(6).ToArray(), 6f, false, false);

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
        EnsureParameter(controller, "SkillTrigger", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "SpinTrigger", AnimatorControllerParameterType.Trigger);
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
        moveTree.AddChild(clipWalkDown, new Vector2(0, -1));
        moveTree.AddChild(clipWalkUp, new Vector2(0, 1));
        moveTree.AddChild(clipWalkRight, new Vector2(1, 0));
        moveTree.AddChild(clipWalkLeft, new Vector2(-1, 0));
        AssetDatabase.AddObjectToAsset(moveTree, controller);

        var moveState = rootSm.AddState("Move");
        moveState.motion = moveTree;

        // 3. Blend Tree Attack 1 (Crescent Slash)
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

        // 4. Blend Tree Attack 2 (Heavy Bash)
        var attack2Tree = new BlendTree { name = "Attack2Tree", blendType = BlendTreeType.SimpleDirectional2D };
        attack2Tree.blendParameter = "MoveX";
        attack2Tree.blendParameterY = "MoveY";
        attack2Tree.AddChild(clipAttack2Down, new Vector2(0, -1));
        attack2Tree.AddChild(clipAttack2Up, new Vector2(0, 1));
        attack2Tree.AddChild(clipAttack2Right, new Vector2(1, 0));
        attack2Tree.AddChild(clipAttack2Left, new Vector2(-1, 0));
        AssetDatabase.AddObjectToAsset(attack2Tree, controller);

        var attack2State = rootSm.AddState("Attack2");
        attack2State.motion = attack2Tree;

        // 5. Blend Tree Spin (Whirlwind 360)
        var spinTree = new BlendTree { name = "SpinTree", blendType = BlendTreeType.SimpleDirectional2D };
        spinTree.blendParameter = "MoveX";
        spinTree.blendParameterY = "MoveY";
        spinTree.AddChild(clipSpinDown, new Vector2(0, -1));
        spinTree.AddChild(clipSpinUp, new Vector2(0, 1));
        spinTree.AddChild(clipSpinRight, new Vector2(1, 0));
        spinTree.AddChild(clipSpinLeft, new Vector2(-1, 0));
        AssetDatabase.AddObjectToAsset(spinTree, controller);

        var spinState = rootSm.AddState("Spin");
        spinState.motion = spinTree;

        var anyToSpin = rootSm.AddAnyStateTransition(spinState);
        anyToSpin.AddCondition(AnimatorConditionMode.If, 0, "SpinTrigger");
        anyToSpin.hasExitTime = false;
        anyToSpin.canTransitionToSelf = false;
        anyToSpin.duration = 0f;

        // Spin state: Duy trì lặp liên tục trong toàn bộ thời gian 3.2s của SpinWhirlwindRoutine (khi hết 3.2s code sẽ tự chuyển về Idle)

        // 6. Blend Tree Skill (Cataclysm Earth Slam)
        var skillTree = new BlendTree { name = "SkillTree", blendType = BlendTreeType.SimpleDirectional2D };
        skillTree.blendParameter = "MoveX";
        skillTree.blendParameterY = "MoveY";
        skillTree.AddChild(clipSkillDown, new Vector2(0, -1));
        skillTree.AddChild(clipSkillUp, new Vector2(0, 1));
        skillTree.AddChild(clipSkillRight, new Vector2(1, 0));
        skillTree.AddChild(clipSkillLeft, new Vector2(-1, 0));
        AssetDatabase.AddObjectToAsset(skillTree, controller);

        var skillState = rootSm.AddState("Skill");
        skillState.motion = skillTree;

        var anyToSkill = rootSm.AddAnyStateTransition(skillState);
        anyToSkill.AddCondition(AnimatorConditionMode.If, 0, "SkillTrigger");
        anyToSkill.hasExitTime = false;
        anyToSkill.canTransitionToSelf = false;
        anyToSkill.duration = 0f;

        var skillToIdle = skillState.AddTransition(idleState);
        skillToIdle.hasExitTime = true;
        skillToIdle.exitTime = 0.92f;
        skillToIdle.duration = 0.05f;

        // 5. Hurt State
        var hurtState = rootSm.AddState("Hurt");
        hurtState.motion = clipHurt;

        // 6. Death State
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
        anyToAttack.canTransitionToSelf = false;
        anyToAttack.duration = 0f;

        var attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 0.92f;
        attackToIdle.duration = 0.05f;

        var attack1ToIdle = attack1State.AddTransition(idleState);
        attack1ToIdle.hasExitTime = true;
        attack1ToIdle.exitTime = 0.92f;
        attack1ToIdle.duration = 0.05f;

        var attack2ToIdle = attack2State.AddTransition(idleState);
        attack2ToIdle.hasExitTime = true;
        attack2ToIdle.exitTime = 0.92f;
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
        Debug.Log("<color=green>[PLAYER 4 SETUP]</color> Hoàn tất cài đặt Player 4 Animator Controller sạch 100%!");
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
