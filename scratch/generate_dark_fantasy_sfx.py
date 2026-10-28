import math
import struct
import wave
import random
import os

SAMPLE_RATE = 44100

def clamp(val, low=-1.0, high=1.0):
    return max(low, min(high, val))

def write_wav(filename, samples, sample_rate=SAMPLE_RATE):
    os.makedirs(os.path.dirname(filename), exist_ok=True)
    with wave.open(filename, 'w') as wav_file:
        wav_file.setnchannels(1)  # Mono
        wav_file.setsampwidth(2)  # 16-bit
        wav_file.setframerate(sample_rate)
        
        # Soft-clip and convert to 16-bit signed int
        packed_frames = bytearray()
        for s in samples:
            # Soft saturate
            s = math.tanh(s)
            val = int(clamp(s, -0.999, 0.999) * 32767)
            packed_frames.extend(struct.pack('<h', val))
        wav_file.writeframes(packed_frames)
    print(f"Generated {filename} ({len(samples)/sample_rate:.2f}s, {len(samples)} samples)")

# 1. SWORD SLASH LIGHT (Fast Katana Whoosh with Razor Edge)
def generate_sword_slash_light():
    dur = 0.18
    n = int(dur * SAMPLE_RATE)
    samples = []
    
    # White noise buffer
    noise = [random.uniform(-1, 1) for _ in range(n)]
    
    for i in range(n):
        t = i / SAMPLE_RATE
        progress = t / dur
        
        # Envelope: very snappy attack (12ms), smooth decay
        if progress < 0.12:
            env = math.sin((progress / 0.12) * (math.pi / 2))
        else:
            decay_p = (progress - 0.12) / 0.88
            env = math.exp(-decay_p * 5.0)
            
        # Frequency sweep from 2800Hz down to 600Hz
        freq = 2800.0 * (1.0 - progress * 0.78)
        blade_sine = math.sin(2 * math.pi * freq * t) * 0.45
        
        # High blade singing harmonic
        high_sing = math.sin(2 * math.pi * (freq * 1.85) * t) * 0.2
        
        # Filtered noise component
        air_noise = noise[i] * 0.35 * (1.0 - progress * 0.5)
        
        val = (blade_sine + high_sing + air_noise) * env * 1.1
        samples.append(val)
    return samples

# 2. SWORD SLASH HEAVY (Deep roaring cleave with Iaido weight)
def generate_sword_slash_heavy():
    dur = 0.30
    n = int(dur * SAMPLE_RATE)
    samples = []
    noise = [random.uniform(-1, 1) for _ in range(n)]
    
    for i in range(n):
        t = i / SAMPLE_RATE
        progress = t / dur
        
        # Envelope: punchy swell and heavy release
        if progress < 0.15:
            env = math.sin((progress / 0.15) * (math.pi / 2))
        else:
            env = math.exp(-((progress - 0.15) / 0.85) * 4.2)
            
        # Low frequency displacement: 180Hz down to 55Hz
        sub_freq = 180.0 * math.exp(-progress * 2.5)
        sub_sine = math.sin(2 * math.pi * sub_freq * t) * 0.65
        
        # Mid blade body: 850Hz down to 250Hz
        mid_freq = 850.0 * (1.0 - progress * 0.7)
        mid_sine = math.sin(2 * math.pi * mid_freq * t) * 0.35
        
        # Slicing air friction
        air = noise[i] * 0.4 * (1.0 - progress * 0.6)
        
        val = (sub_sine + mid_sine + air) * env * 1.25
        samples.append(val)
    return samples

# 3. KATANA SHEATHE (Friction slide + crisp metallic ring)
def generate_katana_sheathe():
    dur = 0.36
    n = int(dur * SAMPLE_RATE)
    samples = []
    noise = [random.uniform(-1, 1) for _ in range(n)]
    
    for i in range(n):
        t = i / SAMPLE_RATE
        
        # Phase 1: Blade sliding into wooden/metal scabbard (0.0 to 0.14s)
        if t < 0.14:
            p1 = t / 0.14
            env1 = math.sin(p1 * math.pi) * 0.35
            slide_tone = math.sin(2 * math.pi * (1600 + p1 * 800) * t) * 0.2
            slide_noise = noise[i] * 0.4
            val = (slide_tone + slide_noise) * env1
        else:
            # Phase 2: The "CHING!" clink (0.14s to 0.36s)
            t2 = t - 0.14
            p2 = t2 / 0.22
            env2 = math.exp(-p2 * 6.5)
            # High resonant bell harmonics
            bell1 = math.sin(2 * math.pi * 3250.0 * t2) * 0.55
            bell2 = math.sin(2 * math.pi * 5180.0 * t2) * 0.35
            bell3 = math.sin(2 * math.pi * 1850.0 * t2) * 0.25
            click = noise[i] * 0.4 * math.exp(-p2 * 25.0)
            val = (bell1 + bell2 + bell3 + click) * env2 * 1.2
            
        samples.append(val)
    return samples

# 4. HIT IMPACT VISCERAL (Punchy organic flesh slice + chest thump)
def generate_hit_impact():
    dur = 0.24
    n = int(dur * SAMPLE_RATE)
    samples = []
    noise = [random.uniform(-1, 1) for _ in range(n)]
    
    for i in range(n):
        t = i / SAMPLE_RATE
        progress = t / dur
        
        # Fast explosive attack (4ms)
        if progress < 0.05:
            env = progress / 0.05
        else:
            env = math.exp(-((progress - 0.05) / 0.95) * 5.8)
            
        # Sub punch kick (88Hz -> 38Hz)
        sub_f = 92.0 * math.exp(-progress * 3.5)
        sub = math.sin(2 * math.pi * sub_f * t) * 0.85
        
        # Flesh crunch / organic tear (filtered harsh noise)
        crunch_env = math.exp(-progress * 9.0)
        crunch = noise[i] * 0.65 * crunch_env
        
        # Blade bite snap
        snap_f = 1450.0 * math.exp(-progress * 8.0)
        snap = math.sin(2 * math.pi * snap_f * t) * 0.4 * crunch_env
        
        val = (sub + crunch + snap) * env * 1.3
        samples.append(val)
    return samples

# 5. CRIT STRIKE IMPACT (Explosive sub bass + glass/steel shatter + rune chime)
def generate_crit_impact():
    dur = 0.58
    n = int(dur * SAMPLE_RATE)
    samples = []
    noise = [random.uniform(-1, 1) for _ in range(n)]
    
    for i in range(n):
        t = i / SAMPLE_RATE
        progress = t / dur
        
        # 1. Explosive initial transient
        if progress < 0.03:
            hit_env = progress / 0.03
        else:
            hit_env = math.exp(-((progress - 0.03) / 0.97) * 7.0)
            
        # Deep Sub Drop (120Hz -> 35Hz)
        sub = math.sin(2 * math.pi * (120.0 * math.exp(-progress * 4.0)) * t) * 0.95 * hit_env
        
        # Sharp high shatter snap
        shatter_env = math.exp(-progress * 18.0)
        shatter = (noise[i] * 0.8 + math.sin(2 * math.pi * 3800 * t) * 0.5) * shatter_env
        
        # 2. Dark Fantasy Chime / Soul Ring (Ethereal lingering resonance)
        ring_env = math.exp(-progress * 3.2) * (1.0 - math.exp(-progress * 25.0))
        # Chord: A minor / Dorian rune tone (550Hz, 825Hz, 1100Hz, 1650Hz)
        chime = (
            math.sin(2 * math.pi * 550.0 * t) * 0.35 +
            math.sin(2 * math.pi * 825.0 * t) * 0.28 +
            math.sin(2 * math.pi * 1100.0 * t) * 0.22 +
            math.sin(2 * math.pi * 1650.0 * t) * 0.15
        ) * ring_env
        
        val = (sub + shatter + chime) * 1.35
        samples.append(val)
    return samples

# 6. TITAN SLAM / GROUND SHATTER (Heavy bludgeon & earth vibration)
def generate_titan_slam():
    dur = 0.45
    n = int(dur * SAMPLE_RATE)
    samples = []
    noise = [random.uniform(-1, 1) for _ in range(n)]
    
    for i in range(n):
        t = i / SAMPLE_RATE
        progress = t / dur
        
        env = math.exp(-progress * 4.8) if progress >= 0.02 else progress / 0.02
        
        # Huge seismic rumble (75Hz -> 25Hz)
        rumble = math.sin(2 * math.pi * (75.0 * math.exp(-progress * 2.8)) * t) * 1.0
        
        # Rock crack / debris
        debris = noise[i] * 0.6 * math.exp(-progress * 7.5)
        
        # Heavy metallic / dense bludgeon body
        thud = math.sin(2 * math.pi * 160.0 * math.exp(-progress * 5.0) * t) * 0.5 * env
        
        val = (rumble + debris + thud) * env * 1.3
        samples.append(val)
    return samples

# 7. DASH WIND BURST
def generate_dash_wind():
    dur = 0.22
    n = int(dur * SAMPLE_RATE)
    samples = []
    noise = [random.uniform(-1, 1) for _ in range(n)]
    
    for i in range(n):
        t = i / SAMPLE_RATE
        progress = t / dur
        
        # Smooth bell envelope
        env = math.sin(progress * math.pi)
        
        # Whoosh bandpass noise
        air = noise[i] * 0.75
        sweep = math.sin(2 * math.pi * (450 + 600 * math.sin(progress * math.pi)) * t) * 0.35
        
        val = (air * 0.65 + sweep) * env * 1.15
        samples.append(val)
    return samples

# 8. CHAIN LIGHTNING / THUNDER CRACKLE
def generate_thunder_strike():
    dur = 0.38
    n = int(dur * SAMPLE_RATE)
    samples = []
    noise = [random.uniform(-1, 1) for _ in range(n)]
    
    for i in range(n):
        t = i / SAMPLE_RATE
        progress = t / dur
        
        # Electric zap envelope
        env = math.exp(-progress * 5.5) if progress >= 0.01 else progress / 0.01
        
        # FM synthesis for electric buzz: carrier 1400Hz, modulator 190Hz
        mod = math.sin(2 * math.pi * 190.0 * t) * 600.0
        zap = math.sin(2 * math.pi * (1400.0 + mod) * t) * 0.55
        
        # Low thunder boom
        boom = math.sin(2 * math.pi * (95.0 * math.exp(-progress * 3.0)) * t) * 0.65
        
        # Electrical crackle
        crackle = noise[i] * 0.4 * math.exp(-progress * 8.0)
        
        val = (zap + boom + crackle) * env * 1.25
        samples.append(val)
    return samples

# 9. STONE UI CLICK (Soulslike stone click / slider)
def generate_stone_ui_click():
    dur = 0.12
    n = int(dur * SAMPLE_RATE)
    samples = []
    noise = [random.uniform(-1, 1) for _ in range(n)]
    
    for i in range(n):
        t = i / SAMPLE_RATE
        progress = t / dur
        
        env = math.exp(-progress * 14.0)
        tap = math.sin(2 * math.pi * 220.0 * t) * 0.6
        clink = math.sin(2 * math.pi * 2650.0 * t) * 0.35 * math.exp(-progress * 28.0)
        fric = noise[i] * 0.35 * env
        
        val = (tap + clink + fric) * env * 1.1
        samples.append(val)
    return samples

# 10. RUNIC GEM BUY / ASCENSION HARMONIC (Glorious Dark Fantasy Chord)
def generate_gem_buy_chime():
    dur = 0.65
    n = int(dur * SAMPLE_RATE)
    samples = []
    
    for i in range(n):
        t = i / SAMPLE_RATE
        progress = t / dur
        
        env = math.exp(-progress * 3.2) if progress >= 0.02 else progress / 0.02
        
        # Ascending celestial chord: C5, E5, G5, B5, D6
        c5 = math.sin(2 * math.pi * 523.25 * t) * 0.35
        e5 = math.sin(2 * math.pi * 659.25 * t) * 0.30
        g5 = math.sin(2 * math.pi * 783.99 * t) * 0.28
        b5 = math.sin(2 * math.pi * 987.77 * t) * 0.22
        d6 = math.sin(2 * math.pi * 1174.66 * t) * 0.18
        
        # Shimmer harmonic
        shimmer = math.sin(2 * math.pi * 2349.32 * t) * 0.12 * math.exp(-progress * 5.0)
        
        val = (c5 + e5 + g5 + b5 + d6 + shimmer) * env * 1.25
        samples.append(val)
    return samples

def main():
    target_dir = "Assets/Audio/SFX"
    
    write_wav(os.path.join(target_dir, "sword_slash.wav"), generate_sword_slash_light())
    write_wav(os.path.join(target_dir, "sword_slash_heavy.wav"), generate_sword_slash_heavy())
    write_wav(os.path.join(target_dir, "katana_sheathe.wav"), generate_katana_sheathe())
    write_wav(os.path.join(target_dir, "hit_impact.wav"), generate_hit_impact())
    write_wav(os.path.join(target_dir, "hit_crit.wav"), generate_crit_impact())
    write_wav(os.path.join(target_dir, "titan_slam.wav"), generate_titan_slam())
    write_wav(os.path.join(target_dir, "dash_wind.wav"), generate_dash_wind())
    write_wav(os.path.join(target_dir, "thunder_strike.wav"), generate_thunder_strike())
    write_wav(os.path.join(target_dir, "ui_stone_click.wav"), generate_stone_ui_click())
    write_wav(os.path.join(target_dir, "heavenly_ascension.wav"), generate_gem_buy_chime())
    
    print("ALL DARK FANTASY SFX GENERATED SUCCESSFULLY!")

if __name__ == "__main__":
    main()
