# StatsSystem

[![Version](https://img.shields.io/github/v/release/MedveMarci/StatsSystem?label=Version&color=d500ff)](https://github.com/MedveMarci/StatsSystem/releases/latest) [![LabAPI Version](https://img.shields.io/badge/LabAPI_Version-1.1.7-51f4ff)](https://github.com/northwood-studios/LabAPI/releases/tag/1.1.7) [![SCP:SL Version](https://img.shields.io/badge/SCP:SL_Version-14.2.7-e5b200)](https://store.steampowered.com/app/700330/SCP_Secret_Laboratory/) [![Downloads](https://img.shields.io/github/downloads/MedveMarci/StatsSystem/total?label=Downloads&color=ffbf00)](https://github.com/MedveMarci/StatsSystem/releases) [![License](https://img.shields.io/github/license/MedveMarci/StatsSystem?label=License&color=2ea44f)](LICENSE) [![DeltaPatch](https://image2url.com/images/1759565889245-ff2e02c2-1f19-4f72-bc06-43a3b77fb4bd.png)](https://github.com/KenleyundLeon/DeltaPatch)

An SCP: Secret Laboratory [LabAPI](https://github.com/northwood-studios/LabAPI) plugin for comprehensive, modular player
statistics tracking.

---

## Features

- **Modular stat tracking** — kills, deaths, playtime, SCP kills, MicroHID kills, Class D kills and more, all toggleable
  individually
- **Daily / historical stats** — configurable time windows (e.g. last 7, 30, 90 days) for every tracked stat
- **Two storage backends** — human-readable JSON or compact Binary format, swappable
- **Leaderboards** — in-game leaderboard for any stat, including historical period views
- **Public API** — full C# API for other plugins to read and write custom statistics
- **Admin tools** — save, reload, reset and inspect stats via Remote Admin
- **Auto-save** — configurable auto-save interval with file writes (no data corruption)

---

## Installation

1. Download the latest `StatsSystem.dll` from [GitHub Releases](https://github.com/MedveMarci/StatsSystem/releases).
2. Place the DLL in your server's folder.
    - Windows: `%AppData%\SCP Secret Laboratory\LabAPI\plugins\global\`
    - Linux: `~/.config/SCP Secret Laboratory/LabAPI/plugins/global/`
3. Start the server — a default `config.yml` is generated automatically.
4. Adjust the config to your needs and reload.

---

## Configuration

Configuration is located at:

- Windows: `%AppData%\SCP Secret Laboratory\LabAPI\configs\<port>\`
- Linux: `~/.config/SCP Secret Laboratory/LabAPI/configs/<port>/`

| Key                       | Default       | Description                               |
|---------------------------|---------------|-------------------------------------------|
| `PlaytimeTracking`        | `true`        | Track total playtime per player           |
| `KillsTracking`           | `true`        | Track total kills                         |
| `DeathsTracking`          | `true`        | Track total deaths                        |
| `KillsAsClassDTracking`   | `true`        | Track kills made while playing as Class D |
| `ClassDKillsTracking`     | `true`        | Track kills where the victim is Class D   |
| `ScpKillsTracking`        | `true`        | Track kills where the victim is an SCP    |
| `MicroHidKillsTracking`   | `true`        | Track kills made with MicroHID            |
| `LastDays`                | `[7, 30, 90]` | Day ranges for historical stat display    |
| `StatsDataFolder`         | `StatsSystem` | Folder name for stat storage files        |
| `AutoSaveIntervalSeconds` | `60`          | How often stats are saved (minimum: 10)   |
| `StorageProvider`         | `Json`        | Storage format: `Json` or `Binary`        |
| `Debug`                   | `false`       | Enable verbose debug logging              |

---

## Commands

### Player Commands (Client / Remote Admin)

| Command                        | Aliases        | Description                                    |
|--------------------------------|----------------|------------------------------------------------|
| `.getstat`                     | `.gs`          | View your own stats                            |
| `.getstat <player>`            | `.gs <player>` | View another player's stats by name or user ID |
| `.getleaderboard <stat>`       | `.gl <stat>`   | Top 10 players for a given stat                |
| `.gl <stat> <top>`             |                | Top N players for a stat                       |
| `.gl <stat> last <days>`       |                | Top players for the last N days                |
| `.gl <stat> last <days> <top>` |                | Top N players for the last N days              |

**Leaderboard examples:**

```
.gl Kills 20
.gl TotalPlayTime last 7
.gl ScpKills last 30 15
```

### Admin Commands (Remote Admin / Game Console)

> Requires the `stat.manage` permission.

| Command                   | Aliases             | Description                                                 |
|---------------------------|---------------------|-------------------------------------------------------------|
| `ss save`                 | `ss s`              | Immediately save all stats to disk                          |
| `ss reload`               | `ss r`              | Reload stats from disk                                      |
| `ss resetplayer <userId>` | `ss rp`, `ss reset` | Delete all stats for a player                               |
| `ss info`                 | `ss i`              | Show system status (player count, keys, auto-save interval) |

#### Stat Modification (Remote Admin)

> Requires the `stat.manage` permission.

| Command                                  | Description                  |
|------------------------------------------|------------------------------|
| `addstat <player> <statKey> <amount>`    | Add to a stat value          |
| `removestat <player> <statKey> <amount>` | Subtract from a stat value   |
| `setstat <player> <statKey> <value>`     | Set a stat to an exact value |
| `deletestat <userId> <statKey>`          | Remove a stat key entirely   |

---

## Storage

Stats are saved to:

- Windows: `%AppData%\SCP Secret Laboratory\LabAPI\configs\StatsSystem\`
- Linux: `~/.config/SCP Secret Laboratory/LabAPI/configs/StatsSystem/`

(or `player_stats.bin` when using Binary storage)

### JSON vs Binary

|                | JSON   | Binary        |
|----------------|--------|---------------|
| Human-readable | Yes    | No            |
| File size      | Larger | ~70% smaller  |
| Speed          | Slower | Faster        |
| Editability    | Easy   | Requires tool |

You can switch between formats at runtime — the plugin will automatically save in the old format and reload in the new
one.

---

## Default Stat Keys

| Key             | Type     | Description                    |
|-----------------|----------|--------------------------------|
| `Kills`         | Counter  | Total kills                    |
| `Deaths`        | Counter  | Total deaths                   |
| `KillsAsClassD` | Counter  | Kills made while Class D       |
| `ClassDKills`   | Counter  | Kills where victim was Class D |
| `ScpKills`      | Counter  | Kills where victim was an SCP  |
| `MicroHidKills` | Counter  | Kills with MicroHID            |
| `TotalPlayTime` | Duration | Total time spent on server     |

---

## Support

<a href="https://discord.gg/KmpA8cfaSA"><img src="https://www.allkpop.com/upload/2021/01/content/262046/1611711962-discord-button.png" height="80"></a>

---

## Credits

- Made by [MedveMarci](https://github.com/MedveMarci)
