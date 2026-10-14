#!/usr/bin/env python3
"""Import the eight FruitTree visual reference images from the concept ZIP.

Usage: python tools/import_panorama_concepts.py /path/to/fruit_tree_8_environment_concepts.zip
These are reference concepts, NOT seamless equirectangular panorama textures.
"""
from pathlib import Path
import sys
from zipfile import ZipFile

THEMES = (
    "Meadow", "Park", "CityPark", "Mountains", "JapaneseGarden",
    "ChineseGarden", "Lakeside", "EveningGarden",
)
DESTINATION = Path(__file__).resolve().parents[1] / "Assets" / "Art" / "Environment" / "Concepts"


def import_concepts(archive: Path) -> None:
    if not archive.is_file():
        raise FileNotFoundError(archive)
    with ZipFile(archive) as source:
        entries = {Path(name).name: name for name in source.namelist()}
        expected = [f"{theme}_concept.png" for theme in THEMES]
        missing = [name for name in expected if name not in entries]
        if missing:
            raise ValueError("Archive is missing: " + ", ".join(missing))
        DESTINATION.mkdir(parents=True, exist_ok=True)
        for name in expected:
            data = source.read(entries[name])
            if not data.startswith(b"\x89PNG\r\n\x1a\n"):
                raise ValueError(f"{name} is not a PNG")
            (DESTINATION / name).write_bytes(data)
            print(f"Imported {DESTINATION / name}")


if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit("Usage: python tools/import_panorama_concepts.py <concept-zip>")
    import_concepts(Path(sys.argv[1]))
