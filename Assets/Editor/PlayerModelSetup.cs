#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools 메뉴에서 Soldier_demo 모델을 Player에 붙이고,
// 16장의 Idle/Run 블렌드 트리 + Shoot 애니메이션을 구성한다.
public static class PlayerModelSetup
{
    private const string ModelPath = "Assets/LowPolySoldiers_demo/models/Soldier_demo.FBX";
    private const string IdlePath = "Assets/LowPolySoldiers_demo/animation/demo_combat_idle.FBX";
    private const string RunPath = "Assets/LowPolySoldiers_demo/animation/demo_combat_run.FBX";
    private const string ShootPath = "Assets/LowPolySoldiers_demo/animation/demo_combat_shoot.FBX";
    private const string ControllerPath = "Assets/07.Animations/Player/PlayerAC.controller";

    [MenuItem("Tools/Setup Player Model")]
    public static void SetupPlayerModel()
    {
        if (!ConfigureImports()) return;

        PlayerMove playerMove = Object.FindFirstObjectByType<PlayerMove>();
        if (playerMove == null)
        {
            EditorUtility.DisplayDialog("Player Model", "씬에서 PlayerMove를 찾지 못했습니다.", "확인");
            return;
        }

        GameObject player = playerMove.gameObject;
        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (modelAsset == null)
        {
            EditorUtility.DisplayDialog("Player Model", ModelPath + " 를 찾지 못했습니다.", "확인");
            return;
        }

        Transform oldModel = player.transform.Find("Model");
        if (oldModel != null)
        {
            Undo.DestroyObjectImmediate(oldModel.gameObject);
        }

        GameObject model = PrefabUtility.InstantiatePrefab(modelAsset, player.transform) as GameObject;
        if (model == null)
        {
            model = Object.Instantiate(modelAsset, player.transform);
        }

        Undo.RegisterCreatedObjectUndo(model, "Create Player Model");
        model.name = "Model";
        model.transform.localPosition = new Vector3(0f, -1f, 0f);
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;
        SetLayerRecursively(model, player.layer);

        Animator animator = model.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            animator = model.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = CreateController();
        animator.applyRootMotion = false;

        animator.avatar = LoadAvatar(ModelPath);

        foreach (MeshRenderer renderer in player.GetComponents<MeshRenderer>())
        {
            renderer.enabled = false;
        }

        Transform nose = player.transform.Find("Nose");
        if (nose != null)
        {
            Undo.DestroyObjectImmediate(nose.gameObject);
        }

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.center = new Vector3(0f, -0.06f, 0f);
            controller.radius = 0.4f;
            controller.height = 1.8f;
            controller.skinWidth = 0.08f;
            PlaceModelAtCapsuleBottom(model.transform, controller);
        }

        Transform cameraPosition = player.transform.Find("CameraPosition");
        if (cameraPosition != null)
        {
            cameraPosition.localPosition = new Vector3(0.117f, 0.553f, 0.373f);
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.nearClipPlane = 0.01f;
        }

        AssignAnimator(playerMove, "_animator", animator);

        PlayerGunFire gunFire = player.GetComponent<PlayerGunFire>();
        if (gunFire != null)
        {
            AssignAnimator(gunFire, "_animator", animator);
        }

        GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            AssignAnimator(gameManager, "_playerAnimator", animator);
        }

        EditorSceneManager.MarkSceneDirty(player.scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = model;

        Debug.Log("[PlayerModel] Soldier_demo 모델과 애니메이션을 Player에 연결했습니다.");
    }

    private static AnimatorController CreateController()
    {
        EnsureFolder("Assets/07.Animations");
        EnsureFolder("Assets/07.Animations/Player");

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("MoveMotion", AnimatorControllerParameterType.Float);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

        AnimationClip idleClip = LoadClip(IdlePath, "demo_combat_idle");
        AnimationClip runClip = LoadClip(RunPath, "demo_combat_run");
        AnimationClip shootClip = LoadClip(ShootPath, "demo_combat_shoot");

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        BlendTree moveTree = new BlendTree
        {
            name = "MoveBlend",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "MoveMotion",
            useAutomaticThresholds = false
        };

        moveTree.AddChild(idleClip, 0f);
        moveTree.AddChild(runClip, 1f);
        AssetDatabase.AddObjectToAsset(moveTree, controller);

        AnimatorState moveState = stateMachine.AddState("MoveBlend");
        moveState.motion = moveTree;
        moveState.iKOnFeet = true;

        AnimatorState shootState = stateMachine.AddState("Shoot");
        shootState.motion = shootClip;
        shootState.iKOnFeet = true;

        stateMachine.defaultState = moveState;

        AnimatorStateTransition toShoot = moveState.AddTransition(shootState);
        toShoot.hasExitTime = false;
        toShoot.duration = 0.05f;
        toShoot.AddCondition(AnimatorConditionMode.If, 0f, "Attack");

        AnimatorStateTransition backToMove = shootState.AddTransition(moveState);
        backToMove.hasExitTime = true;
        backToMove.exitTime = 0.9f;
        backToMove.duration = 0.1f;

        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(ControllerPath);
        return controller;
    }

    [MenuItem("Tools/Repair Player Animation Rig")]
    public static void RepairPlayerAnimationRig()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[PlayerModel] Play를 종료한 뒤 Rig 복구를 실행하세요.");
            return;
        }

        if (!ConfigureImports()) return;
        EnableFootIK();
        PlayerMove player = Object.FindFirstObjectByType<PlayerMove>();
        if (player != null)
        {
            Transform model = player.transform.Find("Model");
            Animator animator = model != null ? model.GetComponentInChildren<Animator>() : null;
            if (animator != null)
            {
                Undo.RecordObject(animator, "Repair Player Root Motion");
                animator.avatar = LoadAvatar(ModelPath);
                animator.applyRootMotion = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
                CharacterController controller = player.GetComponent<CharacterController>();
                if (controller != null) PlaceModelAtCapsuleBottom(model, controller);
                EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[PlayerModel] Humanoid / Bake Into Pose / Y 기준 Feet / Foot IK를 적용했습니다. 씬 변경을 저장하세요.");
    }

    private static bool ConfigureImports()
    {
        // 원본 Avatar를 먼저 완성해야 클립이 Generic/미생성 Avatar를 복사하지 않는다.
        ModelImporter modelImporter = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (modelImporter == null)
        {
            Debug.LogError("[PlayerModel] 모델을 찾지 못했습니다: " + ModelPath);
            return false;
        }

        modelImporter.globalScale = 0.7f;
        modelImporter.animationType = ModelImporterAnimationType.Human;
        modelImporter.sourceAvatar = null;
        modelImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        modelImporter.SaveAndReimport();

        Avatar avatar = LoadAvatar(ModelPath);
        if (avatar == null || !avatar.isValid || !avatar.isHuman)
        {
            Debug.LogError("[PlayerModel] Soldier_demo의 Humanoid Avatar가 유효하지 않습니다. 모델의 Rig > Configure에서 뼈 매핑을 확인하세요.");
            return false;
        }

        bool idleReady = ConfigureAnimation(IdlePath, avatar);
        bool runReady = ConfigureAnimation(RunPath, avatar);
        bool shootReady = ConfigureAnimation(ShootPath, avatar);
        return idleReady && runReady && shootReady;
    }

    private static bool ConfigureAnimation(string path, Avatar avatar)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError("[PlayerModel] 애니메이션을 찾지 못했습니다: " + path);
            return false;
        }
        importer.globalScale = 0.7f;
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
        importer.sourceAvatar = avatar;
        // Inspector의 Copy From Other Avatar와 동일하게 생성된 Avatar의 데이터를 복사한다.
        using (SerializedObject avatarData = new SerializedObject(avatar))
        using (SerializedObject importerData = new SerializedObject(importer))
        {
            SerializedProperty description = avatarData.FindProperty("m_HumanDescription");
            if (description == null)
            {
                Debug.LogError("[PlayerModel] Avatar의 HumanDescription을 읽지 못했습니다: " + path);
                return false;
            }
            importerData.CopyFromSerializedProperty(description);
            importerData.ApplyModifiedPropertiesWithoutUndo();
        }
        ModelImporterClipAnimation[] clips = importer.clipAnimations;
        if (clips.Length == 0) clips = importer.defaultClipAnimations;
        foreach (ModelImporterClipAnimation clip in clips)
        {
            // 세 루트 축은 Bake Into Pose. 높이는 이 에셋의 발을 기준으로 통일한다.
            // 이동/회전/점프는 PlayerMove와 PlayerRotate가 담당한다.
            clip.lockRootRotation = true;
            clip.lockRootHeightY = true;
            clip.lockRootPositionXZ = true;
            clip.keepOriginalOrientation = true;
            clip.keepOriginalPositionY = false;
            clip.keepOriginalPositionXZ = true;
            clip.heightFromFeet = true;
            clip.rotationOffset = 0f;
            clip.heightOffset = 0f;
            clip.loopTime = path != ShootPath;
        }
        importer.clipAnimations = clips;
        importer.SaveAndReimport();
        return true;
    }

    private static void PlaceModelAtCapsuleBottom(Transform model, CharacterController controller)
    {
        // 클립의 Y 기준이 Feet이므로 모델 원점을 캡슐 바닥에 둔다.
        // 특정 Idle 프레임의 메시 경계로 높이를 보정하면 다른 프레임/상태에서 다시 뜬다.
        Vector3 capsuleBottom = controller.transform.TransformPoint(controller.center
            - Vector3.up * (controller.height * 0.5f));
        Vector3 position = model.localPosition;
        position.y = model.parent.InverseTransformPoint(capsuleBottom).y;
        Undo.RecordObject(model, "Place Player Model At Capsule Bottom");
        model.localPosition = position;
        PrefabUtility.RecordPrefabInstancePropertyModifications(model);
        Debug.Log($"[PlayerModel] Feet 기준 Model Y={position.y:F3}. 이전 Idle 메시 높이 보정을 해제했습니다.", model);
    }

    private static void EnableFootIK()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null || controller.layers.Length == 0)
        {
            Debug.LogWarning("[PlayerModel] PlayerAC를 찾지 못해 Foot IK를 적용하지 못했습니다.");
            return;
        }

        // 기존 블렌드 트리, 파라미터, 전환은 보존한다.
        foreach (ChildAnimatorState child in controller.layers[0].stateMachine.states)
        {
            AnimatorState state = child.state;
            if (state.name != "MoveBlend" && state.name != "Shoot") continue;
            Undo.RecordObject(state, "Enable Player Foot IK");
            state.iKOnFeet = true;
            EditorUtility.SetDirty(state);
        }
    }

    private static Avatar LoadAvatar(string fbxPath)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
        {
            if (asset is Avatar avatar)
            {
                return avatar;
            }
        }

        return null;
    }

    private static AnimationClip LoadClip(string fbxPath, string clipName)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
        {
            if (asset is AnimationClip clip && clip.name == clipName)
            {
                return clip;
            }
        }

        return null;
    }

    private static void AssignAnimator(Component target, string propertyName, Animator animator)
    {
        SerializedObject serializedTarget = new SerializedObject(target);
        SerializedProperty property = serializedTarget.FindProperty(propertyName);
        if (property == null)
        {
            return;
        }

        property.objectReferenceValue = animator;
        serializedTarget.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(parent))
        {
            return;
        }

        AssetDatabase.CreateFolder(parent.Replace("\\", "/"), System.IO.Path.GetFileName(path));
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;

        foreach (Transform child in target.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
#endif
