# Main Patcher Script
import os

def write_f(path, text):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, 'w', encoding='utf-8') as fp:
        fp.write(text)
    print(f'[SUCCESS] Wrote {path}')
mmm_code = '''using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections; 
                                 