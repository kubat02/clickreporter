"""Tests for clickreporter core logic (no display/pynput required)."""

import pytest
from clickreporter import ClickReporter, colorize


# ---------------------------------------------------------------------------
# Fake key helpers
# ---------------------------------------------------------------------------

class _CharKey:
    """Mimics a pynput character key."""

    def __init__(self, char: str) -> None:
        self.char = char


class _SpecialKey:
    """Mimics a pynput special key (no .char attribute)."""

    def __init__(self, name: str) -> None:
        self._name = name

    def __str__(self) -> str:
        return f"Key.{self._name}"

    @property
    def char(self):
        raise AttributeError("special key has no char")


# ---------------------------------------------------------------------------
# ClickReporter tests
# ---------------------------------------------------------------------------

def _press(reporter: ClickReporter, *keys) -> None:
    for key in keys:
        reporter.on_press(key)


def test_initial_state():
    r = ClickReporter()
    assert r.total == 0
    assert len(r.counts) == 0
    assert r.streak == 0
    assert r.max_streak == 0
    assert r.last_key == ""


def test_char_key_counting():
    r = ClickReporter()
    _press(r, _CharKey("a"), _CharKey("b"), _CharKey("a"))
    assert r.total == 3
    assert r.counts["a"] == 2
    assert r.counts["b"] == 1


def test_special_key_counting():
    r = ClickReporter()
    _press(r, _SpecialKey("space"), _SpecialKey("space"), _SpecialKey("enter"))
    assert r.total == 3
    assert r.counts["<space>"] == 2
    assert r.counts["<enter>"] == 1


def test_streak_same_key():
    r = ClickReporter()
    _press(r, _CharKey("x"), _CharKey("x"), _CharKey("x"))
    assert r.streak == 3
    assert r.max_streak == 3
    assert r.last_key == "x"


def test_streak_resets_on_different_key():
    r = ClickReporter()
    _press(r, _CharKey("a"), _CharKey("a"), _CharKey("b"))
    assert r.streak == 1
    assert r.max_streak == 2
    assert r.last_key == "b"


def test_max_streak_preserved():
    r = ClickReporter()
    # streak of 3 then broken, then streak of 2
    _press(r,
           _CharKey("a"), _CharKey("a"), _CharKey("a"),
           _CharKey("b"), _CharKey("b"))
    assert r.max_streak == 3
    assert r.streak == 2


def test_render_contains_total():
    r = ClickReporter()
    _press(r, _CharKey("a"), _CharKey("b"), _CharKey("c"))
    output = r._render()
    assert "3" in output
    assert "a" in output


def test_render_shows_top_keys():
    r = ClickReporter()
    # Press 'z' 5 times and 'q' once
    for _ in range(5):
        r.on_press(_CharKey("z"))
    r.on_press(_CharKey("q"))
    output = r._render()
    assert "z" in output
    assert "q" in output
    # z should appear before q (higher count)
    assert output.index("🥇") < output.index("🥈")


def test_bar_full():
    r = ClickReporter()
    bar = r._bar(20, 20)
    # Should be all filled blocks (ignoring ANSI codes)
    plain = bar.replace("\033[96m", "").replace("\033[0m", "")
    assert "░" not in plain


def test_bar_empty():
    r = ClickReporter()
    bar = r._bar(0, 20)
    plain = bar.replace("\033[96m", "").replace("\033[0m", "")
    assert "█" not in plain


def test_bar_zero_max():
    r = ClickReporter()
    # Should not raise even when max_value is 0
    bar = r._bar(0, 0)
    assert bar is not None


def test_colorize():
    result = colorize("hello", "green")
    assert "hello" in result
    assert "\033[" in result
    # Should end with reset
    assert result.endswith("\033[0m")


def test_colorize_unknown_color():
    # Unknown color name should not raise, just produce no ANSI for that name
    result = colorize("test", "nonexistent_color")
    assert "test" in result
