# EmoteReactor

[![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/Valiice/EmoteReactor/total?style=flat-square&color=blue)](https://github.com/Valiice/EmoteReactor/releases)
[![GitHub Release](https://img.shields.io/github/v/release/Valiice/EmoteReactor?include_prereleases&style=flat-square&color=green)](https://github.com/Valiice/EmoteReactor/releases/latest)
[![GitHub License](https://img.shields.io/github/license/Valiice/EmoteReactor?style=flat-square&color=orange)](LICENSE)
[![GitHub Issues](https://img.shields.io/github/issues/Valiice/EmoteReactor?style=flat-square)](https://github.com/Valiice/EmoteReactor/issues)
[![Download Latest Zip](https://img.shields.io/badge/Download-Latest%20Zip-blue?style=flat-square)](https://github.com/Valiice/EmoteReactor/releases/latest/download/EmoteReactor.zip)

**EmoteReactor** is a Dalamud plugin for Final Fantasy XIV that automatically performs an emote back when another player performs a specific emote directed at you.

Define simple line-by-line rules - *"when someone does **Pet** at me, I react with **Backflip**"* - and the plugin does the rest, with per-rule cooldowns, a randomized delay, and a global throttle so it never feels like a machine gun.

## ⚠️ Disclaimer

This plugin **automatically responds to other players' actions on your behalf**. Automating reactions is against FINAL FANTASY XIV's Terms of Service, and using it carries a real risk to your account. Use it at your own risk, keep the cooldowns/delays sensible, and don't spam. You have been warned.

## Installation

1. Add my custom plugin repository to Dalamud (Settings → Experimental → *Custom Plugin Repositories*):
   ```
   https://raw.githubusercontent.com/Valiice/DalamudPluginRepo/master/repo.json
   ```
2. Search for **EmoteReactor** in the Plugin Installer.
3. Install and enable.

## How It Works

- **Detection** - hooks the game's own emote handling (via FFXIVClientStructs' `EmoteController`), so it knows exactly *who* emoted, *which* emote, and *who they targeted*.
- **Decision** - a rule fires only when the emote was aimed **at you**, subject to your cooldown and throttle settings.
- **Reaction** - the emote command is sent through the game's chat box (via [ECommons](https://github.com/NightmareXIV/ECommons)), optionally targeting the instigator first.

There are **no hand-copied memory signatures** - everything rides on ClientStructs and ECommons, which update alongside Dalamud, so it keeps working across game patches.

## Configuration

Open the config window with `/emotereactor`.

### Global options
| Option | Description |
| :--- | :--- |
| **Enabled** | Master on/off switch for the whole plugin. |
| **Global throttle (seconds)** | Minimum gap between *any* two reactions, across all rules. Anti-spam safety net. |
| **Debug logging** | Writes each emote aimed at you to `/xllog`, and reveals a live status panel (detection/sending health + counters). Leave off for normal use. |

### Per-rule options
Click **Add rule** to create a reaction. Each rule reads as a sentence:

> ✔ Enabled &nbsp; **When someone does** `[Pet ▼]` &nbsp; **I react with** `[Backflip ▼]`

| Option | Description |
| :--- | :--- |
| **When someone does** | The emote another player must perform **on you** to trigger the rule. Searchable, with icons. |
| **I react with** | The emote you automatically perform back. |
| **Motion only** | Play just the animation, with no chat text line. |
| **At them** | Target the person and emote *at* them. Off = just perform the emote without targeting. |
| **Restore target** | After reacting, re-select whoever you had targeted before. |
| **Cooldown (s)** | Minimum gap before this rule reacts to the **same person** again. |
| **Reaction delay (s)** | Waits a random time in this range before reacting - drag the two handles for min/max. Set both to `0` for instant. |

Multiple rules can share the same trigger; they all fire (stack). Every change saves automatically.

## Building From Source

Requires the .NET 10 SDK and a Dalamud dev install (`DALAMUD_HOME` pointing at `%AppData%\XIVLauncher\addon\Hooks\dev`). ECommons is a git submodule.

```bash
git clone --recursive https://github.com/Valiice/EmoteReactor.git
cd EmoteReactor
dotnet build
dotnet test
```

## License

Licensed under **AGPL-3.0-or-later**.
