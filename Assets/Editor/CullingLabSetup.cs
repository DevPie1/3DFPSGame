using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 22.2~22.4 전용 실험 씬. 게임 씬과 LOD 설정은 변경하지 않는다.
public static class CullingLabSetup
{
    private const string ScenePath = "Assets/01.Scenes/CullingLab.unity";
    private const string MaterialFolder = "Assets/09.materials/CullingLab";
    private static Scene _bakingScene;

    [MenuItem("Tools/CullingLab/Setup And Bake Through 22.4")]
    public static void Configure()
    {
        Scene scene = RequireLab();
        if (StaticOcclusionCulling.isRunning) throw new InvalidOperationException("오클루전 Bake가 진행 중입니다.");
        Directory.CreateDirectory(".utmp");
        if (!File.Exists(".utmp/CullingLab.before-22-4.unity"))
            EditorSceneManager.SaveScene(scene, ".utmp/CullingLab.before-22-4.unity", true);
        SpartanCombatAnimations.EnsureFolder(MaterialFolder);
        Material yellow = MakeMaterial("Yellow", Color.yellow);
        Material green = MakeMaterial("Green", Color.green);
        Material ground = MakeMaterial("Ground", new Color(.45f, .47f, .5f));

        // 노션 표의 Plane Scale을 그대로 따른다. 기본 Plane은 10m이므로 실제 폭은 300m다.
        MakePrimitive(scene, "Ground", PrimitiveType.Plane, new Vector3(0, 0, 7), new Vector3(30, 1, 30), ground, StaticEditorFlags.OccluderStatic);
        MakePrimitive(scene, "Wall", PrimitiveType.Cube, new Vector3(0, 3, 0), new Vector3(6, 6, 1), yellow, StaticEditorFlags.OccluderStatic);
        for (int i = 0; i < 3; i++)
            MakePrimitive(scene, "Target" + (i + 1), PrimitiveType.Cube, new Vector3(-2 + i * 2, 1, 5 + i * 2), new Vector3(1, 2, 1), green, StaticEditorFlags.OccludeeStatic);

        RestoreFrontView();
        StaticOcclusionCulling.SetDefaultOcclusionBakeSettings();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        _bakingScene = scene;
        if (!StaticOcclusionCulling.GenerateInBackground()) throw new InvalidOperationException("오클루전 Bake를 시작하지 못했습니다.");
        EditorApplication.update -= FinishBake;
        EditorApplication.update += FinishBake;
        EditorApplication.ExecuteMenuItem("Window/Rendering/Occlusion Culling");
        Selection.activeGameObject = LabCamera(scene).gameObject;
    }

    private static void FinishBake()
    {
        if (StaticOcclusionCulling.isRunning) return;
        EditorApplication.update -= FinishBake;
        if (!_bakingScene.IsValid() || !_bakingScene.isLoaded) return;
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(_bakingScene);
        EditorSceneManager.SaveScene(_bakingScene);
        SceneView.RepaintAll();
        Debug.Log("[CullingLab] 22.4 오클루전 Bake 완료. Visualization 또는 Game Stats에서 비교하세요.");
    }

    [MenuItem("Tools/CullingLab/22.3 Far Clip 10 (Wall Off)")]
    public static void ShowFarClip()
    {
        Scene scene = RequireLab();
        ShowFrustumBaseline();
        Camera camera = LabCamera(scene);
        Undo.RecordObject(camera, "Far Clip Experiment"); camera.farClipPlane = 10;
        EditorSceneManager.MarkSceneDirty(scene);
    }

    [MenuItem("Tools/CullingLab/22.3 Cubes Visible (Wall Off, Far 1000)")]
    public static void ShowFrustumBaseline()
    {
        Scene scene = RequireLab();
        RestoreFrontView();
        Camera camera = LabCamera(scene);
        // 이미 Bake된 벽의 가림 정보가 프러스텀 실험에 섞이지 않도록 잠시 끈다.
        Undo.RecordObject(camera, "Disable Occlusion For Frustum Experiment"); camera.useOcclusionCulling = false;
        GameObject wall = scene.GetRootGameObjects().Single(g => g.name == "Wall");
        Undo.RecordObject(wall, "Hide Wall For Frustum Experiment"); wall.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    [MenuItem("Tools/CullingLab/22.4 Side View")]
    public static void ShowSideView()
    {
        Scene scene = RequireLab();
        RestoreFrontView();
        Camera camera = LabCamera(scene);
        Undo.RecordObject(camera.transform, "Side View Experiment");
        camera.transform.SetPositionAndRotation(new Vector3(8, 2, -8), Quaternion.Euler(0, -25, 0));
        EditorSceneManager.MarkSceneDirty(scene);
    }

    [MenuItem("Tools/CullingLab/Restore Front View")]
    public static void RestoreFrontView()
    {
        Scene scene = RequireLab();
        Camera camera = LabCamera(scene);
        Undo.RecordObject(camera.transform, "Restore Culling Camera");
        Undo.RecordObject(camera, "Restore Culling Projection");
        camera.transform.SetPositionAndRotation(new Vector3(0, 2, -8), Quaternion.identity);
        camera.orthographic = false; camera.fieldOfView = 60;
        camera.nearClipPlane = .3f; camera.farClipPlane = 1000;
        camera.useOcclusionCulling = true;
        GameObject wall = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Wall");
        if (wall != null) { Undo.RecordObject(wall, "Restore Wall"); wall.SetActive(true); }
        Selection.activeGameObject = camera.gameObject;
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static Scene RequireLab()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("설정 변경은 Play를 종료하고 실행하세요.");
        if (scene.path != ScenePath || SceneManager.sceneCount != 1)
            throw new InvalidOperationException("CullingLab만 단독으로 연 상태에서 실행하세요.");
        return scene;
    }

    private static Camera LabCamera(Scene scene)
    {
        Camera camera = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>()).FirstOrDefault(c => c.CompareTag("MainCamera"));
        if (camera == null) throw new InvalidOperationException("CullingLab의 Main Camera가 없습니다.");
        return camera;
    }

    private static void MakePrimitive(Scene scene, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, StaticEditorFlags flags)
    {
        GameObject go = scene.GetRootGameObjects().FirstOrDefault(g => g.name == name);
        if (go == null) { go = GameObject.CreatePrimitive(type); go.name = name; Undo.RegisterCreatedObjectUndo(go, "Create Culling Lab Object"); }
        Undo.RecordObject(go.transform, "Place Culling Lab Object");
        go.transform.SetPositionAndRotation(position, Quaternion.identity); go.transform.localScale = scale;
        go.SetActive(true); GameObjectUtility.SetStaticEditorFlags(go, flags);
        Renderer renderer = go.GetComponent<Renderer>();
        Undo.RecordObject(renderer, "Color Culling Lab Object"); renderer.sharedMaterial = material;
    }

    private static Material MakeMaterial(string name, Color color)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
        material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", .2f);
        EditorUtility.SetDirty(material);
        return material;
    }
}
