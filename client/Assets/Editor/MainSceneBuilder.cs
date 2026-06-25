#if UNITY_EDITOR
using EquipmentIdle.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class MainSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Main.unity";

    [MenuItem("EquipmentIdle/Build Main Scene")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 主相机
        var camObj = new GameObject("Main Camera");
        var cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5;
        camObj.transform.position = new Vector3(0, 0, -10);

        // MainController（IMGUI 界面，无需 Canvas）
        var ctrlObj = new GameObject("MainController");
        ctrlObj.AddComponent<MainController>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("[MainSceneBuilder] Main scene built at " + ScenePath);

        // 加入 BuildSettings 为场景 0
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    [MenuItem("EquipmentIdle/Build Verify Scene")]
    public static void BuildVerify()
    {
        const string path = "Assets/Scenes/Verify.unity";
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camObj = new GameObject("Main Camera");
        var cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5;
        camObj.transform.position = new Vector3(0, 0, -10);

        var verifyObj = new GameObject("AutoVerifier");
        verifyObj.AddComponent<AutoVerifier>();

        EditorSceneManager.SaveScene(scene, path);
        Debug.Log("[MainSceneBuilder] Verify scene built at " + path);
    }
}
#endif
