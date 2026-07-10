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
            panel.style.minHeight = 520;
            panel.style.flexGrow = 1;
            panel.style.marginBottom = 8;
            panel.style.backgroundColor = new StyleColor(new Color32(10, 9, 8, 255));
            root.Add(panel);

            panel.Add(SectionTitle("转生天赋"));

            var row = Row();
            row.style.flexGrow = 1;
            row.style.alignItems = Align.Stretch;
            panel.Add(row);

            var altar = new VisualElement();
            altar.style.width = 380;
            altar.style.marginRight = 10;
            altar.style.paddingLeft = 10;
            altar.style.paddingRight = 10;
            altar.style.paddingTop = 10;
            altar.style.paddingBottom = 10;
            altar.style.backgroundColor = new StyleColor(new Color32(16, 12, 10, 245));
            altar.style.borderTopWidth = 1;
            altar.style.borderRightWidth = 1;
            altar.style.borderBottomWidth = 2;
            altar.style.borderLeftWidth = 1;
            altar.style.borderTopColor = new StyleColor(PanelBorderHot);
            altar.style.borderRightColor = new StyleColor(new Color32(72, 52, 34, 255));
            altar.style.borderBottomColor = new StyleColor(new Color32(80, 35, 22, 255));
            altar.style.borderLeftColor = new StyleColor(PanelBorderHot);
            row.Add(altar);

            altar.Add(Text("转生祭坛", 16, true));
            _reincarnText = Text("", 14, true);
            _reincarnText.style.color = new StyleColor(GoldText);
            _reincarnText.style.marginTop = 6;
            _reincarnText.style.marginBottom = 8;
            altar.Add(_reincarnText);
            _reincarnPlanContent = new VisualElement();
            _reincarnPlanContent.style.flexGrow = 1;
            altar.Add(_reincarnPlanContent);
            altar.Add(ActionButton(L10n.UIReincarnate, () =>
            {
                if (_gameState.CanReincarn) _gameState.Reincarn();
            }, -1, ButtonAscend));

            var talents = new VisualElement();
            talents.style.flexGrow = 1;
            talents.style.paddingLeft = 10;
            talents.style.paddingRight = 10;
            talents.style.paddingTop = 10;
            talents.style.paddingBottom = 10;
            talents.style.backgroundColor = new StyleColor(new Color32(13, 11, 9, 245));
            talents.style.borderTopWidth = 1;
            talents.style.borderRightWidth = 1;
            talents.style.borderBottomWidth = 2;
            talents.style.borderLeftWidth = 1;
            talents.style.borderTopColor = new StyleColor(new Color32(72, 52, 34, 255));
            talents.style.borderRightColor = new StyleColor(new Color32(72, 52, 34, 255));
            talents.style.borderBottomColor = new StyleColor(PanelBorderHot);
            talents.style.borderLeftColor = new StyleColor(new Color32(72, 52, 34, 255));
            row.Add(talents);

            talents.Add(Text("永久成长", 16, true));
            _talentsText = Text("", 14, false);
            _talentsText.style.color = new StyleColor(TextMuted);
            _talentsText.style.marginTop = 4;
            _talentsText.style.marginBottom = 8;
            talents.Add(_talentsText);
            _talentActions = new VisualElement();
            _talentActions.style.flexDirection = FlexDirection.Column;
            _talentActions.style.flexWrap = Wrap.Wrap;
            _talentActions.style.flexGrow = 1;
            talents.Add(_talentActions);
        }

        private void RefreshProgression()
        {
            if (_reincarnText == null || _talentsText == null)
            {
                RefreshTalentActions();
                return;
            }
            _reincarnText.text = string.Format(L10n.UISouls, _gameState.Souls, _gameState.MaxFloor, _gameState.CanReincarn);
            _talentsText.text = $"魂点 {_gameState.Souls} · 最高层 {_gameState.MaxFloor}";
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
            string[] desc = { L10n.TalentDamageDesc, L10n.TalentQualityDesc, L10n.TalentDropDesc, L10n.TalentOfflineDesc };
            for (int i = 0; i < TalentKeys.Length; i++)
            {
                int index = i;
                int level = TalentLevel(TalentKeys[index]);
                _talentActions.Add(TalentCard(
                    TalentNames[index],
                    desc[index],
                    level,
                    TalentMax[index],
                    _gameState.Souls,
                    () => _gameState.TalentUp(TalentKeys[index])));
            }
        }

        private static VisualElement TalentCard(string name, string description, int level, int maxLevel, int souls, System.Action action)
        {
            var card = Row();
            card.style.height = 78;
            card.style.marginBottom = 8;
            card.style.paddingLeft = 10;
            card.style.paddingRight = 10;
            card.style.backgroundColor = new StyleColor(new Color32(15, 13, 10, 235));
            card.style.borderLeftWidth = 4;
            card.style.borderLeftColor = new StyleColor(level >= maxLevel ? GoldText : ButtonAscend);
            card.style.borderTopWidth = 1;
            card.style.borderRightWidth = 1;
            card.style.borderBottomWidth = 1;
            card.style.borderTopColor = new StyleColor(new Color32(72, 52, 34, 220));
            card.style.borderRightColor = new StyleColor(new Color32(42, 31, 24, 220));
            card.style.borderBottomColor = new StyleColor(new Color32(31, 24, 19, 220));

            var copy = new VisualElement();
            copy.style.flexGrow = 1;
            var title = Text($"{name} Lv{level}/{maxLevel}", 14, true);
            title.style.color = new StyleColor(GoldText);
            copy.Add(title);
            var desc = Text(description, 11, false);
            desc.style.color = new StyleColor(TextMuted);
            copy.Add(desc);
            copy.Add(TalentProgressBar(level, maxLevel));
            card.Add(copy);

            var button = ActionButton(level >= maxLevel ? "满级" : "升级", action, 68, ButtonAscend);
            button.style.height = 32;
            button.style.alignSelf = Align.Center;
            button.SetEnabled(souls > 0 && level < maxLevel);
            card.Add(button);
            return card;
        }

        private static VisualElement TalentProgressBar(int level, int maxLevel)
        {
            var frame = new VisualElement();
            frame.style.height = 8;
            frame.style.marginTop = 8;
            frame.style.backgroundColor = new StyleColor(new Color32(8, 7, 6, 255));
            frame.style.borderTopLeftRadius = 4;
            frame.style.borderTopRightRadius = 4;
            frame.style.borderBottomLeftRadius = 4;
            frame.style.borderBottomRightRadius = 4;

            var fill = new VisualElement();
            fill.style.height = Length.Percent(100);
            float pct = maxLevel <= 0 ? 0f : Mathf.Clamp01((float)level / maxLevel) * 100f;
            fill.style.width = Length.Percent(pct);
            fill.style.backgroundColor = new StyleColor(level >= maxLevel ? GoldText : ButtonAscend);
            fill.style.borderTopLeftRadius = 4;
            fill.style.borderTopRightRadius = 4;
            fill.style.borderBottomLeftRadius = 4;
            fill.style.borderBottomRightRadius = 4;
            frame.Add(fill);
            return frame;
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
