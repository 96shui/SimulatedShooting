import csv, json, xml.etree.ElementTree as ET
from pathlib import Path
root = Path(__file__).resolve().parent
rows = []
summary = {}
for platform in ("editmode", "playmode"):
    def read(name):
        tree = ET.parse(root / name)
        cases = {}
        for t in tree.findall(".//test-case"):
            key = t.attrib["fullname"]
            assert key not in cases, key
            cases[key] = {"result": t.attrib["result"], "message": t.findtext("failure/message", ""), "stack": t.findtext("failure/stack-trace", "")}
        return tree.getroot().attrib, cases
    baseline, before = read("baseline-" + platform + ".xml")
    current, after = read(platform + "-full.xml")
    missing = sorted(before.keys() - after.keys())
    added = sorted(after.keys() - before.keys())
    new_failures = sorted(k for k,v in after.items() if v["result"] == "Failed" and before.get(k,{}).get("result") != "Failed")
    changed = sorted(k for k in before.keys() & after.keys() if before[k]["result"] != after[k]["result"])
    summary[platform] = {"baseline": baseline, "current": current, "missing_fullnames": missing, "added_fullnames": added, "new_failures": new_failures, "status_changes": changed}
    for name in sorted(before.keys() | after.keys()):
        b,a = before.get(name,{}),after.get(name,{})
        rows.append({"platform":platform,"fullname":name,"baseline":b.get("result","Absent"),"current":a.get("result","Missing"),"baseline_message":b.get("message",""),"current_message":a.get("message",""),"baseline_stack":b.get("stack",""),"current_stack":a.get("stack","")})
(root / "baseline-comparison.json").write_text(json.dumps({"summary":summary,"cases":rows},ensure_ascii=False,indent=2),encoding="utf-8")
with (root / "fullname-comparison.csv").open("w",encoding="utf-8-sig",newline="") as output:
    writer=csv.DictWriter(output,fieldnames=rows[0].keys());writer.writeheader();writer.writerows(rows)
print(json.dumps(summary,ensure_ascii=False,indent=2))
assert all(not s["missing_fullnames"] and not s["new_failures"] for s in summary.values())
