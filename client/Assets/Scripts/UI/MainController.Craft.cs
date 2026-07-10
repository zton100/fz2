using EquipmentIdle.Data;
using EquipmentIdle.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace EquipmentIdle.UI
{
    public partial class MainController
    {
        private void BuildCraftPanel(VisualElement root)
        {
            var row = Row();
            row.style.minHeight = 520;
            row.style.flexGrow = 1;
            row.style.marginBottom = 8;
            row.style.alignItems = Align.Stretch;
            root.Add(row);

            var craft = Panel("mobile-craft");
            craft.style.flexGrow = 1;
            craft.style.marginRight = 10;
            craft.style.backgroundColor = new StyleColor(new Color32(12, 10, 8, 250));
            craft.Add(SectionTitle("魔炉强化"));
            if (_craftIcon != null)
            {
                var header = Row();
                header.style.marginBottom = 8;
                header.style.alignItems = Align.Center;
                var craftImage = IconImage(_craftIcon, 72, 72);
                craftImage.style.marginRight = 10;
                header.Add(craftImage);
                var copy = new VisualElement();
                copy.style.flexGrow = 1;
                copy.Add(Text("强化、继承、重铸会围绕当前选择装备预估收益。", 13, true));
                var hint = Text("先在背包选择装备，再回到锻造页处理。", 11, false);
                hint.style.color = new StyleColor(TextMuted);
                copy.Add(hint);
                header.Add(copy);
                craft.Add(header);
            }
            _craftPlanContent = new VisualElement();
            _craftPlanContent.style.flexGrow = 1;
            craft.Add(_craftPlanContent);
            var bulkActions = Row();
            bulkActions.style.marginTop = 8;
            bulkActions.style.alignItems = Align.Stretch;
            var equipBest = ActionButton(L10n.UIEquipBest, EquipBestBySlot, -1, ButtonEquip);
            equipBest.style.flexGrow = 1;
            var cleanup = ActionButton(L10n.UIDecomposeWeak, DecomposeWeakItems, -1, ButtonDanger);
            cleanup.style.flexGrow = 1;
            bulkActions.Add(equipBest);
            bulkActions.Add(cleanup);
            craft.Add(bulkActions);
            row.Add(craft);

            var mats = Panel("mobile-materials");
            mats.style.width = 268;
            mats.style.marginRight = 10;
            mats.style.backgroundColor = new StyleColor(new Color32(13, 11, 9, 250));
            mats.Add(SectionTitle("材料库"));
            _materialsText = Text("", 14, false);
            _materialsText.style.display = DisplayStyle.None;
            mats.Add(_materialsText);
            _materialsContent = new VisualElement();
            _materialsContent.style.flexGrow = 1;
            mats.Add(_materialsContent);
            row.Add(mats);

            var compose = Panel("compose-panel");
            compose.style.width = 250;
            compose.style.backgroundColor = new StyleColor(new Color32(12, 10, 8, 250));
            compose.Add(SectionTitle("合成槽"));
            var composeHint = Text("选择部位消耗基础材料，补齐缺口或赌新词缀。", 12, false);
            composeHint.style.color = new StyleColor(TextMuted);
            composeHint.style.marginBottom = 8;
            compose.Add(composeHint);
            for (int s = 0; s < 8; s++)
            {
                int slot = s;
                var button = ActionButton(L10n.SlotName(slot), () => _gameState.Compose(slot), -1, ButtonCraft);
                button.style.height = 30;
                button.style.marginTop = 2;
                button.style.marginBottom = 2;
                compose.Add(button);
            }
            row.Add(compose);
        }

        private void RefreshMaterials()
        {
            string text = L10n.UIMaterials;
            foreach (var kv in _gameState.Materials)
            {
                text += $"{MaterialName(kv.Key)}={kv.Value}  ";
            }
            if (_materialsText != null) _materialsText.text = text;
            RefreshMaterialCards();
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
                _selected != null ? EquippedAtSlot(_selected.slot) : null,
                BaseMaterialCount(),
                weakCount,
                EquipmentPresenter.EquipBestDelta(_gameState.Bag, _gameState.Equipped));

            _craftPlanContent.Add(PlanLine(plan.TransferLine, ButtonEquip));
            _craftPlanContent.Add(PlanLine(plan.UpgradeLine, ButtonCraft));
            _craftPlanContent.Add(PlanLine(plan.ReforgeLine, ButtonReforge));
            _craftPlanContent.Add(PlanLine(plan.ComposeLine, BaseMaterialCount() >= 10 ? ButtonEquip : ButtonDefault));
            _craftPlanContent.Add(PlanLine(plan.CleanupLine, weakCount > 0 ? ButtonDanger : ButtonDefault));
        }

        private static Label PlanLine(string text, Color32 accent)
        {
            var label = Text(text, 13, true);
            label.style.minHeight = 38;
            label.style.marginBottom = 7;
            label.style.paddingLeft = 10;
            label.style.paddingRight = 10;
            label.style.paddingTop = 7;
            label.style.paddingBottom = 7;
            label.style.backgroundColor = new StyleColor(new Color32(14, 12, 10, 230));
            label.style.borderLeftWidth = 4;
            label.style.borderLeftColor = new StyleColor(accent);
            label.style.borderTopWidth = 1;
            label.style.borderRightWidth = 1;
            label.style.borderBottomWidth = 1;
            label.style.borderTopColor = new StyleColor(new Color32(72, 52, 34, 220));
            label.style.borderRightColor = new StyleColor(new Color32(42, 31, 24, 220));
            label.style.borderBottomColor = new StyleColor(new Color32(31, 24, 19, 220));
            label.style.color = new StyleColor(TextMain);
            return label;
        }

        private void RefreshMaterialCards()
        {
            if (_materialsContent == null) return;
            _materialsContent.Clear();
            _materialsContent.Add(MaterialCard("基础材料", BaseMaterialCount(), ButtonCraft, "强化 / 合成"));
            _materialsContent.Add(MaterialCard("词缀材料1", MaterialCount("affix_mat_1"), ButtonReforge, "低阶重铸"));
            _materialsContent.Add(MaterialCard("词缀材料2", MaterialCount("affix_mat_2"), ButtonReforge, "进阶重铸"));
            _materialsContent.Add(MaterialCard("词缀材料3", MaterialCount("affix_mat_3"), ButtonReforge, "稀有重铸"));
            _materialsContent.Add(MaterialCard("词缀材料4", MaterialCount("affix_mat_4"), ButtonReforge, "传奇重铸"));
            _materialsContent.Add(MaterialCard("词缀材料5", MaterialCount("affix_mat_5"), ButtonReforge, "神器重铸"));
        }

        private static VisualElement MaterialCard(string name, int value, Color32 accent, string usage)
        {
            var card = Row();
            card.style.height = 56;
            card.style.marginBottom = 6;
            card.style.paddingLeft = 8;
            card.style.paddingRight = 8;
            card.style.backgroundColor = new StyleColor(new Color32(15, 13, 10, 235));
            card.style.borderLeftWidth = 4;
            card.style.borderLeftColor = new StyleColor(accent);

            var copy = new VisualElement();
            copy.style.flexGrow = 1;
            var title = Text(name, 12, true);
            title.style.color = new StyleColor(GoldText);
            copy.Add(title);
            var sub = Text(usage, 11, false);
            sub.style.color = new StyleColor(TextMuted);
            copy.Add(sub);
            card.Add(copy);

            var amount = Text(value.ToString(), 20, true);
            amount.style.width = 58;
            amount.style.unityTextAlign = TextAnchor.MiddleRight;
            amount.style.color = new StyleColor(value > 0 ? TextMain : TextMuted);
            card.Add(amount);
            return card;
        }

        private int MaterialCount(string key)
        {
            return _gameState.Materials.TryGetValue(key, out var value) ? value : 0;
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
    }
}
