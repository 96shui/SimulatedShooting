"""Compare only the executed targeted cases against round6's complete baseline by fullname."""
import csv
import json
from pathlib import Path
import xml.etree.ElementTree as ET

folder=Path(__file__).resolve().parent
rows=[]
summary={}
for platform in ('editmode','playmode'):
    def read(path):
        tree=ET.parse(path)
        cases={t.attrib['fullname']:dict(result=t.attrib['result'],message=t.findtext('failure/message',''),stack=t.findtext('failure/stack-trace','')) for t in tree.findall('.//test-case')}
        return tree.getroot().attrib,cases
    baseline,before=read(folder.parent/(platform+'-full.xml'))
    current,after=read(folder/(platform+'-targeted.xml'))
    for name,test in sorted(after.items()):
        old=before.get(name,{})
        rows.append(dict(platform=platform,fullname=name,baseline=old.get('result','Absent'),current=test['result'],failure_message_identical=old.get('message','')==test['message'],baseline_message=old.get('message',''),current_message=test['message']))
    summary[platform]=dict(baseline={k:baseline[k] for k in ('total','passed','failed','skipped')},targeted={k:current[k] for k in ('total','passed','failed','skipped')},unrecognized_cases=sorted(after.keys()-before.keys()),status_changes=[n for n in after if n in before and after[n]['result']!=before[n]['result']],new_failures=[n for n in after if after[n]['result']=='Failed' and before.get(n,{}).get('result')!='Failed'])
output=dict(summary=summary,cases=rows)
(folder/'targeted-baseline-comparison.json').write_text(json.dumps(output,ensure_ascii=False,indent=2),encoding='utf-8')
with (folder/'targeted-fullname-comparison.csv').open('w',encoding='utf-8-sig',newline='') as stream:
    writer=csv.DictWriter(stream,fieldnames=rows[0].keys());writer.writeheader();writer.writerows(rows)
print(json.dumps(summary,ensure_ascii=False,indent=2))
assert all(not s['new_failures'] and not s['status_changes'] and not s['unrecognized_cases'] for s in summary.values())
