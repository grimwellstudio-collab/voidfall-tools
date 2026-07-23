using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Grimwell.SessionBoard
{
    /// <summary>
    /// Presence exchange over a shared folder. Point every teammate's editor at the
    /// same synced folder (Dropbox / Google Drive / iCloud) and the board works with
    /// no server at all. An HTTP backend can replace this class later without
    /// touching the publisher or the window.
    /// </summary>
    public class SharedFolderTransport : IPresenceTransport
    {
        readonly string _root;

        public SharedFolderTransport(string root)
        {
            _root = root;
            try
            {
                Directory.CreateDirectory(PresenceDir);
                Directory.CreateDirectory(EventsDir);
                Directory.CreateDirectory(ClaimsDir);
                Directory.CreateDirectory(HistoryDir);
            }
            catch (IOException) { }
        }

        public string Root => _root;
        string PresenceDir => Path.Combine(_root, "presence");
        string EventsDir => Path.Combine(_root, "events");
        string ClaimsDir => Path.Combine(_root, "claims");
        string HistoryDir => Path.Combine(_root, "history");

        static string FileSafe(string name)
        {
            if (string.IsNullOrEmpty(name)) return "_";
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        public void PublishPresence(PresenceState state)
        {
            try
            {
                var path = Path.Combine(PresenceDir, FileSafe(state.userName) + ".json");
                File.WriteAllText(path, JsonUtility.ToJson(state));
            }
            catch (IOException) { } // synced folders lock briefly; next heartbeat retries
        }

        public void PublishEvent(FeedEvent evt)
        {
            try
            {
                var path = Path.Combine(EventsDir, FileSafe(evt.userName) + ".jsonl");
                File.AppendAllText(path, JsonUtility.ToJson(evt) + "\n");
            }
            catch (IOException) { }
        }

        public void PublishClaim(ClaimEntry claim)
        {
            try
            {
                var path = Path.Combine(ClaimsDir, FileSafe(claim.item) + ".json");
                File.WriteAllText(path, JsonUtility.ToJson(claim));
            }
            catch (IOException) { }
        }

        public void ReleaseClaim(ClaimEntry claim)
        {
            try
            {
                var path = Path.Combine(ClaimsDir, FileSafe(claim.item) + ".json");
                if (!File.Exists(path)) return;
                var stored = JsonUtility.FromJson<ClaimEntry>(File.ReadAllText(path));
                if (stored != null && stored.userName == claim.userName)
                    File.Delete(path);
            }
            catch (IOException) { }
        }

        public void WriteHistoryRollup(HistoryEntry e)
        {
            try
            {
                var path = Path.Combine(HistoryDir, "d_" + e.date + "_" + FileSafe(e.userName) + ".json");
                File.WriteAllText(path, JsonUtility.ToJson(e));
            }
            catch (IOException) { }
        }

        public List<PresenceState> ReadAllPresence()
        {
            var result = new List<PresenceState>();
            try
            {
                if (!Directory.Exists(PresenceDir)) return result;
                foreach (var file in Directory.GetFiles(PresenceDir, "*.json"))
                {
                    try
                    {
                        var state = JsonUtility.FromJson<PresenceState>(File.ReadAllText(file));
                        if (state != null && !string.IsNullOrEmpty(state.userName))
                            result.Add(state);
                    }
                    catch (Exception) { } // partial write from another machine; skip this cycle
                }
            }
            catch (IOException) { }
            return result;
        }

        public List<FeedEvent> ReadRecentEvents(int max)
        {
            var result = new List<FeedEvent>();
            try
            {
                if (!Directory.Exists(EventsDir)) return result;
                foreach (var file in Directory.GetFiles(EventsDir, "*.jsonl"))
                {
                    try
                    {
                        var lines = File.ReadAllLines(file);
                        foreach (var line in lines.Skip(Math.Max(0, lines.Length - max)))
                        {
                            var evt = JsonUtility.FromJson<FeedEvent>(line);
                            if (evt != null && !string.IsNullOrEmpty(evt.message))
                                result.Add(evt);
                        }
                    }
                    catch (Exception) { }
                }
            }
            catch (IOException) { }
            return result.OrderByDescending(e => e.utcTicks).Take(max).ToList();
        }

        public List<ClaimEntry> ReadClaims()
        {
            var result = new List<ClaimEntry>();
            try
            {
                if (!Directory.Exists(ClaimsDir)) return result;
                foreach (var file in Directory.GetFiles(ClaimsDir, "*.json"))
                {
                    try
                    {
                        var claim = JsonUtility.FromJson<ClaimEntry>(File.ReadAllText(file));
                        if (claim == null || string.IsNullOrEmpty(claim.item)) continue;
                        if (DateTime.UtcNow - new DateTime(claim.utcTicks, DateTimeKind.Utc) > TimeSpan.FromHours(8))
                        {
                            try { File.Delete(file); } catch (IOException) { } // expired; best-effort cleanup
                            continue;
                        }
                        result.Add(claim);
                    }
                    catch (Exception) { } // partial write from another machine; skip this cycle
                }
            }
            catch (IOException) { }
            return result;
        }

        public List<HistoryEntry> ReadHistory(int days)
        {
            var result = new List<HistoryEntry>();
            var cutoff = DateTime.Now.AddDays(-days).ToString("yyyy-MM-dd");
            var maxCutoff = DateTime.Now.AddDays(-31).ToString("yyyy-MM-dd");
            try
            {
                if (!Directory.Exists(HistoryDir)) return result;
                foreach (var file in Directory.GetFiles(HistoryDir, "*.json"))
                {
                    try
                    {
                        var entry = JsonUtility.FromJson<HistoryEntry>(File.ReadAllText(file));
                        if (entry == null || string.IsNullOrEmpty(entry.date)) continue;
                        if (string.Compare(entry.date, maxCutoff, StringComparison.Ordinal) < 0)
                        {
                            try { File.Delete(file); } catch (IOException) { } // expired; best-effort cleanup
                            continue;
                        }
                        if (string.Compare(entry.date, cutoff, StringComparison.Ordinal) >= 0)
                            result.Add(entry);
                    }
                    catch (Exception) { } // partial write from another machine; skip this cycle
                }
            }
            catch (IOException) { }
            return result;
        }
    }
}
