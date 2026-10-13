"""Data-driven, independent combat sampler for Deep Space Saga."""

from .gamedata import load_game_data
from .core import sample_battle

__all__ = ["load_game_data", "sample_battle"]
