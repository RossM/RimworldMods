"""Compose the eleven source portraits and transparent icons with Pillow."""

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont, ImageOps


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = Path(__file__).resolve().parent
FONTS = Path("C:/Windows/Fonts")
NAMES = [
    "Bossaps", "Chyrr", "Dvergr", "Nixie", "Omegabeaver", "Scaleborn",
    "Succuboid", "Titan", "Trog", "Warcat", "Zeegee",
]
SIZE = 704
GAP = 24
MARGIN = 40


def font(filename, size):
    return ImageFont.truetype(str(FONTS / filename), size)


def icon(name, size):
    source = Image.open(ROOT / "XylXenos/Textures/Xyl/UI/Icons/Xenotypes" / f"{name}_small.png").convert("RGBA")
    return ImageOps.contain(source, (size, size), Image.Resampling.LANCZOS)


def draw_tracked(draw, xy, text, typeface, tracking, fill):
    x, y = xy
    for char in text:
        draw.text((x, y), char, font=typeface, fill=fill, anchor="lt")
        x += draw.textlength(char, font=typeface) + tracking


def card(name, style):
    portrait = Image.open(ROOT / "Art/Misc" / f"{name}.png").convert("RGBA").resize((SIZE, SIZE), Image.Resampling.LANCZOS)
    if style in ("overlay", "refined"):
        refined = style == "refined"
        # A gradual scrim keeps the face clear while providing label contrast.
        scrim = Image.new("RGBA", portrait.size)
        draw = ImageDraw.Draw(scrim)
        for y in range(SIZE):
            start = .70 if refined else .64
            t = max(0, (y - SIZE * start) / (SIZE * (1 - start)))
            draw.line((0, y, SIZE, y), fill=(11, 15, 19, round(239 * t ** 1.1)))
        portrait = Image.alpha_composite(portrait, scrim)
        icon_size = 128 if refined else 112
        portrait.alpha_composite(icon(name, icon_size), (24, SIZE - icon_size - 20))
        draw = ImageDraw.Draw(portrait)
        if refined:
            typeface = font("SourceSerifPro-Semibold.ttf", 52)
            left, top, right, bottom = draw.textbbox((0, 0), name, font=typeface)
            assert 173 + right < SIZE - 24, f"Label exceeds card: {name}"
            draw.text((173, SIZE - 80 - (bottom - top) / 2 - top), name, font=typeface, fill="#f4ede0")
        else:
            draw.text((155, SIZE - 73), name, font=font("SourceSerifPro-Semibold.ttf", 48), fill="#f4ede0", anchor="lm")
        draw.rectangle((0, 0, SIZE - 1, SIZE - 1), outline="#55534e", width=1 if refined else 2)
        return portrait
    if style == "paper":
        tile = Image.new("RGBA", (SIZE, SIZE + 130), "#e2d6bf")
        tile.alpha_composite(portrait)
        tile.alpha_composite(icon(name, 104), (25, SIZE + 12))
        draw = ImageDraw.Draw(tile)
        draw.text((155, SIZE + 65), name, font=font("BASKVILL.TTF", 51), fill="#302e29", anchor="lm")
        draw.line((0, SIZE, SIZE, SIZE), fill="#88765b", width=2)
        return tile
    tile = Image.new("RGBA", (SIZE, SIZE + 120), "#202b34")
    tile.alpha_composite(portrait)
    tile.alpha_composite(icon(name, 100), (22, SIZE + 10))
    draw = ImageDraw.Draw(tile)
    typeface = font("bahnschrift.ttf", 42)
    draw_tracked(draw, (147, SIZE + 43), name.upper(), typeface, 2, "#e9eff1")
    draw.line((0, SIZE, SIZE, SIZE), fill="#617988", width=3)
    return tile


def sheet(filename, rows, style, background):
    assert sum(rows) == len(NAMES) == len(set(NAMES))
    tiles = [card(name, style) for name in NAMES]
    height = tiles[0].height
    width = max(rows) * SIZE + (max(rows) - 1) * GAP + 2 * MARGIN
    canvas = Image.new("RGBA", (width, len(rows) * height + (len(rows) - 1) * GAP + 2 * MARGIN), background)
    index = 0
    for row, count in enumerate(rows):
        start_x = (width - (count * SIZE + (count - 1) * GAP)) // 2
        for col in range(count):
            canvas.alpha_composite(tiles[index], (start_x + col * (SIZE + GAP), MARGIN + row * (height + GAP)))
            index += 1
    rgb = canvas.convert("RGB")
    destination = OUTPUT if style == "refined" else OUTPUT / "variants"
    destination.mkdir(exist_ok=True)
    rgb.save(destination / f"{filename}.png", dpi=(300, 300))
    preview = ImageOps.contain(rgb, (1500, 1400), Image.Resampling.LANCZOS)
    preview.save(destination / f"{filename}-preview.jpg", quality=94)
    print(f"{filename}: {rgb.width} x {rgb.height}; {index} portraits, icons, and names")


if __name__ == "__main__":
    sheet("01-charcoal-overlay", (4, 4, 3), "overlay", "#15191c")
    sheet("02-parchment", (4, 3, 4), "paper", "#b4a58b")
    sheet("03-slate-wide", (6, 5), "slate", "#101a22")
    sheet("xenotypes-contact-sheet", (4, 4, 3), "refined", "#15191c")
