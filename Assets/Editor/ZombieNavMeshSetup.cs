#if UNITY_EDITOR
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// Tools 메뉴에서 좀비 프리팹을 NavMeshAgent + CapsuleCollider로 정리하고,
// 씬에 NavMeshSurface를 추가해 NavMesh를 굽는다.
public static class ZombieNavMeshSetup
{
    private const string ZombiePrefabPath = "Assets/03.Prefabs/Monster/Zombie.prefab";
    private const string NavMeshAssetPath = "Assets/01.Scenes/GameScene/NavMesh-Map.asset";

    [MenuItem("Tools/Setup Zombie NavMesh")]
    public static void SetupZombieNavMesh()
    {
        SetupZombiePrefab();

        NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>();
        if (surface == null)
        {
            GameObject surfaceObject = new GameObject("NavMeshSurface");
            surface = surfaceObject.AddComponent<NavMeshSurface>();
            Undo.RegisterCreatedObjectUndo(surfaceObject, "Create NavMesh Surface");
        }

        surface.collectObjects = CollectObjects.All;
        surface.layerMask = 1 << 0; // Default 레이어의 바닥/벽/경사로만 굽는다.
        surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
        surface.agentTypeID = 0;
        surface.center = Vector3.zero;
        surface.size = new Vector3(200f, 50f, 200f);

        surface.BuildNavMesh();

        NavMeshData data = surface.navMeshData;
        if (data != null)
        {
            string existingPath = AssetDatabase.GetAssetPath(data);
            if (string.IsNullOrEmpty(existingPath))
            {
                if (AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshAssetPath) != null)
                {
                    AssetDatabase.DeleteAsset(NavMeshAssetPath);
                }

                AssetDatabase.CreateAsset(data, NavMeshAssetPath);
            }
            else
            {
                EditorUtility.SetDirty(data);
            }
        }

        EditorSceneManager.MarkSceneDirty(surface.gameObject.scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[ZombieNavMesh] NavMesh 생성 완료. 씬을 저장한 뒤 Play로 확인하세요.");
        Selection.activeGameObject = surface.gameObject;
    }

    private static void SetupZombiePrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(ZombiePrefabPath);

        try
        {
            CharacterController controller = root.GetComponent<CharacterController>();
            if (controller != null)
            {
                Object.DestroyImmediate(controller, true);
            }

            CapsuleCollider capsule = root.GetComponent<CapsuleCollider>();
            if (capsule == null)
            {
                capsule = root.AddComponent<CapsuleCollider>();
            }

            capsule.center = Vector3.zero;
            capsule.radius = 0.5f;
            capsule.height = 2f;
            capsule.direction = 1;

            NavMeshAgent agent = root.GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                agent = root.AddComponent<NavMeshAgent>();
            }

            agent.radius = 0.5f;
            agent.height = 2f;
            agent.baseOffset = 1f;
            agent.speed = 5f;
            agent.acceleration = 8f;
            agent.angularSpeed = 360f;
            agent.stoppingDistance = 0.7f;
            agent.autoBraking = true;
            agent.updateRotation = true;

            PrefabUtility.SaveAsPrefabAsset(root, ZombiePrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
#endif
