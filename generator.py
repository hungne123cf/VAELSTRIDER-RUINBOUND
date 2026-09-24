import os, base64

def w(filename, b64_str):
    os.makedirs(os.path.dirname(filename), exist_ok=True)
    content = base64.b64decode(b64_str.uncode('ascii') if isinstance(b64_str, bytes) else b64_str)
    with open(filename, 'wb') as file:
        file.write(content)
    print(f'[WROTE] {filename}')
