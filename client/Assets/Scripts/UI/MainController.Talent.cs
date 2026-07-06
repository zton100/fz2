using EquipmentIdle.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace EquipmentIdle.UI
{
    public partial class MainController
    {
        private static readonly string[] TalentKeys = { "damage", "quality", "drop", "offline_gain" };

        private static readonly string[] TalentNames = { "伤害", "品质", "掉落", "离线" };

        private static readonly int[] TalentMax = { 10, 3, 10, 5 };

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
    }
}
