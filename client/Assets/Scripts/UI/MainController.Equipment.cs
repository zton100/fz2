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
            row.style.minHeight = 520;
            row.style.flexGrow = 1;
            row.style.marginBottom = 8;
            row.style.alignItems = Align.Stretch;
            root.Add(row);

            var bag = Panel("mobile-bag");
            bag.style.flexGrow = 1;
            bag.style.marginRight = 10;
            bag.style.backgroundColor = new StyleColor(new Color32(12, 11, 9, 250));
            bag.Add(SectionTitle("背包"));
            _bagFilterActions = new VisualElement();
            _bagFilterActions.style.flexDirection = FlexDirection.Row;
            _bagFilterActions.style.flexWrap = Wrap.Wrap;
            _bagFilterActions.style.marginBottom = 8;
            bag.Add(_bagFilterActions);
            var bagScroll = new ScrollView();
            bagScroll.style.flexGrow = 1;
            _bagContent = bagScroll.contentContainer;
            bag.Add(bagScroll);
            row.Add(bag);

            var equipped = Panel("bag-equipped");
            equipped.style.width = 350;
            equipped.style.backgroundColor = new StyleColor(new Color32(13, 11, 9, 250));
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
            _bagFilterActions.Add(FilterButton("全部", EquipmentBagFilter.All));
            _bagFilterActions.Add(FilterButton("提升", EquipmentBagFilter.Upgrades));
            _bagFilterActions.Add(FilterButton("稀有", EquipmentBagFilter.Rare));
            _bagFilterActions.Add(FilterButton("分解", EquipmentBagFilter.Decompose));
        }

        private Button FilterButton(string label, EquipmentBagFilter filter)
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
            detail.style.height = 340;
            detail.style.marginBottom = 0;
            detail.style.borderTopWidth = 2;
            detail.style.borderTopColor = new StyleColor(PanelBorderHot);
            detail.style.backgroundColor = new StyleColor(new Color32(10, 9, 8, 255));
            root.Add(detail);

            detail.Add(SectionTitle("装备详情"));
            var detailColumns = Row();
            detailColumns.style.flexGrow = 1;
            detailColumns.style.marginBottom = 8;
            detailColumns.style.alignItems = Align.Stretch;
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
            column.style.paddingLeft = 10;
            column.style.paddingRight = 10;
            column.style.paddingTop = 8;
            column.style.paddingBottom = 8;
            column.style.backgroundColor = new StyleColor(new Color32(15, 13, 10, 245));
            column.style.borderTopWidth = 1;
            column.style.borderRightWidth = 1;
            column.style.borderBottomWidth = 2;
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
            return EquipmentPresenter.ShouldShowInBag(eq, EquippedAtSlot(eq.slot), _bagFilter, eq != null && _lockedEquipment.Contains(eq.uid));
        }

        private VisualElement EquipmentSlotTile(string slot, EquipmentDTO eq, System.Action secondary, System.Action select)
        {
            int rarity = eq != null ? eq.rarity : 0;
            var tile = new VisualElement();
            bool selected = eq != null && _selected != null && _selected.uid == eq.uid;
            tile.style.width = Length.Percent(48);
            tile.style.height = 110;
            tile.style.marginRight = 5;
            tile.style.marginBottom = 8;
            tile.style.paddingLeft = 7;
            tile.style.paddingRight = 7;
            tile.style.paddingTop = 7;
            tile.style.paddingBottom = 7;
            tile.style.backgroundColor = new StyleColor(eq != null ? RarityTileBackground(rarity) : new Color32(12, 11, 10, 255));
            tile.style.borderTopWidth = 2;
            tile.style.borderRightWidth = 2;
            tile.style.borderBottomWidth = selected ? 4 : 2;
            tile.style.borderLeftWidth = 2;
            tile.style.borderTopColor = new StyleColor(eq != null ? RarityGlowColor(rarity) : new Color32(55, 48, 39, 255));
            tile.style.borderRightColor = new StyleColor(eq != null ? RarityGlowColor(rarity) : new Color32(55, 48, 39, 255));
            tile.style.borderBottomColor = new StyleColor(selected ? GoldText : eq != null ? RarityFrameColor(rarity) : new Color32(31, 27, 22, 255));
            tile.style.borderLeftColor = new StyleColor(eq != null ? RarityGlowColor(rarity) : new Color32(55, 48, 39, 255));
            tile.RegisterCallback<ClickEvent>(_ => select?.Invoke());

            var top = Row();
            top.style.alignItems = Align.FlexStart;
            var icon = IconImage(eq != null ? IconForEquipment(eq) : IconForSlot(SlotIndexByName(slot)), 48, 48);
            icon.style.marginRight = 7;
            top.Add(icon);

            var copy = new VisualElement();
            copy.style.flexGrow = 1;
            var slotText = Text(slot + (eq != null && _lockedEquipment.Contains(eq.uid) ? " 锁定" : ""), 11, true);
            slotText.style.color = new StyleColor(TextMuted);
            copy.Add(slotText);

            string name = eq != null ? eq.name : L10n.UIEmptySlot;
            var nameText = Text(name, 12, true);
            nameText.style.color = new StyleColor(eq != null ? RarityUIColor(eq.rarity) : new Color32(105, 96, 82, 255));
            nameText.style.height = 34;
            copy.Add(nameText);
            top.Add(copy);
            tile.Add(top);

            var footerText = Text(eq != null ? $"战力贡献 {EquipmentPresenter.Score(eq):F0}" : "等待掉落", 11, true);
            footerText.style.color = new StyleColor(eq != null && eq.rarity >= 2 ? GoldText : eq != null ? TextMain : TextMuted);
            footerText.style.marginTop = 6;
            tile.Add(footerText);

            if (eq != null)
            {
                var bottom = Row();
                bottom.style.marginTop = 4;
                bottom.Add(MiniBadge(L10n.RarityName(eq.rarity), RarityGlowColor(eq.rarity), new Color32(12, 10, 8, 255), 48));
                bottom.Add(MiniBadge($"+{eq.upgrade}", new Color32(44, 34, 23, 255), GoldText, 42));
                tile.Add(bottom);
            }
            return tile;
        }

        private VisualElement BagEquipmentCard(EquipmentDTO eq, EquipmentDTO current)
        {
            float delta = EquipmentPresenter.Score(eq) - EquipmentPresenter.Score(current);
            bool selected = _selected != null && _selected.uid == eq.uid;
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Stretch;
            row.style.minHeight = 104;
            row.style.backgroundColor = new StyleColor(RarityTileBackground(eq.rarity));
            row.style.borderLeftWidth = selected ? 7 : 5;
            row.style.borderLeftColor = new StyleColor(RarityGlowColor(eq.rarity));
            row.style.borderTopWidth = 1;
            row.style.borderRightWidth = 1;
            row.style.borderBottomWidth = selected ? 3 : 1;
            row.style.borderTopColor = new StyleColor(RarityGlowColor(eq.rarity));
            row.style.borderRightColor = new StyleColor(RarityFrameColor(eq.rarity));
            row.style.borderBottomColor = new StyleColor(selected ? GoldText : RarityFrameColor(eq.rarity));
            row.style.paddingLeft = 8;
            row.style.paddingRight = 6;
            row.style.paddingTop = 8;
            row.style.paddingBottom = 8;
            row.style.marginBottom = 8;
            row.RegisterCallback<ClickEvent>(_ => Select(eq));

            var icon = IconImage(IconForEquipment(eq), 64, 64);
            icon.style.marginRight = 10;
            icon.style.alignSelf = Align.Center;
            row.Add(icon);

            var body = new VisualElement();
            body.style.flexGrow = 1;

            string lockText = _lockedEquipment.Contains(eq.uid) ? " 锁" : "";
            var title = Text($"{eq.name}{lockText}", 15, true);
            title.style.color = RarityUIColor(eq.rarity);
            title.style.height = 24;
            body.Add(title);

            string state = EquipmentPresenter.HasEconomyAffix(eq)
                ? "经济流派"
                : current == null ? "新部位" : delta > 0f ? $"提升 +{delta:F0}" : delta < 0f ? $"更弱 {delta:F0}" : "持平";
            var tags = Row();
            tags.style.marginTop = 3;
            tags.style.marginBottom = 4;
            tags.Add(MiniBadge(L10n.SlotName(eq.slot), new Color32(39, 31, 23, 255), TextMuted, 54));
            tags.Add(MiniBadge(L10n.RarityName(eq.rarity), RarityGlowColor(eq.rarity), new Color32(9, 7, 6, 255), 54));
            tags.Add(MiniBadge($"+{eq.upgrade}", new Color32(63, 42, 22, 255), GoldText, 42));
            body.Add(tags);

            var meta = Text($"战力贡献 {EquipmentPresenter.Score(eq):F0} · {state}", 12, true);
            meta.style.color = new StyleColor(delta > 0f || current == null ? new Color32(74, 222, 128, 255) : delta < 0f ? new Color32(248, 113, 113, 255) : TextMuted);
            body.Add(meta);

            var affix = Text(FirstAffixText(eq), 11, false);
            affix.style.color = new StyleColor(TextMuted);
            affix.style.height = 18;
            body.Add(affix);

            row.Add(body);
            var actions = new VisualElement();
            actions.style.width = 76;
            actions.style.flexDirection = FlexDirection.Column;
            actions.style.justifyContent = Justify.Center;
            actions.Add(CompactButton(_lockedEquipment.Contains(eq.uid) ? "解锁" : "锁", () => ToggleLock(eq), ButtonDefault));
            actions.Add(CompactButton(L10n.UIEquip, () => _gameState.Equip(eq.uid), ButtonEquip));
            var decompose = ActionButton(L10n.UIDecompose, () => DecomposeFromUI(eq), 64, ButtonDanger);
            decompose.style.height = 26;
            decompose.style.fontSize = 11;
            decompose.style.marginTop = 2;
            decompose.style.marginBottom = 2;
            decompose.SetEnabled(!_lockedEquipment.Contains(eq.uid));
            actions.Add(decompose);
            row.Add(actions);
            return row;
        }

        private static Button CompactButton(string label, System.Action action, Color32 color)
        {
            var button = ActionButton(label, action, 64, color);
            button.style.height = 26;
            button.style.fontSize = 11;
            button.style.marginTop = 2;
            button.style.marginBottom = 2;
            return button;
        }

        private static Label MiniBadge(string text, Color32 background, Color color, float width)
        {
            var badge = Text(text, 10, true);
            badge.style.width = width;
            badge.style.height = 20;
            badge.style.marginRight = 4;
            badge.style.unityTextAlign = TextAnchor.MiddleCenter;
            badge.style.color = new StyleColor(color);
            badge.style.backgroundColor = new StyleColor(background);
            badge.style.borderTopWidth = 1;
            badge.style.borderRightWidth = 1;
            badge.style.borderBottomWidth = 1;
            badge.style.borderLeftWidth = 1;
            badge.style.borderTopColor = new StyleColor(new Color32(96, 70, 42, 220));
            badge.style.borderRightColor = new StyleColor(new Color32(42, 31, 24, 220));
            badge.style.borderBottomColor = new StyleColor(new Color32(31, 24, 19, 220));
            badge.style.borderLeftColor = new StyleColor(new Color32(96, 70, 42, 220));
            return badge;
        }

        private static string FirstAffixText(EquipmentDTO eq)
        {
            if (eq == null || eq.affixes == null || eq.affixes.Length == 0) return "无词缀";
            string text = EquipmentPresenter.FormatAffix(eq.affixes[0]);
            if (eq.affixes.Length > 1) text += $" 等 {eq.affixes.Length} 条词缀";
            return text;
        }

        private Texture2D IconForSlot(int slot)
        {
            if (_slotIcons == null || _slotIcons.Length == 0) return null;
            if (slot < 0) slot = 0;
            if (slot >= _slotIcons.Length) slot = _slotIcons.Length - 1;
            return _slotIcons[slot];
        }

        private Texture2D IconForEquipment(EquipmentDTO eq)
        {
            if (eq == null) return IconForSlot(0);
			string resourceKey = EquipmentPresenter.IconResourceKey(eq);
            if (!string.IsNullOrEmpty(resourceKey))
            {
				if (!_equipmentIconCache.TryGetValue(resourceKey, out Texture2D texture))
                {
					texture = Resources.Load<Texture2D>("UI/Equipment/" + resourceKey);
					if (texture == null && !string.IsNullOrEmpty(eq.base_id) && resourceKey != eq.base_id)
						texture = Resources.Load<Texture2D>("UI/Equipment/" + eq.base_id);
					_equipmentIconCache[resourceKey] = texture;
                }
                if (texture != null) return texture;
            }
            return IconForSlot(eq.slot);
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
            if (eq == null)
            {
                var label = Text(text, 13, false);
                label.style.whiteSpace = WhiteSpace.Normal;
                label.style.color = new StyleColor(color);
                column.Add(label);
                return;
            }

            var header = Row();
            header.style.alignItems = Align.FlexStart;
            header.style.marginBottom = 8;
            var icon = IconImage(IconForEquipment(eq), 68, 68);
            icon.style.marginRight = 10;
            header.Add(icon);

            var copy = new VisualElement();
            copy.style.flexGrow = 1;
            var name = Text(eq.name, 15, true);
            name.style.color = new StyleColor(color);
            name.style.height = 24;
            copy.Add(name);
            var tags = Row();
            tags.Add(MiniBadge(L10n.RarityName(eq.rarity), RarityGlowColor(eq.rarity), new Color32(10, 8, 6, 255), 54));
            tags.Add(MiniBadge(L10n.SlotName(eq.slot), new Color32(39, 31, 23, 255), TextMuted, 54));
            tags.Add(MiniBadge($"+{eq.upgrade}", new Color32(63, 42, 22, 255), GoldText, 42));
            copy.Add(tags);
            header.Add(copy);
            column.Add(header);

            column.Add(DetailStatLine("战力贡献", $"{EquipmentPresenter.Score(eq):F0}", GoldText));
			if (!string.IsNullOrEmpty(eq.legendary_id))
			{
				column.Add(DetailTextBlock("传奇特效", eq.legendary_description, new Color32(255, 153, 50, 255)));
				if (eq.legendary_bonuses != null)
				{
					foreach (var bonus in eq.legendary_bonuses)
					{
						column.Add(DetailStatLine("固定加成", EquipmentPresenter.FormatAffix(bonus), GoldText));
					}
				}
				if (eq.legendary_power_bonus > 0f)
					column.Add(DetailStatLine("传奇增幅", $"全局战力 +{eq.legendary_power_bonus * 100f:F1}%", GoldText));
				if (eq.boss_reward_bonus > 0f)
					column.Add(DetailStatLine("传奇增幅", $"首通材料 +{eq.boss_reward_bonus * 100f:F1}%", GoldText));
			}
			if (!string.IsNullOrEmpty(eq.artifact_id))
			{
				column.Add(DetailTextBlock("神器触发", eq.artifact_description, new Color32(248, 113, 113, 255)));
				if (eq.artifact_bonuses != null)
				{
					foreach (var bonus in eq.artifact_bonuses)
					{
						column.Add(DetailStatLine("固定加成", EquipmentPresenter.FormatAffix(bonus), GoldText));
					}
				}
				if (!string.IsNullOrEmpty(eq.artifact_trigger))
					column.Add(DetailStatLine("触发类型", EquipmentPresenter.ArtifactTriggerName(eq.artifact_trigger), new Color32(248, 113, 113, 255)));
				if (eq.artifact_value > 0f)
					column.Add(DetailStatLine("触发强度", $"+{eq.artifact_value * 100f:F1}%", GoldText));
			}
            if (eq.affixes == null || eq.affixes.Length == 0)
            {
                column.Add(DetailStatLine("词缀", "无", TextMuted));
                return;
            }

            foreach (var affix in eq.affixes)
            {
                column.Add(DetailStatLine("词缀", EquipmentPresenter.FormatAffix(affix), TextMain));
            }
        }

        private static VisualElement DetailStatLine(string label, string value, Color color)
        {
            var row = Row();
            row.style.height = 25;
            row.style.marginBottom = 3;
            row.style.paddingLeft = 6;
            row.style.paddingRight = 6;
            row.style.backgroundColor = new StyleColor(new Color32(10, 9, 8, 210));

            var name = Text(label, 11, true);
            name.style.width = 58;
            name.style.whiteSpace = WhiteSpace.NoWrap;
            name.style.color = new StyleColor(TextMuted);
            row.Add(name);

            var val = Text(value, 12, true);
            val.style.flexGrow = 1;
            val.style.color = new StyleColor(color);
            row.Add(val);
            return row;
        }

		private static VisualElement DetailTextBlock(string label, string value, Color color)
		{
			var block = new VisualElement();
			block.style.minHeight = 48;
			block.style.marginBottom = 3;
			block.style.paddingLeft = 6;
			block.style.paddingRight = 6;
			block.style.paddingTop = 5;
			block.style.paddingBottom = 5;
			block.style.backgroundColor = new StyleColor(new Color32(10, 9, 8, 210));

			var title = Text(label, 11, true);
			title.style.color = new StyleColor(color);
			block.Add(title);
			var copy = Text(string.IsNullOrEmpty(value) ? "固定传奇能力" : value, 11, false);
			copy.style.whiteSpace = WhiteSpace.Normal;
			copy.style.color = new StyleColor(TextMain);
			block.Add(copy);
			return block;
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
            container.style.marginBottom = 4;
            container.style.minHeight = 27;
            container.style.paddingLeft = 6;
            container.style.paddingRight = 6;
            container.style.backgroundColor = new StyleColor(new Color32(11, 10, 9, 220));
            container.style.borderLeftWidth = 3;
            container.style.borderLeftColor = new StyleColor(row.Delta > 0f ? new Color32(74, 222, 128, 255) : row.Delta < 0f ? new Color32(248, 113, 113, 255) : PanelBorder);

            var name = Text(row.Label, 12, true);
            name.style.width = 76;
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
            EquipmentDTO current = _selected != null ? EquippedAtSlot(_selected.slot) : null;
            foreach (var action in EquipmentPresenter.DetailActions(_selected, isEquipped, current))
            {
                var captured = action;
                var button = ActionButton(captured.Label, () => RunDetailAction(captured.Id), 84, ButtonForAction(captured.Id));
                if (captured.Id == "decompose" && _selected != null && _lockedEquipment.Contains(_selected.uid))
                {
                    button.SetEnabled(false);
                }
                if (captured.Id == "transfer_upgrade" && _selected != null && (current == null || _lockedEquipment.Contains(_selected.uid) || _lockedEquipment.Contains(current.uid)))
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
                case "transfer_upgrade":
                    EquipmentDTO source = EquippedAtSlot(_selected.slot);
                    if (source == null)
                    {
                        AddToast("当前部位无旧装可继承。", ToastDuration);
                        return;
                    }
                    TrackPendingCraft("继承", _selected);
                    AddToast($"已发送继承：{source.name} -> {_selected.name}", ToastDuration);
                    _gameState.TransferUpgrade(source.uid, _selected.uid);
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
            if (equippedCount > 0) AddToast($"{string.Format(L10n.UIEquipBestDone, equippedCount)}  战力贡献 +{expectedDelta:F0}", ToastDuration);
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
