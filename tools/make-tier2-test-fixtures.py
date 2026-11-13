"""Create owned synthetic inputs for the upstream sky GPU tests (no game assets)."""
from pathlib import Path
import argparse
import struct

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('output', type=Path)
args = p.parse_args()
args.output.mkdir(parents=True, exist_ok=True)
geometry = [0] * (485 * 6)
for i in range(485):
    geometry[i * 6 + 5] = (1023 << 16) | (20 if i % 2 else 4076)
# Construct a fixture with the verified FNV fingerprint. It is synthetic geometry,
# solely for exercising the GPU verdict, exact UV math and mismatch branches.
h = 2166136261
for i in range(485):
    for j in (0, 1, 2, 5):
        if i == 484 and j == 5:
            geometry[i * 6 + j] = h ^ ((0xba38bc6f * pow(16777619, -1, 1 << 32)) & 0xffffffff)
        h = ((h ^ geometry[i * 6 + j]) * 16777619) & 0xffffffff
assert h == 0xba38bc6f
image = bytearray(2048 * 512 * 4)
for y in range(512):
    for x in range(2048):
        n = ((x * 13 + y * 7) % 3) - 1 if y < 96 else 0
        at = (y * 2048 + x) * 4
        image[at:at + 4] = bytes((8 + n, 14 + n, 21 + n, 255))
points = [(0,0),(128,4),(256,8),(512,12),(768,16),(1024,24),(1280,32),(1536,48),
          (1792,64),(2047,80),(32,128),(320,256),(640,320),(960,384),(1280,448),(1920,511)]
values = [(8,14,21),(8,16,21),(10,17,24),(10,17,27),(13,21,30),(16,24,33),(18,29,41),(24,37,51),
          (27,45,60),(41,56,74),(49,73,93),(115,125,123),(142,148,148),(123,134,132),(79,89,90),(99,98,90)]
for (x,y), rgb in zip(points, values):
    at = (y * 2048 + x) * 4
    image[at:at + 3] = bytes(rgb)
for name, data in [('sky-geometry.bin', struct.pack('<' + 'I' * len(geometry), *geometry)),
                   ('sky-panorama.rgba8', image)]:
    target = args.output / name
    if target.exists() and target.read_bytes() != data:
        raise SystemExit(f'Refusing to overwrite a different fixture: {target}')
    target.write_bytes(data)
print(f'Owned synthetic sky fixtures: {args.output.resolve()}')
