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
