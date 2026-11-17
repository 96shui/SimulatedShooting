"""Assemble the requested report from exact NUnit and workspace evidence."""
from pathlib import Path
import csv
import difflib
import hashlib
import json
import xml.etree.ElementTree as ET
from PIL import Image

ROOT = Path.cwd().resolve()
OUT = Path(__file__).resolve().parent
BACKUP = Path('C:/Users/10532/AppData/Local/Temp/mode-intro-panel-frame-2026-11-16-backup')
data = json.loads((OUT/'baseline-comparison.json').read_text(encoding='utf8'))
workspace = json.loads((OUT/'workspace-verification.json').read_text(encoding='utf8'))
assert data['gate_passed'] and workspace['all_other_entry_files_identical']
owned = workspace['task_changed_files']
diff = []
for name in owned:
    diff.extend(difflib.unified_diff((BACKUP/name).read_text(encoding='utf8').splitlines(True),
                (ROOT/name).read_text(encoding='utf8').splitlines(True), fromfile='entry/'+name, tofile='current/'+name))
(OUT/'task-changes.diff').write_text(''.join(diff), encoding='utf8')

manifest = []
for name in ['Screen_MainMenu.png'] + [f'Screen_ModeIntro_{mode}.png' for mode in ('Zeroing','MovingTarget','Trench','Urban')]:
    path = OUT/name
    with Image.open(path) as img:
        assert img.size == (1920,1080), (name,img.size)
        manifest.append({'file':name,'width':img.width,'height':img.height,'bytes':path.stat().st_size,
                         'sha256':hashlib.sha256(path.read_bytes()).hexdigest()})
(OUT/'screenshot-manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf8')
prior_menu = OUT.parent/'mainmenu-heading-restore-2026-11-16/Screen_MainMenu.png'
if not prior_menu.exists():
    prior_menu = OUT.parent/'mainmenu-heading-restore-2026-11-16/gallery/Screen_MainMenu.png'
menu_identical = prior_menu.exists() and prior_menu.read_bytes() == (OUT/'Screen_MainMenu.png').read_bytes()
(OUT/'mainmenu-unchanged.json').write_text(json.dumps({'reference':str(prior_menu),'png_bytes_identical':menu_identical},ensure_ascii=False,indent=2),encoding='utf8')

def counts(p):
    return f"{p['total']} / {p.get('Passed',0)} / {p.get('Failed',0)}"

def link(name):
    return f'[evidence/{OUT.name}/{name}](evidence/{OUT.name}/{name})'

lines = [
'# 模式操作介绍页面板恢复与进入训练高亮验收（2026-11-16）', '',
'四个介绍页已恢复 `panel_holo_large` 蓝色发光外框与内部深色科技网格；“进入训练”使用 `button_primary_orange` 黄色高亮，“返回”保持 `button_secondary_cyan`。面板和内部坐标未修改，四页正文均一屏完整显示。证据目录按请求保留 2026-11-16 命名，实际执行为北京时间 2026-11-17。', '',
'## 本次文件与实现', '',
'生产代码只修改 `Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs`。删除 `BuildModeIntro` 中清空 sprite、重染色、额外添加 Outline 的六行特例，完整沿用原 AddPanel 调用；进入按钮最后一个参数改为 true。', '',
'定向测试另发现 `TacticalUIStyle.Apply` 会按名称把 Enter 归类为 secondary，覆盖 AddButton 的 primary 参数。因此仅在 MainMenuUI 的共用样式应用后，针对四个介绍页的 Enter 调用现有 `TacticalUIStyle.StyleButton(..., true)`，保留普通、悬停、按下和选中状态的 primary 贴图。未修改共用样式文件。', '',
'同步更新受影响的 `Assets/VRShooting/Tests/PlayMode/UI/Screen02_04_MainMenuZeroingBriefingUITests.cs` 和 `docs/BDD/screens/37-各模式操作介绍.feature.md`。其余新增文件为本报告、测试原始 XML/日志、比较脚本和截图证据。', '',
'对象名、TestId、监听与原入口路由未修改。未执行 commit、push、切分支、stash、reset 或 checkout。每次启动前检查 Unity 进程为零，所有运行串行；启动窗口隐藏。临时截图助手只在出图时导入，退出后移除，源码存档为证据目录中的 `.cs.txt`。', '',
'## 与旧操作介绍页对照', '',
'依据 `ef5a00e` 的 `BuildMainMenuControls` 与 [旧版截图](evidence/ui-chinese-copy-2026-11-12/gallery/Screen_MainMenu_ControlsOpen.png)。当前四页使用相同 AddPanel 参数与同一 `panel_holo_large` 资源，Image 为 Sliced、Color.white，面板无 Outline；目视确认同款蓝色发光框线、斜角装饰和内部深色网格均恢复。', '',
'| 项目 | 旧页面/要求 | 本次四页实测 |',
'|---|---|---|',
'| 面板 | (130,120)–(1790,960)，1660×840 | 完全保留 |',
'| AddPanel 填色/边线参数 | (11,19,16,245) / (45,66,56,255) | 完全保留；由原贴图原色显示 |',
'| 标题 | (80,750)–(1580,820)，38号 | 完全保留 |',
'| 滚动区 | (75,150)–(1585,720)，1510×570 | 完全保留 |',
'| 正文 | 26号，lineSpacing=10，栏间距60 | 完全保留 |',
'| 返回 | (460,35)–(760,105)，secondary | 完全保留 |',
'| 进入训练 | (900,35)–(1200,105)，primary | 坐标保留，黄色高亮生效 |', '',
'## 测试与基线比较', '',
'测试使用 Unity 2022.3.62f3c1，参数 `-batchmode -projectPath <repo> -runTests -testPlatform <EditMode|PlayMode> -testResults <xml> -logFile <log>`；定向追加 testFilter。测试不传 -quit/-nographics；截图传 -quit 并调用专用捕获入口。若编辑器在结果保存及输入系统关闭后停滞，运行器只终止自己启动的已完成进程，相关 shutdown.json 如实记录，未把停滞当作测试通过依据。', '',
'| 执行 | 总数 | 通过 | 失败 |',
'|---|---:|---:|---:|',
]
for name,label in [('editmode-targeted','EditMode 定向：MainMenuControlsTests'),('playmode-targeted-final','PlayMode 定向：介绍页与主菜单流程')]:
    result = ET.parse(OUT/(name+'.xml')).getroot().attrib
    assert result['failed'] == '0'
    lines.append(f"| {label} | {result['total']} | {result['passed']} | {result['failed']} |")
for kind in ('editmode','playmode'):
    p=data['platforms'][kind]
    for key,label in [('baseline','指定基线'),('current','本次全量')]:
        c=p[key]
        lines.append(f"| {kind} {label} | {c['total']} | {c.get('Passed',0)} | {c.get('Failed',0)} |")
lines += ['',
'先增加断言再修复：`playmode-red.xml` 的唯一用例按预期失败于 sprite=null。初版定向 `playmode-targeted.xml` 为12通过/1失败，定位到共用名称分类覆盖 primary；最终 `playmode-targeted-final.xml` 为全部通过。上述两次诊断结果保留，不混入最终全量回归统计。', '',
f"完整 fullname 比较共 {data['compared_fullnames']} 条记录，未使用别名、失败排除或覆盖基线。指定基线为 `evidence/mainmenu-heading-border-2026-11-15/editmode-full.xml` 和 `playmode-full.xml`。EditMode 19 项和 PlayMode 21 项失败均与基线逐条同名；新增失败 **0**，已消失的基线失败 **0**，两套失败集合完全一致。共同 fullname 的结果状态变化 **0**。", '',
'工作区进入本任务前已经有上一轮未提交的测试改动。相对于指定基线，EditMode 已存在四条参数化用例的 fullname 变化与九条新用例，PlayMode 已存在两条新用例。为完整保留用户工作区，未回退这些用例；比较 CSV 原样列出 MISSING 和新增 fullname，不把它们别名合并。本次另与进入时已存在的 `mainmenu-heading-restore-2026-11-16` 全量 XML 比较：未删除用例、未改变任何既有结果，仅新增一条通过的 PlayMode 面板/按钮样式用例。', '',
'| 已存在的参数化 fullname 前缀 | 指定基线参数 | 进入本任务时及本次参数 |',
'|---|---|---|']
p=data['platforms']['editmode']
for old in p['preexisting_missing_fullnames']:
    prefix=old.split('(')[0]
    button=old.split('"')[1]
    matches=[n for n in p['preexisting_added_fullnames'] if n.startswith(prefix+'(') and f'"{button}"' in n]
    assert len(matches)==1
    lines.append(f"| `{prefix}` | `{old[len(prefix):]}` | `{matches[0][len(prefix):]}` |")
lines += ['', '进入本任务前已新增且继续通过的完整 fullname：', '', '```text']
for kind,p in data['platforms'].items():
    for name in p['preexisting_added_fullnames']:
        if name.split('(')[0].endswith('Bdd02_TrainingButtonGeometryIsUnchanged'):
            continue
        lines.append(name)
lines += ['```', '', '本次有意更新的断言逐项对照：', '',
'| 测试 | 原断言 | 本次断言 |',
'|---|---|---|',
'| `Bdd37_FourIntroPanelsPreserveLegacyFrameAndPrimaryEnterStyle`（新增） | 未验证介绍页面板资源与按钮资源 | 遍历四页：panel rect/offset、非空 panel_holo_large、Sliced、Color.white、无 Outline、标题/滚动区/按钮坐标、字号/行距/栏距、primary Enter 与 secondary Back |',
'| `Bdd37_BodyFitsCommonResolutionsAndOverflowScrollDoesNotCoverButtons`（加强） | 正文底边不低于 viewport 底边，按钮不属于滚动层；另有完整正文与溢出滚动验证 | 保留原断言，增加左右/顶部边界和正文底边高于两按钮顶边；四页×三种分辨率验证 |', '',
'1920×1080、1280×720、2560×1440 三种 Canvas 等比尺度均通过；正文无溢出、完整字符数匹配，四边不越出 viewport，正文与底部按钮不相交。超大字号时仍可滚至末行，返回按钮可点击。现有测试继续覆盖反复开关、返回不改服务状态、四个确认落到原页面及隐藏确认按钮不重放。', '',
'### 基线失败逐条核对', '',
'以下所有完整 fullname 的指定基线结果与本次结果均为 Failed；失败消息与堆栈逐条保存在 CSV/JSON 中。', '']
for kind,p in data['platforms'].items():
    lines += [f"{kind}：{len(p['failed_fullnames'])} 项，新增0、消失0。", '', '```text', *p['failed_fullnames'], '```', '']
lines += ['失败详情也已比较：EditMode 的19条失败消息/堆栈均相同；PlayMode 有7条文本详情差异，逐项如下。断言阈值与失败状态均未改变。完整原文见CSV。', '',
          '| 完整 fullname | 指定基线 → 本次详情 |', '|---|---|']
notes = {
    'Bdd14_NewTrenchAllowsContinuousMovementAndFiring': '消息中的会话ID变化；期望范围(2.018377,3.428377)、实测1.92627943f和堆栈均相同。',
    'NewSceneInspectionWalkerCanLeaveEntry': '实测1.92736042f → 1.9273802f；期望2.17788005f ± 0.0799999982f不变，堆栈相同。',
    'Screen14_18_CancelLoadingThenSwitchModeKeepsExclusiveScene': '同为期望UrbanStreetHud、实得UrbanBuildingHud；堆栈P3ProductionFlowTests.cs:84 → :85，为进入本任务前已存在的源码行号变化。',
    'Screen14_18_DesktopSceneHasSingleOutputAndVisiblePageBounds': '同为NullReferenceException；堆栈P3ProductionFlowTests.cs:56 → :57，为进入本任务前已存在的源码行号变化。',
    'Screen21_Urban_ProductionVictoryRetryAndReturn': '同为Entry prompt期望True、实得False；堆栈P3ProductionFlowTests.cs:446 → :449，为进入本任务前已存在的源码行号变化。',
    'Bdd33_TrackedTwoHandVisibleSightAndTriggerCanZeroNextRound': '实测77.9776382f → 77.9776611f；期望小于1.0f不变，堆栈相同。',
    'Bdd34_MenuVisibilityProtectionDoesNotHideTrainingHands': '同为期望2、实得0；堆栈XRSceneHandoffTests.cs:148 → :149，为进入本任务前已存在的源码行号变化。',
}
for fullname in data['platforms']['playmode']['failure_detail_changes']:
    lines.append(f'| `{fullname}` | {notes[fullname.rsplit(".",1)[1]]} |')
lines.append('')
lines += [
'比较产物：', '',
f'- {link("baseline-comparison.json")}',
f'- {link("fullname-comparison.csv")}',
f'- {link("editmode-full.xml")}',
f'- {link("playmode-full.xml")}', '',
'## 1920×1080 逐页截图验收', '',
'复用上一轮隔离 PreviewScene 的真实 UGUI 捕获方式：正交相机位置(960,540,-1000)、orthographicSize=540、RenderTexture=1920×1080；Canvas WorldSpace、尺寸1920×1080、中心(960,540,0)、缩放1。临时关闭画布适配/Scaler，以同一布局像素口径出图。背景、框线、网格、文字与按钮均由 Unity Camera.Render 直接渲染，未改写 PNG 像素。', '',
'| 页面 | 面板框线与网格 | 进入训练 | 正文 | 图片 |',
'|---|---|---|---|---|']
for mode,label in [('Zeroing','模式一 · 100m精度射校'),('MovingTarget','模式二 · 移动目标射击'),('Trench','模式三 · 堑壕射击'),('Urban','模式四 · 城镇攻防')]:
    lines.append(f'| {label} | 蓝色发光外框与深色科技网格已恢复 | 黄色 primary；返回为青色 secondary | 左右栏完整，无裁切、重叠或压按钮 | {link("Screen_ModeIntro_"+mode+".png")} |')
lines += ['',f'主菜单截图：{link("Screen_MainMenu.png")}。']
if menu_identical:
    lines += ['与进入本任务前的主菜单截图 PNG 字节完全一致，确认主菜单视觉未变化。']
lines += ['',f'布局与资源实测：{link("gallery-inspection.txt")}；PNG 尺寸与 SHA-256：{link("screenshot-manifest.json")}。', '',
'## 工作区保护与验收边界', '',
f'执行前对 {workspace["protected_files"]} 个既有文件建立 SHA-256 清单并备份可写资源。测试自动生成或覆盖的历史证据、字体/设置等文件已按进入时字节恢复；保留所有原有未提交改动。除本次授权的三个源码/BDD文件和新报告证据外，其余进入时文件均与快照一致。详见 '+link('workspace-verification.json')+' 与 '+link('task-changes.diff')+'。', '',
'本次为 UI 样式修复，没有修改玩法、输入或服务。真实 VR 设备的阅读舒适度、射线悬停/点击仍按 BDD37 实机验收；自动化测试与画廊截图不代替头显实测。', '']
report=ROOT/'docs/codex-reports/mode-intro-panel-frame-2026-11-16.md'
report.write_text('\n'.join(lines),encoding='utf8')
print('Wrote',report)
print('Main menu PNG identical to entry:',menu_identical)
