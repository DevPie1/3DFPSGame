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
        surface.minRegionArea = 0.5f; // 좁은 2층 발판도 NavMesh에서 제외되지 않게 한다.

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

    [MenuItem("Tools/Setup Zombie Jump")]
    public static void SetupZombieJump()
    {
        SetupZombiePrefab();
        AssetDatabase.SaveAssets();
        Debug.Log("[ZombieJump] 좀비 프리팹에 점프 컴포넌트를 추가했습니다.");
    }

    [MenuItem("Tools/Debug Zombie Jump")]
    public static void DebugZombieJump()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("[JumpDebug] Player 태그를 가진 오브젝트를 찾지 못했습니다.");
            return;
        }

        Vector3 playerPosition = player.transform.position;
        if (NavMesh.SamplePosition(playerPosition, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            Debug.Log(
                $"[JumpDebug] 플레이어 위치의 NavMesh: {hit.position} " +
                $"(거리 {Vector3.Distance(playerPosition, hit.position):F2})");
        }
        else
        {
            Debug.LogWarning(
                "[JumpDebug] 플레이어 근처에 NavMesh가 없습니다. " +
                "2층 발판이 NavMesh에 구워지지 않았을 가능성이 큽니다.");
        }

        Zombie[] zombies = Object.FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        foreach (Zombie zombie in zombies)
        {
            NavMeshAgent agent = zombie.GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                continue;
            }

            float height = playerPosition.y - zombie.transform.position.y;
            Debug.Log(
                $"[JumpDebug] {zombie.name}: state={zombie.State}, height={height:F2}, " +
                $"path={agent.pathStatus}, onLink={agent.isOnOffMeshLink}, " +
                $"position={zombie.transform.position}");
        }
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
            agent.autoTraverseOffMeshLink = false;

            if (root.GetComponent<ZombieJump>() == null)
            {
                root.AddComponent<ZombieJump>();
            }

            PrefabUtility.SaveAsPrefabAsset(root, ZombiePrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
#endif
