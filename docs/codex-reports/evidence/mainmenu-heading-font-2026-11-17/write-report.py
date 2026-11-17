from pathlib import Path
import json
import xml.etree.ElementTree as ET

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[3]
PREFIX='evidence/mainmenu-heading-font-2026-11-17/'
m=json.loads((HERE/'font-measurements.json').read_text(encoding='utf8'))
px=json.loads((HERE/'pixel-verification.json').read_text(encoding='utf8'))
comparison=json.loads((HERE/'baseline-comparison.json').read_text(encoding='utf8'))
cleanup=json.loads((HERE/'cleanup.json').read_text(encoding='utf8'))

def link(label,path):return f'[{label}]({PREFIX}{path})'
def summary(name):
    r=ET.parse(HERE/(name+'.xml')).getroot()
    return f"{r.get('total')}条：{r.get('passed')}通过 / {r.get('failed')}失败"
def metric_table(cases):
    lines=['| 图像 | 字形宽×高(px) | 左内缩 / 右侧剩余(px) | 墨迹像素 / 亮像素 | 亮像素占包围盒 | 平均墨迹亮度(0–255) | 笔画宽度代理(px) |',
           '|---|---:|---:|---:|---:|---:|---:|']
    for name,v in cases:
        lines.append(f"| {name} | {v['width']}×{v['height']} | {v['left_inset']} / {v['right_inset']} | {v['ink_pixels']} / {v['bright_pixels']} | {100*v['bright_fraction_of_glyph_bbox']:.4f}% | {v['mean_ink_luminance']:.4f} | {v['stroke_width_proxy']:.6f} |")
    return '\n'.join(lines)

lines=[
'# 主菜单“选择训练科目”字体恢复（2026-11-17）',
'',
'已恢复为独立worktree中`9a1c01224a7e87ed84c6d54fe47cec4e46d5422a`的字形效果。实测117×20px、左内缩30px、右侧剩余243px，与基线误差均为0；相同黑底、按面板中心对齐后的全部字形RGB像素差异为0。标题条继续使用hud_capsule、Color.white、无Outline，坐标(1360,805)–(1750,865)。',
'',
'**渲染流程与原始基准**',
'',
'使用Unity 2022.3.62f3c1，在独立detached worktree `E:/SimulateShooting-heading-font-9a1c012`检出指定提交；主工作区HEAD和分支保持原状。同一份'+link('截图脚本','capture.ps1')+'和'+link('C#渲染源','HeadingFontEvidence.cs.txt')+'分别渲染基线、开工版本、单变量探针和最终版本。脚本沿用上一单TacticalUIGallery.Create → MainMenuUI.Initialize → PreviewScene → WorldSpace Canvas → Camera.Render流程，1920×1080、RGB24、1设计单位=1像素；未重绘原始截图。所有Unity实例串行运行。',
'',
'指定提交及11月15日认可截图的标题条实际位于(1360,667)–(1750,737)，PNG区域为x1360..1749、y343..412；当前标题条按用户指定坐标保留，PNG区域为x1360..1749、y215..274。11月12日原版标题条PNG区域为y215..284。比较以各自面板左边缘为原点；字形单独渲染后只按面板中心对齐，不修改任何基线源码或原始全景布局。两版70px/60px高度相差10px，比较黑底字形时在当前面板上下各补5px黑底。',
'',
'量化规则：在标题条内部x18..179、y15..高度−16的文字搜索窗口，取min(R,G,B)≥80且max(R,G,B)−min(R,G,B)≤45的墨迹像素，避免蓝色边框；亮度L=0.2126R+0.7152G+0.0722B，亮像素为L≥200。亮像素占比的分母是字形包围盒面积。笔画宽度代理=2×墨迹面积/四邻接数字周长，作为同条件的粗细比较指标。右侧剩余指最后一个字形像素到面板右边缘的空白，不等同于RectTransform右padding。',
'',
'**原始全景的量化比较**',
'',
metric_table([(n,m[k]) for n,k in [('worktree 9a1c012','baseline-9a1c012'),('11月15日认可截图','accepted-2026-11-15'),('11月12日原版截图','original-2026-11-12'),('本轮修复前','before'),('本轮修复后','after')]]),
'',
'本轮修复前图与用户指出的11月16日问题图量化结果完全一致。基线与两个认可参考的宽度、高度、左右空白、墨迹像素数、亮像素数及笔画宽度代理互相印证。完整数据见'+link('font-measurements.json','font-measurements.json')+'。',
'',
'新版面板保留蓝色原色，旧版面板为暗色tint；抗锯齿边缘与不同底色混合后，原始全景平均墨迹亮度相差0.9632/255，亮像素占包围盒相差0.2564个百分点。将文字通过同一相机单独渲染到相同黑底，得到以下结果，所有量化项严格一致：',
'',
metric_table([(n,px['isolated_font_metrics'][k]) for n,k in [('worktree 9a1c012，同底字形','baseline-9a1c012'),('修复前，同底字形','before'),('修复后，同底字形','after')]]),
'',
'基线与最终版按中心对齐的390×70同底图，changed_pixels=0、max_channel_delta=0。TMP六字逐字的bottomLeft/topRight也完全一致，原始记录见'+link('基线组件检查','baseline-9a1c012/gallery-inspection.txt')+'、'+link('最终组件检查','after/gallery-inspection.txt')+'及'+link('逐像素审计','pixel-verification.json')+'。',
'',
'**定位与最终改动**',
'',
'基线和当前AddPanel的源参数相同：AddLabel(...,20,FontStyles.Bold,Left,(20,18),(-20,-18),...)。AddLabel首先剥离Bold、设Regular、extraPadding=true；TacticalUIStyle.Apply随后保留Normal并将extraPadding=false，小面板文字内边距改成(30,4)至(-30,-4)。基线最终是Normal/Regular，而开工版本在共用样式之后又强行恢复Bold及(20,18)偏移，造成增粗和左移。',
'',
'单变量探针先仅将Bold改为Normal，实测宽117px、高20px、左内缩20px、右侧剩余253px、亮像素占比20.9829%，验证增粗/增宽来自合成Bold，剩余10px左移来自标题专用偏移。最终生产代码只在MainMenuUI.cs删除该标题专用覆盖块10行，恢复基线共用流程；未改字号、颜色、fontWeight或共用样式。',
'',
'恢复后文字容器从350×24变成330×52，基线Ellipsis模式下isTextOverflowing=false、六个字形均可见，省略号没有替换标题。生产代码中其它入口、状态条、介绍页、边框和面板坐标均无本轮修改。相对开工快照的精确差异见'+link('task-changes.diff','task-changes.diff')+'。同步更新的文档为BDD02、接口01与P1/UI task008；DTO、命令和事件无变化。',
'',
'**断言新旧对照**',
'',
'EditMode保留完整fullname `VRShooting.Tests.EditMode.Application.MainMenuControlsTests.Bdd02_HeadingUsesPrimaryVisualWithoutInteractionAndEntriesUseSecondaryVisual`；PlayMode保留 `VRShooting.Tests.PlayMode.UI.Screen02_04_MainMenuZeroingBriefingUITests.Screen02_CreatorsEntryAndPanelAreAbsentAndNavigationRemainsUsable`。相对开工源码，仅标题相关断言有以下有意变化；其它断言保留。',
'',
'| 断言 | 开工时 | 本轮最终 |',
'|---|---|---|',
'| fontStyle（两平台） | Bold | Normal，与9a1c012实际输出一致 |',
'| offsetMin / offsetMax（两平台） | (20,18) / (-20,-18) | (30,4) / (-30,-4) |',
'| fontWeight | EditMode已有Regular；PlayMode未检查 | 两平台检查Regular |',
'| extraPadding | 未检查 | 两平台检查false |',
'| 六字可见 | 两平台检查6个可见字形 | 保留；说明文字同步为恢复后的容器 |',
'| isTextOverflowing | 未检查 | 两平台检查false，禁止省略掉标题 |',
'| 实际字形栅格 | 未检查 | EditMode用Camera.Render真实渲染390×60标题文字，验证存在墨迹、宽117±1px、高20±1px、左30±1px、右243±1px |',
'| 粗细保护 | 未检查 | EditMode亮墨迹/全部墨迹=484/1291，容差±0.015；注释写明基线实测值 |',
'| 标题面板、字体大小/颜色/对齐 | hud_capsule、Sliced、white、无Outline、固定坐标；20号、原色、左对齐 | 全部保留 |',
'',
'回归测试先于修复加入：'+link('editmode-raster-red.xml','editmode-raster-red.xml')+'为15通过/1失败，准确报出左内缩期望30±1、实际19；'+link('初次修复测试','editmode-targeted.xml')+'中栅格断言通过，但测试清理未解除Camera.targetTexture产生Unity错误，随后补齐finally清理并在最终定向/全量测试中通过。',
'',
'**前后影响范围证明**',
'',
'| 比较对象 | 变化像素数 |',
'|---|---:|',
'| 主菜单全景 | 2226，全部位于x1379..1506、y236..255的标题文字区域 |',
'| 标题条区域以外的全部全景像素 | 0 |',
'| 01、02、03、04各自整行入口 | 各0 |',
'| 底部状态条 | 0 |',
'| 隐藏标题文字后的整张主菜单（包含标题边框/内芯） | 0 |',
'| 模式一、二、三、四介绍页各自完整画面 | 各0 |',
'',
'上述均按未经处理的PNG RGB像素比较。'+link('verify-pixels.py','verify-pixels.py')+'可独立复算，'+link('pixel-verification.json','pixel-verification.json')+'保存全部结果；四页介绍的before/after原图也已保留。',
'',
'**测试与指定基线逐条比较**',
'',
f"最终EditMode定向：{summary('editmode-targeted-final')}。相关UI、输入和P1流程PlayMode定向：{summary('playmode-targeted')}。两平台全量结果如下：",
'',
'| 平台 | 用户指定11月15日基线 | 本轮全量 | 新增失败 | 失败集合逐条同名 |',
'|---|---|---|---:|---|',
]
for kind,p in comparison['platforms'].items():
    b,c=p['baseline'],p['current']
    lines.append(f"| {kind} | {b['total']}：{b.get('Passed',0)}通过/{b.get('Failed',0)}失败 | {c['total']}：{c.get('Passed',0)}通过/{c.get('Failed',0)}失败 | {len(p['new_failures'])} | {'是' if p['failure_sets_identical'] else '否'} |")
lines += ['',
'主基线严格使用`mainmenu-heading-border-2026-11-15/editmode-full.xml`与`playmode-full.xml`原始文件。按完整fullname比较，不做别名、参数归一化或失败豁免。保留的共同fullname状态变化为0；40个失败逐条同名。全量仍有这些既有失败。',
'',
'开工时已有11月16日介绍页/布局改动，因此相对11月15日基线，存在以下四个通过用例的参数fullname变化。它们在本轮开工前的源码和XML中已经存在，本次未改TestCase参数、未删除或改名这些方法。完整fullname原样列出，比较CSV仍将旧项标为MISSING、新项标为新增，不互相映射：',
'',
'| 11月15日基线完整fullname（Passed） | 开工版本及本轮完整fullname（Passed） |',
'|---|---|']
p=comparison['platforms']['editmode']
for old in p['missing_fullnames']:
    prefix=old.split('",')[0]+'",'
    new=next(n for n in p['added_fullnames'] if n.startswith(prefix))
    lines.append(f'| `{old}` | `{new}` |')
lines += ['',
'此外，开工时已有9个EditMode介绍/编号/悬停用例及3个PlayMode介绍页用例，相对11月15日为新增。本轮全部通过，逐条列在下表；本次没有增加新的测试fullname，新增栅格断言放在既有标题测试中。',
'','| 平台 | 开工时已有的新增完整fullname | 本轮 |','|---|---|---|']
for kind,p in comparison['platforms'].items():
    for name in p['added_fullnames']:
        if 'Bdd02_TrainingButtonGeometryIsUnchanged' not in name:
            lines.append(f'| {kind} | `{name}` | Passed |')
lines += ['',
'从本轮开工已存在的最新`mode-intro-panel-frame-2026-11-16`完整测试清单到最终版本，748个fullname的缺失/新增/状态变化均为0。开工源码快照也包含第三条介绍页面板用例。该辅助清单仅用于证明既有差异来源，主基线仍是用户指定的11月15日XML。比较脚本同时强制“与指定基线失败集合相同、无共同项状态变化”和“本次测试清单无额外变化”。',
'',
link('全量EditMode XML','editmode-full.xml')+'、'+link('全量PlayMode XML','playmode-full.xml')+'、'+link('EditMode定向XML','editmode-targeted-final.xml')+'、'+link('PlayMode定向XML','playmode-targeted.xml')+'。'+link('逐条完整fullname CSV','fullname-comparison.csv')+'包含752条并集记录（736条指定基线用例与开工前已有差异的新增项），'+link('比较JSON','baseline-comparison.json')+'保留XML SHA256、缺失/新增全集、状态变化、失败消息与堆栈差异。',
'',
'40个既有失败完整fullname如下；每条均为指定基线Failed → 本轮Failed：',
'','| 平台 | 完整fullname | 基线 → 本轮 |','|---|---|---|']
for kind,p in comparison['platforms'].items():
    for name in p['failed_fullnames']:
        lines.append(f'| {kind} | `{name}` | Failed → Failed |')
lines += ['',
'**截图与复算**','',
'- '+link('最终主菜单全景1920×1080','Screen_MainMenu.png'),
'- '+link('最终右侧入口列特写1920×1080','Screen_MainMenu_Closeup.png'),
'- '+link('9a1c012独立worktree原始全景1920×1080','Screen_MainMenu_9a1c012.png'),
'- '+link('9a1c012 / 修复前 / 修复后原尺寸局部对照','Heading_Font_Comparison.png')+'（仅裁切拼排并标注，原图另存）',
'- '+link('PNG尺寸和SHA256清单','screenshot-manifest.json'),
'',
'![标题文字对照]('+PREFIX+'Heading_Font_Comparison.png)',
'',
'复算命令（项目根目录，Python需numpy和Pillow）：','',
'```powershell',
'python docs/codex-reports/evidence/mainmenu-heading-font-2026-11-17/measure-font.py --check',
'python docs/codex-reports/evidence/mainmenu-heading-font-2026-11-17/verify-pixels.py',
'python docs/codex-reports/evidence/mainmenu-heading-font-2026-11-17/compare-tests.py',
'```','',
'全景、入口列与字形对照均已人工检查，六个汉字完整可见。实机VR显示未在本次无设备渲染中验证。',
'',
'**工作区保护**','',
f"开工快照保护{cleanup['protected_file_count']+len(cleanup['task_changed_files'])}个文件。批跑后按原始字节恢复Unity改写的资源、设置、场景和历史证据；除本次六个授权文件外，其余{cleanup['protected_file_count']}个保护文件SHA256全部一致。被跟踪的Assets/InitTestScene*.unity及meta均保留。原有介绍页面板、其它UI和未提交代码已保留。详见"+link('cleanup.json','cleanup.json')+'、'+link('初始Git状态','initial-git-status.txt')+'和'+link('最终Git状态','final-git-status.txt')+'。',
'',
'独立worktree在渲染取证后已删除；基线源码副本与commit值保留在baseline-9a1c012/。未执行git commit、push、stash、主工作区checkout或分支切换。HEAD保持9a1c01224a7e87ed84c6d54fe47cec4e46d5422a。收尾验证见'+link('final-validation.json','final-validation.json')+'。',
]
(ROOT/'docs/codex-reports/mainmenu-heading-font-2026-11-17.md').write_text('\n'.join(lines)+'\n',encoding='utf8')
print('Report written:',len(lines),'lines')
