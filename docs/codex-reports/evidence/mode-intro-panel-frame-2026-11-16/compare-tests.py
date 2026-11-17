"""Exact fullname comparison; preserve baseline differences already present on entry."""
from pathlib import Path
from collections import Counter
import csv
import hashlib
import json
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
BASE = HERE.parent/'mainmenu-heading-border-2026-11-15'
ENTRY = HERE.parent/'mainmenu-heading-restore-2026-11-16'
NEW = 'VRShooting.Tests.PlayMode.UI.Screen02_04_MainMenuZeroingBriefingUITests.Bdd37_FourIntroPanelsPreserveLegacyFrameAndPrimaryEnterStyle'

def read(path):
    result = {}
    for case in ET.parse(path).getroot().findall('.//test-case'):
        name = case.attrib['fullname']
        assert name not in result, name
        result[name] = {'result': case.attrib['result'], 'message': case.findtext('failure/message', ''),
                        'stack': case.findtext('failure/stack-trace', '')}
    return result

def counts(cases):
    return {'total': len(cases), **dict(Counter(c['result'] for c in cases.values()))}

rows, platforms = [], {}
for kind in ('editmode', 'playmode'):
    files = [folder/f'{kind}-full.xml' for folder in (BASE, ENTRY, HERE)]
    baseline, entry, current = [read(p) for p in files]
    old_failures = {n for n,c in baseline.items() if c['result'] == 'Failed'}
    new_failures = {n for n,c in current.items() if c['result'] == 'Failed'}
    expected_added = {NEW} if kind == 'playmode' else set()
    platform = {
        'files': {key: {'path': str(path), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()}
                  for key,path in zip(('baseline','entry','current'), files)},
        'baseline': counts(baseline), 'entry': counts(entry), 'current': counts(current),
        'missing_fullnames': sorted(baseline.keys()-current.keys()),
        'added_fullnames': sorted(current.keys()-baseline.keys()),
        'preexisting_missing_fullnames': sorted(baseline.keys()-entry.keys()),
        'preexisting_added_fullnames': sorted(entry.keys()-baseline.keys()),
        'this_task_added_fullnames': sorted(current.keys()-entry.keys()),
        'this_task_missing_fullnames': sorted(entry.keys()-current.keys()),
        'status_changes_from_baseline': sorted(n for n in baseline.keys() & current.keys() if baseline[n]['result'] != current[n]['result']),
        'status_changes_from_entry': sorted(n for n in entry.keys() & current.keys() if entry[n]['result'] != current[n]['result']),
        'new_failures': sorted(new_failures-old_failures),
        'resolved_failures': sorted(old_failures-new_failures),
        'failure_sets_identical': old_failures == new_failures,
        'failed_fullnames': sorted(new_failures),
        'failure_detail_changes': sorted(n for n in old_failures & new_failures if baseline[n] != current[n]),
    }
    platform['gate_passed'] = (old_failures == new_failures and not platform['status_changes_from_baseline']
        and not platform['status_changes_from_entry'] and not platform['this_task_missing_fullnames']
        and set(platform['this_task_added_fullnames']) == expected_added
        and all(current[n]['result'] == 'Passed' for n in expected_added)
        and platform['missing_fullnames'] == platform['preexisting_missing_fullnames'])
    platforms[kind] = platform
    for name in sorted(baseline.keys() | entry.keys() | current.keys()):
        old, existing, new = baseline.get(name, {}), entry.get(name, {}), current.get(name, {})
        rows.append({'platform': kind, 'fullname': name, 'baseline_result': old.get('result','MISSING'),
                     'entry_result': existing.get('result','MISSING'), 'current_result': new.get('result','MISSING'),
                     'baseline_message': old.get('message',''), 'current_message': new.get('message',''),
                     'baseline_stack': old.get('stack',''), 'current_stack': new.get('stack','')})

with (HERE/'fullname-comparison.csv').open('w', encoding='utf-8-sig', newline='') as handle:
    writer = csv.DictWriter(handle, fieldnames=list(rows[0]))
    writer.writeheader()
    writer.writerows(rows)
result = {'gate_passed': all(p['gate_passed'] for p in platforms.values()), 'compared_fullnames':len(rows), 'platforms':platforms}
(HERE/'baseline-comparison.json').write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf8')
for kind,p in platforms.items():
    print(kind, p['baseline'], '->', p['current'], 'new failures', len(p['new_failures']), 'same failure set', p['failure_sets_identical'], 'gate', p['gate_passed'])
raise SystemExit(0 if result['gate_passed'] else 1)
