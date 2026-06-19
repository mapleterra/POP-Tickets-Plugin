# POP Tickets — Dalamud Plugin

A host tool for running 3-digit POP Ticket lottery rounds inside Final Fantasy XIV.

## Features

- **Add tickets** — enter a player name, pick three digits (0–9), and set a bet in gil (up to 500,000)
- **Random ticket** — one click generates a random 3-digit number
- **Countdown timer** — always shows time until the next top-of-hour draw
- **Auto-detect winning number** — monitors a configured chat channel for a trigger phrase (e.g. "Winning number: 123") and pre-fills the resolve dialog automatically
- **Payout resolution** — enter the winning number and click Resolve; every ticket is evaluated instantly:
  | Matching digits | Multiplier |
  |---|---|
  | 0 | ×0 (no win) |
  | 1 | ×2 |
  | 2 | ×5 |
  | 3 | ×50 (jackpot) |
- **Results panel** — shows every player's ticket, match count, multiplier, and gil payout after resolution
- **Draw history** — keeps a permanent log of every past round (date/time, winning number, total paid out)
- **Persistent** — all tickets and history survive plugin reload via Dalamud's config system

## Commands

| Command | Description |
|---|---|
| `/poptickets` | Toggle the host window |
| `/pop` | Toggle the host window (alias) |

## Installation (custom repo)

1. Open Dalamud's plugin installer → **Settings** → **Experimental**
2. Add this URL to the **Custom Plugin Repositories** list
   ```
   https://raw.githubusercontent.com/mapleterra/POPTickets-Dalamud/main/repo.json
   ```
3. Save, then search for **POP Tickets** in the plugin installer and install it.

## Manual installation (testing)

1. Download the latest `POPTickets-vX.X.X.zip` from the [Releases](../../releases) page
2. Extract `POPTickets.dll` and `manifest.json` into your Dalamud `devPlugins` folder
3. Run `/xlplugins` in-game → **Dev Tools** → **Load Plugin**

## Configuration

Click the **⚙ Settings** button in the host window, or use Dalamud's plugin settings gear. Options:

- **Monitor Channel** — the chat channel type to listen to (Say, Shout, Party, etc.)
- **Trigger Regex** — a regular expression whose capture group 1 contains 3 digits (default: `[Ww]inning\s+number[:\s]+(\d{3})`)
- **Accent Colour** — highlight colour used throughout the UI

## Building from source

Requirements: .NET 9 SDK, FFXIV + Dalamud installed locally.

```bash
git clone https://github.com/mapleterra/POPTickets-Dalamud.git
cd POPTickets-Dalamud
dotnet restore POPTickets/POPTickets.csproj
dotnet build  POPTickets/POPTickets.csproj --configuration Release
```

The CI workflow (`.github/workflows/build.yml`) downloads Dalamud automatically and publishes a release ZIP on every `v*.*.*` tag push.

## Releasing a new version

```bash
git tag v1.0.1
git push origin v1.0.1
```

GitHub Actions will build, package `POPTickets.dll + manifest.json` into a ZIP, and publish it as a GitHub Release automatically.

## License

MIT — see [LICENSE](LICENSE).
