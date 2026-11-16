"""Compare complete fullnames against the accepted main-menu workspace baseline; no aliases."""
import csv
import hashlib
import json
import xml.etree.ElementTree as ET
from pathlib import Path

folder = Path(__file__).resolve().parent
baseline = folder.parent / 'mainmenu-relayout-2026-11-15'
rows, summary = [], {}
for platform, expected in [('editmode', (391, 371, 20)), ('playmode', (322, 299, 23))]:
    def read(path):
        root = ET.parse(path).getroot()
        cases = {}
        for case in root.findall('.//test-case'):
            name = case.attrib['fullname']
            assert name not in cases, name
            cases[name] = {'result': case.attrib['result'], 'message': case.findtext('failure/message', '')}
        return root.attrib, cases, hashlib.sha256(path.read_bytes()).hexdigest()

    before_attrs, before, before_hash = read(baseline / f'{platform}-full.xml')
    after_attrs, after, after_hash = read(folder / f'{platform}-full.xml')
    assert tuple(int(before_attrs[k]) for k in ('total', 'passed', 'failed')) == expected
    missing = sorted(before.keys() - after.keys())
    added = sorted(after.keys() - before.keys())
    before_failures = sorted(name for name, case in before.items() if case['result'] == 'Failed')
    after_failures = sorted(name for name, case in after.items() if case['result'] == 'Failed')
    changed = []
    for name in sorted(before.keys() | after.keys()):
        b = before.get(name, {'result': 'Absent', 'message': ''})
        a = after.get(name, {'result': 'Missing', 'message': ''})
        rows.append({'platform': platform, 'fullname': name, 'baseline': b['result'], 'current': a['result'],
                     'baseline_message': b['message'], 'current_message': a['message']})
        if name in before and a['result'] != b['result']:
            changed.append(name)
    summary[platform] = {
        'baseline': {k: before_attrs[k] for k in ('total', 'passed', 'failed', 'skipped', 'inconclusive')},
        'current': {k: after_attrs[k] for k in ('total', 'passed', 'failed', 'skipped', 'inconclusive')},
        'baseline_sha256': before_hash, 'current_sha256': after_hash,
        'missing_fullnames': missing, 'added_fullnames': added, 'changed_results': changed,
        'baseline_failed_fullnames': before_failures, 'current_failed_fullnames': after_failures,
        'new_failures': sorted(set(after_failures) - set(before_failures)),
        'added_all_passed': all(after[name]['result'] == 'Passed' for name in added),
        'failure_sets_identical': before_failures == after_failures,
    }

(folder / 'baseline-comparison.json').write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding='utf8')
with (folder / 'fullname-comparison.csv').open('w', encoding='utf-8-sig', newline='') as output:
    writer = csv.DictWriter(output, fieldnames=rows[0].keys()); writer.writeheader(); writer.writerows(rows)
for platform, result in summary.items():
    print(platform, json.dumps({k: v for k, v in result.items() if k not in
        ('baseline_failed_fullnames', 'current_failed_fullnames', 'added_fullnames')}, ensure_ascii=False))
assert all(not s['missing_fullnames'] and not s['changed_results'] and not s['new_failures'] and
           s['failure_sets_identical'] and s['added_all_passed'] and s['current']['skipped'] == '0' and
           s['current']['inconclusive'] == '0' for s in summary.values())
