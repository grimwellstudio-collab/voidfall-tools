# VoidFall Tools

Grimwell Studio's Unity editor tooling for the VoidFall collab. Each tool is a standalone UPM package; game projects consume them by git URL reference — tools never live inside a game project.

Pinned Unity version: **6000.3.20f1** (Unity 6.3 LTS).

## Packages

- `com.grimwell.sessionboard` — **Session Board**: team presence dashboard (who's online, what they're editing, activity feed, popups, same-scene collision warning), level-piece claims + one-click Join Session, and an Insights tab with per-member activity charts. [Full guide](com.grimwell.sessionboard/README.md).
- `com.grimwell.doctor` — **Setup Doctor**: one-click machine/project checker for onboarding (Unity version, Git, Git LFS, GitHub Desktop, merge-safe project settings — with Fix buttons). [Guide](com.grimwell.doctor/README.md).

## Installing (easiest way)

In Unity: **Window → Package Manager → “+” → Add package from git URL…** and paste:

```
https://github.com/grimwellstudio-collab/voidfall-tools.git?path=/com.grimwell.sessionboard
```

Repeat with `?path=/com.grimwell.doctor` for the Setup Doctor. Requires Git installed on the machine (GitHub Desktop includes it — run the Setup Doctor if unsure).

Alternatively, add both lines directly to the project's `Packages/manifest.json`:

```json
"com.grimwell.sessionboard": "https://github.com/grimwellstudio-collab/voidfall-tools.git?path=/com.grimwell.sessionboard",
"com.grimwell.doctor": "https://github.com/grimwellstudio-collab/voidfall-tools.git?path=/com.grimwell.doctor"
```

For local development of the tools themselves, use a `file:` reference to a package folder instead.

## After installing

1. **Setup Doctor**: Window → Grimwell → Setup Doctor — get every row green (Fix buttons handle the two project settings).
2. **Session Board**: Window → Grimwell → Session Board — set your name, paste the Team key you were given privately. See the [Session Board guide](com.grimwell.sessionboard/README.md) for everything it does.

## Backend

The Session Board relay worker lives in `backend/session-board-relay/` (Cloudflare). The team key is a worker secret, never committed. Deploy with `npx wrangler deploy`; rotate the key with `npx wrangler secret put TEAM_KEY`.
