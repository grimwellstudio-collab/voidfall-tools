# VoidFall Tools

Grimwell Studio's Unity editor tooling for the VoidFall collab. Each tool is a standalone UPM package; game projects consume them by git URL reference — tools never live inside a game project.

Pinned Unity version: **6000.3.20f1** (Unity 6.3 LTS).

## Packages

- `com.grimwell.sessionboard` — Session Board: team presence dashboard (who's online, what they're editing, activity feed, popup notifications, same-scene collision warning).

## Consuming a package

In a project's `Packages/manifest.json`:

```json
"com.grimwell.sessionboard": "https://github.com/grimwellstudio-collab/voidfall-tools.git?path=/com.grimwell.sessionboard",
"com.grimwell.doctor": "https://github.com/grimwellstudio-collab/voidfall-tools.git?path=/com.grimwell.doctor"
```

Or for local development, a `file:` reference to the package folder.

## Packages (continued)

- `com.grimwell.doctor` — Setup Doctor: one-click machine/project checker for team onboarding (Unity version, Git, Git LFS, GitHub Desktop, merge-safe project settings).

The Session Board relay worker lives in `backend/session-board-relay/` (Cloudflare; the team key is a worker secret, never committed).
