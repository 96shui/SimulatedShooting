"""Restore byte-for-byte pre-run files, excluding this task's explicitly owned sources/docs."""
from pathlib import Path
import hashlib
import json
import shutil
import subprocess

root = Path.cwd().resolve()
backup = Path('C:/Users/10532/AppData/Local/Temp/round7-enemy-vision-backup')
names = json.loads((backup / 'manifest.json').read_text(encoding='utf8'))
owned = {
    'Assets/SimulatedShooting/Runtime/Scene/CombatActorView.cs',
    'Assets/SimulatedShooting/Runtime/Scene/CombatDoorView.cs',
    'Assets/SimulatedShooting/Runtime/Scene/CombatSceneRuntime.cs',
    'Assets/VRShooting/Runtime/Application/Combat/CombatCoreService.cs',
    'Assets/VRShooting/Runtime/Application/Combat/UrbanService.cs',
    'Assets/VRShooting/Runtime/Contracts/P3/CombatConfigDto.cs',
    'Assets/VRShooting/Runtime/Contracts/P3/CombatCoreContracts.cs',
    'docs/BDD/README.md', 'docs/接口文档/README.md',
    'docs/任务规格/阶段三-P3-小型堑壕与城镇战斗原型/玩法功能/task004-combat-input-weapon-damage-and-enemy.md',
    'docs/任务规格/阶段三-P3-小型堑壕与城镇战斗原型/玩法功能/task010-urban-phases-room-search-and-results.md',
    'docs/任务规格/阶段三-P3-小型堑壕与城镇战斗原型/场景/task012-urban-doors-navigation-and-feedback.md',
}
out = root / 'docs/codex-reports/evidence/round7-enemy-vision-2026-11-15/cleanup.json'
previous = json.loads(out.read_text(encoding='utf8')) if out.exists() else {}
restored = previous.get('restored', [])
for name in names:
    if name in owned:
        continue
    current, saved = root / name, backup / name
    if not current.exists() or current.read_bytes() != saved.read_bytes():
        current.parent.mkdir(parents=True, exist_ok=True); shutil.copy2(saved, current); restored.append(name)

# Tests may create extra pictures in old evidence directories and temporary test scenes.
# Preserve every file that existed initially, including tracked InitTestScene assets.
removed = previous.get('removed_new_batch_files', [])
candidates = list((root / 'docs/codex-reports/evidence').rglob('*')) + list((root / 'Assets').glob('InitTestScene*'))
for path in candidates:
    if not path.is_file():
        continue
    name = path.relative_to(root).as_posix()
    if '/round7-enemy-vision-2026-11-15/' in '/' + name or name in names:
        continue
    path.unlink(); removed.append(name)

# One historical test writes this root-level tracked image. It was clean at entry
# (unlike the protected menu files), so its original bytes also equal the HEAD blob.
name = 'Evidence/P3-production-M67-in-flight.png'
initial_status = (backup / 'initial-status.txt').read_text(encoding='utf8')
assert name not in initial_status, 'Do not restore any pre-existing change from HEAD'
original = subprocess.check_output(['git', 'show', 'HEAD:' + name])
if (root / name).read_bytes() != original:
    (root / name).write_bytes(original); restored.append(name)

protected = [
    'Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs',
    'Assets/VRShooting/Runtime/Unity/UI/TacticalUIStyle.cs',
    'Assets/VRShooting/Tests/EditMode/Application/MainMenuControlsTests.cs',
    'Assets/VRShooting/Tests/PlayMode/UI/Screen02_04_MainMenuZeroingBriefingUITests.cs',
    'docs/BDD/screens/02-游戏主界面.feature.md',
    'docs/任务规格/阶段一-P1-100m射校原型闭环/UI/task008-ui-mainmenu-zeroing-briefing.md',
    'docs/接口文档/01-页面导航与UI事件.md',
]
checks = {name: hashlib.sha256((root / name).read_bytes()).hexdigest() for name in protected}
assert all((root / name).read_bytes() == (backup / name).read_bytes() for name in protected)
assert all((root / name).read_bytes() == (backup / name).read_bytes() for name in names
           if name.startswith('docs/codex-reports/evidence/'))
init_scenes = [name for name in names if name.startswith('Assets/InitTestScene')]
assert all((root / name).is_file() and (root / name).read_bytes() == (backup / name).read_bytes() for name in init_scenes)
result = {'restored': restored, 'removed_new_batch_files': removed,
          'protected_mainmenu_sha256': checks, 'old_evidence_identical': True, 'preserved_init_test_scenes': init_scenes}
out.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf8')
print(json.dumps(result, ensure_ascii=False, indent=2))
