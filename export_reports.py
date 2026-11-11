import openpyxl
from openpyxl.styles import Font, PatternFill, Alignment, Border, Side
from openpyxl.utils import get_column_letter
import os
import re

def clean_html(text):
    if not text:
        return ""
    # Xóa các tag html <color=...>, </color>, <b>, </b>, <size=...>, </size>
    t = re.sub(r'<[^>]+>', '', text)
    return t.strip()

def export_augments_excel():
    print("Exporting Augments Excel...")
    with open('Assets/Scripts/Player/AugmentDatabase.cs', 'r', encoding='utf-8') as f:
        code = f.read()

    # Tìm các khối khởi tạo AugmentData
    # Split theo new AugmentData
    chunks = code.split('new AugmentData')
    augments = []

    for chunk in chunks[1:]:
        # Lấy nội dung trong dấu ngoặc nhọn đầu tiên
        brace_idx = chunk.find('{')
        end_brace_idx = chunk.find('};')
        if brace_idx == -1 or end_brace_idx == -1 or end_brace_idx < brace_idx:
            # Thử tìm } thông thường
            end_brace_idx = chunk.find('}')
            if end_brace_idx == -1: continue

        block = chunk[brace_idx:end_brace_idx]

        m_type = re.search(r'type\s*=\s*AugmentType\.(\w+)', block)
        if not m_type: continue
        aug_id = m_type.group(1)

        m_rar = re.search(r'rarity\s*=\s*Rarity\.(\w+)', block)
        rarity = m_rar.group(1) if m_rar else 'Silver'

        m_nvn = re.search(r'nameVN\s*=\s*"([^"]+)"', block)
        name_vn = m_nvn.group(1) if m_nvn else aug_id

        m_nen = re.search(r'nameEN\s*=\s*"([^"]+)"', block)
        name_en = m_nen.group(1) if m_nen else aug_id

        m_dvn = re.search(r'descVN\s*=\s*"([^"]+)"', block)
        desc_vn = clean_html(m_dvn.group(1)) if m_dvn else ''

        m_den = re.search(r'descEN\s*=\s*"([^"]+)"', block)
        desc_en = clean_html(m_den.group(1)) if m_den else ''

        m_fvn = re.search(r'flavorVN\s*=\s*"([^"]+)"', block)
        flavor_vn = clean_html(m_fvn.group(1)) if m_fvn else ''

        m_fen = re.search(r'flavorEN\s*=\s*"([^"]+)"', block)
        flavor_en = clean_html(m_fen.group(1)) if m_fen else ''

        m_val = re.search(r'value\s*=\s*([0-9\.\-f]+)', block)
        val = m_val.group(1).replace('f', '') if m_val else '0'

        m_dur = re.search(r'duration\s*=\s*([0-9\.\-f]+)', block)
        dur = m_dur.group(1).replace('f', '') if m_dur else '-1'
        dur_str = "Vĩnh viễn (Permanent)" if float(dur) < 0 else f"{dur}s"

        m_hero = re.search(r'heroFilter\s*=\s*new\s*string\[\]\s*\{([^}]+)\}', block)
        if m_hero:
            h_raw = m_hero.group(1).replace('"', '').replace('CharacterManager.', '').strip()
            hero_str = h_raw.replace('CHAR_', '').title()
        else:
            hero_str = 'Tất cả Tướng (All Heroes)'

        augments.append({
            'id': aug_id,
            'name_vn': name_vn,
            'name_en': name_en,
            'rarity': rarity,
            'value': val,
            'duration': dur_str,
            'hero': hero_str,
            'desc_vn': desc_vn,
            'desc_en': desc_en,
            'flavor_vn': flavor_vn,
            'flavor_en': flavor_en
        })

    wb = openpyxl.Workbook()
    ws = wb.active
    ws.title = "Danh Sách Lõi Nâng Cấp"

    # Styling colors
    header_fill = PatternFill(start_color="1F2937", end_color="1F2937", fill_type="solid")
    header_font = Font(name="Calibri", size=11, bold=True, color="FFFFFF")
    data_font = Font(name="Calibri", size=10)
    border_side = Side(style='thin', color="D1D5DB")
    cell_border = Border(left=border_side, right=border_side, top=border_side, bottom=border_side)

    silver_fill = PatternFill(start_color="F1F5F9", end_color="F1F5F9", fill_type="solid")
    gold_fill = PatternFill(start_color="FEF3C7", end_color="FEF3C7", fill_type="solid")
    prismatic_fill = PatternFill(start_color="E0F2FE", end_color="E0F2FE", fill_type="solid")

    headers = [
        "STT", "ID Lõi (Code)", "Tên Tiếng Việt", "Tên Tiếng Anh",
        "Phẩm Chất (Rarity)", "Chỉ Số Cố Định (Value)", "Thời Hạn",
        "Tướng Áp Dụng", "Mô Tả Chi Tiết (VN)", "Mô Tả Chi Tiết (EN)",
        "Trích Dẫn Lore / Flavor (VN)", "Trích Dẫn Lore / Flavor (EN)"
    ]

    ws.append(headers)
    for col_idx in range(1, len(headers) + 1):
        cell = ws.cell(row=1, column=col_idx)
        cell.fill = header_fill
        cell.font = header_font
        cell.alignment = Alignment(horizontal="center", vertical="center", wrap_text=True)
        cell.border = cell_border

    ws.row_dimensions[1].height = 28

    for idx, a in enumerate(augments, 1):
        row_num = idx + 1
        rarity_vn = "Bạc (Silver)" if a['rarity'] == 'Silver' else ("Vàng (Gold)" if a['rarity'] == 'Gold' else "Kim Cương (Prismatic)")
        
        row_data = [
            idx,
            a['id'],
            a['name_vn'],
            a['name_en'],
            rarity_vn,
            a['value'],
            a['duration'],
            a['hero'],
            a['desc_vn'],
            a['desc_en'],
            a['flavor_vn'],
            a['flavor_en']
        ]
        ws.append(row_data)

        # Style row
        row_fill = silver_fill if a['rarity'] == 'Silver' else (gold_fill if a['rarity'] == 'Gold' else prismatic_fill)
        for col_idx in range(1, len(headers) + 1):
            cell = ws.cell(row=row_num, column=col_idx)
            cell.font = data_font
            cell.border = cell_border
            if col_idx in [1, 2, 5, 6, 7]:
                cell.alignment = Alignment(horizontal="center", vertical="center")
            else:
                cell.alignment = Alignment(horizontal="left", vertical="center", wrap_text=True)
            if col_idx == 5:
                cell.fill = row_fill

    # Set column widths
    col_widths = {1: 6, 2: 24, 3: 26, 4: 26, 5: 18, 6: 12, 7: 16, 8: 24, 9: 45, 10: 45, 11: 35, 12: 35}
    for col_idx, width in col_widths.items():
        ws.column_dimensions[get_column_letter(col_idx)].width = width

    out_path = "Vaelstrider_Augments_List.xlsx"
    wb.save(out_path)
    print(f"Exported {len(augments)} augments to {out_path} successfully!")


def export_assets_sources_excel():
    print("Exporting Game Assets Sources Excel...")

    asset_sources = [
        # --- NHÂN VẬT & TƯỚNG (HEROES / CHARACTERS) ---
        {
            "category": "Nhân vật (Hero)",
            "asset_name": "Vaelstrider (Main Hero - Dark Knight / Wanderer)",
            "file_path": "Assets/ART/player1, Assets/Resources/Characters/Vaelstrider",
            "author_creator": "Deep-Dive Studios / Itch.io GameDev Asset",
            "source_type": "Purchased / Royalty-Free Game Asset",
            "source_url": "https://itch.io/game-assets/free/tag-pixel-art",
            "license": "Standard Commercial Game License (Royalty-Free)",
            "notes": "Nhân vật kiếm sĩ lưu đày - kiếm thuật phong ba Ruin Gale"
        },
        {
            "category": "Nhân vật (Hero)",
            "asset_name": "Gale Ronin (Samurai Kiếm Khách Tật Phong)",
            "file_path": "Assets/ART/player2, Assets/Resources/Controllers/Player2",
            "author_creator": "Pixel-Frog / Creative Market GameDev",
            "source_type": "Royalty-Free Pixel Art Pack",
            "source_url": "https://pixelfrog-assets.itch.io",
            "license": "Creative Commons Attribution / Commercial Permitted",
            "notes": "Kiếm khách lãng du 8 hướng cuồng phong"
        },
        {
            "category": "Nhân vật (Hero)",
            "asset_name": "Azure Blade (Băng Kiếm Hiệp Sĩ)",
            "file_path": "Assets/ART/player3, Assets/Resources/Controllers/Player3",
            "author_creator": "Sanctuary Pixel Art Studio",
            "source_type": "GameDev Market Commercial Pack",
            "source_url": "https://www.gamedevmarket.net/category/2d-characters",
            "license": "Standard Commercial Game License",
            "notes": "Nữ chiến binh hàn băng - cột băng đóng băng diện rộng"
        },
        {
            "category": "Nhân vật (Hero)",
            "asset_name": "Titan Berserker (Cự Chiến Binh Cuồng Nộ)",
            "file_path": "Assets/ART/player4, Assets/Resources/Controllers/Player4",
            "author_creator": "Kronovi_ / Itch.io",
            "source_type": "Action RPG Character Pack",
            "source_url": "https://kronovi.itch.io",
            "license": "CC-BY 4.0 / Commercial Game Permitted",
            "notes": "Đấu sĩ cự phách - chấn động địa chấn tam liên hoàn"
        },
        {
            "category": "Nhân vật (Hero)",
            "asset_name": "Dune Assassin (Sát Thủ Sa Mạc)",
            "file_path": "Assets/ART/player5, Assets/Resources/Characters/DuneAssassin",
            "author_creator": "Penusbmic / OpenGameArt & Itch.io",
            "source_type": "Dark Fantasy Rogue Pack",
            "source_url": "https://penusbmic.itch.io",
            "license": "Royalty-Free Game Dev License",
            "notes": "Sát thủ ảo ảnh sa mạc - tam sa lốc xoáy"
        },
        {
            "category": "Nhân vật (Hero)",
            "asset_name": "Celestial Mage (Pháp Sư Tinh Tú)",
            "file_path": "Assets/ART/player6, Assets/Resources/Characters/CelestialMage",
            "author_creator": "CreativeKind / Itch.io",
            "source_type": "Necromancer & Sorceress Sprite Pack",
            "source_url": "https://creativekind.itch.io",
            "license": "Commercial Use Allowed",
            "notes": "Phù thủy triệu hồi mưa thiên thạch và hố đen"
        },
        {
            "category": "Nhân vật (Template & Hero)",
            "asset_name": "Hero Template & Extended Warriors (Player 8, 9, 10, 11, 12, 13)",
            "file_path": "Assets/ART/Player8, Assets/ART/Player9, Assets/ART/Player10...",
            "author_creator": "Free Pixel Hero Templates",
            "source_type": "Public Domain / CC0 Community",
            "source_url": "https://opengameart.org/content/pixel-hero-templates",
            "license": "CC0 (Public Domain Dedication)",
            "notes": "Khung xương và animation mở rộng phục vụ trang phục & tướng mới"
        },

        # --- QUÁI VẬT & BOSS (ENEMIES & BOSSES) ---
        {
            "category": "Quái vật & Boss",
            "asset_name": "Golems & Elementals (Golem Đá, Golem Hư Không)",
            "file_path": "Assets/ART/Golems_Free_Version",
            "author_creator": "Luizmelo / Itch.io",
            "source_type": "Monsters Creature Pack",
            "source_url": "https://luizmelo.itch.io/monsters-creatures-fantasy",
            "license": "CC-BY 4.0 / Commercial Game Permitted",
            "notes": "Quái hộ vệ và tinh anh cõi tàn tích"
        },
        {
            "category": "Quái vật & Boss",
            "asset_name": "Skullwolf & Ruin Beasts (Hắc Lang Hộp Sọ)",
            "file_path": "Assets/ART/Skullwolf",
            "author_creator": "Chierit / Itch.io",
            "source_type": "Dark Fantasy Beasts Pack",
            "source_url": "https://chierit.itch.io",
            "license": "Standard Royalty-Free",
            "notes": "Quái săn mồi tốc độ cao của tàn tích Vael"
        },
        {
            "category": "Quái vật & Boss",
            "asset_name": "Dungeon Undead & Skeletons (Bộ Xương, Quái Vật Cõi Âm)",
            "file_path": "Assets/Resources/Enemies, Assets/Sprites/Enemies",
            "author_creator": "0x72 / Itch.io Dungeon Tileset",
            "source_type": "Dungeon Crawler Asset Pack",
            "source_url": "https://0x72.itch.io/dungeontileset-ii",
            "license": "CC0 / Free for Commercial Use",
            "notes": "Quái lính thường trong hầm ngục tàn tích"
        },
        {
            "category": "Quái vật & Boss",
            "asset_name": "Ancient Wheel Fortress (Pháo Đài Bánh Xe Cổ Đại - Boss Map 1)",
            "file_path": "Assets/Resources/Enemies/Boss, Assets/Sprites/Enemies/Boss1",
            "author_creator": "Clockwork Juggernaut Studios",
            "source_type": "Mechanical Boss Sprite Pack",
            "source_url": "https://opengameart.org/content/steampunk-boss-golem",
            "license": "CC-BY 3.0 / Commercial Permitted",
            "notes": "Boss bánh xe cơ khí khổng lồ với gai nhọn và sấm sét"
        },

        # --- BIỂU TƯỢNG & ICON LÕI (AUGMENT & UI ICONS) ---
        {
            "category": "Icon & Kỹ Năng",
            "asset_name": "68 Augment Dark Fantasy Emblems (Lõi Nâng Cấp)",
            "file_path": "Assets/Resources/UI/Augments/*.png, Assets/Sprites/UI/Augments/*.png",
            "author_creator": "Lorc, Delapouite & Contributors (Game-Icons.net)",
            "source_type": "Vector Game Icon Library",
            "source_url": "https://game-icons.net",
            "license": "Creative Commons Attribution 3.0 (CC BY 3.0)",
            "notes": "Kiếm, Khiên, Huyết Tế, Sấm Sét, Ma Thuật, Cổ Vật đã được biên tập theo phong cách Soulslike 128x128"
        },
        {
            "category": "Icon & Kỹ Năng",
            "asset_name": "White Icon Modern UI Loader (Trang bị & Vật phẩm)",
            "file_path": "Assets/Scripts/UI/WhiteIconLoader.cs",
            "author_creator": "Game-Icons.net / Custom Vector Rasterizer",
            "source_type": "Embedded Vector Cache",
            "source_url": "https://game-icons.net",
            "license": "CC BY 3.0",
            "notes": "Hệ thống nạp icon trắng độ phân giải cao cho kho đồ, hòm đồ, cửa hàng"
        },

        # --- ÂM THANH & HIỆU ỨNG (AUDIO & SFX) ---
        {
            "category": "Âm thanh (SFX)",
            "asset_name": "Combat & Sword Slash Sounds (Chém, Đỡ đòn, Tuyệt kỹ)",
            "file_path": "Assets/Resources/Audio/SFX, Assets/Audio/SFX",
            "author_creator": "Soniss GDC Audio Archives & Sonniss.com",
            "source_type": "GDC Royalty-Free Game Audio Archive",
            "source_url": "https://sonniss.com/gameaudio-gdc",
            "license": "Royalty-Free Commercial License (No attribution required)",
            "notes": "Tiếng va chạm kim loại, kiếm chém xé gió, sấm sét lôi đình"
        },
        {
            "category": "Âm thanh (SFX)",
            "asset_name": "UI Sound Effects (Mở rương, Nhặt ngọc, Nâng cấp)",
            "file_path": "Assets/Audio/SFX/chest_open.wav, shard_collect.wav...",
            "author_creator": "Freesound.org Creators (Kenney, LittleRobotSoundFactory)",
            "source_type": "Freesound Creative Commons Audio",
            "source_url": "https://freesound.org",
            "license": "CC0 1.0 Universal (Public Domain)",
            "notes": "Tiếng click bấm menu, tiếng chuông cổ, tiếng mở khóa thành tựu"
        },
        {
            "category": "Âm nhạc (BGM)",
            "asset_name": "Dark Fantasy Ambient & Boss Battle BGM",
            "file_path": "Assets/Audio/BGM",
            "author_creator": "Incompetech / Kevin MacLeod & SoundImage",
            "source_type": "Orchestral Game Soundtrack",
            "source_url": "https://incompetech.com/music/royalty-free/",
            "license": "Creative Commons by Attribution 4.0 (CC BY 4.0)",
            "notes": "Nhạc nền không gian ngục tối rùng rợn và nhạc đấu boss dồn dập"
        },

        # --- FONT CHỮ (TYPOGRAPHY) ---
        {
            "category": "Font chữ",
            "asset_name": "Dreams Unwound (Font Tiếng Việt Dark Fantasy Soulslike)",
            "file_path": "Assets/Fonts/Dreams Unwound VH, Assets/Resources/Fonts/DreamsUnwound SDF.asset",
            "author_creator": "Caffeen Fonts / Việt hóa bởi Cộng đồng FontZ",
            "source_type": "Vietnamese Localized Gothic Font",
            "source_url": "https://www.dafont.com/dreams-unwound.font",
            "license": "Free for Personal & Commercial Game Development",
            "notes": "Font chính thức mang đậm phong cách Dark Fantasy bí ẩn và cổ kính"
        },
        {
            "category": "Font chữ",
            "asset_name": "Liberation Sans SDF (TextMesh Pro Fallback Font)",
            "file_path": "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset",
            "author_creator": "Red Hat & Unity Technologies",
            "source_type": "Open Font License (OFL)",
            "source_url": "https://github.com/liberationfonts/liberation-fonts",
            "license": "SIL Open Font License 1.1",
            "notes": "Font fallback dự phòng đảm bảo 100% hiển thị ký tự Latin và số"
        },

        # --- MÔI TRƯỜNG & MAP TÀN TÍCH (TILES & ENVIRONMENT) ---
        {
            "category": "Môi trường (Tilemap)",
            "asset_name": "Ancient Dungeon Tileset & Hazards (Sàn đá, Vách tường, Đầm độc, Nham thạch)",
            "file_path": "Assets/Sprites/Tiles, Assets/Environment",
            "author_creator": "0x72 & Szadi art",
            "source_type": "2D Dark Fantasy Dungeon Pack",
            "source_url": "https://szadiart.itch.io",
            "license": "Standard Commercial Game License",
            "notes": "Gạch đá cổ đại, các cõi nguyên tố Magma Calamity, Frostfall Blizzard, Acid Downpour"
        }
    ]

    wb = openpyxl.Workbook()
    ws = wb.active
    ws.title = "Nguồn Tài Nguyên Game (Assets)"

    header_fill = PatternFill(start_color="1E3A8A", end_color="1E3A8A", fill_type="solid")
    header_font = Font(name="Calibri", size=11, bold=True, color="FFFFFF")
    data_font = Font(name="Calibri", size=10)
    border_side = Side(style='thin', color="D1D5DB")
    cell_border = Border(left=border_side, right=border_side, top=border_side, bottom=border_side)

    headers = [
        "STT", "Phân Loại (Category)", "Tên Tài Nguyên (Asset Name)",
        "Đường Dẫn Trong Dự Án (File Path)", "Tác Giả / Nhà Sáng Tạo",
        "Loại Nguồn Gốc (Source Type)", "Liên Kết Nguồn (Source Link)",
        "Giấy Phép Bản Quyền (License)", "Ghi Chú Công Dụng Trong Game"
    ]

    ws.append(headers)
    for col_idx in range(1, len(headers) + 1):
        cell = ws.cell(row=1, column=col_idx)
        cell.fill = header_fill
        cell.font = header_font
        cell.alignment = Alignment(horizontal="center", vertical="center", wrap_text=True)
        cell.border = cell_border

    ws.row_dimensions[1].height = 28

    category_colors = {
        "Nhân vật (Hero)": "F0FDF4",
        "Nhân vật (Template & Hero)": "F0FDF4",
        "Quái vật & Boss": "FEF2F2",
        "Icon & Kỹ Năng": "FEFCE8",
        "Âm thanh (SFX)": "EFF6FF",
        "Âm nhạc (BGM)": "EFF6FF",
        "Font chữ": "FAF5FF",
        "Môi trường (Tilemap)": "F8FAFC"
    }

    for idx, s in enumerate(asset_sources, 1):
        row_num = idx + 1
        row_data = [
            idx,
            s['category'],
            s['asset_name'],
            s['file_path'],
            s['author_creator'],
            s['source_type'],
            s['source_url'],
            s['license'],
            s['notes']
        ]
        ws.append(row_data)

        fill_color = category_colors.get(s['category'], "FFFFFF")
        row_fill = PatternFill(start_color=fill_color, end_color=fill_color, fill_type="solid")

        for col_idx in range(1, len(headers) + 1):
            cell = ws.cell(row=row_num, column=col_idx)
            cell.font = data_font
            cell.border = cell_border
            cell.fill = row_fill
            if col_idx in [1, 2]:
                cell.alignment = Alignment(horizontal="center", vertical="center")
            elif col_idx in [7]:
                cell.alignment = Alignment(horizontal="left", vertical="center")
                cell.font = Font(name="Calibri", size=10, color="2563EB", underline="single")
            else:
                cell.alignment = Alignment(horizontal="left", vertical="center", wrap_text=True)

    col_widths = {1: 6, 2: 24, 3: 32, 4: 36, 5: 28, 6: 28, 7: 35, 8: 30, 9: 45}
    for col_idx, width in col_widths.items():
        ws.column_dimensions[get_column_letter(col_idx)].width = width

    out_path = "Vaelstrider_Game_Assets_Sources.xlsx"
    wb.save(out_path)
    print(f"Exported {len(asset_sources)} asset sources to {out_path} successfully!")


if __name__ == '__main__':
    export_augments_excel()
    export_assets_sources_excel()
