using System;
using System.IO;
using Bootstrap;
using GameScreen.Presentation;
using SplashScreen.Presentation;
using TapRunner.Application;
using TapRunner.Presentation;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EditorTools
{
    /// <summary>One-shot Tap Runner Addressables + gameplay prefabs (Chapter companion project).</summary>
    public static class TapRunnerProjectTools
    {
        const string AddressablesTapDir = "Assets/_Project/Addressables/TapRunner";
        const string TuningPath = AddressablesTapDir + "/TapRunnerTuning.asset";
        const string PrefabDir = "Assets/_Project/Prefabs/UI";
        const string GameplayPrefabDir = "Assets/_Project/Prefabs/Gameplay";
        const string PlayerPrefabPath = GameplayPrefabDir + "/TapRunner_Player.prefab";
        const string ObstaclePrefabPath = GameplayPrefabDir + "/TapRunner_Obstacle.prefab";
        const string GamePrefabPath = PrefabDir + "/Screen_Game.prefab";
        const string SplashPrefabPath = PrefabDir + "/Screen_Splash.prefab";
        const string BootstrapScenePath = "Assets/_Project/Scenes/BootstrapScene.unity";

        [MenuItem("TapRunner/Project/Bootstrap Content (Scene + Prefabs + Addressables)")]
        public static void MenuBootstrap() => BootstrapAll(true);

        public static void BatchBootstrap()
        {
            try
            {
                BootstrapAll(true);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                EditorApplication.Exit(1);
                return;
            }

            EditorApplication.Exit(0);
        }

        static void BootstrapAll(bool buildAddressablesPlayerContent)
        {
            EnsureFolder("Assets/_Project/Addressables");
            EnsureFolder(AddressablesTapDir);
            EnsureFolder(PrefabDir);
            EnsureFolder(GameplayPrefabDir);
            CreateOrUpdateTuningAsset();
            CreatePlayerPrefabIfMissing();
            CreateObstaclePrefabIfMissing();
            CreateScreenPrefabIfMissing(GamePrefabPath, "Screen_Game", typeof(GameScreenController));
            CreateScreenPrefabIfMissing(SplashPrefabPath, "Screen_Splash", typeof(SplashScreenController));
            EnsureAppEntryPointInBootstrapScene();
            RegisterAddressablesEntries();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (buildAddressablesPlayerContent)
                AddressableAssetSettings.BuildPlayerContent();

            Debug.Log("[TapRunner] Project bootstrap finished.");
        }

        static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
                return;
            var parent = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
            var name = Path.GetFileName(assetPath);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
                return;
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        static void CreateOrUpdateTuningAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<TapRunnerTuningConfig>(TuningPath) != null)
                return;

            var tuning = ScriptableObject.CreateInstance<TapRunnerTuningConfig>();
            AssetDatabase.CreateAsset(tuning, TuningPath);
            EditorUtility.SetDirty(tuning);
        }

        static void CreatePlayerPrefabIfMissing()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) != null)
                return;

            var root = new GameObject("TapRunner_Player");
            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var cap = root.AddComponent<CapsuleCollider>();
            cap.isTrigger = true;
            cap.radius = 0.35f;
            cap.height = 1.1f;
            cap.center = new Vector3(0f, 0.55f, 0f);

            var vis = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            vis.name = "Visual";
            vis.transform.SetParent(root.transform, false);
            vis.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            UnityEngine.Object.DestroyImmediate(vis.GetComponent<Collider>());

            root.AddComponent<TapRunnerPlayerHitProxy>();

            Directory.CreateDirectory(Path.GetDirectoryName(PlayerPrefabPath) ?? GameplayPrefabDir);
            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        static void CreateObstaclePrefabIfMissing()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ObstaclePrefabPath) != null)
                return;

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "TapRunner_Obstacle";
            cube.transform.localScale = new Vector3(0.9f, 1.1f, 0.9f);

            var box = cube.GetComponent<BoxCollider>();
            if (box != null)
                box.isTrigger = true;

            var rb = cube.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            cube.AddComponent<TapRunnerObstacleView>();

            PrefabUtility.SaveAsPrefabAsset(cube, ObstaclePrefabPath);
            UnityEngine.Object.DestroyImmediate(cube);
        }

        static void CreateScreenPrefabIfMissing(string prefabPath, string rootName, Type controllerType)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                return;

            var go = new GameObject(rootName, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            go.AddComponent(controllerType);

            Directory.CreateDirectory(Path.GetDirectoryName(prefabPath) ?? PrefabDir);
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            UnityEngine.Object.DestroyImmediate(go);
        }

        static void EnsureAppEntryPointInBootstrapScene()
        {
            var scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
            GameObject entry = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponent<AppEntryPoint>() != null)
                    entry = root;
            }

            // Strip any extra roots: BootstrapScene must contain only AppEntryPoint (camera/light/UI come from code).
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root != entry)
                    UnityEngine.Object.DestroyImmediate(root);
            }

            // Recreate if missing or if someone saved AppEntryPoint with RectTransform (UI root) by mistake.
            if (entry == null || entry.transform is RectTransform)
            {
                if (entry != null)
                    UnityEngine.Object.DestroyImmediate(entry);

                entry = new GameObject("AppEntryPoint");
                entry.AddComponent<AppEntryPoint>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static void RegisterAddressablesEntries()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                Debug.LogError("[TapRunner] AddressableAssetSettings could not be created.");
                return;
            }

            var localGroup = settings.DefaultGroup;
            if (localGroup == null)
            {
                Debug.LogError("[TapRunner] Addressables DefaultGroup is null.");
                return;
            }

            var uiGroup = settings.FindGroup("UI_Screens");
            if (uiGroup == null)
                uiGroup = settings.CreateGroup("UI_Screens", false, false, true, null, typeof(BundledAssetGroupSchema));

            void Ensure(string path, string address, AddressableAssetGroup group)
            {
                var guid = AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrEmpty(guid))
                {
                    Debug.LogError($"[TapRunner] Missing asset for Addressables: {path}");
                    return;
                }

                var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                entry.SetAddress(address, false);
            }

            Ensure(TuningPath, TapRunnerAddressKeys.Config, localGroup);
            Ensure(PlayerPrefabPath, TapRunnerAddressKeys.PlayerPrefab, localGroup);
            Ensure(ObstaclePrefabPath, TapRunnerAddressKeys.ObstaclePrefab, localGroup);
            Ensure(GamePrefabPath, "Screen_Game", uiGroup);
            Ensure(SplashPrefabPath, "Screen_Splash", uiGroup);

            EditorUtility.SetDirty(settings);
        }
    }
}
