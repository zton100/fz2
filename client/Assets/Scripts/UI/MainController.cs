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
        private Label _detailText;
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
        private Label _toastText;
        private Label _offlineText;
        private VisualElement _offlinePanel;
        private VisualElement _equippedContent;
        private VisualElement _bagContent;
        private VisualElement _detailActions;
        private VisualElement _talentActions;
        private VisualElement _dungeonPanel;
        private VisualElement _lootPanel;
        private VisualElement _bossProgressFill;
        private VisualElement _stageHeroHealthFill;
        private VisualElement _stageMonsterHealthFill;
        private VisualElement _stageSlash;
        private EquipmentDTO _selected;
        private float _prevPower;
        private bool _prevCanReincarn;
        private float _battlePulseUntil;
        private float _lootPulseUntil;
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
        private const float ToastDuration = 3f;
        private static readonly string[] TalentKeys = { "damage", "quality", "drop", "offline_gain" };
        private static readonly string[] TalentNames = { "伤害", "品质", "掉落", "离线" };
        private static readonly int[] TalentMax = { 10, 3, 10, 5 };
        private static readonly Color32 RootBg = new Color32(8, 10, 10, 255);
        private static readonly Color32 PanelBg = new Color32(20, 21, 19, 255);
        private static readonly Color32 PanelBgWarm = new Color32(30, 25, 20, 255);
        private static readonly Color32 PanelBorder = new Color32(83, 70, 49, 255);
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
            RefreshDungeon();
        }

        private void OnPower(float power)
        {
            float delta = power - _prevPower;
            _prevPower = power;
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
            if (IsLootUpgrade(eq))
            {
                Select(eq);
                AddToast("发现提升装备：" + lootToast, ToastDuration + 1f);
                return;
            }

            AddToast(lootToast, ToastDuration);
        }

        private void OnFloor(int newFloor)
        {
            _battlePulseUntil = Time.realtimeSinceStartup + 0.7f;
            if (newFloor > 1 && (newFloor - 1) % 5 == 0)
            {
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
            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(945, 1672);

            var doc = gameObject.AddComponent<UIDocument>();
            doc.panelSettings = panelSettings;

            var root = doc.rootVisualElement;
            root.style.flexGrow = 1;
            root.style.backgroundColor = new StyleColor(RootBg);
            root.style.paddingLeft = 16;
            root.style.paddingRight = 16;
            root.style.paddingTop = 12;
            root.style.paddingBottom = 12;

            BuildTopHud(root);

            BuildDungeonPanel(root);

            BuildMobileStatusCards(root);
            BuildMobileFeatureCards(root);
            BuildMobileDetailPanel(root);

            BuildBottomBar(root);

            BuildOfflinePanel(root);
        }

        private void BuildTopHud(VisualElement root)
        {
            var header = Panel("top-hud");
            header.style.height = 108;
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.marginBottom = 10;
            header.style.paddingTop = 6;
            header.style.paddingBottom = 6;
            root.Add(header);

            var titleCol = Column(180);
            var title = Text("装备放置", 28, true);
            title.style.color = new StyleColor(new Color32(244, 202, 121, 255));
            titleCol.Add(title);
            _statusText = Text("", 13, true);
            titleCol.Add(_statusText);
            header.Add(titleCol);

            _floorText = HudCell(header, "层数", "1", 110);
            _powerText = HudCell(header, "战力", "0", 170);
            _soulsText = HudCell(header, "魂点", "0", 145);
            _syncText = HudCell(header, "账号", "hero", 160);
            _powerText.style.color = new StyleColor(new Color32(255, 171, 64, 255));

            var loginCol = Row();
            loginCol.style.flexGrow = 1;
            loginCol.style.justifyContent = Justify.FlexEnd;
            _accountInput = new TextField { value = "hero" };
            _accountInput.style.width = 170;
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
            cell.style.paddingTop = 4;
            cell.style.paddingBottom = 4;
            cell.style.backgroundColor = new StyleColor(new Color32(13, 14, 13, 255));
            cell.style.borderBottomWidth = 1;
            cell.style.borderBottomColor = new StyleColor(PanelBorder);

            var labelText = Text(label, 11, true);
            labelText.style.color = new StyleColor(TextMuted);
            var valueText = Text(value, 18, true);
            cell.Add(labelText);
            cell.Add(valueText);
            parent.Add(cell);
            return valueText;
        }

        private void BuildBottomBar(VisualElement root)
        {
            var bottom = Panel("bottom-bar");
            bottom.style.height = 112;
            bottom.style.marginTop = 10;
            bottom.style.flexDirection = FlexDirection.Column;
            root.Add(bottom);

            var log = new VisualElement();
            log.style.height = 38;
            log.style.paddingLeft = 8;
            log.style.paddingRight = 8;
            log.style.justifyContent = Justify.Center;
            _toastText = Text("", 13, false);
            _toastText.style.color = new StyleColor(new Color32(244, 202, 121, 255));
            log.Add(_toastText);
            bottom.Add(log);

            var nav = Row();
            nav.style.flexGrow = 1;
            nav.style.alignItems = Align.Stretch;
            bottom.Add(nav);

            nav.Add(NavButton("战斗", () => AddToast("自动战斗中，击败怪物推进层数。", ToastDuration), ButtonCraft));
            nav.Add(NavButton("背包", () => AddToast($"背包：{_gameState.Bag.Count}/120", ToastDuration), ButtonDefault));
            nav.Add(NavButton("锻造", () => AddToast("锻造：选择装备后可强化、重铸、分解。", ToastDuration), ButtonDanger));
            nav.Add(NavButton("天赋", () => AddToast(_gameState.CanReincarn ? "可以转生，领取魂点提升天赋。" : "第 10 层后可转生。", ToastDuration), ButtonAscend));
        }

        private static Button NavButton(string label, System.Action action, Color32 color)
        {
            var button = ActionButton(label, action, -1, color);
            button.style.flexGrow = 1;
            button.style.height = Length.Percent(100);
            button.style.fontSize = 20;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.marginLeft = 2;
            button.style.marginRight = 2;
            return button;
        }

        private void BuildDungeonPanel(VisualElement root)
        {
            var dungeon = Panel("dungeon");
            _dungeonPanel = dungeon;
            dungeon.style.height = 560;
            dungeon.style.marginBottom = 10;
            dungeon.style.flexDirection = FlexDirection.Column;
            dungeon.style.backgroundColor = new StyleColor(PanelBgWarm);
            root.Add(dungeon);

            var bossRow = Row();
            bossRow.style.marginBottom = 8;
            _dungeonTitleText = Text("", 24, true);
            _dungeonTitleText.style.flexGrow = 1;
            bossRow.Add(_dungeonTitleText);
            _monsterText = Text("", 16, true);
            _monsterText.style.unityTextAlign = TextAnchor.MiddleRight;
            bossRow.Add(_monsterText);
            dungeon.Add(bossRow);

            var progressFrame = new VisualElement();
            progressFrame.style.height = 26;
            progressFrame.style.marginBottom = 12;
            progressFrame.style.backgroundColor = new StyleColor(new Color32(12, 16, 14, 255));
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
            combatFooter.style.marginTop = 10;
            combatFooter.style.alignItems = Align.FlexStart;
            _battleText = Text("", 16, true);
            _stuckText = Text("", 13, false);
            _stuckText.style.color = new StyleColor(new Color32(248, 113, 113, 255));
            _goalText = Text("", 13, false);
            _bossHintText = Text("", 13, true);
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
            row.style.height = 150;
            row.style.marginBottom = 10;
            root.Add(row);

            _objectiveCardText = StatusCard(row, "当前目标", "连接后开始自动战斗。");
            _bossProgressCardText = StatusCard(row, "BOSS 进度", "每 5 层出现 Boss。");
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

        private void BuildMobileFeatureCards(VisualElement root)
        {
            var row = Row();
            row.style.height = 210;
            row.style.marginBottom = 10;
            root.Add(row);

            var equipped = Panel("mobile-equipped");
            equipped.style.flexGrow = 1.35f;
            equipped.style.marginRight = 10;
            equipped.Add(SectionTitle("穿戴装备"));
            var equippedScroll = new ScrollView();
            equippedScroll.style.flexGrow = 1;
            _equippedContent = equippedScroll.contentContainer;
            equipped.Add(equippedScroll);
            row.Add(equipped);

            var bag = Panel("mobile-bag");
            bag.style.flexGrow = 0.9f;
            bag.style.marginRight = 10;
            bag.Add(SectionTitle("背包"));
            var bagScroll = new ScrollView();
            bagScroll.style.flexGrow = 1;
            _bagContent = bagScroll.contentContainer;
            bag.Add(bagScroll);
            row.Add(bag);

            var craft = Panel("mobile-craft");
            craft.style.flexGrow = 0.9f;
            craft.style.marginRight = 10;
            craft.Add(SectionTitle("强化"));
            craft.Add(Text("选择装备后可强化", 14, false));
            craft.Add(ActionButton(L10n.UIEquipBest, EquipBestBySlot, -1, ButtonEquip));
            craft.Add(ActionButton(L10n.UIDecomposeWeak, DecomposeWeakItems, -1, ButtonDanger));
            row.Add(craft);

            var mats = Panel("mobile-materials");
            mats.style.flexGrow = 0.9f;
            mats.Add(SectionTitle("材料摘要"));
            _materialsText = Text("", 14, false);
            _materialsText.style.flexGrow = 1;
            mats.Add(_materialsText);
            row.Add(mats);
        }

        private void BuildMobileDetailPanel(VisualElement root)
        {
            var detail = Panel("mobile-detail");
            detail.style.flexGrow = 1;
            detail.style.marginBottom = 0;
            root.Add(detail);

            detail.Add(SectionTitle("装备详情"));
            _detailText = Text("", 18, false);
            _detailText.style.flexGrow = 1;
            detail.Add(_detailText);

            _detailActions = new VisualElement();
            _detailActions.style.flexDirection = FlexDirection.Row;
            _detailActions.style.flexWrap = Wrap.Wrap;
            _detailActions.style.marginTop = 8;
            _detailActions.style.marginBottom = 8;
            detail.Add(_detailActions);

            var composeGrid = new VisualElement();
            composeGrid.style.flexDirection = FlexDirection.Row;
            composeGrid.style.flexWrap = Wrap.Wrap;
            composeGrid.style.marginBottom = 8;
            for (int s = 0; s < 8; s++)
            {
                int slot = s;
                composeGrid.Add(ActionButton(L10n.SlotName(slot), () => _gameState.Compose(slot), 100, ButtonCraft));
            }
            detail.Add(composeGrid);

            _reincarnText = Text("", 13, false);
            detail.Add(_reincarnText);
            detail.Add(ActionButton(L10n.UIReincarnate, () =>
            {
                if (_gameState.CanReincarn) _gameState.Reincarn();
            }, -1, ButtonAscend));
            _talentsText = Text("", 13, false);
            detail.Add(_talentsText);
            _talentActions = new VisualElement();
            _talentActions.style.flexDirection = FlexDirection.Row;
            _talentActions.style.flexWrap = Wrap.Wrap;
            _talentActions.style.marginTop = 4;
            detail.Add(_talentActions);
        }

        private VisualElement BuildBattleStage()
        {
            var stage = new VisualElement();
            stage.style.flexGrow = 1;
            stage.style.marginTop = 4;
            stage.style.paddingLeft = 8;
            stage.style.paddingRight = 8;
            stage.style.paddingTop = 8;
            stage.style.paddingBottom = 8;
            stage.style.backgroundColor = new StyleColor(new Color32(15, 17, 16, 255));
            stage.style.borderTopWidth = 1;
            stage.style.borderRightWidth = 1;
            stage.style.borderBottomWidth = 1;
            stage.style.borderLeftWidth = 1;
            stage.style.borderTopColor = new StyleColor(new Color32(55, 47, 34, 255));
            stage.style.borderRightColor = new StyleColor(new Color32(55, 47, 34, 255));
            stage.style.borderBottomColor = new StyleColor(new Color32(55, 47, 34, 255));
            stage.style.borderLeftColor = new StyleColor(new Color32(55, 47, 34, 255));
            stage.style.borderTopLeftRadius = 6;
            stage.style.borderTopRightRadius = 6;
            stage.style.borderBottomLeftRadius = 6;
            stage.style.borderBottomRightRadius = 6;

            _stageZoneText = Text("", 13, true);
            _stageZoneText.style.unityTextAlign = TextAnchor.MiddleCenter;
            _stageZoneText.style.marginBottom = 6;
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
            stage.Add(_stageStatusText);
            return stage;
        }

        private static VisualElement CombatantCard(Color accent, out Label name, out Label power, out VisualElement healthFill)
        {
            var card = new VisualElement();
            card.style.flexGrow = 1;
            card.style.paddingLeft = 8;
            card.style.paddingRight = 8;
            card.style.paddingTop = 6;
            card.style.paddingBottom = 6;
            card.style.backgroundColor = new StyleColor(new Color32(25, 24, 21, 255));
            card.style.borderBottomWidth = 3;
            card.style.borderBottomColor = accent;
            card.style.borderTopLeftRadius = 5;
            card.style.borderTopRightRadius = 5;
            card.style.borderBottomLeftRadius = 5;
            card.style.borderBottomRightRadius = 5;

            name = Text("", 16, true);
            name.style.unityTextAlign = TextAnchor.MiddleCenter;
            power = Text("", 12, false);
            power.style.unityTextAlign = TextAnchor.MiddleCenter;
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

            var label = Text(EquipmentPresenter.BuildLootLine(eq), 12, eq != null && eq.rarity >= 2);
            label.style.color = RarityUIColor(eq != null ? eq.rarity : 0);
            label.style.flexGrow = 1;
            row.Add(label);
            return row;
        }

        private void UpdateCombatFeedback()
        {
            if (_bossProgressFill != null)
            {
                _bossProgressCurrent = Mathf.Lerp(_bossProgressCurrent, _bossProgressTarget, Time.deltaTime * 8f);
                _bossProgressFill.style.width = Length.Percent(_bossProgressCurrent * 100f);
            }

            if (_dungeonPanel != null)
            {
                float pulse = Mathf.Clamp01((_battlePulseUntil - Time.realtimeSinceStartup) / 0.7f);
                Color baseColor = PanelBgWarm;
                Color hitColor = new Color32(94, 47, 25, 255);
                _dungeonPanel.style.backgroundColor = new StyleColor(Color.Lerp(baseColor, hitColor, pulse));
                if (_stageSlash != null) _stageSlash.style.opacity = 0.65f + pulse * 0.35f;
            }

            if (_lootPanel != null)
            {
                float pulse = Mathf.Clamp01((_lootPulseUntil - Time.realtimeSinceStartup) / 0.8f);
                Color baseColor = Color.clear;
                Color lootColor = new Color(0.75f, 0.50f, 0.12f, 0.35f);
                _lootPanel.style.backgroundColor = new StyleColor(Color.Lerp(baseColor, lootColor, pulse));
            }
        }

        private void RefreshEquipmentLists()
        {
            _equippedContent.Clear();
            for (int slot = 0; slot < 8; slot++)
            {
                int capturedSlot = slot;
                EquipmentDTO eq = EquippedAtSlot(slot);
                if (eq == null)
                {
                    _equippedContent.Add(EquipmentRow(L10n.SlotName(slot), L10n.UIEmptySlot, null, null, null, 0));
                    continue;
                }
                EquipmentDTO capturedEq = eq;
                _equippedContent.Add(EquipmentRow(L10n.SlotName(slot), EquipmentPresenter.BuildEquipmentLine(eq, null, true), () => Select(capturedEq), null, () => _gameState.Unequip(capturedSlot), eq.rarity));
            }

            _bagContent.Clear();
            foreach (var item in EquipmentPresenter.SortBagForDisplay(_gameState.Bag, _gameState.Equipped))
            {
                EquipmentDTO eq = item;
                _bagContent.Add(EquipmentRow(L10n.SlotName(eq.slot), EquipmentPresenter.BuildEquipmentLine(eq, EquippedAtSlot(eq.slot), false), () => Select(eq), () => _gameState.Equip(eq.uid), () => _gameState.Decompose(eq.uid), eq.rarity));
            }
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

        private void RefreshProgression()
        {
            _reincarnText.text = string.Format(L10n.UISouls, _gameState.Souls, _gameState.MaxFloor, _gameState.CanReincarn);
            string[] desc = { L10n.TalentDamageDesc, L10n.TalentQualityDesc, L10n.TalentDropDesc, L10n.TalentOfflineDesc };
            string text = L10n.UITalentsLabel + "\n";
            for (int i = 0; i < TalentNames.Length; i++)
            {
                int lv = TalentLevel(TalentKeys[i]);
                text += EquipmentPresenter.BuildTalentLine(TalentNames[i], lv, TalentMax[i], desc[i], _gameState.Souls) + "\n";
            }
            _talentsText.text = text;
            RefreshTalentActions();
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

        private void RefreshDetail()
        {
            if (_selected == null)
            {
                _detailText.text = EquipmentPresenter.BuildDetail(null, null);
                RefreshDetailActions();
                return;
            }
            _detailText.text = EquipmentPresenter.BuildDetail(_selected, EquippedAtSlot(_selected.slot));
            RefreshDetailActions();
        }

        private void RefreshDetailActions()
        {
            if (_detailActions == null) return;
            _detailActions.Clear();

            bool isEquipped = IsSelectedEquipped();
            foreach (var action in EquipmentPresenter.DetailActions(_selected, isEquipped))
            {
                var captured = action;
                _detailActions.Add(ActionButton(captured.Label, () => RunDetailAction(captured.Id), 84, ButtonForAction(captured.Id)));
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
                    AddToast($"已发送分解：{_selected.name}", ToastDuration);
                    _gameState.Decompose(_selected.uid);
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
                _gameState.Decompose(eq.uid);
                count++;
            }
            if (count > 0) AddToast(string.Format(L10n.UIDecomposeWeakDone, count), ToastDuration);
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
            button.style.height = 34;
            button.style.backgroundColor = new StyleColor(color);
            button.style.color = new StyleColor(new Color32(255, 247, 237, 255));
            button.style.borderTopWidth = 1;
            button.style.borderRightWidth = 1;
            button.style.borderBottomWidth = 1;
            button.style.borderLeftWidth = 1;
            button.style.borderTopColor = new StyleColor(new Color32(160, 124, 69, 255));
            button.style.borderRightColor = new StyleColor(new Color32(70, 48, 31, 255));
            button.style.borderBottomColor = new StyleColor(new Color32(37, 25, 18, 255));
            button.style.borderLeftColor = new StyleColor(new Color32(160, 124, 69, 255));
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
