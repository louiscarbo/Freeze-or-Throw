import struct
import zlib
from pathlib import Path

import numpy

OUTPUT_DIRECTORY = Path(__file__).resolve().parent

WOOD_SIZE = 1024
WOOD_BASE_RGB = (217, 179, 130)
WOOD_GRAIN_CONTRAST = 0.72

KNIT_SIZE = 512
KNIT_COLUMN_COUNT = 4
KNIT_ROW_COUNT = 6
KNIT_CHEVRON_RISE_IN_ROWS = 1.0
KNIT_GAP_BRIGHTNESS = 0.5
KNIT_NORMAL_STRENGTH = 7.0


def write_png(path, pixels):
    height, width, channel_count = pixels.shape
    colour_type = {1: 0, 3: 2}[channel_count]
    rows = pixels.reshape(height, width * channel_count)

    sub_filtered = rows.copy()
    sub_filtered[:, channel_count:] = rows[:, channel_count:] - rows[:, :-channel_count]
    up_filtered = rows.copy()
    up_filtered[1:] = rows[1:] - rows[:-1]

    smallest = None
    for filter_type, filtered in ((0, rows), (1, sub_filtered), (2, up_filtered)):
        with_filter_byte = numpy.concatenate(
            [numpy.full((height, 1), filter_type, numpy.uint8), filtered], axis=1)
        compressed = zlib.compress(with_filter_byte.tobytes(), 9)
        if smallest is None or len(compressed) < len(smallest):
            smallest = compressed

    def chunk(chunk_type, data):
        body = chunk_type + data
        return struct.pack(">I", len(data)) + body + struct.pack(">I", zlib.crc32(body))

    header = struct.pack(">IIBBBBB", width, height, 8, colour_type, 0, 0, 0)
    path.write_bytes(
        b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", header) + chunk(b"IDAT", smallest) + chunk(b"IEND", b""))
    print(f"REPORT {path.name}: {path.stat().st_size / 1024:.0f} KB")


def to_png_rows(values_with_v_up):
    clipped = numpy.clip(values_with_v_up, 0.0, 1.0)
    as_bytes = numpy.round(clipped * 255).astype(numpy.uint8)
    if as_bytes.ndim == 2:
        as_bytes = as_bytes[:, :, None]
    return numpy.ascontiguousarray(as_bytes[::-1])


def smoothstep(edge_low, edge_high, value):
    blend = numpy.clip((value - edge_low) / (edge_high - edge_low), 0.0, 1.0)
    return blend * blend * (3 - 2 * blend)


def periodic_value_noise(across, along, cells_across, cells_along, seed):
    lattice = numpy.random.default_rng(seed).random((cells_along, cells_across))
    scaled_across = across * cells_across
    scaled_along = along * cells_along
    cell_across = numpy.floor(scaled_across).astype(int)
    cell_along = numpy.floor(scaled_along).astype(int)
    blend_across = smoothstep(0.0, 1.0, scaled_across - cell_across)
    blend_along = smoothstep(0.0, 1.0, scaled_along - cell_along)
    low_across = cell_across % cells_across
    high_across = (cell_across + 1) % cells_across
    low_along = cell_along % cells_along
    high_along = (cell_along + 1) % cells_along
    bottom = lattice[low_along, low_across] * (1 - blend_across) + lattice[low_along, high_across] * blend_across
    top = lattice[high_along, low_across] * (1 - blend_across) + lattice[high_along, high_across] * blend_across
    return bottom * (1 - blend_along) + top * blend_along


def texel_grid(size):
    centres = (numpy.arange(size) + 0.5) / size
    return numpy.meshgrid(centres, centres)


def build_wood_albedo():
    across, along = texel_grid(WOOD_SIZE)
    warp = (
        0.02 * (periodic_value_noise(across, along, 3, 2, seed=11) - 0.5)
        + 0.006 * (periodic_value_noise(across, along, 7, 5, seed=12) - 0.5)
    )
    warped = across + warp

    broad_streaks = (
        0.5 * periodic_value_noise(warped, along, 16, 1, seed=21)
        + 0.3 * periodic_value_noise(warped, along, 41, 1, seed=22)
        + 0.2 * periodic_value_noise(warped, along, 97, 1, seed=23)
    )
    fibres = periodic_value_noise(warped, along, 190, 5, seed=31)
    ring_source = periodic_value_noise(warped, along, 11, 1, seed=41)
    ring_lines = numpy.exp(-(((ring_source - 0.5) / 0.035) ** 2))

    grain = 0.2 * (broad_streaks - 0.5) + 0.07 * (fibres - 0.5) - 0.07 * ring_lines
    for label, contrast in (("full strength", 1.0), ("used", WOOD_GRAIN_CONTRAST)):
        candidate = 1.0 + contrast * grain
        candidate = candidate / candidate.mean()
        low, high = numpy.percentile(candidate, [5, 95])
        print(f"REPORT wood grain contrast {label} ({contrast}): "
              f"5th to 95th percentile lightness {low:.3f} to {high:.3f}, spread {(high - low) * 100:.1f}%")
    lightness = 1.0 + WOOD_GRAIN_CONTRAST * grain
    lightness = lightness / lightness.mean()

    base = numpy.array(WOOD_BASE_RGB) / 255
    colour = numpy.stack([
        base[0] * lightness ** 0.9,
        base[1] * lightness ** 1.15,
        base[2] * lightness ** 1.45,
    ], axis=-1)
    write_png(OUTPUT_DIRECTORY / "wood-albedo.png", to_png_rows(colour))


def knit_height_field():
    across, along = texel_grid(KNIT_SIZE)
    column_position = across * KNIT_COLUMN_COUNT
    distance_from_column_centre = numpy.abs(column_position - numpy.floor(column_position) - 0.5)

    chevron_position = along * KNIT_ROW_COUNT - KNIT_CHEVRON_RISE_IN_ROWS * distance_from_column_centre * 2
    across_strand = chevron_position - numpy.floor(chevron_position)
    strand_profile = numpy.clip(1.0 - (2 * across_strand - 1) ** 2, 0.0, None) ** 0.6

    centre_crease = 0.55 + 0.45 * smoothstep(0.0, 0.14, distance_from_column_centre)
    column_gap = smoothstep(0.5, 0.36, distance_from_column_centre) ** 0.7
    return strand_profile * centre_crease * column_gap


def build_knit_textures():
    height = knit_height_field()

    albedo = KNIT_GAP_BRIGHTNESS + (1.0 - KNIT_GAP_BRIGHTNESS) * smoothstep(0.0, 0.55, height)
    albedo = albedo * (0.94 + 0.06 * height)
    write_png(OUTPUT_DIRECTORY / "knit-albedo.png", to_png_rows(albedo))

    slope_across = (numpy.roll(height, -1, axis=1) - numpy.roll(height, 1, axis=1)) / 2
    slope_along = (numpy.roll(height, -1, axis=0) - numpy.roll(height, 1, axis=0)) / 2
    normal = numpy.stack([
        -slope_across * KNIT_NORMAL_STRENGTH,
        -slope_along * KNIT_NORMAL_STRENGTH,
        numpy.ones_like(height),
    ], axis=-1)
    normal = normal / numpy.linalg.norm(normal, axis=-1, keepdims=True)
    write_png(OUTPUT_DIRECTORY / "knit-normal.png", to_png_rows(normal * 0.5 + 0.5))


build_wood_albedo()
build_knit_textures()
