"""Preserve the entry workspace while Unity tests regenerate their own artifacts."""
from pathlib import Path
import hashlib
import json
import shutil
import subprocess
import sys

ROOT = Path.cwd().resolve()
OUT = Path(__file__).resolve().parent
BACKUP = Path('C:/Users/10532/AppData/Local/Temp/mode-intro-panel-frame-2026-11-16-backup')
OWNED = {
    'Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs',
    'Assets/VRShooting/Tests/PlayMode/UI/Screen02_04_MainMenuZeroingBriefingUITests.cs',
    'docs/BDD/screens/37-各模式操作介绍.feature.md',
}

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def collect():
    paths = set(subprocess.check_output(['git', 'ls-files', '-z']).decode('utf8').split('\0'))
    paths.update(subprocess.check_output(['git', 'ls-files', '--others', '--exclude-standard', '-z']).decode('utf8').split('\0'))
    return sorted(n for n in paths if n and (ROOT/n).is_file() and not (ROOT/n).is_relative_to(OUT))

if sys.argv[1] == 'snapshot':
    assert not BACKUP.exists(), 'Never overwrite the entry snapshot.'
    BACKUP.mkdir(parents=True)
    names = collect()
    manifest = {name: digest(ROOT/name) for name in names}
    # Backup code/configuration and every existing evidence file that tests may write.
    for name in names:
        if Path(name).suffix.lower() in {'.cs', '.meta', '.asset', '.unity', '.prefab', '.md', '.json', '.asmdef'} or name.startswith(('docs/', 'Evidence/', 'ProjectSettings/', 'Packages/', 'Assets/Screenshots/')):
            target = BACKUP/name
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(ROOT/name, target)
    (BACKUP/'manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf8')
    (OUT/'initial-status.txt').write_bytes(subprocess.check_output(['git', '-c', 'core.quotepath=false', 'status', '--short']))
    print('Protected', len(names), 'files;', sum(p.stat().st_size for p in BACKUP.rglob('*') if p.is_file()), 'backup bytes')
else:
    assert sys.argv[1] == 'restore-generated'
    manifest = json.loads((BACKUP/'manifest.json').read_text(encoding='utf8'))
    changed = [n for n in manifest if not (ROOT/n).exists() or digest(ROOT/n) != manifest[n]]
    restored = []
    for name in changed:
        if name in OWNED:
            continue
        # Restore only generated evidence/configuration; never overwrite other code edits.
        assert name.startswith(('docs/codex-reports/evidence/', 'Evidence/', 'Assets/Screenshots/', 'ProjectSettings/')) or name.endswith('SDF.asset') or name in {
            'Assets/Resources/UI/Tactical/TrainingChinese.asset',
            'Assets/XR/Settings/Open XR Package Settings.asset',
        }, 'Unexpected external edit: ' + name
        assert (BACKUP/name).is_file(), 'No entry backup: ' + name
        shutil.copy2(BACKUP/name, ROOT/name)
        restored.append(name)
    removed = []
    for name in set(collect())-set(manifest):
        if name.startswith(('docs/codex-reports/evidence/', 'Evidence/', 'Assets/InitTestScene')):
            target = (ROOT/name).resolve()
            assert target.is_relative_to(ROOT) and not target.is_relative_to(OUT)
            target.unlink()
            removed.append(name)
    external = [n for n in manifest if n not in OWNED and digest(ROOT/n) != manifest[n]]
    assert not external, external
    result = {'all_other_entry_files_identical': True, 'protected_files': len(manifest),
              'restored_generated_artifacts': restored, 'removed_new_generated_artifacts': sorted(removed),
              'task_changed_files': sorted(n for n in OWNED if digest(ROOT/n) != manifest[n])}
    (OUT/'workspace-verification.json').write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf8')
    print(json.dumps(result, ensure_ascii=False, indent=2))
