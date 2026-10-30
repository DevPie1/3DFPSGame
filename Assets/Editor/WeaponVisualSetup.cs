using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class WeaponVisualSetup
{
    public const string FutureFolder = "Assets/05.Models/Meshy_AI_Futuristic_Battle_Rifle_20261030_texture_fbx";
    public const string FutureStem = "Meshy_AI_Futuristic_Battle_Rifle_20261030_texture";
    public const string VssPath = "Assets/Gece Studio/Rifle VSS - Free/Prefab/Rifle_VSS.prefab";
    public const string MaterialFolder = "Assets/09.materials/PlayerWeapons";

    [MenuItem("Tools/Player/Setup Rifle Models And Katana Trail")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play를 종료하세요.");
        PlayerMove player = Object.FindFirstObjectByType<PlayerMove>();
        if (player == null) throw new InvalidOperationException("Player가 없습니다.");
        Directory.CreateDirectory(".utmp");
        if (!File.Exists(".utmp/GameScene.before-rifle-models.unity"))
            EditorSceneManager.SaveScene(player.gameObject.scene, ".utmp/GameScene.before-rifle-models.unity", true);
        Material futureMaterial = PrepareMaterials();
        Material vssMaterial = PrepareVssMaterial();
        GameObject futurePrefab = MakeRifle("FutureRifle", FutureFolder + "/" + FutureStem + ".fbx",
            futureMaterial, new Vector3(-.62f, -.24f, 0), .06f, .95f);
        GameObject vssPrefab = MakeRifle("VSSRifle", VssPath, vssMaterial, new Vector3(-.19f, -.10f, 0), .012f, 1.08f);
        Transform model = player.transform.Find("Model");
        Transform hand = model.GetComponentsInChildren<Transform>(true).Single(t => t.name == "RightHand");
        Transform socket = hand.Find("RifleSocket");
        if (socket == null)
        {
            var go = new GameObject("RifleSocket");
            Undo.RegisterCreatedObjectUndo(go, "Create Rifle Socket");
            go.transform.SetParent(hand, false);
            socket = go.transform;
        }
        socket.localPosition = hand.Find("SwordSocket").localPosition;
        socket.localRotation = CalibrateIdleRotation(model.gameObject);
        GameObject future = Equip(futurePrefab, socket, "FutureRifle");
        GameObject vss = Equip(vssPrefab, socket, "VSSRifle");
        future.SetActive(true); vss.SetActive(false);
        PlayerWeaponModels models = Ensure<PlayerWeaponModels>(player.gameObject);
        Assign(models, "_weapons", player.GetComponent<PlayerWeapons>());
        Assign(models, "_gun", player.GetComponent<PlayerGunFire>());
        Assign(models, "_feedback", player.GetComponent<EasyFpsWeaponFeedback>());
        Assign(models, "_futureRifle", future);
        Assign(models, "_vssRifle", vss);
        Assign(models, "_futureMuzzle", future.transform.Find("Muzzle"));
        Assign(models, "_vssMuzzle", vss.transform.Find("Muzzle"));
        Assign(models, "_fallbackMuzzle", Camera.main.transform.Find("GunMuzzle"));
        player.GetComponent<PlayerGunFire>().SetMuzzle(future.transform.Find("Muzzle"));
        player.GetComponent<EasyFpsWeaponFeedback>().SetMuzzle(future.transform.Find("Muzzle"));
        EditorUtility.SetDirty(player.GetComponent<PlayerGunFire>());
        EditorUtility.SetDirty(player.GetComponent<EasyFpsWeaponFeedback>());

        Transform katana = model.GetComponentsInChildren<Transform>(true).Single(t => t.name == "SpartanKatana");
        Transform bladeBase = EnsureMarker(katana, "TrailBase", new Vector3(0, .018f, .03f));
        Transform bladeTip = EnsureMarker(katana, "TrailTip", new Vector3(0, .018f, .705f));
        string trailPath = "Assets/09.materials/SpartanSword/KatanaSlashTrail.mat";
        Material trailMaterial = AssetDatabase.LoadAssetAtPath<Material>(trailPath);
        Shader trailShader = Shader.Find("Game/KatanaSlashTrail");
        if (trailShader == null) throw new InvalidOperationException("KatanaSlashTrail 셰이더가 필요합니다.");
        if (trailMaterial == null) { trailMaterial = new Material(trailShader); AssetDatabase.CreateAsset(trailMaterial, trailPath); }
        trailMaterial.SetColor("_TrailColor", new Color(.18f, .75f, 1.1f, .65f));
        trailMaterial.SetColor("_EdgeColor", new Color(.85f, .98f, 1.2f, 1));
        EditorUtility.SetDirty(trailMaterial);
        KatanaSlashTrail trail = Ensure<KatanaSlashTrail>(player.gameObject);
        Assign(trail, "_bladeBase", bladeBase); Assign(trail, "_bladeTip", bladeTip); Assign(trail, "_material", trailMaterial);
        Assign(player.GetComponent<PlayerMeleeAttack>(), "_slashTrail", trail);
        Sprite futureIcon = MakeIcon(futurePrefab, "Weapon_FutureRifle");
        Sprite vssIcon = MakeIcon(vssPrefab, "Weapon_VSSRifle");
        foreach (AmmoHud hud in Object.FindObjectsByType<AmmoHud>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Assign(hud, "_rifleSprite", futureIcon); Assign(hud, "_sniperSprite", vssIcon);
        }
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(player.gameObject.scene);
    }

    private static Material PrepareVssMaterial()
    {
        const string textures = "Assets/Gece Studio/Rifle VSS - Free/Textures/";
        string path = MaterialFolder + "/VSSRifle_URP.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
        var normalImporter = (TextureImporter)AssetImporter.GetAtPath(textures + "T_Rifle_VSS_Normal.png");
        if (normalImporter.textureType != TextureImporterType.NormalMap) { normalImporter.textureType = TextureImporterType.NormalMap; normalImporter.SaveAndReimport(); }
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(textures + "T_Rifle_VSS_BaseColor.png"));
        material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(textures + "T_Rifle_VSS_Normal.png"));
        material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(textures + "T_Rifle_VSS_MetallicSmoothness02.png"));
        material.SetTexture("_OcclusionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(textures + "T_Rifle_VSS_AmbientOcclusion.png"));
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Metallic", 1); material.SetFloat("_Smoothness", 1);
        material.EnableKeyword("_NORMALMAP"); material.EnableKeyword("_METALLICSPECGLOSSMAP");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject MakeRifle(string name, string sourcePath, Material material, Vector3 grip, float muzzleHeight, float length)
    {
        var root = new GameObject(name);
        try
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            Renderer[] renderers = asset.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            float scale = length / bounds.size.x;
            Quaternion yaw = Quaternion.Euler(0, -90, 0);
            var source = (GameObject)PrefabUtility.InstantiatePrefab(asset, root.transform);
            source.name = "Model";
            source.transform.localRotation = yaw * asset.transform.localRotation;
            source.transform.localScale = asset.transform.localScale * scale;
            source.transform.localPosition = -(yaw * grip) * scale;
            foreach (Renderer renderer in source.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = material;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(source.transform);
            var muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(root.transform, false);
            muzzle.transform.localPosition = yaw * (new Vector3(bounds.max.x, muzzleHeight, 0) - grip) * scale;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 6;
            return PrefabUtility.SaveAsPrefabAsset(root, "Assets/03.Prefabs/Weapon/" + name + ".prefab");
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static Quaternion CalibrateIdleRotation(GameObject original)
    {
        Scene scene = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject copy = Object.Instantiate(original);
            SceneManager.MoveGameObjectToScene(copy, scene);
            copy.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Animator animator = copy.GetComponent<Animator>();
            animator.fireEvents = false; animator.Rebind(); animator.Update(0);
            Transform hand = copy.GetComponentsInChildren<Transform>(true).Single(t => t.name == "RightHand");
            return Quaternion.Inverse(hand.rotation);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    private static GameObject Equip(GameObject prefab, Transform socket, string name)
    {
        Transform found = socket.Find(name);
        if (found != null) return found.gameObject;
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, socket);
        Undo.RegisterCreatedObjectUndo(instance, "Equip Rifle Model");
        instance.name = name;
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
        return instance;
    }

    private static Transform EnsureMarker(Transform parent, string name, Vector3 position)
    {
        Transform marker = parent.Find(name);
        if (marker == null)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); marker = go.transform;
            Undo.RegisterCreatedObjectUndo(go, "Create Blade Trail Marker");
        }
        marker.localPosition = position;
        return marker;
    }

    private static Sprite MakeIcon(GameObject prefab, string name)
    {
        Scene scene = EditorSceneManager.NewPreviewScene();
        var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        var rt = new RenderTexture(256, 96, 24, RenderTextureFormat.ARGB32);
        var texture = new Texture2D(256, 96, TextureFormat.RGBA32, false);
        RenderTexture previous = RenderTexture.active;
        try
        {
            material.SetColor("_BaseColor", Color.white); material.SetFloat("_Cull", 0);
            GameObject model = Object.Instantiate(prefab);
            SceneManager.MoveGameObjectToScene(model, scene);
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) { bounds.Encapsulate(renderer.bounds); renderer.sharedMaterial = material; }
            var go = new GameObject("IconCamera"); SceneManager.MoveGameObjectToScene(go, scene);
            Camera camera = go.AddComponent<Camera>(); camera.scene = scene;
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(bounds.size.y * .6f, bounds.size.z * .5f / (256f / 96f) * 1.1f);
            camera.transform.position = bounds.center + Vector3.right * 2;
            camera.transform.LookAt(bounds.center, Vector3.up);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.clear;
            camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0, 0, 256, 96), 0, 0); texture.Apply();
            File.WriteAllBytes("Assets/04.Images/UI/" + name + ".png", texture.EncodeToPNG());
            camera.targetTexture = null;
        }
        finally
        {
            RenderTexture.active = previous;
            EditorSceneManager.ClosePreviewScene(scene);
            Object.DestroyImmediate(material); Object.DestroyImmediate(rt); Object.DestroyImmediate(texture);
        }
        string path = "Assets/04.Images/UI/" + name + ".png";
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static T Ensure<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(go);
    }

    private static void Assign(Object target, string property, Object reference)
    {
        var data = new SerializedObject(target);
        data.FindProperty(property).objectReferenceValue = reference;
        data.ApplyModifiedProperties();
    }

    public static Material PrepareMaterials()
    {
        SpartanCombatAnimations.EnsureFolder(MaterialFolder);
        string normalPath = FutureFolder + "/" + FutureStem + "_normal.png";
        var normalImporter = (TextureImporter)AssetImporter.GetAtPath(normalPath);
        if (normalImporter.textureType != TextureImporterType.NormalMap)
        {
            normalImporter.textureType = TextureImporterType.NormalMap;
            normalImporter.SaveAndReimport();
        }
        string packedPath = MaterialFolder + "/FutureRifle_MetallicSmoothness.png";
        var metallic = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
        var roughness = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
        Texture2D packed = null;
        try
        {
            ImageConversion.LoadImage(metallic, File.ReadAllBytes(FutureFolder + "/" + FutureStem + "_metallic.png"));
            ImageConversion.LoadImage(roughness, File.ReadAllBytes(FutureFolder + "/" + FutureStem + "_roughness.png"));
            Color32[] metalPixels = metallic.GetPixels32();
            Color32[] roughPixels = roughness.GetPixels32();
            if (metallic.width != roughness.width || metallic.height != roughness.height) throw new InvalidOperationException("PBR 텍스처 크기가 다릅니다.");
            for (int i = 0; i < metalPixels.Length; i++) metalPixels[i] = new Color32(metalPixels[i].r, 0, 0, (byte)(255 - roughPixels[i].r));
            packed = new Texture2D(metallic.width, metallic.height, TextureFormat.RGBA32, false, true);
            packed.SetPixels32(metalPixels); packed.Apply();
            File.WriteAllBytes(packedPath, packed.EncodeToPNG());
        }
        finally
        {
            Object.DestroyImmediate(metallic); Object.DestroyImmediate(roughness);
            if (packed != null) Object.DestroyImmediate(packed);
        }
        AssetDatabase.ImportAsset(packedPath);
        var packedImporter = (TextureImporter)AssetImporter.GetAtPath(packedPath);
        packedImporter.sRGBTexture = false;
        packedImporter.SaveAndReimport();
        string path = MaterialFolder + "/FutureRifle_URP.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(FutureFolder + "/" + FutureStem + ".png"));
        material.SetColor("_BaseColor", Color.white);
        material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
        material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(packedPath));
        material.SetFloat("_Smoothness", 1);
        material.SetFloat("_Metallic", 1);
        material.EnableKeyword("_NORMALMAP"); material.EnableKeyword("_METALLICSPECGLOSSMAP");
        EditorUtility.SetDirty(material);
        var importer = (ModelImporter)AssetImporter.GetAtPath(FutureFolder + "/" + FutureStem + ".fbx");
        importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Material.001"), material);
        importer.SaveAndReimport();
        AssetDatabase.SaveAssets();
        return material;
    }
}
