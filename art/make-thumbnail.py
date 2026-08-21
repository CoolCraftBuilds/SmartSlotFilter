from PIL import Image, ImageDraw, ImageFilter, ImageFont

SRC = r"C:\Users\drwko\Pictures\Screenshots\Schermafbeelding 2026-08-17 205033.png"
OUT = r"C:\Users\drwko\SmartSlotFilter\art\thumbnail-1280x720.png"

W, H = 1280, 720
ACCENT = (52, 169, 232)
INK = (255, 255, 255)
MUTED = (168, 182, 196)

def font(name, size):
    return ImageFont.truetype(rf"C:\Windows\Fonts\{name}", size)

src = Image.open(SRC).convert("RGB")

# Backdrop: the same shot blown up, blurred and darkened. Keeps the card sitting
# in the game's own colours instead of on a flat rectangle.
scale = max(W / src.width, H / src.height) * 1.6
bg = src.resize((int(src.width * scale), int(src.height * scale)), Image.LANCZOS)
bg = bg.crop(((bg.width - W) // 2, (bg.height - H) // 2,
              (bg.width - W) // 2 + W, (bg.height - H) // 2 + H))
bg = bg.filter(ImageFilter.GaussianBlur(28))
bg = Image.blend(bg, Image.new("RGB", (W, H), (10, 13, 19)), 0.62)

canvas = bg
d = ImageDraw.Draw(canvas)

# --- the crisp cutout: filter panel plus the dropdown holding the two buttons
crop = src.crop((367, 82, 790, 492))
cs = 1.40
card = crop.resize((int(crop.width * cs), int(crop.height * cs)), Image.LANCZOS)

cx, cy = W - card.width - 56, (H - card.height) // 2

shadow = Image.new("RGBA", (card.width + 80, card.height + 80), (0, 0, 0, 0))
ImageDraw.Draw(shadow).rounded_rectangle(
    (40, 40, 40 + card.width, 40 + card.height), radius=14, fill=(0, 0, 0, 190))
shadow = shadow.filter(ImageFilter.GaussianBlur(22))
canvas.paste(shadow, (cx - 40, cy - 40), shadow)
canvas.paste(card, (cx, cy))

# --- ring around the two buttons the mod adds, in cutout coordinates
bx0, by0, bx1, by1 = 263, 16, 403, 75
ring = [cx + int(bx0 * cs) - 6, cy + int(by0 * cs) - 6,
        cx + int(bx1 * cs) + 6, cy + int(by1 * cs) + 6]
glow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
ImageDraw.Draw(glow).rounded_rectangle(ring, radius=10, outline=ACCENT + (255,), width=4)
canvas.paste(glow.filter(ImageFilter.GaussianBlur(7)), (0, 0), glow.filter(ImageFilter.GaussianBlur(7)))
d.rounded_rectangle(ring, radius=10, outline=ACCENT, width=3)

# --- left column
x = 64
d.text((x, 236), "SMART", font=font("seguibl.ttf", 78), fill=INK)
d.text((x, 316), "SLOT FILTER", font=font("seguibl.ttf", 78), fill=ACCENT)

d.rectangle((x, 424, x + 72, 428), fill=ACCENT)

for i, line in enumerate([
        "Sets a slot's filter from the",
        "item that is already in it.",
]):
    d.text((x, 456 + i * 34), line, font=font("segoeui.ttf", 27), fill=MUTED)

d.text((x, 556), "One click per slot. Or one for the whole container.",
       font=font("segoeuii.ttf", 20), fill=(120, 134, 148))

d.text((x, 636), "COOLCRAFTBUILDS", font=font("segoeuib.ttf", 17), fill=(96, 110, 124))

canvas.save(OUT)
print("wrote", OUT, canvas.size)
