using System;

namespace Grimwell.SessionBoard
{
    [Serializable]
    public class PresenceState
    {
        public string userName;
        public string machineName;
        public string statusLine;
        public string openScene;
        public string selection;
        public bool inPlayMode;
        public long heartbeatUtcTicks;
        public int weekMinutes;
        public int weekSaves;
        public int weekPlaytests;

        public DateTime HeartbeatUtc => new DateTime(heartbeatUtcTicks, DateTimeKind.Utc);
    }

    [Serializable]
    public class FeedEvent
    {
        public string userName;
        public string message;
        public long utcTicks;

        public DateTime Utc => new DateTime(utcTicks, DateTimeKind.Utc);
    }

    [Serializable]
    public class ClaimEntry
    {
        public string item;
        public string userName;
        public long utcTicks;
    }
}
