using System;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// 21장의 맵 이식과 배치만 구성한다. 하늘/안개/기존 주광은 유지한다.
public static class FactoryMapSetup
{
    private const string DemoPath = "Assets/RPG_FPS_game_assets_industrial/Map_v1.unity";
    private const string NavPath = "Assets/01.Scenes/GameScene/NavMesh-Map.asset";
    private const string DustPath = "Assets/RPG_FPS_game_assets_industrial/Particles/Dust/Dust_v1/Source/Dust_v1.mat";

    [MenuItem("Tools/Setup Chapter 21 Factory Map")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play를 종료하세요.");
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/01.Scenes/GameScene.unity") throw new InvalidOperationException("GameScene에서 실행하세요.");
        PlayerMove player = Object.FindFirstObjectByType<PlayerMove>();
        GameObject map = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Map");
        if (player == null || map == null) throw new InvalidOperationException("Player와 Map이 필요합니다.");
        Directory.CreateDirectory(".utmp");
        if (!File.Exists(".utmp/GameScene.before-factory-map.unity"))
            EditorSceneManager.SaveScene(scene, ".utmp/GameScene.before-factory-map.unity", true);
        if (File.Exists(NavPath) && !File.Exists(".utmp/NavMesh-Map.before-factory.asset"))
            File.Copy(NavPath, ".utmp/NavMesh-Map.before-factory.asset");

        Transform factory = map.transform.Find("Map_v1");
        if (factory == null) factory = ImportMap(map.transform, scene);
        Transform old = map.transform.Find("TestTerrain");
        if (old == null)
        {
            var group = new GameObject("TestTerrain"); Undo.RegisterCreatedObjectUndo(group, "Keep Test Terrain");
            group.transform.SetParent(map.transform, false); old = group.transform;
        }
        foreach (Transform child in map.transform.Cast<Transform>().ToArray())
            if (child != factory && child != old) Undo.SetTransformParent(child, old, "Archive Test Terrain");
        Undo.RecordObject(old.gameObject, "Disable Test Terrain"); old.gameObject.SetActive(false);
        // 가져온 맵의 중복 조명만 끄고 기존 주광과 환경 설정은 유지한다.
        Transform importedLight = factory.Find("Light");
        if (importedLight != null) importedLight.gameObject.SetActive(false);
        factory.gameObject.isStatic = true;
        FixDust();

        NavMeshSurface surface = map.GetComponent<NavMeshSurface>();
        if (surface == null) surface = Undo.AddComponent<NavMeshSurface>(map);
        Undo.RecordObject(surface, "Bake Factory Navigation");
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
        surface.layerMask = 1; surface.minRegionArea = .5f;
        BakeKeepingAsset(surface);
        PlaceGameplay(scene, player, surface);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = map;
    }

    private static Transform ImportMap(Transform parent, Scene target)
    {
        Scene preview = EditorSceneManager.OpenPreviewScene(DemoPath);
        try
        {
            GameObject root = preview.GetRootGameObjects().Single(g => g.name == "Map_v1");
            SceneManager.MoveGameObjectToScene(root, target);
            Undo.RegisterCreatedObjectUndo(root, "Import Factory Map");
            Undo.SetTransformParent(root.transform, parent, "Parent Factory Map");
            return root.transform;
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }

    private static void FixDust()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(DustPath);
        if (material == null) return;
        if (material.shader != null && material.shader.name == "Universal Render Pipeline/Particles/Unlit") return;
        Texture texture = material.GetTexture("_MainTex");
        material.shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 2);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)BlendMode.One);
        material.SetFloat("_ZWrite", 0); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.SetOverrideTag("RenderType", "Transparent"); material.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(material);
    }

    private static void BakeKeepingAsset(NavMeshSurface surface)
    {
        NavMeshData existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(NavPath);
        surface.BuildNavMesh();
        NavMeshData built = surface.navMeshData;
        if (built == null) throw new InvalidOperationException("공장 NavMesh 생성에 실패했습니다.");
        if (existing == null) AssetDatabase.CreateAsset(built, NavPath);
        else if (built != existing)
        {
            surface.RemoveData();
            EditorUtility.CopySerialized(built, existing);
            existing.name = "NavMesh-Map";
            EditorUtility.SetDirty(existing);
            surface.navMeshData = existing;
            surface.AddData();
            Object.DestroyImmediate(built);
        }
        EditorUtility.SetDirty(surface);
    }

    private static void PlaceGameplay(Scene scene, PlayerMove player, NavMeshSurface surface)
    {
        Undo.RecordObject(player.transform, "Move Player To Factory");
        CharacterController body = player.GetComponent<CharacterController>();
        bool enabled = body.enabled; body.enabled = false;
        player.transform.SetPositionAndRotation(new Vector3(10, 1, 0), Quaternion.identity);
        body.enabled = enabled;
        Camera main = Camera.main;
        if (main != null)
        {
            CameraRotate rotate = main.GetComponent<CameraRotate>();
            if (rotate != null) { SetFloat(rotate, "_accumulationX", 0); SetFloat(rotate, "_accumulationY", 0); }
            CameraFollow follow = main.GetComponent<CameraFollow>();
            Transform target = follow != null ? (Transform)new SerializedObject(follow).FindProperty("_target").objectReferenceValue : null;
            Undo.RecordObject(main.transform, "Move Camera To Player");
            main.transform.SetPositionAndRotation(target != null ? target.position : player.transform.position + Vector3.up * .55f, Quaternion.identity);
        }

        var filter = new NavMeshQueryFilter { agentTypeID = surface.agentTypeID, areaMask = NavMesh.AllAreas };
        ZombieSpawner normal = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ZombieSpawner>(true)).First(s => s.name == "ZombieSpawner");
        var settings = new SerializedObject(normal);
        SerializedProperty points = settings.FindProperty("_spawnPoints");
        for (int i = 0; i < points.arraySize; i++)
        {
            Transform marker = (Transform)points.GetArrayElementAtIndex(i).FindPropertyRelative("Position").objectReferenceValue;
            if (marker == null) continue;
            Vector3 position = i == 0 ? new Vector3(45, 1.9f, -29) : new Vector3(48, 1.9f, -31);
            PlaceOnNavMesh(marker, position, 1, filter);
        }
        Transform patrol = scene.GetRootGameObjects().Select(g => g.transform).FirstOrDefault(t => t.name == "ZombiePatrol_Pos");
        if (patrol != null)
        {
            Transform[] markers = patrol.Cast<Transform>().ToArray();
            for (int i = 0; i < markers.Length; i++)
                PlaceOnNavMesh(markers[i], i == 0 ? new Vector3(48, 1.9f, -31) : new Vector3(44, 1.9f, -24), .05f, filter);
        }
        ZombieSpawner elite = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ZombieSpawner>(true)).FirstOrDefault(s => s.name == "EliteZombieSpawner");
        if (elite != null)
        {
            Transform marker = elite.transform.Find("EliteSpawnPoint");
            if (marker != null) PlaceOnNavMesh(marker, FindEliteGround(filter, player.transform.position), 1, filter);
        }

        DrumCylinder[] drums = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<DrumCylinder>(true)).Where(d => !d.transform.IsChildOf(surface.transform)).ToArray();
        for (int i = 0; i < 3; i++)
        {
            DrumCylinder drum = i < drums.Length ? drums[i] : ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/DrumCylinder.prefab"), scene)).GetComponent<DrumCylinder>();
            if (i >= drums.Length) { drum.name = "DrumCylinder (" + i + ")"; Undo.RegisterCreatedObjectUndo(drum.gameObject, "Add Factory Drum"); }
            PlaceProp(drum.transform, new Vector3(40 + i, 1.9f, -27), filter);
        }
        Box box = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Box>(true)).FirstOrDefault(b => !b.transform.IsChildOf(surface.transform));
        if (box == null)
        {
            GameObject existingBox = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Box" || g.name == "Crate");
            if (existingBox != null) box = Undo.AddComponent<Box>(existingBox);
        }
        if (box != null)
        {
            box.gameObject.layer = LayerMask.NameToLayer("Drum");
            PlaceProp(box.transform, new Vector3(16, -.09f, -18), filter);
        }
        foreach (MinimapCamera minimap in Object.FindObjectsByType<MinimapCamera>(FindObjectsSortMode.None))
        {
            SetFloat(minimap, "_height", 30);
            minimap.transform.position = player.transform.position + Vector3.up * 30;
            minimap.transform.rotation = Quaternion.Euler(90, 0, 0);
        }
    }

    private static void PlaceOnNavMesh(Transform target, Vector3 feet, float offset, NavMeshQueryFilter filter)
    {
        if (!NavMesh.SamplePosition(feet, out NavMeshHit hit, 3f, filter)) throw new InvalidOperationException("NavMesh 위치를 찾지 못했습니다: " + target.name + " " + feet);
        Undo.RecordObject(target, "Place Factory Actor"); target.position = hit.position + Vector3.up * offset;
        PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }

    private static Vector3 FindEliteGround(NavMeshQueryFilter filter, Vector3 player)
    {
        var path = new NavMeshPath();
        if (!NavMesh.SamplePosition(player - Vector3.up, out NavMeshHit playerGround, 3f, filter)) throw new InvalidOperationException("플레이어 길의 NavMesh가 없습니다.");
        Vector3[] candidates = { new Vector3(47, 1.9f, -40), new Vector3(50, 1.9f, -35), new Vector3(43, 1.9f, -36), new Vector3(48, 1.9f, -45), new Vector3(42, 1.9f, -30) };
        foreach (Vector3 candidate in candidates)
        {
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, filter)) continue;
            if (Physics.CheckCapsule(hit.position + Vector3.up * .55f, hit.position + Vector3.up * 3.05f, .5f, 1, QueryTriggerInteraction.Ignore)) continue;
            if (NavMesh.CalculatePath(hit.position, playerGround.position, filter, path) && path.status == NavMeshPathStatus.PathComplete) return hit.position;
        }
        throw new InvalidOperationException("플레이어 길과 이어진 높은 천장의 엘리트 생성 위치를 찾지 못했습니다.");
    }

    private static void PlaceProp(Transform target, Vector3 feet, NavMeshQueryFilter filter)
    {
        Physics.SyncTransforms();
        Collider collider = target.GetComponent<Collider>();
        float bottom = collider != null ? target.position.y - collider.bounds.min.y : .5f;
        if (!NavMesh.SamplePosition(feet, out NavMeshHit hit, 3f, filter)) throw new InvalidOperationException("소품 근처에 NavMesh가 없습니다: " + target.name);
        // NavMesh의 복셀 높이 오차 대신 실제 바닥 콜라이더에 맞춰 공중에 뜨지 않게 한다.
        RaycastHit[] ground = Physics.RaycastAll(hit.position + Vector3.up * .5f, Vector3.down, 2f, 1, QueryTriggerInteraction.Ignore)
            .Where(h => h.normal.y > .5f && Mathf.Abs(h.point.y - hit.position.y) < .5f).OrderBy(h => Mathf.Abs(h.point.y - hit.position.y)).ToArray();
        Vector3 position = ground.Length > 0 ? ground[0].point : hit.position;
        Undo.RecordObject(target, "Place Factory Prop"); target.position = position + Vector3.up * (bottom + .02f);
        PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }

    private static void SetFloat(Component component, string name, float value)
    {
        var settings = new SerializedObject(component);
        SerializedProperty property = settings.FindProperty(name);
        if (property == null) throw new InvalidOperationException(component.GetType().Name + "에 직렬화된 필드가 없습니다: " + name);
        property.floatValue = value; settings.ApplyModifiedProperties();
    }
}
