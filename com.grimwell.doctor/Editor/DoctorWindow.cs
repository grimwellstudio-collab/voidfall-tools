using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Grimwell.Doctor
{
    public class DoctorWindow : EditorWindow
    {
        static readonly Color BgColor = new Color(0.13f, 0.13f, 0.15f);
        static readonly Color CardColor = new Color(0.19f, 0.19f, 0.22f);
        static readonly Color AccentColor = new Color(0.35f, 0.55f, 0.95f);
        static readonly Color PassGreen = new Color(0.30f, 0.80f, 0.40f);
        static readonly Color WarnOrange = new Color(0.95f, 0.65f, 0.25f);
        static readonly Color FailRed = new Color(0.90f, 0.35f, 0.35f);
        static readonly Color TextDim = new Color(0.65f, 0.65f, 0.70f);
        static readonly Color TextBright = new Color(0.92f, 0.92f, 0.95f);

        VisualElement _resultsBox;

        [MenuItem("Grimwell/Setup Doctor")]
        public static void Open()
        {
            var window = GetWindow<DoctorWindow>("Setup Doctor");
            window.minSize = new Vector2(340, 420);
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.backgroundColor = BgColor;
            root.style.paddingLeft = 12;
            root.style.paddingRight = 12;
            root.style.paddingTop = 10;
            root.style.paddingBottom = 10;

            var header = Row(root);
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;
            header.style.marginBottom = 10;

            var title = new Label("SETUP DOCTOR");
            title.style.fontSize = 15;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = TextBright;
            title.style.letterSpacing = 2;
            header.Add(title);

            var runButton = new Button(RunChecks) { text = "Run Checks" };
            StyleButton(runButton, AccentColor);
            header.Add(runButton);

            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;
            root.Add(scroll);

            _resultsBox = new VisualElement();
            scroll.Add(_resultsBox);

            RunChecks();
        }

        void RunChecks()
        {
            var results = Checks.RunAll();
            RebuildResults(results);
        }

        void RebuildResults(List<CheckResult> results)
        {
            _resultsBox.Clear();
            foreach (var result in results)
            {
                var card = Card(_resultsBox);

                var topRow = Row(card);
                topRow.style.alignItems = Align.Center;

                var glyph = new Label(GlyphFor(result.Status));
                glyph.style.color = ColorFor(result.Status);
                glyph.style.unityFontStyleAndWeight = FontStyle.Bold;
                glyph.style.marginRight = 8;
                topRow.Add(glyph);

                var name = new Label(result.Name);
                name.style.unityFontStyleAndWeight = FontStyle.Bold;
                name.style.color = TextBright;
                name.style.flexGrow = 1;
                topRow.Add(name);

                if (result.Fix != null && result.Status == Status.Fail)
                {
                    var fixButton = new Button(() =>
                    {
                        result.Fix();
                        RunChecks();
                    }) { text = "Fix" };
                    StyleButton(fixButton, AccentColor);
                    topRow.Add(fixButton);
                }

                var message = new Label(result.Message);
                message.style.color = TextDim;
                message.style.fontSize = 10;
                message.style.marginTop = 4;
                card.Add(message);
            }
        }

        static string GlyphFor(Status status)
        {
            switch (status)
            {
                case Status.Pass: return "[ok]";
                case Status.Warn: return "[!]";
                default: return "[x]";
            }
        }

        static Color ColorFor(Status status)
        {
            switch (status)
            {
                case Status.Pass: return PassGreen;
                case Status.Warn: return WarnOrange;
                default: return FailRed;
            }
        }

        // ----- helpers -----

        static VisualElement Row(VisualElement parent)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            parent.Add(row);
            return row;
        }

        static VisualElement Card(VisualElement parent)
        {
            var card = new VisualElement();
            card.style.backgroundColor = CardColor;
            SetRadius(card, 8);
            card.style.paddingLeft = 10;
            card.style.paddingRight = 10;
            card.style.paddingTop = 8;
            card.style.paddingBottom = 8;
            card.style.marginBottom = 6;
            parent.Add(card);
            return card;
        }

        static void StyleButton(Button button, Color color)
        {
            button.style.backgroundColor = color;
            button.style.color = Color.white;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            SetRadius(button, 6);
            button.style.paddingLeft = 12;
            button.style.paddingRight = 12;
            button.style.paddingTop = 5;
            button.style.paddingBottom = 5;
            button.style.borderTopWidth = 0;
            button.style.borderBottomWidth = 0;
            button.style.borderLeftWidth = 0;
            button.style.borderRightWidth = 0;
        }

        static void SetRadius(VisualElement ve, float radius)
        {
            ve.style.borderTopLeftRadius = radius;
            ve.style.borderTopRightRadius = radius;
            ve.style.borderBottomLeftRadius = radius;
            ve.style.borderBottomRightRadius = radius;
        }
    }
}
