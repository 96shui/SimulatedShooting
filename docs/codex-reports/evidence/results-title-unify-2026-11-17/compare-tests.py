"""Compare exact NUnit fullnames; never normalize names or exempt failures."""
from pathlib import Path
from collections import Counter
import csv
import hashlib
import json
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
BASE = HERE.parent / 'mainmenu-heading-font-2026-11-17'


def read(path):
    cases = {}
    for c in ET.parse(path).getroot().findall('.//test-case'):
        name = c.attrib['fullname']
        assert name not in cases, name
        cases[name] = dict(result=c.attrib['result'],
                           message=c.findtext('failure/message', ''),
                           stack=c.findtext('failure/stack-trace', ''))
    return cases


platforms, rows = {}, []
for kind in ('editmode', 'playmode'):
    old_path, new_path = BASE / (kind + '-full.xml'), HERE / (kind + '-full.xml')
    old, new = read(old_path), read(new_path)
    old_fail = {n for n, c in old.items() if c['result'] == 'Failed'}
    new_fail = {n for n, c in new.items() if c['result'] == 'Failed'}
    changes = sorted(n for n in old.keys() & new.keys() if old[n]['result'] != new[n]['result'])
    missing, added = sorted(old.keys() - new.keys()), sorted(new.keys() - old.keys())
    p = dict(baseline_file=str(old_path), current_file=str(new_path),
             baseline_sha256=hashlib.sha256(old_path.read_bytes()).hexdigest(),
             current_sha256=hashlib.sha256(new_path.read_bytes()).hexdigest(),
             baseline=dict(total=len(old), **Counter(c['result'] for c in old.values())),
             current=dict(total=len(new), **Counter(c['result'] for c in new.values())),
             missing_fullnames=missing, added_fullnames=added, status_changes=changes,
             new_failures=sorted(new_fail - old_fail), resolved_failures=sorted(old_fail - new_fail),
             failure_sets_identical=old_fail == new_fail, failed_fullnames=sorted(new_fail),
             failure_detail_changes={n: dict(baseline=old[n], current=new[n])
                                     for n in sorted(old_fail & new_fail) if old[n] != new[n]})
    p['gate_passed'] = not missing and not added and not changes and old_fail == new_fail
    platforms[kind] = p
    for name in sorted(old.keys() | new.keys()):
        a, b = old.get(name, {}), new.get(name, {})
        rows.append(dict(platform=kind, fullname=name,
                         baseline_result=a.get('result', 'MISSING'), current_result=b.get('result', 'MISSING'),
                         baseline_message=a.get('message', ''), current_message=b.get('message', ''),
                         baseline_stack=a.get('stack', ''), current_stack=b.get('stack', '')))

with (HERE / 'fullname-comparison.csv').open('w', encoding='utf-8-sig', newline='') as f:
    writer = csv.DictWriter(f, fieldnames=list(rows[0]))
    writer.writeheader()
    writer.writerows(rows)
result = dict(gate_passed=all(p['gate_passed'] for p in platforms.values()),
              rows_compared=len(rows), platforms=platforms)
(HERE / 'baseline-comparison.json').write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf8')
for kind, p in platforms.items():
    print(kind, p['baseline'], '->', p['current'], 'new failures:', len(p['new_failures']),
          'missing/added/status changes:', len(p['missing_fullnames']), len(p['added_fullnames']), len(p['status_changes']),
          'failure sets identical:', p['failure_sets_identical'], 'gate:', p['gate_passed'])
raise SystemExit(0 if result['gate_passed'] else 1)
