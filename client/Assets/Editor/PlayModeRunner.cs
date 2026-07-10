#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EquipmentIdle.Net;
using EquipmentIdle.State;
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
    private static bool _visualCapturePopulated;

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
        StartMainVisualCapture(false);
    }

    public static void RunMainPopulatedVisualCapture()
    {
        StartMainVisualCapture(true);
    }

    private static void StartMainVisualCapture(bool populated)
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
        File.WriteAllText("verify_result.txt", "VISUAL_CAPTURE_PENDING");
        _mainSmokeStart = EditorApplication.timeSinceStartup;
        _visualCaptureIndex = 0;
        _visualCapturePhase = 0;
        _visualCaptureNextAt = 0;
        _visualCapturePopulated = populated;
        string repositoryRoot = Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
        _visualCaptureDirectory = Path.Combine(repositoryRoot, "artifacts", "ui-qa");
        if (populated) _visualCaptureDirectory = Path.Combine(_visualCaptureDirectory, "populated");
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
                if (_visualCapturePopulated) SeedPopulatedState();
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

    private static void SeedPopulatedState()
    {
        var state = UnityEngine.Object.FindObjectOfType<GameState>();
        if (state == null) throw new Exception("GameState missing");

        var wsField = typeof(GameState).GetField("_ws", BindingFlags.Instance | BindingFlags.NonPublic);
        var ws = wsField != null ? wsField.GetValue(state) as WSClient : null;
        var openField = typeof(WSClient).GetField("_isOpen", BindingFlags.Instance | BindingFlags.NonPublic);
        if (ws != null && openField != null) openField.SetValue(ws, true);

        var equipped = new[]
        {
            Equipment("equipped-weapon", "精铸烈焰长剑", 0, 2, 7, false, Affix("strength", 3, 84), Affix("crit_rate", 2, 0.075f)),
            Equipment("equipped-helm", "守夜人的战盔", 1, 2, 5, false, Affix("armor", 3, 96), Affix("max_hp", 2, 140)),
            Equipment("equipped-armor", "熔岩锻造胸甲", 2, 3, 6, false, Affix("vitality", 4, 110), Affix("reflect", 3, 24)),
            Equipment("equipped-gloves", "猎魔者铁手", 3, 1, 4, false, Affix("attack_speed", 2, 0.12f)),
            Equipment("equipped-boots", "灰烬疾行战靴", 4, 2, 5, false, Affix("agility", 3, 72), Affix("move_speed", 2, 18)),
            Equipment("equipped-ring-1", "赤焰指环", 5, 3, 8, true, Affix("fire_dmg", 4, 132), Affix("crit_damage", 3, 48)),
            Equipment("equipped-ring-2", "寒霜誓约", 6, 2, 5, false, Affix("cold_dmg", 3, 96), Affix("shield", 2, 120)),
            Equipment("equipped-neck", "远古守护者护符", 7, 4, 9, true, Affix("resource_gain", 5, 38), Affix("cooldown_red", 4, 22)),
        };
        var bag = new[]
        {
            Equipment("bag-legendary-weapon", "焚城者的远古双手巨剑", 0, 4, 2, false, Affix("strength", 5, 260), Affix("crit_rate", 4, 0.145f), Affix("fire_dmg", 5, 310), Affix("attack_speed", 3, 0.18f)),
            Equipment("bag-rare-helm", "永夜守望者的不朽冠冕", 1, 3, 3, false, Affix("armor", 4, 180), Affix("max_hp", 4, 280), Affix("lifesteal", 3, 16)),
            Equipment("bag-artifact-ring", "星陨回响之戒", 5, 4, 10, true, Affix("lightning_dmg", 5, 340), Affix("crit_damage", 5, 82), Affix("drop_rate", 4, 26)),
            Equipment("bag-magic-armor", "符文刻印重甲", 2, 1, 1, false, Affix("armor", 2, 64), Affix("vitality", 2, 44)),
            Equipment("bag-rare-gloves", "风暴追猎者手甲", 3, 2, 4, false, Affix("agility", 3, 78), Affix("attack_speed", 3, 0.13f)),
            Equipment("bag-common-boots", "磨损的远征靴", 4, 0, 0, false),
            Equipment("bag-legendary-neck", "深渊君王的猩红吊坠", 7, 3, 6, false, Affix("kill_heal", 4, 96), Affix("drop_rate", 3, 18), Affix("resource_gain", 3, 24)),
            Equipment("bag-magic-ring", "旅法师的青蓝指环", 6, 1, 2, false, Affix("intellect", 2, 58), Affix("cold_dmg", 2, 66)),
        };

        PushStateMessage(state, Message.TypeSync, new SyncData
        {
            account = "visual_qa_hero",
            floor = 125,
            souls = 24,
            inventory = Array.Empty<string>(),
        });
        PushStateMessage(state, Message.TypeBag, new BagData { items = bag, equipped = equipped });
        PushStateMessage(state, Message.TypePower, new PowerData { power = 52384.6f });
        PushStateMessage(state, Message.TypeMaterials, new MaterialsData
        {
            materials = new[]
            {
                new MaterialEntry { k = "base_mat", v = 1280 },
                new MaterialEntry { k = "affix_mat_1", v = 497 },
                new MaterialEntry { k = "affix_mat_2", v = 186 },
                new MaterialEntry { k = "affix_mat_3", v = 64 },
                new MaterialEntry { k = "affix_mat_4", v = 21 },
                new MaterialEntry { k = "affix_mat_5", v = 7 },
            },
        });
        PushStateMessage(state, Message.TypeTalents, new TalentsData
        {
            souls = 24,
            max_floor = 165,
            can_reincarn = true,
            talents = new[]
            {
                new TalentEntry { name = "damage", level = 10 },
                new TalentEntry { name = "quality", level = 3 },
                new TalentEntry { name = "drop", level = 7 },
                new TalentEntry { name = "offline_gain", level = 5 },
            },
        });
        PushStateMessage(state, Message.TypeLoot, new LootData
        {
            uid = bag[0].uid,
            base_id = bag[0].base_id,
            name = bag[0].name,
            slot = bag[0].slot,
            rarity = bag[0].rarity,
            upgrade = bag[0].upgrade,
            affixes = bag[0].affixes,
        });
    }

    private static void PushStateMessage(GameState state, string type, object data)
    {
        var handleMessage = typeof(GameState).GetMethod("HandleMessage", BindingFlags.Instance | BindingFlags.NonPublic);
        if (handleMessage == null) throw new Exception("GameState.HandleMessage missing");
        handleMessage.Invoke(state, new[]
        {
            new ParsedMessage { t = type, dataJson = JsonUtility.ToJson(data) },
        });
    }

    private static EquipmentDTO Equipment(string uid, string name, int slot, int rarity, int upgrade, bool locked, params AffixData[] affixes)
    {
        return new EquipmentDTO
        {
            uid = uid,
            base_id = "visual_qa_" + slot,
            name = name,
            slot = slot,
            rarity = rarity,
            upgrade = upgrade,
            locked = locked,
            affixes = affixes ?? Array.Empty<AffixData>(),
        };
    }

    private static AffixData Affix(string type, int tier, float value)
    {
        return new AffixData { type = type, tier = tier, value = value };
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
