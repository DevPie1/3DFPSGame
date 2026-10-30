using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// 같은 스파르탄 뼈대에 맞춘 곡선을 만들어 기존 FBX 축/Avatar 차이를 재현하지 않는다.
public static class SpartanCombatAnimations
{
    public const string Folder = "Assets/07.Animations/Player/Melee";
    public const string ControllerPath = Folder + "/SpartanCombat.controller";
    private const string SpartanPath = "Assets/05.Models/GreenSpartan_Rigged.fbx";
    private const string SoldierPath = "Assets/LowPolySoldiers_demo/models/Soldier_demo.FBX";
    private const string SwordSourcePath = "Assets/EEJANAI_Team/FreeSwordAnimations/Prefabs/EEJANAIbotSword1.prefab";

    public static AnimatorController Build()
    {
        EnsureFolder(Folder);
        AnimationClip idle = Bake("Idle", SoldierPath, LoadClip("Assets/LowPolySoldiers_demo/animation/demo_combat_idle.FBX"), true);
        AnimationClip run = Bake("Run", SoldierPath, LoadClip("Assets/LowPolySoldiers_demo/animation/demo_combat_run.FBX"), true);
        AnimationClip shoot = Bake("Shoot", SoldierPath, LoadClip("Assets/LowPolySoldiers_demo/animation/demo_combat_shoot.FBX"), false);
        AnimationClip bomb = Bake("ThrowBomb", SoldierPath, AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/07.Animations/Player/Player_BombLeftArm.anim"), false);
        AnimationClip slash = Bake("Slash", SwordSourcePath, AssetDatabase.LoadAssetAtPath<AnimationClip>(
            "Assets/EEJANAI_Team/FreeSwordAnimations/Animations/slash1.anim"), false);
        AnimationUtility.SetAnimationEvents(slash, new[]
        {
            new AnimationEvent { time = .35f, functionName = "ActivateCollider" },
            new AnimationEvent { time = .42f, functionName = "FanAttack" },
            new AnimationEvent { time = .48f, functionName = "DeactivateCollider" }
        });

        GameObject skeleton = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SpartanPath));
        AvatarMask upper;
        AvatarMask left;
        try
        {
            upper = MakeMask("UpperBodyMask", skeleton.transform, "/Spine");
            left = MakeMask("LeftArmMask", skeleton.transform, "/LeftShoulder");
        }
        finally { Object.DestroyImmediate(skeleton); }

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        foreach (AnimatorControllerLayer layer in controller.layers)
            foreach (ChildAnimatorState state in layer.stateMachine.states) layer.stateMachine.RemoveState(state.state);
        controller.layers = new[] { controller.layers[0] };
        controller.parameters = new AnimatorControllerParameter[0];
        controller.AddParameter("MoveMotion", AnimatorControllerParameterType.Float);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("ThrowBomb", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Slash", AnimatorControllerParameterType.Trigger);
        var tree = new BlendTree { name = "MoveBlend", blendParameter = "MoveMotion", blendType = BlendTreeType.Simple1D, useAutomaticThresholds = false };
        tree.AddChild(idle, 0f);
        tree.AddChild(run, 1f);
        AssetDatabase.AddObjectToAsset(tree, controller);
        AnimatorState move = controller.layers[0].stateMachine.AddState("MoveBlend");
        move.motion = tree;
        controller.layers[0].stateMachine.defaultState = move;
        AddOverlay(controller, "Fire Layer", "Shoot", "Attack", shoot, upper, .02f, .9f);
        AddOverlay(controller, "Bomb Left Arm", "ThrowBomb", "ThrowBomb", bomb, left, .05f, .95f);
        AddOverlay(controller, "Melee Layer", "Slash", "Slash", slash, upper, .1f, .9f);
        ConfigureMeleeIdle(controller);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    private static void AddOverlay(AnimatorController controller, string layerName, string stateName,
        string trigger, AnimationClip clip, AvatarMask mask, float fade, float exitTime)
    {
        controller.AddLayer(layerName);
        var layers = controller.layers;
        var layer = layers[layers.Length - 1];
        layer.defaultWeight = 1f;
        layer.avatarMask = mask;
        AnimatorState empty = layer.stateMachine.AddState("Empty");
        AnimatorState action = layer.stateMachine.AddState(stateName);
        action.motion = clip;
        layer.stateMachine.defaultState = empty;
        AnimatorStateTransition enter = empty.AddTransition(action);
        enter.hasExitTime = false;
        enter.hasFixedDuration = true;
        enter.duration = fade;
        enter.AddCondition(AnimatorConditionMode.If, 0, trigger);
        AnimatorStateTransition exit = action.AddTransition(empty);
        exit.hasExitTime = true;
        exit.exitTime = exitTime;
        exit.hasFixedDuration = true;
        exit.duration = fade;
        controller.layers = layers;
    }

    // 기존 컨트롤러에 대기 자세만 갱신할 때 전체 Humanoid 변환을 다시 굽지 않는다.
    [MenuItem("Tools/Player/Update Katana Idle Pose")]
    public static void ConfigureMeleeIdle()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play를 종료하세요.");
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) throw new InvalidOperationException("SpartanCombat.controller가 필요합니다.");
        ConfigureMeleeIdle(controller);
        AssetDatabase.SaveAssets();
    }

    private static void ConfigureMeleeIdle(AnimatorController controller)
    {
        Scene scene = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SpartanPath));
            SceneManager.MoveGameObjectToScene(model, scene);
            model.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            model.transform.localScale = Vector3.one;
            Animator animator = model.GetComponent<Animator>();
            animator.avatar = null; // 이 프로젝트의 클립은 대상 리그의 Generic Transform 곡선이다.
            animator.enabled = false;
            AnimationClip idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "/Idle.anim");
            idle.SampleAnimation(model, 0f);
            Transform[] bones = model.GetComponentsInChildren<Transform>();
            Transform shoulder = bones.Single(t => t.name == "RightShoulder");
            Transform upper = bones.Single(t => t.name == "RightUpperArm");
            Transform lower = bones.Single(t => t.name == "RightLowerArm");
            Transform hand = bones.Single(t => t.name == "RightHand");

            // 쇄골을 편 뒤, 팔꿈치가 몸 바깥으로 굽도록 두 관절을 풀어 손을 앞/옆으로 옮긴다.
            Quaternion oldHand = hand.rotation;
            Quaternion swordSocket = Quaternion.Euler(314.28f, 266.16f, 65.89f);
            Vector3 oldBlade = oldHand * (swordSocket * Vector3.up);
            Quaternion handRotation = Quaternion.FromToRotation(oldBlade, new Vector3(.15f, .90f, .40f).normalized) * oldHand;
            Vector3 shoulderDirection = new Vector3(.25f, -.02f, -.03f).normalized;
            shoulder.rotation = Quaternion.FromToRotation(upper.position - shoulder.position, shoulderDirection) * shoulder.rotation;
            Vector3 target = new Vector3(.43f, 1.24f, .25f);
            Vector3 start = upper.position;
            float a = Vector3.Distance(start, lower.position);
            float b = Vector3.Distance(lower.position, hand.position);
            Vector3 direction = (target - start).normalized;
            float distance = Mathf.Clamp(Vector3.Distance(start, target), Mathf.Abs(a - b) + .001f, a + b - .001f);
            float along = (a * a - b * b + distance * distance) / (2f * distance);
            Vector3 pole = Vector3.ProjectOnPlane(Vector3.right + Vector3.back * .3f, direction).normalized;
            Vector3 elbow = start + direction * along + pole * Mathf.Sqrt(Mathf.Max(0f, a * a - along * along));
            upper.rotation = Quaternion.FromToRotation(lower.position - start, elbow - start) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, target - lower.position) * lower.rotation;
            hand.rotation = handRotation;

            string path = Folder + "/MeleeIdle.anim";
            AnimationClip pose = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (pose == null) { pose = new AnimationClip { name = "MeleeIdle" }; AssetDatabase.CreateAsset(pose, path); }
            pose.ClearCurves();
            pose.frameRate = 60;
            // 손가락까지 현재 파지 모양을 유지하되 왼팔/머리/하체에는 곡선을 쓰지 않는다.
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(idle).Where(c => c.path.Contains("/RightShoulder")))
            {
                AnimationCurve original = AnimationUtility.GetEditorCurve(idle, binding);
                AnimationUtility.SetEditorCurve(pose, binding, AnimationCurve.Constant(0, 1, original.Evaluate(0)));
            }
            foreach (Transform bone in new[] { shoulder, upper, lower, hand })
            {
                Quaternion q = bone.localRotation;
                string bonePath = AnimationUtility.CalculateTransformPath(bone, model.transform);
                float[] values = { q.x, q.y, q.z, q.w };
                string[] axes = { "x", "y", "z", "w" };
                for (int i = 0; i < 4; i++)
                    AnimationUtility.SetEditorCurve(pose, EditorCurveBinding.FloatCurve(bonePath, typeof(Transform), "m_LocalRotation." + axes[i]), AnimationCurve.Constant(0, 1, values[i]));
            }
            pose.EnsureQuaternionContinuity();
            var settings = AnimationUtility.GetAnimationClipSettings(pose);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(pose, settings);
            EditorUtility.SetDirty(pose);
            AvatarMask mask = MakeMask("RightArmMask", model.transform, "/RightShoulder");
            AnimatorControllerLayer idleLayer = controller.layers.FirstOrDefault(l => l.name == "Melee Idle");
            if (idleLayer == null)
            {
                controller.AddLayer("Melee Idle");
                idleLayer = controller.layers.Last();
            }
            idleLayer.avatarMask = mask;
            idleLayer.defaultWeight = 0f;
            AnimatorState guard = idleLayer.stateMachine.states.Select(s => s.state).FirstOrDefault(s => s.name == "Guard");
            if (guard == null) guard = idleLayer.stateMachine.AddState("Guard");
            guard.motion = pose;
            idleLayer.stateMachine.defaultState = guard;
            var layers = controller.layers.Where(l => l.name != "Melee Idle").ToList();
            int slashIndex = layers.FindIndex(l => l.name == "Melee Layer");
            layers.Insert(slashIndex < 0 ? layers.Count : slashIndex, idleLayer);
            controller.layers = layers.ToArray();
            EditorUtility.SetDirty(controller);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    private static AvatarMask MakeMask(string name, Transform root, string branch)
    {
        string path = Folder + "/" + name + ".mask";
        AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(path);
        if (mask == null) { mask = new AvatarMask { name = name }; AssetDatabase.CreateAsset(mask, path); }
        Transform[] bones = root.GetComponentsInChildren<Transform>();
        mask.transformCount = bones.Length;
        for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++) mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
        for (int i = 0; i < bones.Length; i++)
        {
            string bonePath = AnimationUtility.CalculateTransformPath(bones[i], root);
            mask.SetTransformPath(i, bonePath);
            mask.SetTransformActive(i, bonePath.Contains(branch));
        }
        EditorUtility.SetDirty(mask);
        return mask;
    }

    private static AnimationClip Bake(string name, string sourcePath, AnimationClip original, bool loop)
    {
        if (original == null || !original.humanMotion) throw new InvalidOperationException("Humanoid 클립 필요: " + name);
        string samplingPath = Folder + "/_Sampling.controller";
        Scene scene = EditorSceneManager.NewPreviewScene();
        GameObject source = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath));
        GameObject target = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SpartanPath));
        SceneManager.MoveGameObjectToScene(source, scene);
        SceneManager.MoveGameObjectToScene(target, scene);
        Animator sourceAnimator = source.GetComponentInChildren<Animator>();
        Animator targetAnimator = target.GetComponent<Animator>();
        AnimationClip samplingClip = Object.Instantiate(original);
        var samplingSettings = AnimationUtility.GetAnimationClipSettings(samplingClip);
        samplingSettings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(samplingClip, samplingSettings);
        var sampler = AnimatorController.CreateAnimatorControllerAtPath(samplingPath);
        AnimatorState samplingState = sampler.layers[0].stateMachine.AddState("Sample");
        samplingState.motion = samplingClip;
        sampler.layers[0].stateMachine.defaultState = samplingState;
        sourceAnimator.runtimeAnimatorController = sampler;
        sourceAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        sourceAnimator.applyRootMotion = true;
        sourceAnimator.Rebind();
        var sourceHandler = new HumanPoseHandler(sourceAnimator.avatar, sourceAnimator.transform);
        var targetHandler = new HumanPoseHandler(targetAnimator.avatar, targetAnimator.transform);
        var mesh = new Mesh();
        try
        {
            var clip = new AnimationClip { name = name, frameRate = 60 };
            Transform[] bones = target.GetComponentsInChildren<Transform>()
                .Where(t => t != target.transform && t.GetComponent<Renderer>() == null).ToArray();
            var curves = bones.ToDictionary(b => b, b => Enumerable.Range(0, 7).Select(i => new AnimationCurve()).ToArray());
            var firstPositions = new Dictionary<Transform, Vector3>();
            var firstRotations = new Dictionary<Transform, Quaternion>();
            var previousRotations = new Dictionary<Transform, Quaternion>();
            var skin = target.GetComponentsInChildren<SkinnedMeshRenderer>().First(r => r.name.EndsWith("LOD0"));
            var pose = new HumanPose();
            int frames = Mathf.Max(1, Mathf.RoundToInt(original.length * 60));
            for (int frame = 0; frame <= frames; frame++)
            {
                float phase = (float)frame / frames;
                sourceAnimator.Play("Sample", 0, phase);
                sourceAnimator.Update(0);
                sourceHandler.GetHumanPose(ref pose);
                pose.bodyPosition.x = pose.bodyPosition.z = 0;
                targetHandler.SetHumanPose(ref pose);
                skin.BakeMesh(mesh);
                float bottom = mesh.vertices.Min(v => skin.transform.TransformPoint(v).y);
                targetAnimator.GetBoneTransform(HumanBodyBones.Hips).position -= Vector3.up * bottom;
                if (frame == 0)
                    foreach (Transform bone in bones) { firstPositions[bone] = bone.localPosition; firstRotations[bone] = bone.localRotation; }
                float blend = loop ? Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.9f, 1, phase)) : 0;
                foreach (Transform bone in bones)
                {
                    Vector3 position = Vector3.Lerp(bone.localPosition, firstPositions[bone], blend);
                    Quaternion rotation = Quaternion.Slerp(bone.localRotation, firstRotations[bone], blend);
                    if (previousRotations.ContainsKey(bone) && Quaternion.Dot(previousRotations[bone], rotation) < 0)
                        rotation = new Quaternion(-rotation.x, -rotation.y, -rotation.z, -rotation.w);
                    previousRotations[bone] = rotation;
                    float[] values = { position.x, position.y, position.z, rotation.x, rotation.y, rotation.z, rotation.w };
                    for (int i = 0; i < 7; i++) curves[bone][i].AddKey(original.length * phase, values[i]);
                }
            }
            string[] properties = { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z",
                "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w" };
            foreach (Transform bone in bones)
                for (int i = 0; i < 7; i++)
                    AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(
                        AnimationUtility.CalculateTransformPath(bone, target.transform), typeof(Transform), properties[i]), Reduce(curves[bone][i], i < 3 ? .00001f : .0001f));
            clip.EnsureQuaternionContinuity();
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            string path = Folder + "/" + name + ".anim";
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing == null) AssetDatabase.CreateAsset(clip, path);
            else { EditorUtility.CopySerialized(clip, existing); Object.DestroyImmediate(clip); clip = existing; EditorUtility.SetDirty(clip); }
            return clip;
        }
        finally
        {
            sourceHandler.Dispose(); targetHandler.Dispose();
            EditorSceneManager.ClosePreviewScene(scene);
            Object.DestroyImmediate(mesh);
            Object.DestroyImmediate(samplingClip);
            AssetDatabase.DeleteAsset(samplingPath);
        }
    }

    private static AnimationCurve Reduce(AnimationCurve curve, float tolerance)
    {
        Keyframe[] keys = curve.keys;
        var keep = new bool[keys.Length];
        keep[0] = keep[keys.Length - 1] = true;
        var segments = new Stack<int[]>();
        segments.Push(new[] { 0, keys.Length - 1 });
        while (segments.Count > 0)
        {
            int[] segment = segments.Pop();
            int start = segment[0], end = segment[1], split = -1;
            float maximum = tolerance;
            for (int k = start + 1; k < end; k++)
            {
                float ratio = Mathf.InverseLerp(keys[start].time, keys[end].time, keys[k].time);
                float error = Mathf.Abs(keys[k].value - Mathf.Lerp(keys[start].value, keys[end].value, ratio));
                if (error > maximum) { maximum = error; split = k; }
            }
            if (split < 0) continue;
            keep[split] = true;
            segments.Push(new[] { start, split }); segments.Push(new[] { split, end });
        }
        var result = new AnimationCurve(keys.Where((k, i) => keep[i]).ToArray());
        for (int i = 0; i < result.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(result, i, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(result, i, AnimationUtility.TangentMode.Linear);
        }
        return result;
    }

    private static AnimationClip LoadClip(string path) => AssetDatabase.LoadAllAssetsAtPath(path)
        .OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"));

    public static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
        AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
    }
}
