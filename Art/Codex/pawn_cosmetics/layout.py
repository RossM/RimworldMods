"""Measure rendered triplets and pack complete cosmetic groups into a fixed width."""

from dataclasses import dataclass
from math import ceil

from PIL import Image, ImageDraw, ImageFont

from .model import OUTPUT_FACINGS
from .settings import SheetStyle


def pack_rows(widths, available_width, gap):
    rows = []
    start, used = 0, 0
    for index, width in enumerate(widths):
        if width > available_width:
            raise ValueError(f"Group width {width} exceeds available row width {available_width}")
        required = width + (gap if index > start else 0)
        if used + required > available_width:
            rows.append(range(start, index))
            start, used = index, width
        else:
            used += required
    if widths:
        rows.append(range(start, len(widths)))
    return rows


@dataclass(frozen=True)
class Frame:
    image: Image.Image
    bounds: tuple[int, int, int, int]
    width: int


@dataclass(frozen=True)
class Triplet:
    label: str
    frames: tuple[Frame, ...]
    width: int


@dataclass(frozen=True)
class Card:
    label: str
    triplets: tuple[Triplet, ...]
    graphics_width: int
    width: int


@dataclass(frozen=True)
class Row:
    cards: tuple[Card, ...]
    width: int
    bottom: int
    caption_lines: int
    height: int


class SheetComposer:
    def __init__(self, renderer, style=SheetStyle()):
        self.renderer, self.style = renderer, style
        for filename in style.fonts:
            try:
                self.font = ImageFont.truetype(filename, style.font_size)
                break
            except OSError:
                continue
        else:
            self.font = ImageFont.load_default()

    def _measure(self, group):
        style = self.style
        draw = ImageDraw.Draw(Image.new("RGB", (1, 1)))
        triplets = []
        for preview in group.previews:
            frames = []
            for facing in OUTPUT_FACINGS:
                image = self.renderer.render(preview, facing)
                bounds = image.getchannel("A").getbbox()
                if bounds is None:
                    raise ValueError(f"Empty preview: {group.label} / {preview.label} / {facing.name}")
                frames.append(Frame(image, bounds, bounds[2] - bounds[0] + 2 * style.horizontal_padding))
            width = sum(frame.width for frame in frames) + (len(frames) - 1) * style.view_gap
            if len(group.previews) > 1:
                width = max(width, ceil(draw.textlength(preview.label, font=self.font)))
            triplets.append(Triplet(preview.label, tuple(frames), width))
        graphics_width = sum(triplet.width for triplet in triplets) + (len(triplets) - 1) * style.version_gap
        return Card(group.label, tuple(triplets), graphics_width,
                    max(graphics_width, ceil(draw.textlength(group.label, font=self.font))))

    def _paint_card(self, sheet, draw, card, left, top, top_edge, bottom_edge, caption_lines):
        style = self.style
        view_height = bottom_edge - top_edge
        x = left + (card.width - card.graphics_width) // 2
        for triplet in card.triplets:
            start = x
            frames_width = sum(frame.width for frame in triplet.frames) + (len(triplet.frames) - 1) * style.view_gap
            x += (triplet.width - frames_width) // 2
            for frame in triplet.frames:
                crop = frame.image.crop((frame.bounds[0] - style.horizontal_padding, top_edge,
                                         frame.bounds[2] + style.horizontal_padding, bottom_edge))
                sheet.paste(crop, (x, top), crop)
                x += frame.width + style.view_gap
            if len(card.triplets) > 1:
                draw.text((start + triplet.width / 2, top + view_height + style.label_top), triplet.label,
                          fill=(40, 40, 40), font=self.font, anchor="mt")
            x = start + triplet.width + style.version_gap
        caption_y = top + view_height + style.label_top + (caption_lines - 1) * style.line_height
        draw.text((left + card.width / 2, caption_y), card.label,
                  fill=(24, 24, 24), font=self.font, anchor="mt")

    def compose(self, groups, width):
        style = self.style
        cards = [self._measure(group) for group in groups]
        if not cards:
            raise ValueError("No cosmetics to compose")
        widest = max(cards, key=lambda card: card.width)
        minimum = widest.width + 2 * style.margin
        if width < minimum:
            raise ValueError(f"--width must be at least {minimum} pixels to fit {widest.label!r}")
        available = width - 2 * style.margin
        packed = pack_rows([card.width for card in cards], available, style.group_gap)
        top_edge = min(frame.bounds[1] for card in cards for triplet in card.triplets
                       for frame in triplet.frames) - style.vertical_padding
        rows = []
        for indices in packed:
            row_cards = tuple(cards[index] for index in indices)
            bottom = max(frame.bounds[3] for card in row_cards for triplet in card.triplets
                         for frame in triplet.frames) + style.vertical_padding
            lines = 2 if any(len(card.triplets) > 1 for card in row_cards) else 1
            row_width = sum(card.width for card in row_cards) + (len(row_cards) - 1) * style.group_gap
            rows.append(Row(row_cards, row_width, bottom, lines,
                            bottom - top_edge + style.caption_height(lines)))
        height = 2 * style.margin + sum(row.height for row in rows) + (len(rows) - 1) * style.row_gap
        sheet = Image.new("RGB", (width, height), style.background)
        draw = ImageDraw.Draw(sheet)
        top = style.margin
        for row in rows:
            left = style.margin + (available - row.width) // 2
            for card in row.cards:
                self._paint_card(sheet, draw, card, left, top, top_edge, row.bottom, row.caption_lines)
                left += card.width + style.group_gap
            top += row.height + style.row_gap
        return sheet
