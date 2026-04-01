# clickreporter

**Keyboard Hit Stacker** — a fun real-time terminal app that counts every key you press and shows live statistics and a leaderboard of your most-used keys.

```
  ___  _ _      _    ____                       _
 / __|| (_) ___| | _|  _ \ ___ _ __   ___  _ __| |_ ___ _ __
| |   | | |/ __| |/ / |_) / _ \ '_ \ / _ \| '__| __/ _ \ '__|
| |__ | | | (__|   <|  _ <  __/ |_) | (_) | |  | ||  __/ |
 \___||_|_|\___|_|\_\_| \_\___| .__/ \___/|_|   \__\___|_|
                               |_|
         ⌨  Keyboard Hit Stacker  ⌨
```

## Features

- ⚡ **Real-time** keystroke counter, updated 5× per second
- 📊 **Live leaderboard** — top-10 most-pressed keys with animated bars
- 🏅 **Medals** for your three most-used keys (🥇🥈🥉)
- 🔥 **Streak tracker** — how many times in a row you pressed the same key
- ⌨  **Keys/second** metric

## Requirements

- Python 3.8+
- [pynput](https://pypi.org/project/pynput/)

## Installation

```bash
pip install -r requirements.txt
```

## Usage

```bash
python clickreporter.py
```

Press **ESC** to quit and see your final stats.

## Notes

- On **macOS** you may need to grant Terminal accessibility permissions  
  (*System Preferences → Security & Privacy → Privacy → Accessibility*).
- On **Linux** (Wayland) pynput may require running under XWayland or as root.

