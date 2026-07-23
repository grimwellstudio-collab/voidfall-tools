# Session Board

Team presence dashboard for the Unity editor. Open it from **Window → Grimwell → Session Board**.

## Board tab

- **You**: set your display name and a status line ("blocking out Act I crypt") — both appear on your card for teammates.
- **Team**: a card per teammate — online / playtesting / away, their status line, open scene, current selection, weekly totals, last seen. Teammates unseen for over a day drop off the list.
- **Pieces**: appears when the project has a Session Definition (see below). One row per level piece with who has claimed it. **Claim** a piece before working on it; **Release** when done. Opening a scene someone else has claimed pops a warning.
- **Activity**: live feed — came online, opened/saved scenes, started a playtest, claims, session joins.
- **Popups**: corner notifications for teammate activity. Mute them in Settings; the same-scene collision warning is never muted (it prevents lost work — Unity scenes don't merge).
- **Join Session** (header): opens the shared level — every piece from the Session Definition loaded together, so you see the whole level while owning your piece.
- **Join Discord Call** (header): opens the link set in Settings → Discord link.

## Insights tab

Manager view of team participation. Pick 7 / 14 / 30 days:

- **Totals** per member: active hours, saves, playtests, script lines.
- **Charts** per day per member: active hours, saves, playtests, script lines touched.

All of it is tracked automatically from real editor activity — heartbeats while Unity is open, scene saves/opens, play-mode entries, and code files as they're saved into the project ("script lines" is the size of files as saved, an activity signal rather than an exact diff).

## Setup (each teammate, once)

1. Install the package (see the repo root README).
2. Open **Window → Grimwell → Session Board**, type your display name.
3. Open **Settings** (bottom of the panel) and paste the **Team key** you were given privately. Room stays `voidfall` unless told otherwise.
4. Optionally paste the team's **Discord link**.

That's it — your card appears on everyone's board within seconds.

**Names must be unique**: two people using the same display name merge into one card. Pick distinct names (Discord-based sign-in is planned to make this automatic).

## Session Definition (level pieces)

Create one via **Assets → Create → Grimwell → Session Definition** and add your level's scenes as pieces (or run **Grimwell → Create Demo Session** to see a working example). The Pieces section and Join Session button light up once one exists in the project.

## How syncing works

Two modes (Settings):

- **Online relay (default)**: a tiny Cloudflare worker (`backend/session-board-relay/` in this repo). The Team key is required; it lives as the worker's `TEAM_KEY` secret — never commit it; rotate with `wrangler secret put TEAM_KEY`.
- **Shared folder**: zero-server fallback — point every teammate at the same synced folder (Dropbox/Drive/iCloud).

## Trying it alone

Open two Unity projects with this package, give each a different display name and the same Team key — the second "teammate" appears on the board.
