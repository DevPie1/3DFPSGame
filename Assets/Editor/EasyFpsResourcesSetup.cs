#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class EasyFpsResourcesSetup
{
    private const string EffectFolder = "Assets/03.Prefabs/Weapon/Effects/EasyFPS";
    private const string MaterialFolder = "Assets/09.materials/EasyFPS";
    private const string IconFolder = "Assets/04.Images/UI/EasyFPS";

    [MenuItem("Tools/Apply Easy FPS Feedback")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new System.InvalidOperationException("Play를 종료하고 적용하세요.");
        PlayerGunFire gun = Object.FindFirstObjectByType<PlayerGunFire>();
        if (gun == null) throw new System.InvalidOperationException("기존 플레이어를 찾지 못했습니다.");
        EnsureFolder(EffectFolder);
        EnsureFolder(MaterialFolder);
        EnsureFolder(IconFolder);
        GameObject[] effects = new GameObject[5];
        for (int i = 0; i < effects.Length; i++) effects[i] = ConfigureFlash(i + 1);
        MakeIcon("NewGun_auto_img", "Weapon_Auto");
        MakeIcon("NewGun_semi_img", "Weapon_Semi");

        AudioSource shot = EnsureAudio(gun.transform, "ShotAudio", .35f, false);
        AudioSource steps = EnsureAudio(gun.transform, "FootstepAudio", .35f, true);
        EasyFpsWeaponFeedback feedback = Ensure<EasyFpsWeaponFeedback>(gun.gameObject);
        Transform muzzle = gun.transform.Find("GunMuzzle");
        SerializedObject gunData = new SerializedObject(gun);
        Transform assignedMuzzle = gunData.FindProperty("_muzzle").objectReferenceValue as Transform;
        if (assignedMuzzle != null) muzzle = assignedMuzzle;
        if (muzzle == null)
        {
            GameObject point = new GameObject("GunMuzzle");
            Undo.RegisterCreatedObjectUndo(point, "Create muzzle point");
            Camera main = Camera.main;
            if (main == null) throw new System.InvalidOperationException("Main Camera가 없습니다.");
            point.transform.SetParent(main.transform, false);
            // 기존 레이저의 카메라 기준 시작점을 총구 효과에도 공유한다.
            point.transform.localPosition = new Vector3(.15f, -.12f, .5f);
            muzzle = point.transform;
        }
        Assign(gun, "_muzzle", muzzle);
        Assign(feedback, "_gun", gun);
        Assign(feedback, "_shotSource", shot);
        Assign(feedback, "_shotClip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Easy FPS/Sounds/shotSound.wav"));
        Assign(feedback, "_muzzle", muzzle);
        SerializedObject feedbackData = new SerializedObject(feedback);
        SerializedProperty prefabs = feedbackData.FindProperty("_muzzleFlashPrefabs");
        prefabs.arraySize = effects.Length;
        for (int i = 0; i < effects.Length; i++) prefabs.GetArrayElementAtIndex(i).objectReferenceValue = effects[i];
        feedbackData.ApplyModifiedProperties();

        PlayerFootstepAudio footsteps = Ensure<PlayerFootstepAudio>(gun.gameObject);
        Assign(footsteps, "_movement", gun.GetComponent<PlayerMove>());
        Assign(footsteps, "_source", steps);
        Assign(footsteps, "_walkClip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Easy FPS/Sounds/playerSound/walk_sound 1.wav"));
        Assign(footsteps, "_runClip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Easy FPS/Sounds/playerSound/run_sound 1.wav"));
        PlayerWeaponsSetup.Configure();
        EditorSceneManager.MarkSceneDirty(gun.gameObject.scene);
        Debug.Log("[EasyFPS] 발사음·걷기/달리기 발소리·URP 총구 효과 5종·HUD 아이콘 연결 완료");
    }

    private static GameObject ConfigureFlash(int index)
    {
        string suffix = index.ToString("00");
        string sourcePath = "Assets/Easy FPS/MuzzelFlash/MuzzlePrefabs/muzzelFlash " + suffix + ".prefab";
        string path = EffectFolder + "/MuzzleFlash_" + suffix + ".prefab";
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
        if (source == null) throw new System.InvalidOperationException("총구 효과 원본 없음: " + sourcePath);
        Material original = source.GetComponent<MeshRenderer>().sharedMaterial;
        string materialPath = MaterialFolder + "/MuzzleFlash_" + suffix + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) throw new System.InvalidOperationException("URP Unlit 셰이더가 없습니다.");
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
        }
        // 원본 효과는 MeshRenderer이므로 파티클 전용 정점 입력을 요구하지 않는다.
        material.shader = shader;
        material.shaderKeywords = new string[0];
        material.SetTexture("_BaseMap", original.mainTexture);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Surface", 1);
        material.SetFloat("_Blend", 2);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.One);
        material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        material.SetFloat("_DstBlendAlpha", (float)BlendMode.One);
        material.SetFloat("_ZWrite", 0);
        material.SetFloat("_Cull", (float)CullMode.Off);
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssetIfDirty(material);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null && !AssetDatabase.CopyAsset(sourcePath, path))
            throw new System.InvalidOperationException("총구 효과 복제 실패: " + path);
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = new Vector3(.16f, .16f, 0f);
            MeshRenderer renderer = root.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Light light = root.GetComponent<Light>();
            if (light != null) { light.intensity = 1.5f; light.range = 2; light.shadows = LightShadows.None; }
            DestroyAfterTimeParticle lifetime = root.GetComponent<DestroyAfterTimeParticle>();
            if (lifetime != null) lifetime.timeToDestroy = .05f;
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static AudioSource EnsureAudio(Transform parent, string name, float volume, bool loop)
    {
        Transform found = parent.Find(name);
        GameObject go;
        if (found != null) go = found.gameObject;
        else
        {
            go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create player feedback audio");
            go.transform.SetParent(parent, false);
        }
        AudioSource source = Ensure<AudioSource>(go);
        Undo.RecordObject(source, "Configure player audio");
        source.playOnAwake = false;
        source.spatialBlend = 0;
        source.dopplerLevel = 0;
        source.volume = volume;
        source.loop = loop;
        return source;
    }

    private static void MakeIcon(string sourceName, string targetName)
    {
        Texture2D original = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Easy FPS/Resources/Weap_Icons/" + sourceName + ".png");
        Texture2D readable = null, output = null;
        try
        {
            // 레거시 임포트 설정과 무관하게 원본 PNG의 알파를 보존한다.
            readable = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(readable, File.ReadAllBytes(AssetDatabase.GetAssetPath(original))))
                throw new System.InvalidOperationException("아이콘 PNG를 읽지 못했습니다.");
            float angle = -135 * Mathf.Deg2Rad, cosine = Mathf.Cos(angle), sine = Mathf.Sin(angle);
            float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
            Color32[] source = readable.GetPixels32();
            for (int y = 0; y < readable.height; y++)
                for (int x = 0; x < readable.width; x++)
                {
                    if (source[y * readable.width + x].a < 16) continue;
                    float px = x - readable.width * .5f, py = y - readable.height * .5f;
                    float rx = cosine * px - sine * py, ry = sine * px + cosine * py;
                    minX = Mathf.Min(minX, rx); maxX = Mathf.Max(maxX, rx);
                    minY = Mathf.Min(minY, ry); maxY = Mathf.Max(maxY, ry);
                }
            if (float.IsInfinity(minX)) throw new System.InvalidOperationException("무기 아이콘 알파가 비어 있습니다.");
            float scale = Mathf.Min(244f / Mathf.Max(1, maxX - minX), 84f / Mathf.Max(1, maxY - minY));
            output = new Texture2D(256, 96, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[256 * 96];
            for (int y = 0; y < 96; y++)
                for (int x = 0; x < 256; x++)
                {
                    float rx = (x + .5f - 128) / scale + (minX + maxX) * .5f;
                    float ry = (y + .5f - 48) / scale + (minY + maxY) * .5f;
                    float u = (cosine * rx + sine * ry + readable.width * .5f) / readable.width;
                    float v = (-sine * rx + cosine * ry + readable.height * .5f) / readable.height;
                    float alpha = u >= 0 && u <= 1 && v >= 0 && v <= 1 ? readable.GetPixelBilinear(u, v).a : 0;
                    // 원본의 매우 옅은 배경 무늬를 제거하고 손잡이가 아래로 오도록 정렬한다.
                    alpha = Mathf.Clamp01((alpha - .1f) / .9f);
                    pixels[(95 - y) * 256 + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255));
                }
            output.SetPixels32(pixels);
            output.Apply();
            string path = IconFolder + "/" + targetName + ".png";
            File.WriteAllBytes(path, output.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        finally
        {
            if (readable != null) Object.DestroyImmediate(readable);
            if (output != null) Object.DestroyImmediate(output);
        }
    }

    private static void Assign(Object target, string property, Object reference)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(property).objectReferenceValue = reference;
        serialized.ApplyModifiedProperties();
    }

    private static T Ensure<T>(GameObject go) where T : Component
    {
        T existing = go.GetComponent<T>();
        return existing != null ? existing : Undo.AddComponent<T>(go);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
#endif
