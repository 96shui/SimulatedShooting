"""Compare every exact NUnit fullname; no aliases, exclusions or baseline rewrites."""
from pathlib import Path
from collections import Counter
import csv
import hashlib
import json
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
BASE = HERE.parent / 'mainmenu-heading-border-2026-11-15'
EXPECTED = {'editmode': (403, 384, 19), 'playmode': (333, 312, 21)}

def read(path):
    cases = ET.parse(path).getroot().findall('.//test-case')
    result = {}
    for case in cases:
        name = case.attrib['fullname']
        assert name not in result, f'Duplicate fullname: {name}'
        result[name] = {'result': case.attrib['result'],
                        'message': case.findtext('failure/message', ''),
                        'stack': case.findtext('failure/stack-trace', '')}
    return result

rows, platforms = [], {}
for kind, expected in EXPECTED.items():
    baseline_file = BASE/f'{kind}-full.xml'
    current_file = HERE/('playmode-full-final.xml' if kind == 'playmode' else 'editmode-full.xml')
    baseline, current = read(baseline_file), read(current_file)
    old_counts, new_counts = Counter(c['result'] for c in baseline.values()), Counter(c['result'] for c in current.values())
    assert (len(baseline), old_counts['Passed'], old_counts['Failed']) == expected
    old_failures = {n for n, c in baseline.items() if c['result'] == 'Failed'}
    new_failures = {n for n, c in current.items() if c['result'] == 'Failed'}
    changed = [n for n in baseline.keys() & current.keys() if baseline[n]['result'] != current[n]['result']]
    platform = {'baseline_file': str(baseline_file), 'current_file': str(current_file),
        'baseline_sha256': hashlib.sha256(baseline_file.read_bytes()).hexdigest(),
        'current_sha256': hashlib.sha256(current_file.read_bytes()).hexdigest(),
        'baseline': {'total': len(baseline), **dict(old_counts)},
        'current': {'total': len(current), **dict(new_counts)},
        'missing_fullnames': sorted(baseline.keys()-current.keys()),
        'added_fullnames': sorted(current.keys()-baseline.keys()),
        'status_changes': sorted(changed),
        'new_failures': sorted(new_failures-old_failures),
        'resolved_failures': sorted(old_failures-new_failures),
        'failure_sets_identical': old_failures == new_failures,
        'failed_fullnames': sorted(new_failures),
        'failure_detail_changes': [n for n in sorted(old_failures & new_failures) if baseline[n] != current[n]]}
    # Exact fullnames are compared without aliases. Parameterized coordinate cases have four
    # intentional replacements, recorded explicitly; all other original coverage must remain.
    replacements = {}
    if kind == 'editmode':
        prefix = 'VRShooting.Tests.EditMode.Application.MainMenuControlsTests.Bdd02_TrainingButtonGeometryIsUnchanged'
        for entry, old_y, new_y in [('OpenZeroing',(539,624),(665,750)), ('OpenMovingTarget',(411,496),(535,620)),
                                    ('Trench',(283,368),(405,490)), ('Urban',(155,240),(275,360))]:
            old = prefix + '("Button_MainMenu_' + entry + '",' + str(old_y[0]) + '.0f,' + str(old_y[1]) + '.0f)'
            new = prefix + '("Button_MainMenu_' + entry + '",' + str(new_y[0]) + '.0f,' + str(new_y[1]) + '.0f)'
            assert baseline[old]['result'] == 'Passed'
            assert current[new]['result'] == 'Passed'
            replacements[old] = new
    platform['coordinate_contract_replacements'] = replacements
    platform['all_original_coverage_present_or_replaced_by_exact_new_coordinates'] = set(platform['missing_fullnames']) == set(replacements)
    platform['strict_gate_passed'] = (not platform['new_failures'] and
        platform['all_original_coverage_present_or_replaced_by_exact_new_coordinates'])
    platforms[kind] = platform
    for name in sorted(baseline.keys() | current.keys()):
        old, new = baseline.get(name, {}), current.get(name, {})
        rows.append({'platform': kind, 'fullname': name, 'baseline_result': old.get('result', 'MISSING'),
            'current_result': new.get('result', 'MISSING'),
            'baseline_message': old.get('message', ''), 'current_message': new.get('message', ''),
            'baseline_stack': old.get('stack', ''), 'current_stack': new.get('stack', '')})

with (HERE/'fullname-comparison.csv').open('w', encoding='utf-8-sig', newline='') as f:
    writer = csv.DictWriter(f, fieldnames=list(rows[0]))
    writer.writeheader()
    writer.writerows(rows)
result = {'strict_gate_passed': all(p['strict_gate_passed'] for p in platforms.values()),
          'compared_fullnames': len(rows), 'platforms': platforms}
(HERE/'baseline-comparison.json').write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf8')
for kind, platform in platforms.items():
    print(kind, platform['baseline'], '->', platform['current'], 'new failures:', len(platform['new_failures']),
          'identical failure sets:', platform['failure_sets_identical'], 'strict gate:', platform['strict_gate_passed'])
raise SystemExit(0 if result['strict_gate_passed'] else 1)
