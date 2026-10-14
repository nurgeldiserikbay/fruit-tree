#!/usr/bin/env python3
"""Prepare a 2:1 environment panorama PNG for Unity.

Usage:
  python tools/prepare_panorama.py Meadow /path/to/source.png

This creates Assets/Resources/Panoramas/Meadow.png. Generated/illustrated
panoramas still need 360-degree visual inspection: edge blending does not
turn a perspective painting into a physically correct equirectangular HDRI.
"""
from pathlib import Path
import argparse
from PIL import Image

THEMES = {
    "Meadow", "Park", "CityPark", "Mountains", "JapaneseGarden",
    "ChineseGarden", "Lakeside", "EveningGarden",
}
ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "Assets" / "Resources" / "Panoramas"


def prepare(theme: str, source: Path, output_dir: Path = OUT_DIR) -> Path:
    if theme not in THEMES:
        raise ValueError(f"Unknown theme: {theme}. Expected {sorted(THEMES)}")
    with Image.open(source) as src:
        image = src.convert("RGB")
    width, height = image.size
    if width != height * 2:
        raise ValueError(f"Expected 2:1 equirectangular image, got {width}x{height}")

    # Never stretch a perspective or labeled concept into a panorama.
    # Keep the artwork intact; check its longitudinal seam in Unity.
    output_dir.mkdir(parents=True, exist_ok=True)
    out = output_dir / f"{theme}.png"
    image.save(out, "PNG", optimize=True)
    return out


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("theme", choices=sorted(THEMES))
    parser.add_argument("source", type=Path)
    args = parser.parse_args()
    print(prepare(args.theme, args.source))
