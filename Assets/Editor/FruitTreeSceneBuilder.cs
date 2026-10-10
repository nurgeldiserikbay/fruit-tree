#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace FruitTree.Editor
{
    public static class FruitTreeSceneBuilder
    {
        [MenuItem("Fruit Tree/Generate Playable Scene")]
        public static void Generate()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Apple Tree");
            var tree = root.AddComponent<TreeGenerator>();
            tree.Generate();

            var grass = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            grass.name = "Grass Island";
            grass.transform.position = new Vector3(0, -0.18f, 0);
            grass.transform.localScale = new Vector3(3.8f, 0.17f, 3.8f);
            var green = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            green.color = new Color(0.34f, 0.52f, 0.21f);
            grass.GetComponent<Renderer>().sharedMaterial = green;

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.69f, 0.83f, 0.9f);
            camera.fieldOfView = 48f;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<OrbitPickingCamera>().target = root.transform;
            var lightObject = new GameObject("Sun");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            lightObject.transform.rotation = Quaternion.Euler(48, -28, 0);
            RenderSettings.ambientLight = new Color(0.68f, 0.72f, 0.68f);

            var canvasObj = new GameObject("HUD");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            var textObj = new GameObject("Fruit Counter");
            textObj.transform.SetParent(canvasObj.transform, false);
            var text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 35;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.2f, 0.25f, 0.14f);
            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.15f, 0.87f);
            rect.anchorMax = new Vector2(0.85f, 0.98f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var hud = canvasObj.AddComponent<HarvestHUD>();
            hud.tree = tree;
            hud.countText = text;

            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true) };
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            AssetDatabase.SaveAssets();
            Debug.Log("Fruit Tree scene created. Press Play. Drag to rotate, tap apples, pinch to zoom.");
        }
    }
}
