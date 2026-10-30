using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class EliteGoldSetup
{
    private const string ZombiePath = "Assets/03.Prefabs/Monster/Zombie.prefab";
    private const string ElitePath = "Assets/03.Prefabs/Monster/EliteZombie.prefab";
    private const string ItemFolder = "Assets/03.Prefabs/Item";
    private const string EffectFolder = "Assets/03.Prefabs/Weapon/Effects/EliteGold";
    private const string MaterialFolder = "Assets/09.materials/EliteGold";

    [MenuItem("Tools/Setup Elite Zombie And Gold")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play를 종료하세요.");
        PlayerMove player = Object.FindFirstObjectByType<PlayerMove>();
        if (player == null) throw new InvalidOperationException("GameScene의 Player가 필요합니다.");
        Scene scene = player.gameObject.scene;
        Directory.CreateDirectory(".utmp");
        if (!File.Exists(".utmp/GameScene.before-elite-gold.unity"))
            EditorSceneManager.SaveScene(scene, ".utmp/GameScene.before-elite-gold.unity", true);
        foreach (string folder in new[] { ItemFolder, EffectFolder, MaterialFolder }) SpartanCombatAnimations.EnsureFolder(folder);
        Gold gold = MakeGold();
        Gold bigGold = MakeBigGold(gold);
        ParticleSystem dust = MakeSmashEffect();
        GameObject warning = MakeWarning();
        GoldPickupText popup = MakePickupText();
        ConfigureBaseZombie(gold);
        GameObject elite = MakeElite(bigGold, dust, warning, player.gameObject.layer);
        ZombieSpawner spawner = ConfigureSpawner(elite.GetComponent<Zombie>(), player);
        PlayerGold wallet = Ensure<PlayerGold>(player.gameObject);
        ConfigureHud(wallet, popup, spawner);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
    }

    private static Gold MakeGold()
    {
        Material material = GetMaterial(MaterialFolder + "/GoldMaterial.mat", Shader.Find("Universal Render Pipeline/Lit"));
        material.SetColor("_BaseColor", new Color32(255, 200, 40, 255));
        material.SetFloat("_Metallic", .75f); material.SetFloat("_Smoothness", .65f);
        EditorUtility.SetDirty(material);
        GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        try
        {
            coin.name = "Gold"; coin.layer = 2;
            coin.transform.position = Vector3.zero; coin.transform.localScale = new Vector3(.3f, .02f, .3f);
            Object.DestroyImmediate(coin.GetComponent<Collider>());
            coin.AddComponent<BoxCollider>().size = new Vector3(1, 2, 1);
            Rigidbody body = coin.AddComponent<Rigidbody>();
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            coin.GetComponent<MeshRenderer>().sharedMaterial = material;
            coin.AddComponent<Gold>();
            return PrefabUtility.SaveAsPrefabAsset(coin, ItemFolder + "/Gold.prefab").GetComponent<Gold>();
        }
        finally { Object.DestroyImmediate(coin); }
    }

    private static Gold MakeBigGold(Gold gold)
    {
        var coin = (GameObject)PrefabUtility.InstantiatePrefab(gold.gameObject);
        try
        {
            coin.name = "BigGold"; coin.transform.localScale = gold.transform.localScale * 2f;
            var settings = new SerializedObject(coin.GetComponent<Gold>());
            settings.FindProperty("_amount").intValue = 50; settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(coin.transform);
            return PrefabUtility.SaveAsPrefabAsset(coin, ItemFolder + "/BigGold.prefab").GetComponent<Gold>();
        }
        finally { Object.DestroyImmediate(coin); }
    }

    private static ParticleSystem MakeSmashEffect()
    {
        string texturePath = MaterialFolder + "/DustSoft.png";
        if (!File.Exists(texturePath))
        {
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float radius = new Vector2((x + .5f) / 32 - 1, (y + .5f) / 32 - 1).magnitude;
                    texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1 - radius), 1.5f)));
                }
            texture.Apply(); File.WriteAllBytes(texturePath, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(texturePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.SaveAndReimport();
        }
        Material material = GetMaterial(MaterialFolder + "/SmashDust.mat", Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 0);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
        material.SetOverrideTag("RenderType", "Transparent"); EditorUtility.SetDirty(material);
        var go = new GameObject("SmashEffect");
        try
        {
            go.layer = 2; go.transform.rotation = Quaternion.Euler(-90, 0, 0);
            ParticleSystem particles = go.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.duration = .5f; main.loop = false; main.startLifetime = .6f; main.startSpeed = 5f;
            main.startSize = 1f; main.startColor = (Color)new Color32(150, 130, 110, 210);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = particles.emission; emission.rateOverTime = 0; emission.SetBursts(new[] { new ParticleSystem.Burst(0, 30) });
            var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = .5f; shape.sphericalDirectionAmount = 1;
            var colors = particles.colorOverLifetime; colors.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(.9f, 0), new GradientAlphaKey(0, 1) });
            colors.color = gradient;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            return PrefabUtility.SaveAsPrefabAsset(go, EffectFolder + "/SmashEffect.prefab").GetComponent<ParticleSystem>();
        }
        finally { Object.DestroyImmediate(go); }
    }

    private static GameObject MakeWarning()
    {
        string path = MaterialFolder + "/SmashWarningMesh.asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            mesh = new Mesh { name = "SmashWarningQuad" };
            mesh.vertices = new[] { new Vector3(-.5f, 0, -.5f), new Vector3(-.5f, 0, .5f), new Vector3(.5f, 0, .5f), new Vector3(.5f, 0, -.5f) };
            mesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 }; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
        }
        Material material = GetMaterial(MaterialFolder + "/SmashWarning.mat", Shader.Find("Game/SmashWarning"));
        material.SetColor("_Color", new Color(1, .08f, .03f, .55f)); EditorUtility.SetDirty(material);
        var go = new GameObject("SmashWarning");
        try
        {
            go.layer = 2; go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            return PrefabUtility.SaveAsPrefabAsset(go, EffectFolder + "/SmashWarning.prefab");
        }
        finally { Object.DestroyImmediate(go); }
    }

    private static GoldPickupText MakePickupText()
    {
        var go = new GameObject("GoldPickupText", typeof(RectTransform), typeof(Canvas));
        try
        {
            go.layer = 2;
            var rect = (RectTransform)go.transform; rect.sizeDelta = new Vector2(200, 60); rect.localScale = Vector3.one * .005f;
            go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var child = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            child.transform.SetParent(go.transform, false); child.layer = 2;
            TextMeshProUGUI text = child.GetComponent<TextMeshProUGUI>(); Style(text, 38);
            text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.sizeDelta = Vector2.zero; text.rectTransform.anchoredPosition = Vector2.zero;
            text.alignment = TextAlignmentOptions.Center; text.text = "+10";
            GoldPickupText popup = go.AddComponent<GoldPickupText>(); Assign(popup, "_text", text);
            return PrefabUtility.SaveAsPrefabAsset(go, EffectFolder + "/GoldPickupText.prefab").GetComponent<GoldPickupText>();
        }
        finally { Object.DestroyImmediate(go); }
    }

    private static void ConfigureBaseZombie(Gold gold)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(ZombiePath);
        try
        {
            var settings = new SerializedObject(root.GetComponent<Zombie>());
            settings.FindProperty("_goldPrefab").objectReferenceValue = gold;
            settings.FindProperty("_goldCount").intValue = 3;
            settings.FindProperty("_randomizeGoldCount").boolValue = true;
            settings.ApplyModifiedPropertiesWithoutUndo();
            Ensure<ZombieAnimationEvents>(root.GetComponentInChildren<Animator>().gameObject);
            PrefabUtility.SaveAsPrefabAsset(root, ZombiePath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AnimationClip attack = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Zombie/Animations/Z_Attack.anim");
        AnimationEvent[] current = AnimationUtility.GetAnimationEvents(attack);
        if (current.Count(e => e.functionName == "OnAttackHit") != 1
            || !current.Any(e => e.functionName == "OnAttackHit" && Mathf.Abs(e.time - .45f) < .001f))
        {
            var events = current.Where(e => e.functionName != "OnAttackHit").ToList();
            events.Add(new AnimationEvent { time = .45f, functionName = "OnAttackHit" });
            AnimationUtility.SetAnimationEvents(attack, events.ToArray()); EditorUtility.SetDirty(attack);
        }
    }

    private static GameObject MakeElite(Gold bigGold, ParticleSystem dust, GameObject warning, int playerLayer)
    {
        Material original = AssetDatabase.LoadAssetAtPath<Material>("Assets/Zombie/Materials/Zombie.mat");
        string materialPath = MaterialFolder + "/EliteZombie.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null) { material = new Material(original); AssetDatabase.CreateAsset(material, materialPath); }
        material.SetColor("_BaseColor", new Color32(255, 110, 110, 255)); EditorUtility.SetDirty(material);
        var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ZombiePath));
        try
        {
            root.name = "EliteZombie";
            Transform model = root.transform.Find("ZombieVisual"); model.localScale = Vector3.one * 2f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(model);
            foreach (SkinnedMeshRenderer renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(m => material).ToArray();
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            CapsuleCollider capsule = root.GetComponent<CapsuleCollider>(); capsule.center = new Vector3(0, .8f, 0); capsule.radius = .5f; capsule.height = 3.6f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(capsule);
            NavMeshAgent agent = root.GetComponent<NavMeshAgent>(); agent.height = 3.6f; agent.radius = .5f; agent.speed = 2.5f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(agent);
            Zombie zombie = root.GetComponent<Zombie>();
            var settings = new SerializedObject(zombie);
            settings.FindProperty("_maxHealth").floatValue = 500;
            settings.FindProperty("_health").floatValue = 500;
            settings.FindProperty("_attackDamage").floatValue = 30;
            settings.FindProperty("_moveSpeed").floatValue = 2.5f;
            settings.FindProperty("_detectDistance").floatValue = 12;
            settings.FindProperty("_attackRange").floatValue = 3;
            settings.FindProperty("_combackDistance").floatValue = 30;
            settings.FindProperty("_keepChasingPlayer").boolValue = false;
            settings.FindProperty("_attackInterval").floatValue = 3;
            settings.FindProperty("_deathDelay").floatValue = 2;
            settings.FindProperty("_superArmor").boolValue = true;
            settings.FindProperty("_smashRadius").floatValue = 3.5f;
            settings.FindProperty("_smashTargetMask").intValue = (1 << playerLayer) | (1 << LayerMask.NameToLayer("Drum"));
            settings.FindProperty("_smashEffectPrefab").objectReferenceValue = dust;
            settings.FindProperty("_smashWarningPrefab").objectReferenceValue = warning;
            settings.FindProperty("_enrageAtHalfHealth").boolValue = true;
            // Gold Prefab은 바탕 프리팹에서 상속하고 개수와 추가 보상만 덮어쓴다.
            settings.FindProperty("_goldCount").intValue = 10;
            settings.FindProperty("_bonusGoldPrefab").objectReferenceValue = bigGold;
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(zombie);
            var bar = new SerializedObject(root.GetComponent<ZombieHealthBar>());
            bar.FindProperty("_heightOffset").floatValue = 3.2f; bar.ApplyModifiedPropertiesWithoutUndo();
            RectTransform canvas = (RectTransform)root.GetComponentInChildren<Canvas>().transform;
            canvas.localPosition = new Vector3(0, 3.2f, 0); canvas.sizeDelta = new Vector2(200, 24);
            PrefabUtility.RecordPrefabInstancePropertyModifications(canvas);
            Slider slider = canvas.GetComponentInChildren<Slider>();
            RectTransform sliderRect = (RectTransform)slider.transform; sliderRect.sizeDelta = new Vector2(200, 24);
            PrefabUtility.RecordPrefabInstancePropertyModifications(sliderRect);
            return PrefabUtility.SaveAsPrefabAsset(root, ElitePath);
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static ZombieSpawner ConfigureSpawner(Zombie elite, PlayerMove player)
    {
        Scene scene = player.gameObject.scene;
        GameObject go = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "EliteZombieSpawner");
        if (go == null) { go = new GameObject("EliteZombieSpawner"); Undo.RegisterCreatedObjectUndo(go, "Create Elite Spawner"); }
        Transform point = go.transform.Find("EliteSpawnPoint");
        if (point == null)
        {
            var marker = new GameObject("EliteSpawnPoint"); marker.transform.SetParent(go.transform, false); point = marker.transform;
            NavMeshAgent agent = elite.GetComponent<NavMeshAgent>();
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            if (!NavMesh.SamplePosition(new Vector3(0, 0, 20), out NavMeshHit hit, 8f, filter))
                throw new InvalidOperationException("엘리트 스폰 위치 근처에 NavMesh가 없습니다.");
            point.position = hit.position + Vector3.up * agent.baseOffset;
            Vector3 facing = player.transform.position - point.position; facing.y = 0; point.rotation = Quaternion.LookRotation(facing);
        }
        ZombieSpawner spawner = Ensure<ZombieSpawner>(go);
        var settings = new SerializedObject(spawner);
        settings.FindProperty("_zombiePrefab").objectReferenceValue = elite;
        settings.FindProperty("_maxZombieCount").intValue = 1; settings.FindProperty("_maxAliveZombies").intValue = 1;
        settings.FindProperty("_spawnInterval").floatValue = 1;
        SerializedProperty points = settings.FindProperty("_spawnPoints"); points.arraySize = 1;
        SerializedProperty entry = points.GetArrayElementAtIndex(0);
        entry.FindPropertyRelative("Position").objectReferenceValue = point;
        entry.FindPropertyRelative("IdleMode").enumValueIndex = (int)ZombieIdleMode.Idle;
        entry.FindPropertyRelative("PatrolPoints").arraySize = 0;
        settings.ApplyModifiedProperties();
        return spawner;
    }

    private static void ConfigureHud(PlayerGold wallet, GoldPickupText popup, ZombieSpawner eliteSpawner)
    {
        Canvas canvas = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).First(c => c.isRootCanvas && c.renderMode != RenderMode.WorldSpace);
        TextMeshProUGUI gold = EnsureText(canvas.transform, "GoldText");
        gold.rectTransform.anchorMin = gold.rectTransform.anchorMax = new Vector2(0, 1);
        gold.rectTransform.pivot = new Vector2(0, 1); gold.rectTransform.anchoredPosition = new Vector2(40, -40);
        gold.rectTransform.sizeDelta = new Vector2(300, 50); gold.text = "골드 0"; Style(gold, 32);
        TextMeshProUGUI feedback = EnsureText(gold.transform, "PickupFeedback"); Style(feedback, 26);
        feedback.rectTransform.anchorMin = feedback.rectTransform.anchorMax = new Vector2(0, 1);
        feedback.rectTransform.pivot = new Vector2(0, 1); feedback.rectTransform.anchoredPosition = new Vector2(0, -46);
        feedback.rectTransform.sizeDelta = new Vector2(200, 40); feedback.text = ""; feedback.alpha = 0;
        UI_Gold hud = Ensure<UI_Gold>(gold.gameObject); Assign(hud, "_player", wallet); Assign(hud, "_pickupTextPrefab", popup); Assign(hud, "_pickupFeedback", feedback);
        foreach (UI_ZombieCount count in Object.FindObjectsByType<UI_ZombieCount>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var settings = new SerializedObject(count);
            SerializedProperty sources = settings.FindProperty("_additionalSpawners");
            bool exists = Enumerable.Range(0, sources.arraySize).Any(i => sources.GetArrayElementAtIndex(i).objectReferenceValue == eliteSpawner);
            if (!exists) { sources.arraySize++; sources.GetArrayElementAtIndex(sources.arraySize - 1).objectReferenceValue = eliteSpawner; }
            settings.ApplyModifiedProperties();
            count.GetComponent<TextMeshProUGUI>().text = "남은 좀비: " + Object.FindObjectsByType<ZombieSpawner>(FindObjectsSortMode.None).Sum(s => s.MaxCount)
                + " / " + Object.FindObjectsByType<ZombieSpawner>(FindObjectsSortMode.None).Sum(s => s.MaxCount);
        }
    }

    private static void Style(TextMeshProUGUI text, float size)
    {
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/08.Fonts/강한육군 Medium SDF5.asset");
        text.fontSize = size; text.color = new Color32(255, 210, 60, 255); text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.Left; text.fontStyle = FontStyles.Bold;
    }

    private static TextMeshProUGUI EnsureText(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child == null) { var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); go.layer = 5; child = go.transform; }
        return Ensure<TextMeshProUGUI>(child.gameObject);
    }

    private static T Ensure<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(go);
    }

    private static void Assign(Object owner, string name, Object value)
    {
        var settings = new SerializedObject(owner); settings.FindProperty(name).objectReferenceValue = value; settings.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(owner);
    }

    private static Material GetMaterial(string path, Shader shader)
    {
        if (shader == null) throw new InvalidOperationException("셰이더가 없습니다: " + path);
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
        return material;
    }
}
