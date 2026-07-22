using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Grimwell.SessionBoard
{
    public class SessionBoardWindow : EditorWindow
    {
        const double RefreshSeconds = 2;
        static readonly TimeSpan OnlineWindow = TimeSpan.FromSeconds(30);

        static readonly Color BgColor = new Color(0.13f, 0.13f, 0.15f);
        static readonly Color CardColor = new Color(0.19f, 0.19f, 0.22f);
        static readonly Color AccentColor = new Color(0.35f, 0.55f, 0.95f);
        static readonly Color DiscordColor = new Color(0.35f, 0.40f, 0.90f);
        static readonly Color OnlineGreen = new Color(0.30f, 0.80f, 0.40f);
        static readonly Color PlayOrange = new Color(0.95f, 0.65f, 0.25f);
        static readonly Color AwayGray = new Color(0.5f, 0.5f, 0.5f);
        static readonly Color TextDim = new Color(0.65f, 0.65f, 0.70f);
        static readonly Color TextBright = new Color(0.92f, 0.92f, 0.95f);

        double _nextRefresh;
        long _latestToastedTicks;
        List<PresenceState> _presence = new List<PresenceState>();
        List<FeedEvent> _feed = new List<FeedEvent>();
        List<ClaimEntry> _claims = new List<ClaimEntry>();
        readonly HashSet<string> _collisionsWarned = new HashSet<string>();

        VisualElement _teamBox;
        VisualElement _feedBox;
        VisualElement _piecesBox;
        Label _piecesSectionLabel;
        Label _emptyHint;
        Label _meStatsLabel;

        [MenuItem("Window/Grimwell/Session Board")]
        public static void Open()
        {
            var window = GetWindow<SessionBoardWindow>("Session Board");
            window.minSize = new Vector2(300, 400);
        }

        void OnEnable()
        {
            _latestToastedTicks = DateTime.UtcNow.Ticks;
            EditorApplication.update += Tick;
        }

        void OnDisable() => EditorApplication.update -= Tick;

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.backgroundColor = BgColor;
            root.style.paddingLeft = 12;
            root.style.paddingRight = 12;
            root.style.paddingTop = 10;
            root.style.paddingBottom = 10;

            // Header: title + Discord button
            var header = Row(root);
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;
            header.style.marginBottom = 10;

            var title = new Label("SESSION BOARD");
            title.style.fontSize = 15;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = TextBright;
            title.style.letterSpacing = 2;
            header.Add(title);

            var headerButtons = Row(header);
            headerButtons.style.alignItems = Align.Center;

            var join = new Button(JoinSession) { text = "Join Session" };
            StyleButton(join, AccentColor);
            join.style.marginRight = 6;
            headerButtons.Add(join);

            var discord = new Button(() =>
            {
                if (string.IsNullOrEmpty(BoardSettings.DiscordUrl))
                    ShowNotification(new GUIContent("Set your team's Discord link in Settings below."));
                else
                    Application.OpenURL(BoardSettings.DiscordUrl);
            }) { text = "Join Discord Call" };
            StyleButton(discord, DiscordColor);
            headerButtons.Add(discord);

            // Me: avatar + name/status fields
            var me = Card(root);
            var meRow = Row(me);
            meRow.style.alignItems = Align.Center;
            meRow.Add(Avatar(BoardSettings.UserName, 36));

            var meFields = new VisualElement { style = { flexGrow = 1, marginLeft = 10 } };
            var nameField = new TextField { value = BoardSettings.UserName, tooltip = "Your display name" };
            StyleField(nameField);
            nameField.RegisterValueChangedCallback(e => BoardSettings.UserName = e.newValue);
            meFields.Add(nameField);

            var statusField = new TextField { value = BoardSettings.StatusLine, tooltip = "What are you working on?" };
            StyleField(statusField);
            statusField.RegisterValueChangedCallback(e => BoardSettings.StatusLine = e.newValue);
            var hint = new Label("your name  /  what you're working on");
            hint.style.color = TextDim;
            hint.style.fontSize = 9;
            meFields.Add(statusField);
            meFields.Add(hint);

            _meStatsLabel = new Label();
            _meStatsLabel.style.color = TextDim;
            _meStatsLabel.style.fontSize = 9;
            meFields.Add(_meStatsLabel);
            UpdateMeStats();

            meRow.Add(meFields);

            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;
            root.Add(scroll);

            scroll.Add(SectionLabel("TEAM"));
            _emptyHint = new Label("No teammates online yet.");
            _emptyHint.style.color = TextDim;
            _emptyHint.style.marginLeft = 4;
            _emptyHint.style.marginBottom = 6;
            scroll.Add(_emptyHint);
            _teamBox = new VisualElement();
            scroll.Add(_teamBox);

            _piecesSectionLabel = SectionLabel("PIECES");
            scroll.Add(_piecesSectionLabel);
            _piecesBox = new VisualElement();
            scroll.Add(_piecesBox);

            scroll.Add(SectionLabel("ACTIVITY"));
            _feedBox = new VisualElement();
            scroll.Add(_feedBox);

            root.Add(BuildSettings());
        }

        VisualElement BuildSettings()
        {
            var fold = new Foldout { text = "Settings", value = false };
            fold.style.marginTop = 8;
            fold.style.color = TextDim;

            var mode = new DropdownField("Sync", new List<string> { "Online relay", "Shared folder" },
                BoardSettings.OnlineMode ? 0 : 1);
            mode.RegisterValueChangedCallback(e => BoardSettings.OnlineMode = e.newValue == "Online relay");
            fold.Add(mode);

            var server = new TextField("Relay URL") { value = BoardSettings.ServerUrl };
            server.RegisterValueChangedCallback(e => BoardSettings.ServerUrl = e.newValue);
            fold.Add(server);

            var room = new TextField("Room") { value = BoardSettings.Room };
            room.RegisterValueChangedCallback(e => BoardSettings.Room = e.newValue);
            fold.Add(room);

            var key = new TextField("Team key") { value = BoardSettings.TeamKey, isPasswordField = true };
            key.RegisterValueChangedCallback(e => BoardSettings.TeamKey = e.newValue);
            fold.Add(key);

            var folder = new TextField("Shared folder") { value = BoardSettings.SharedFolder };
            folder.RegisterValueChangedCallback(e => BoardSettings.SharedFolder = e.newValue);
            fold.Add(folder);

            var discordUrl = new TextField("Discord link") { value = BoardSettings.DiscordUrl };
            discordUrl.RegisterValueChangedCallback(e => BoardSettings.DiscordUrl = e.newValue);
            fold.Add(discordUrl);

            var muteToggle = new Toggle("Mute popups") { value = BoardSettings.MuteToasts };
            muteToggle.RegisterValueChangedCallback(e => BoardSettings.MuteToasts = e.newValue);
            fold.Add(muteToggle);

            return fold;
        }

        void JoinSession()
        {
            var def = SessionDefinition.FindFirst();
            if (def == null)
            {
                ShowNotification(new GUIContent("No Session Definition found — create one via Grimwell menu."));
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scenePaths = (def.pieces ?? new List<SceneEntry>())
                .Where(p => p.scene != null)
                .Select(p => AssetDatabase.GetAssetPath(p.scene))
                .ToList();
            if (scenePaths.Count == 0) return;

            EditorSceneManager.OpenScene(scenePaths[0], OpenSceneMode.Single);
            for (var i = 1; i < scenePaths.Count; i++)
                EditorSceneManager.OpenScene(scenePaths[i], OpenSceneMode.Additive);

            PresencePublisher.PublishEvent("joined " + def.sessionName);
        }

        void Tick()
        {
            if (EditorApplication.timeSinceStartup < _nextRefresh) return;
            _nextRefresh = EditorApplication.timeSinceStartup + RefreshSeconds;

            _presence = PresencePublisher.Transport.ReadAllPresence();
            _feed = PresencePublisher.Transport.ReadRecentEvents(30);
            _claims = PresencePublisher.Transport.ReadClaims();
            RebuildTeam();
            RebuildFeed();
            RebuildPieces();
            ToastNewActivity();
            WarnOnSceneCollisions();
            UpdateMeStats();
        }

        void UpdateMeStats()
        {
            if (_meStatsLabel == null) return;
            var minutes = PresencePublisher.WeekMinutes;
            var saves = PresencePublisher.WeekSaves;
            var playtests = PresencePublisher.WeekPlaytests;
            if (minutes == 0 && saves == 0 && playtests == 0)
            {
                _meStatsLabel.style.display = DisplayStyle.None;
                return;
            }
            _meStatsLabel.style.display = DisplayStyle.Flex;
            _meStatsLabel.text = $"this week: {(minutes / 60f):0.0}h · {saves} saves · {playtests} playtests";
        }

        void RebuildTeam()
        {
            if (_teamBox == null) return;
            _teamBox.Clear();
            var others = _presence
                .Where(p => p.userName != BoardSettings.UserName)
                .OrderByDescending(p => p.heartbeatUtcTicks)
                .ToList();
            _emptyHint.style.display = others.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

            foreach (var p in others)
            {
                var card = Card(_teamBox);
                var row = Row(card);
                row.style.alignItems = Align.Center;
                row.Add(Avatar(p.userName, 32));

                var info = new VisualElement { style = { flexGrow = 1, marginLeft = 10 } };
                var nameRow = Row(info);
                nameRow.style.alignItems = Align.Center;

                var dotColor = IsOnline(p) ? (p.inPlayMode ? PlayOrange : OnlineGreen) : AwayGray;
                var dot = new VisualElement();
                dot.style.width = 8; dot.style.height = 8;
                SetRadius(dot, 4);
                dot.style.backgroundColor = dotColor;
                dot.style.marginRight = 6;
                nameRow.Add(dot);

                var name = new Label(p.userName);
                name.style.unityFontStyleAndWeight = FontStyle.Bold;
                name.style.color = TextBright;
                nameRow.Add(name);

                var stateText = new Label(IsOnline(p) ? (p.inPlayMode ? "playtesting" : "online") : "away " + Ago(p.HeartbeatUtc));
                stateText.style.color = TextDim;
                stateText.style.fontSize = 10;
                stateText.style.marginLeft = 8;
                nameRow.Add(stateText);

                if (!string.IsNullOrEmpty(p.statusLine))
                {
                    var status = new Label("“" + p.statusLine + "”");
                    status.style.color = new Color(0.8f, 0.8f, 0.85f);
                    status.style.unityFontStyleAndWeight = FontStyle.Italic;
                    status.style.fontSize = 11;
                    info.Add(status);
                }

                var sceneName = string.IsNullOrEmpty(p.openScene)
                    ? "no scene"
                    : System.IO.Path.GetFileNameWithoutExtension(p.openScene);
                var detail = "Scene: " + sceneName +
                             (string.IsNullOrEmpty(p.selection) ? "" : "    Editing: " + p.selection);
                var detailLabel = new Label(detail);
                detailLabel.style.color = TextDim;
                detailLabel.style.fontSize = 10;
                info.Add(detailLabel);

                if (p.weekMinutes > 0 || p.weekSaves > 0 || p.weekPlaytests > 0)
                {
                    var statsLabel = new Label($"this week: {(p.weekMinutes / 60f):0.0}h · {p.weekSaves} saves · {p.weekPlaytests} playtests");
                    statsLabel.style.color = TextDim;
                    statsLabel.style.fontSize = 9;
                    info.Add(statsLabel);
                }

                row.Add(info);
            }
        }

        void RebuildFeed()
        {
            if (_feedBox == null) return;
            _feedBox.Clear();
            foreach (var e in _feed)
            {
                var row = Row(_feedBox);
                row.style.marginBottom = 2;
                var time = new Label(e.Utc.ToLocalTime().ToString("HH:mm"));
                time.style.color = TextDim;
                time.style.fontSize = 10;
                time.style.width = 38;
                row.Add(time);
                var text = new Label(e.userName + " " + e.message);
                text.style.color = new Color(0.8f, 0.8f, 0.85f);
                text.style.fontSize = 10;
                row.Add(text);
            }
        }

        void RebuildPieces()
        {
            if (_piecesBox == null) return;
            var def = SessionDefinition.FindFirst();
            if (def == null || def.pieces == null || def.pieces.Count == 0)
            {
                _piecesSectionLabel.style.display = DisplayStyle.None;
                _piecesBox.style.display = DisplayStyle.None;
                _piecesBox.Clear();
                return;
            }
            _piecesSectionLabel.style.display = DisplayStyle.Flex;
            _piecesBox.style.display = DisplayStyle.Flex;
            _piecesBox.Clear();

            foreach (var piece in def.pieces)
            {
                if (piece.scene == null) continue;
                var path = AssetDatabase.GetAssetPath(piece.scene);
                var claim = _claims.FirstOrDefault(c => c.item == path);

                var card = Card(_piecesBox);
                var row = Row(card);
                row.style.alignItems = Align.Center;

                var name = new Label(piece.scene.name);
                name.style.unityFontStyleAndWeight = FontStyle.Bold;
                name.style.color = TextBright;
                row.Add(name);

                var claimLabel = new Label(claim == null ? "unclaimed" : claim.userName);
                claimLabel.style.color = claim == null ? TextDim : ColorFor(claim.userName);
                claimLabel.style.fontSize = 10;
                claimLabel.style.marginLeft = 8;
                row.Add(claimLabel);

                var spacer = new VisualElement { style = { flexGrow = 1 } };
                row.Add(spacer);

                if (claim == null)
                {
                    var claimBtn = new Button(() => PresencePublisher.Transport.PublishClaim(new ClaimEntry
                    {
                        item = path,
                        userName = BoardSettings.UserName,
                        utcTicks = DateTime.UtcNow.Ticks,
                    }))
                    { text = "Claim" };
                    StyleButton(claimBtn, AccentColor);
                    row.Add(claimBtn);
                }
                else if (claim.userName == BoardSettings.UserName)
                {
                    var releaseBtn = new Button(() => PresencePublisher.Transport.ReleaseClaim(new ClaimEntry
                    {
                        item = path,
                        userName = BoardSettings.UserName,
                    }))
                    { text = "Release" };
                    StyleButton(releaseBtn, AwayGray);
                    row.Add(releaseBtn);
                }
            }
        }

        void ToastNewActivity()
        {
            var muted = BoardSettings.MuteToasts;
            foreach (var e in _feed)
            {
                if (e.utcTicks <= _latestToastedTicks || e.userName == BoardSettings.UserName) continue;
                if (!muted) ShowNotification(new GUIContent(e.userName + " " + e.message), 2.5);
            }
            if (_feed.Count > 0)
                _latestToastedTicks = Math.Max(_latestToastedTicks, _feed.Max(e => e.utcTicks));
        }

        void WarnOnSceneCollisions()
        {
            var me = BoardSettings.UserName;
            var myScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            if (string.IsNullOrEmpty(myScene)) return;

            foreach (var p in _presence)
            {
                if (p.userName == me || !IsOnline(p) || p.openScene != myScene) continue;
                if (!_collisionsWarned.Add(p.userName + "|" + myScene)) continue;

                var msg = p.userName + " has this scene open too.\nUnity scenes don't merge — coordinate before saving.";
                ShowNotification(new GUIContent(msg), 5);
                if (SceneView.lastActiveSceneView != null)
                    SceneView.lastActiveSceneView.ShowNotification(new GUIContent(msg), 5);
                Debug.LogWarning("[Session Board] " + p.userName + " is editing the same scene (" + myScene + "). Coordinate before saving.");
            }

            foreach (var c in _claims)
            {
                if (c.userName == me || c.item != myScene) continue;
                if (!_collisionsWarned.Add(c.userName + "|" + myScene)) continue;

                var msg = c.userName + " has claimed this piece.\nCheck with them before editing.";
                ShowNotification(new GUIContent(msg), 5);
                if (SceneView.lastActiveSceneView != null)
                    SceneView.lastActiveSceneView.ShowNotification(new GUIContent(msg), 5);
                Debug.LogWarning("[Session Board] " + c.userName + " has claimed this piece (" + myScene + "). Check with them before editing.");
            }
        }

        // ----- helpers -----

        static bool IsOnline(PresenceState p) => DateTime.UtcNow - p.HeartbeatUtc < OnlineWindow;

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

        static Label SectionLabel(string text)
        {
            var label = new Label(text);
            label.style.color = TextDim;
            label.style.fontSize = 10;
            label.style.letterSpacing = 2;
            label.style.marginTop = 8;
            label.style.marginBottom = 4;
            return label;
        }

        static VisualElement Avatar(string name, int size)
        {
            var initials = new string((name ?? "?")
                .Split(new[] { ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries)
                .Take(2).Select(w => char.ToUpperInvariant(w[0])).ToArray());
            if (initials.Length == 0) initials = "?";

            var avatar = new Label(initials);
            avatar.style.width = size;
            avatar.style.height = size;
            SetRadius(avatar, size / 2f);
            avatar.style.backgroundColor = ColorFor(name);
            avatar.style.color = Color.white;
            avatar.style.unityTextAlign = TextAnchor.MiddleCenter;
            avatar.style.unityFontStyleAndWeight = FontStyle.Bold;
            avatar.style.fontSize = size / 2 - 2;
            avatar.style.flexShrink = 0;
            return avatar;
        }

        static Color ColorFor(string name)
        {
            var hash = 17;
            foreach (var c in name ?? "") hash = hash * 31 + c;
            var hue = Mathf.Abs(hash % 360) / 360f;
            return Color.HSVToRGB(hue, 0.55f, 0.75f);
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

        static void StyleField(TextField field)
        {
            field.style.marginBottom = 2;
        }

        static void SetRadius(VisualElement ve, float radius)
        {
            ve.style.borderTopLeftRadius = radius;
            ve.style.borderTopRightRadius = radius;
            ve.style.borderBottomLeftRadius = radius;
            ve.style.borderBottomRightRadius = radius;
        }

        static string Ago(DateTime utc)
        {
            var span = DateTime.UtcNow - utc;
            if (span.TotalSeconds < 60) return "just now";
            if (span.TotalMinutes < 60) return ((int)span.TotalMinutes) + " min ago";
            if (span.TotalHours < 24) return ((int)span.TotalHours) + " h ago";
            return utc.ToLocalTime().ToString("MMM d");
        }
    }
}
