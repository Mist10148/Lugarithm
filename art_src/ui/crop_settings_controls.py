from pathlib import Path
from PIL import Image

src = Path("Assets/UI/Sprites/LugarithmUi/Settings/settings_controls_generated.png")
out = src.parent / "Controls"
out.mkdir(parents=True, exist_ok=True)
im = Image.open(src).convert("RGBA")
px = im.load(); w, h = im.size
seen = set(); boxes = []
for y in range(h):
    for x in range(w):
        if (x, y) in seen or px[x, y][3] < 32:
            continue
        stack=[(x,y)]; seen.add((x,y)); xs=[]; ys=[]
        while stack:
            a,b=stack.pop(); xs.append(a); ys.append(b)
            for q in ((a-1,b),(a+1,b),(a,b-1),(a,b+1)):
                if 0 <= q[0] < w and 0 <= q[1] < h and q not in seen and px[q[0],q[1]][3] >= 32:
                    seen.add(q); stack.append(q)
        if len(xs) > 120:
            boxes.append((min(xs),min(ys),max(xs)+1,max(ys)+1))
boxes.sort(key=lambda b:(b[1],b[0]))
names = ["selector_normal","selector_selected","selector_hover","selector_disabled",
         "slider_empty","knob_a","knob_b","slider_fill","knob_c","knob_d",
         "icon_gameplay","icon_controls","icon_audio","icon_dialogue","icon_code","close"]
for name, box in zip(names, boxes):
    im.crop(box).save(out / f"{name}.png")
print(f"wrote {min(len(names),len(boxes))} controls from {len(boxes)} components")
