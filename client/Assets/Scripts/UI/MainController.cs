using EquipmentIdle.Data;
using EquipmentIdle.Net;
using EquipmentIdle.State;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace EquipmentIdle.UI
{
    public class MainController : MonoBehaviour
    {
        private enum BagFilter
        {
            All,
            Upgrades,
            Rare,
            Decompose,
        }

        private enum MainTab
        {
            Battle,
            Bag,
            Craft,
            Talent,
        }

        private GameState _gameState;
        private TextField _accountInput;
        private Label _statusText;
        private Label _syncText;
        private Label _powerText;
        private Label _stuckText;
        private Label _floorText;
        private Label _soulsText;
        private Label _materialsText;
        private Label _reincarnText;
        private Label _talentsText;
        private VisualElement _selectedDetailContent;
        private VisualElement _equippedDetailContent;
        private VisualElement _compareDetailContent;
        private Label _dungeonTitleText;
        private Label _monsterText;
        private Label _battleText;
        private Label _goalText;
        private Label _bossHintText;
        private Label _stageZoneText;
        private Label _stageHeroNameText;
        private Label _stageHeroPowerText;
        private Label _stageMonsterNameText;
        private Label _stageMonsterPowerText;
        private Label _stageStatusText;
        private Label _objectiveCardText;
        private Label _bossProgressCardText;
        private Label _lootFeedText;
        private VisualElement _lootFeedContent;
        private VisualElement _progressNodesContent;
        private Label _toastText;
        private Label _offlineText;
        private VisualElement _offlinePanel;
        private VisualElement _equippedContent;
        private VisualElement _equippedMirrorContent;
        private VisualElement _bagContent;
        private VisualElement _bagFilterActions;
        private VisualElement _battleTabContent;
        private VisualElement _bagTabContent;
        private VisualElement _craftTabContent;
        private VisualElement _talentTabContent;
        private VisualElement _bottomNavContent;
        private VisualElement _detailActions;
        private VisualElement _talentActions;
        private VisualElement _craftPlanContent;
        private VisualElement _reincarnPlanContent;
        private VisualElement _dungeonPanel;
        private VisualElement _lootPanel;
        private VisualElement _bossProgressFill;
        private VisualElement _stageHeroHealthFill;
        private VisualElement _stageMonsterHealthFill;
        private Label _stageSlash;
        private Label _stageImpactText;
        private Label _stageBannerText;
        private Image _stageHeroSpriteImage;
        private Image _stageBossSpriteImage;
        private Texture2D _battleBackground;
        private Texture2D _heroSprite;
        private Texture2D _bossSprite;
        private Texture2D _craftIcon;
        private Texture2D[] _slotIcons;
        private EquipmentDTO _selected;
        private MainTab _activeTab = MainTab.Battle;
        private BagFilter _bagFilter = BagFilter.All;
        private float _prevPower;
        private bool _prevCanReincarn;
        private float _battlePulseUntil;
        private float _combatBeatStartedAt = -10f;
        private float _nextCombatBeatAt;
        private float _lootPulseUntil;
        private float _stageBannerUntil;
        private float _bossProgressTarget;
        private float _bossProgressCurrent;
        private string _pendingCraftUid;
        private string _pendingCraftAction;
        private float _pendingCraftScore;

        private struct Toast
        {
            public string Text;
            public float ExpireAt;
        }

        private readonly List<Toast> _toasts = new List<Toast>();
        private readonly List<EquipmentDTO> _lootFeed = new List<EquipmentDTO>();
        private readonly HashSet<string> _lockedEquipment = new HashSet<string>();
        private const float ToastDuration = 3f;
        private const float CombatBeatDuration = 0.55f;
        private const float CombatBeatInterval = 2f;
        private static readonly string[] TalentKeys = { "damage", "quality", "drop", "offline_gain" };
        private static readonly string[] TalentNames = { "伤害", "品质", "掉落", "离线" };
        private static readonly int[] TalentMax = { 10, 3, 10, 5 };
        private static readonly Color32 RootBg = new Color32(8, 10, 10, 255);
        private static readonly Color32 PanelBg = new Color32(20, 21, 19, 255);
        private static readonly Color32 PanelBgWarm = new Color32(30, 25, 20, 255);
        private static readonly Color32 PanelBorder = new Color32(83, 70, 49, 255);
        private static readonly Color32 PanelBorderHot = new Color32(178, 104, 42, 255);
        private static readonly Color32 GoldText = new Color32(244, 202, 121, 255);
        private static readonly Color32 EmberText = new Color32(255, 122, 45, 255);
        private static readonly Color32 GoodText = new Color32(113, 204, 86, 255);
        private static readonly Color32 TextMain = new Color32(238, 229, 207, 255);
        private static readonly Color32 TextMuted = new Color32(178, 165, 139, 255);
        private static readonly Color32 ButtonDefault = new Color32(67, 49, 31, 255);
        private static readonly Color32 ButtonEquip = new Color32(48, 97, 38, 255);
        private static readonly Color32 ButtonCraft = new Color32(130, 84, 22, 255);
        private static readonly Color32 ButtonReforge = new Color32(28, 76, 120, 255);
        private static readonly Color32 ButtonDanger = new Color32(110, 36, 30, 255);
        private static readonly Color32 ButtonAscend = new Color32(116, 55, 22, 255);

        private void Start()
        {
            if (GameState.Instance == null)
            {
                var go = new GameObject("GameState");
                go.AddComponent<GameState>();
            }

            _gameState = GameState.Instance;
            _gameState.OnSyncReceived += OnSync;
            _gameState.OnBagReceived += OnBag;
            _gameState.OnPowerReceived += OnPower;
            _gameState.OnLootReceived += OnLoot;
            _gameState.OnFloorReceived += OnFloor;
            _gameState.OnMaterialsReceived += OnMaterials;
            _gameState.OnOfflineResultReceived += OnOfflineResult;
            _gameState.OnCraftResult += OnCraftResult;
            _gameState.OnTalentsReceived += OnTalents;

            BuildUI();
            RefreshAll();
        }

        private void OnDestroy()
        {
            if (_gameState == null) return;
            _gameState.OnSyncReceived -= OnSync;
            _gameState.OnBagReceived -= OnBag;
            _gameState.OnPowerReceived -= OnPower;
            _gameState.OnLootReceived -= OnLoot;
            _gameState.OnFloorReceived -= OnFloor;
            _gameState.OnMaterialsReceived -= OnMaterials;
            _gameState.OnOfflineResultReceived -= OnOfflineResult;
            _gameState.OnCraftResult -= OnCraftResult;
            _gameState.OnTalentsReceived -= OnTalents;
        }

        private void Update()
        {
            PruneToasts();
            UpdateCombatFeedback();
        }

        private void OnSync(SyncData data)
        {
            RefreshAll();
        }

        private void OnBag(List<EquipmentDTO> bag, List<EquipmentDTO> equipped)
        {
            TryReportPendingCraftScoreChange();
            KeepSelectedIfPresent();
            RefreshEquipmentLists();
            RefreshDetail();
            RefreshCraftPlan();
            RefreshDungeon();
        }

        private void OnPower(float power)
        {
            float delta = power - _prevPower;
            _prevPower = power;
            if (_nextCombatBeatAt <= 0f) _nextCombatBeatAt = Time.realtimeSinceStartup + 0.35f;
            if (delta > 0.5f)
                _powerText.text = $"战力：{power:F1} +{delta:F1}";
            else if (delta < -0.5f)
                _powerText.text = $"战力：{power:F1} {delta:F1}";
            else
                _powerText.text = string.Format(L10n.UIPowerLabel, power);
            RefreshStuck();
        }

        private void OnLoot(EquipmentDTO eq)
        {
            _lootFeed.Insert(0, eq);
            if (_lootFeed.Count > 6) _lootFeed.RemoveAt(_lootFeed.Count - 1);
            RefreshLootFeed();
            _lootPulseUntil = Time.realtimeSinceStartup + 0.8f;

            string lootToast = EquipmentPresenter.BuildLootToast(eq);
            bool upgrade = IsLootUpgrade(eq);
            string ceremony = EquipmentPresenter.BuildLootCeremonyText(eq, upgrade);
            if (!string.IsNullOrEmpty(ceremony))
            {
                ShowStageBanner(ceremony, RarityUIColor(eq.rarity));
            }
            if (upgrade)
            {
                Select(eq);
                AddToast("发现提升装备：" + lootToast, ToastDuration + 1f);
                return;
            }

            AddToast(lootToast, ToastDuration);
        }

        private void OnFloor(int newFloor)
        {
            StartCombatBeat();
            if (newFloor > 1 && (newFloor - 1) % 5 == 0)
            {
                string banner = EquipmentPresenter.BuildBossClearBanner(newFloor - 1);
                if (!string.IsNullOrEmpty(banner)) ShowStageBanner(banner, new Color32(255, 92, 64, 255));
                AddToast($"击败 Boss：第 {newFloor - 1} 层", ToastDuration);
            }
            RefreshHeader();
            RefreshStuck();
        }

        private void OnOfflineResult(OfflineResultData ord)
        {
            int h = ord.duration_seconds / 3600;
            int m = (ord.duration_seconds % 3600) / 60;
            string dur = h > 0 ? $"{h}小时{m}分" : $"{m}分";
            _offlineText.text = $"{L10n.UIOfflineTitle}\n"
                + $"{string.Format(L10n.UIOfflineDuration, dur)}\n"
                + $"{string.Format(L10n.UIOfflineLoot, ord.loot_count)}\n"
                + $"{string.Format(L10n.UIOfflineFloors, ord.floors_advanced)}\n"
                + $"{string.Format(L10n.UIOfflineTicks, ord.ticks_simulated)}";
            _offlinePanel.style.display = DisplayStyle.Flex;
        }

        private void OnMaterials(Dictionary<string, int> materials)
        {
            RefreshMaterials();
            RefreshCraftPlan();
            RefreshDungeon();
        }

        private void OnCraftResult(CraftResultData cr)
        {
            AddToast(cr.msg, ToastDuration);
            if (!cr.ok)
            {
                ClearPendingCraft();
            }
            else if (!string.IsNullOrEmpty(_pendingCraftUid) && cr.uid == _pendingCraftUid)
            {
                AddToast($"{_pendingCraftAction}完成：评分未变化", ToastDuration);
                ClearPendingCraft();
            }
            RefreshMaterials();
            RefreshDetail();
            RefreshCraftPlan();
        }

        private void OnTalents(int souls, int maxFloor, bool canReincarn, Dictionary<string, int> talents)
        {
            if (canReincarn && !_prevCanReincarn)
            {
                AddToast("可以转生了", 5f);
            }
            _prevCanReincarn = canReincarn;
            RefreshProgression();
            RefreshDungeon();
        }

        private void BuildUI()
        {
            _battleBackground = Resources.Load<Texture2D>("UI/dark-dungeon-battle-bg");
            _heroSprite = Resources.Load<Texture2D>("UI/hero-combat-sprite");
            _bossSprite = Resources.Load<Texture2D>("UI/boss-combat-sprite");
            _craftIcon = Resources.Load<Texture2D>("UI/icon-craft");
            _slotIcons = new[]
            {
                Resources.Load<Texture2D>("UI/icon-weapon"),
                Resources.Load<Texture2D>("UI/icon-helm"),
                Resources.Load<Texture2D>("UI/icon-armor"),
                Resources.Load<Texture2D>("UI/icon-gloves"),
                Resources.Load<Texture2D>("UI/icon-boots"),
                Resources.Load<Texture2D>("UI/icon-ring"),
                Resources.Load<Texture2D>("UI/icon-ring"),
                Resources.Load<Texture2D>("UI/icon-amulet"),
            };

            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;

            var doc = gameObject.AddComponent<UIDocument>();
            doc.panelSettings = panelSettings;

            var root = doc.rootVisualElement;
            root.style.flexGrow = 1;
            root.style.backgroundColor = new StyleColor(RootBg);
            root.style.alignItems = Align.Center;

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            scroll.style.width = Length.Percent(100);
            scroll.contentContainer.style.alignItems = Align.Center;
            root.Add(scroll);

            var mobile = new VisualElement { name = "mobile-frame" };
            mobile.style.width = Length.Percent(100);
            mobile.style.maxWidth = 945;
            mobile.style.flexDirection = FlexDirection.Column;
            mobile.style.paddingLeft = 12;
            mobile.style.paddingRight = 12;
            mobile.style.paddingTop = 8;
            mobile.style.paddingBottom = 8;
            scroll.Add(mobile);

            BuildTopHud(mobile);

            BuildTabbedContent(mobile);

            BuildBottomBar(mobile);

            BuildOfflinePanel(root);
        }

        private void BuildTabbedContent(VisualElement root)
        {
            _battleTabContent = new VisualElement();
            _battleTabContent.style.flexDirection = FlexDirection.Column;
            root.Add(_battleTabContent);
            BuildDungeonPanel(_battleTabContent);
            BuildMobileStatusCards(_battleTabContent);
            BuildEquipmentOverview(_battleTabContent);

            _bagTabContent = new VisualElement();
            _bagTabContent.style.flexDirection = FlexDirection.Column;
            root.Add(_bagTabContent);
            BuildBagPanel(_bagTabContent);
            BuildMobileDetailPanel(_bagTabContent);

            _craftTabContent = new VisualElement();
            _craftTabContent.style.flexDirection = FlexDirection.Column;
            root.Add(_craftTabContent);
            BuildCraftPanel(_craftTabContent);

            _talentTabContent = new VisualElement();
            _talentTabContent.style.flexDirection = FlexDirection.Column;
            root.Add(_talentTabContent);
            BuildTalentPanel(_talentTabContent);

            RefreshActiveTab();
        }

        private void SetActiveTab(MainTab tab)
        {
            _activeTab = tab;
            RefreshActiveTab();
            RefreshBottomNav();
        }

        private void RefreshActiveTab()
        {
            SetTabVisible(_battleTabContent, _activeTab == MainTab.Battle);
            SetTabVisible(_bagTabContent, _activeTab == MainTab.Bag);
            SetTabVisible(_craftTabContent, _activeTab == MainTab.Craft);
            SetTabVisible(_talentTabContent, _activeTab == MainTab.Talent);
        }

        private static void SetTabVisible(VisualElement tab, bool visible)
        {
            if (tab == null) return;
            tab.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void BuildTopHud(VisualElement root)
        {
            var header = Panel("top-hud");
            header.style.height = 92;
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.marginBottom = 6;
            header.style.paddingTop = 4;
            header.style.paddingBottom = 4;
            header.style.backgroundColor = new StyleColor(new Color32(9, 9, 8, 245));
            root.Add(header);

            var titleCol = Column(96);
            titleCol.style.alignItems = Align.Center;
            var avatar = new VisualElement();
            avatar.style.width = 66;
            avatar.style.height = 66;
            avatar.style.borderTopWidth = 3;
            avatar.style.borderRightWidth = 3;
            avatar.style.borderBottomWidth = 3;
            avatar.style.borderLeftWidth = 3;
            avatar.style.borderTopColor = new StyleColor(PanelBorderHot);
            avatar.style.borderRightColor = new StyleColor(PanelBorderHot);
            avatar.style.borderBottomColor = new StyleColor(new Color32(70, 43, 25, 255));
            avatar.style.borderLeftColor = new StyleColor(PanelBorderHot);
            avatar.style.borderTopLeftRadius = 33;
            avatar.style.borderTopRightRadius = 33;
            avatar.style.borderBottomLeftRadius = 33;
            avatar.style.borderBottomRightRadius = 33;
            avatar.style.backgroundColor = new StyleColor(new Color32(25, 18, 15, 255));
            var title = Text("72", 22, true);
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.color = new StyleColor(GoldText);
            title.style.flexGrow = 1;
            avatar.Add(title);
            titleCol.Add(avatar);
            _statusText = Text("", 13, true);
            _statusText.style.color = new StyleColor(TextMuted);
            titleCol.Add(_statusText);
            header.Add(titleCol);

            _floorText = HudCell(header, "层数", "1", 92);
            _powerText = HudCell(header, "战力", "0", 142);
            _soulsText = HudCell(header, "魂点", "0", 112);
            _syncText = HudCell(header, "账号", "hero", 122);
            _powerText.style.color = new StyleColor(new Color32(255, 178, 83, 255));

            var loginCol = Row();
            loginCol.style.flexGrow = 1;
            loginCol.style.justifyContent = Justify.FlexEnd;
            _accountInput = new TextField { value = "hero" };
            _accountInput.style.width = 118;
            _accountInput.style.height = 34;
            loginCol.Add(_accountInput);
            loginCol.Add(ActionButton(L10n.UIConnect, () =>
            {
                string acc = _accountInput.value.Trim();
                if (string.IsNullOrEmpty(acc)) acc = "hero";
                _statusText.text = "在线状态：连接中...";
                _gameState.ConnectAndLogin(acc);
            }, 82, ButtonEquip));
            header.Add(loginCol);
        }

        private static Label HudCell(VisualElement parent, string label, string value, float width)
        {
            var cell = new VisualElement();
            cell.style.width = width;
            cell.style.marginRight = 10;
            cell.style.paddingLeft = 10;
            cell.style.paddingRight = 10;
            cell.style.paddingTop = 7;
            cell.style.paddingBottom = 7;
            cell.style.backgroundColor = new StyleColor(new Color32(15, 13, 11, 255));
            cell.style.borderTopWidth = 1;
            cell.style.borderRightWidth = 1;
            cell.style.borderBottomWidth = 1;
            cell.style.borderLeftWidth = 1;
            cell.style.borderTopColor = new StyleColor(new Color32(45, 38, 31, 255));
            cell.style.borderRightColor = new StyleColor(new Color32(45, 38, 31, 255));
            cell.style.borderBottomColor = new StyleColor(PanelBorderHot);
            cell.style.borderLeftColor = new StyleColor(new Color32(45, 38, 31, 255));

            var labelText = Text(label, 11, true);
            labelText.style.color = new StyleColor(GoldText);
            var valueText = Text(value, 18, true);
            cell.Add(labelText);
            cell.Add(valueText);
            parent.Add(cell);
            return valueText;
        }

        private void BuildBottomBar(VisualElement root)
        {
            var bottom = Panel("bottom-bar");
            bottom.style.height = 84;
            bottom.style.marginTop = 8;
            bottom.style.flexDirection = FlexDirection.Column;
            root.Add(bottom);

            var log = new VisualElement();
            log.style.height = 24;
            log.style.paddingLeft = 8;
            log.style.paddingRight = 8;
            log.style.justifyContent = Justify.Center;
            _toastText = Text("", 13, false);
            _toastText.style.color = new StyleColor(new Color32(244, 202, 121, 255));
            log.Add(_toastText);
            bottom.Add(log);

            var nav = Row();
            _bottomNavContent = nav;
            nav.style.flexGrow = 1;
            nav.style.alignItems = Align.Stretch;
            bottom.Add(nav);

            RefreshBottomNav();
        }

        private void RefreshBottomNav()
        {
            if (_bottomNavContent == null) return;
            _bottomNavContent.Clear();
            _bottomNavContent.Add(NavButton("战斗", () => SetActiveTab(MainTab.Battle), _activeTab == MainTab.Battle ? ButtonCraft : ButtonDefault));
            _bottomNavContent.Add(NavButton("背包", () => SetActiveTab(MainTab.Bag), _activeTab == MainTab.Bag ? ButtonCraft : ButtonDefault));
            _bottomNavContent.Add(NavButton("锻造", () => SetActiveTab(MainTab.Craft), _activeTab == MainTab.Craft ? ButtonCraft : ButtonDefault));
            _bottomNavContent.Add(NavButton("天赋", () => SetActiveTab(MainTab.Talent), _activeTab == MainTab.Talent ? ButtonCraft : ButtonDefault));
        }

        private static Button NavButton(string label, System.Action action, Color32 color)
        {
            var button = ActionButton(label, action, -1, color);
            button.style.flexGrow = 1;
            button.style.height = Length.Percent(100);
            button.style.fontSize = 16;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.marginLeft = 2;
            button.style.marginRight = 2;
            return button;
        }

        private void BuildDungeonPanel(VisualElement root)
        {
            var dungeon = Panel("dungeon");
            _dungeonPanel = dungeon;
            dungeon.style.height = 540;
            dungeon.style.marginBottom = 8;
            dungeon.style.flexDirection = FlexDirection.Column;
            dungeon.style.backgroundColor = new StyleColor(new Color32(7, 8, 8, 255));
            dungeon.style.paddingLeft = 8;
            dungeon.style.paddingRight = 8;
            dungeon.style.paddingTop = 8;
            dungeon.style.paddingBottom = 8;
            root.Add(dungeon);

            var bossRow = Row();
            bossRow.style.marginBottom = 5;
            _dungeonTitleText = Text("", 23, true);
            _dungeonTitleText.style.flexGrow = 1;
            _dungeonTitleText.style.color = new StyleColor(new Color32(255, 72, 54, 255));
            _dungeonTitleText.style.unityTextAlign = TextAnchor.MiddleCenter;
            bossRow.Add(_dungeonTitleText);
            _monsterText = Text("", 14, true);
            _monsterText.style.unityTextAlign = TextAnchor.MiddleRight;
            _monsterText.style.color = new StyleColor(TextMuted);
            bossRow.Add(_monsterText);
            dungeon.Add(bossRow);

            var progressFrame = new VisualElement();
            progressFrame.style.height = 24;
            progressFrame.style.marginLeft = 170;
            progressFrame.style.marginRight = 170;
            progressFrame.style.marginBottom = 7;
            progressFrame.style.backgroundColor = new StyleColor(new Color32(10, 8, 7, 255));
            progressFrame.style.borderTopWidth = 2;
            progressFrame.style.borderRightWidth = 2;
            progressFrame.style.borderBottomWidth = 2;
            progressFrame.style.borderLeftWidth = 2;
            progressFrame.style.borderTopColor = new StyleColor(new Color32(66, 52, 42, 255));
            progressFrame.style.borderRightColor = new StyleColor(new Color32(66, 52, 42, 255));
            progressFrame.style.borderBottomColor = new StyleColor(new Color32(24, 19, 16, 255));
            progressFrame.style.borderLeftColor = new StyleColor(new Color32(66, 52, 42, 255));
            progressFrame.style.borderTopLeftRadius = 5;
            progressFrame.style.borderTopRightRadius = 5;
            progressFrame.style.borderBottomLeftRadius = 5;
            progressFrame.style.borderBottomRightRadius = 5;
            _bossProgressFill = new VisualElement();
            _bossProgressFill.style.height = Length.Percent(100);
            _bossProgressFill.style.backgroundColor = new StyleColor(new Color32(234, 88, 12, 255));
            _bossProgressFill.style.borderTopLeftRadius = 5;
            _bossProgressFill.style.borderTopRightRadius = 5;
            _bossProgressFill.style.borderBottomLeftRadius = 5;
            _bossProgressFill.style.borderBottomRightRadius = 5;
            progressFrame.Add(_bossProgressFill);
            dungeon.Add(progressFrame);

            dungeon.Add(BuildBattleStage());

            var combatFooter = Row();
            combatFooter.style.marginTop = 8;
            combatFooter.style.alignItems = Align.FlexStart;
            _battleText = Text("", 16, true);
            _battleText.style.color = new StyleColor(GoldText);
            _stuckText = Text("", 13, false);
            _stuckText.style.color = new StyleColor(new Color32(248, 113, 113, 255));
            _goalText = Text("", 13, false);
            _bossHintText = Text("", 13, true);
            _bossHintText.style.color = new StyleColor(EmberText);
            var left = new VisualElement();
            left.style.flexGrow = 1;
            left.Add(_battleText);
            left.Add(_stuckText);
            left.Add(_goalText);
            var right = new VisualElement();
            right.style.width = 300;
            right.Add(_bossHintText);
            combatFooter.Add(left);
            combatFooter.Add(right);
            dungeon.Add(combatFooter);
        }

        private void BuildMobileStatusCards(VisualElement root)
        {
            var row = Row();
            row.style.height = 136;
            row.style.marginBottom = 8;
            root.Add(row);

            _objectiveCardText = StatusCard(row, "当前目标", "连接后开始自动战斗。");

            var bossCard = Panel("boss-progress-card");
            bossCard.style.flexGrow = 1;
            bossCard.style.marginRight = 10;
            bossCard.Add(SectionTitle("BOSS 进度"));
            _bossProgressCardText = Text("每 5 层出现 Boss。", 13, true);
            _bossProgressCardText.style.flexGrow = 1;
            bossCard.Add(_bossProgressCardText);
            _progressNodesContent = Row();
            _progressNodesContent.style.height = 34;
            _progressNodesContent.style.alignItems = Align.Center;
            bossCard.Add(_progressNodesContent);
            row.Add(bossCard);

            var lootCard = Panel("recent-loot-card");
            _lootPanel = lootCard;
            lootCard.style.flexGrow = 1;
            lootCard.style.marginLeft = 10;
            lootCard.Add(SectionTitle("最近掉落"));
            _lootFeedContent = new VisualElement();
            _lootFeedContent.style.flexGrow = 1;
            lootCard.Add(_lootFeedContent);
            row.Add(lootCard);
        }

        private Label StatusCard(VisualElement parent, string title, string initialText)
        {
            var card = Panel(title);
            card.style.flexGrow = 1;
            card.style.marginRight = 10;
            card.Add(SectionTitle(title));
            var label = Text(initialText, 15, true);
            label.style.flexGrow = 1;
            card.Add(label);
            parent.Add(card);
            return label;
        }

        private void BuildEquipmentOverview(VisualElement root)
        {
            var equipped = Panel("battle-equipped-overview");
            equipped.style.height = 205;
            equipped.style.marginBottom = 8;
            equipped.Add(SectionTitle("穿戴装备"));
            _equippedContent = new VisualElement();
            _equippedContent.style.flexDirection = FlexDirection.Row;
            _equippedContent.style.flexWrap = Wrap.Wrap;
            _equippedContent.style.flexGrow = 1;
            equipped.Add(_equippedContent);
            root.Add(equipped);
        }

        private void BuildBagPanel(VisualElement root)
        {
            var row = Row();
            row.style.height = 430;
            row.style.marginBottom = 8;
            root.Add(row);

            var bag = Panel("mobile-bag");
            bag.style.flexGrow = 1;
            bag.style.marginRight = 10;
            bag.Add(SectionTitle("背包"));
            _bagFilterActions = new VisualElement();
            _bagFilterActions.style.flexDirection = FlexDirection.Row;
            _bagFilterActions.style.flexWrap = Wrap.Wrap;
            _bagFilterActions.style.marginBottom = 4;
            bag.Add(_bagFilterActions);
            var bagScroll = new ScrollView();
            bagScroll.style.flexGrow = 1;
            _bagContent = bagScroll.contentContainer;
            bag.Add(bagScroll);
            row.Add(bag);

            var equipped = Panel("bag-equipped");
            equipped.style.width = 350;
            equipped.Add(SectionTitle("当前穿戴"));
            var equippedScroll = new ScrollView();
            equippedScroll.style.flexGrow = 1;
            var equippedMirror = equippedScroll.contentContainer;
            equippedMirror.style.flexDirection = FlexDirection.Row;
            equippedMirror.style.flexWrap = Wrap.Wrap;
            _equippedMirrorContent = equippedMirror;
            equipped.Add(equippedScroll);
            row.Add(equipped);
        }

        private void BuildCraftPanel(VisualElement root)
        {
            var row = Row();
            row.style.height = 430;
            row.style.marginBottom = 8;
            root.Add(row);

            var craft = Panel("mobile-craft");
            craft.style.flexGrow = 1;
            craft.style.marginRight = 10;
            craft.Add(SectionTitle("强化"));
            if (_craftIcon != null)
            {
                var craftImage = IconImage(_craftIcon, 84, 84);
                craftImage.style.alignSelf = Align.Center;
                craft.Add(craftImage);
            }
            _craftPlanContent = new VisualElement();
            _craftPlanContent.style.flexGrow = 1;
            craft.Add(_craftPlanContent);
            craft.Add(ActionButton(L10n.UIEquipBest, EquipBestBySlot, -1, ButtonEquip));
            craft.Add(ActionButton(L10n.UIDecomposeWeak, DecomposeWeakItems, -1, ButtonDanger));
            row.Add(craft);

            var mats = Panel("mobile-materials");
            mats.style.flexGrow = 1;
            mats.Add(SectionTitle("材料摘要"));
            _materialsText = Text("", 14, false);
            _materialsText.style.flexGrow = 1;
            mats.Add(_materialsText);
            row.Add(mats);

            var compose = Panel("compose-panel");
            compose.style.flexGrow = 1;
            compose.style.marginLeft = 10;
            compose.Add(SectionTitle("合成"));
            for (int s = 0; s < 8; s++)
            {
                int slot = s;
                compose.Add(ActionButton(L10n.SlotName(slot), () => _gameState.Compose(slot), -1, ButtonCraft));
            }
            row.Add(compose);
        }

        private void BuildTalentPanel(VisualElement root)
        {
            var panel = Panel("talent-panel");
            panel.style.height = 430;
            panel.style.marginBottom = 8;
            root.Add(panel);

            panel.Add(SectionTitle("转生天赋"));
            _reincarnText = Text("", 15, false);
            panel.Add(_reincarnText);
            _reincarnPlanContent = new VisualElement();
            _reincarnPlanContent.style.marginTop = 4;
            _reincarnPlanContent.style.marginBottom = 4;
            panel.Add(_reincarnPlanContent);
            panel.Add(ActionButton(L10n.UIReincarnate, () =>
            {
                if (_gameState.CanReincarn) _gameState.Reincarn();
            }, -1, ButtonAscend));
            _talentsText = Text("", 14, false);
            _talentsText.style.flexGrow = 1;
            panel.Add(_talentsText);
            _talentActions = new VisualElement();
            _talentActions.style.flexDirection = FlexDirection.Row;
            _talentActions.style.flexWrap = Wrap.Wrap;
            _talentActions.style.marginTop = 4;
            panel.Add(_talentActions);
        }

        private void RefreshBagFilterActions()
        {
            if (_bagFilterActions == null) return;
            _bagFilterActions.Clear();
            _bagFilterActions.Add(FilterButton("全部", BagFilter.All));
            _bagFilterActions.Add(FilterButton("提升", BagFilter.Upgrades));
            _bagFilterActions.Add(FilterButton("稀有", BagFilter.Rare));
            _bagFilterActions.Add(FilterButton("分解", BagFilter.Decompose));
        }

        private Button FilterButton(string label, BagFilter filter)
        {
            var button = ActionButton(label, () =>
            {
                _bagFilter = filter;
                RefreshEquipmentLists();
            }, 48, _bagFilter == filter ? ButtonCraft : ButtonDefault);
            button.style.height = 28;
            button.style.fontSize = 11;
            button.style.marginLeft = 1;
            button.style.marginRight = 1;
            return button;
        }

        private void BuildMobileDetailPanel(VisualElement root)
        {
            var detail = Panel("mobile-detail");
            detail.style.height = 250;
            detail.style.marginBottom = 0;
            detail.style.borderTopWidth = 2;
            detail.style.borderTopColor = new StyleColor(PanelBorderHot);
            root.Add(detail);

            detail.Add(SectionTitle("装备详情"));
            var detailColumns = Row();
            detailColumns.style.flexGrow = 1;
            detailColumns.style.marginBottom = 8;
            _selectedDetailContent = DetailColumn("当前选择", 1.05f);
            _equippedDetailContent = DetailColumn("已装备", 1.05f);
            _compareDetailContent = DetailColumn("对比结果", 1.25f);
            detailColumns.Add(_selectedDetailContent);
            detailColumns.Add(_equippedDetailContent);
            detailColumns.Add(_compareDetailContent);
            detail.Add(detailColumns);

            _detailActions = new VisualElement();
            _detailActions.style.flexDirection = FlexDirection.Row;
            _detailActions.style.flexWrap = Wrap.Wrap;
            _detailActions.style.marginTop = 8;
            _detailActions.style.marginBottom = 8;
            detail.Add(_detailActions);
        }

        private VisualElement DetailColumn(string title, float grow)
        {
            var column = new VisualElement();
            column.style.flexGrow = grow;
            column.style.marginRight = 8;
            column.style.paddingLeft = 8;
            column.style.paddingRight = 8;
            column.style.paddingTop = 6;
            column.style.paddingBottom = 6;
            column.style.backgroundColor = new StyleColor(new Color32(13, 12, 10, 235));
            column.style.borderTopWidth = 1;
            column.style.borderRightWidth = 1;
            column.style.borderBottomWidth = 1;
            column.style.borderLeftWidth = 1;
            column.style.borderTopColor = new StyleColor(new Color32(72, 52, 34, 255));
            column.style.borderRightColor = new StyleColor(new Color32(72, 52, 34, 255));
            column.style.borderBottomColor = new StyleColor(new Color32(40, 33, 24, 255));
            column.style.borderLeftColor = new StyleColor(new Color32(72, 52, 34, 255));

            var label = Text(title, 13, true);
            label.style.color = new StyleColor(GoldText);
            label.style.marginBottom = 4;
            column.Add(label);
            return column;
        }

        private VisualElement BuildBattleStage()
        {
            var stage = new VisualElement();
            stage.style.flexGrow = 1;
            stage.style.marginTop = 4;
            stage.style.paddingLeft = 16;
            stage.style.paddingRight = 16;
            stage.style.paddingTop = 14;
            stage.style.paddingBottom = 12;
            stage.style.backgroundColor = new StyleColor(new Color32(15, 17, 16, 255));
            stage.style.overflow = Overflow.Hidden;
            if (_battleBackground != null)
            {
                var bg = new Image { image = _battleBackground, scaleMode = ScaleMode.ScaleAndCrop };
                bg.style.position = Position.Absolute;
                bg.style.left = 0;
                bg.style.right = 0;
                bg.style.top = 0;
                bg.style.bottom = 0;
                bg.style.opacity = 0.92f;
                stage.Add(bg);
            }
            if (_heroSprite != null)
            {
                _stageHeroSpriteImage = StageSprite(_heroSprite, 170, 170, 34, null, ScaleMode.ScaleToFit);
                stage.Add(_stageHeroSpriteImage);
            }
            if (_bossSprite != null)
            {
                _stageBossSpriteImage = StageSprite(_bossSprite, 270, 270, null, 28, ScaleMode.ScaleToFit);
                stage.Add(_stageBossSpriteImage);
            }
            _stageImpactText = Text("", 24, true);
            _stageImpactText.style.position = Position.Absolute;
            _stageImpactText.style.left = 0;
            _stageImpactText.style.right = 0;
            _stageImpactText.style.top = 172;
            _stageImpactText.style.unityTextAlign = TextAnchor.MiddleCenter;
            _stageImpactText.style.color = new StyleColor(new Color32(255, 224, 137, 255));
            _stageImpactText.style.opacity = 0f;
            stage.Add(_stageImpactText);
            _stageBannerText = Text("", 19, true);
            _stageBannerText.style.position = Position.Absolute;
            _stageBannerText.style.left = 34;
            _stageBannerText.style.right = 34;
            _stageBannerText.style.top = 50;
            _stageBannerText.style.height = 34;
            _stageBannerText.style.unityTextAlign = TextAnchor.MiddleCenter;
            _stageBannerText.style.color = new StyleColor(new Color32(255, 224, 137, 255));
            _stageBannerText.style.backgroundColor = new StyleColor(new Color32(55, 30, 14, 220));
            _stageBannerText.style.borderTopWidth = 1;
            _stageBannerText.style.borderRightWidth = 1;
            _stageBannerText.style.borderBottomWidth = 1;
            _stageBannerText.style.borderLeftWidth = 1;
            _stageBannerText.style.borderTopColor = new StyleColor(PanelBorderHot);
            _stageBannerText.style.borderRightColor = new StyleColor(PanelBorderHot);
            _stageBannerText.style.borderBottomColor = new StyleColor(PanelBorderHot);
            _stageBannerText.style.borderLeftColor = new StyleColor(PanelBorderHot);
            _stageBannerText.style.opacity = 0f;
            stage.Add(_stageBannerText);
            stage.style.borderTopWidth = 1;
            stage.style.borderRightWidth = 1;
            stage.style.borderBottomWidth = 1;
            stage.style.borderLeftWidth = 1;
            stage.style.borderTopColor = new StyleColor(PanelBorderHot);
            stage.style.borderRightColor = new StyleColor(new Color32(70, 47, 31, 255));
            stage.style.borderBottomColor = new StyleColor(new Color32(42, 27, 19, 255));
            stage.style.borderLeftColor = new StyleColor(new Color32(70, 47, 31, 255));
            stage.style.borderTopLeftRadius = 6;
            stage.style.borderTopRightRadius = 6;
            stage.style.borderBottomLeftRadius = 6;
            stage.style.borderBottomRightRadius = 6;

            _stageZoneText = Text("", 13, true);
            _stageZoneText.style.unityTextAlign = TextAnchor.MiddleCenter;
            _stageZoneText.style.marginBottom = 6;
            _stageZoneText.style.color = new StyleColor(GoldText);
            stage.Add(_stageZoneText);

            var combatants = Row();
            combatants.style.alignItems = Align.Stretch;
            combatants.style.flexGrow = 1;

            VisualElement heroCard = CombatantCard(
                new Color32(59, 130, 246, 255),
                out _stageHeroNameText,
                out _stageHeroPowerText,
                out _stageHeroHealthFill);
            combatants.Add(heroCard);

            var center = new VisualElement();
            center.style.width = 54;
            center.style.alignItems = Align.Center;
            center.style.justifyContent = Justify.Center;
            _stageSlash = Text("VS", 20, true);
            _stageSlash.style.unityTextAlign = TextAnchor.MiddleCenter;
            _stageSlash.style.color = new StyleColor(new Color32(255, 202, 112, 255));
            center.Add(_stageSlash);
            combatants.Add(center);

            VisualElement monsterCard = CombatantCard(
                new Color32(220, 38, 38, 255),
                out _stageMonsterNameText,
                out _stageMonsterPowerText,
                out _stageMonsterHealthFill);
            combatants.Add(monsterCard);
            stage.Add(combatants);

            _stageStatusText = Text("", 14, true);
            _stageStatusText.style.unityTextAlign = TextAnchor.MiddleCenter;
            _stageStatusText.style.marginTop = 6;
            _stageStatusText.style.color = new StyleColor(GoldText);
            stage.Add(_stageStatusText);
            return stage;
        }

        private static Image StageSprite(Texture2D texture, float width, float height, float? left, float? right, ScaleMode scaleMode)
        {
            var image = new Image { image = texture, scaleMode = scaleMode };
            image.style.position = Position.Absolute;
            image.style.width = width;
            image.style.height = height;
            image.style.bottom = 6;
            if (left.HasValue) image.style.left = left.Value;
            if (right.HasValue) image.style.right = right.Value;
            image.style.opacity = 0.96f;
            return image;
        }

        private static VisualElement CombatantCard(Color accent, out Label name, out Label power, out VisualElement healthFill)
        {
            var card = new VisualElement();
            card.style.flexGrow = 1;
            card.style.paddingLeft = 8;
            card.style.paddingRight = 8;
            card.style.paddingTop = 6;
            card.style.paddingBottom = 6;
            card.style.backgroundColor = new StyleColor(new Color32(10, 9, 8, 175));
            card.style.borderTopWidth = 1;
            card.style.borderRightWidth = 1;
            card.style.borderBottomWidth = 3;
            card.style.borderLeftWidth = 1;
            card.style.borderTopColor = new StyleColor(new Color32(68, 52, 40, 220));
            card.style.borderRightColor = new StyleColor(new Color32(68, 52, 40, 220));
            card.style.borderBottomColor = accent;
            card.style.borderLeftColor = new StyleColor(new Color32(68, 52, 40, 220));
            card.style.borderTopLeftRadius = 5;
            card.style.borderTopRightRadius = 5;
            card.style.borderBottomLeftRadius = 5;
            card.style.borderBottomRightRadius = 5;

            name = Text("", 16, true);
            name.style.unityTextAlign = TextAnchor.MiddleCenter;
            name.style.color = new StyleColor(GoldText);
            power = Text("", 12, false);
            power.style.unityTextAlign = TextAnchor.MiddleCenter;
            power.style.color = new StyleColor(TextMain);
            card.Add(name);
            card.Add(power);

            var healthFrame = new VisualElement();
            healthFrame.style.height = 8;
            healthFrame.style.marginTop = 8;
            healthFrame.style.backgroundColor = new StyleColor(new Color32(15, 18, 16, 255));
            healthFrame.style.borderTopLeftRadius = 4;
            healthFrame.style.borderTopRightRadius = 4;
            healthFrame.style.borderBottomLeftRadius = 4;
            healthFrame.style.borderBottomRightRadius = 4;
            healthFill = new VisualElement();
            healthFill.style.height = Length.Percent(100);
            healthFill.style.backgroundColor = new StyleColor(accent);
            healthFill.style.borderTopLeftRadius = 4;
            healthFill.style.borderTopRightRadius = 4;
            healthFill.style.borderBottomLeftRadius = 4;
            healthFill.style.borderBottomRightRadius = 4;
            healthFrame.Add(healthFill);
            card.Add(healthFrame);

            return card;
        }

        private void BuildOfflinePanel(VisualElement root)
        {
            _offlinePanel = new VisualElement();
            _offlinePanel.style.position = Position.Absolute;
            _offlinePanel.style.left = 0;
            _offlinePanel.style.right = 0;
            _offlinePanel.style.top = 0;
            _offlinePanel.style.bottom = 0;
            _offlinePanel.style.backgroundColor = new Color(0, 0, 0, 0.65f);
            _offlinePanel.style.alignItems = Align.Center;
            _offlinePanel.style.justifyContent = Justify.Center;
            _offlinePanel.style.display = DisplayStyle.None;

            var box = Panel("offline");
            box.style.width = 380;
            box.style.height = 240;
            _offlineText = Text("", 15, false);
            _offlineText.style.flexGrow = 1;
            box.Add(_offlineText);
            box.Add(ActionButton(L10n.UIOK, () => _offlinePanel.style.display = DisplayStyle.None));
            _offlinePanel.Add(box);
            root.Add(_offlinePanel);
        }

        private void RefreshAll()
        {
            RefreshHeader();
            RefreshEquipmentLists();
            RefreshMaterials();
            RefreshProgression();
            RefreshDetail();
            RefreshCraftPlan();
            RefreshStuck();
            RefreshDungeon();
            RefreshLootFeed();
        }

        private void RefreshHeader()
        {
            string status = _gameState.IsConnected ? L10n.UIStatusConnected : L10n.UIStatusDisconnected;
            _statusText.text = "在线状态：" + status;
            _syncText.text = string.IsNullOrEmpty(_gameState.Account) ? "hero" : _gameState.Account;
            if (_floorText != null) _floorText.text = $"{_gameState.Floor} 层";
            if (_soulsText != null) _soulsText.text = _gameState.Souls.ToString();
            _powerText.text = string.Format(L10n.UIPowerLabel, _gameState.Power);
            RefreshDungeon();
        }

        private void RefreshStuck()
        {
            float monsterPower = EquipmentPresenter.MonsterPowerAtFloor(_gameState.Floor);
            if (_gameState.Power > 0 && _gameState.Power <= monsterPower)
                _stuckText.text = string.Format(L10n.UIStuckPrefix, _gameState.Power, monsterPower, _gameState.Floor);
            else
                _stuckText.text = $"怪物战力：{monsterPower:F1}";
            RefreshDungeon();
        }

        private void RefreshDungeon()
        {
            if (_dungeonTitleText == null) return;
            var dungeon = EquipmentPresenter.BuildDungeonState(_gameState.Floor, _gameState.Power);
            _bossProgressTarget = dungeon.GateProgress;

            _dungeonTitleText.text = dungeon.Title;
            _monsterText.text = dungeon.Monster;
            _battleText.text = dungeon.Battle;
            _bossHintText.text = dungeon.BossHint;
            _bossHintText.style.color = new StyleColor(dungeon.IsBoss ? new Color32(255, 92, 64, 255) : EmberText);
            RefreshBattleStage();
            string nextGoal = EquipmentPresenter.BuildNextGoal(
                _gameState.Floor,
                _gameState.Power,
                _gameState.CanReincarn,
                _gameState.Bag.Count,
                BaseMaterialCount());
            _goalText.text = nextGoal;
            if (_objectiveCardText != null) _objectiveCardText.text = nextGoal.Replace("目标：", "");
            if (_bossProgressCardText != null) _bossProgressCardText.text = dungeon.BossHint;
            _bossProgressFill.style.backgroundColor = new StyleColor(dungeon.IsBoss ? new Color32(220, 38, 38, 255) : new Color32(217, 119, 6, 255));
            RefreshProgressNodes();
        }

        private void RefreshProgressNodes()
        {
            if (_progressNodesContent == null) return;
            _progressNodesContent.Clear();
            foreach (var node in EquipmentPresenter.BuildProgressNodes(_gameState.Floor))
            {
                _progressNodesContent.Add(ProgressNode(node));
            }
        }

        private VisualElement ProgressNode(ProgressNodeState node)
        {
            var box = new Label(node.Label);
            box.style.flexGrow = 1;
            box.style.height = 26;
            box.style.marginLeft = 2;
            box.style.marginRight = 2;
            box.style.unityTextAlign = TextAnchor.MiddleCenter;
            box.style.fontSize = node.IsBoss ? 13 : 12;
            box.style.unityFontStyleAndWeight = node.Current || node.IsBoss ? FontStyle.Bold : FontStyle.Normal;
            box.style.color = new StyleColor(node.Current ? new Color32(255, 247, 237, 255) : node.Passed ? GoldText : TextMuted);
            box.style.backgroundColor = new StyleColor(node.Current
                ? (node.IsBoss ? new Color32(136, 32, 24, 255) : new Color32(126, 72, 24, 255))
                : node.Passed ? new Color32(52, 42, 26, 255) : new Color32(14, 13, 11, 255));
            box.style.borderTopWidth = 1;
            box.style.borderRightWidth = 1;
            box.style.borderBottomWidth = 1;
            box.style.borderLeftWidth = 1;
            Color32 border = node.IsBoss ? new Color32(255, 92, 64, 255) : node.Current ? PanelBorderHot : PanelBorder;
            box.style.borderTopColor = new StyleColor(border);
            box.style.borderRightColor = new StyleColor(border);
            box.style.borderBottomColor = new StyleColor(border);
            box.style.borderLeftColor = new StyleColor(border);
            box.style.borderTopLeftRadius = 4;
            box.style.borderTopRightRadius = 4;
            box.style.borderBottomLeftRadius = 4;
            box.style.borderBottomRightRadius = 4;
            return box;
        }

        private void RefreshBattleStage()
        {
            if (_stageHeroNameText == null) return;
            var stage = EquipmentPresenter.BuildBattleStageState(_gameState.Floor, _gameState.Power);
            _stageZoneText.text = stage.Zone;
            _stageHeroNameText.text = stage.HeroName;
            _stageHeroPowerText.text = stage.HeroPower;
            _stageMonsterNameText.text = stage.MonsterName;
            _stageMonsterPowerText.text = stage.MonsterPower;
            _stageStatusText.text = stage.IsBoss ? $"Boss 战：{stage.Status}" : $"自动战斗：{stage.Status}";
            _stageStatusText.style.color = new StyleColor(stage.Status == "受阻" ? new Color32(248, 113, 113, 255) : GoldText);
            if (_stageSlash != null)
            {
                _stageSlash.text = stage.IsBoss ? "BOSS" : "VS";
                _stageSlash.style.color = new StyleColor(stage.IsBoss ? new Color32(255, 92, 64, 255) : new Color32(255, 202, 112, 255));
            }
            _stageHeroHealthFill.style.width = Length.Percent(stage.HeroHealth * 100f);
            _stageMonsterHealthFill.style.width = Length.Percent(stage.MonsterHealth * 100f);
            _stageMonsterHealthFill.style.backgroundColor = new StyleColor(stage.IsBoss ? new Color32(220, 38, 38, 255) : new Color32(245, 158, 11, 255));
        }

        private int BaseMaterialCount()
        {
            return _gameState.Materials.TryGetValue("base_mat", out var value) ? value : 0;
        }

        private void RefreshLootFeed()
        {
            if (_lootFeedContent == null) return;
            _lootFeedContent.Clear();
            if (_lootFeed.Count == 0)
            {
                _lootFeedText = Text("暂无掉落。连接后自动战斗会开始产出装备。", 13, false);
                _lootFeedContent.Add(_lootFeedText);
                return;
            }
            foreach (var eq in _lootFeed)
            {
                _lootFeedContent.Add(LootFeedRow(eq));
            }
        }

        private VisualElement LootFeedRow(EquipmentDTO eq)
        {
            var row = Row();
            int rarity = eq != null ? eq.rarity : 0;
            row.style.backgroundColor = new StyleColor(RarityRowBackground(rarity));
            row.style.borderLeftWidth = 5;
            row.style.borderLeftColor = RarityUIColor(eq != null ? eq.rarity : 0);
            row.style.borderTopWidth = 1;
            row.style.borderRightWidth = 1;
            row.style.borderBottomWidth = 1;
            row.style.borderTopColor = new StyleColor(RarityFrameColor(rarity));
            row.style.borderRightColor = new StyleColor(RarityFrameColor(rarity));
            row.style.borderBottomColor = new StyleColor(RarityFrameColor(rarity));
            row.style.paddingLeft = 6;
            row.style.paddingRight = 6;
            row.style.paddingTop = 3;
            row.style.paddingBottom = 3;
            row.style.marginBottom = 4;

            var icon = IconImage(IconForSlot(eq != null ? eq.slot : 0), 36, 36);
            icon.style.marginRight = 6;
            row.Add(icon);

            var label = Text(EquipmentPresenter.BuildLootLine(eq), 12, eq != null && eq.rarity >= 2);
            label.style.color = RarityUIColor(eq != null ? eq.rarity : 0);
            label.style.flexGrow = 1;
            row.Add(label);
            return row;
        }

        private void UpdateCombatFeedback()
        {
            float now = Time.realtimeSinceStartup;
            if (_gameState != null && _gameState.IsConnected && _gameState.Power > 0f && now >= _nextCombatBeatAt)
            {
                StartCombatBeat();
            }

            if (_bossProgressFill != null)
            {
                _bossProgressCurrent = Mathf.Lerp(_bossProgressCurrent, _bossProgressTarget, Time.deltaTime * 8f);
                _bossProgressFill.style.width = Length.Percent(_bossProgressCurrent * 100f);
            }

            if (_dungeonPanel != null)
            {
                float pulse = Mathf.Clamp01((_battlePulseUntil - now) / 0.7f);
                Color baseColor = new Color32(7, 8, 8, 255);
                Color hitColor = new Color32(82, 21, 12, 255);
                _dungeonPanel.style.backgroundColor = new StyleColor(Color.Lerp(baseColor, hitColor, pulse));
                if (_stageSlash != null) _stageSlash.style.opacity = 0.65f + pulse * 0.35f;
            }

            RefreshCombatBeat(now);
            RefreshStageBanner(now);

            if (_lootPanel != null)
            {
                float pulse = Mathf.Clamp01((_lootPulseUntil - now) / 0.8f);
                Color baseColor = Color.clear;
                Color lootColor = new Color(0.75f, 0.50f, 0.12f, 0.35f);
                _lootPanel.style.backgroundColor = new StyleColor(Color.Lerp(baseColor, lootColor, pulse));
            }
        }

        private void ShowStageBanner(string text, Color color)
        {
            if (_stageBannerText == null) return;
            _stageBannerText.text = text;
            _stageBannerText.style.color = new StyleColor(color);
            _stageBannerUntil = Time.realtimeSinceStartup + 2.4f;
        }

        private void RefreshStageBanner(float now)
        {
            if (_stageBannerText == null) return;
            float remaining = _stageBannerUntil - now;
            if (remaining <= 0f)
            {
                _stageBannerText.style.opacity = 0f;
                return;
            }
            _stageBannerText.style.opacity = Mathf.Clamp01(remaining / 0.35f);
        }

        private void StartCombatBeat()
        {
            float now = Time.realtimeSinceStartup;
            _combatBeatStartedAt = now;
            _battlePulseUntil = now + 0.7f;
            _nextCombatBeatAt = now + CombatBeatInterval;
        }

        private void RefreshCombatBeat(float now)
        {
            if (_stageMonsterHealthFill == null) return;
            float elapsed = now - _combatBeatStartedAt;
            var beat = EquipmentPresenter.BuildCombatBeatState(_gameState.Floor, _gameState.Power, elapsed, CombatBeatDuration);
            var stage = EquipmentPresenter.BuildBattleStageState(_gameState.Floor, _gameState.Power);
            float monsterHealth = beat.Active ? beat.MonsterHealth : stage.MonsterHealth;
            _stageMonsterHealthFill.style.width = Length.Percent(monsterHealth * 100f);

            if (_stageHeroSpriteImage != null)
            {
                _stageHeroSpriteImage.style.left = 34 + beat.HeroOffset;
            }
            if (_stageBossSpriteImage != null)
            {
                _stageBossSpriteImage.style.right = 28 - beat.MonsterOffset;
            }
            if (_stageImpactText != null)
            {
                _stageImpactText.text = beat.Active ? beat.DamageText : "";
                _stageImpactText.style.opacity = beat.ImpactOpacity;
                _stageImpactText.style.top = 172 - beat.HeroOffset * 0.65f;
            }
        }

        private void RefreshEquipmentLists()
        {
            PruneLockedEquipment();
            RefreshBagFilterActions();
            RefreshEquippedTiles(_equippedContent);
            RefreshEquippedTiles(_equippedMirrorContent);

            _bagContent.Clear();
            foreach (var item in EquipmentPresenter.SortBagForDisplay(_gameState.Bag, _gameState.Equipped))
            {
                EquipmentDTO eq = item;
                if (!ShouldShowInBag(eq)) continue;
                _bagContent.Add(BagEquipmentCard(eq, EquippedAtSlot(eq.slot)));
            }
        }

        private void RefreshEquippedTiles(VisualElement container)
        {
            if (container == null) return;
            container.Clear();
            for (int slot = 0; slot < 8; slot++)
            {
                int capturedSlot = slot;
                EquipmentDTO eq = EquippedAtSlot(slot);
                if (eq == null)
                {
                    container.Add(EquipmentSlotTile(L10n.SlotName(slot), null, null, () => AddToast($"空槽位：{L10n.SlotName(capturedSlot)}", ToastDuration)));
                    continue;
                }
                EquipmentDTO capturedEq = eq;
                container.Add(EquipmentSlotTile(L10n.SlotName(slot), eq, () => _gameState.Unequip(capturedSlot), () => Select(capturedEq)));
            }
        }

        private bool ShouldShowInBag(EquipmentDTO eq)
        {
            if (eq == null) return false;
            EquipmentDTO current = EquippedAtSlot(eq.slot);
            float delta = EquipmentPresenter.Score(eq) - EquipmentPresenter.Score(current);
            switch (_bagFilter)
            {
                case BagFilter.Upgrades:
                    return delta > 0f;
                case BagFilter.Rare:
                    return eq.rarity >= 2;
                case BagFilter.Decompose:
                    return !_lockedEquipment.Contains(eq.uid) && eq.rarity <= 1 && delta <= 0f;
                default:
                    return true;
            }
        }

        private VisualElement EquipmentSlotTile(string slot, EquipmentDTO eq, System.Action secondary, System.Action select)
        {
            int rarity = eq != null ? eq.rarity : 0;
            var tile = new VisualElement();
            tile.style.width = Length.Percent(32);
            tile.style.height = 76;
            tile.style.marginRight = 5;
            tile.style.marginBottom = 6;
            tile.style.paddingLeft = 6;
            tile.style.paddingRight = 6;
            tile.style.paddingTop = 5;
            tile.style.paddingBottom = 5;
            tile.style.backgroundColor = new StyleColor(eq != null ? RarityTileBackground(rarity) : new Color32(12, 11, 10, 255));
            tile.style.borderTopWidth = 2;
            tile.style.borderRightWidth = 2;
            tile.style.borderBottomWidth = 2;
            tile.style.borderLeftWidth = 2;
            tile.style.borderTopColor = new StyleColor(eq != null ? RarityGlowColor(rarity) : new Color32(55, 48, 39, 255));
            tile.style.borderRightColor = new StyleColor(eq != null ? RarityGlowColor(rarity) : new Color32(55, 48, 39, 255));
            tile.style.borderBottomColor = new StyleColor(eq != null ? RarityFrameColor(rarity) : new Color32(31, 27, 22, 255));
            tile.style.borderLeftColor = new StyleColor(eq != null ? RarityGlowColor(rarity) : new Color32(55, 48, 39, 255));
            tile.RegisterCallback<ClickEvent>(_ => select?.Invoke());

            var top = Row();
            top.style.alignItems = Align.FlexStart;
            var icon = IconImage(IconForSlot(eq != null ? eq.slot : SlotIndexByName(slot)), 42, 42);
            icon.style.marginRight = 5;
            top.Add(icon);

            var copy = new VisualElement();
            copy.style.flexGrow = 1;
            var slotText = Text(slot + (eq != null && _lockedEquipment.Contains(eq.uid) ? " 锁" : ""), 11, true);
            slotText.style.color = new StyleColor(TextMuted);
            copy.Add(slotText);

            string name = eq != null ? eq.name : L10n.UIEmptySlot;
            var nameText = Text(name, 13, true);
            nameText.style.color = new StyleColor(eq != null ? RarityUIColor(eq.rarity) : new Color32(105, 96, 82, 255));
            copy.Add(nameText);
            top.Add(copy);
            tile.Add(top);

            string footer = eq != null ? $"+{eq.upgrade}  评分 {EquipmentPresenter.Score(eq):F0}" : "等待掉落";
            var footerText = Text(footer, 11, false);
            footerText.style.color = new StyleColor(eq != null && eq.rarity >= 2 ? GoldText : eq != null ? TextMain : TextMuted);
            tile.Add(footerText);
            return tile;
        }

        private VisualElement BagEquipmentCard(EquipmentDTO eq, EquipmentDTO current)
        {
            var row = Row();
            row.style.backgroundColor = new StyleColor(RarityTileBackground(eq.rarity));
            row.style.borderLeftWidth = 5;
            row.style.borderLeftColor = new StyleColor(RarityGlowColor(eq.rarity));
            row.style.borderTopWidth = 1;
            row.style.borderRightWidth = 1;
            row.style.borderBottomWidth = 1;
            row.style.borderTopColor = new StyleColor(RarityGlowColor(eq.rarity));
            row.style.borderRightColor = new StyleColor(RarityFrameColor(eq.rarity));
            row.style.borderBottomColor = new StyleColor(RarityFrameColor(eq.rarity));
            row.style.paddingLeft = 8;
            row.style.paddingRight = 6;
            row.style.paddingTop = 6;
            row.style.paddingBottom = 6;
            row.style.marginBottom = 6;

            var icon = IconImage(IconForSlot(eq.slot), 46, 46);
            icon.style.marginRight = 7;
            row.Add(icon);

            var body = new VisualElement();
            body.style.flexGrow = 1;
            body.RegisterCallback<ClickEvent>(_ => Select(eq));

            string lockText = _lockedEquipment.Contains(eq.uid) ? " 锁" : "";
            var title = Text($"{eq.name} +{eq.upgrade}{lockText}", 14, true);
            title.style.color = RarityUIColor(eq.rarity);
            body.Add(title);

            float delta = EquipmentPresenter.Score(eq) - EquipmentPresenter.Score(current);
            string state = current == null ? "新部位" : delta > 0f ? $"提升 +{delta:F0}" : delta < 0f ? $"更弱 {delta:F0}" : "持平";
            var meta = Text($"{L10n.SlotName(eq.slot)}  评分 {EquipmentPresenter.Score(eq):F0}  {state}", 11, false);
            meta.style.color = new StyleColor(delta > 0f || current == null ? new Color32(74, 222, 128, 255) : delta < 0f ? new Color32(248, 113, 113, 255) : TextMuted);
            body.Add(meta);

            row.Add(body);
            row.Add(ActionButton(_lockedEquipment.Contains(eq.uid) ? "解锁" : "锁", () => ToggleLock(eq), 50, ButtonDefault));
            row.Add(ActionButton(L10n.UIEquip, () => _gameState.Equip(eq.uid), 54, ButtonEquip));
            var decompose = ActionButton(L10n.UIDecompose, () => DecomposeFromUI(eq), 64, ButtonDanger);
            decompose.SetEnabled(!_lockedEquipment.Contains(eq.uid));
            row.Add(decompose);
            return row;
        }

        private Texture2D IconForSlot(int slot)
        {
            if (_slotIcons == null || _slotIcons.Length == 0) return null;
            if (slot < 0) slot = 0;
            if (slot >= _slotIcons.Length) slot = _slotIcons.Length - 1;
            return _slotIcons[slot];
        }

        private static Image IconImage(Texture2D texture, float width, float height)
        {
            var image = new Image { image = texture, scaleMode = ScaleMode.ScaleAndCrop };
            image.style.width = width;
            image.style.height = height;
            image.style.backgroundColor = new StyleColor(new Color32(9, 8, 7, 255));
            image.style.borderTopWidth = 1;
            image.style.borderRightWidth = 1;
            image.style.borderBottomWidth = 1;
            image.style.borderLeftWidth = 1;
            image.style.borderTopColor = new StyleColor(PanelBorderHot);
            image.style.borderRightColor = new StyleColor(new Color32(72, 45, 28, 255));
            image.style.borderBottomColor = new StyleColor(new Color32(35, 23, 16, 255));
            image.style.borderLeftColor = new StyleColor(PanelBorderHot);
            return image;
        }

        private static int SlotIndexByName(string slotName)
        {
            for (int i = 0; i < 8; i++)
            {
                if (L10n.SlotName(i) == slotName) return i;
            }
            return 0;
        }

        private void ToggleLock(EquipmentDTO eq)
        {
            if (eq == null) return;
            if (_lockedEquipment.Contains(eq.uid))
            {
                _lockedEquipment.Remove(eq.uid);
                AddToast($"已解锁：{eq.name}", ToastDuration);
            }
            else
            {
                _lockedEquipment.Add(eq.uid);
                AddToast($"已锁定：{eq.name}", ToastDuration);
            }
            RefreshEquipmentLists();
            RefreshDetail();
            RefreshCraftPlan();
        }

        private void DecomposeFromUI(EquipmentDTO eq)
        {
            if (eq == null) return;
            if (_lockedEquipment.Contains(eq.uid))
            {
                AddToast($"已锁定，不能分解：{eq.name}", ToastDuration);
                return;
            }
            AddToast($"已发送分解：{eq.name}", ToastDuration);
            _gameState.Decompose(eq.uid);
        }

        private VisualElement EquipmentRow(string slot, string label, System.Action select, System.Action primary, System.Action secondary, int rarity)
        {
            var row = Row();
            row.style.backgroundColor = new StyleColor(RarityRowBackground(rarity));
            row.style.borderLeftWidth = 5;
            row.style.borderLeftColor = RarityUIColor(rarity);
            row.style.borderTopWidth = 1;
            row.style.borderRightWidth = 1;
            row.style.borderBottomWidth = 1;
            row.style.borderTopColor = new StyleColor(RarityFrameColor(rarity));
            row.style.borderRightColor = new StyleColor(RarityFrameColor(rarity));
            row.style.borderBottomColor = new StyleColor(RarityFrameColor(rarity));
            row.style.paddingLeft = 8;
            row.style.paddingRight = 8;
            row.style.paddingTop = 6;
            row.style.paddingBottom = 6;
            row.style.marginBottom = 6;

            var slotLabel = Text(slot, 12, true);
            slotLabel.style.width = 58;
            slotLabel.style.color = new StyleColor(TextMuted);
            row.Add(slotLabel);

            var body = ActionButton(label, select ?? (() => { }));
            body.SetEnabled(select != null);
            body.style.flexGrow = 1;
            body.style.unityTextAlign = TextAnchor.MiddleLeft;
            body.style.backgroundColor = new StyleColor(new Color32(18, 18, 16, 255));
            body.style.borderLeftWidth = 0;
            row.Add(body);

            if (primary != null) row.Add(ActionButton(L10n.UIEquip, primary, 54, ButtonEquip));
            if (secondary != null) row.Add(ActionButton(primary == null ? L10n.UIUnequip : L10n.UIDecompose, secondary, 72, primary == null ? ButtonDefault : ButtonDanger));
            return row;
        }

        private void RefreshMaterials()
        {
            string text = L10n.UIMaterials;
            foreach (var kv in _gameState.Materials)
            {
                text += $"{MaterialName(kv.Key)}={kv.Value}  ";
            }
            _materialsText.text = text;
        }

        private void RefreshCraftPlan()
        {
            if (_craftPlanContent == null) return;
            _craftPlanContent.Clear();
            int weakCount = 0;
            foreach (var eq in EquipmentPresenter.BulkDecomposeCandidates(_gameState.Bag, _gameState.Equipped))
            {
                if (eq != null && !_lockedEquipment.Contains(eq.uid)) weakCount++;
            }
            var plan = EquipmentPresenter.BuildCraftPlan(
                _selected,
                BaseMaterialCount(),
                weakCount,
                EquipmentPresenter.EquipBestDelta(_gameState.Bag, _gameState.Equipped));

            _craftPlanContent.Add(PlanLine(plan.UpgradeLine, ButtonCraft));
            _craftPlanContent.Add(PlanLine(plan.ReforgeLine, ButtonReforge));
            _craftPlanContent.Add(PlanLine(plan.ComposeLine, BaseMaterialCount() >= 10 ? ButtonEquip : ButtonDefault));
            _craftPlanContent.Add(PlanLine(plan.CleanupLine, weakCount > 0 ? ButtonDanger : ButtonDefault));
        }

        private static Label PlanLine(string text, Color32 accent)
        {
            var label = Text(text, 13, true);
            label.style.marginBottom = 6;
            label.style.paddingLeft = 8;
            label.style.paddingRight = 8;
            label.style.paddingTop = 5;
            label.style.paddingBottom = 5;
            label.style.backgroundColor = new StyleColor(new Color32(13, 12, 10, 220));
            label.style.borderLeftWidth = 3;
            label.style.borderLeftColor = new StyleColor(accent);
            label.style.color = new StyleColor(TextMain);
            return label;
        }

        private void RefreshProgression()
        {
            if (_reincarnText == null || _talentsText == null)
            {
                RefreshTalentActions();
                return;
            }
            _reincarnText.text = string.Format(L10n.UISouls, _gameState.Souls, _gameState.MaxFloor, _gameState.CanReincarn);
            string[] desc = { L10n.TalentDamageDesc, L10n.TalentQualityDesc, L10n.TalentDropDesc, L10n.TalentOfflineDesc };
            string text = L10n.UITalentsLabel + "\n";
            for (int i = 0; i < TalentNames.Length; i++)
            {
                int lv = TalentLevel(TalentKeys[i]);
                text += EquipmentPresenter.BuildTalentLine(TalentNames[i], lv, TalentMax[i], desc[i], _gameState.Souls) + "\n";
            }
            _talentsText.text = text;
            RefreshReincarnPlan();
            RefreshTalentActions();
        }

        private void RefreshReincarnPlan()
        {
            if (_reincarnPlanContent == null) return;
            _reincarnPlanContent.Clear();
            var plan = EquipmentPresenter.BuildReincarnationPlan(
                _gameState.Floor,
                _gameState.Souls,
                _gameState.MaxFloor,
                _gameState.CanReincarn,
                RecommendedTalentName());
            _reincarnPlanContent.Add(PlanLine(plan.StatusLine, _gameState.CanReincarn ? ButtonAscend : ButtonDefault));
            _reincarnPlanContent.Add(PlanLine(plan.RewardLine, GoldText));
            _reincarnPlanContent.Add(PlanLine(plan.ResetLine, ButtonDanger));
            _reincarnPlanContent.Add(PlanLine(plan.NextTalentLine, ButtonEquip));
        }

        private void RefreshTalentActions()
        {
            if (_talentActions == null) return;
            _talentActions.Clear();
            for (int i = 0; i < TalentKeys.Length; i++)
            {
                int index = i;
                int level = TalentLevel(TalentKeys[index]);
                var button = ActionButton("升" + TalentNames[index], () => _gameState.TalentUp(TalentKeys[index]), 78, ButtonAscend);
                button.SetEnabled(_gameState.Souls > 0 && level < TalentMax[index]);
                _talentActions.Add(button);
            }
        }

        private int TalentLevel(string key)
        {
            return _gameState.Talents.ContainsKey(key) ? _gameState.Talents[key] : 0;
        }

        private string RecommendedTalentName()
        {
            if (TalentLevel("damage") < 3) return "伤害";
            if (TalentLevel("quality") < 1) return "品质";
            if (TalentLevel("drop") < 3) return "掉落";
            if (TalentLevel("offline_gain") < 1) return "离线";
            for (int i = 0; i < TalentKeys.Length; i++)
            {
                if (TalentLevel(TalentKeys[i]) < TalentMax[i]) return TalentNames[i];
            }
            return "";
        }

        private void RefreshDetail()
        {
            if (_selected == null)
            {
                RefreshDetailTextColumn(_selectedDetailContent, "选择一件装备查看属性和操作。", TextMuted);
                RefreshDetailTextColumn(_equippedDetailContent, "暂无对比对象。", TextMuted);
                RefreshComparisonRows(null, null);
                RefreshDetailActions();
                return;
            }
            EquipmentDTO current = EquippedAtSlot(_selected.slot);
            RefreshEquipmentDetailColumn(_selectedDetailContent, _selected, EquipmentPresenter.BuildSelectedSummary(_selected), RarityUIColor(_selected.rarity));
            RefreshEquipmentDetailColumn(_equippedDetailContent, current, EquipmentPresenter.BuildCurrentSummary(current), current != null ? RarityUIColor(current.rarity) : TextMuted);
            RefreshComparisonRows(_selected, current);
            RefreshDetailActions();
        }

        private void RefreshDetailTextColumn(VisualElement column, string text, Color color)
        {
            if (column == null) return;
            while (column.childCount > 1)
            {
                column.RemoveAt(1);
            }
            var label = Text(text, 13, false);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.color = new StyleColor(color);
            column.Add(label);
        }

        private void RefreshEquipmentDetailColumn(VisualElement column, EquipmentDTO eq, string text, Color color)
        {
            if (column == null) return;
            while (column.childCount > 1)
            {
                column.RemoveAt(1);
            }
            if (eq != null)
            {
                var icon = IconImage(IconForSlot(eq.slot), 64, 64);
                icon.style.marginBottom = 5;
                column.Add(icon);
            }
            var label = Text(text, 13, false);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.color = new StyleColor(color);
            column.Add(label);
        }

        private void RefreshComparisonRows(EquipmentDTO selected, EquipmentDTO current)
        {
            if (_compareDetailContent == null) return;
            while (_compareDetailContent.childCount > 1)
            {
                _compareDetailContent.RemoveAt(1);
            }

            var rows = EquipmentPresenter.BuildComparisonRows(selected, current);
            if (rows.Count == 0)
            {
                var empty = Text("选择装备后显示逐条差异。", 13, false);
                empty.style.color = new StyleColor(TextMuted);
                _compareDetailContent.Add(empty);
                return;
            }

            foreach (var row in rows)
            {
                _compareDetailContent.Add(ComparisonRow(row));
            }
        }

        private VisualElement ComparisonRow(EquipmentComparisonRow row)
        {
            var container = Row();
            container.style.marginBottom = 3;
            container.style.minHeight = 20;

            var name = Text(row.Label, 12, true);
            name.style.width = 64;
            name.style.color = new StyleColor(TextMuted);
            container.Add(name);

            var values = Text($"{row.CurrentValue} → {row.SelectedValue}", 12, false);
            values.style.flexGrow = 1;
            values.style.color = new StyleColor(TextMain);
            container.Add(values);

            var delta = Text(row.DeltaText, 12, true);
            delta.style.width = 58;
            delta.style.unityTextAlign = TextAnchor.MiddleRight;
            delta.style.color = new StyleColor(row.Delta > 0f ? new Color32(74, 222, 128, 255) : row.Delta < 0f ? new Color32(248, 113, 113, 255) : TextMuted);
            container.Add(delta);
            return container;
        }

        private void RefreshDetailActions()
        {
            if (_detailActions == null) return;
            _detailActions.Clear();

            bool isEquipped = IsSelectedEquipped();
            if (_selected != null && !isEquipped)
            {
                string lockLabel = _lockedEquipment.Contains(_selected.uid) ? "解锁" : "锁定";
                _detailActions.Add(ActionButton(lockLabel, () => ToggleLock(_selected), 84, ButtonDefault));
            }
            foreach (var action in EquipmentPresenter.DetailActions(_selected, isEquipped))
            {
                var captured = action;
                var button = ActionButton(captured.Label, () => RunDetailAction(captured.Id), 84, ButtonForAction(captured.Id));
                if (captured.Id == "decompose" && _selected != null && _lockedEquipment.Contains(_selected.uid))
                {
                    button.SetEnabled(false);
                }
                _detailActions.Add(button);
            }
        }

        private void RunDetailAction(string id)
        {
            if (_selected == null) return;
            switch (id)
            {
                case "equip":
                    _gameState.Equip(_selected.uid);
                    break;
                case "unequip":
                    _gameState.Unequip(_selected.slot);
                    break;
                case "upgrade":
                    TrackPendingCraft("强化", _selected);
                    AddToast($"已发送强化：{_selected.name}", ToastDuration);
                    _gameState.Upgrade(_selected.uid);
                    break;
                case "reforge":
                    TrackPendingCraft("重铸", _selected);
                    AddToast($"已发送重铸：{_selected.name}", ToastDuration);
                    _gameState.Reforge(_selected.uid);
                    break;
                case "decompose":
                    DecomposeFromUI(_selected);
                    break;
            }
        }

        private void TrackPendingCraft(string action, EquipmentDTO eq)
        {
            if (eq == null) return;
            _pendingCraftUid = eq.uid;
            _pendingCraftAction = action;
            _pendingCraftScore = EquipmentPresenter.Score(eq);
        }

        private void TryReportPendingCraftScoreChange()
        {
            if (string.IsNullOrEmpty(_pendingCraftUid)) return;
            EquipmentDTO updated = FindEquipmentByUid(_pendingCraftUid);
            if (updated == null) return;

            float delta = EquipmentPresenter.Score(updated) - _pendingCraftScore;
            if (Mathf.Abs(delta) < 0.1f) return;
            AddToast($"{_pendingCraftAction}完成：评分 {delta:+0;-0;0}", ToastDuration + 1f);
            ClearPendingCraft();
        }

        private void ClearPendingCraft()
        {
            _pendingCraftUid = null;
            _pendingCraftAction = null;
            _pendingCraftScore = 0f;
        }

        private EquipmentDTO FindEquipmentByUid(string uid)
        {
            foreach (var eq in _gameState.Bag)
            {
                if (eq.uid == uid) return eq;
            }
            foreach (var eq in _gameState.Equipped)
            {
                if (eq.uid == uid) return eq;
            }
            return null;
        }

        private bool IsLootUpgrade(EquipmentDTO eq)
        {
            if (eq == null) return false;
            return EquipmentPresenter.Score(eq) > EquipmentPresenter.Score(EquippedAtSlot(eq.slot));
        }

        private void Select(EquipmentDTO eq)
        {
            _selected = eq;
            RefreshDetail();
            RefreshCraftPlan();
        }

        private void KeepSelectedIfPresent()
        {
            if (_selected == null) return;
            foreach (var eq in _gameState.Bag)
            {
                if (eq.uid == _selected.uid)
                {
                    _selected = eq;
                    return;
                }
            }
            foreach (var eq in _gameState.Equipped)
            {
                if (eq.uid == _selected.uid)
                {
                    _selected = eq;
                    return;
                }
            }
            _selected = null;
        }

        private bool IsSelectedEquipped()
        {
            if (_selected == null) return false;
            foreach (var eq in _gameState.Equipped)
            {
                if (eq.uid == _selected.uid) return true;
            }
            return false;
        }

        private void EquipBestBySlot()
        {
            int equippedCount = 0;
            float expectedDelta = EquipmentPresenter.EquipBestDelta(_gameState.Bag, _gameState.Equipped);
            for (int slot = 0; slot < 8; slot++)
            {
                EquipmentDTO current = EquippedAtSlot(slot);
                EquipmentDTO best = null;
                float bestScore = current != null ? EquipmentPresenter.Score(current) : -1f;
                foreach (var eq in _gameState.Bag)
                {
                    if (eq.slot != slot) continue;
                    float score = EquipmentPresenter.Score(eq);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = eq;
                    }
                }
                if (best != null)
                {
                    _gameState.Equip(best.uid);
                    equippedCount++;
                }
            }
            if (equippedCount > 0) AddToast($"{string.Format(L10n.UIEquipBestDone, equippedCount)}  评分 +{expectedDelta:F0}", ToastDuration);
        }

        private void DecomposeWeakItems()
        {
            int count = 0;
            foreach (var eq in EquipmentPresenter.BulkDecomposeCandidates(_gameState.Bag, _gameState.Equipped))
            {
                if (_lockedEquipment.Contains(eq.uid)) continue;
                _gameState.Decompose(eq.uid);
                count++;
            }
            if (count > 0) AddToast(string.Format(L10n.UIDecomposeWeakDone, count), ToastDuration);
            else AddToast("没有可分解的未锁定弱装。", ToastDuration);
        }

        private void PruneLockedEquipment()
        {
            if (_lockedEquipment.Count == 0) return;
            var existing = new HashSet<string>();
            foreach (var eq in _gameState.Bag)
            {
                if (eq != null) existing.Add(eq.uid);
            }
            foreach (var eq in _gameState.Equipped)
            {
                if (eq != null) existing.Add(eq.uid);
            }
            _lockedEquipment.RemoveWhere(uid => !existing.Contains(uid));
        }

        private EquipmentDTO EquippedAtSlot(int slot)
        {
            foreach (var eq in _gameState.Equipped)
            {
                if (eq.slot == slot) return eq;
            }
            return null;
        }

        private static string MaterialName(string key)
        {
            switch (key)
            {
                case "base_mat": return "基础材料";
                case "affix_mat_1": return "词缀材料1";
                case "affix_mat_2": return "词缀材料2";
                case "affix_mat_3": return "词缀材料3";
                case "affix_mat_4": return "词缀材料4";
                case "affix_mat_5": return "词缀材料5";
                default: return key;
            }
        }

        private string MaterialSummary()
        {
            if (_gameState.Materials.Count == 0) return "材料：暂无";
            string text = "材料：";
            foreach (var kv in _gameState.Materials)
            {
                text += $"{MaterialName(kv.Key)} {kv.Value}  ";
            }
            return text;
        }

        private void AddToast(string text, float duration)
        {
            _toasts.Add(new Toast { Text = text, ExpireAt = Time.realtimeSinceStartup + duration });
            if (_toasts.Count > 5) _toasts.RemoveAt(0);
            PruneToasts();
        }

        private void PruneToasts()
        {
            float now = Time.realtimeSinceStartup;
            for (int i = _toasts.Count - 1; i >= 0; i--)
            {
                if (now >= _toasts[i].ExpireAt) _toasts.RemoveAt(i);
            }
            if (_toastText == null) return;
            string text = "";
            foreach (var toast in _toasts) text += toast.Text + "\n";
            _toastText.text = text;
        }

        private static VisualElement Panel(string name)
        {
            var el = new VisualElement { name = name };
            el.style.backgroundColor = new StyleColor(PanelBg);
            el.style.overflow = Overflow.Hidden;
            el.style.borderTopWidth = 1;
            el.style.borderRightWidth = 1;
            el.style.borderBottomWidth = 1;
            el.style.borderLeftWidth = 1;
            el.style.borderTopColor = new StyleColor(PanelBorder);
            el.style.borderRightColor = new StyleColor(PanelBorder);
            el.style.borderBottomColor = new StyleColor(new Color32(42, 34, 24, 255));
            el.style.borderLeftColor = new StyleColor(PanelBorder);
            el.style.borderTopLeftRadius = 4;
            el.style.borderTopRightRadius = 4;
            el.style.borderBottomLeftRadius = 4;
            el.style.borderBottomRightRadius = 4;
            el.style.paddingLeft = 10;
            el.style.paddingRight = 10;
            el.style.paddingTop = 10;
            el.style.paddingBottom = 10;
            return el;
        }

        private static VisualElement Column(float width)
        {
            var el = new VisualElement();
            el.style.width = width;
            el.style.flexDirection = FlexDirection.Column;
            el.style.marginRight = 12;
            return el;
        }

        private static VisualElement Row()
        {
            var el = new VisualElement();
            el.style.flexDirection = FlexDirection.Row;
            el.style.alignItems = Align.Center;
            return el;
        }

        private static Label Text(string value, int size, bool bold)
        {
            var label = new Label(value);
            label.style.fontSize = size;
            label.style.color = new StyleColor(TextMain);
            label.style.whiteSpace = WhiteSpace.Normal;
            if (bold) label.style.unityFontStyleAndWeight = FontStyle.Bold;
            return label;
        }

        private static Label SectionTitle(string value)
        {
            var label = Text(value, 18, true);
            label.style.color = new StyleColor(new Color32(244, 202, 121, 255));
            label.style.marginBottom = 6;
            return label;
        }

        private static Button ActionButton(string label, System.Action action, float width = -1)
        {
            return ActionButton(label, action, width, ButtonDefault);
        }

        private static Button ActionButton(string label, System.Action action, float width, Color32 color)
        {
            var button = new Button(action) { text = label };
            button.style.height = 38;
            button.style.backgroundColor = new StyleColor(color);
            button.style.color = new StyleColor(new Color32(255, 247, 237, 255));
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.borderTopWidth = 2;
            button.style.borderRightWidth = 2;
            button.style.borderBottomWidth = 2;
            button.style.borderLeftWidth = 2;
            button.style.borderTopColor = new StyleColor(new Color32(218, 156, 63, 255));
            button.style.borderRightColor = new StyleColor(new Color32(70, 48, 31, 255));
            button.style.borderBottomColor = new StyleColor(new Color32(37, 25, 18, 255));
            button.style.borderLeftColor = new StyleColor(new Color32(218, 156, 63, 255));
            button.style.borderTopLeftRadius = 3;
            button.style.borderTopRightRadius = 3;
            button.style.borderBottomLeftRadius = 3;
            button.style.borderBottomRightRadius = 3;
            button.style.marginLeft = 4;
            button.style.marginRight = 4;
            button.style.marginTop = 3;
            button.style.marginBottom = 3;
            if (width > 0) button.style.width = width;
            return button;
        }

        private static Color32 ButtonForAction(string id)
        {
            switch (id)
            {
                case "equip": return ButtonEquip;
                case "upgrade": return ButtonCraft;
                case "reforge": return ButtonReforge;
                case "decompose": return ButtonDanger;
                case "unequip": return ButtonDefault;
                default: return ButtonDefault;
            }
        }

        private static Color32 RarityRowBackground(int r)
        {
            switch (r)
            {
                case 1: return new Color32(16, 34, 42, 255);
                case 2: return new Color32(48, 40, 16, 255);
                case 3: return new Color32(58, 31, 13, 255);
                case 4: return new Color32(59, 18, 18, 255);
                default: return new Color32(29, 29, 26, 255);
            }
        }

        private static Color32 RarityFrameColor(int r)
        {
            switch (r)
            {
                case 1: return new Color32(28, 88, 110, 255);
                case 2: return new Color32(122, 95, 22, 255);
                case 3: return new Color32(142, 75, 25, 255);
                case 4: return new Color32(142, 40, 40, 255);
                default: return new Color32(58, 54, 47, 255);
            }
        }

        private static Color32 RarityGlowColor(int r)
        {
            switch (r)
            {
                case 1: return new Color32(83, 178, 255, 255);
                case 2: return new Color32(255, 173, 63, 255);
                case 3: return new Color32(255, 111, 37, 255);
                case 4: return new Color32(255, 96, 207, 255);
                default: return new Color32(126, 111, 88, 255);
            }
        }

        private static Color32 RarityTileBackground(int r)
        {
            switch (r)
            {
                case 1: return new Color32(14, 29, 43, 245);
                case 2: return new Color32(42, 25, 9, 245);
                case 3: return new Color32(55, 20, 8, 245);
                case 4: return new Color32(46, 18, 44, 245);
                default: return new Color32(17, 15, 12, 245);
            }
        }

        private static Color RarityUIColor(int r)
        {
            switch (r)
            {
                case 0: return new Color32(148, 163, 184, 255);
                case 1: return new Color32(56, 189, 248, 255);
                case 2: return new Color32(250, 204, 21, 255);
                case 3: return new Color32(251, 146, 60, 255);
                case 4: return new Color32(248, 113, 113, 255);
                default: return Color.white;
            }
        }
    }
}
