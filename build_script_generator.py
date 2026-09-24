# -*- coding: utf-8 -*-
import os, sys

def write_file(path, content):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)
    print(f'Wrote {path} ({len(content)} bytes)')

print('Writing all game scripts...')
