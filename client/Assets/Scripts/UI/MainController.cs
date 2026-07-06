using EquipmentIdle.Data;
using EquipmentIdle.Net;
using EquipmentIdle.State;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace EquipmentIdle.UI
{
    public partial class MainController : MonoBehaviour
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
            SyncLockedEquipment(bag, equipped);
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

        private int BaseMaterialCount()
        {
            return _gameState.Materials.TryGetValue("base_mat", out var value) ? value : 0;
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
