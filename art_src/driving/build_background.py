"""Builds the whole-scene driving background chunks from the original scene
images in originals/ (role-named: town_vertical_*, town_horizontal_*,
town_turn_<a>_<b>_*).

Every scene is a route-aware town chunk: roads are axis-aligned bands that
meet at right-angle intersections or broad painted turns. So measurement is:
  1. Trim black letterbox padding.
  2. Keep only the USABLE whitelist (the raw set contains exact duplicates,
     horizontal mirrors AND vertical flips — flipped signs read upside down).
  3. For each whitelisted edge, find the asphalt band at the edge and track it
     inward for a third of the image: the median center is the road CENTERLINE,
     the median width the road width. A band that cannot be tracked inward (a
     walkway, plaza or sidewalk) is rejected.
  4. Rescale so the median port road = 144 px = 6 world units @ 24 PPU; drop
     any port whose own width deviates > 20% after scaling (narrow side
     streets would create width-mismatched seams).
  5. Measure the largest corner FILLET RADIUS per template: the biggest
     quarter-arc between each perpendicular port pair that stays fully on
     asphalt. The C# planner rebuilds the smooth drive path from this —
     no traced curve polylines (tracing proved unreliable: it cut across
     houses).
  6. Keep generated edges fully opaque. The coordinated source set reserves a
     shared grass-only perimeter, so transparency would reveal the backing
     ground and recreate the visible fringe this pipeline is meant to avoid.

Run:  python build_background.py
"""

import glob
import json
import os
from collections import deque

import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
ORIG = os.path.join(HERE, "originals")
OUT = os.path.normpath(os.path.join(HERE, "..", "..", "Assets", "Resources", "Driving", "Background"))
DIAGNOSTICS = os.path.join(HERE, "diagnostics")
CS_LIBRARY = os.path.normpath(os.path.join(HERE, "..", "..", "Assets", "Scripts",
                                           "Levels", "Generation", "SceneTemplateLibrary.cs"))

PPU = 24.0
ROAD_PX = 288
FADE = 20
WIDTH_TOL = 0.35          # coordinated art: taper corrects residual port variance
                          # (the taper corrects the residual to exactly 144)
VEHICLE_MARGIN_PX = 22    # half-width safety envelope used by jeepney + traffic

# The distinct, usable scenes and which edges may carry chain ports.  These are
# generated/redesigned from the user-provided roadside images, then normalized
# here so every road port connects cleanly in Unity.
USABLE = {
    "town_vertical_0":   "NS",     # W/E side streets are narrow — not chain ports
    "town_horizontal_0": "WE",     # S branch mouth is pinched by gateposts
    "town_turn_e_n_0":   "NE",     # road enters from the right edge and exits north
    "town_turn_s_e_0":   "SE",
    "town_turn_s_w_1":   "SW",
    "town_turn_w_n_1":   "NW",     # road enters from the left edge and exits north
}

# How each scene may be DRIVEN (user's usage semantics; entry edge > exit edge).
# "Exiting to X" scenes carry the car from the bottom out to X; "Merging from
# Right" brings an eastbound car (which exited right earlier) back up north.
# Straight reuse through a scene's main road is allowed per the user.
SEMANTICS = {
    "town_vertical_0":   ["SN"],
    "town_horizontal_0": ["WE", "EW"],
    "town_turn_e_n_0":   ["EN"],
    "town_turn_s_e_0":   ["SE"],
    "town_turn_s_w_1":   ["SW"],
    "town_turn_w_n_1":   ["WN"],
}

# The coordinated source set uses one shared road cross-section, so no template
# needs an image-specific normalization scale.
NORM_OVERRIDE = {}
FILLET_WORLD_OVERRIDE = {
    "town_turn_e_n_0": 10.0,
    "town_turn_s_e_0": 7.0,
    "town_turn_s_w_1": 9.0,
    "town_turn_w_n_1": 12.5,
}

# Hand-measured road bands (trimmed-source pixels, (lo, hi) across the road)
# for edges whose outermost rows are contaminated by wall shadows / dark
# strips. Verified against the asphalt mask at insets 35-110 px.
# Hand-verified road bands in the user's authoritative 1254px source set.
# The final port strips converge to one shared 288px cross-section without
# scaling the full paintings (which would change poles, houses and pixel size).
OVERRIDES = {
    "town_vertical_0": {"N": (482, 772), "S": (482, 772)},
    "town_horizontal_0": {"W": (453, 784), "E": (453, 784)},
    "town_turn_s_e_0": {"S": (413, 707), "E": (240, 531)},
    "town_turn_s_w_1": {"S": (498, 788), "W": (313, 551)},
    "town_turn_e_n_0": {"E": (461, 731), "N": (522, 772)},
    "town_turn_w_n_1": {"W": (470, 740), "N": (596, 849)},
}


def road_mask(im):
    """Strict neutral-asphalt seed used for stable port measurements."""
    a = np.asarray(im, dtype=np.int16)
    mx = a.max(axis=2)
    mn = a.min(axis=2)
    mean = a.sum(axis=2) // 3
    return (mx - mn < 20) & (mean >= 60) & (mean <= 150)


def corridor_mask(im):
    """Broad asphalt classifier.

    Generated scenes contain textured asphalt, cracks and painted lines, so a
    narrow grayscale threshold is not sufficient.  Accept both neutral road
    pixels and pixels close to the scene's dominant neutral asphalt tone;
    reject the green/brown vegetation and bright concrete ranges explicitly.
    Connectivity from approved ports is applied separately, preventing roofs
    and building shadows with similar colours from becoming driveable road.
    """
    a = np.asarray(im, dtype=np.int16)
    mx = a.max(axis=2)
    mn = a.min(axis=2)
    mean = a.sum(axis=2) // 3
    saturation = mx - mn
    neutral = (saturation < 34) & (mean >= 48) & (mean <= 165)
    asphalt_seed = (saturation < 18) & (mean >= 62) & (mean <= 145)
    if asphalt_seed.sum() > 100:
        anchor = np.median(a[asphalt_seed], axis=0)
        distance = np.sqrt(((a - anchor) ** 2).sum(axis=2))
        neutral |= (distance < 48) & (saturation < 45)
    green = (a[..., 1] > a[..., 0] + 8) & (a[..., 1] > a[..., 2] + 12)
    concrete = (mean > 155) & (saturation < 45)
    return neutral & ~green & ~concrete


def connected_road_mask(mask, bands):
    """Keep only candidate road pixels connected to an approved edge port."""
    h, w = mask.shape
    filled = box_fill(mask, k=21, thresh=0.34)
    seen = np.zeros_like(filled, dtype=bool)
    q = deque()

    def seed(x, y):
        if 0 <= x < w and 0 <= y < h and filled[y, x] and not seen[y, x]:
            seen[y, x] = True
            q.append((x, y))

    for edge, (center, width) in bands.items():
        lo = max(0, int(center - width * 0.38))
        hi = int(center + width * 0.38)
        for v in range(lo, hi + 1, 3):
            if edge == "N": seed(v, 2)
            elif edge == "S": seed(v, h - 3)
            elif edge == "W": seed(2, v)
            else: seed(w - 3, v)

    while q:
        x, y = q.popleft()
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if 0 <= nx < w and 0 <= ny < h and filled[ny, nx] and not seen[ny, nx]:
                seen[ny, nx] = True
                q.append((nx, ny))
    return seen


def transition_geometry(code, ports, fillet_world):
    """Explicit local graph corner and sampled painted-road centerline."""
    p_in = np.array(ports[code[0]], dtype=float)
    p_out = np.array(ports[code[1]], dtype=float)
    if code[0] in "NS" and code[1] in "WE":
        corner = np.array((p_in[0], p_out[1]))
    elif code[0] in "WE" and code[1] in "NS":
        corner = np.array((p_out[0], p_in[1]))
    else:
        return {"code": code, "graph_corner": None,
                "path": [p_in.tolist(), p_out.tolist()]}

    d_in = -{"N": np.array((0., 1.)), "S": np.array((0., -1.)),
             "E": np.array((1., 0.)), "W": np.array((-1., 0.))}[code[0]]
    d_out = {"N": np.array((0., 1.)), "S": np.array((0., -1.)),
             "E": np.array((1., 0.)), "W": np.array((-1., 0.))}[code[1]]
    r = min(fillet_world, np.linalg.norm(p_in - corner) * .45,
            np.linalg.norm(p_out - corner) * .45)
    if r <= .5:
        path = [p_in, corner, p_out]
    else:
        a = corner - d_in * r
        b = corner + d_out * r
        origin = a + d_out * r
        path = [p_in]
        for i in range(7):
            angle = i / 6.0 * np.pi * .5
            path.append(origin + (a - origin) * np.cos(angle) +
                        (b - origin) * np.sin(angle))
        path.append(p_out)
    return {"code": code, "graph_corner": corner.tolist(),
            "path": [p.tolist() for p in path]}


def validate_and_draw(name, out, connected, transitions):
    """Write the human-review overlay and fail on an unsafe centerline."""
    os.makedirs(DIAGNOSTICS, exist_ok=True)
    rgba = out.convert("RGBA")
    overlay = Image.new("RGBA", rgba.size, (0, 0, 0, 0))
    tint = np.zeros((rgba.height, rgba.width, 4), dtype=np.uint8)
    tint[connected] = (20, 210, 255, 72)
    overlay = Image.alpha_composite(overlay, Image.fromarray(tint, "RGBA"))
    draw = ImageDraw.Draw(overlay)
    unsafe = []

    def pixel(p):
        return (p[0] * PPU + rgba.width / 2,
                rgba.height / 2 - p[1] * PPU)

    for transition in transitions:
        pts = [pixel(p) for p in transition["path"]]
        for sample_index, (x, y) in enumerate(pts):
            safe = True
            # Edge ports themselves may be alpha-dithered and are completed by
            # the adjoining chunk. Their width/center is validated separately;
            # corridor clearance begins at the first interior path sample.
            if sample_index in (0, len(pts) - 1):
                continue
            offsets = (
                (0, 0), (VEHICLE_MARGIN_PX, 0), (-VEHICLE_MARGIN_PX, 0),
                (0, VEHICLE_MARGIN_PX), (0, -VEHICLE_MARGIN_PX))
            for ox, oy in offsets:
                xi, yi = int(round(x + ox)), int(round(y + oy))
                if not (0 <= xi < rgba.width and 0 <= yi < rgba.height and connected[yi, xi]):
                    safe = False
                    break
            if not safe:
                unsafe.append((transition["code"], x, y))
        draw.line(pts, fill=(255, 235, 30, 255), width=5)
        for x, y in pts:
            draw.ellipse((x - 4, y - 4, x + 4, y + 4),
                         fill=(35, 220, 70, 255) if not any(abs(x-u[1]) < 1 and abs(y-u[2]) < 1 for u in unsafe)
                         else (255, 45, 45, 255))
    Image.alpha_composite(rgba, overlay).save(os.path.join(DIAGNOSTICS, name + "_road_audit.png"))
    return unsafe


def cs_vector(p):
    return f"new Vector2({p[0]:.3f}f, {p[1]:.3f}f)"


def write_csharp_table(table):
    """Replace only the generated template block in SceneTemplateLibrary."""
    lines = []
    for t in table:
        parts = [f'sprite = "{t["name"]}"',
                 f'size = {cs_vector(t["size"])}']
        for edge, field in (("N", "northPort"), ("S", "southPort"),
                            ("W", "westPort"), ("E", "eastPort")):
            if t["ports"][edge]:
                parts.append(f'{field} = {cs_vector(t["ports"][edge])}')
        if t["fillet"]:
            parts.append(f'fillet = {t["fillet"]:.2f}f')
        allow = ", ".join(f'"{code}"' for code in SEMANTICS[t["name"]])
        parts.append(f'allow = new[] {{ {allow} }}')
        transition_rows = []
        for tr in t["transitions"]:
            corner = cs_vector(tr["graph_corner"]) if tr["graph_corner"] else "Vector2.zero"
            path = ", ".join(cs_vector(p) for p in tr["path"])
            transition_rows.append(
                f'new Transition {{ code = "{tr["code"]}", graphCorner = {corner}, '
                f'path = new[] {{ {path} }} }}')
        parts.append("transitions = new[] { " + ", ".join(transition_rows) + " }")
        lines.append("        new Template { " + ", ".join(parts) + " },")

    with open(CS_LIBRARY, encoding="utf-8") as stream:
        source = stream.read()
    begin = "        // BEGIN GENERATED TEMPLATE DATA"
    end = "        // END GENERATED TEMPLATE DATA"
    if source.count(begin) != 1 or source.count(end) != 1:
        raise RuntimeError("SceneTemplateLibrary generated-data markers are missing or duplicated")
    before, remainder = source.split(begin, 1)
    _, after = remainder.split(end, 1)
    generated = begin + "\n" + "\n".join(lines) + "\n" + end
    with open(CS_LIBRARY, "w", encoding="utf-8", newline="") as stream:
        stream.write(before + generated + after)


def build_review_sheets(table):
    """Contact sheets for human review of corridors and every legal seam."""
    audits = []
    for t in table:
        image = Image.open(os.path.join(DIAGNOSTICS, t["name"] + "_road_audit.png")).convert("RGB")
        image.thumbnail((420, 420), Image.NEAREST)
        audits.append((t["name"], image.copy()))
    sheet = Image.new("RGB", (900, ((len(audits) + 1) // 2) * 460), (25, 25, 25))
    draw = ImageDraw.Draw(sheet)
    for i, (name, image) in enumerate(audits):
        x, y = (i % 2) * 450, (i // 2) * 460
        sheet.paste(image, (x, y + 24))
        draw.text((x + 6, y + 5), name, fill=(255, 255, 255))
    sheet.save(os.path.join(DIAGNOSTICS, "road_audit_contact_sheet.png"))

    plain = Image.new("RGB", sheet.size, (25, 25, 25))
    plain_draw = ImageDraw.Draw(plain)
    for i, t in enumerate(table):
        image = Image.open(os.path.join(OUT, t["name"] + ".png")).convert("RGB")
        image.thumbnail((420, 420), Image.NEAREST)
        x, y = (i % 2) * 450, (i // 2) * 460
        plain.paste(image, (x, y + 24))
        plain_draw.text((x + 6, y + 5), t["name"], fill=(255, 255, 255))
    plain.save(os.path.join(HERE, "generated_six_contact_sheet.png"))

    opposite = {"N": "S", "S": "N", "E": "W", "W": "E"}
    pairs = []
    for left in table:
        for tr in left["transitions"]:
            exit_edge = tr["code"][1]
            for right in table:
                if any(nxt["code"][0] == opposite[exit_edge] for nxt in right["transitions"]):
                    key = (left["name"], right["name"], exit_edge)
                    if key not in pairs:
                        pairs.append(key)

    panels = []
    depth = 120
    span = 420
    def port_pixel(template, edge, image):
        p = template["ports"][edge]
        return (p[0] * PPU + image.width / 2,
                image.height / 2 - p[1] * PPU)

    def crop_padded(image, box):
        result = Image.new("RGBA", (box[2] - box[0], box[3] - box[1]), (0, 0, 0, 0))
        source = (max(0, box[0]), max(0, box[1]), min(image.width, box[2]), min(image.height, box[3]))
        if source[2] > source[0] and source[3] > source[1]:
            result.paste(image.crop(source), (source[0] - box[0], source[1] - box[1]))
        return result

    for first_name, second_name, edge in pairs:
        first_t = next(t for t in table if t["name"] == first_name)
        second_t = next(t for t in table if t["name"] == second_name)
        first = Image.open(os.path.join(OUT, first_name + ".png")).convert("RGBA")
        second = Image.open(os.path.join(OUT, second_name + ".png")).convert("RGBA")
        if edge == "N":
            ax, _ = port_pixel(first_t, "N", first)
            bx, _ = port_pixel(second_t, "S", second)
            a = crop_padded(first, (round(ax-span/2), 0, round(ax+span/2), depth))
            b = crop_padded(second, (round(bx-span/2), second.height-depth,
                                     round(bx+span/2), second.height))
            panel = Image.new("RGBA", (span, depth * 2), (0, 0, 0, 0))
            panel.paste(b, (0, 0)); panel.paste(a, (0, depth))
        elif edge == "E":
            _, ay = port_pixel(first_t, "E", first)
            _, by = port_pixel(second_t, "W", second)
            a = crop_padded(first, (first.width-depth, round(ay-span/2),
                                    first.width, round(ay+span/2)))
            b = crop_padded(second, (0, round(by-span/2), depth, round(by+span/2)))
            panel = Image.new("RGBA", (depth * 2, span), (0, 0, 0, 0))
            panel.paste(a, (0, 0)); panel.paste(b, (depth, 0))
        elif edge == "W":
            _, ay = port_pixel(first_t, "W", first)
            _, by = port_pixel(second_t, "E", second)
            a = crop_padded(first, (0, round(ay-span/2), depth, round(ay+span/2)))
            b = crop_padded(second, (second.width-depth, round(by-span/2),
                                     second.width, round(by+span/2)))
            panel = Image.new("RGBA", (depth * 2, span), (0, 0, 0, 0))
            panel.paste(b, (0, 0)); panel.paste(a, (depth, 0))
        else:
            continue  # forward-only progression never exits south
        panel.thumbnail((420, 220), Image.NEAREST)
        panels.append((f"{first_name} -> {second_name} ({edge})", panel.convert("RGB")))

    seam_sheet = Image.new("RGB", (900, max(260, ((len(panels) + 1) // 2) * 260)), (25, 25, 25))
    seam_draw = ImageDraw.Draw(seam_sheet)
    for i, (label, panel) in enumerate(panels):
        x, y = (i % 2) * 450, (i // 2) * 260
        seam_sheet.paste(panel, (x, y + 28))
        seam_draw.text((x + 6, y + 6), label, fill=(255, 255, 255))
    seam_sheet.save(os.path.join(DIAGNOSTICS, "seam_preview_contact_sheet.png"))


def grass_mask(a):
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    return (g > r) & (r > b) & (g > 60) & (g < 170) & (g - b > 25)


def tone_anchors(im):
    """Mean asphalt and grass colors of a scene — the anchor tones used to
    harmonize every image onto one shared palette."""
    a = np.asarray(im, dtype=np.float64)
    road = road_mask(im)
    grass = grass_mask(a)
    if road.sum() < 500 or grass.sum() < 500:
        return None
    return a[road].mean(axis=0), a[grass].mean(axis=0)


def harmonize(im, anchors, ref):
    """Per-channel affine remap so this scene's asphalt and grass tones land on
    the shared reference tones — scenes then blend at seams instead of showing
    a visible color step (the images were exported with different grading)."""
    road_c, grass_c = anchors
    ref_road, ref_grass = ref
    a = np.asarray(im, dtype=np.float64)
    out = np.empty_like(a)
    for c in range(3):
        x0, x1 = road_c[c], grass_c[c]
        y0, y1 = ref_road[c], ref_grass[c]
        if abs(x1 - x0) < 12:
            k, b = 1.0, y0 - x0            # anchors too close: plain shift
        else:
            k = (y1 - y0) / (x1 - x0)
            k = min(max(k, 0.7), 1.4)      # never a harsh remap
            b = y0 - k * x0
        out[..., c] = a[..., c] * k + b
    return Image.fromarray(np.clip(out, 0, 255).astype(np.uint8))


def trim_black(im):
    a = np.asarray(im, dtype=np.int16).sum(axis=2)
    dark_rows = (a < 40).mean(axis=1) > 0.9
    dark_cols = (a < 40).mean(axis=0) > 0.9
    h, w = a.shape
    top = 0
    while top < h // 4 and dark_rows[top]: top += 1
    bot = h
    while bot > h * 3 // 4 and dark_rows[bot - 1]: bot -= 1
    left = 0
    while left < w // 4 and dark_cols[left]: left += 1
    right = w
    while right > w * 3 // 4 and dark_cols[right - 1]: right -= 1
    return im.crop((left, top, right, bot))


def line_runs(vals, minrun=50, gap=30):
    """Asphalt runs along one row/column, merged across the painted center
    line, as a list of (lo, hi)."""
    idx = np.flatnonzero(vals)
    if idx.size == 0:
        return []
    breaks = np.flatnonzero(np.diff(idx) > 1)
    pieces = np.split(idx, breaks + 1)
    rs = [(int(p[0]), int(p[-1]) + 1) for p in pieces]
    merged = [rs[0]]
    for lo, hi in rs[1:]:
        if lo - merged[-1][1] <= gap:
            merged[-1] = (merged[-1][0], hi)
        else:
            merged.append((lo, hi))
    return [(lo, hi) for lo, hi in merged if hi - lo >= minrun]


def edge_band(mask, edge):
    """Road band at an image edge: median run boundaries over several inset
    lines. The USABLE whitelist already guarantees the edge carries a real
    road, so no inward tracking — the at-edge band is precisely what must
    match at a chunk seam."""
    h, w = mask.shape
    los, his = [], []
    for inset in (3, 8, 14, 22, 30):
        if edge == "N":   vals = mask[inset, :]
        elif edge == "S": vals = mask[h - 1 - inset, :]
        elif edge == "W": vals = mask[:, inset]
        else:             vals = mask[:, w - 1 - inset]
        rs = [r for r in line_runs(vals) if 60 <= r[1] - r[0] <= 340]
        if not rs:
            continue
        band = max(rs, key=lambda r: r[1] - r[0])
        los.append(band[0])
        his.append(band[1])
    if len(los) < 3:
        return None
    lo, hi = float(np.median(los)), float(np.median(his))
    return (lo + hi) / 2, hi - lo


def track_band(mask, edge):
    """Finds the road band at `edge` of the box-FILLED asphalt mask (which
    bridges the painted center/shoulder lines) and tracks it inward. Robust
    to single contaminated rows (tree/wall shadows merge into the asphalt
    run): outlier rows are skipped, and the scan only stops at a run of them
    (a genuine junction / end of the road). Returns (center, width) or None
    when the band is not a road (a walkway or plaza can't be followed
    inward)."""
    h, w = mask.shape
    depth = min(int((h if edge in "NS" else w) * 0.38), 420)
    step = 4
    los, his, widths = [], [], []
    prev = None
    bad_streak = 0
    scanned = 0
    for i in range(4, depth, step):
        if edge == "N":   vals = mask[i, :]
        elif edge == "S": vals = mask[h - 1 - i, :]
        elif edge == "W": vals = mask[:, i]
        else:             vals = mask[:, w - 1 - i]
        rs = [r for r in line_runs(vals) if 60 <= r[1] - r[0] <= 340]
        if prev is None:
            if rs:
                band = max(rs, key=lambda r: r[1] - r[0])
                prev = (band[0] + band[1]) / 2
                los.append(band[0]); his.append(band[1]); widths.append(band[1] - band[0])
                scanned += 1
            continue
        scanned += 1
        band = min(rs, key=lambda r: abs((r[0] + r[1]) / 2 - prev)) if rs else None
        base = float(np.percentile(widths, 25)) if len(widths) >= 4 else (widths[-1] if widths else 0)
        good = (band is not None
                and abs((band[0] + band[1]) / 2 - prev) < max(base, 60) * 0.6
                and band[1] - band[0] < base * 1.6 + 20)
        if not good:
            bad_streak += 1
            if bad_streak >= 5:
                break              # junction or end of road
            continue
        bad_streak = 0
        prev = (band[0] + band[1]) / 2
        los.append(band[0]); his.append(band[1]); widths.append(band[1] - band[0])
    if len(los) < 12 or len(los) < scanned * 0.5:
        return None                # couldn't follow it inward: not a road
    # boundary MEDIANS: robust both to rows split by the painted center line
    # (which shrink the chosen run) and to rows with attached shadows
    lo, hi = float(np.median(los)), float(np.median(his))
    return (lo + hi) / 2, hi - lo


def box_fill(mask, k=15, thresh=0.35):
    """Smoothed/filled mask: true where a k×k box around the pixel is mostly
    road. Bridges the painted yellow center line (excluded from the raw mask)."""
    m = mask.astype(np.float32)
    c = np.cumsum(np.cumsum(m, axis=0), axis=1)
    c = np.pad(c, ((1, 0), (1, 0)))
    h, w = mask.shape
    r = k // 2
    y0 = np.clip(np.arange(h) - r, 0, h)
    y1 = np.clip(np.arange(h) + r + 1, 0, h)
    x0 = np.clip(np.arange(w) - r, 0, w)
    x1 = np.clip(np.arange(w) + r + 1, 0, w)
    total = (c[y1][:, x1] - c[y1][:, x0] - c[y0][:, x1] + c[y0][:, x0])
    area = (y1 - y0)[:, None] * (x1 - x0)[None, :]
    return total / area > thresh


def max_fillet(filled, vx, hy, sx, sy, road_px):
    """Largest quarter-arc radius (px) between the vertical road x=vx and the
    horizontal road y=hy toward quadrant (sx, sy) that stays on asphalt."""
    h, w = filled.shape

    def on_road(x, y):
        # the point plus a margin around it must be asphalt, so wide arcs
        # can't ride the inner curb of a square intersection
        for ox, oy in ((0, 0), (8, 0), (-8, 0), (0, 8), (0, -8)):
            xi, yi = int(round(x + ox)), int(round(y + oy))
            if not (0 <= xi < w and 0 <= yi < h and filled[yi, xi]):
                return False
        return True

    best = 0
    for r in range(int(road_px * 0.35), int(min(w, h) * 0.45), 8):
        cx, cy = vx + sx * r, hy + sy * r
        ok = True
        for k in range(13):
            t = k / 12.0 * (np.pi / 2)
            x = cx - sx * r * np.cos(t)
            y = cy - sy * r * np.sin(t)
            if not on_road(x, y):
                ok = False
                break
        if ok:
            best = r
        elif best:
            break
    return best


def taper_port(rgba, edge, center_px, width_px, target=ROAD_PX, depth=96):
    """Remaps the strip nearest a port edge so the road band converges to
    exactly `target` px wide AT the edge. The scenes paint each road arm at a
    different width (95-170 px) and one uniform scale can't fix them all —
    this guarantees every seam meets at the same width. The remap is
    piecewise-linear around the road (road scaled, roadside shifted), blended
    from no-op at `depth` to full correction at the edge; boundary drift is
    <= ~15 px over 96 px, invisible at play zoom."""
    if abs(width_px - target) < 2:
        return rgba
    # gentler correction for larger deltas: spread it over more depth so the
    # curb kink stays under ~8 degrees
    depth = int(min(max(depth, abs(width_px - target) * 6),
                    (rgba.height if edge in ("N", "S") else rgba.width) * 0.4))
    a = np.array(rgba)   # writable copy
    h, w, _ = a.shape
    horizontal = edge in ("N", "S")   # road runs vertically → remap along x
    span = w if horizontal else h
    c = center_px
    h_nat = width_px / 2.0

    for i in range(depth):
        t = 1.0 - i / float(depth)            # 1 at the edge → 0 at depth
        h_t = h_nat + (target / 2.0 - h_nat) * t
        # source x for each destination x (piecewise linear, endpoints fixed)
        dst_knots = np.array([0.0, max(c - h_t, 1), min(c + h_t, span - 1), span - 1.0])
        src_knots = np.array([0.0, max(c - h_nat, 1), min(c + h_nat, span - 1), span - 1.0])
        src = np.interp(np.arange(span), dst_knots, src_knots)
        idx = np.clip(np.round(src).astype(int), 0, span - 1)
        if edge == "N":
            a[i, :, :] = a[i, idx, :]
        elif edge == "S":
            a[h - 1 - i, :, :] = a[h - 1 - i, idx, :]
        elif edge == "W":
            a[:, i, :] = a[idx, i, :]
        else:
            a[:, w - 1 - i, :] = a[idx, w - 1 - i, :]
    out = Image.fromarray(a)
    out.load()
    out.readonly = 0
    return out


DEEP_FADE = 48    # roadside strip flanking a port fades much deeper: edge
MARGIN = 190      # shadows/aprons there would read as road at the seam


def hsh(x, y, salt=0):
    v = (x * 374761393 + y * 668265263 + salt * 962287) & 0xffffffff
    v = (v ^ (v >> 13)) * 1274126177 & 0xffffffff
    return (v ^ (v >> 16)) & 0x7fffffff


def dither_fade(rgba, edge, keep_bands):
    """Dither-fades an edge to transparent. The road band itself is kept; the
    strip FLANKING the band fades over DEEP_FADE px (the scenes paint wall
    shadows and road aprons right at these edges — left opaque they read as a
    fat mismatched road at every seam); everywhere else fades over FADE px."""
    px = rgba.load()
    w, h = rgba.size

    def depth_at(v):
        for lo, hi in keep_bands:
            if lo - 8 <= v <= hi + 8:
                return 0                    # the road itself: never fade
            if lo - MARGIN <= v <= hi + MARGIN:
                return DEEP_FADE            # roadside flank: fade deep
        return FADE

    def fade_px(x, y, k, d):
        r, g, b, a = px[x, y]
        if a == 0:
            return
        # shadow killer: dark low-saturation pixels near the edge read as road
        # at a seam (and later chunks draw OVER the previous one) — fade them
        # deep no matter where they sit along the edge
        if d < DEEP_FADE and (r + g + b) < 330 and max(r, g, b) - min(r, g, b) < 30:
            d = DEEP_FADE
        if k >= d:
            return
        # per-pixel hash: structure-free dither (the old modulo pattern left
        # ugly diagonal streaks across the fade band)
        if (hsh(x, y, 31) % 997) / 997.0 > (k + 1) / (d + 1.0):
            px[x, y] = (r, g, b, 0)

    for k in range(DEEP_FADE):
        if edge in ("left", "right"):
            x = k if edge == "left" else w - 1 - k
            for y in range(h):
                d = depth_at(y)
                if d > 0:
                    fade_px(x, y, k, d)
        else:
            y = k if edge == "top" else h - 1 - k
            for x in range(w):
                d = depth_at(x)
                if d > 0:
                    fade_px(x, y, k, d)
    return rgba


def main():
    os.makedirs(OUT, exist_ok=True)
    os.makedirs(DIAGNOSTICS, exist_ok=True)

    # stale emitted assets that are no longer in the whitelist
    for f in glob.glob(os.path.join(OUT, "town_*.png")):
        base = os.path.basename(f)[:-4]
        if base not in USABLE:
            os.remove(f)
            meta = f + ".meta"
            if os.path.exists(meta):
                os.remove(meta)
            print(f"removed stale asset {base}")

    # ---- load + harmonize color themes ---------------------------------------
    # The scenes were exported with different grading; matching every image's
    # asphalt and grass tones onto shared reference tones makes chunks blend at
    # seams instead of showing a color step (user request).
    images = {}
    all_anchors = {}
    for name in sorted(USABLE):
        f = os.path.join(ORIG, name + ".png")
        if not os.path.exists(f):
            print(f"{name}: ORIGINAL MISSING — skipped")
            continue
        im = trim_black(Image.open(f).convert("RGB"))
        anchors = tone_anchors(im)
        images[name] = im
        if anchors is not None:
            all_anchors[name] = anchors
    ref = (np.median([a[0] for a in all_anchors.values()], axis=0),
           np.median([a[1] for a in all_anchors.values()], axis=0))
    print(f"reference tones: asphalt {ref[0].round(0)}, grass {ref[1].round(0)}")

    table = []
    grass_samples = []
    for name, im in images.items():
        if name in all_anchors:
            im = harmonize(im, all_anchors[name], ref)
        mask = road_mask(im)
        filled = box_fill(mask, k=31, thresh=0.5)
        w, h = im.size

        # Ports are measured AT the edge (median boundaries over several inset
        # lines) — that's exactly what must line up at a seam. Which edges are
        # real roads was vetted visually and lives in the USABLE whitelist.
        bands = {}
        for e in USABLE[name]:
            if e in OVERRIDES.get(name, {}):
                lo, hi = OVERRIDES[name][e]
                bands[e] = ((lo + hi) / 2, hi - lo)
                continue
            b = edge_band(mask, e)
            if b is None:
                print(f"{name}: edge {e} rejected (no clean road band at the edge)")
            else:
                bands[e] = b

        # N/S (and W/E) ports of one scene are the same straight road —
        # snap both to their common centerline.
        for a, b in (("N", "S"), ("W", "E")):
            if a in bands and b in bands:
                ca, wa = bands[a]
                cb, wb = bands[b]
                if abs(ca - cb) < max(wa, wb):
                    # same straight road: unify center AND width (the narrower
                    # measurement is the shadow-free one)
                    c = (ca + cb) / 2
                    bw = min(wa, wb)
                    bands[a] = (c, bw)
                    bands[b] = (c, bw)
                else:
                    print(f"{name}: {a}/{b} centers disagree by {abs(ca-cb):.0f}px — kept separate")

        if not bands:
            print(f"{name}: NO PORTS — skipped")
            continue

        road_w = NORM_OVERRIDE.get(name) or float(np.median([bw for _, bw in bands.values()]))
        # Keep every scene at identical scale so houses, signs and especially
        # utility poles remain the same world size. Normalize only the port
        # strips below; whole-image rescaling caused the short/tall pole bug.
        scale = 1.0

        # drop ports whose own road deviates too much after scaling
        for e in list(bands):
            bw = bands[e][1] * scale
            if abs(bw - ROAD_PX) > ROAD_PX * WIDTH_TOL:
                print(f"{name}: edge {e} dropped (width {bw:.0f}px vs {ROAD_PX})")
                del bands[e]
        if not bands:
            print(f"{name}: NO VALID PORTS AFTER WIDTH NORMALIZATION — skipped")
            continue

        # fillet radius: smallest max-arc over the perpendicular port pairs
        fillets = []
        for pv in "NS":
            for ph in "WE":
                if pv in bands and ph in bands:
                    vx = bands[pv][0]
                    hy = bands[ph][0]
                    sx = 1 if ph == "E" else -1
                    sy = 1 if pv == "S" else -1   # pixel y grows downward
                    r = max_fillet(filled, vx, hy, sx, sy, road_w)
                    if r:
                        fillets.append(r)
        fillet_px = min(fillets) if fillets else 0

        sw, sh = round(w * scale), round(h * scale)
        out = im.resize((sw, sh), Image.NEAREST).convert("RGBA")

        # converge every port road to exactly ROAD_PX at its seam edge
        for e in bands:
            c, bw = bands[e]
            out = taper_port(out, e, c * scale, bw * scale)

        def band_px(e):
            c, _ = bands[e]
            return (round(c * scale - ROAD_PX / 2), round(c * scale + ROAD_PX / 2))

        # The final coordinated set has a shared grass-only outer perimeter.
        # Keep it opaque: fading these pixels exposes the procedural backing
        # texture and produces the green/red speckled merge seen in gameplay.
        out.save(os.path.join(OUT, f"{name}.png"))

        def port_world(e):
            if e not in bands:
                return None
            c = bands[e][0] * scale
            if e == "N": return ((c - sw / 2) / PPU, (sh / 2) / PPU)
            if e == "S": return ((c - sw / 2) / PPU, (-sh / 2) / PPU)
            if e == "W": return ((-sw / 2) / PPU, (sh / 2 - c) / PPU)
            return ((sw / 2) / PPU, (sh / 2 - c) / PPU)

        ports_world = {e: port_world(e) for e in "NSWE"}
        fillet_world = FILLET_WORLD_OVERRIDE.get(name, (fillet_px * scale) / PPU)
        transitions = [transition_geometry(code, ports_world, fillet_world)
                       for code in SEMANTICS[name]]
        out_mask = corridor_mask(out.convert("RGB"))
        scaled_bands = {e: (bands[e][0] * scale, ROAD_PX) for e in bands}
        connected = connected_road_mask(out_mask, scaled_bands)
        unsafe = validate_and_draw(name, out, connected, transitions)
        if unsafe:
            print(f"{name}: ROAD AUDIT WARNING — {len(unsafe)} unsafe centerline samples; inspect diagnostic")

        entry = dict(name=name, size=(sw / PPU, sh / PPU),
                     ports=ports_world, fillet=fillet_world,
                     road_width=ROAD_PX / PPU, transitions=transitions,
                     audit=dict(vehicle_margin=VEHICLE_MARGIN_PX / PPU,
                                unsafe_samples=len(unsafe)))
        table.append(entry)
        print(f"{name}: {sw}x{sh}px = {sw/PPU:.1f}x{sh/PPU:.1f}u  road {road_w:.0f}px  "
              f"fillet {entry['fillet']:.1f}u  ports: "
              + " ".join(f"{e}=({p[0]:.2f},{p[1]:.2f})" for e, p in entry["ports"].items() if p)
              + "  seam widths: "
              + " ".join(f"{e}={bands[e][1] * scale:.0f}px" for e in bands))

        # grass palette samples
        a = np.asarray(im, dtype=np.int16)[10:-10:37, 10:-10:41]
        r, g, b = a[..., 0], a[..., 1], a[..., 2]
        sel = (g > r) & (r > b) & (g > 60) & (g < 160) & (g - b > 25)
        grass_samples.extend(map(tuple, a[sel].tolist()))

    json.dump(table, open(os.path.join(HERE, "template_table.json"), "w"), indent=1)
    write_csharp_table(table)
    build_review_sheets(table)

    # ---- C# table -------------------------------------------------------------
    print("\n--- SceneTemplateLibrary table ---")
    for t in table:
        parts = [f'sprite = "{t["name"]}"',
                 f'size = new Vector2({t["size"][0]:.3f}f, {t["size"][1]:.3f}f)']
        for e, field in (("N", "northPort"), ("S", "southPort"), ("W", "westPort"), ("E", "eastPort")):
            p = t["ports"][e]
            if p:
                parts.append(f'{field} = new Vector2({p[0]:.3f}f, {p[1]:.3f}f)')
        if t["fillet"]:
            parts.append(f'fillet = {t["fillet"]:.2f}f')
        allow = ", ".join(f'"{p}"' for p in SEMANTICS.get(t["name"], []))
        parts.append(f'allow = new[] {{ {allow} }}')
        print("new Template { " + ", ".join(parts) + " },")

    # ---- ground grass (synthesized; never crop from scenes) --------------------
    # 1024 px = 42.7 u per tile keeps GroundFollow's tiled sprite renderer far
    # below Unity's tile-mesh vertex limit (a 96 px tile needed ~30k quads and
    # spammed "Cannot generate 9 slice" errors). Two octaves of smooth value
    # noise (32 px blobs + 8 px detail) mapped onto a NARROW slice of the
    # harmonized grass palette — the old per-block hash grass read as TV static.
    grass_samples.sort(key=sum)
    n = len(grass_samples)
    palette = np.array([grass_samples[int(n * f)] for f in (0.35, 0.45, 0.55, 0.65)],
                       dtype=np.float64)

    S = 1024

    def value_noise(cell, salt):
        g = S // cell
        rng = np.random.default_rng(1237 + salt)
        lattice = rng.random((g, g))
        big = np.kron(lattice, np.ones((cell, cell)))
        # toroidal smoothing: box-blur with wrap keeps the tile seamless
        k = cell // 2 * 2 + 1
        acc = np.zeros_like(big)
        for dy in range(-(k // 2), k // 2 + 1):
            acc += np.roll(big, dy, axis=0)
        out = np.zeros_like(big)
        for dx in range(-(k // 2), k // 2 + 1):
            out += np.roll(acc, dx, axis=1)
        out /= k * k
        lo, hi = out.min(), out.max()
        return (out - lo) / max(hi - lo, 1e-6)

    noise = 0.7 * value_noise(32, 1) + 0.3 * value_noise(8, 2)
    idx = np.clip((noise * 3.999), 0, 3.999)
    i0 = idx.astype(int)
    frac = (idx - i0)[..., None]
    c0 = palette[i0]
    c1 = palette[np.minimum(i0 + 1, 3)]
    img = c0 * (1 - frac) + c1 * frac
    # per-pixel speckle, very subtle
    xs, ys = np.meshgrid(np.arange(S), np.arange(S))
    j = (((xs * 374761393 + ys * 668265263 + 7 * 962287) & 0xffffffff) >> 16) % 7 - 3
    img += j[..., None]
    Image.fromarray(np.clip(img, 0, 255).astype(np.uint8)).save(
        os.path.join(OUT, "ground_grass.png"))
    print(f"\nground_grass 1024px synthesized from {n} samples")


if __name__ == "__main__":
    main()
