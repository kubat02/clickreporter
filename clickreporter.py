#!/usr/bin/env python3
"""
clickreporter - Keyboard Hit Stacker
A fun real-time keyboard press tracker with live stats and leaderboard.
"""

import sys
import time
import threading
import collections
from datetime import datetime

BANNER = r"""
  ___  _ _      _    ____                       _
 / __|| (_) ___| | _|  _ \ ___ _ __   ___  _ __| |_ ___ _ __
| |   | | |/ __| |/ / |_) / _ \ '_ \ / _ \| '__| __/ _ \ '__|
| |__ | | | (__|   <|  _ <  __/ |_) | (_) | |  | ||  __/ |
 \___||_|_|\___|_|\_\_| \_\___| .__/ \___/|_|   \__\___|_|
                               |_|
         ⌨  Keyboard Hit Stacker  ⌨
"""

COLORS = {
    "reset":   "\033[0m",
    "bold":    "\033[1m",
    "red":     "\033[91m",
    "green":   "\033[92m",
    "yellow":  "\033[93m",
    "blue":    "\033[94m",
    "magenta": "\033[95m",
    "cyan":    "\033[96m",
    "white":   "\033[97m",
}


def colorize(text: str, *color_names: str) -> str:
    codes = "".join(COLORS.get(c, "") for c in color_names)
    return f"{codes}{text}{COLORS['reset']}"


class ClickReporter:
    def __init__(self) -> None:
        self.counts: collections.Counter = collections.Counter()
        self.total: int = 0
        self.start_time: float = time.time()
        self.last_key: str = ""
        self.streak: int = 0
        self.max_streak: int = 0
        self._lock = threading.Lock()
        self._running = True

    # ------------------------------------------------------------------
    # Key normalization
    # ------------------------------------------------------------------

    @staticmethod
    def _key_name(key) -> str:
        """Return a human-readable name for a pynput key."""
        try:
            # Regular character keys
            char = key.char
            if char is None:
                return "<unknown>"
            return char
        except AttributeError:
            # Special keys (Key.space, Key.enter, ...)
            name = str(key).replace("Key.", "")
            return f"<{name}>"

    # ------------------------------------------------------------------
    # Event handler
    # ------------------------------------------------------------------

    def on_press(self, key) -> None:
        name = self._key_name(key)
        with self._lock:
            self.counts[name] += 1
            self.total += 1
            if name == self.last_key:
                self.streak += 1
            else:
                self.streak = 1
                self.last_key = name
            if self.streak > self.max_streak:
                self.max_streak = self.streak

    def on_release(self, key) -> None:
        # Quit on Escape
        try:
            from pynput import keyboard as kb
            esc = kb.Key.esc
        except ImportError:
            esc = None
        if esc is not None and key == esc:
            self._running = False
            return False  # stops the listener

    # ------------------------------------------------------------------
    # Display
    # ------------------------------------------------------------------

    def _bar(self, value: int, max_value: int, width: int = 20) -> str:
        if max_value == 0:
            filled = 0
        else:
            filled = int(width * value / max_value)
        bar = "█" * filled + "░" * (width - filled)
        return colorize(bar, "cyan")

    def _render(self) -> str:
        with self._lock:
            counts_snapshot = list(self.counts.most_common(10))
            total = self.total
            last_key = self.last_key
            streak = self.streak
            max_streak = self.max_streak

        elapsed = time.time() - self.start_time
        kps = total / elapsed if elapsed > 0 else 0.0
        max_count = counts_snapshot[0][1] if counts_snapshot else 1

        lines = [
            colorize(BANNER, "cyan", "bold"),
            colorize(f"  Press  ESC  to quit\n", "yellow"),
            colorize(f"  Total keystrokes : ", "white") + colorize(str(total), "green", "bold"),
            colorize(f"  Keys/second      : ", "white") + colorize(f"{kps:.2f}", "yellow", "bold"),
            colorize(f"  Last key         : ", "white") + colorize(repr(last_key), "magenta", "bold"),
            colorize(f"  Current streak   : ", "white") + colorize(str(streak), "blue", "bold"),
            colorize(f"  Best streak      : ", "white") + colorize(str(max_streak), "red", "bold"),
            "",
            colorize("  ── Top 10 Keys ──────────────────────────────", "white"),
            "",
        ]

        for rank, (key_name, count) in enumerate(counts_snapshot, start=1):
            medal = ("🥇", "🥈", "🥉")[rank - 1] if rank <= 3 else f"  {rank}."
            bar = self._bar(count, max_count)
            pct = 100.0 * count / total if total > 0 else 0.0
            lines.append(
                f"  {medal}  {colorize(key_name.ljust(12), 'bold')}  {bar}  "
                f"{colorize(str(count).rjust(6), 'green')}  "
                f"{colorize(f'({pct:5.1f}%)', 'yellow')}"
            )

        lines += [
            "",
            colorize(f"  Running since {datetime.fromtimestamp(self.start_time).strftime('%H:%M:%S')}", "white"),
        ]
        return "\n".join(lines)

    # ------------------------------------------------------------------
    # Main loop
    # ------------------------------------------------------------------

    def _refresh_loop(self) -> None:
        """Redraw the display every 0.2 seconds."""
        while self._running:
            # Move cursor to top-left and clear screen
            sys.stdout.write("\033[2J\033[H")
            sys.stdout.write(self._render())
            sys.stdout.flush()
            time.sleep(0.2)

    def run(self) -> None:
        try:
            from pynput import keyboard
        except ImportError:
            print("Missing dependency: pynput")
            print("Install it with:  pip install pynput")
            sys.exit(1)

        # Start the display thread
        display_thread = threading.Thread(target=self._refresh_loop, daemon=True)
        display_thread.start()

        # Start pynput listener (blocking until ESC or listener stops)
        with keyboard.Listener(on_press=self.on_press, on_release=self.on_release) as listener:
            listener.join()

        self._running = False
        display_thread.join(timeout=0.5)

        # Final render after quitting
        sys.stdout.write("\033[2J\033[H")
        sys.stdout.write(self._render())
        sys.stdout.write(
            "\n\n"
            + colorize("  Thanks for using ClickReporter! 🎉\n", "green", "bold")
        )
        sys.stdout.flush()


def main() -> None:
    reporter = ClickReporter()
    reporter.run()


if __name__ == "__main__":
    main()
