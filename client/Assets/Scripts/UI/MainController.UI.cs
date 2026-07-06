using EquipmentIdle.Data;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace EquipmentIdle.UI
{
    public partial class MainController
    {
        private struct Toast
        {
            public string Text;
            public float ExpireAt;
        }

        private readonly List<Toast> _toasts = new List<Toast>();
        private static readonly Color32 RootBg = new Color32(8, 10, 10, 255);
        private static readonly Color32 PanelBg = new Color32(20, 21, 19, 255);
        private static readonly Color32 PanelBgWarm = new Color32(30, 25, 20, 255);
        private static readonly Color32 PanelBorder = new Color32(83, 70, 49, 255);
        private static readonly Color32 PanelBorderHot = new Color32(178, 104, 42, 255);
        private static readonly Color32 GoldText = new Color32(244, 202, 121, 255);
        private static readonly Color32 EmberText = new Color32(255, 122, 45, 255);
        private static readonly Color32 GoodText = new Color32(113, 204, 86, 255);
        private static readonly Color32 TextMain = new Color32(238, 229, 207, 255);
        private static readonly Color32 TextMuted = new Color32(178, 165, 139, 255);
        private static readonly Color32 ButtonDefault = new Color32(67, 49, 31, 255);
        private static readonly Color32 ButtonEquip = new Color32(48, 97, 38, 255);
        private static readonly Color32 ButtonCraft = new Color32(130, 84, 22, 255);
        private static readonly Color32 ButtonReforge = new Color32(28, 76, 120, 255);
        private static readonly Color32 ButtonDanger = new Color32(110, 36, 30, 255);
        private static readonly Color32 ButtonAscend = new Color32(116, 55, 22, 255);


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

        private static Image IconImage(Texture2D texture, float width, float height)
        {
            var image = new Image { image = texture, scaleMode = ScaleMode.ScaleAndCrop };
            image.style.width = width;
            image.style.height = height;
            image.style.backgroundColor = new StyleColor(new Color32(9, 8, 7, 255));
            image.style.borderTopWidth = 1;
            image.style.borderRightWidth = 1;
            image.style.borderBottomWidth = 1;
            image.style.borderLeftWidth = 1;
            image.style.borderTopColor = new StyleColor(PanelBorderHot);
            image.style.borderRightColor = new StyleColor(new Color32(72, 45, 28, 255));
            image.style.borderBottomColor = new StyleColor(new Color32(35, 23, 16, 255));
            image.style.borderLeftColor = new StyleColor(PanelBorderHot);
            return image;
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
            el.style.overflow = Overflow.Hidden;
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

        private static Color32 RarityGlowColor(int r)
        {
            switch (r)
            {
                case 1: return new Color32(83, 178, 255, 255);
                case 2: return new Color32(255, 173, 63, 255);
                case 3: return new Color32(255, 111, 37, 255);
                case 4: return new Color32(255, 96, 207, 255);
                default: return new Color32(126, 111, 88, 255);
            }
        }

        private static Color32 RarityTileBackground(int r)
        {
            switch (r)
            {
                case 1: return new Color32(14, 29, 43, 245);
                case 2: return new Color32(42, 25, 9, 245);
                case 3: return new Color32(55, 20, 8, 245);
                case 4: return new Color32(46, 18, 44, 245);
                default: return new Color32(17, 15, 12, 245);
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


        private static Button ActionButton(string label, System.Action action, float width, Color32 color)
        {
            var button = new Button(action) { text = label };
            button.style.height = 38;
            button.style.backgroundColor = new StyleColor(color);
            button.style.color = new StyleColor(new Color32(255, 247, 237, 255));
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.borderTopWidth = 2;
            button.style.borderRightWidth = 2;
            button.style.borderBottomWidth = 2;
            button.style.borderLeftWidth = 2;
            button.style.borderTopColor = new StyleColor(new Color32(218, 156, 63, 255));
            button.style.borderRightColor = new StyleColor(new Color32(70, 48, 31, 255));
            button.style.borderBottomColor = new StyleColor(new Color32(37, 25, 18, 255));
            button.style.borderLeftColor = new StyleColor(new Color32(218, 156, 63, 255));
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
    }
}
