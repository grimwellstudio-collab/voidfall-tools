using System.Collections.Generic;

namespace Grimwell.SessionBoard
{
    public interface IPresenceTransport
    {
        void PublishPresence(PresenceState state);
        void PublishEvent(FeedEvent evt);
        List<PresenceState> ReadAllPresence();
        List<FeedEvent> ReadRecentEvents(int max);
        void PublishClaim(ClaimEntry claim);
        void ReleaseClaim(ClaimEntry claim);
        List<ClaimEntry> ReadClaims();
        List<HistoryEntry> ReadHistory(int days);
    }
}
