from pathlib import Path
import difflib
import hashlib
import json
import xml.etree.ElementTree as ET
from PIL import Image

ROOT = Path.cwd()
HERE = Path(__file__).resolve().parent
BACKUP = Path('C:/Users/10532/AppData/Local/Temp/mainmenu-heading-border-2026-11-15-backup')
comparison = json.loads((HERE/'baseline-comparison.json').read_text(encoding='utf8'))
cleanup = json.loads((HERE/'cleanup.json').read_text(encoding='utf8'))
assert comparison['strict_gate_passed']
assert cleanup['all_protected_files_identical']

changes = []
for name in cleanup['task_changed_files']:
    changes.extend(difflib.unified_diff((BACKUP/name).read_text(encoding='utf8').splitlines(True),
        (ROOT/name).read_text(encoding='utf8').splitlines(True), fromfile='entry/'+name, tofile='final/'+name))
(HERE/'task-changes.diff').write_text(''.join(changes), encoding='utf8')

stats, focused = [], []
for kind, p in comparison['platforms'].items():
    b, c = p['baseline'], p['current']
    stats.append(f"| {kind} | {b['total']} / {b['Passed']} / {b['Failed']} | {c['total']} / {c['Passed']} / {c['Failed']} | 0 | 0 | 相同 |")
    for suite in ET.parse(HERE/f'{kind}-full.xml').getroot().findall('.//test-suite'):
        if suite.attrib.get('type') == 'TestFixture' and suite.attrib.get('name') in (
                'MainMenuControlsTests', 'Screen02_04_MainMenuZeroingBriefingUITests'):
            focused.append(f"- `{suite.attrib['fullname']}`：{suite.attrib['total']} 条，{suite.attrib['passed']} 通过、{suite.attrib['failed']} 失败（来自最终全量 XML）。")
failures = []
failure_details = []
for kind, p in comparison['platforms'].items():
    for name in p['failed_fullnames']:
        failures.append(f'| {kind} | `{name}` | Failed → Failed |')
    baseline_cases = {t.attrib['fullname']: t for t in ET.parse(p['baseline_file']).getroot().findall('.//test-case')}
    current_cases = {t.attrib['fullname']: t for t in ET.parse(p['current_file']).getroot().findall('.//test-case')}
    for name in p['failure_detail_changes']:
        old, new = baseline_cases[name], current_cases[name]
        old_message = old.findtext('failure/message', '').strip().replace('\n', '<br>')
        new_message = new.findtext('failure/message', '').strip().replace('\n', '<br>')
        same_stack = old.findtext('failure/stack-trace') == new.findtext('failure/stack-trace')
        failure_details.append(f'| `{name}` | {old_message} | {new_message} | {"相同" if same_stack else "不同，见CSV"} |')

images = []
for name, expected in [('Screen_MainMenu.png', (1920, 1080)), ('Screen_MainMenu_Closeup.png', (720, 1080))]:
    with Image.open(HERE/name) as im:
        assert im.size == expected
    images.append({'name': name, 'size': list(expected), 'sha256': hashlib.sha256((HERE/name).read_bytes()).hexdigest()})
(HERE/'screenshot-manifest.json').write_text(json.dumps(images, ensure_ascii=False, indent=2), encoding='utf8')

prefix = 'evidence/mainmenu-heading-border-2026-11-15/'
text = f'''# 主菜单标题条：深色面板 + 橙色边框（2026-11-15）

已按 P1/UI task008、BDD02 和接口01完成标题条视觉修订。“选择训练科目”恢复深色填充和原来的20号浅色普通字，边框为橙色；操作介绍 → 标题 → 01→04 的坐标与顺序保持开工时状态。两套全量回归严格门禁通过，736 个完整 fullname 均保留，40 个失败与指定基线逐条同名，新增失败为0。

## 本轮实际改动

本报告按开工工作区快照比较；此前重排和敌人视野改动均已经存在，未将它们算成本轮改动。精确补丁见 [task-changes.diff]({prefix}task-changes.diff)。

| 文件 | 本轮变化 |
|---|---|
| `Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs` | 删除绿色 Image_MainMenu_MenuFill；AddPanel 填充恢复(17,29,24,230)、描边(247,185,85,255)。保留 ResolvePanelSprite 的 hud_capsule，标题保留显式填充/描边；同文件内添加无贴图的细线描边 Graphic，避免青色贴图与橙色 Outline 相乘导致偏黄绿。未引入或修改贴图。 |
| `Assets/VRShooting/Runtime/Unity/UI/TacticalUIStyle.cs` | 删除标题套 primary 贴图及 Outline 的专用块，删除文字28号/Bold/深字覆盖；通用面板重设流程跳过此标题，以保留 AddPanel 的填充/描边。OpenZeroing 仍不在 primary 名单内。 |
| `Assets/VRShooting/Tests/EditMode/Application/MainMenuControlsTests.cs` | 只修订原有标题视觉测试中的样式断言；保留方法名、所有 TestCase 参数及其他断言。 |
| `docs/BDD/screens/02-游戏主界面.feature.md` | 将高亮标题描述同步为深色面板 + 橙色边框、20号浅色普通字，保留全部布局与非交互约束。 |
| `docs/接口文档/01-页面导航与UI事件.md` | 同步标题视觉契约。 |
| `docs/任务规格/阶段一-P1-100m射校原型闭环/UI/task008-ui-mainmenu-zeroing-briefing.md` | 同步本任务视觉交付要求。 |

另新增本报告和 `{prefix}` 下的运行/截图/保护/比较脚本及证据。PlayMode 测试源码与开工时逐字节相同；没有修改任何玩法实现、场景、Prefab、字体或XR配置作为交付。

## 样式断言逐项新旧对照

完整 fullname 保持 `VRShooting.Tests.EditMode.Application.MainMenuControlsTests.Bdd02_HeadingUsesPrimaryVisualWithoutInteractionAndEntriesUseSecondaryVisual`。方法名含 PrimaryVisual 是历史名称，仅为保持基线完整 fullname 而保留；实际断言已明确约束深色面板与橙色边框。

| 断言 | 开工时旧值 | 本轮新值 |
|---|---|---|
| 标题 Image.sprite.name | `button_primary_orange` | 严格等于 `hud_capsule` |
| 标题 Image.color | `Color.white` | 严格等于 `Color32(17,29,24,230)` |
| 额外填充内芯 | 必须存在，颜色(200,255,106,235)，且位于文字之前 | `Image_MainMenu_MenuFill` 必须不存在；无内芯后不再检查其兄弟顺序 |
| Outline | 存在、enabled、颜色(247,185,85,255) | 全部保留，并新增 effectDistance 严格等于(2,-2) |
| 实际可见橙色轮廓 | 未独立检查，依赖贴图乘色 | 必须存在 MainMenuHeadingBorderGraphic 与 CanvasRenderer，颜色严格等于(247,185,85,255) |
| 字号 | 范围26–28 | 严格等于20 |
| 字重/字形 | 含 Bold | 严格等于 FontStyles.Normal，并检查 FontWeight.Regular |
| 文字颜色 | (7,16,13,255) | 严格等于(231,242,235,255) |

`Image.Type.Sliced`、“选择训练科目”文字、无 Selectable/按钮、所有 Graphic.raycastTarget=false 的断言保留。01/02/03/04与操作介绍的次级贴图、颜色、悬停贴图和文字颜色一致性断言保留。没有删除、跳过、忽略或改名任何测试，也没有修改非样式断言或放宽容差。

原字号和颜色以 HEAD 的 AddPanel→AddLabel 为准：20号、(231,242,235,255)。AddLabel 原有流程去除合成 Bold 并设 FontWeight.Regular，TacticalUIStyle 同样去除 Bold；本轮撤回文字特例后恢复这一结果。

## 保留的布局与交互

1920×1080设计坐标、y向上，六行x均为1360–1750：操作介绍780–865；标题667–737；01 539–624；02 411–496；03 283–368；04 155–240。六行间隙43，01–04中心间距128，Frame与底部状态条未变。标题只展示，不挂按钮行为；原有路由、CombatScenesAvailable门禁及操作介绍内容未改。

## 全量回归与严格基线比较

使用 Unity 2022.3.62f3c1。直接读取用户指定的最近全量原始XML `round7-enemy-vision-2026-11-15/editmode-full.xml` 和 `playmode-full.xml`，不使用该目录更早的历史基线，不重建或修改基线。逐例以完整 fullname 匹配，无别名映射；同时保存原始失败消息与堆栈。

| 平台 | 基线总数 / 通过 / 失败 | 本轮总数 / 通过 / 失败 | 新增失败 | fullname缺失/新增 | 失败集合 |
|---|---|---|---:|---:|---|
{chr(10).join(stats)}

736条结果没有状态变化，Skipped/Inconclusive均为0。机器门禁脚本返回0。原始结果：[EditMode XML]({prefix}editmode-full.xml)、[PlayMode XML]({prefix}playmode-full.xml)；完整逐例比较：[736条CSV]({prefix}fullname-comparison.csv)、[JSON]({prefix}baseline-comparison.json)。

{chr(10).join(focused)}

最初 EditMode全量为403/384/19；截图发现边框乘色偏黄绿后，补充无贴图描边并对最终代码重新运行全量。早期结果保留为editmode-initial.xml/log；不将其当作最终验收结果。初次截图辅助脚本缺少命名空间的编译错误保留为gallery-initial-compile-error.log，修正后截图正常；另保留中间乘色截图/日志。最终测试时临时截图辅助脚本及meta已移除。

全部基线失败逐条列在下表，不把总数相同当作同一集合：

| 平台 | 完整 fullname | 基线 → 本轮 |
|---|---|---|
{chr(10).join(failures)}

失败消息/堆栈细节变化列在JSON的failure_detail_changes，并在CSV中保留两侧原文；不对原始XML做归一化或覆写。严格门禁依据完整fullname和状态，两侧失败集合完全相同。

EditMode的19条失败消息和堆栈均相同；PlayMode仅以下2条消息有变化，堆栈均相同。第一条只改变随机Session ID，范围和实际值相同；第二条实际值相差约0.000016m，仍是原有约0.25m高度偏差。两个测试源码均未修改。

| 完整 fullname | 基线失败消息 | 本轮失败消息 | 堆栈 |
|---|---|---|---|
{chr(10).join(failure_details)}

复跑命令（等待每个Unity退出后再运行下一条，脚本遇到已有Unity只等待、不强杀；已存在的证据名称须换新名称）：

```powershell
& '{prefix}run-unity.ps1' -Kind EditMode -Name editmode-full
& '{prefix}run-unity.ps1' -Kind PlayMode -Name playmode-full
python {prefix}compare-tests.py
```

## 截图

截图由独立Unity预览场景调用真实MainMenuUI.Initialize及最终样式生成，渲染世界空间UI，不包含Editor面板、XR角色或调试覆盖。特写为同一UI的独立相机渲染，无重绘或生成式修改。

- [Screen_MainMenu.png]({prefix}Screen_MainMenu.png)：1920×1080纯UI主菜单全景。
- [Screen_MainMenu_Closeup.png]({prefix}Screen_MainMenu_Closeup.png)：720×1080右侧入口列特写。
- [渲染属性记录]({prefix}gallery-inspection.txt)：标题hud_capsule、深色填充、橙色描边、20号Normal/Regular浅字；四科目均button_secondary_cyan。图片尺寸与SHA256见[screenshot-manifest.json]({prefix}screenshot-manifest.json)。

人工看图确认标题无亮绿内芯、外边框为橙色，文字恢复较小的浅色普通字，四科目为不同的青色次级按钮。

## 工作区保护与清理

开工快照存放在项目外临时目录，跑批后按原始字节恢复字体、XR设置及旧证据；Prefab未出现额外差异。保护检查：{cleanup['protected_file_count']}个非本轮文件逐字节一致；{cleanup['old_evidence_files_identical']}个旧证据文件一致；四份被跟踪的Assets/InitTestScene*.unity及各自meta均保留。详情见[cleanup.json]({prefix}cleanup.json)，其中记录了全部恢复项及敌人任务相关文件SHA256。

用户列出的敌人圆形视野8个实现文件（含CombatEnemySight）及其meta、Round7新增测试及其meta、相关BDD/接口/任务文档均保持开工内容。未执行git checkout、stash、reset、commit、push或切分支；所有Unity运行串行，收尾无Unity进程，临时截图C#辅助脚本及meta已删除。

本次为无设备Unity验证。VR实机仍可按BDD02复核20号标题可读性、原有按钮射线点击与误触；未把纯UI截图或自动化测试当作头显实机验收。
'''
(ROOT/'docs/codex-reports/mainmenu-heading-border-2026-11-15.md').write_text(text, encoding='utf8')
print('Wrote report, source diff, and screenshot manifest.')
