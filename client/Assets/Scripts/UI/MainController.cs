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
        private VisualElement _materialsContent;
        private VisualElement _reincarnPlanContent;
        private VisualElement _dungeonPanel;
        private VisualElement _lootPanel;
        private VisualElement _bossProgressFill;
        private VisualElement _stageHeroHealthFill;
        private VisualElement _stageMonsterHealthFill;
        private Label _stageSlash;
        private Label _stageImpactText;
        private Label _stageArtifactText;
        private Label _stageBannerText;
        private Image _stageHeroSpriteImage;
        private Image _stageBossSpriteImage;
        private Texture2D _battleBackground;
        private Texture2D _heroSprite;
        private Texture2D[] _heroAttackFrames;
        private Texture2D _bossSprite;
        private Texture2D _minionSprite;
        private Texture2D _guardianSprite;
        private Texture2D _craftIcon;
        private Texture2D[] _slotIcons;
        private EquipmentDTO _selected;
        private MainTab _activeTab = MainTab.Battle;
        private EquipmentBagFilter _bagFilter = EquipmentBagFilter.All;
        private float _prevPower;
        private bool _prevCanReincarn;
        private float _battlePulseUntil;
        private float _combatBeatStartedAt = -10f;
        private float _lootPulseUntil;
        private float _stageBannerUntil;
        private float _bossProgressTarget;
        private float _bossProgressCurrent;
        private string _pendingCraftUid;
        private string _pendingCraftAction;
        private float _pendingCraftScore;
        private CombatData _activeCombat;
        private bool _combatTransitionPending;


        private readonly List<EquipmentDTO> _lootFeed = new List<EquipmentDTO>();
        private readonly HashSet<string> _lockedEquipment = new HashSet<string>();
        private readonly Dictionary<string, Texture2D> _equipmentIconCache = new Dictionary<string, Texture2D>();
        private const float ToastDuration = 3f;
        private const float CombatBeatDuration = 0.55f;

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
            _gameState.OnCombatReceived += OnCombat;
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
            _gameState.OnCombatReceived -= OnCombat;
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
            bool hasPreviousPower = _prevPower > 0f;
            float delta = power - _prevPower;
            _prevPower = power;
            if (hasPreviousPower && delta > 0.5f)
                _powerText.text = $"{power:F1} +{delta:F1}";
            else if (hasPreviousPower && delta < -0.5f)
                _powerText.text = $"{power:F1} {delta:F1}";
            else
                _powerText.text = power.ToString("F1");
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

        private void OnCombat(CombatData combat)
        {
            _activeCombat = combat;
            StartCombatBeat();
            if (combat.win && combat.enemy_kind == "minion" && combat.minions_killed >= combat.minions_total)
            {
                _combatTransitionPending = true;
                ShowStageBanner(combat.floor % 5 == 0 ? "小兵清剿完成 · Boss 现身" : "小兵清剿完成 · 守关精英现身", new Color32(255, 202, 112, 255));
            }
            if (combat.floor_advanced) _combatTransitionPending = true;
            RefreshDungeon();
        }

        private void OnFloor(int newFloor)
        {
            if (_activeCombat != null) _combatTransitionPending = true;
            if (newFloor > 1 && (newFloor - 1) % 5 == 0)
            {
                string banner = EquipmentPresenter.BuildBossClearBanner(newFloor - 1);
                if (!string.IsNullOrEmpty(banner)) ShowStageBanner(banner, new Color32(255, 92, 64, 255));
                AddToast($"击败 Boss：第 {newFloor - 1} 层", ToastDuration);
            }
            RefreshHeader();
            RefreshStuck();
            RefreshDungeon();
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
                AddToast($"{_pendingCraftAction}完成：战力贡献未变化", ToastDuration);
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
            _battleBackground = Resources.Load<Texture2D>("UI/dungeon-battle-bg-v2");
            _heroSprite = Resources.Load<Texture2D>("UI/hero-combat-sprite-v2");
            _heroAttackFrames = new[]
            {
                Resources.Load<Texture2D>("UI/hero-combat-attack-0"),
                Resources.Load<Texture2D>("UI/hero-combat-attack-1"),
                Resources.Load<Texture2D>("UI/hero-combat-attack-2"),
                Resources.Load<Texture2D>("UI/hero-combat-attack-3"),
            };
            _bossSprite = Resources.Load<Texture2D>("UI/boss-combat-sprite-v2");
            _minionSprite = Resources.Load<Texture2D>("UI/minion-combat-sprite");
            _guardianSprite = Resources.Load<Texture2D>("UI/guardian-combat-sprite");
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

            if (_battleBackground != null)
            {
                var shellBackground = new Image { image = _battleBackground, scaleMode = ScaleMode.ScaleAndCrop };
                shellBackground.style.position = Position.Absolute;
                shellBackground.style.left = 0;
                shellBackground.style.right = 0;
                shellBackground.style.top = 0;
                shellBackground.style.bottom = 0;
                shellBackground.style.opacity = 0.16f;
                root.Add(shellBackground);
            }

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            scroll.style.width = Length.Percent(100);
            scroll.contentContainer.style.alignItems = Align.Center;
            scroll.contentContainer.style.flexGrow = 1;
            root.Add(scroll);

            var mobile = new VisualElement { name = "mobile-frame" };
            mobile.style.width = Length.Percent(100);
            mobile.style.maxWidth = 945;
            mobile.style.flexGrow = 1;
            mobile.style.flexDirection = FlexDirection.Column;
            mobile.style.paddingLeft = 12;
            mobile.style.paddingRight = 12;
            mobile.style.paddingTop = 8;
            mobile.style.paddingBottom = 8;
            scroll.Add(mobile);

            BuildTopHud(mobile);

            BuildTabbedContent(mobile);

            BuildBottomBar(root);

            BuildOfflinePanel(root);
        }

        private void BuildTabbedContent(VisualElement root)
        {
            _battleTabContent = new VisualElement();
            _battleTabContent.style.flexGrow = 1;
            _battleTabContent.style.flexDirection = FlexDirection.Column;
            root.Add(_battleTabContent);
            BuildDungeonPanel(_battleTabContent);
            BuildMobileStatusCards(_battleTabContent);

            _bagTabContent = new VisualElement();
            _bagTabContent.style.flexGrow = 1;
            _bagTabContent.style.flexDirection = FlexDirection.Column;
            root.Add(_bagTabContent);
            BuildBagPanel(_bagTabContent);
            BuildMobileDetailPanel(_bagTabContent);

            _craftTabContent = new VisualElement();
            _craftTabContent.style.flexGrow = 1;
            _craftTabContent.style.flexDirection = FlexDirection.Column;
            root.Add(_craftTabContent);
            BuildCraftPanel(_craftTabContent);

            _talentTabContent = new VisualElement();
            _talentTabContent.style.flexGrow = 1;
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

            var titleCol = Column(110);
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
            _statusText.style.fontSize = 11;
            _statusText.style.whiteSpace = WhiteSpace.NoWrap;
            _statusText.style.unityTextAlign = TextAnchor.MiddleCenter;
            titleCol.Add(_statusText);
            header.Add(titleCol);

            _floorText = HudCell(header, "层数", "1", 92);
            _powerText = HudCell(header, "战力", "0", 142);
            _powerText.style.fontSize = 16;
            _powerText.style.whiteSpace = WhiteSpace.NoWrap;
            _soulsText = HudCell(header, "魂点", "0", 112);
            _syncText = HudCell(header, "账号", "hero", 122);
            _syncText.style.fontSize = 15;
            _syncText.style.whiteSpace = WhiteSpace.NoWrap;
            _powerText.style.color = new StyleColor(new Color32(255, 178, 83, 255));

            var loginCol = Row();
            loginCol.style.flexGrow = 1;
            loginCol.style.justifyContent = Justify.FlexEnd;
            _accountInput = new TextField { value = "hero" };
            _accountInput.style.width = 118;
            _accountInput.style.height = 34;
            _accountInput.style.marginRight = 4;
            _accountInput.style.color = new StyleColor(TextMain);
            _accountInput.style.backgroundColor = new StyleColor(new Color32(18, 15, 12, 255));
            _accountInput.style.borderTopWidth = 1;
            _accountInput.style.borderRightWidth = 1;
            _accountInput.style.borderBottomWidth = 1;
            _accountInput.style.borderLeftWidth = 1;
            _accountInput.style.borderTopColor = new StyleColor(PanelBorder);
            _accountInput.style.borderRightColor = new StyleColor(PanelBorder);
            _accountInput.style.borderBottomColor = new StyleColor(PanelBorderHot);
            _accountInput.style.borderLeftColor = new StyleColor(PanelBorder);
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
            bottom.style.width = Length.Percent(100);
            bottom.style.maxWidth = 921;
            bottom.style.flexShrink = 0;
            bottom.style.marginTop = 8;
            bottom.style.flexDirection = FlexDirection.Column;
            root.Add(bottom);

            var log = new VisualElement();
            log.style.height = 24;
            log.style.paddingLeft = 8;
            log.style.paddingRight = 8;
            log.style.justifyContent = Justify.Center;
            log.style.overflow = Overflow.Hidden;
            _toastText = Text("", 13, false);
            _toastText.style.color = new StyleColor(new Color32(244, 202, 121, 255));
            _toastText.style.whiteSpace = WhiteSpace.NoWrap;
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
            _syncText.text = CompactAccount(_gameState.Account);
            if (_floorText != null) _floorText.text = $"{_gameState.Floor} 层";
            if (_soulsText != null) _soulsText.text = _gameState.Souls.ToString();
            _powerText.text = _gameState.Power.ToString("F1");
            RefreshDungeon();
        }

        private static string CompactAccount(string account)
        {
            if (string.IsNullOrEmpty(account)) return "hero";
            return account.Length <= 10 ? account : account.Substring(0, 8) + "...";
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


    }
}
