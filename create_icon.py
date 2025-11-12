#!/usr/bin/env python3
"""
Creates a Windows ICO file from SVG.
Requires: pip install pillow cairosvg
"""

try:
    from PIL import Image
    import cairosvg
    import io
    import os
except ImportError:
    print("Error: Required packages not installed.")
    print("Please install with: pip install pillow cairosvg")
    print("\nAlternatively, use an online converter:")
    print("1. Go to https://convertio.co/svg-ico/")
    print("2. Upload GlobalAudio/icon.svg")
    print("3. Convert and download as app.ico")
    print("4. Save to GlobalAudio/app.ico")
    exit(1)

def create_ico():
    svg_path = "GlobalAudio/icon.svg"
    ico_path = "GlobalAudio/app.ico"

    if not os.path.exists(svg_path):
        print(f"Error: {svg_path} not found")
        return

    # Read SVG
    with open(svg_path, 'rb') as f:
        svg_data = f.read()

    # Create images at different sizes for ICO
    sizes = [16, 32, 48, 64, 128, 256]
    images = []

    for size in sizes:
        # Convert SVG to PNG at this size
        png_data = cairosvg.svg2png(bytestring=svg_data, output_width=size, output_height=size)
        img = Image.open(io.BytesIO(png_data))
        images.append(img)

    # Save as ICO
    images[0].save(ico_path, format='ICO', sizes=[(s, s) for s in sizes], append_images=images[1:])
    print(f"✓ Icon created successfully: {ico_path}")

if __name__ == "__main__":
    create_ico()
