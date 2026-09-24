# -*- coding: utf-8 -*-
import os, sys

def write_file(path, text):
    d = os.path.dirname(path)
    if d: os.makedirs(d, exist_ok=True)
    with open(path, 'w', encoding='utf-8') as f:
        f.write(text.strip() + '\n')
    print(f'[OK] {path}')
