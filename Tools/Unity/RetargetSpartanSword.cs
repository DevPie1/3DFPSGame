// Unity MCP CLI의 code execute --file 로 실행하는 Editor 전용 코드 본문이다.
// Free Sword slash3 -> HumanPose -> Spartan 본별 곡선. 런타임에서 재타기팅하지 않는다.
if (EditorApplication.isPlayingOrWillChangePlaymode)
    throw new InvalidOperationException("Play를 종료한 뒤 실행하세요.");
const string clipPath = "Assets/07.Animations/Player/SpartanSword/Slash3_SpartanLoop.anim";
const string controllerPath = "Assets/07.Animations/Player/SpartanSword/SpartanSwordLoop.controller";
var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
if (controller == null) throw new InvalidOperationException("SpartanSwordLoop 컨트롤러가 필요합니다.");
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var source = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
    "Assets/EEJANAI_Team/FreeSwordAnimations/Prefabs/EEJANAIbotSword1.prefab"));
var target = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
    "Assets/05.Models/GreenSpartan_Rigged.fbx"));
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(source, scene);
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(target, scene);
var sourceAnimator = source.GetComponentInChildren<Animator>();
var targetAnimator = target.GetComponent<Animator>();
var sourceHandler = new HumanPoseHandler(sourceAnimator.avatar, sourceAnimator.transform);
var targetHandler = new HumanPoseHandler(targetAnimator.avatar, targetAnimator.transform);
var mesh = new Mesh();
try
{
    sourceAnimator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
        "Assets/EEJANAI_Team/FreeSwordAnimations/Animations/Animator/slash3.controller");
    sourceAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    sourceAnimator.applyRootMotion = true;
    sourceAnimator.Rebind();
    var clip = new AnimationClip { name = "Slash3_SpartanLoop", frameRate = 60 };
    var bones = target.GetComponentsInChildren<Transform>()
        .Where(t => t != target.transform && t.GetComponent<Renderer>() == null).ToArray();
    var curves = new Dictionary<string, AnimationCurve[]>();
    foreach (var bone in bones)
        curves.Add(AnimationUtility.CalculateTransformPath(bone, target.transform),
            Enumerable.Range(0, 7).Select(i => new AnimationCurve()).ToArray());
    float duration = AssetDatabase.LoadAssetAtPath<AnimationClip>(
        "Assets/EEJANAI_Team/FreeSwordAnimations/Animations/slash3.anim").length;
    int frames = Mathf.RoundToInt(duration * 60);
    var pose = new HumanPose();
    var initialPositions = new Dictionary<Transform, Vector3>();
    var initialRotations = new Dictionary<Transform, Quaternion>();
    var previousRotations = new Dictionary<Transform, Quaternion>();
    var lod0 = target.GetComponentsInChildren<SkinnedMeshRenderer>().First(r => r.name.EndsWith("LOD0"));
    for (int frame = 0; frame <= frames; frame++)
    {
        float phase = (float)frame / frames;
        sourceAnimator.Play("slash3", 0, phase >= 1f ? 0f : phase);
        sourceAnimator.Update(0);
        sourceHandler.GetHumanPose(ref pose);
        pose.bodyPosition.x = 0;
        pose.bodyPosition.z = 0;
        targetHandler.SetHumanPose(ref pose);
        // 매 프레임 실제 메시의 발바닥을 원점에 둔다. 이동은 CharacterController가 담당한다.
        lod0.BakeMesh(mesh);
        float lowest = mesh.vertices.Min(v => lod0.transform.TransformPoint(v).y);
        targetAnimator.GetBoneTransform(HumanBodyBones.Hips).position -= Vector3.up * lowest;
        if (frame == 0)
            foreach (var bone in bones)
            {
                initialPositions[bone] = bone.localPosition;
                initialRotations[bone] = bone.localRotation;
            }
        // 마지막 구간을 시작 자세로 부드럽게 연결해 반복 경계의 튐을 줄인다.
        float closeLoop = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.84f, 1f, phase));
        foreach (var bone in bones)
        {
            var position = Vector3.Lerp(bone.localPosition, initialPositions[bone], closeLoop);
            var rotation = Quaternion.Slerp(bone.localRotation, initialRotations[bone], closeLoop);
            if (previousRotations.ContainsKey(bone) && Quaternion.Dot(previousRotations[bone], rotation) < 0)
                rotation = new Quaternion(-rotation.x, -rotation.y, -rotation.z, -rotation.w);
            previousRotations[bone] = rotation;
            var curveSet = curves[AnimationUtility.CalculateTransformPath(bone, target.transform)];
            var values = new[] { position.x, position.y, position.z, rotation.x, rotation.y, rotation.z, rotation.w };
            for (int i = 0; i < 7; i++) curveSet[i].AddKey(duration * phase, values[i]);
        }
    }
    var properties = new[] { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z",
        "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w" };
    foreach (var pair in curves)
        for (int i = 0; i < 7; i++)
        {
            var curve = pair.Value[i];
            // 일정한 뼈/선형 구간에 중복 키를 남기지 않는다.
            var keys = curve.keys;
            var keep = new bool[keys.Length];
            keep[0] = keep[keys.Length - 1] = true;
            var segments = new Stack<int[]>();
            segments.Push(new[] { 0, keys.Length - 1 });
            float tolerance = i < 3 ? 0.00001f : 0.0001f;
            while (segments.Count > 0)
            {
                var segment = segments.Pop();
                int start = segment[0], end = segment[1], split = -1;
                float maximum = tolerance;
                for (int key = start + 1; key < end; key++)
                {
                    float ratio = Mathf.InverseLerp(keys[start].time, keys[end].time, keys[key].time);
                    float error = Mathf.Abs(keys[key].value - Mathf.Lerp(keys[start].value, keys[end].value, ratio));
                    if (error > maximum) { maximum = error; split = key; }
                }
                if (split < 0) continue;
                keep[split] = true;
                segments.Push(new[] { start, split });
                segments.Push(new[] { split, end });
            }
            curve = new AnimationCurve(keys.Where((key, index) => keep[index]).ToArray());
            for (int key = 0; key < curve.length; key++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, key, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, key, AnimationUtility.TangentMode.Linear);
            }
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve(pair.Key, typeof(Transform), properties[i]), curve);
        }
    clip.EnsureQuaternionContinuity();
    var settings = AnimationUtility.GetAnimationClipSettings(clip);
    settings.loopTime = true;
    AnimationUtility.SetAnimationClipSettings(clip, settings);
    var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
    if (existing == null) AssetDatabase.CreateAsset(clip, clipPath);
    else
    {
        EditorUtility.CopySerialized(clip, existing);
        UnityEngine.Object.DestroyImmediate(clip);
        clip = existing;
        EditorUtility.SetDirty(clip);
    }
    var state = controller.layers[0].stateMachine.states.Single(s => s.state.name == "SwordSwing").state;
    state.motion = clip;
    state.iKOnFeet = false;
    EditorUtility.SetDirty(state);
    AssetDatabase.SaveAssets();
    return new { clip = clipPath, duration, frames, bones = bones.Length };
}
finally
{
    sourceHandler.Dispose();
    targetHandler.Dispose();
    UnityEngine.Object.DestroyImmediate(mesh);
    UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
}
