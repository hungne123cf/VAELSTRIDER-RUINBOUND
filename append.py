import sys, os, base64, re
if len(sys.argv) >= 3:
    path = sys.argv[1]
    b64 = re.sub(r']A-Za-z0-9+/=]', '', ''.join(sys.argv[2:]))
    while len(b64) % 4 != 0:
        b64 += '='
    data = base64.b64decode(b64)
    d = os.path.dirname(path)
    if d: os.makedirs(d, exist_ok=True)
    with open(path, 'ab') as f:
        f.write(data)
    print(f'[APPENDED] {len(data)} bytes to {path}')
