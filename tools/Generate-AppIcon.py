"""Export the approved YanJi source into Windows multi-resolution branding assets.
Requires Pillow (build-time only); does not alter the source artwork.
"""
from pathlib import Path
import hashlib
import json
from PIL import Image, ImageOps

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "design/branding/yanji-icon-v1.png"
TARGET = ROOT / "src/YanJi/Assets/Icons"
SIZES = (16, 24, 32, 48, 64, 128, 256)

def main():
    TARGET.mkdir(parents=True, exist_ok=True)
    with Image.open(SOURCE) as original:
        image = ImageOps.exif_transpose(original).convert("RGBA")
        if image.width != image.height:
            raise ValueError("Approved icon must be square; do not silently crop artwork.")
        if min(image.size) < max(SIZES):
            raise ValueError("Source must be at least256 pixels; do not upscale.")
        image.resize((256, 256), Image.Resampling.LANCZOS).save(TARGET / "YanJi.png")
        image.save(TARGET / "YanJi.ico", format="ICO", sizes=[(n, n) for n in SIZES])
    with Image.open(TARGET / "YanJi.ico") as icon:
        assert icon.ico.sizes() == {(n, n) for n in SIZES}
        for n in SIZES:
            assert icon.ico.getimage((n, n)).size == (n, n)
    for file in [SOURCE, TARGET / "YanJi.ico", TARGET / "YanJi.png"]:
        print(json.dumps({"Path": str(file.relative_to(ROOT)), "Bytes": file.stat().st_size,
                          "Sha256": hashlib.sha256(file.read_bytes()).hexdigest().upper()}))

if __name__ == "__main__":
    main()
