from pathlib import Path
import hashlib
import json
import re
import subprocess

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[3]
state=json.loads((HERE/'workspace-snapshot.json').read_text(encoding='utf8'))
backup=Path(state['backup'])
manifest=json.loads((backup/'manifest.json').read_text(encoding='utf8'))
cleanup=json.loads((HERE/'cleanup.json').read_text(encoding='utf8'))
owned=set(cleanup['task_changed_files'])
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
protected=[n for n in manifest if n not in owned]
assert all((ROOT/n).is_file() and sha(ROOT/n)==manifest[n] for n in protected)
head=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT).decode().strip()
assert head==state['head']
assert not Path('E:/SimulateShooting-heading-font-9a1c012').exists()
processes=subprocess.check_output(['tasklist','/FI','IMAGENAME eq Unity.exe','/FO','CSV']).decode(errors='replace')
assert '"Unity.exe"' not in processes
assert not (ROOT/'Assets/VRShooting/Editor/HeadingFontEvidence.cs').exists()
comparison=json.loads((HERE/'baseline-comparison.json').read_text(encoding='utf8'))
assert comparison['failure_gate_passed'] and comparison['inventory_gate_passed']
pixels=json.loads((HERE/'pixel-verification.json').read_text(encoding='utf8'))
assert pixels['baseline_vs_final_isolated_center_aligned_390x70']['changed_pixels']==0
assert pixels['mainmenu_outside_heading_changed_pixels']==0
production='Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs'
before=(backup/production).read_text(encoding='utf-8-sig')
after=(ROOT/production).read_text(encoding='utf-8-sig')
start=before.index('            // Preserve the menu heading\'s original text style after the shared normalization.')
end=before.index('            // XRI sends the standard enter/exit events.',start)
assert before[:start]+before[end:]==after, 'Production diff must only remove the title override'
archives={}
for filename in ('MainMenuUI.cs','TacticalUIStyle.cs'):
    name='Assets/VRShooting/Runtime/Unity/UI/'+filename
    archived=(HERE/'baseline-9a1c012'/str(filename+'.txt')).read_text(encoding='utf-8-sig')
    git_source=subprocess.check_output(['git','show','9a1c012:'+name],cwd=ROOT).decode('utf8').replace('\r\n','\n')
    assert archived==git_source
    archives[name]={'matches_commit_source':True,'archive_sha256':sha(HERE/'baseline-9a1c012'/str(filename+'.txt'))}
inventory={}
for name in sorted(owned):
    item={'entry_sha256':sha(backup/name),'final_sha256':sha(ROOT/name)}
    if name.endswith('Tests.cs'):
        text=(backup/name).read_text(encoding='utf-8-sig')
        item['entry_testcase_attributes']=re.findall(r'^\s*\[TestCase\(.*$',text,re.M)
        item['entry_public_test_methods']=re.findall(r'public (?:void|IEnumerator) (\w+)\(',text)
    inventory[name]=item
(HERE/'entry-source-inventory.json').write_text(json.dumps(inventory,ensure_ascii=False,indent=2),encoding='utf8')
status=subprocess.check_output(['git','-c','core.quotepath=false','status','--short'],cwd=ROOT)
(HERE/'final-git-status.txt').write_bytes(status)
result={'head':head,'branch':subprocess.check_output(['git','branch','--show-current'],cwd=ROOT).decode().strip(),
    'unity_process_count':0,'worktree_removed':True,'temporary_editor_helper_removed':True,
    'production_change_exactly_title_override_removal':True,'baseline_sources':archives,
    'protected_file_count':len(protected),'all_protected_files_identical':True,
    'preserved_tracked_init_test_scenes':cleanup['preserved_init_test_scenes'],
    'changed_authorized_files':sorted(owned),'failure_gate_passed':True,'inventory_gate_passed':True,
    'baseline_font_isolated_rgb_changed_pixels':0,'outside_heading_changed_pixels':0,
    'report_exists':(ROOT/'docs/codex-reports/mainmenu-heading-font-2026-11-17.md').is_file()}
assert result['report_exists']
(HERE/'final-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
print(json.dumps({k:v for k,v in result.items() if k not in ('baseline_sources','preserved_tracked_init_test_scenes')},ensure_ascii=False,indent=2))
