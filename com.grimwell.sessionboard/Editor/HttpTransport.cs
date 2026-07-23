using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Grimwell.SessionBoard
{
    /// <summary>
    /// Presence exchange over the Session Board relay (a tiny Cloudflare worker).
    /// Publishes are fire-and-forget; reads are served from the last poll, with a
    /// new poll kicked off at most every few seconds.
    /// </summary>
    public class HttpTransport : IPresenceTransport
    {
        const double PollSeconds = 4;
        const double HistoryPollSeconds = 30;

        static readonly HttpClient Client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };

        readonly string _baseUrl;
        readonly string _query;

        volatile List<PresenceState> _presence = new List<PresenceState>();
        volatile List<FeedEvent> _events = new List<FeedEvent>();
        volatile List<ClaimEntry> _claims = new List<ClaimEntry>();
        volatile List<HistoryEntry> _history = new List<HistoryEntry>();
        double _nextPoll;
        int _polling; // 0/1 flag so overlapping polls never stack up
        double _nextHistoryPoll;
        int _historyPolling; // 0/1 flag so overlapping polls never stack up

        [Serializable]
        class StatePayload
        {
            public PresenceState[] presence;
            public FeedEvent[] events;
            public ClaimEntry[] claims;
        }

        [Serializable]
        class HistoryPayload
        {
            public HistoryEntry[] history;
        }

        public HttpTransport(string baseUrl, string room, string teamKey)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _query = "?room=" + Uri.EscapeDataString(room ?? "") +
                     "&key=" + Uri.EscapeDataString(teamKey ?? "");
        }

        public void PublishPresence(PresenceState state) => Post("/presence", JsonUtility.ToJson(state));
        public void PublishEvent(FeedEvent evt) => Post("/event", JsonUtility.ToJson(evt));
        public void PublishClaim(ClaimEntry claim) => Post("/claim", JsonUtility.ToJson(claim));
        public void ReleaseClaim(ClaimEntry claim) => Post("/release", JsonUtility.ToJson(claim));

        void Post(string path, string json)
        {
            var url = _baseUrl + path + _query;
            Task.Run(async () =>
            {
                try
                {
                    using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                        await Client.PostAsync(url, content);
                }
                catch (Exception) { } // offline or server hiccup; next beat retries
            });
        }

        public List<PresenceState> ReadAllPresence()
        {
            PollIfDue();
            return _presence;
        }

        public List<FeedEvent> ReadRecentEvents(int max)
        {
            PollIfDue();
            var events = _events;
            return events.Count <= max ? events : events.GetRange(0, max); // sorted newest-first
        }

        public List<ClaimEntry> ReadClaims()
        {
            PollIfDue();
            return _claims;
        }

        public List<HistoryEntry> ReadHistory(int days)
        {
            PollHistoryIfDue();
            var cutoff = DateTime.Now.AddDays(-days).ToString("yyyy-MM-dd");
            var result = new List<HistoryEntry>();
            foreach (var entry in _history)
                if (entry != null && string.Compare(entry.date, cutoff, StringComparison.Ordinal) >= 0)
                    result.Add(entry);
            return result;
        }

        void PollIfDue()
        {
            var now = EditorApplication.timeSinceStartup;
            if (now < _nextPoll || System.Threading.Interlocked.Exchange(ref _polling, 1) == 1) return;
            _nextPoll = now + PollSeconds;

            var url = _baseUrl + "/state" + _query;
            Task.Run(async () =>
            {
                try
                {
                    var json = await Client.GetStringAsync(url);
                    var payload = JsonUtility.FromJson<StatePayload>(json);
                    if (payload != null)
                    {
                        _presence = new List<PresenceState>(payload.presence ?? new PresenceState[0]);
                        var events = new List<FeedEvent>(payload.events ?? new FeedEvent[0]);
                        events.Sort((a, b) => b.utcTicks.CompareTo(a.utcTicks));
                        _events = events;
                        _claims = new List<ClaimEntry>(payload.claims ?? new ClaimEntry[0]);
                    }
                }
                catch (Exception) { }
                finally { System.Threading.Interlocked.Exchange(ref _polling, 0); }
            });
        }

        void PollHistoryIfDue()
        {
            var now = EditorApplication.timeSinceStartup;
            if (now < _nextHistoryPoll || System.Threading.Interlocked.Exchange(ref _historyPolling, 1) == 1) return;
            _nextHistoryPoll = now + HistoryPollSeconds;

            var url = _baseUrl + "/history" + _query + "&days=31";
            Task.Run(async () =>
            {
                try
                {
                    var json = await Client.GetStringAsync(url);
                    var payload = JsonUtility.FromJson<HistoryPayload>(json);
                    if (payload != null)
                        _history = new List<HistoryEntry>(payload.history ?? new HistoryEntry[0]);
                }
                catch (Exception) { }
                finally { System.Threading.Interlocked.Exchange(ref _historyPolling, 0); }
            });
        }
    }
}
