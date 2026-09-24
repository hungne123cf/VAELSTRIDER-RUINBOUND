import sys, os, base64

if len(sys.argv) >= 3:
    path = sys.argv[1]
    b64 = sys.argv[2]
    data = base64.b64decode(b64 + '=' * (-len(b64) % 4))
    d = os.path.dirname(path)
    if d:
        os.makedirs(d, exist_ok=True)
    with open(path, 'wb') as f:
        f.write(data)
    print(f'[WROTE] {path}')
