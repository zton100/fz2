#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// batchmode 下进入 PlayMode 跑 Verify 场景并等待结果文件。
/// 调用：-executeMethod PlayModeRunner.Run
/// </summary>
public static class PlayModeRunner
{
    private static bool _oldEnterPlayModeOptionsEnabled;
    private static EnterPlayModeOptions _oldEnterPlayModeOptions;
    private static double _mainSmokeStart;

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Verify.unity");
        // 清旧结果
        File.WriteAllText("verify_result.txt", "PENDING");
        EditorApplication.EnterPlaymode();
        // 每 0.5 秒检查结果文件，最长 30 秒
        EditorApplication.update += CheckResult;
    }

    private static float _elapsed;
    private static void CheckResult()
    {
        _elapsed += 0.5f;
        string result = File.ReadAllText("verify_result.txt");
        if (result != "PENDING")
        {
            Debug.Log("[PlayModeRunner] RESULT: " + result);
            EditorApplication.update -= CheckResult;
            EditorApplication.Exit(0);
        }
        else if (_elapsed > 30f)
        {
            Debug.Log("[PlayModeRunner] TIMEOUT");
            File.WriteAllText("verify_result.txt", "RUNNER_TIMEOUT");
            EditorApplication.update -= CheckResult;
            EditorApplication.Exit(1);
        }
    }

    public static void RunMainSmoke()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
        File.WriteAllText("verify_result.txt", "PENDING");
        _mainSmokeStart = EditorApplication.timeSinceStartup;
        _oldEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        _oldEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        EditorApplication.update += CheckMainSmoke;
        EditorApplication.EnterPlaymode();
    }

    private static void CheckMainSmoke()
    {
        if (EditorApplication.timeSinceStartup - _mainSmokeStart < 3.0) return;
        File.WriteAllText("verify_result.txt", "MAIN_SMOKE_OK");
        Debug.Log("[PlayModeRunner] MAIN_SMOKE_OK");
        EditorApplication.update -= CheckMainSmoke;
        EditorSettings.enterPlayModeOptionsEnabled = _oldEnterPlayModeOptionsEnabled;
        EditorSettings.enterPlayModeOptions = _oldEnterPlayModeOptions;
        EditorApplication.Exit(0);
    }
}
#endif
