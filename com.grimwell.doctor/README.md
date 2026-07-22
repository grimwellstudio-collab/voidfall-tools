# Setup Doctor

A one-click checkup for your Unity machine and project setup. Open it from
**Window > Grimwell > Setup Doctor**. It runs automatically when you open the
window, and again any time you press **Run Checks**.

## What it checks

- **Unity Version** — are you on the studio's exact Unity version?
- **Git** — is Git installed and on your machine?
- **Git LFS** — is Git LFS (for large files) installed?
- **GitHub Desktop** — is it installed? (recommended, not required)
- **Text Serialization** — is the project set to save assets as text, so merges work?
- **Visible Meta Files** — are `.meta` files visible, so version control tracks them?
- **Project In Git Repo** — is this project actually inside a Git repo, and does it have a `.gitignore`?

## Reading the results

Each check shows one of three states:

- `[ok]` green — you're good.
- `[!]` orange — a warning, worth fixing but not blocking.
- `[x]` red — a problem that needs fixing before you work with the team.

**If everything is green, reply "green" in the team channel** — that's the
signal you're ready to go.

## Fix buttons

Two checks — **Text Serialization** and **Visible Meta Files** — are project
settings Setup Doctor can fix for you automatically. If either fails, a
**Fix** button appears next to it. Click it and the setting is corrected
immediately, no menu digging required.

Everything else (installing Unity, Git, Git LFS, or GitHub Desktop) needs to
be done by hand, but the message next to each check tells you exactly what to
install.
