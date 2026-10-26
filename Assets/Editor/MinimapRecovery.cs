#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

public static class MinimapRecovery
{
    [MenuItem("Tools/Restore Minimap UI")]
    public static void Restore()
    {
        // 복구 도중 생성한 미저장 오브젝트 대신 저장된 씬에서 다시 구성한다.
        var scene = EditorSceneManager.OpenScene("Assets/01.Scenes/GameScene.unity");
        MinimapSetup.CreateMinimap();
        EditorSceneManager.SaveScene(scene);
    }
}
#endif
