from pathlib import Path
import hashlib
import json
import shutil
import subprocess
import sys
import tempfile

ROOT = Path.cwd().resolve()
OUT = ROOT / 'docs/codex-reports/evidence/mainmenu-heading-font-2026-11-17'
STATE = OUT / 'workspace-snapshot.json'
OWNED = {
    'Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs',
    'Assets/VRShooting/Tests/EditMode/Application/MainMenuControlsTests.cs',
    'Assets/VRShooting/Tests/PlayMode/UI/Screen02_04_MainMenuZeroingBriefingUITests.cs',
    'docs/BDD/screens/02-游戏主界面.feature.md',
    'docs/任务规格/阶段一-P1-100m射校原型闭环/UI/task008-ui-mainmenu-zeroing-briefing.md',
    'docs/接口文档/01-页面导航与UI事件.md',
}

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def git_names(*args):
    return {n for n in subprocess.check_output(['git', *args, '-z']).decode('utf8').split('\0') if n}

if sys.argv[1] == 'snapshot':
    assert not STATE.exists(), 'Never overwrite the entry snapshot'
    backup = Path(tempfile.mkdtemp(prefix='mainmenu-heading-restore-'))
    tracked = git_names('ls-files')
    names = {n for n in tracked if (ROOT/n).is_file() and (
        (ROOT/n).suffix.lower() in {'.cs', '.meta', '.asset', '.unity', '.prefab', '.md', '.json', '.asmdef'}
        or n.startswith(('docs/', 'Evidence/', 'ProjectSettings/', 'Packages/', 'Assets/Screenshots/')))}
    for folder in ('docs', 'Evidence', 'Assets/Screenshots'):
        names.update(p.relative_to(ROOT).as_posix() for p in (ROOT/folder).rglob('*')
                     if p.is_file() and not p.is_relative_to(OUT))
    names.update(n for n in git_names('ls-files', '--others', '--exclude-standard')
                 if n.startswith('Assets/') and (ROOT/n).is_file())
    manifest = {}
    for name in sorted(names):
        dest = backup/name
        dest.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(ROOT/name, dest)
        manifest[name] = sha(dest)
    (backup/'manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf8')
    (OUT/'initial-git-status.txt').write_bytes(subprocess.check_output(['git', '-c', 'core.quotepath=false', 'status', '--short']))
    STATE.write_text(json.dumps({'backup':str(backup), 'files':len(manifest),
        'head':subprocess.check_output(['git','rev-parse','HEAD']).decode().strip()}, indent=2), encoding='utf8')
    print(f'Protected {len(manifest)} files / {sum((backup/n).stat().st_size for n in manifest):,} bytes at {backup}')
else:
    assert sys.argv[1] == 'restore'
    procs = subprocess.check_output(['tasklist', '/FI', 'IMAGENAME eq Unity.exe', '/FO', 'CSV']).decode(errors='replace')
    assert '"Unity.exe"' not in procs, 'Unity must exit before restoration'
    backup = Path(json.loads(STATE.read_text(encoding='utf8'))['backup'])
    manifest = json.loads((backup/'manifest.json').read_text(encoding='utf8'))
    restored = []
    for name, digest in manifest.items():
        if name in OWNED:
            continue
        dest = ROOT/name
        if not dest.exists() or sha(dest) != digest:
            dest.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(backup/name, dest)
            restored.append(name)
    removed = []
    tracked = git_names('ls-files')
    candidates = list((ROOT/'docs/codex-reports/evidence').rglob('*')) + list((ROOT/'Assets').glob('InitTestScene*'))
    for p in candidates:
        if not p.is_file() or p.is_relative_to(OUT):
            continue
        name = p.relative_to(ROOT).as_posix()
        if name not in manifest and name not in tracked:
            assert p.resolve().is_relative_to(ROOT), name
            p.unlink()
            removed.append(name)
    protected = {n:d for n,d in manifest.items() if n not in OWNED}
    assert all((ROOT/n).is_file() and sha(ROOT/n) == d for n,d in protected.items())
    report = {'restored':restored, 'removed_new_batch_files':removed,
        'protected_file_count':len(protected), 'all_protected_files_identical':True,
        'preserved_init_test_scenes':[n for n in protected if n.startswith('Assets/InitTestScene')],
        'task_changed_files':[n for n in sorted(OWNED) if sha(ROOT/n) != manifest[n]]}
    (OUT/'cleanup.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf8')
    print(json.dumps(report, ensure_ascii=False, indent=2))
