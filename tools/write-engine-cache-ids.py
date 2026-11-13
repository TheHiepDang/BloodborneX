"""Reproduce applied engine build.sh's cache IDs for a native Windows build."""
from pathlib import Path
import argparse
import hashlib
import re

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('engine', type=Path)
args = p.parse_args()
root = args.engine.resolve()
if not (root / 'build.sh').is_file():
    p.error('Expected a complete applied engine source tree')
files = set()
for directory in ['gpu/shadps4/shader_recompiler', 'gpu/shadps4/video_core/amdgpu']:
    files.update(f for f in (root / directory).rglob('*') if f.suffix in ('.cpp','.h','.inc'))
for pattern in ['gpu/shadps4/common/serdes.h', 'gpu/shadps4/video_core/cache_storage.cpp',
                'gpu/shadps4/video_core/cache_storage.h',
                'gpu/shadps4/video_core/renderer_vulkan/liverpool_to_vk.*',
                'gpu/shadps4/video_core/renderer_vulkan/vk_*pipeline*',
                'gpu/shadps4/video_core/renderer_vulkan/pipeline_cache_blob.h',
                'gpu/shadps4/video_core/renderer_vulkan/vk_object_motion.*']:
    files.update(f for f in root.glob(pattern) if f.is_file() and f.suffix in ('.cpp','.h','.inc'))
files = sorted(files, key=lambda f: f.relative_to(root).as_posix().encode())
exclude = re.compile(r'vk_(graphics|compute)_pipeline\.cpp$|vk_pipeline_common\.(cpp|h)$|pipeline_compile_queue\.h$|native/capture\.(cpp|h)$')
whole = b''.join(f.read_bytes() for f in files)
normalized = b''.join(f.read_bytes() for f in files if not exclude.search(f.as_posix())).replace(b'\r',b'')
lines = []
for line in normalized.split(b'\n'):
    if any(s in line for s in [b'Shader::Native::Capture',b'shader_recompiler/native/capture.h',b'lab_flat',b'fetch_override']):
        continue
    if re.match(rb'^\s*//',line):
        continue
    line = line.rstrip(b' \t\v\f')
    if line:
        lines.append(line+b'\n')
out = root / 'out'
out.mkdir(exist_ok=True)
for name, data in [('gpu-cache.id', b''.join(lines)), ('gpu-cache.legacy.id', whole)]:
    identity = hashlib.sha256(data).hexdigest()[:16]
    (out / name).write_text(identity+'\n',encoding='ascii',newline='\n')
    print(f'{name}: {identity}')
