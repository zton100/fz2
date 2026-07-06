using EquipmentIdle.Data;
using EquipmentIdle.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace EquipmentIdle.UI
{
    public partial class MainController
    {
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

        private static int SlotIndexByName(string slotName)
        {
            for (int i = 0; i < 8; i++)
            {
                if (L10n.SlotName(i) == slotName) return i;
            }
            return 0;
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

        private EquipmentDTO EquippedAtSlot(int slot)
        {
            foreach (var eq in _gameState.Equipped)
            {
                if (eq.slot == slot) return eq;
            }
            return null;
        }
    }
}
