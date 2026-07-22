# Session Board

Team presence dashboard for the Unity editor. Open it from **Window → Grimwell → Session Board**.

- **You**: set your display name and a status line ("blocking out Act I crypt"). Pick the shared folder.
- **Team**: a card per teammate — online/away/playtesting, open scene, current selection, last seen.
- **Activity**: live feed — came online, opened/saved scenes, started a playtest.
- **Collision warning**: if a teammate has your open scene open too, you get a popup. Unity scenes don't merge; this exists to stop lost work.

## How presence syncs

Two modes (Settings foldout in the panel):

- **Online relay (default):** a tiny Cloudflare worker (`backend/session-board-relay/` in this repo, deployed at `https://session-board-relay.grimwellstudio.workers.dev`). Each teammate enters the shared **Team key** (distributed privately — never commit it; it lives as the worker's `TEAM_KEY` secret, rotate with `wrangler secret put TEAM_KEY`). Room name groups a team; default `voidfall`.
- **Shared folder:** zero-server fallback — point every teammate at the same synced folder (Dropbox/Drive/iCloud).

The **Join Discord Call** button opens the link set in Settings → Discord link.

## Trying it alone

Open two Unity projects with this package, give each a different display name and the same Team key (or shared folder) — the second "teammate" appears on the board.
