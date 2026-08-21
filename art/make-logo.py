from PIL import Image, ImageDraw

# One source of truth for the shape, in a 0..100 space, so the raster exports and
# the SVG cannot drift apart.
BG      = (18, 22, 30)
ACCENT  = (52, 169, 232)
INK     = (255, 255, 255)
# Blended flat rather than drawn with alpha: ImageDraw replaces pixels instead of
# compositing, so a translucent stroke would leave holes in the icon and shift
# colour with whatever page it is shown on.
EDGE    = tuple(round(a * 0.27 + b * 0.73) for a, b in zip(ACCENT, BG))

# The game's own filter glyph: a funnel. Recognisable at a glance to anyone who
# has opened the panel this mod adds buttons to.
FUNNEL = [(13, 30), (87, 30), (57, 62), (57, 88), (43, 94), (43, 62)]

# The item that the filter gets derived from, dropping into the funnel's mouth.
CUBE = (38, 10, 62, 34)      # x0, y0, x1, y1
CUBE_R = 5

S = 8  # supersample factor


def render(px):
    n = px * S
    k = n / 100.0
    img = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    d.rounded_rectangle((0, 0, n - 1, n - 1), radius=int(23 * k), fill=BG + (255,))
    d.rounded_rectangle((int(2.5 * k), int(2.5 * k), n - 1 - int(2.5 * k), n - 1 - int(2.5 * k)),
                        radius=int(20 * k), outline=EDGE + (255,), width=max(1, int(1.2 * k)))

    d.polygon([(x * k, y * k) for x, y in FUNNEL], fill=INK + (255,))

    # Knockout ring keeps the cube legible where it crosses the white funnel.
    pad = 3.2 * k
    d.rounded_rectangle((CUBE[0] * k - pad, CUBE[1] * k - pad, CUBE[2] * k + pad, CUBE[3] * k + pad),
                        radius=int((CUBE_R + 3.2) * k), fill=BG + (255,))
    d.rounded_rectangle((CUBE[0] * k, CUBE[1] * k, CUBE[2] * k, CUBE[3] * k),
                        radius=int(CUBE_R * k), fill=ACCENT + (255,))

    return img.resize((px, px), Image.LANCZOS)


def svg():
    pts = " ".join(f"{x},{y}" for x, y in FUNNEL)
    return f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="512" height="512">
  <rect width="100" height="100" rx="23" fill="rgb{BG}"/>
  <rect x="2.5" y="2.5" width="95" height="95" rx="20" fill="none"
        stroke="rgb{EDGE}" stroke-width="1.2"/>
  <polygon points="{pts}" fill="rgb{INK}"/>
  <rect x="{CUBE[0]-3.2}" y="{CUBE[1]-3.2}" width="{CUBE[2]-CUBE[0]+6.4}" height="{CUBE[3]-CUBE[1]+6.4}"
        rx="{CUBE_R+3.2}" fill="rgb{BG}"/>
  <rect x="{CUBE[0]}" y="{CUBE[1]}" width="{CUBE[2]-CUBE[0]}" height="{CUBE[3]-CUBE[1]}"
        rx="{CUBE_R}" fill="rgb{ACCENT}"/>
</svg>
'''


if __name__ == "__main__":
    import io, os
    out = r"C:\Users\drwko\SmartSlotFilter\art"
    os.makedirs(out, exist_ok=True)
    for px in (512, 256, 128, 64):
        render(px).save(os.path.join(out, f"logo-{px}.png"))
    io.open(os.path.join(out, "logo.svg"), "w", encoding="utf-8", newline="\n").write(svg())
    # contact sheet, so the small sizes can be judged before shipping
    sheet = Image.new("RGB", (760, 200), (245, 246, 248))
    x = 24
    for px in (128, 64, 32):
        sheet.paste(render(px), (x, (200 - px) // 2), render(px)); x += px + 28
    dark = Image.new("RGB", (360, 200), (24, 27, 33)); sheet.paste(dark, (400, 0))
    x = 424
    for px in (128, 64, 32):
        sheet.paste(render(px), (x, (200 - px) // 2), render(px)); x += px + 28
    sheet.save("logo-sheet.png")
    print("ok")
