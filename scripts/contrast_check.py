import math

def srgb_to_linear(c):
    c = c/255.0
    return c/12.92 if c <= 0.04045 else ((c+0.055)/1.055)**2.4

def rel_lum(rgb):
    r,g,b = rgb
    R = srgb_to_linear(r)
    G = srgb_to_linear(g)
    B = srgb_to_linear(b)
    return 0.2126*R + 0.7152*G + 0.0722*B

def contrast(rgb1, rgb2):
    L1 = rel_lum(rgb1)
    L2 = rel_lum(rgb2)
    L1, L2 = max(L1,L2), min(L1,L2)
    return (L1+0.05)/(L2+0.05)

def hex_to_rgb(h):
    h = h.lstrip('#')
    return tuple(int(h[i:i+2],16) for i in (0,2,4))

def blend_over(fg_rgb, alpha, bg_rgb):
    r = round(fg_rgb[0]*alpha + bg_rgb[0]*(1-alpha))
    g = round(fg_rgb[1]*alpha + bg_rgb[1]*(1-alpha))
    b = round(fg_rgb[2]*alpha + bg_rgb[2]*(1-alpha))
    return (r,g,b)

def mix_srgb(rgb1, p1, rgb2, p2):
    r = round(rgb1[0]*p1 + rgb2[0]*p2)
    g = round(rgb1[1]*p1 + rgb2[1]*p2)
    b = round(rgb1[2]*p1 + rgb2[2]*p2)
    return (r,g,b)

checks = []
# Light theme
bg = hex_to_rgb('#ffffff')
fg = hex_to_rgb('#111827')
checks.append(("Light: body text", fg, bg))
checks.append(("Light: link on bg", hex_to_rgb('#2563eb'), bg))
checks.append(("Light: muted on bg", hex_to_rgb('#6b7280'), bg))
checks.append(("Light: code fg on code bg", hex_to_rgb('#111827'), hex_to_rgb('#f3f4f6')))
checks.append(("Light: pre fg on pre bg", hex_to_rgb('#e5e7eb'), hex_to_rgb('#0b1220')))

# Dark theme
bg_d = hex_to_rgb('#0b1220')
checks.append(("Dark: body text", hex_to_rgb('#e5e7eb'), bg_d))
checks.append(("Dark: muted on bg", hex_to_rgb('#94a3b8'), bg_d))
checks.append(("Dark: link on bg", hex_to_rgb('#60a5fa'), bg_d))
# Tab background composite (tabstrip #0a111d with rgba(255,255,255,0.04))
strip = hex_to_rgb('#0a111d')
tab_bg = blend_over((255,255,255), 0.04, strip)
checks.append(("Dark: tab fg on tab bg", hex_to_rgb('#e5e7eb'), tab_bg))
checks.append(("Dark: tab inactive fg on tab bg", hex_to_rgb('#cbd5e1'), tab_bg))
# Dialogs
surface = hex_to_rgb('#0c1526')
checks.append(("Dark: dialog body fg on surface", hex_to_rgb('#e5e7eb'), surface))
checks.append(("Dark: dialog body muted on surface", hex_to_rgb('#94a3b8'), surface))
# Titlebar mix: 14% accent + 86% surface
accent = hex_to_rgb('#60a5fa')
titlebar = mix_srgb(accent, 0.14, surface, 0.86)
checks.append(("Dark: dialog title fg on titlebar", hex_to_rgb('#e5e7eb'), titlebar))

threshold = 4.5
min_name = None
min_ratio = 99

for name, fgc, bgc in checks:
    c = contrast(fgc,bgc)
    print(f"{name}: fg {fgc} on bg {bgc} => contrast {c:.2f}:1")
    if c < min_ratio:
        min_ratio = c
        min_name = name

print(f"\nWorst-case contrast: {min_ratio:.2f}:1 ({min_name})")
if min_ratio < threshold:
    import sys
    print(f"FAIL: Minimum contrast {min_ratio:.2f}:1 is below AA threshold {threshold}:1", file=sys.stderr)
    sys.exit(1)
else:
    print(f"PASS: All checked pairs meet or exceed {threshold}:1")
