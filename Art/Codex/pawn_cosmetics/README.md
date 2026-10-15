# Pawn cosmetic compositor

From the repository root (requires Python 3.10+ and Pillow):

```powershell
python -B Art/Codex/compose-pawn-cosmetics.py
python -B Art/Codex/compose-pawn-cosmetics.py --width 3000 --output Art/Codex/custom-sheet.png
python -B -m unittest discover -s Art/Codex -p test_pawn_cosmetics.py
```

The command also works from other directories using an absolute script path.
`--rimworld-data PATH` selects an installed RimWorld/Data directory for adult
geometry. The default Steam installation is optional; extracted reference art
and geometry defaults make the script usable without it.

## Where to change things

| Change | Location |
| --- | --- |
| Display names, variant captions, reference bodies/head shapes, palette | `settings.py` |
| Spacing, fonts, background, rendering scale | `SheetStyle` / `RenderStyle` in `settings.py` |
| XML interpretation, game class behavior, inherited head geometry | `definitions.py` |
| Which variants share a group, body/head/sex comparisons, prerequisites, sorting | `catalog.py` |
| Tint masks, silhouettes, offsets, mesh scaling, compositing | `rendering.py` |
| Measuring, cropping, captions, fixed-width row packing | `layout.py` |
| Typed definitions and selected preview data | `model.py` |
| Command arguments and output | `../compose-pawn-cosmetics.py` |

New XML cosmetics normally require no script edits. A label override is keyed
by gene name and render-node debug label, so two nodes sharing a texture can
have different names (cow/dragon horns). Coverage validation rejects omitted
humanlike attachment/head art before saving.

Definitions are parsed once. The catalog first selects the primary cosmetic's
comparison bodies/heads, then includes prerequisite graphics. This keeps the
scaly tail grouped with the smooth tail even though scaleskin contributes head
and body overlays. The renderer receives complete previews and never reads XML
or decides grouping. Packing measures rendered alpha bounds and captions, keeps
each group intact, and preserves catalog order.

This is a focused static art preview, not a complete RimWorld renderer. It
approximates adult geometry and cutout/pattern shaders without lighting,
random brightness, animation, or crawling. Conditional nodes represent
alternative variants; prerequisites that themselves have multiple variants
are rejected because choosing one would be ambiguous.
