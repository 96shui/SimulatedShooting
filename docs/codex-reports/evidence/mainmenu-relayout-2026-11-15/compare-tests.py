import csv
import hashlib
import json
import xml.etree.ElementTree as ET
from pathlib import Path

root = Path(__file__).resolve().parent
prefix = 'VRShooting.Tests.EditMode.Application.MainMenuControlsTests.Bdd02_TrainingButtonGeometryIsUnchanged'
revisions = [('Button_MainMenu_OpenZeroing',665,750,539,624),
             ('Button_MainMenu_OpenMovingTarget',535,620,411,496),
             ('Button_MainMenu_Trench',405,490,283,368),
             ('Button_MainMenu_Urban',275,360,155,240)]
aliases = {f'{prefix}("{name}",{b}.0f,{t}.0f)': f'{prefix}("{name}",{nb}.0f,{nt}.0f)'
           for name,b,t,nb,nt in revisions}
rows, summary = [], {}
for platform in ('editmode','playmode'):
    def read(name):
        path = root / name
        tree = ET.parse(path)
        cases = {}
        for case in tree.findall('.//test-case'):
            name = case.attrib['fullname']
            assert name not in cases, name
            cases[name] = {'result':case.attrib['result'], 'message':case.findtext('failure/message',''),
                           'stack':case.findtext('failure/stack-trace','')}
        return tree.getroot().attrib, cases, hashlib.sha256(path.read_bytes()).hexdigest()
    ba, before, bhash = read(f'baseline-{platform}.xml')
    ca, after, chash = read(f'{platform}-full.xml')
    mapped = {name: aliases.get(name,name) if platform == 'editmode' else name for name in before}
    missing = [name for name,new in mapped.items() if new not in after]
    added = sorted(after.keys() - mapped.values())
    changes, failures = [], []
    for old,new in mapped.items():
        b,a = before[old],after.get(new,{'result':'Missing','message':'','stack':''})
        if b['result'] != a['result']: changes.append({'baseline_fullname':old,'current_fullname':new,'before':b['result'],'after':a['result']})
        if a['result'] == 'Failed' and b['result'] != 'Failed': failures.append(new)
        rows.append({'platform':platform,'baseline_fullname':old,'current_fullname':new,'baseline':b['result'],'current':a['result'],
                     'intentional_parameter_revision':old!=new,'failure_message_identical':b['message']==a['message'],
                     'baseline_message':b['message'],'current_message':a['message'],'baseline_stack':b['stack'],'current_stack':a['stack']})
    for new in added:
        a = after[new]
        if a['result'] == 'Failed': failures.append(new)
        rows.append({'platform':platform,'baseline_fullname':'','current_fullname':new,'baseline':'Absent','current':a['result'],
                     'intentional_parameter_revision':False,'failure_message_identical':False,'baseline_message':'','current_message':a['message'],
                     'baseline_stack':'','current_stack':a['stack']})
    summary[platform] = {'baseline':ba,'current':ca,'baseline_sha256':bhash,'current_sha256':chash,
        'intentional_parameter_revisions':{old:new for old,new in mapped.items() if old!=new},
        'missing_cases':missing,'added_cases':added,'status_changes':changes,'new_failures':failures,
        'failure_message_changes':[r['current_fullname'] for r in rows if r['platform']==platform and r['baseline']=='Failed' and not r['failure_message_identical']]}
(root/'baseline-comparison.json').write_text(json.dumps({'summary':summary,'cases':rows},ensure_ascii=False,indent=2),encoding='utf-8')
with (root/'fullname-comparison.csv').open('w',encoding='utf-8-sig',newline='') as f:
    writer=csv.DictWriter(f,fieldnames=rows[0].keys()); writer.writeheader(); writer.writerows(rows)
print(json.dumps(summary,ensure_ascii=False,indent=2))
assert all(not s['missing_cases'] and not s['new_failures'] and not s['status_changes'] for s in summary.values())
assert all(s['current']['skipped']=='0' and s['current']['inconclusive']=='0' for s in summary.values())
