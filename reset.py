import sys, os
path = sys.argv[1]
d = os.path.dirname(path)
if d: os.makedirs(d, exist_ok=True)
open(path, 'wb').close()
print(f'[RESET] {path}')
