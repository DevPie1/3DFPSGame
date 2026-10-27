#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Pink(분홍) 좀비 머티리얼을 URP Lit으로 변환하고,
// 모델이 바닥에서 뜨는 높이를 보정한다.
public static class ZombieVisualFix
{
    private const string ZombiePrefabPath = "Assets/03.Prefabs/Monster/Zombie.prefab";
    private const string MaterialFolder = "Assets/Zombie/Materials";
    private const string TextureFolder = "Assets/Zombie/Textures";

    [MenuItem("Tools/Fix Zombie Visual")]
    public static void FixZombieVisual()
    {
        ConvertMaterials();

        GameObject root = PrefabUtility.LoadPrefabContents(ZombiePrefabPath);

        try
        {
            Transform visual = root.transform.Find("ZombieVisual");
            if (visual != null)
            {
                Vector3 position = visual.localPosition;
                position.y = -1f;
                visual.localPosition = position;
            }

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
        Debug.Log("[ZombieVisualFix] URP 머티리얼 변환과 좀비 높이 보정을 완료했습니다.");
    }

    public static void ConvertMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("[ZombieVisualFix] URP Lit 셰이더를 찾지 못했습니다.");
            return;
        }

        SetTextureLinear(TextureFolder + "/Zombie_metallic.tga");
        SetTextureLinear(TextureFolder + "/Zombie_ao.png");

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { MaterialFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                continue;
            }

            material.shader = urpLit;

            if (material.name == "Zombie")
            {
                material.SetTexture("_BaseMap", LoadTexture("Zombie.tga"));
                material.SetColor("_BaseColor", Color.white);
                material.SetTexture("_BumpMap", LoadTexture("Zombie_nm.png"));
                material.SetFloat("_BumpScale", 1f);
                material.SetTexture("_MetallicGlossMap", LoadTexture("Zombie_metallic.tga"));
                material.SetTexture("_OcclusionMap", LoadTexture("Zombie_ao.png"));
                material.SetTexture("_EmissionMap", LoadTexture("zombie_emission.tga"));
                material.SetColor("_EmissionColor", Color.white);
                material.EnableKeyword("_NORMALMAP");
                material.EnableKeyword("_METALLICGLOSSMAP");
                material.EnableKeyword("_EMISSION");
                material.SetFloat("_Metallic", 1f);
                material.SetFloat("_Smoothness", 0.5f);
                material.SetFloat("_OcclusionStrength", 1f);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                material.SetColor("_BaseColor", new Color(0.72f, 0.72f, 0.72f, 1f));
            }

            material.SetFloat("_Surface", 0f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_AlphaClip", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
            material.SetFloat("_ZWrite", 1f);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;

            EditorUtility.SetDirty(material);
        }
    }

    private static Texture2D LoadTexture(string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder + "/" + fileName);
    }

    private static void SetTextureLinear(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null || importer.sRGBTexture == false)
        {
            return;
        }

        importer.sRGBTexture = false;
        importer.SaveAndReimport();
    }
}
#endif
