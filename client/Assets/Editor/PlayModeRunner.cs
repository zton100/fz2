#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// batchmode 下进入 PlayMode 跑 Verify 场景并等待结果文件。
/// 调用：-executeMethod PlayModeRunner.Run
/// </summary>
public static class PlayModeRunner
{
    private static bool _oldEnterPlayModeOptionsEnabled;
    private static EnterPlayModeOptions _oldEnterPlayModeOptions;
    private static double _mainSmokeStart;
    private static bool _mainSmokeChecked;
    private static readonly string[] VisualCaptureTabs = { "Battle", "Bag", "Craft", "Talent" };
    private static readonly string[] VisualCaptureNodes = { "dungeon", "mobile-bag", "mobile-craft", "talent-panel" };
    private static int _visualCaptureIndex;
    private static int _visualCapturePhase;
    private static double _visualCaptureNextAt;
    private static RenderTexture _visualCaptureTexture;
    private static PanelSettings _visualCapturePanelSettings;
    private static string _visualCaptureDirectory;

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
        _mainSmokeChecked = false;
        _oldEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        _oldEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        EditorApplication.update += CheckMainSmoke;
        EditorApplication.EnterPlaymode();
    }

    public static void RunMainVisualCapture()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
        File.WriteAllText("verify_result.txt", "VISUAL_CAPTURE_PENDING");
        _mainSmokeStart = EditorApplication.timeSinceStartup;
        _visualCaptureIndex = 0;
        _visualCapturePhase = 0;
        _visualCaptureNextAt = 0;
        string repositoryRoot = Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
        _visualCaptureDirectory = Path.Combine(repositoryRoot, "artifacts", "ui-qa");
        Directory.CreateDirectory(_visualCaptureDirectory);
        foreach (string tab in VisualCaptureTabs)
        {
            string path = Path.Combine(_visualCaptureDirectory, tab.ToLowerInvariant() + ".png");
            if (File.Exists(path)) File.Delete(path);
        }

        _oldEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        _oldEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        EditorApplication.update += CheckMainVisualCapture;
        EditorApplication.EnterPlaymode();
    }

    private static void CheckMainVisualCapture()
    {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup - _mainSmokeStart < 3.0) return;
        try
        {
            var doc = UnityEngine.Object.FindObjectOfType<UIDocument>();
            if (doc == null || doc.rootVisualElement == null) throw new Exception("UIDocument missing");

            if (_visualCapturePhase == 0)
            {
                _visualCaptureTexture = new RenderTexture(945, 1672, 24, RenderTextureFormat.ARGB32);
                _visualCaptureTexture.Create();
                _visualCapturePanelSettings = doc.panelSettings;
                _visualCapturePanelSettings.targetTexture = _visualCaptureTexture;
                ActivateTabAndAssertVisible(doc.rootVisualElement, VisualCaptureTabs[0], VisualCaptureNodes[0]);
                ScrollToTop(doc.rootVisualElement);
                MarkAllDirty(doc.rootVisualElement);
                _visualCapturePhase = 1;
                _visualCaptureNextAt = EditorApplication.timeSinceStartup + 1.0;
                return;
            }

            if (EditorApplication.timeSinceStartup < _visualCaptureNextAt) return;
            CaptureVisualTab(VisualCaptureTabs[_visualCaptureIndex]);
            _visualCaptureIndex++;
            if (_visualCaptureIndex >= VisualCaptureTabs.Length)
            {
                File.WriteAllText("verify_result.txt", "VISUAL_CAPTURE_OK " + _visualCaptureDirectory);
                Debug.Log("[PlayModeRunner] VISUAL_CAPTURE_OK " + _visualCaptureDirectory);
                FinishMainVisualCapture(0);
                return;
            }

            ActivateTabAndAssertVisible(
                doc.rootVisualElement,
                VisualCaptureTabs[_visualCaptureIndex],
                VisualCaptureNodes[_visualCaptureIndex]);
            ScrollToTop(doc.rootVisualElement);
            MarkAllDirty(doc.rootVisualElement);
            _visualCaptureNextAt = EditorApplication.timeSinceStartup + 0.8;
        }
        catch (Exception e)
        {
            File.WriteAllText("verify_result.txt", "VISUAL_CAPTURE_FAIL " + e.Message);
            Debug.LogError("[PlayModeRunner] VISUAL_CAPTURE_FAIL: " + e);
            FinishMainVisualCapture(1);
        }
    }

    private static void CaptureVisualTab(string tabName)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = _visualCaptureTexture;
        var texture = new Texture2D(_visualCaptureTexture.width, _visualCaptureTexture.height, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, _visualCaptureTexture.width, _visualCaptureTexture.height), 0, 0);
        texture.Apply();
        RenderTexture.active = previous;

        string path = Path.Combine(_visualCaptureDirectory, tabName.ToLowerInvariant() + ".png");
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        if (!File.Exists(path) || new FileInfo(path).Length < 1024)
            throw new Exception("capture missing or empty: " + tabName);
        Debug.Log("[PlayModeRunner] captured " + path);
    }

    private static void ScrollToTop(VisualElement root)
    {
        var scroll = root.Q<ScrollView>();
        if (scroll != null) scroll.scrollOffset = Vector2.zero;
    }

    private static void MarkAllDirty(VisualElement root)
    {
        root.MarkDirtyRepaint();
        var elements = new List<VisualElement>();
        root.Query<VisualElement>().ToList(elements);
        foreach (var element in elements) element.MarkDirtyRepaint();
    }

    private static void FinishMainVisualCapture(int exitCode)
    {
        EditorApplication.update -= CheckMainVisualCapture;
        if (_visualCapturePanelSettings != null) _visualCapturePanelSettings.targetTexture = null;
        if (_visualCaptureTexture != null)
        {
            _visualCaptureTexture.Release();
            UnityEngine.Object.DestroyImmediate(_visualCaptureTexture);
        }
        _visualCapturePanelSettings = null;
        _visualCaptureTexture = null;
        EditorSettings.enterPlayModeOptionsEnabled = _oldEnterPlayModeOptionsEnabled;
        EditorSettings.enterPlayModeOptions = _oldEnterPlayModeOptions;
        EditorApplication.Exit(exitCode);
    }

    private static void CheckMainSmoke()
    {
        if (EditorApplication.timeSinceStartup - _mainSmokeStart < 3.0) return;
        if (_mainSmokeChecked) return;
        _mainSmokeChecked = true;
        try
        {
            AssertMainUi();
            File.WriteAllText("verify_result.txt", "MAIN_SMOKE_OK");
            Debug.Log("[PlayModeRunner] MAIN_SMOKE_OK");
            FinishMainSmoke(0);
        }
        catch (Exception e)
        {
            File.WriteAllText("verify_result.txt", "MAIN_SMOKE_FAIL " + e.Message);
            Debug.LogError("[PlayModeRunner] MAIN_SMOKE_FAIL: " + e);
            FinishMainSmoke(1);
        }
    }

    private static void FinishMainSmoke(int exitCode)
    {
        EditorApplication.update -= CheckMainSmoke;
        EditorSettings.enterPlayModeOptionsEnabled = _oldEnterPlayModeOptionsEnabled;
        EditorSettings.enterPlayModeOptions = _oldEnterPlayModeOptions;
        EditorApplication.Exit(exitCode);
    }

    private static void AssertMainUi()
    {
        var doc = UnityEngine.Object.FindObjectOfType<UIDocument>();
        if (doc == null) throw new Exception("UIDocument missing");
        var root = doc.rootVisualElement;
        if (root == null) throw new Exception("rootVisualElement missing");

        AssertNode(root, "mobile-frame");
        AssertNode(root, "top-hud");
        AssertNode(root, "dungeon");
        AssertNode(root, "boss-progress-card");
        AssertNode(root, "recent-loot-card");
        AssertNode(root, "mobile-bag");
        AssertNode(root, "bag-equipped");
        AssertNode(root, "mobile-detail");
        AssertNode(root, "mobile-craft");
        AssertNode(root, "mobile-materials");
        AssertNode(root, "compose-panel");
        AssertNode(root, "talent-panel");
        AssertNode(root, "bottom-bar");
        AssertNode(root, "offline");

        AssertButton(root, "战斗");
        AssertButton(root, "背包");
        AssertButton(root, "锻造");
        AssertButton(root, "天赋");
        ActivateTabAndAssertVisible(root, "Battle", "dungeon");
        ActivateTabAndAssertVisible(root, "Bag", "mobile-bag");
        ActivateTabAndAssertVisible(root, "Craft", "mobile-craft");
        ActivateTabAndAssertVisible(root, "Talent", "talent-panel");
    }

    private static void AssertNode(VisualElement root, string name)
    {
        var node = root.Q<VisualElement>(name);
        if (node == null) throw new Exception("missing node " + name);
    }

    private static void AssertButton(VisualElement root, string text)
    {
        if (FindButton(root, text) == null) throw new Exception("missing button " + text);
    }

    private static void ActivateTabAndAssertVisible(VisualElement root, string tabName, string visibleNodeName)
    {
        var controller = FindMainController();
        var controllerType = controller.GetType();
        var tabType = controllerType.GetNestedType("MainTab", BindingFlags.NonPublic);
        var tab = Enum.Parse(tabType, tabName);
        var setActiveTab = controllerType.GetMethod("SetActiveTab", BindingFlags.Instance | BindingFlags.NonPublic);
        if (setActiveTab == null) throw new Exception("SetActiveTab missing");
        setActiveTab.Invoke(controller, new[] { tab });

        var node = root.Q<VisualElement>(visibleNodeName);
        if (node == null) throw new Exception("missing node after tab switch " + visibleNodeName);
        if (node.resolvedStyle.display == DisplayStyle.None)
            throw new Exception("node hidden after tab switch " + visibleNodeName);
    }

    private static MonoBehaviour FindMainController()
    {
        var behaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
        foreach (var behaviour in behaviours)
        {
            if (behaviour != null && behaviour.GetType().FullName == "EquipmentIdle.UI.MainController")
            {
                return behaviour;
            }
        }
        throw new Exception("MainController missing");
    }

    private static Button FindButton(VisualElement root, string text)
    {
        var buttons = new List<Button>();
        root.Query<Button>().ToList(buttons);
        foreach (var button in buttons)
        {
            if (button.text == text) return button;
        }
        return null;
    }
}
#endif
