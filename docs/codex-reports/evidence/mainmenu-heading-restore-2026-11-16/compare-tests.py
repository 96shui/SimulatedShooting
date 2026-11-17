"""Compare exact fullnames and per-case results against the requested untouched baseline."""
from collections import Counter
from pathlib import Path
import csv
import hashlib
import json
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
BASE = HERE.parent/'mode-intro-pages-2026-11-16'

def read(path):
    cases = {}
    for case in ET.parse(path).getroot().findall('.//test-case'):
        name = case.attrib['fullname']
        assert name not in cases, name
        cases[name] = {'result':case.attrib['result'], 'message':case.findtext('failure/message', ''),
                       'stack':case.findtext('failure/stack-trace', '')}
    return cases

platforms, rows = {}, []
for kind, baseline_name, expected in [('editmode','editmode-full.xml',(412,393,19)),
                                      ('playmode','playmode-full-final.xml',(335,314,21))]:
    baseline_file, current_file = BASE/baseline_name, HERE/(kind+'-full.xml')
    baseline, current = read(baseline_file), read(current_file)
    old, new = Counter(c['result'] for c in baseline.values()), Counter(c['result'] for c in current.values())
    assert (len(baseline),old['Passed'],old['Failed']) == expected
    old_failed = {n for n,c in baseline.items() if c['result']=='Failed'}
    new_failed = {n for n,c in current.items() if c['result']=='Failed'}
    record = {'baseline_file':str(baseline_file.relative_to(HERE.parent.parent.parent.parent)),
        'current_file':str(current_file.relative_to(HERE.parent.parent.parent.parent)),
        'baseline_sha256':hashlib.sha256(baseline_file.read_bytes()).hexdigest(),
        'current_sha256':hashlib.sha256(current_file.read_bytes()).hexdigest(),
        'baseline':{'total':len(baseline),**dict(old)}, 'current':{'total':len(current),**dict(new)},
        'missing_fullnames':sorted(baseline.keys()-current.keys()),
        'added_fullnames':sorted(current.keys()-baseline.keys()),
        'status_changes':sorted(n for n in baseline.keys() & current.keys() if baseline[n]['result'] != current[n]['result']),
        'new_failures':sorted(new_failed-old_failed), 'resolved_failures':sorted(old_failed-new_failed),
        'failure_sets_identical':old_failed == new_failed,
        'failed_fullnames':sorted(new_failed),
        'failure_detail_changes':sorted(n for n in old_failed & new_failed if baseline[n] != current[n])}
    record['strict_gate_passed'] = not any(record[k] for k in ('missing_fullnames','added_fullnames','status_changes','new_failures'))
    platforms[kind] = record
    for name in sorted(baseline.keys() | current.keys()):
        a,b = baseline.get(name,{}), current.get(name,{})
        rows.append({'platform':kind, 'fullname':name,
            'baseline_result':a.get('result','MISSING'), 'current_result':b.get('result','MISSING'),
            'baseline_message':a.get('message',''), 'current_message':b.get('message',''),
            'baseline_stack':a.get('stack',''), 'current_stack':b.get('stack','')})

with (HERE/'fullname-comparison.csv').open('w',encoding='utf-8-sig',newline='') as stream:
    writer=csv.DictWriter(stream,fieldnames=list(rows[0]))
    writer.writeheader()
    writer.writerows(rows)
result={'strict_gate_passed':all(p['strict_gate_passed'] for p in platforms.values()),
        'compared_fullnames':len(rows), 'platforms':platforms}
(HERE/'baseline-comparison.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
for kind,p in platforms.items():
    print(kind,p['baseline'],'->',p['current'],'new failures:',len(p['new_failures']),
          'missing:',len(p['missing_fullnames']),'status changes:',len(p['status_changes']),
          'identical failure sets:',p['failure_sets_identical'])
raise SystemExit(0 if result['strict_gate_passed'] else 1)
