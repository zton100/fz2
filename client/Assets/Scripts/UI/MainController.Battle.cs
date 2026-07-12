using EquipmentIdle.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace EquipmentIdle.UI
{
    public partial class MainController
    {
        private void BuildDungeonPanel(VisualElement root)
        {
            var dungeon = Panel("dungeon");
            _dungeonPanel = dungeon;
            dungeon.style.minHeight = 760;
            dungeon.style.flexGrow = 1;
            dungeon.style.marginBottom = 8;
            dungeon.style.flexDirection = FlexDirection.Column;
            dungeon.style.backgroundColor = new StyleColor(new Color32(7, 8, 8, 255));
            dungeon.style.paddingLeft = 0;
            dungeon.style.paddingRight = 0;
            dungeon.style.paddingTop = 0;
            dungeon.style.paddingBottom = 0;
            root.Add(dungeon);

            var bossRow = Row();
            bossRow.style.height = 66;
            bossRow.style.paddingLeft = 18;
            bossRow.style.paddingRight = 18;
            bossRow.style.marginBottom = 0;
            bossRow.style.backgroundColor = new StyleColor(new Color32(9, 7, 6, 235));
            _dungeonTitleText = Text("", 23, true);
            _dungeonTitleText.style.flexGrow = 1;
            _dungeonTitleText.style.color = new StyleColor(new Color32(255, 214, 138, 255));
            _dungeonTitleText.style.unityTextAlign = TextAnchor.MiddleLeft;
            bossRow.Add(_dungeonTitleText);
            _monsterText = Text("", 14, true);
            _monsterText.style.unityTextAlign = TextAnchor.MiddleRight;
            _monsterText.style.color = new StyleColor(TextMuted);
            bossRow.Add(_monsterText);
            dungeon.Add(bossRow);

            var progressFrame = new VisualElement();
            progressFrame.style.height = 18;
            progressFrame.style.marginLeft = 20;
            progressFrame.style.marginRight = 20;
            progressFrame.style.marginTop = 10;
            progressFrame.style.marginBottom = 10;
            progressFrame.style.backgroundColor = new StyleColor(new Color32(10, 8, 7, 255));
            progressFrame.style.borderTopWidth = 2;
            progressFrame.style.borderRightWidth = 2;
            progressFrame.style.borderBottomWidth = 2;
            progressFrame.style.borderLeftWidth = 2;
            progressFrame.style.borderTopColor = new StyleColor(new Color32(66, 52, 42, 255));
            progressFrame.style.borderRightColor = new StyleColor(new Color32(66, 52, 42, 255));
            progressFrame.style.borderBottomColor = new StyleColor(new Color32(24, 19, 16, 255));
            progressFrame.style.borderLeftColor = new StyleColor(new Color32(66, 52, 42, 255));
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
            combatFooter.style.height = 84;
            combatFooter.style.paddingLeft = 18;
            combatFooter.style.paddingRight = 18;
            combatFooter.style.paddingTop = 10;
            combatFooter.style.paddingBottom = 10;
            combatFooter.style.backgroundColor = new StyleColor(new Color32(9, 8, 7, 235));
            combatFooter.style.alignItems = Align.FlexStart;
            _battleText = Text("", 16, true);
            _battleText.style.color = new StyleColor(GoldText);
            _stuckText = Text("", 13, false);
            _stuckText.style.color = new StyleColor(new Color32(248, 113, 113, 255));
            _goalText = Text("", 13, false);
            _bossHintText = Text("", 13, true);
            _bossHintText.style.color = new StyleColor(EmberText);
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
            row.style.height = 160;
            row.style.marginBottom = 8;
            row.style.alignItems = Align.Stretch;
            root.Add(row);

            _objectiveCardText = StatusCard(row, "当前目标", "连接后开始自动战斗。");

            var bossCard = Panel("boss-progress-card");
            bossCard.style.flexGrow = 1;
            bossCard.style.marginRight = 10;
            bossCard.Add(SectionTitle("BOSS 进度"));
            _bossProgressCardText = Text("每 5 层出现 Boss。", 13, true);
            _bossProgressCardText.style.flexGrow = 1;
            bossCard.Add(_bossProgressCardText);
            _progressNodesContent = Row();
            _progressNodesContent.style.height = 34;
            _progressNodesContent.style.alignItems = Align.Center;
            bossCard.Add(_progressNodesContent);
            row.Add(bossCard);

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

        private VisualElement BuildBattleStage()
        {
            var stage = new VisualElement();
            stage.style.flexGrow = 1;
            stage.style.marginLeft = 12;
            stage.style.marginRight = 12;
            stage.style.paddingLeft = 0;
            stage.style.paddingRight = 0;
            stage.style.paddingTop = 0;
            stage.style.paddingBottom = 0;
            stage.style.backgroundColor = new StyleColor(new Color32(7, 7, 7, 255));
            stage.style.overflow = Overflow.Hidden;
            if (_battleBackground != null)
            {
                var bg = new Image { image = _battleBackground, scaleMode = ScaleMode.ScaleAndCrop };
                bg.style.position = Position.Absolute;
                bg.style.left = 0;
                bg.style.right = 0;
                bg.style.top = 0;
                bg.style.bottom = 0;
                bg.style.opacity = 1f;
                stage.Add(bg);
            }
            if (_heroSprite != null)
            {
                _stageHeroSpriteImage = StageSprite(_heroSprite, 330, 380, 26, null, ScaleMode.ScaleToFit);
                stage.Add(_stageHeroSpriteImage);
            }
            if (_bossSprite != null)
            {
                _stageBossSpriteImage = StageSprite(_bossSprite, 430, 430, null, -8, ScaleMode.ScaleToFit);
                stage.Add(_stageBossSpriteImage);
            }
            _stageImpactText = Text("", 24, true);
            _stageImpactText.style.position = Position.Absolute;
            _stageImpactText.style.left = 0;
            _stageImpactText.style.right = 0;
            _stageImpactText.style.top = 220;
            _stageImpactText.style.unityTextAlign = TextAnchor.MiddleCenter;
            _stageImpactText.style.color = new StyleColor(new Color32(255, 224, 137, 255));
            _stageImpactText.style.opacity = 0f;
            stage.Add(_stageImpactText);
            _stageArtifactText = Text("", 17, true);
            _stageArtifactText.style.position = Position.Absolute;
            _stageArtifactText.style.left = 58;
            _stageArtifactText.style.right = 58;
            _stageArtifactText.style.top = 126;
            _stageArtifactText.style.height = 32;
            _stageArtifactText.style.unityTextAlign = TextAnchor.MiddleCenter;
            _stageArtifactText.style.color = new StyleColor(new Color32(255, 209, 102, 255));
            _stageArtifactText.style.backgroundColor = new StyleColor(new Color32(92, 18, 18, 210));
            _stageArtifactText.style.borderTopWidth = 1;
            _stageArtifactText.style.borderRightWidth = 1;
            _stageArtifactText.style.borderBottomWidth = 1;
            _stageArtifactText.style.borderLeftWidth = 1;
            _stageArtifactText.style.borderTopColor = new StyleColor(new Color32(248, 113, 113, 255));
            _stageArtifactText.style.borderRightColor = new StyleColor(new Color32(248, 113, 113, 255));
            _stageArtifactText.style.borderBottomColor = new StyleColor(new Color32(251, 191, 36, 255));
            _stageArtifactText.style.borderLeftColor = new StyleColor(new Color32(248, 113, 113, 255));
            _stageArtifactText.style.opacity = 0f;
            stage.Add(_stageArtifactText);
            _stageBannerText = Text("", 19, true);
            _stageBannerText.style.position = Position.Absolute;
            _stageBannerText.style.left = 34;
            _stageBannerText.style.right = 34;
            _stageBannerText.style.top = 74;
            _stageBannerText.style.height = 42;
            _stageBannerText.style.unityTextAlign = TextAnchor.MiddleCenter;
            _stageBannerText.style.color = new StyleColor(new Color32(255, 224, 137, 255));
            _stageBannerText.style.backgroundColor = new StyleColor(new Color32(55, 30, 14, 220));
            _stageBannerText.style.borderTopWidth = 1;
            _stageBannerText.style.borderRightWidth = 1;
            _stageBannerText.style.borderBottomWidth = 1;
            _stageBannerText.style.borderLeftWidth = 1;
            _stageBannerText.style.borderTopColor = new StyleColor(PanelBorderHot);
            _stageBannerText.style.borderRightColor = new StyleColor(PanelBorderHot);
            _stageBannerText.style.borderBottomColor = new StyleColor(PanelBorderHot);
            _stageBannerText.style.borderLeftColor = new StyleColor(PanelBorderHot);
            _stageBannerText.style.opacity = 0f;
            stage.Add(_stageBannerText);
            stage.style.borderTopWidth = 1;
            stage.style.borderRightWidth = 1;
            stage.style.borderBottomWidth = 1;
            stage.style.borderLeftWidth = 1;
            stage.style.borderTopColor = new StyleColor(PanelBorderHot);
            stage.style.borderRightColor = new StyleColor(new Color32(70, 47, 31, 255));
            stage.style.borderBottomColor = new StyleColor(new Color32(42, 27, 19, 255));
            stage.style.borderLeftColor = new StyleColor(new Color32(70, 47, 31, 255));
            stage.style.borderTopLeftRadius = 6;
            stage.style.borderTopRightRadius = 6;
            stage.style.borderBottomLeftRadius = 6;
            stage.style.borderBottomRightRadius = 6;

            _stageZoneText = Text("", 13, true);
            _stageZoneText.style.position = Position.Absolute;
            _stageZoneText.style.left = 22;
            _stageZoneText.style.right = 22;
            _stageZoneText.style.top = 18;
            _stageZoneText.style.height = 26;
            _stageZoneText.style.unityTextAlign = TextAnchor.MiddleCenter;
            _stageZoneText.style.color = new StyleColor(GoldText);
            _stageZoneText.style.backgroundColor = new StyleColor(new Color32(13, 10, 8, 190));
            stage.Add(_stageZoneText);

            var combatants = Row();
            combatants.style.position = Position.Absolute;
            combatants.style.left = 18;
            combatants.style.right = 18;
            combatants.style.bottom = 18;
            combatants.style.height = 84;
            combatants.style.alignItems = Align.Stretch;

            VisualElement heroCard = CombatantCard(
                new Color32(59, 130, 246, 255),
                out _stageHeroNameText,
                out _stageHeroPowerText,
                out _stageHeroHealthFill);
            combatants.Add(heroCard);

            var center = new VisualElement();
            center.style.width = 96;
            center.style.alignItems = Align.Center;
            center.style.justifyContent = Justify.Center;
            _stageSlash = Text("VS", 28, true);
            _stageSlash.style.unityTextAlign = TextAnchor.MiddleCenter;
            _stageSlash.style.color = new StyleColor(new Color32(255, 202, 112, 255));
            _stageSlash.style.backgroundColor = new StyleColor(new Color32(55, 21, 13, 205));
            _stageSlash.style.borderTopWidth = 1;
            _stageSlash.style.borderRightWidth = 1;
            _stageSlash.style.borderBottomWidth = 1;
            _stageSlash.style.borderLeftWidth = 1;
            _stageSlash.style.borderTopColor = new StyleColor(PanelBorderHot);
            _stageSlash.style.borderRightColor = new StyleColor(PanelBorderHot);
            _stageSlash.style.borderBottomColor = new StyleColor(PanelBorderHot);
            _stageSlash.style.borderLeftColor = new StyleColor(PanelBorderHot);
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
            _stageStatusText.style.position = Position.Absolute;
            _stageStatusText.style.left = 0;
            _stageStatusText.style.right = 0;
            _stageStatusText.style.bottom = 112;
            _stageStatusText.style.unityTextAlign = TextAnchor.MiddleCenter;
            _stageStatusText.style.color = new StyleColor(GoldText);
            stage.Add(_stageStatusText);
            return stage;
        }

        private static Image StageSprite(Texture2D texture, float width, float height, float? left, float? right, ScaleMode scaleMode)
        {
            var image = new Image { image = texture, scaleMode = scaleMode };
            image.style.position = Position.Absolute;
            image.style.width = width;
            image.style.height = height;
            image.style.bottom = 54;
            if (left.HasValue) image.style.left = left.Value;
            if (right.HasValue) image.style.right = right.Value;
            image.style.opacity = 1f;
            return image;
        }

        private static VisualElement CombatantCard(Color accent, out Label name, out Label power, out VisualElement healthFill)
        {
            var card = new VisualElement();
            card.style.flexGrow = 1;
            card.style.paddingLeft = 8;
            card.style.paddingRight = 8;
            card.style.paddingTop = 6;
            card.style.paddingBottom = 6;
            card.style.backgroundColor = new StyleColor(new Color32(10, 9, 8, 215));
            card.style.borderTopWidth = 1;
            card.style.borderRightWidth = 1;
            card.style.borderBottomWidth = 3;
            card.style.borderLeftWidth = 1;
            card.style.borderTopColor = new StyleColor(new Color32(68, 52, 40, 220));
            card.style.borderRightColor = new StyleColor(new Color32(68, 52, 40, 220));
            card.style.borderBottomColor = accent;
            card.style.borderLeftColor = new StyleColor(new Color32(68, 52, 40, 220));
            card.style.borderTopLeftRadius = 5;
            card.style.borderTopRightRadius = 5;
            card.style.borderBottomLeftRadius = 5;
            card.style.borderBottomRightRadius = 5;

            name = Text("", 16, true);
            name.style.unityTextAlign = TextAnchor.MiddleCenter;
            name.style.color = new StyleColor(GoldText);
            power = Text("", 12, false);
            power.style.unityTextAlign = TextAnchor.MiddleCenter;
            power.style.color = new StyleColor(TextMain);
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

        private void RefreshDungeon()
        {
            if (_dungeonTitleText == null) return;
            bool showingCombatEvent = _activeCombat != null && Time.realtimeSinceStartup - _combatBeatStartedAt < CombatBeatDuration;
            int displayFloor = showingCombatEvent ? _activeCombat.floor : _gameState.Floor;
            int minionsKilled = showingCombatEvent ? _activeCombat.minions_killed : _gameState.FloorKills;
            int minionsTotal = showingCombatEvent ? _activeCombat.minions_total : _gameState.MinionsTotal;
            string enemyKind = showingCombatEvent
                ? _activeCombat.enemy_kind
                : minionsKilled < minionsTotal ? "minion" : displayFloor % 5 == 0 ? "boss" : "guardian";
            var dungeon = EquipmentPresenter.BuildDungeonState(displayFloor, _gameState.Power);
            bool clearingMinions = enemyKind == "minion";
            bool boss = enemyKind == "boss";
            _bossProgressTarget = clearingMinions
                ? (float)minionsKilled / Mathf.Max(1, minionsTotal)
                : 1f;

            _dungeonTitleText.text = clearingMinions
                ? $"第 {displayFloor} 层 · 清剿 {minionsKilled}/{minionsTotal}"
                : boss ? $"第 {displayFloor} 层 Boss 关 · {dungeon.Zone}" : $"第 {displayFloor} 层守关战 · {dungeon.Zone}";
            float encounterPower = showingCombatEvent ? _activeCombat.enemy_power : clearingMinions ? dungeon.MonsterPower * 0.5f : dungeon.MonsterPower;
            string encounterName = clearingMinions ? "地牢爪牙" : boss ? "守层 Boss" : "守关精英";
            string traits = showingCombatEvent ? EquipmentPresenter.MonsterTraitLine(_activeCombat) : "";
            _monsterText.text = string.IsNullOrEmpty(traits)
                ? $"{encounterName}\n战力 {encounterPower:F1}"
                : $"{encounterName}\n战力 {encounterPower:F1}\n{traits}";
            _battleText.text = clearingMinions
                ? $"小兵进度 {minionsKilled}/{minionsTotal}，清剿完成后进入守关战。"
                : dungeon.Battle;
            _bossHintText.text = dungeon.BossHint;
            _bossHintText.style.color = new StyleColor(boss ? new Color32(255, 92, 64, 255) : EmberText);
            RefreshBattleStage(displayFloor, minionsKilled, minionsTotal, clearingMinions, boss, encounterName, encounterPower);
            string nextGoal = EquipmentPresenter.BuildNextGoal(
                _gameState.Floor,
                _gameState.Power,
                _gameState.CanReincarn,
                _gameState.Bag.Count,
                BaseMaterialCount());
            _goalText.text = nextGoal;
            if (_objectiveCardText != null) _objectiveCardText.text = nextGoal.Replace("目标：", "");
            if (_bossProgressCardText != null) _bossProgressCardText.text = dungeon.BossHint;
            _bossProgressFill.style.backgroundColor = new StyleColor(boss ? new Color32(220, 38, 38, 255) : clearingMinions ? new Color32(59, 130, 246, 255) : new Color32(217, 119, 6, 255));
            RefreshProgressNodes();
        }

        private void RefreshProgressNodes()
        {
            if (_progressNodesContent == null) return;
            _progressNodesContent.Clear();
            foreach (var node in EquipmentPresenter.BuildProgressNodes(_gameState.Floor))
            {
                _progressNodesContent.Add(ProgressNode(node));
            }
        }

        private VisualElement ProgressNode(ProgressNodeState node)
        {
            var box = new Label(node.Label);
            box.style.flexGrow = 1;
            box.style.height = 26;
            box.style.marginLeft = 2;
            box.style.marginRight = 2;
            box.style.unityTextAlign = TextAnchor.MiddleCenter;
            box.style.fontSize = node.IsBoss ? 13 : 12;
            box.style.unityFontStyleAndWeight = node.Current || node.IsBoss ? FontStyle.Bold : FontStyle.Normal;
            box.style.color = new StyleColor(node.Current ? new Color32(255, 247, 237, 255) : node.Passed ? GoldText : TextMuted);
            box.style.backgroundColor = new StyleColor(node.Current
                ? (node.IsBoss ? new Color32(136, 32, 24, 255) : new Color32(126, 72, 24, 255))
                : node.Passed ? new Color32(52, 42, 26, 255) : new Color32(14, 13, 11, 255));
            box.style.borderTopWidth = 1;
            box.style.borderRightWidth = 1;
            box.style.borderBottomWidth = 1;
            box.style.borderLeftWidth = 1;
            Color32 border = node.IsBoss ? new Color32(255, 92, 64, 255) : node.Current ? PanelBorderHot : PanelBorder;
            box.style.borderTopColor = new StyleColor(border);
            box.style.borderRightColor = new StyleColor(border);
            box.style.borderBottomColor = new StyleColor(border);
            box.style.borderLeftColor = new StyleColor(border);
            box.style.borderTopLeftRadius = 4;
            box.style.borderTopRightRadius = 4;
            box.style.borderBottomLeftRadius = 4;
            box.style.borderBottomRightRadius = 4;
            return box;
        }

        private void RefreshBattleStage(int displayFloor, int minionsKilled, int minionsTotal, bool clearingMinions, bool boss, string encounterName, float encounterPower)
        {
            if (_stageHeroNameText == null) return;
            var stage = EquipmentPresenter.BuildBattleStageState(displayFloor, _gameState.Power);
            _stageZoneText.text = stage.Zone;
            _stageHeroNameText.text = stage.HeroName;
            _stageHeroPowerText.text = stage.HeroPower;
            _stageMonsterNameText.text = encounterName;
            _stageMonsterPowerText.text = $"战力 {encounterPower:F1}";
            if (_activeCombat != null && !string.IsNullOrEmpty(_activeCombat.enemy_family))
            {
                _stageMonsterNameText.text = $"{encounterName} · {EquipmentPresenter.MonsterFamilyName(_activeCombat.enemy_family)}";
                _stageMonsterPowerText.text = $"战力 {encounterPower:F1} · {EquipmentPresenter.ElementName(_activeCombat.enemy_element)}";
            }
            _stageStatusText.text = clearingMinions ? $"清剿小兵：{minionsKilled}/{minionsTotal}" : boss ? $"Boss 战：{stage.Status}" : $"守关战：{stage.Status}";
            _stageStatusText.style.color = new StyleColor(stage.Status == "受阻" ? new Color32(248, 113, 113, 255) : GoldText);
            if (_stageSlash != null)
            {
                _stageSlash.text = boss ? "BOSS" : clearingMinions ? $"{minionsKilled}/{minionsTotal}" : "ELITE";
                _stageSlash.style.color = new StyleColor(boss ? new Color32(255, 92, 64, 255) : new Color32(255, 202, 112, 255));
            }
            _stageHeroHealthFill.style.width = Length.Percent(stage.HeroHealth * 100f);
            _stageMonsterHealthFill.style.width = Length.Percent(stage.MonsterHealth * 100f);
            if (_stageArtifactText != null) _stageArtifactText.style.opacity = 0f;
            _stageMonsterHealthFill.style.backgroundColor = new StyleColor(boss ? new Color32(220, 38, 38, 255) : clearingMinions ? new Color32(234, 179, 8, 255) : new Color32(245, 158, 11, 255));
            if (_stageBossSpriteImage != null)
            {
                _stageBossSpriteImage.image = clearingMinions ? _minionSprite : boss ? _bossSprite : _guardianSprite;
                _stageBossSpriteImage.style.width = clearingMinions ? 330 : boss ? 430 : 380;
                _stageBossSpriteImage.style.height = clearingMinions ? 360 : boss ? 430 : 410;
                _stageBossSpriteImage.style.right = clearingMinions ? 18 : -8;
                _stageBossSpriteImage.style.opacity = 1f;
            }
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

            var icon = IconImage(eq != null ? IconForEquipment(eq) : IconForSlot(0), 36, 36);
            icon.style.marginRight = 6;
            row.Add(icon);

            var label = Text(EquipmentPresenter.BuildLootLine(eq), 12, eq != null && eq.rarity >= 2);
            label.style.color = RarityUIColor(eq != null ? eq.rarity : 0);
            label.style.flexGrow = 1;
            row.Add(label);
            return row;
        }

        private void UpdateCombatFeedback()
        {
            float now = Time.realtimeSinceStartup;
            if (_bossProgressFill != null)
            {
                _bossProgressCurrent = Mathf.Lerp(_bossProgressCurrent, _bossProgressTarget, Time.deltaTime * 8f);
                _bossProgressFill.style.width = Length.Percent(_bossProgressCurrent * 100f);
            }

            if (_dungeonPanel != null)
            {
                float pulse = Mathf.Clamp01((_battlePulseUntil - now) / 0.7f);
                Color baseColor = new Color32(7, 8, 8, 255);
                Color hitColor = new Color32(82, 21, 12, 255);
                _dungeonPanel.style.backgroundColor = new StyleColor(Color.Lerp(baseColor, hitColor, pulse));
                if (_stageSlash != null) _stageSlash.style.opacity = 0.65f + pulse * 0.35f;
            }

            RefreshCombatBeat(now);
            if (_combatTransitionPending && now - _combatBeatStartedAt >= CombatBeatDuration)
            {
                _combatTransitionPending = false;
                _activeCombat = null;
                RefreshDungeon();
            }
            RefreshStageBanner(now);

            if (_lootPanel != null)
            {
                float pulse = Mathf.Clamp01((_lootPulseUntil - now) / 0.8f);
                Color baseColor = Color.clear;
                Color lootColor = new Color(0.75f, 0.50f, 0.12f, 0.35f);
                _lootPanel.style.backgroundColor = new StyleColor(Color.Lerp(baseColor, lootColor, pulse));
            }
        }

        private void ShowStageBanner(string text, Color color)
        {
            if (_stageBannerText == null) return;
            _stageBannerText.text = text;
            _stageBannerText.style.color = new StyleColor(color);
            _stageBannerUntil = Time.realtimeSinceStartup + 2.4f;
        }

        private void RefreshStageBanner(float now)
        {
            if (_stageBannerText == null) return;
            float remaining = _stageBannerUntil - now;
            if (remaining <= 0f)
            {
                _stageBannerText.style.opacity = 0f;
                return;
            }
            _stageBannerText.style.opacity = Mathf.Clamp01(remaining / 0.35f);
        }

        private void StartCombatBeat()
        {
            float now = Time.realtimeSinceStartup;
            _combatBeatStartedAt = now;
            _battlePulseUntil = now + 0.7f;
        }

        private void RefreshCombatBeat(float now)
        {
            if (_stageMonsterHealthFill == null) return;
            float elapsed = now - _combatBeatStartedAt;
            int floor = _activeCombat != null ? _activeCombat.floor : _gameState.Floor;
            float playerPower = _activeCombat != null ? _activeCombat.player_power : _gameState.Power;
            var beat = EquipmentPresenter.BuildCombatBeatState(floor, playerPower, elapsed, CombatBeatDuration);
            if (_activeCombat != null && _activeCombat.events != null && _activeCombat.events.Length > 0)
            {
                RefreshServerCombatBeat(elapsed, beat);
                return;
            }
            var stage = EquipmentPresenter.BuildBattleStageState(floor, playerPower);
            float monsterHealth = beat.Active ? beat.MonsterHealth : stage.MonsterHealth;
            if (_activeCombat != null && _activeCombat.win && elapsed >= CombatBeatDuration * 0.62f)
            {
                float deathProgress = Mathf.Clamp01((elapsed - CombatBeatDuration * 0.62f) / (CombatBeatDuration * 0.38f));
                monsterHealth = Mathf.Lerp(monsterHealth, 0f, deathProgress);
            }
            _stageMonsterHealthFill.style.width = Length.Percent(monsterHealth * 100f);

            if (_stageHeroSpriteImage != null)
            {
                _stageHeroSpriteImage.style.left = 34 + beat.HeroOffset;
            }
            if (_stageBossSpriteImage != null)
            {
                float enemyRight = _activeCombat != null && _activeCombat.enemy_kind == "minion" ? 18f : -8f;
                _stageBossSpriteImage.style.right = enemyRight - beat.MonsterOffset;
                if (_activeCombat != null && _activeCombat.win && elapsed >= CombatBeatDuration * 0.62f)
                {
                    float deathProgress = Mathf.Clamp01((elapsed - CombatBeatDuration * 0.62f) / (CombatBeatDuration * 0.38f));
                    _stageBossSpriteImage.style.opacity = Mathf.Lerp(1f, 0.18f, deathProgress);
                }
            }
            if (_stageImpactText != null)
            {
                string hitLabel = _activeCombat != null && _activeCombat.enemy_kind == "boss" ? "重击" : "斩击";
                _stageImpactText.text = beat.Active ? $"{hitLabel} {beat.DamageText}" : "";
                _stageImpactText.style.opacity = beat.ImpactOpacity;
                _stageImpactText.style.top = 172 - beat.HeroOffset * 0.65f;
            }
            if (_stageArtifactText != null) _stageArtifactText.style.opacity = 0f;
        }

        private void RefreshServerCombatBeat(float elapsed, CombatBeatState beat)
        {
            float progress = Mathf.Clamp01(elapsed / CombatBeatDuration);
            int eventIndex = Mathf.Clamp(Mathf.FloorToInt(progress * _activeCombat.events.Length), 0, _activeCombat.events.Length - 1);
            CombatEventData evt = _activeCombat.events[eventIndex];
            float playerMax = Mathf.Max(1f, _activeCombat.player_start_hp + _activeCombat.player_start_shield);
            float enemyMax = Mathf.Max(1f, _activeCombat.enemy_start_hp + _activeCombat.enemy_start_shield);
            float playerCurrent = Mathf.Max(0f, evt.player_hp + evt.player_shield);
            float enemyCurrent = Mathf.Max(0f, evt.enemy_hp + evt.enemy_shield);
            _stageHeroHealthFill.style.width = Length.Percent(Mathf.Clamp01(playerCurrent / playerMax) * 100f);
            _stageMonsterHealthFill.style.width = Length.Percent(Mathf.Clamp01(enemyCurrent / enemyMax) * 100f);

            bool playerHit = evt.actor == "player";
            if (_stageHeroSpriteImage != null)
            {
                _stageHeroSpriteImage.style.left = 34 + (playerHit ? beat.HeroOffset : -beat.MonsterOffset * 0.35f);
            }
            if (_stageBossSpriteImage != null)
            {
                float enemyRight = _activeCombat.enemy_kind == "minion" ? 18f : -8f;
                _stageBossSpriteImage.style.right = enemyRight - (playerHit ? beat.MonsterOffset : beat.HeroOffset * 0.35f);
                if (_activeCombat.win && progress >= 0.88f)
                {
                    float deathProgress = Mathf.Clamp01((progress - 0.88f) / 0.12f);
                    _stageBossSpriteImage.style.opacity = Mathf.Lerp(1f, 0.18f, deathProgress);
                }
            }
            if (_stageImpactText != null)
            {
                string verb = playerHit ? (_activeCombat.enemy_kind == "boss" ? "重击" : "斩击") : "受击";
                string critical = evt.critical ? " 暴击" : "";
                string artifactLabel = EquipmentPresenter.CombatEventLabel(evt.kind, evt.damage);
                bool artifactEvent = !string.IsNullOrEmpty(artifactLabel);
                _stageImpactText.text = artifactEvent ? $"{evt.damage:F0}" : $"{verb}{critical} {evt.damage:F0}";
                _stageImpactText.style.opacity = Mathf.Max(beat.ImpactOpacity, 0.35f);
                _stageImpactText.style.top = artifactEvent ? 166 : playerHit ? 172 - beat.HeroOffset * 0.65f : 188 + beat.MonsterOffset * 0.45f;
                _stageImpactText.style.color = new StyleColor(artifactEvent ? new Color32(255, 209, 102, 255) : playerHit ? new Color32(255, 224, 137, 255) : new Color32(248, 113, 113, 255));
                if (_stageArtifactText != null)
                {
                    _stageArtifactText.text = artifactLabel;
                    _stageArtifactText.style.opacity = artifactEvent ? Mathf.Max(0.42f, beat.ImpactOpacity) : 0f;
                }
            }
        }
    }
}
