"""Compare exact NUnit fullnames against the user-specified Nov 15 XMLs.

No aliases/normalization: four pre-existing geometry TestCase name changes are
reported literally as missing/added, and independently checked against the
Nov 16 inventory already present at entry. No failures may differ.
"""
from pathlib import Path
from collections import Counter
import csv
import hashlib
import json
import xml.etree.ElementTree as ET

HERE=Path(__file__).resolve().parent
BASE=HERE.parent/'mainmenu-heading-border-2026-11-15'
ENTRY=HERE.parent/'mode-intro-panel-frame-2026-11-16'

def read(path):
    result={}
    for c in ET.parse(path).getroot().findall('.//test-case'):
        name=c.attrib['fullname']
        assert name not in result,name
        result[name]={'result':c.attrib['result'],'message':c.findtext('failure/message',''),
                      'stack':c.findtext('failure/stack-trace','')}
    return result
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()

platforms={}; rows=[]
for kind in ('editmode','playmode'):
    baseline_file=BASE/(kind+'-full.xml')
    entry_file=ENTRY/(kind+'-full.xml')
    current_file=HERE/(kind+'-full.xml')
    baseline,entry,current=map(read,(baseline_file,entry_file,current_file))
    old_fail={n for n,c in baseline.items() if c['result']=='Failed'}
    new_fail={n for n,c in current.items() if c['result']=='Failed'}
    missing=baseline.keys()-current.keys(); added=current.keys()-baseline.keys()
    entry_missing=baseline.keys()-entry.keys(); entry_added=entry.keys()-baseline.keys()
    changes=[n for n in baseline.keys()&current.keys() if baseline[n]['result']!=current[n]['result']]
    entry_changes=[n for n in entry.keys()&current.keys() if entry[n]['result']!=current[n]['result']]
    p={'baseline_file':str(baseline_file),'baseline_sha256':sha(baseline_file),
       'entry_inventory_file':str(entry_file),'entry_inventory_sha256':sha(entry_file),
       'current_file':str(current_file),'current_sha256':sha(current_file),
       'baseline':{'total':len(baseline),**Counter(c['result'] for c in baseline.values())},
       'current':{'total':len(current),**Counter(c['result'] for c in current.values())},
       'missing_fullnames':sorted(missing),'added_fullnames':sorted(added),
       'preexisting_missing_fullnames':sorted(entry_missing),'preexisting_added_fullnames':sorted(entry_added),
       'status_changes':sorted(changes),'new_failures':sorted(new_fail-old_fail),
       'resolved_failures':sorted(old_fail-new_fail),'failure_sets_identical':old_fail==new_fail,
       'failed_fullnames':sorted(new_fail),
       'failure_detail_changes':{n:{'baseline':baseline[n],'current':current[n]}
          for n in sorted(old_fail&new_fail) if baseline[n]!=current[n]},
       'current_vs_entry_missing':sorted(entry.keys()-current.keys()),
       'current_vs_entry_added':sorted(current.keys()-entry.keys()),
       'current_vs_entry_status_changes':sorted(entry_changes)}
    p['failure_gate_passed']=old_fail==new_fail and not changes
    p['inventory_gate_passed']=(missing==entry_missing and added==entry_added and
                              entry.keys()==current.keys() and not entry_changes)
    platforms[kind]=p
    for name in sorted(baseline.keys()|current.keys()):
        old,new=baseline.get(name,{}),current.get(name,{})
        rows.append({'platform':kind,'fullname':name,'baseline_result':old.get('result','MISSING'),
          'entry_inventory_result':entry.get(name,{}).get('result','MISSING'),
          'current_result':new.get('result','MISSING'),
          'baseline_message':old.get('message',''),'current_message':new.get('message',''),
          'baseline_stack':old.get('stack',''),'current_stack':new.get('stack','')})

with (HERE/'fullname-comparison.csv').open('w',encoding='utf-8-sig',newline='') as f:
    w=csv.DictWriter(f,fieldnames=list(rows[0]));w.writeheader();w.writerows(rows)
result={'failure_gate_passed':all(p['failure_gate_passed'] for p in platforms.values()),
        'inventory_gate_passed':all(p['inventory_gate_passed'] for p in platforms.values()),
        'union_rows_compared':len(rows),'platforms':platforms}
(HERE/'baseline-comparison.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
for k,p in platforms.items():
    print(k,p['baseline'],'->',p['current'],'new failures:',len(p['new_failures']),
          'exact failure sets identical:',p['failure_sets_identical'],
          'pre-existing missing/added:',len(p['missing_fullnames']),len(p['added_fullnames']),
          'failure gate:',p['failure_gate_passed'],'inventory gate:',p['inventory_gate_passed'])
raise SystemExit(0 if result['failure_gate_passed'] and result['inventory_gate_passed'] else 1)
