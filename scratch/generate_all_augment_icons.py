import os
import math
import uuid
from PIL import Image, ImageDraw, ImageFilter

OUTPUT_DIRS = [
    r"Assets/Resources/UI/Augments",
    r"Assets/Sprites/UI/Augments"
]

META_TEMPLATE = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalStrength: 1
  isReadable: 1
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMasterTextureLimit: 0
  globalAdditionalMipLevels: 0
  ambientOcclusion: 0
  colorType: 0
  aniso: 1
  format: -1
  maxTextureSize: 512
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 100
  spriteBorder: {x: 0, y: 0, z: 0, w: 0}
  spriteGenerateFallbackPhysicsShape: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbook: 0
  platformSettings: []
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
"""

def create_base_canvas(bg_color=(20, 24, 32, 230), border_color=(212, 175, 55, 255)):
    # 128x128 RGBA
    img = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Viền ngoài tròn/bo góc phong cách phù hiệu Dark Fantasy
    draw.ellipse([6, 6, 121, 121], fill=bg_color, outline=border_color, width=4)
    # Vòng chỉ khảm hoa văn nhỏ bên trong
    draw.ellipse([14, 14, 113, 113], outline=(border_color[0]//2, border_color[1]//2, border_color[2]//2, 180), width=2)
    return img, draw

def save_icon(name, img):
    for d in OUTPUT_DIRS:
        os.makedirs(d, exist_ok=True)
        png_path = os.path.join(d, f"{name}.png")
        meta_path = os.path.join(d, f"{name}.png.meta")

        img.save(png_path, "PNG")

        if not os.path.exists(meta_path):
            g = uuid.uuid4().hex
            with open(meta_path, "w", encoding="utf-8") as f:
                f.write(META_TEMPLATE.replace("{guid}", g))

def draw_sword(color=(255, 60, 60), cross=False):
    img, draw = create_base_canvas(bg_color=(28, 12, 12, 240), border_color=color)
    # Lưỡi kiếm chém sắc bén
    # Thân kiếm chính
    draw.polygon([(64, 18), (72, 28), (68, 82), (60, 82), (56, 28)], fill=(240, 245, 255, 255), outline=color)
    draw.polygon([(64, 20), (66, 28), (64, 80), (62, 80)], fill=(255, 255, 255, 255))
    # Chuôi và đốc kiếm
    draw.rectangle([48, 82, 80, 88], fill=color)
    draw.rectangle([61, 88, 67, 104], fill=(139, 90, 43, 255))
    draw.ellipse([58, 102, 70, 110], fill=color)
    if cross:
        # Kiếm phụ bắt chéo
        draw.polygon([(26, 32), (34, 26), (88, 80), (82, 88)], fill=(220, 230, 245, 255), outline=color)
    return img

def draw_dagger(color=(255, 80, 50)):
    img, draw = create_base_canvas(bg_color=(30, 16, 12, 240), border_color=color)
    # Dao găm răng cưa xé rách
    draw.polygon([(64, 22), (76, 36), (70, 48), (78, 60), (68, 76), (60, 76), (52, 40)], fill=(245, 245, 255, 255), outline=color)
    draw.rectangle([50, 76, 78, 82], fill=color)
    draw.rectangle([61, 82, 67, 102], fill=(120, 80, 30, 255))
    return img

def draw_hammer(color=(245, 158, 11)):
    img, draw = create_base_canvas(bg_color=(28, 20, 10, 240), border_color=color)
    # Búa tạ đập nát
    draw.rectangle([38, 30, 90, 56], fill=(180, 190, 205, 255), outline=color, width=2)
    draw.rectangle([44, 34, 84, 52], fill=(220, 230, 245, 255))
    draw.rectangle([60, 56, 68, 104], fill=(140, 90, 40, 255))
    draw.ellipse([58, 100, 70, 108], fill=color)
    return img

def draw_skull(color=(255, 50, 80)):
    img, draw = create_base_canvas(bg_color=(24, 10, 14, 240), border_color=color)
    # Đầu lâu tử thần
    draw.ellipse([38, 28, 90, 76], fill=(240, 240, 245, 255), outline=color, width=2)
    draw.rectangle([48, 70, 80, 92], fill=(240, 240, 245, 255), outline=color, width=2)
    # Hốc mắt
    draw.ellipse([46, 44, 58, 62], fill=(20, 10, 15, 255))
    draw.ellipse([70, 44, 82, 62], fill=(20, 10, 15, 255))
    # Mũi tam giác
    draw.polygon([(64, 60), (60, 68), (68, 68)], fill=(20, 10, 15, 255))
    # Răng
    for x in [54, 62, 70]:
        draw.line([(x, 76), (x, 90)], fill=(40, 20, 25, 255), width=2)
    return img

def draw_heart(color=(255, 75, 105), is_vitality=False):
    img, draw = create_base_canvas(bg_color=(12, 28, 20, 240), border_color=color)
    # Trái tim tỏa sáng
    draw.polygon([(64, 98), (30, 62), (28, 42), (40, 30), (64, 44), (88, 30), (100, 42), (98, 62)], fill=(235, 45, 75, 255), outline=color)
    draw.ellipse([38, 38, 52, 50], fill=(255, 160, 180, 255))
    if is_vitality:
        # Cây mầm xanh hồi sinh vươn lên từ tim
        draw.line([(64, 44), (64, 22)], fill=(46, 224, 115, 255), width=4)
        draw.polygon([(64, 22), (54, 16), (62, 28)], fill=(85, 239, 196, 255))
        draw.polygon([(64, 26), (74, 20), (66, 32)], fill=(85, 239, 196, 255))
    return img

def draw_wings(color=(51, 204, 255)):
    img, draw = create_base_canvas(bg_color=(12, 22, 36, 240), border_color=color)
    # Đôi cánh lông vũ lướt gió
    draw.polygon([(62, 68), (22, 38), (32, 24), (64, 48)], fill=(220, 245, 255, 255), outline=color)
    draw.polygon([(60, 72), (20, 54), (28, 44), (62, 58)], fill=(180, 225, 255, 255), outline=color)
    draw.polygon([(58, 76), (26, 70), (32, 60), (60, 68)], fill=(140, 205, 255, 255), outline=color)
    # Cánh phải
    draw.polygon([(66, 68), (106, 38), (96, 24), (64, 48)], fill=(220, 245, 255, 255), outline=color)
    draw.polygon([(68, 72), (108, 54), (100, 44), (66, 58)], fill=(180, 225, 255, 255), outline=color)
    draw.polygon([(70, 76), (102, 70), (96, 60), (68, 68)], fill=(140, 205, 255, 255), outline=color)
    # Ngọc tâm ở giữa
    draw.ellipse([58, 58, 70, 70], fill=(255, 225, 100, 255), outline=color)
    return img

def draw_shield(color=(59, 166, 255), emblem="cross"):
    img, draw = create_base_canvas(bg_color=(14, 22, 34, 240), border_color=color)
    # Khiên giáp chiến binh
    points = [(64, 20), (98, 30), (94, 76), (64, 108), (34, 76), (30, 30)]
    draw.polygon(points, fill=(40, 65, 100, 255), outline=color)
    draw.polygon([(64, 26), (92, 35), (88, 72), (64, 100), (40, 72), (36, 35)], fill=(25, 45, 75, 255))
    if emblem == "cross":
        draw.rectangle([60, 36, 68, 86], fill=(255, 215, 0, 255))
        draw.rectangle([46, 50, 82, 58], fill=(255, 215, 0, 255))
    elif emblem == "rune":
        draw.polygon([(64, 42), (76, 60), (64, 78), (52, 60)], fill=(100, 220, 255, 255))
    return img

def draw_hourglass(color=(250, 150, 46)):
    img, draw = create_base_canvas(bg_color=(28, 18, 10, 240), border_color=color)
    draw.polygon([(36, 26), (92, 26), (68, 64), (92, 102), (36, 102), (60, 64)], fill=(30, 25, 35, 255), outline=color)
    draw.rectangle([30, 20, 98, 28], fill=(212, 175, 55, 255), outline=(120, 90, 20, 255))
    draw.rectangle([30, 100, 98, 108], fill=(212, 175, 55, 255), outline=(120, 90, 20, 255))
    draw.polygon([(48, 30), (80, 30), (64, 60)], fill=(255, 200, 80, 255))
    draw.line([(64, 60), (64, 86)], fill=(255, 220, 100, 255), width=3)
    draw.polygon([(46, 98), (82, 98), (64, 84)], fill=(255, 200, 80, 255))
    return img

def draw_eye(color=(255, 210, 30)):
    img, draw = create_base_canvas(bg_color=(28, 24, 10, 240), border_color=color)
    draw.polygon([(24, 64), (64, 34), (104, 64), (64, 94)], fill=(245, 240, 220, 255), outline=color)
    draw.ellipse([46, 46, 82, 82], fill=(220, 50, 40, 255), outline=color)
    draw.ellipse([56, 52, 72, 76], fill=(20, 10, 10, 255))
    draw.ellipse([64, 54, 70, 60], fill=(255, 255, 255, 255))
    return img

def draw_energy(color=(0, 210, 211)):
    img, draw = create_base_canvas(bg_color=(10, 24, 30, 240), border_color=color)
    draw.ellipse([40, 40, 88, 88], fill=(0, 180, 216, 255), outline=(255, 255, 255, 255), width=2)
    draw.ellipse([48, 48, 80, 80], fill=(72, 202, 228, 255))
    draw.ellipse([56, 56, 72, 72], fill=(240, 255, 255, 255))
    draw.line([(64, 20), (64, 38)], fill=color, width=4)
    draw.line([(64, 90), (64, 108)], fill=color, width=4)
    draw.line([(20, 64), (38, 64)], fill=color, width=4)
    draw.line([(90, 64), (108, 64)], fill=color, width=4)
    return img

def draw_potion(color=(235, 77, 75)):
    img, draw = create_base_canvas(bg_color=(28, 12, 18, 240), border_color=color)
    draw.rectangle([54, 28, 74, 46], fill=(200, 230, 255, 200), outline=(100, 150, 200, 255))
    draw.rectangle([50, 24, 78, 30], fill=(160, 110, 60, 255))
    draw.ellipse([34, 44, 94, 104], fill=(25, 15, 30, 220), outline=(150, 200, 255, 255), width=3)
    draw.chord([38, 48, 90, 100], start=0, end=180, fill=color)
    draw.ellipse([48, 70, 56, 78], fill=(255, 180, 180, 255))
    draw.ellipse([68, 62, 74, 68], fill=(255, 200, 200, 255))
    return img

def draw_poison_flask(color=(31, 230, 107)):
    img, draw = create_base_canvas(bg_color=(10, 28, 16, 240), border_color=color)
    draw.polygon([(64, 30), (74, 52), (100, 98), (28, 98), (54, 52)], fill=(20, 35, 25, 230), outline=(100, 200, 130, 255), width=2)
    draw.rectangle([56, 20, 72, 30], fill=(140, 90, 40, 255))
    draw.polygon([(40, 94), (88, 94), (76, 70), (52, 70)], fill=color)
    draw.ellipse([58, 76, 66, 84], fill=(220, 255, 200, 255))
    return img

def draw_lightning(color=(184, 84, 255)):
    img, draw = create_base_canvas(bg_color=(20, 12, 34, 240), border_color=color)
    pts = [(70, 18), (42, 60), (62, 60), (44, 110), (86, 52), (66, 52)]
    draw.polygon(pts, fill=(255, 255, 255, 255), outline=color)
    draw.polygon([(68, 26), (48, 58), (64, 58), (52, 98), (78, 54), (64, 54)], fill=color)
    return img

def draw_scythe(color=(70, 190, 255)):
    img, draw = create_base_canvas(bg_color=(12, 18, 32, 240), border_color=color)
    draw.line([(34, 110), (84, 24)], fill=(120, 80, 35, 255), width=6)
    draw.arc([16, 12, 108, 96], start=210, end=350, fill=color, width=12)
    draw.arc([18, 14, 106, 94], start=215, end=345, fill=(240, 250, 255, 255), width=6)
    return img

def draw_crown(color=(255, 215, 0)):
    img, draw = create_base_canvas(bg_color=(32, 24, 8, 240), border_color=color)
    draw.polygon([(26, 86), (30, 44), (48, 62), (64, 32), (80, 62), (98, 44), (102, 86)], fill=(255, 205, 30, 255), outline=color)
    draw.rectangle([24, 84, 104, 94], fill=(200, 150, 20, 255), outline=color)
    draw.ellipse([26, 38, 34, 46], fill=(255, 40, 60, 255))
    draw.ellipse([60, 26, 68, 34], fill=(50, 150, 255, 255))
    draw.ellipse([94, 38, 102, 46], fill=(255, 40, 60, 255))
    return img

def draw_meteor(color=(240, 110, 230)):
    img, draw = create_base_canvas(bg_color=(28, 10, 30, 240), border_color=color)
    draw.ellipse([54, 54, 104, 104], fill=(80, 30, 70, 255), outline=color, width=3)
    draw.ellipse([60, 60, 98, 98], fill=(220, 70, 150, 255))
    draw.ellipse([70, 70, 90, 90], fill=(255, 200, 240, 255))
    draw.polygon([(70, 60), (20, 20), (60, 70)], fill=(255, 140, 50, 200))
    draw.polygon([(74, 56), (30, 16), (56, 74)], fill=color)
    return img

def draw_flame(color=(255, 90, 30)):
    img, draw = create_base_canvas(bg_color=(32, 12, 8, 240), border_color=color)
    draw.polygon([(64, 18), (86, 50), (96, 76), (82, 104), (46, 104), (32, 76), (42, 50)], fill=(230, 40, 20, 255), outline=color)
    draw.polygon([(64, 38), (80, 62), (86, 80), (74, 98), (54, 98), (42, 80), (48, 62)], fill=(255, 140, 20, 255))
    draw.polygon([(64, 58), (74, 76), (72, 92), (56, 92), (54, 76)], fill=(255, 235, 100, 255))
    return img

def draw_cyclone(color=(60, 220, 240)):
    img, draw = create_base_canvas(bg_color=(10, 26, 34, 240), border_color=color)
    for y, r in [(32, 38), (52, 32), (72, 24), (90, 16)]:
        draw.ellipse([64 - r, y - 8, 64 + r, y + 8], fill=None, outline=color, width=4)
        draw.ellipse([64 - r + 3, y - 6, 64 + r - 3, y + 6], fill=None, outline=(220, 255, 255, 255), width=2)
    return img

def draw_demon_pact(color=(220, 25, 75)):
    # Biểu tượng Khế Ước Quỷ Dữ: Đầu lâu quỷ sừng nhọn uy dũng
    img, draw = create_base_canvas(bg_color=(32, 8, 18, 245), border_color=color)
    # Sừng quỷ đỏ hai bên
    draw.polygon([(44, 48), (20, 22), (38, 38)], fill=color)
    draw.polygon([(84, 48), (108, 22), (90, 38)], fill=color)
    # Mặt quỷ
    draw.polygon([(40, 48), (88, 48), (78, 92), (64, 106), (50, 92)], fill=(120, 15, 35, 255), outline=color, width=2)
    # Hốc mắt rực đỏ
    draw.polygon([(48, 60), (58, 66), (50, 72)], fill=(255, 230, 80, 255))
    draw.polygon([(80, 60), (70, 66), (78, 72)], fill=(255, 230, 80, 255))
    # Dấu ấn nguyền rủa trán
    draw.polygon([(64, 52), (68, 60), (60, 60)], fill=(255, 80, 120, 255))
    return img

def draw_godforged(color=(255, 190, 40)):
    img, draw = create_base_canvas(bg_color=(32, 22, 10, 240), border_color=color)
    draw.polygon([(64, 16), (72, 26), (68, 78), (60, 78), (56, 26)], fill=(255, 255, 255, 255), outline=color)
    draw.rectangle([48, 78, 80, 84], fill=color)
    draw.rectangle([61, 84, 67, 102], fill=(160, 110, 40, 255))
    for a in range(0, 360, 45):
        rad = math.radians(a)
        x1 = 64 + int(38 * math.cos(rad))
        y1 = 64 + int(38 * math.sin(rad))
        x2 = 64 + int(52 * math.cos(rad))
        y2 = 64 + int(52 * math.sin(rad))
        draw.line([(x1, y1), (x2, y2)], fill=color, width=3)
    return img

def draw_bounty_shard(color=(255, 210, 40)):
    img, draw = create_base_canvas(bg_color=(28, 22, 10, 240), border_color=color)
    draw.polygon([(64, 20), (96, 52), (80, 104), (48, 104), (32, 52)], fill=(255, 190, 20, 255), outline=color)
    draw.polygon([(64, 26), (88, 52), (64, 76), (40, 52)], fill=(255, 245, 180, 255))
    draw.polygon([(64, 76), (76, 98), (52, 98)], fill=(210, 140, 10, 255))
    return img

# ─── 8 LÕI NÂNG CẤP MỚI & CURSED DOUBLE-EDGED ───
def draw_guillotine(color=(255, 55, 75)):
    # Trảm Sát Trảm Kích (PercentCurrentHpExecute): Máy chém / Đại đao trảm thủ dính máu
    img, draw = create_base_canvas(bg_color=(32, 10, 16, 245), border_color=color)
    # Lưỡi máy chém chéo hình thang sắc lạnh
    draw.polygon([(30, 28), (98, 54), (98, 88), (30, 62)], fill=(225, 235, 245, 255), outline=color, width=2)
    # Lưỡi vát đẫm máu đỏ thẫm
    draw.polygon([(30, 62), (98, 88), (98, 96), (30, 70)], fill=(220, 20, 40, 255))
    # Khung sắt treo lưỡi và xích sắt
    draw.rectangle([24, 20, 104, 28], fill=(80, 85, 95, 255), outline=color)
    draw.line([(44, 28), (44, 42)], fill=(180, 185, 195, 255), width=3)
    draw.line([(84, 28), (84, 50)], fill=(180, 185, 195, 255), width=3)
    # Giọt máu rơi
    draw.ellipse([60, 96, 68, 106], fill=(255, 40, 60, 255))
    return img

def draw_golden_sanctuary(color=(255, 215, 0)):
    # Kim Thân Bất Hoại (GoldenSanctuary): Hào quang thánh thể kim cương miễn khống chế
    img, draw = create_base_canvas(bg_color=(34, 28, 8, 245), border_color=color)
    # Các tia hào quang mặt trời 12 hướng
    for a in range(0, 360, 30):
        rad = math.radians(a)
        x1 = 64 + int(36 * math.cos(rad))
        y1 = 64 + int(36 * math.sin(rad))
        x2 = 64 + int(52 * math.cos(rad))
        y2 = 64 + int(52 * math.sin(rad))
        draw.line([(x1, y1), (x2, y2)], fill=color, width=3)
    # Vòng khiên thánh vàng
    draw.ellipse([34, 34, 94, 94], fill=(80, 65, 15, 255), outline=(255, 240, 150, 255), width=3)
    # Kim cương thánh bảo
    draw.polygon([(64, 42), (86, 64), (64, 86), (42, 64)], fill=(255, 245, 180, 255), outline=color, width=2)
    draw.polygon([(64, 48), (80, 64), (64, 80), (48, 64)], fill=(255, 255, 255, 255))
    return img

def draw_cursed_might(color=(186, 85, 211)):
    # Khế Ước Cuồng Nộ (CursedMight): +100 ATK, Quái +50% HP. Lưỡi kiếm hắc ám rực tím
    img, draw = create_base_canvas(bg_color=(28, 10, 34, 245), border_color=color)
    # Lưỡi kiếm tà ác
    draw.polygon([(64, 18), (76, 32), (70, 78), (58, 78), (52, 32)], fill=(220, 180, 255, 255), outline=color, width=2)
    # Khí hắc ám cuộn quanh kiếm
    draw.arc([40, 28, 88, 76], start=45, end=225, fill=(147, 51, 234, 255), width=4)
    # Chuôi kiếm mắt quỷ tím
    draw.rectangle([48, 78, 80, 84], fill=color)
    draw.rectangle([61, 84, 67, 102], fill=(90, 30, 120, 255))
    draw.ellipse([58, 54, 70, 66], fill=(255, 50, 100, 255))
    return img

def draw_hex_ironclad(color=(112, 145, 175)):
    # Minh Ước Hắc Thiết (IroncladPact): +50 Giáp, Quái +5% DR. Khiên thép đen chông gai
    img, draw = create_base_canvas(bg_color=(18, 22, 28, 245), border_color=color)
    # Khiên thép đồ sộ
    pts = [(64, 20), (100, 32), (92, 80), (64, 108), (36, 80), (28, 32)]
    draw.polygon(pts, fill=(45, 55, 68, 255), outline=color, width=3)
    # Lõi kim loại gia cố
    draw.polygon([(64, 30), (90, 40), (84, 75), (64, 98), (44, 75), (38, 40)], fill=(25, 32, 42, 255), outline=(70, 85, 105, 255))
    # Gai nhọn thép ở tâm
    draw.polygon([(64, 46), (76, 64), (64, 82), (52, 64)], fill=(180, 205, 230, 255), outline=(255, 255, 255, 255))
    # Đinh tán thép 4 góc
    for pt in [(48, 40), (80, 40), (46, 70), (82, 70)]:
        draw.ellipse([pt[0]-3, pt[1]-3, pt[0]+3, pt[1]+3], fill=(220, 230, 245, 255))
    return img

def draw_chrono_mire(color=(64, 224, 208)):
    # Đầm Lầy Thời Không (ChronoStasis): Đồng hồ cát băng hà thời gian đóng băng
    img, draw = create_base_canvas(bg_color=(12, 28, 32, 245), border_color=color)
    # Khung đồng hồ pha lê băng
    draw.polygon([(36, 26), (92, 26), (68, 64), (92, 102), (36, 102), (60, 64)], fill=(20, 40, 50, 255), outline=color, width=2)
    draw.rectangle([30, 20, 98, 28], fill=color, outline=(255, 255, 255, 255))
    draw.rectangle([30, 100, 98, 108], fill=color, outline=(255, 255, 255, 255))
    # Xoáy băng thời gian đông cứng
    draw.ellipse([50, 36, 78, 64], outline=(150, 255, 255, 255), width=2)
    draw.line([(64, 42), (64, 52)], fill=(255, 255, 255, 255), width=3)
    draw.line([(64, 52), (72, 52)], fill=(255, 255, 255, 255), width=2)
    # Băng tuyết đóng cứng phía dưới
    draw.polygon([(46, 98), (82, 98), (74, 80), (54, 80)], fill=(120, 240, 255, 255))
    return img

def draw_blood_tithe(color=(220, 20, 60)):
    # Ma Kiếm Huyết Tế (BloodTitheBlade): +40 ATK, +40% Vamp, mất 1% HP/s. Thanh Ma Kiếm huyết sắc
    img, draw = create_base_canvas(bg_color=(32, 10, 16, 245), border_color=color)
    # Lưỡi Katana cong đẫm máu
    draw.polygon([(42, 20), (48, 24), (74, 76), (68, 80)], fill=(255, 220, 230, 255), outline=color, width=2)
    draw.polygon([(44, 22), (48, 24), (72, 74)], fill=(255, 40, 80, 255))
    # Chuôi kiếm quấn dây đen
    draw.line([(70, 78), (86, 106)], fill=(30, 20, 25, 255), width=6)
    draw.ellipse([64, 74, 76, 84], fill=color)
    # Giọt máu tế lễ đang nhỏ xuống
    draw.ellipse([46, 88, 56, 100], fill=(255, 20, 50, 255))
    draw.polygon([(51, 82), (46, 92), (56, 92)], fill=(255, 20, 50, 255))
    return img

def draw_glass_cannon(color=(0, 245, 255)):
    # Bản Thể Thủy Tinh (GlassCannonTitan): +75% Crit, +80% Crit DMG, nhận thêm 30% dmg
    img, draw = create_base_canvas(bg_color=(10, 26, 32, 245), border_color=color)
    # Khối tinh thể kim cương pha lê sắc nhọn
    draw.polygon([(64, 18), (96, 52), (64, 108), (32, 52)], fill=(80, 220, 255, 230), outline=(255, 255, 255, 255), width=2)
    # Các mặt vát tinh thể phản chiếu
    draw.polygon([(64, 24), (90, 52), (64, 68), (38, 52)], fill=(200, 250, 255, 255))
    draw.polygon([(64, 68), (90, 52), (64, 102)], fill=(40, 180, 230, 255))
    # Vết rạn nứt nổ đỏ (điềm báo thủy tinh dễ vỡ khi bị đập)
    draw.line([(52, 42), (64, 60)], fill=(255, 60, 90, 255), width=3)
    draw.line([(64, 60), (74, 78)], fill=(255, 60, 90, 255), width=2)
    return img

def draw_phantom_rush(color=(168, 85, 247)):
    # Bóng Ma Cuồng Nộ (PhantomRush): +50% CDR, -25% Max HP. Đôi cánh u linh bóng ma
    img, draw = create_base_canvas(bg_color=(24, 12, 34, 245), border_color=color)
    # Cánh bóng ma tà mị bên trái
    draw.polygon([(60, 70), (18, 32), (32, 22), (62, 50)], fill=(210, 170, 255, 255), outline=color)
    draw.polygon([(58, 74), (16, 50), (28, 42), (60, 60)], fill=(160, 100, 240, 255), outline=color)
    # Cánh bóng ma bên phải
    draw.polygon([(68, 70), (110, 32), (96, 22), (66, 50)], fill=(210, 170, 255, 255), outline=color)
    draw.polygon([(70, 74), (112, 50), (100, 42), (68, 60)], fill=(160, 100, 240, 255), outline=color)
    # Quả cầu linh hồn ma quái ở tâm
    draw.ellipse([54, 52, 74, 72], fill=(255, 100, 200, 255), outline=(255, 255, 255, 255), width=2)
    # Vệt khói ma trôi phía dưới
    draw.line([(64, 72), (64, 104)], fill=(147, 51, 234, 200), width=4)
    return img

def generate_all():
    icons = {
        # 1. Vũ khí & Tấn công
        "sword_slash": draw_sword((255, 60, 60)),
        "lacerating_edge": draw_dagger((255, 80, 50)),
        "heavy_impact": draw_hammer((245, 158, 11)),
        "relentless_assault": draw_sword((255, 100, 80), cross=True),
        "giant_slayer": draw_sword((255, 45, 45)),
        "battle_frenzy": draw_flame((255, 70, 20)),
        "flame_fire": draw_flame((255, 90, 30)),
        "skull_death": draw_skull((255, 50, 80)),

        # 2. Sinh mệnh & Hồi phục
        "heart_vitality": draw_heart((255, 75, 105), is_vitality=False),
        "abundant_vitality": draw_heart((46, 224, 115), is_vitality=True),
        "second_wind": draw_wings((100, 240, 180)),
        "potion_regen": draw_potion((235, 77, 75)),
        "scythe_harvest": draw_scythe((70, 190, 255)),

        # 3. Tốc độ & Thao tác
        "wings_fleet": draw_wings((51, 204, 255)),
        "swift_shadow": draw_wings((160, 120, 255)),
        "fleet_wind": draw_wings((100, 220, 240)),
        "cyclone_storm": draw_cyclone((60, 220, 240)),

        # 4. Chí mạng & Tập trung
        "eye_focus": draw_eye((255, 210, 30)),
        "crit_damage": draw_eye((255, 60, 60)),
        "shadow_ambush": draw_dagger((140, 60, 255)),
        "chain_reaction": draw_lightning((255, 160, 20)),

        # 5. Phòng ngự & Giáp hộ thân
        "shield_aegis": draw_shield((59, 166, 255), "cross"),
        "iron_bastion": draw_shield((180, 195, 215), "cross"),
        "ancient_shield": draw_shield((255, 190, 50), "rune"),
        "arcane_shield": draw_shield((160, 90, 255), "rune"),
        "unwavering_will": draw_shield((245, 158, 11), "cross"),
        "prismatic_titan": draw_shield((255, 215, 0), "rune"),
        "divine_shield": draw_shield((255, 230, 100), "cross"),

        # 6. Hút máu & Ma thuật huyết sắc
        "blood_drop": draw_potion((220, 30, 50)),
        "vampire_blood": draw_potion((200, 20, 40)),
        "blood_god_tribute": draw_demon_pact((230, 20, 40)),
        "life_transfusion": draw_potion((240, 60, 80)),
        "deaths_touch": draw_skull((220, 40, 70)),
        "poison_flask": draw_poison_flask((31, 230, 107)),

        # 7. Năng lượng & Tuyệt kỹ & Hồi chiêu
        "hourglass_cdr": draw_hourglass((250, 150, 46)),
        "energy_pulse": draw_energy((0, 210, 211)),
        "surging_pulse": draw_energy((80, 225, 255)),
        "combo_mastery": draw_sword((255, 140, 30), cross=True),
        "ultimate_charge": draw_godforged((255, 215, 0)),

        # 8. Sấm sét & Ma thuật huyền bí
        "spell_vamp": draw_potion((180, 70, 240)),
        "lightning_bolt": draw_lightning((184, 84, 255)),
        "arcane_overdrive": draw_lightning((210, 110, 255)),
        "meteor_cascade": draw_meteor((240, 110, 230)),
        "static_overload": draw_lightning((90, 200, 255)),
        "echoing_void": draw_meteor((140, 80, 220)),

        # 9. Đỉnh cao thần khí & Hoàng quyền
        "demon_pact": draw_demon_pact((200, 30, 80)),
        "crown_sovereign": draw_crown((255, 215, 0)),
        "godforged_arsenal": draw_godforged((255, 190, 40)),
        "ascended_divinity": draw_crown((255, 230, 100)),
        "bounty_hunter": draw_bounty_shard((255, 210, 40)),

        # 10. Lõi nguyên tố
        "flame_adaptation": draw_flame((255, 140, 30)),
        "magma_eruption": draw_flame((255, 80, 20)),
        "acid_membrane": draw_poison_flask((40, 240, 120)),
        "virulent_bloom": draw_poison_flask((60, 255, 90)),
        "glacial_soul": draw_wings((150, 230, 255)),
        "absolute_zero_nova": draw_shield((120, 220, 255), "rune"),
        "void_transcendence": draw_demon_pact((170, 60, 240)),
        "shadow_mirroring": draw_scythe((160, 90, 255)),

        # 11. 8 LÕI NÂNG CẤP MỚI & CURSED DOUBLE-EDGED
        "guillotine_execute": draw_guillotine((255, 55, 75)),
        "golden_sanctuary": draw_golden_sanctuary((255, 215, 0)),
        "cursed_might": draw_cursed_might((186, 85, 211)),
        "hex_ironclad": draw_hex_ironclad((112, 145, 175)),
        "chrono_mire": draw_chrono_mire((64, 224, 208)),
        "blood_tithe": draw_blood_tithe((220, 20, 60)),
        "glass_cannon": draw_glass_cannon((0, 245, 255)),
        "phantom_rush": draw_phantom_rush((168, 85, 247))
    }

    for name, img in icons.items():
        save_icon(name, img)
        print(f"Generated: {name}.png")

if __name__ == "__main__":
    generate_all()
