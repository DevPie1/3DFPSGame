#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Tools 메뉴에서 Zombie1 모델을 기존 Zombie 프리팹에 붙이고,
// FSM 상태에 맞는 Animator Controller를 만들어 연결한다.
public static class ZombieVisualSetup
{
    private const string ZombiePrefabPath = "Assets/03.Prefabs/Monster/Zombie.prefab";
    private const string VisualPrefabPath = "Assets/Zombie/Prefabs/Zombie1.prefab";
    private const string AnimationFolder = "Assets/Zombie/Animations/";
    private const string ControllerPath = AnimationFolder + "Zombie_AI.controller";

    [MenuItem("Tools/Setup Zombie Visual")]
    public static void SetupZombieVisual()
    {
        GameObject visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPrefabPath);
        if (visualPrefab == null)
        {
            EditorUtility.DisplayDialog("Zombie Visual", VisualPrefabPath + " 를 찾지 못했습니다.", "확인");
            return;
        }

        ZombieVisualFix.ConvertMaterials();

        AnimatorController controller = CreateController();
        GameObject root = PrefabUtility.LoadPrefabContents(ZombiePrefabPath);

        try
        {
            // 기존 캡슐 메쉬는 숨기고, 실제 모델을 자식으로 넣는다.
            foreach (MeshFilter filter in root.GetComponents<MeshFilter>())
            {
                Object.DestroyImmediate(filter, true);
            }

            foreach (MeshRenderer renderer in root.GetComponents<MeshRenderer>())
            {
                Object.DestroyImmediate(renderer, true);
            }

            Transform oldVisual = root.transform.Find("ZombieVisual");
            if (oldVisual != null)
            {
                Object.DestroyImmediate(oldVisual.gameObject, true);
            }

            GameObject visual = Object.Instantiate(visualPrefab, root.transform);
            visual.name = "ZombieVisual";
            visual.transform.localPosition = new Vector3(0f, -1f, 0f);
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            SetLayerRecursively(visual, root.layer);

            Animator animator = visual.GetComponent<Animator>();
            if (animator == null)
            {
                animator = visual.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            ZombieAnimation zombieAnimation = root.GetComponent<ZombieAnimation>();
            if (zombieAnimation == null)
            {
                zombieAnimation = root.AddComponent<ZombieAnimation>();
            }

            SerializedObject serializedAnimation = new SerializedObject(zombieAnimation);
            serializedAnimation.FindProperty("_animator").objectReferenceValue = animator;
            serializedAnimation.ApplyModifiedPropertiesWithoutUndo();

            ZombieHealthBar healthBar = root.GetComponent<ZombieHealthBar>();
            if (healthBar != null)
            {
                SerializedObject serializedHealthBar = new SerializedObject(healthBar);
                serializedHealthBar.FindProperty("_heightOffset").floatValue = 1.5f;
                serializedHealthBar.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(root, ZombiePrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[ZombieVisual] Zombie1 모델과 애니메이션을 Zombie 프리팹에 연결했습니다.");
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(ZombiePrefabPath);
    }

    private static AnimatorController CreateController()
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        AnimatorState idle = AddState(stateMachine, "Idle", "Z_Idle.anim");
        AnimatorState walk = AddState(stateMachine, "Walk", "Z_Walk_InPlace.anim");
        AnimatorState run = AddState(stateMachine, "Run", "Z_Run_InPlace.anim");
        AnimatorState attack = AddState(stateMachine, "Attack", "Z_Attack.anim");
        AnimatorState hit = AddState(stateMachine, "Hit", "Z_Idle.anim");
        AnimatorState death = AddState(stateMachine, "Death", "Z_FallingForward.anim");

        stateMachine.defaultState = idle;

        AddSpeedTransition(idle, walk, AnimatorConditionMode.Greater, 0.1f);
        AddSpeedTransition(idle, run, AnimatorConditionMode.Greater, 4f);
        AddSpeedTransition(walk, idle, AnimatorConditionMode.Less, 0.1f);
        AddSpeedTransition(walk, run, AnimatorConditionMode.Greater, 4f);
        AddSpeedTransition(run, walk, AnimatorConditionMode.Less, 4f);
        AddSpeedTransition(run, idle, AnimatorConditionMode.Less, 0.1f);

        AddAnyStateTrigger(stateMachine, attack, "Attack");
        AddAnyStateTrigger(stateMachine, hit, "Hit");
        AddAnyStateBool(stateMachine, death, "Dead");

        AddTimedReturn(attack, idle, 0.9f);
        AddReturnTransition(attack, walk, AnimatorConditionMode.Greater, 0.1f);
        AddTimedReturn(hit, idle, 0.9f);
        AddReturnTransition(hit, walk, AnimatorConditionMode.Greater, 0.1f);

        AssetDatabase.SaveAssets();
        return controller;
    }

    private static AnimatorState AddState(AnimatorStateMachine stateMachine, string name, string clipName)
    {
        AnimatorState state = stateMachine.AddState(name);
        state.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimationFolder + clipName);
        state.speed = 1f;
        return state;
    }

    private static void AddSpeedTransition(
        AnimatorState from,
        AnimatorState to,
        AnimatorConditionMode mode,
        float threshold)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.1f;
        transition.AddCondition(mode, threshold, "Speed");
    }

    private static void AddAnyStateTrigger(
        AnimatorStateMachine stateMachine,
        AnimatorState destination,
        string parameter)
    {
        AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(destination);
        transition.hasExitTime = false;
        transition.duration = 0.08f;
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
    }

    private static void AddAnyStateBool(
        AnimatorStateMachine stateMachine,
        AnimatorState destination,
        string parameter)
    {
        AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(destination);
        transition.hasExitTime = false;
        transition.duration = 0.1f;
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
    }

    private static void AddReturnTransition(
        AnimatorState from,
        AnimatorState to,
        AnimatorConditionMode mode,
        float threshold)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.15f;
        transition.AddCondition(mode, threshold, "Speed");
    }

    private static void AddTimedReturn(AnimatorState from, AnimatorState to, float exitTime)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = true;
        transition.exitTime = exitTime;
        transition.duration = 0.15f;
    }

    private static void SetLayerRecursively(GameObject gameObject, int layer)
    {
        gameObject.layer = layer;

        foreach (Transform child in gameObject.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
#endif
