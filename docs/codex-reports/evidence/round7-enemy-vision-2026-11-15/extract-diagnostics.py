"""Extract verbatim diagnostics from the retained original Unity logs."""
from pathlib import Path
import re

folder = Path(__file__).resolve().parent
for source, output in [('repro-gates-before.log', 'room11-room12-before.log'), ('playmode-full.log', 'room11-room12-after.log')]:
    lines = (folder / source).read_text(encoding='utf8', errors='replace').splitlines()
    selected = [line for line in lines if '[R7-' in line or '[EnemyVision]' in line]
    (folder / output).write_text('Source: ' + source + '\n' + '\n'.join(selected) + '\n', encoding='utf8')

lines = (folder / 'playmode-full.log').read_text(encoding='utf8', errors='replace').splitlines()
indices = set()
phases = ['room-11', 'room-12-0', 'room-12-9', 'room-12-19', 'room-12-29',
          'registered-closed-room-11', 'registered-closed-room-12-0', 'registered-closed-room-12-29']
for index, line in enumerate(lines):
    if '[R7-Boundary]' in line or '[R7-Floor]' in line:
        indices.add(index)
    if '[R7-Path]' in line and any(f'phase={phase} ' in line for phase in phases):
        indices.add(index)
        tick = re.search(r' tick=(\d+) ', line).group(1)
        for previous in range(index - 1, max(-1, index - 100), -1):
            if '[EnemyVision]' in lines[previous] and '.enemy-001 ' in lines[previous] and f'tick={tick} ' in lines[previous]:
                indices.add(previous); break

closed = next((i for i, line in enumerate(lines) if '[EnemyVision]' in line and 'chest=room-12/DoorLeaf' in line and 'Visible=False' in line), None)
if closed is not None:
    indices.add(closed)
(folder / 'diagnostic-keyframes.log').write_text('Source: playmode-full.log; lines below are verbatim.\n' +
    '\n'.join(lines[i] for i in sorted(indices)) + '\n', encoding='utf8')

path = [line for line in lines if '[R7-Path] phase=room-12-' in line]
aligned = next(line for line in path if float(re.search(r' angle=([\d.]+)', line).group(1)) <= 12)
damaged = next(line for line in path if float(re.search(r' health=([\d.]+)', line).group(1)) < 10)
print('First frame:', path[0]); print('Aligned:', aligned); print('Damaged:', damaged); print('Last frame:', path[-1])
