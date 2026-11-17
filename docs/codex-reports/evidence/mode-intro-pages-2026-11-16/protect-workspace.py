"""Snapshot batch-writable resources and preserve all pre-existing evidence/work."""
from pathlib import Path
import hashlib
import json
import shutil
import subprocess
import sys

ROOT = Path.cwd().resolve()
OUT = ROOT / 'docs/codex-reports/evidence/mode-intro-pages-2026-11-16'
BACKUP = Path('C:/Users/10532/AppData/Local/Temp/mode-intro-pages-2026-11-16-backup')
OWNED = {
    'Assets/SimulatedShooting/Tests/PlayMode/BriefResultsGuidanceRegressionTests.cs',
    'Assets/SimulatedShooting/Tests/PlayMode/P3ProductionFlowTests.cs',
    'Assets/SimulatedShooting/Tests/PlayMode/XRSceneHandoffTests.cs',
    'Assets/SimulatedShooting/Tests/PlayMode/SimulatedShooting.PlayModeTests.asmdef',
    'Assets/VRShooting/Editor/ModeOneScreenshotAdvance.cs',
    'Assets/VRShooting/Editor/TacticalUIGallery.cs',
    'Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs',
    'Assets/VRShooting/Runtime/Unity/UI/TacticalUIStyle.cs',
    'Assets/VRShooting/Tests/EditMode/Application/MainMenuControlsTests.cs',
    'Assets/VRShooting/Tests/PlayMode/Flow/P1FlowPlayModeGateTests.cs',
    'Assets/VRShooting/Tests/PlayMode/UI/Screen02_04_MainMenuZeroingBriefingUITests.cs',
    'Assets/VRShooting/Tests/PlayMode/UI/Screen02_05_SceneOwnedUIFlowTests.cs',
    'Assets/VRShooting/Tests/PlayMode/UI/Screen05_ZeroingHudUITests.cs',
    'Assets/VRShooting/Tests/PlayMode/UI/Screen06_ZeroingImpactAnalysisUITests.cs',
    'Assets/VRShooting/Tests/PlayMode/UI/Screen07_ZeroingFinalRatingUITests.cs',
    'docs/BDD/README.md',
    'docs/BDD/screens/02-游戏主界面.feature.md',
    'docs/接口文档/01-页面导航与UI事件.md',
    'docs/任务规格/阶段一-P1-100m射校原型闭环/UI/task008-ui-mainmenu-zeroing-briefing.md',
}


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def collect():
    tracked = subprocess.check_output(['git', 'ls-files', '-z']).decode('utf8').split('\0')
    names = {name for name in tracked if name and (ROOT / name).is_file() and
             ((ROOT / name).suffix.lower() in {'.cs', '.meta', '.asset', '.unity', '.prefab', '.md', '.json', '.asmdef'}
              or name.startswith(('docs/', 'Evidence/', 'ProjectSettings/', 'Packages/', 'Assets/Screenshots/')))}
    for folder in ('docs', 'Evidence'):
        names.update(p.relative_to(ROOT).as_posix() for p in (ROOT / folder).rglob('*')
                     if p.is_file() and not p.is_relative_to(OUT))
    names.update(n for n in subprocess.check_output(['git', 'ls-files', '--others', '--exclude-standard', '-z']).decode('utf8').split('\0')
                 if n and n.startswith('Assets/') and (ROOT / n).is_file())
    return sorted(names)

if sys.argv[1] == 'snapshot':
    assert not BACKUP.exists(), 'Use a fresh backup; never overwrite the entry snapshot.'
    BACKUP.mkdir(parents=True)
    names = collect()
    manifest = {}
    for name in names:
        target = BACKUP / name
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(ROOT / name, target)
        manifest[name] = digest(target)
    (BACKUP / 'manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf8')
    (BACKUP / 'initial-status.txt').write_bytes(subprocess.check_output(['git', '-c', 'core.quotepath=false', 'status', '--short']))
    print(f'Protected {len(names)} files, {sum((BACKUP/n).stat().st_size for n in names):,} bytes in {BACKUP}')
else:
    assert sys.argv[1] == 'restore'
    assert not subprocess.check_output(['tasklist', '/FI', 'IMAGENAME eq Unity.exe', '/FO', 'CSV']).decode(errors='replace').count('"Unity.exe"'), 'Wait for Unity to exit.'
    manifest = json.loads((BACKUP / 'manifest.json').read_text(encoding='utf8'))
    restored = []
    for name, sha in manifest.items():
        if name in OWNED:
            continue
        current = ROOT / name
        if not current.exists() or digest(current) != sha:
            current.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(BACKUP / name, current)
            restored.append(name)
    # This tracked root-level PNG was clean at entry; the initial lightweight snapshot
    # omitted root Evidence PNGs. Recover its exact bytes using read-only git show.
    clean_head_evidence = ['Evidence/P3-production-M67-in-flight.png']
    entry_status = (BACKUP/'initial-status.txt').read_text(encoding='utf-8')
    for name in clean_head_evidence:
        assert name not in entry_status, 'Never overwrite pre-existing user evidence changes.'
        original = subprocess.check_output(['git', 'show', '9a1c012:' + name])
        if (ROOT/name).read_bytes() != original:
            (ROOT/name).write_bytes(original)
            restored.append(name)
        assert (ROOT/name).read_bytes() == original
    removed = []
    candidates = list((ROOT/'docs/codex-reports/evidence').rglob('*')) + list((ROOT/'Assets').glob('InitTestScene*'))
    for path in candidates:
        if not path.is_file() or path.is_relative_to(OUT):
            continue
        name = path.relative_to(ROOT).as_posix()
        if name not in manifest:
            path.unlink()
            removed.append(name)
    protected = {name: digest(ROOT/name) for name in manifest if name not in OWNED}
    assert all(sha == manifest[name] for name, sha in protected.items())
    old_evidence = [n for n in protected if n.startswith('docs/codex-reports/evidence/')]
    scenes = [n for n in protected if n.startswith('Assets/InitTestScene')]
    changes = [n for n in OWNED if digest(ROOT/n) != manifest[n]]
    result = {'clean_head_evidence_verified': clean_head_evidence, 'restored': restored, 'removed_new_batch_files': removed,
              'all_protected_files_identical': True, 'protected_file_count': len(protected),
              'old_evidence_files_identical': len(old_evidence), 'preserved_init_test_scenes': scenes,
              'task_changed_files': sorted(changes),
              'enemy_task_files_sha256': {n: sha for n, sha in protected.items() if
                  'Round7' in n or any(t in n for t in ('CombatEnemySight', 'CombatActorView.cs', 'CombatDoorView.cs',
                    'CombatSceneRuntime.cs', 'CombatCoreService.cs', 'UrbanService.cs', 'CombatConfigDto.cs', 'CombatCoreContracts.cs'))}}
    (OUT/'cleanup.json').write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf8')
    print(json.dumps(result, ensure_ascii=False, indent=2))
