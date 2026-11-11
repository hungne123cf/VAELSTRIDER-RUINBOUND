import re
import os
import openpyxl
from openpyxl.styles import Font, PatternFill, Alignment, Border, Side
from openpyxl.utils import get_column_letter

with open('Assets/Scripts/Player/AugmentDatabase.cs', 'r', encoding='utf-8') as f:
    text = f.read()

start_pos = text.find('private static readonly List<AugmentData> _all')
end_pos = text.find('};', start_pos)
all_block = text[start_pos:end_pos]

items = []
pattern = re.compile(r'new AugmentData\s*\{([\s\S]*?)\n\s*\}', re.MULTILINE)

rarity_map = {
    'Common': 'Bạc (Silver)',
    'Rare': 'Vàng (Gold)',
    'Epic': 'Kim Cương (Prismatic)'
}

for idx, match in enumerate(pattern.finditer(all_block)):
    b = match.group(1)
    
    m_type = re.search(r'type\s*=\s*AugmentType\.(\w+)', b)
    aug_type = m_type.group(1) if m_type else ''
    
    m_nvn = re.search(r'nameVN\s*=\s*"([^"]*)"', b)
    name_vn = m_nvn.group(1) if m_nvn else ''
    
    m_nen = re.search(r'nameEN\s*=\s*"([^"]*)"', b)
    name_en = m_nen.group(1) if m_nen else ''
    
    m_dvn = re.search(r'descVN\s*=\s*"([^"]*)"', b)
    desc_vn = m_dvn.group(1) if m_dvn else ''
    
    m_den = re.search(r'descEN\s*=\s*"([^"]*)"', b)
    desc_en = m_den.group(1) if m_den else ''
    
    m_fvn = re.search(r'flavorVN\s*=\s*"([^"]*)"', b)
    flavor_vn = m_fvn.group(1) if m_fvn else ''
    
    m_fen = re.search(r'flavorEN\s*=\s*"([^"]*)"', b)
    flavor_en = m_fen.group(1) if m_fen else ''
    
    m_val = re.search(r'value\s*=\s*([0-9\.]+f?)', b)
    val_raw = m_val.group(1).replace('f', '') if m_val else '0'
    try:
        val_float = float(val_raw)
        val_str = str(int(val_float)) if val_float.is_integer() else f"{val_float:.1f}"
    except:
        val_str = val_raw
        
    m_dur = re.search(r'duration\s*=\s*(-?[0-9\.]+f?)', b)
    dur_raw = m_dur.group(1).replace('f', '') if m_dur else '-1'
    try:
        dur_float = float(dur_raw)
        if dur_float < 0:
            dur_str = "Vĩnh viễn (Permanent)"
        else:
            dur_str = f"{int(dur_float)}s"
    except:
        dur_str = "Vĩnh viễn"
    
    m_rar = re.search(r'rarity\s*=\s*AugmentData\.Rarity\.(\w+)', b)
    rarity_code = m_rar.group(1) if m_rar else 'Common'
    rarity_vn = rarity_map.get(rarity_code, rarity_code)
    
    m_hero = re.search(r'heroFilter\s*=\s*new\s+string\[\]\s*\{([^}]+)\}', b)
    if m_hero:
        raw_heroes = m_hero.group(1).replace('"', '').replace('CharacterManager.', '').strip()
        hero_vn = raw_heroes
    else:
        hero_vn = "Tất cả Tướng (All Heroes)"
        
    # Format desc
    formatted_desc_vn = desc_vn.replace('{v}', val_str).replace('{d}', dur_raw if dur_raw != '-1' else '')
    clean_desc_vn = re.sub(r'<[^>]+>', '', formatted_desc_vn).replace('\\n', ' ')
    
    clean_flavor_vn = flavor_vn.strip('\"«»')
    
    items.append({
        'stt': idx + 1,
        'type': aug_type,
        'name_vn': name_vn,
        'name_en': name_en,
        'rarity': rarity_vn,
        'value': val_str,
        'duration': dur_str,
        'hero': hero_vn,
        'desc_vn': clean_desc_vn,
        'flavor_vn': clean_flavor_vn
    })

print(f"Parsed {len(items)} items successfully.")

# Tạo file Excel mới: Vaelstrider_Augments_List.xlsx
wb = openpyxl.Workbook()
ws = wb.active
ws.title = "Danh Sách Lõi Nâng Cấp"

headers = [
    "STT", "ID Lõi (Code)", "Tên Tiếng Việt", "Tên Tiếng Anh",
    "Phẩm Chất (Rarity)", "Chỉ Số Cố Định", "Thời Hạn", "Tướng Áp Dụng",
    "Mô Tả Chi Tiết Trong Game (Tiếng Việt)", "Trích Dẫn Lore / Flavor"
]

header_fill = PatternFill(start_color="1F2937", end_color="1F2937", fill_type="solid")
header_font = Font(name="Arial", size=11, bold=True, color="F9FAFB")
border_thin = Side(border_style="thin", color="D1D5DB")
cell_border = Border(top=border_thin, left=border_thin, right=border_thin, bottom=border_thin)

ws.append(headers)
for col_num in range(1, len(headers) + 1):
    cell = ws.cell(row=1, column=col_num)
    cell.fill = header_fill
    cell.font = header_font
    cell.alignment = Alignment(horizontal="center", vertical="center", wrap_text=True)
    cell.border = cell_border

# Rarity colors
fill_silver = PatternFill(start_color="F3F4F6", end_color="F3F4F6", fill_type="solid")
fill_gold = PatternFill(start_color="FEF3C7", end_color="FEF3C7", fill_type="solid")
fill_prismatic = PatternFill(start_color="EDE9FE", end_color="EDE9FE", fill_type="solid")

for it in items:
    row_data = [
        it['stt'], it['type'], it['name_vn'], it['name_en'],
        it['rarity'], it['value'], it['duration'], it['hero'],
        it['desc_vn'], it['flavor_vn']
    ]
    ws.append(row_data)
    row_idx = ws.max_row
    
    # Pick fill
    if "Bạc" in it['rarity']:
        rf = fill_silver
    elif "Vàng" in it['rarity']:
        rf = fill_gold
    else:
        rf = fill_prismatic
        
    for col_idx in range(1, len(row_data) + 1):
        cell = ws.cell(row=row_idx, column=col_idx)
        cell.border = cell_border
        cell.font = Font(name="Arial", size=10)
        if col_idx in [1, 5, 6, 7]:
            cell.alignment = Alignment(horizontal="center", vertical="center")
        elif col_idx in [2, 3, 4, 8]:
            cell.alignment = Alignment(horizontal="left", vertical="center")
        else:
            cell.alignment = Alignment(horizontal="left", vertical="center", wrap_text=True)
        if col_idx == 5:
            cell.fill = rf

# Column widths
col_widths = {
    1: 6,   # STT
    2: 24,  # ID Code
    3: 26,  # Ten VN
    4: 24,  # Ten EN
    5: 22,  # Rarity
    6: 15,  # Chi So
    7: 18,  # Thoi Han
    8: 24,  # Tuong
    9: 55,  # Mo Ta VN
    10: 45  # Flavor Lore
}
for col_idx, width in col_widths.items():
    ws.column_dimensions[get_column_letter(col_idx)].width = width

excel_path = "Vaelstrider_Augments_List.xlsx"
wb.save(excel_path)
print(f"Saved Excel successfully to {excel_path}")

