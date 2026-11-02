#!/usr/bin/env python3
"""Pad UI sprite families so every state shares one canvas.

Figma exports a PNG that includes the shadow, and each state has a different shadow
(a hover glow is bigger than a pressed one). Unity's Sprite Swap draws every state in the
same rect, so without this step a button would jump and resize when hovered.

Run it on a fresh Figma export only, after replacing any sprite below. It needs Pillow.

    python3 Tools/normalize_ui_sprites.py <export_dir> <output_dir>

PNGs that are not in a family are not touched. Paddings come from Figma: the distance between
each node's layout box and its rendered bounds, in Figma pixels (left, bottom, right, top).
"""
import sys
from pathlib import Path

from PIL import Image

FAMILIES = {
    "felt": {
        "scale": 2,
        "members": {
            "btn_felt_red_default": (28, 44, 28, 12),
            "btn_felt_red_hover": (34, 54, 34, 22),
            "btn_felt_red_pressed": (12, 18, 12, 6),
            "btn_felt_green_default": (28, 44, 28, 12),
            "btn_felt_green_hover": (34, 40, 34, 22),
            "btn_felt_green_pressed": (12, 18, 12, 6),
        },
    },
    "wood": {
        "scale": 2,
        "members": {
            "btn_wood_default": (20, 32, 20, 8),
            "btn_wood_hover": (24, 40, 24, 16),
            "btn_wood_pressed": (8, 12, 8, 4),
        },
    },
    "pill": {
        "scale": 2,
        "members": {
            "btn_pill_default": (0, 0, 0, 0),
            "btn_pill_hover": (14, 14, 14, 14),
            "btn_pill_pressed": (0, 0, 0, 0),
        },
    },
    "code_box": {
        "scale": 2,
        "members": {
            "sprite_code_box_empty": (0, 0, 0, 0),
            "sprite_code_box_focused": (16, 16, 16, 16),
            "sprite_code_box_filled": (0, 7, 0, 0),
        },
    },
    "timer": {
        "scale": 2,
        "members": {
            "sprite_timer_plank_wood": (24, 36, 24, 12),
            "sprite_timer_plank_red": (18, 18, 18, 18),
        },
    },
    "live_dot": {
        "scale": 2,
        "members": {
            "sprite_dot_live_on": (8, 8, 8, 8),
            "sprite_dot_live_off": (0, 0, 0, 0),
        },
    },
}


def normalize_family(family, export_dir, output_dir):
    scale = family["scale"]
    members = family["members"]
    union = [max(pad[side] for pad in members.values()) for side in range(4)]
    print(f"  shared padding (left, bottom, right, top): {tuple(union)}")

    for name, (left, bottom, right, top) in members.items():
        source = Image.open(export_dir / f"{name}.png").convert("RGBA")
        extra_left = (union[0] - left) * scale
        extra_bottom = (union[1] - bottom) * scale
        extra_right = (union[2] - right) * scale
        extra_top = (union[3] - top) * scale

        canvas = Image.new(
            "RGBA",
            (source.width + extra_left + extra_right, source.height + extra_top + extra_bottom),
            (0, 0, 0, 0),
        )
        canvas.paste(source, (extra_left, extra_top))
        canvas.save(output_dir / f"{name}.png", optimize=True)
        print(f"  {name}: {source.size} -> {canvas.size}")


def main():
    if len(sys.argv) != 3:
        sys.exit(__doc__)

    export_dir, output_dir = Path(sys.argv[1]), Path(sys.argv[2])
    output_dir.mkdir(parents=True, exist_ok=True)
    for family_name, family in FAMILIES.items():
        print(family_name)
        normalize_family(family, export_dir, output_dir)


if __name__ == "__main__":
    main()
