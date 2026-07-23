using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Grimwell.SessionBoard
{
    public static class BoardSettings
    {
        const string UserKey = "Grimwell.SessionBoard.UserName";
        const string StatusKey = "Grimwell.SessionBoard.StatusLine";
        const string FolderKey = "Grimwell.SessionBoard.SharedFolder";
        const string ModeKey = "Grimwell.SessionBoard.SyncMode";
        const string ServerKey = "Grimwell.SessionBoard.ServerUrl";
        const string RoomKey = "Grimwell.SessionBoard.Room";
        const string TeamKeyKey = "Grimwell.SessionBoard.TeamKey";
        const string DiscordKey = "Grimwell.SessionBoard.DiscordUrl";
        const string MuteToastsKey = "Grimwell.SessionBoard.MuteToasts";

        public static string DefaultFolder => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Grimwell", "SessionBoard");

        public static string UserName
        {
            get => EditorPrefs.GetString(UserKey, Environment.UserName);
            set { if (value != UserName) EditorPrefs.SetString(UserKey, value); }
        }

        public static string StatusLine
        {
            get => EditorPrefs.GetString(StatusKey, "");
            set { if (value != StatusLine) EditorPrefs.SetString(StatusKey, value); }
        }

        public static string SharedFolder
        {
            get => EditorPrefs.GetString(FolderKey, DefaultFolder);
            set { if (!string.IsNullOrEmpty(value) && value != SharedFolder) EditorPrefs.SetString(FolderKey, value); }
        }

        /// <summary>true = online relay, false = shared folder.</summary>
        public static bool OnlineMode
        {
            get => EditorPrefs.GetBool(ModeKey, true);
            set { if (value != OnlineMode) EditorPrefs.SetBool(ModeKey, value); }
        }

        public const string DefaultServerUrl = "https://session-board-relay.grimwellstudio.workers.dev";

        public static string ServerUrl
        {
            get => EditorPrefs.GetString(ServerKey, DefaultServerUrl);
            set { if (value != ServerUrl) EditorPrefs.SetString(ServerKey, value.Trim()); }
        }

        public static string Room
        {
            get => EditorPrefs.GetString(RoomKey, "voidfall");
            set { if (value != Room) EditorPrefs.SetString(RoomKey, value.Trim()); }
        }

        public static string TeamKey
        {
            get => EditorPrefs.GetString(TeamKeyKey, "");
            set { if (value != TeamKey) EditorPrefs.SetString(TeamKeyKey, value.Trim()); }
        }

        public static string DiscordUrl
        {
            get => EditorPrefs.GetString(DiscordKey, "");
            set { if (value != DiscordUrl) EditorPrefs.SetString(DiscordKey, value.Trim()); }
        }

        public static bool MuteToasts
        {
            get => EditorPrefs.GetBool(MuteToastsKey, false);
            set { if (value != MuteToasts) EditorPrefs.SetBool(MuteToastsKey, value); }
        }
    }

    [InitializeOnLoad]
    public static class PresencePublisher
    {
        const double HeartbeatSeconds = 5;
        static double _nextBeat;
        static IPresenceTransport _transport;
        static string _transportConfig;

        const string StatsWeekKey = "Grimwell.SessionBoard.Stats.Week";
        const string StatsMinutesKey = "Grimwell.SessionBoard.Stats.Week.Minutes";
        const string StatsSavesKey = "Grimwell.SessionBoard.Stats.Week.Saves";
        const string StatsPlaytestsKey = "Grimwell.SessionBoard.Stats.Week.Playtests";
        static float _activeSecondsAccum;

        const string StatsDayKey = "Grimwell.SessionBoard.Stats.Day";
        const string StatsDayMinutesKey = "Grimwell.SessionBoard.Stats.Day.Minutes";
        const string StatsDaySavesKey = "Grimwell.SessionBoard.Stats.Day.Saves";
        const string StatsDayPlaytestsKey = "Grimwell.SessionBoard.Stats.Day.Playtests";
        const string StatsDayScriptEditsKey = "Grimwell.SessionBoard.Stats.Day.ScriptEdits";
        const string StatsDayScriptLinesKey = "Grimwell.SessionBoard.Stats.Day.ScriptLines";
        static float _daySecondsAccum;

        static PresencePublisher()
        {
            EditorApplication.update += OnUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorSceneManager.sceneSaved += OnSceneSaved;
            EditorSceneManager.sceneOpened += OnSceneOpened;

            if (!SessionState.GetBool("Grimwell.SessionBoard.Announced", false))
            {
                SessionState.SetBool("Grimwell.SessionBoard.Announced", true);
                PublishEvent("came online");
            }
        }

        public static IPresenceTransport Transport
        {
            get
            {
                var config = BoardSettings.OnlineMode
                    ? "http|" + BoardSettings.ServerUrl + "|" + BoardSettings.Room + "|" + BoardSettings.TeamKey
                    : "folder|" + BoardSettings.SharedFolder;
                if (_transport == null || _transportConfig != config)
                {
                    _transport = BoardSettings.OnlineMode && !string.IsNullOrEmpty(BoardSettings.ServerUrl)
                        ? (IPresenceTransport)new HttpTransport(BoardSettings.ServerUrl, BoardSettings.Room, BoardSettings.TeamKey)
                        : new SharedFolderTransport(BoardSettings.SharedFolder);
                    _transportConfig = config;
                }
                return _transport;
            }
        }

        static void OnUpdate()
        {
            if (EditorApplication.timeSinceStartup < _nextBeat) return;
            _nextBeat = EditorApplication.timeSinceStartup + HeartbeatSeconds;
            AddActiveSeconds((float)HeartbeatSeconds);
            var transport = Transport;
            transport.PublishPresence(BuildState());
            if (transport is SharedFolderTransport folderTransport)
            {
                EnsureCurrentDay();
                folderTransport.WriteHistoryRollup(new HistoryEntry
                {
                    date = EditorPrefs.GetString(StatsDayKey, ""),
                    userName = BoardSettings.UserName,
                    minutes = EditorPrefs.GetInt(StatsDayMinutesKey, 0),
                    saves = EditorPrefs.GetInt(StatsDaySavesKey, 0),
                    playtests = EditorPrefs.GetInt(StatsDayPlaytestsKey, 0),
                    scriptEdits = EditorPrefs.GetInt(StatsDayScriptEditsKey, 0),
                    scriptLines = EditorPrefs.GetInt(StatsDayScriptLinesKey, 0),
                });
            }
        }

        static PresenceState BuildState()
        {
            EnsureCurrentWeek();
            EnsureCurrentDay();
            return new PresenceState
            {
                userName = BoardSettings.UserName,
                machineName = Environment.MachineName,
                statusLine = BoardSettings.StatusLine,
                openScene = SceneManager.GetActiveScene().path,
                selection = Selection.activeObject != null ? Selection.activeObject.name : "",
                inPlayMode = EditorApplication.isPlaying,
                heartbeatUtcTicks = DateTime.UtcNow.Ticks,
                weekMinutes = EditorPrefs.GetInt(StatsMinutesKey, 0),
                weekSaves = EditorPrefs.GetInt(StatsSavesKey, 0),
                weekPlaytests = EditorPrefs.GetInt(StatsPlaytestsKey, 0),
                dayStamp = EditorPrefs.GetString(StatsDayKey, ""),
                dayMinutes = EditorPrefs.GetInt(StatsDayMinutesKey, 0),
                daySaves = EditorPrefs.GetInt(StatsDaySavesKey, 0),
                dayPlaytests = EditorPrefs.GetInt(StatsDayPlaytestsKey, 0),
                dayScriptEdits = EditorPrefs.GetInt(StatsDayScriptEditsKey, 0),
                dayScriptLines = EditorPrefs.GetInt(StatsDayScriptLinesKey, 0),
            };
        }

        static string IsoWeek(DateTime date)
        {
            var day = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(date);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday) date = date.AddDays(3);
            var week = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            return date.Year + "-W" + week.ToString("D2");
        }

        static void EnsureCurrentWeek()
        {
            var week = IsoWeek(DateTime.Now);
            if (EditorPrefs.GetString(StatsWeekKey, "") == week) return;
            EditorPrefs.SetString(StatsWeekKey, week);
            EditorPrefs.SetInt(StatsMinutesKey, 0);
            EditorPrefs.SetInt(StatsSavesKey, 0);
            EditorPrefs.SetInt(StatsPlaytestsKey, 0);
            _activeSecondsAccum = 0;
        }

        static void EnsureCurrentDay()
        {
            var day = DateTime.Now.ToString("yyyy-MM-dd");
            if (EditorPrefs.GetString(StatsDayKey, "") == day) return;
            EditorPrefs.SetString(StatsDayKey, day);
            EditorPrefs.SetInt(StatsDayMinutesKey, 0);
            EditorPrefs.SetInt(StatsDaySavesKey, 0);
            EditorPrefs.SetInt(StatsDayPlaytestsKey, 0);
            EditorPrefs.SetInt(StatsDayScriptEditsKey, 0);
            EditorPrefs.SetInt(StatsDayScriptLinesKey, 0);
            _daySecondsAccum = 0;
        }

        static void AddActiveSeconds(float seconds)
        {
            EnsureCurrentWeek();
            _activeSecondsAccum += seconds;
            if (_activeSecondsAccum >= 60f)
            {
                var wholeMinutes = (int)(_activeSecondsAccum / 60f);
                _activeSecondsAccum -= wholeMinutes * 60f;
                EditorPrefs.SetInt(StatsMinutesKey, EditorPrefs.GetInt(StatsMinutesKey, 0) + wholeMinutes);
            }

            EnsureCurrentDay();
            _daySecondsAccum += seconds;
            if (_daySecondsAccum >= 60f)
            {
                var wholeMinutes = (int)(_daySecondsAccum / 60f);
                _daySecondsAccum -= wholeMinutes * 60f;
                EditorPrefs.SetInt(StatsDayMinutesKey, EditorPrefs.GetInt(StatsDayMinutesKey, 0) + wholeMinutes);
            }
        }

        public static int WeekMinutes { get { EnsureCurrentWeek(); return EditorPrefs.GetInt(StatsMinutesKey, 0); } }
        public static int WeekSaves { get { EnsureCurrentWeek(); return EditorPrefs.GetInt(StatsSavesKey, 0); } }
        public static int WeekPlaytests { get { EnsureCurrentWeek(); return EditorPrefs.GetInt(StatsPlaytestsKey, 0); } }

        public static int DayMinutes { get { EnsureCurrentDay(); return EditorPrefs.GetInt(StatsDayMinutesKey, 0); } }
        public static int DaySaves { get { EnsureCurrentDay(); return EditorPrefs.GetInt(StatsDaySavesKey, 0); } }
        public static int DayPlaytests { get { EnsureCurrentDay(); return EditorPrefs.GetInt(StatsDayPlaytestsKey, 0); } }
        public static int DayScriptEdits { get { EnsureCurrentDay(); return EditorPrefs.GetInt(StatsDayScriptEditsKey, 0); } }
        public static int DayScriptLines { get { EnsureCurrentDay(); return EditorPrefs.GetInt(StatsDayScriptLinesKey, 0); } }

        public static void AddScriptEdit(int lineCount)
        {
            EnsureCurrentDay();
            EditorPrefs.SetInt(StatsDayScriptEditsKey, EditorPrefs.GetInt(StatsDayScriptEditsKey, 0) + 1);
            EditorPrefs.SetInt(StatsDayScriptLinesKey, EditorPrefs.GetInt(StatsDayScriptLinesKey, 0) + lineCount);
        }

        public static void PublishEvent(string message)
        {
            Transport.PublishEvent(new FeedEvent
            {
                userName = BoardSettings.UserName,
                message = message,
                utcTicks = DateTime.UtcNow.Ticks,
            });
        }

        static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                EnsureCurrentWeek();
                EditorPrefs.SetInt(StatsPlaytestsKey, EditorPrefs.GetInt(StatsPlaytestsKey, 0) + 1);
                EnsureCurrentDay();
                EditorPrefs.SetInt(StatsDayPlaytestsKey, EditorPrefs.GetInt(StatsDayPlaytestsKey, 0) + 1);
                PublishEvent("started a playtest");
            }
            else if (change == PlayModeStateChange.EnteredEditMode) PublishEvent("stopped playtesting");
        }

        static void OnSceneSaved(Scene scene)
        {
            EnsureCurrentWeek();
            EditorPrefs.SetInt(StatsSavesKey, EditorPrefs.GetInt(StatsSavesKey, 0) + 1);
            EnsureCurrentDay();
            EditorPrefs.SetInt(StatsDaySavesKey, EditorPrefs.GetInt(StatsDaySavesKey, 0) + 1);
            PublishEvent($"saved {scene.name}");
        }
        static void OnSceneOpened(Scene scene, OpenSceneMode mode) => PublishEvent($"opened {scene.name}");
    }
}
