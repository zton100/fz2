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
        private Label _materialsText;
        private Label _reincarnText;
        private Label _talentsText;
        private Label _detailText;
        private Label _dungeonTitleText;
        private Label _monsterText;
        private Label _battleText;
        private Label _lootFeedText;
        private Label _toastText;
        private Label _offlineText;
        private VisualElement _offlinePanel;
        private VisualElement _equippedContent;
        private VisualElement _bagContent;
        private VisualElement _detailActions;
        private VisualElement _dungeonPanel;
        private VisualElement _lootPanel;
        private VisualElement _bossProgressFill;
        private EquipmentDTO _selected;
        private float _prevPower;
        private bool _prevCanReincarn;
        private float _battlePulseUntil;
        private float _lootPulseUntil;
        private float _bossProgressTarget;
        private float _bossProgressCurrent;

        private struct Toast
        {
            public string Text;
            public float ExpireAt;
        }

        private readonly List<Toast> _toasts = new List<Toast>();
        private readonly List<string> _lootFeed = new List<string>();
        private const float ToastDuration = 3f;

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
            KeepSelectedIfPresent();
            RefreshEquipmentLists();
            RefreshDetail();
        }

        private void OnPower(float power)
        {
            float delta = power - _prevPower;
            _prevPower = power;
            if (delta > 0.5f)
                _powerText.text = $"power: {power:F1} +{delta:F1}";
            else if (delta < -0.5f)
                _powerText.text = $"power: {power:F1} {delta:F1}";
            else
                _powerText.text = string.Format(L10n.UIPowerLabel, power);
            RefreshStuck();
        }

        private void OnLoot(EquipmentDTO eq)
        {
            string line = $"{L10n.RarityName(eq.rarity)} {eq.name} +{eq.upgrade}";
            _lootFeed.Insert(0, line);
            if (_lootFeed.Count > 6) _lootFeed.RemoveAt(_lootFeed.Count - 1);
            RefreshLootFeed();
            _lootPulseUntil = Time.realtimeSinceStartup + 0.8f;
            AddToast($"{string.Format(L10n.UILootToast, L10n.RarityName(eq.rarity), eq.name)}", ToastDuration);
        }

        private void OnFloor(int newFloor)
        {
            _battlePulseUntil = Time.realtimeSinceStartup + 0.7f;
            if (newFloor > 1 && (newFloor - 1) % 5 == 0)
            {
                AddToast($"Boss defeated: floor {newFloor - 1}", ToastDuration);
            }
            RefreshHeader();
            RefreshStuck();
        }

        private void OnOfflineResult(OfflineResultData ord)
        {
            int h = ord.duration_seconds / 3600;
            int m = (ord.duration_seconds % 3600) / 60;
            string dur = h > 0 ? $"{h}h{m}m" : $"{m}m";
            _offlineText.text = $"{L10n.UIOfflineTitle}\n"
                + $"{string.Format(L10n.UIOfflineDuration, dur)}\n"
                + $"{string.Format(L10n.UIOfflineLoot, ord.loot_count)}\n"
                + $"{string.Format(L10n.UIOfflineFloors, ord.floors_advanced)}\n"
                + $"{string.Format(L10n.UIOfflineTicks, ord.ticks_simulated)}";
            _offlinePanel.style.display = DisplayStyle.Flex;
        }

        private void OnCraftResult(CraftResultData cr)
        {
            AddToast(cr.msg, ToastDuration);
        }

        private void OnTalents(int souls, int maxFloor, bool canReincarn, Dictionary<string, int> talents)
        {
            if (canReincarn && !_prevCanReincarn)
            {
                AddToast("Reincarnation available", 5f);
            }
            _prevCanReincarn = canReincarn;
            RefreshProgression();
        }

        private void BuildUI()
        {
            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1280, 720);

            var doc = gameObject.AddComponent<UIDocument>();
            doc.panelSettings = panelSettings;

            var root = doc.rootVisualElement;
            root.style.flexGrow = 1;
            root.style.backgroundColor = new StyleColor(new Color32(20, 19, 17, 255));
            root.style.paddingLeft = 16;
            root.style.paddingRight = 16;
            root.style.paddingTop = 12;
            root.style.paddingBottom = 12;

            var header = Panel("header");
            header.style.height = 96;
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.marginBottom = 10;
            root.Add(header);

            var titleCol = Column(360);
            titleCol.Add(Text("Equipment Idle", 24, true));
            _statusText = Text("", 14, false);
            _syncText = Text("", 14, false);
            titleCol.Add(_statusText);
            titleCol.Add(_syncText);
            header.Add(titleCol);

            var powerCol = Column(300);
            _powerText = Text("", 20, true);
            _stuckText = Text("", 13, false);
            _stuckText.style.color = new StyleColor(new Color32(248, 113, 113, 255));
            powerCol.Add(_powerText);
            powerCol.Add(_stuckText);
            header.Add(powerCol);

            var loginCol = Row();
            loginCol.style.flexGrow = 1;
            loginCol.style.justifyContent = Justify.FlexEnd;
            _accountInput = new TextField { value = "hero" };
            _accountInput.style.width = 260;
            _accountInput.style.height = 38;
            loginCol.Add(_accountInput);
            loginCol.Add(ActionButton(L10n.UIConnect, () =>
            {
                string acc = _accountInput.value.Trim();
                if (string.IsNullOrEmpty(acc)) acc = "hero";
                _statusText.text = "Status: " + L10n.UIStatusConnecting;
                _gameState.ConnectAndLogin(acc);
            }, 110));
            header.Add(loginCol);

            BuildDungeonPanel(root);

            var body = Row();
            body.style.flexGrow = 1;
            root.Add(body);

            var left = Panel("equipped");
            left.style.width = 330;
            body.Add(left);
            left.Add(SectionTitle(string.Format(L10n.UIEquipped, 0)));
            var equippedScroll = new ScrollView();
            equippedScroll.style.flexGrow = 1;
            _equippedContent = equippedScroll.contentContainer;
            left.Add(equippedScroll);
            left.Add(ActionButton(L10n.UIEquipBest, EquipBestBySlot));

            var middle = Panel("bag");
            middle.style.flexGrow = 1;
            middle.style.marginLeft = 10;
            middle.style.marginRight = 10;
            body.Add(middle);
            var bagHeader = Row();
            bagHeader.Add(SectionTitle(L10n.UIBackpack));
            bagHeader.Add(ActionButton(L10n.UIDecomposeWeak, DecomposeWeakItems, 160));
            middle.Add(bagHeader);
            var bagScroll = new ScrollView();
            bagScroll.style.flexGrow = 1;
            _bagContent = bagScroll.contentContainer;
            middle.Add(bagScroll);

            var right = Panel("details");
            right.style.width = 370;
            body.Add(right);
            right.Add(SectionTitle("Details"));
            _detailText = Text("", 14, false);
            _detailText.style.flexGrow = 1;
            right.Add(_detailText);
            _detailActions = new VisualElement();
            _detailActions.style.flexDirection = FlexDirection.Row;
            _detailActions.style.flexWrap = Wrap.Wrap;
            _detailActions.style.marginBottom = 8;
            right.Add(_detailActions);
            _materialsText = Text("", 13, false);
            right.Add(_materialsText);
            right.Add(SectionTitle(L10n.UIWorkshop));
            var composeGrid = new VisualElement();
            composeGrid.style.flexDirection = FlexDirection.Row;
            composeGrid.style.flexWrap = Wrap.Wrap;
            composeGrid.style.marginBottom = 8;
            for (int s = 0; s < 8; s++)
            {
                int slot = s;
                composeGrid.Add(ActionButton(L10n.SlotName(slot), () => _gameState.Compose(slot), 82));
            }
            right.Add(composeGrid);
            right.Add(SectionTitle(L10n.UIReincarnation));
            _reincarnText = Text("", 13, false);
            right.Add(_reincarnText);
            right.Add(ActionButton(L10n.UIReincarnate, () =>
            {
                if (_gameState.CanReincarn) _gameState.Reincarn();
            }));
            _talentsText = Text("", 13, false);
            right.Add(_talentsText);

            _toastText = Text("", 14, false);
            _toastText.style.height = 58;
            _toastText.style.marginTop = 10;
            root.Add(_toastText);

            BuildOfflinePanel(root);
        }

        private void BuildDungeonPanel(VisualElement root)
        {
            var dungeon = Panel("dungeon");
            _dungeonPanel = dungeon;
            dungeon.style.height = 160;
            dungeon.style.marginBottom = 10;
            dungeon.style.flexDirection = FlexDirection.Row;
            dungeon.style.backgroundColor = new StyleColor(new Color32(36, 33, 25, 255));
            root.Add(dungeon);

            var hero = Column(270);
            hero.style.marginRight = 16;
            hero.Add(Text("HERO", 12, true));
            hero.Add(Text("Auto battle online", 22, true));
            hero.Add(Text("Equipment drives all power. Loot, compare, equip, repeat.", 13, false));
            dungeon.Add(hero);

            var run = new VisualElement();
            run.style.flexGrow = 1;
            run.style.flexDirection = FlexDirection.Column;
            run.style.marginRight = 16;
            _dungeonTitleText = Text("", 24, true);
            _battleText = Text("", 16, true);
            run.Add(_dungeonTitleText);
            run.Add(_battleText);

            var progressFrame = new VisualElement();
            progressFrame.style.height = 18;
            progressFrame.style.marginTop = 14;
            progressFrame.style.marginBottom = 10;
            progressFrame.style.backgroundColor = new StyleColor(new Color32(12, 16, 14, 255));
            progressFrame.style.borderTopLeftRadius = 5;
            progressFrame.style.borderTopRightRadius = 5;
            progressFrame.style.borderBottomLeftRadius = 5;
            progressFrame.style.borderBottomRightRadius = 5;
            _bossProgressFill = new VisualElement();
            _bossProgressFill.style.height = Length.Percent(100);
            _bossProgressFill.style.backgroundColor = new StyleColor(new Color32(217, 119, 6, 255));
            _bossProgressFill.style.borderTopLeftRadius = 5;
            _bossProgressFill.style.borderTopRightRadius = 5;
            _bossProgressFill.style.borderBottomLeftRadius = 5;
            _bossProgressFill.style.borderBottomRightRadius = 5;
            progressFrame.Add(_bossProgressFill);
            run.Add(progressFrame);
            run.Add(Text("Boss gate every 5 floors", 12, false));
            dungeon.Add(run);

            var monster = Column(260);
            monster.style.marginRight = 16;
            monster.Add(Text("ENCOUNTER", 12, true));
            _monsterText = Text("", 17, true);
            monster.Add(_monsterText);
            dungeon.Add(monster);

            var loot = Column(260);
            _lootPanel = loot;
            loot.style.marginRight = 0;
            loot.Add(Text("RECENT LOOT", 12, true));
            _lootFeedText = Text("", 13, false);
            loot.Add(_lootFeedText);
            dungeon.Add(loot);
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
            _statusText.text = "Status: " + status;
            _syncText.text = $"Account: {_gameState.Account}   Floor: {_gameState.Floor}   Souls: {_gameState.Souls}";
            _powerText.text = string.Format(L10n.UIPowerLabel, _gameState.Power);
            RefreshDungeon();
        }

        private void RefreshStuck()
        {
            float monsterPower = EquipmentPresenter.MonsterPowerAtFloor(_gameState.Floor);
            if (_gameState.Power > 0 && _gameState.Power <= monsterPower)
                _stuckText.text = string.Format(L10n.UIStuckPrefix, _gameState.Power, monsterPower, _gameState.Floor);
            else
                _stuckText.text = $"Monster power: {monsterPower:F1}";
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
            _bossProgressFill.style.backgroundColor = new StyleColor(dungeon.IsBoss ? new Color32(220, 38, 38, 255) : new Color32(217, 119, 6, 255));
        }

        private void RefreshLootFeed()
        {
            if (_lootFeedText == null) return;
            if (_lootFeed.Count == 0)
            {
                _lootFeedText.text = "No drops yet. Connect and let auto battle run.";
                return;
            }
            string text = "";
            foreach (var line in _lootFeed) text += line + "\n";
            _lootFeedText.text = text;
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
                Color baseColor = new Color32(36, 33, 25, 255);
                Color hitColor = new Color32(92, 64, 35, 255);
                _dungeonPanel.style.backgroundColor = new StyleColor(Color.Lerp(baseColor, hitColor, pulse));
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
                _equippedContent.Add(EquipmentRow(L10n.SlotName(slot), ShortEquipment(eq), () => Select(capturedEq), null, () => _gameState.Unequip(capturedSlot), eq.rarity));
            }

            _bagContent.Clear();
            foreach (var item in EquipmentPresenter.SortBagForDisplay(_gameState.Bag, _gameState.Equipped))
            {
                EquipmentDTO eq = item;
                _bagContent.Add(EquipmentRow(L10n.SlotName(eq.slot), ShortEquipment(eq), () => Select(eq), () => _gameState.Equip(eq.uid), () => _gameState.Decompose(eq.uid), eq.rarity));
            }
        }

        private VisualElement EquipmentRow(string slot, string label, System.Action select, System.Action primary, System.Action secondary, int rarity)
        {
            var row = Row();
            row.style.backgroundColor = new StyleColor(new Color32(48, 43, 36, 255));
            row.style.borderLeftWidth = 4;
            row.style.borderLeftColor = RarityUIColor(rarity);
            row.style.paddingLeft = 8;
            row.style.paddingRight = 8;
            row.style.paddingTop = 6;
            row.style.paddingBottom = 6;
            row.style.marginBottom = 6;

            var slotLabel = Text(slot, 12, true);
            slotLabel.style.width = 58;
            row.Add(slotLabel);

            var body = ActionButton(label, select ?? (() => { }));
            body.SetEnabled(select != null);
            body.style.flexGrow = 1;
            row.Add(body);

            if (primary != null) row.Add(ActionButton(L10n.UIEquip, primary, 54));
            if (secondary != null) row.Add(ActionButton(primary == null ? L10n.UIUnequip : L10n.UIDecompose, secondary, 72));
            return row;
        }

        private void RefreshMaterials()
        {
            string text = L10n.UIMaterials;
            foreach (var kv in _gameState.Materials)
            {
                text += $"{kv.Key}={kv.Value}  ";
            }
            _materialsText.text = text;
        }

        private void RefreshProgression()
        {
            _reincarnText.text = string.Format(L10n.UISouls, _gameState.Souls, _gameState.MaxFloor, _gameState.CanReincarn);
            string[] names = { L10n.TalentDamage, L10n.TalentQuality, L10n.TalentDrop, L10n.TalentOfflineGain };
            string[] desc = { L10n.TalentDamageDesc, L10n.TalentQualityDesc, L10n.TalentDropDesc, L10n.TalentOfflineDesc };
            int[] max = { 10, 3, 10, 5 };
            string text = L10n.UITalentsLabel + "\n";
            for (int i = 0; i < names.Length; i++)
            {
                int lv = _gameState.Talents.ContainsKey(names[i]) ? _gameState.Talents[names[i]] : 0;
                text += $"{names[i]} Lv{lv}/{max[i]} - {desc[i]}\n";
            }
            _talentsText.text = text;
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
                _detailActions.Add(ActionButton(captured.Label, () => RunDetailAction(captured.Id), 84));
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
                    _gameState.Upgrade(_selected.uid);
                    break;
                case "reforge":
                    _gameState.Reforge(_selected.uid);
                    break;
                case "decompose":
                    _gameState.Decompose(_selected.uid);
                    break;
            }
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
            if (equippedCount > 0) AddToast($"{string.Format(L10n.UIEquipBestDone, equippedCount)}  score +{expectedDelta:F0}", ToastDuration);
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

        private static string ShortEquipment(EquipmentDTO eq)
        {
            string text = $"[{L10n.RarityName(eq.rarity)}] {eq.name} +{eq.upgrade}";
            if (eq.affixes != null && eq.affixes.Length > 0)
            {
                text += $"  {EquipmentPresenter.FormatAffix(eq.affixes[0])}";
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
            el.style.backgroundColor = new StyleColor(new Color32(32, 36, 30, 255));
            el.style.borderTopLeftRadius = 6;
            el.style.borderTopRightRadius = 6;
            el.style.borderBottomLeftRadius = 6;
            el.style.borderBottomRightRadius = 6;
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
            label.style.color = new StyleColor(new Color32(232, 226, 214, 255));
            label.style.whiteSpace = WhiteSpace.Normal;
            if (bold) label.style.unityFontStyleAndWeight = FontStyle.Bold;
            return label;
        }

        private static Label SectionTitle(string value)
        {
            var label = Text(value, 18, true);
            label.style.marginBottom = 6;
            return label;
        }

        private static Button ActionButton(string label, System.Action action, float width = -1)
        {
            var button = new Button(action) { text = label };
            button.style.height = 34;
            button.style.backgroundColor = new StyleColor(new Color32(62, 52, 38, 255));
            button.style.color = new StyleColor(new Color32(255, 247, 237, 255));
            button.style.marginLeft = 4;
            button.style.marginRight = 4;
            button.style.marginTop = 3;
            button.style.marginBottom = 3;
            if (width > 0) button.style.width = width;
            return button;
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
