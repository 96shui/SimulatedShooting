from pathlib import Path
import json,subprocess,xml.etree.ElementTree as ET
ROOT=Path.cwd();OUT=ROOT/'docs/codex-reports/evidence/mode-intro-pages-2026-11-16'
data=json.loads((OUT/'baseline-comparison.json').read_text(encoding='utf-8'))
cleanup=json.loads((OUT/'cleanup.json').read_text(encoding='utf-8'))
def rel(name):return 'evidence/mode-intro-pages-2026-11-16/'+name
lines=['# 主菜单回退与各模式操作介绍交付报告（2026-11-16）','',
'基于 HEAD `9a1c012`。完成范围仅为 UI、相关测试、BDD、接口与任务文档、截图证据；没有修改战斗、感知、弹药、切肩、手雷或模式一计分逻辑。未使用 commit、push、切分支、checkout、stash、reset。',
'', '## 实现与根因', '',
'旧版本将跨模式说明放在主菜单总览，训练入口仍直接执行原入口逻辑；标题条额外挂载 Outline 与 MainMenuHeadingBorderGraphic；按钮共用居中标签使不同长度的名称挤动编号，左侧贴图装饰逼近01。现已删除旧总览按钮、面板及全部子节点与监听，完整删除专用描边 Graphic，四个入口使用固定编号列和名称列。',
'',
'标题条按 ef5a00e 的 AddPanel 参数恢复为 (1360,805)–(1750,865)、Color32(17,29,24,230)、普通20号/Regular字重、Color32(231,242,235,255)，无交互、无橙色边框。该历史版本的通用 ResolvePanelSprite 对390px宽度实际返回 hud_capsule；本次遵循用户明确指定的 status_strip_long，增加仅针对标题条的解析分支。',
'',
'四按钮均使用 button_secondary_cyan，序号本地矩形 x=84–124，名称 x=130–374，中间固定6px，贴图内嵌装饰保留区 x=26–76。测试比较实际首字符的渲染左边缘（容差≤1px）、图标保留矩形与编号互斥、同一行基线一致。',
'',
'XR UIInputModule 已发送标准 pointerEnter/pointerExit（Library/PackageCache/com.unity.xr.interaction.toolkit@3.1.2/Runtime/UI/UIInputModule.cs），Selectable 的 SpriteSwap 确实响应，因此没有另写重复的指针处理。主菜单四入口关闭点击选中导航，selectedSprite设为normal，避免离开后驻留高亮；普通与TrackedDeviceEventData指针测试均验证overrideSprite，截图另保留一次真实悬停态。',
'',
'介绍页属于MainMenu的本地模态UI，不新增ScreenId/UIEventId/DTO。打开和返回不发送服务命令，不改会话、Presentation或Combat状态。确认先消费并清除待执行回调，再执行原OnOpenZeroingClicked、OnOpenMovingTargetClicked或Combat.OpenMode；重复点击隐藏确认按钮不会重放命令。保留CombatScenesAvailable门禁、模式二设置/场景加载、模式三四地图及后续训练流程。',
'',
'每页沿用(130,120)–(1790,960)深色半透明底(11,19,16,245)、边线(45,66,56)、38号居中标题、左右两栏、底部两个按钮。正文26号、行距10；HorizontalLayoutGroup和ContentSizeFitter按两栏最大首选高度计算内容高度，ScrollRect仅竖直、Clamped，RectMask2D限制正文，按钮位于滚动区外。整屏透明射线遮挡层避免穿透主菜单。',
'', '## 明确的坐标冲突处理', '',
'用户列出的具体范围是：标题805–865；01为665–750；02为535–620；03为405–490；04为275–360，x均1360–1750。这组数值对应中心间距130px、按钮间空隙45px、标题与01空隙55px，与同一请求中的“128/43”不一致。本次优先遵循逐项精确坐标，代码、BDD和测试均锁定这些数值，没有放宽布局判定。',
'', '## 文案前后对照', '',
'| 项目 | 修改前 | 修改后 |','|---|---|---|',
'| 入口 | 主菜单总览“操作介绍” | 各模式入口先显示对应介绍；返回/进入训练 |',
'| 通用握枪射击 | 一份“通用 VR 操作” | 四页均完整写出右手侧握键抓枪、左手侧握键辅助抓握、右扳机射击 |',
'| 模式一 | 总览中简略轮次、调整量及通过规则 | 完整说明单发固定点、4发×3轮、分析四调整按钮、立即判定、作废本轮、评级、蓝点与分散禁用 |',
'| 模式二 | 双手据枪与两发起射/连射短说明 | 增补速度、跟随/等待射击、弹药命中用时与结算重训返回 |',
'| 模式三/四 | 合用一段按键操作 | 各自重复全部X/Y/B/A、左肩左手左扳机、左右摇杆；分别说明堑壕状态和城镇建筑交互 |',
'| 英文缩写 | VR、UI等操作说明缩写 | 新介绍页写“界面射线”“抬头显示区”；仅保留键位和数值单位 |',
'', '以下为截图时从真实UI组件提取的最终文案（共14/11/13/13行，含两栏小标题；自动换行不计入文案行数）：','']
inspection=(OUT/'gallery-inspection.txt').read_text(encoding='utf-8').splitlines()
for mode,title in [('Zeroing','模式一 · 100m 精度射校'),('MovingTarget','模式二 · 移动目标射击'),('Trench','模式三 · 堑壕射击'),('Urban','模式四 · 城镇攻防')]:
 lines+=['### '+title,'']
 for side in ['Left','Right']:
  line=next(l for l in inspection if l.startswith('Text_ModeIntro_'+mode+'_'+side+' '))
  copy=line.split(' text=',1)[1].split(' font=',1)[0].replace(' | ','\n')
  lines+=['```text',copy,'```','']
lines+=['## 测试与基线比较','','定向 EditMode `MainMenuControlsTests`：16/16通过。最终扩展定向 PlayMode：62/62通过，覆盖主菜单、P1Flow、场景UI所有权、HUD、弹着分析和最终评级。','',
'| 平台 | 指定基线 | 本次全量 | 新增失败 | 原失败转通过 |','|---|---|---|---:|---:|']
for kind in ['editmode','playmode']:
 p=data['platforms'][kind];a=p['baseline'];b=p['current']
 lines.append('| '+kind+' | '+str(a['total'])+'（通过'+str(a.get('Passed',0))+' / 失败'+str(a.get('Failed',0))+'） | '+str(b['total'])+'（通过'+str(b.get('Passed',0))+' / 失败'+str(b.get('Failed',0))+'） | '+str(len(p['new_failures']))+' | '+str(len(p['resolved_failures']))+' |')
lines+=['',
'比较使用完整fullname与两份XML的SHA256，记录每条状态、失败信息和堆栈，未改写基线、未按总数替代逐条检查。四个参数化坐标用例因实际参数改变产生4个新fullname，旧名与精确新名逐对写入coordinate_contract_replacements，且新用例必须通过；原有覆盖均保留或由这4个精确坐标用例替换，其余新增用例均通过。原有方法名称保留以便对应历史覆盖，断言已改为新契约。','',
'基线失败转通过：'+('无，原失败集合完全保留。' if all(not p['resolved_failures'] for p in data['platforms'].values()) else '见baseline-comparison.json resolved_failures。'),'']
for name,label in [('baseline-comparison.json','逐条比较结论和新增/缺失/状态变化集合'),('fullname-comparison.csv','完整fullname逐条CSV（含失败消息与堆栈）'),('editmode-full.xml','本次EditMode全量XML'),('playmode-full-final.xml','本次PlayMode最终全量XML'),('editmode-targeted-r2.xml','EditMode定向XML'),('playmode-targeted-final.xml','PlayMode最终定向XML')]:lines.append('- ['+label+']('+rel(name)+')')
lines+=['','测试过程保留了首轮EditMode编译错误日志（测试文件补入Common命名空间后修复），以及首轮PlayMode新XR断言失败XML：介绍刚打开同帧Graphic尚未注册到XR射线缓存，测试改为与实际逐帧输入一致，等待一帧后再射线命中确认按钮；原命中断言保留，没有放宽。首轮全量另发现SimulatedShooting测试目录中的3处旧入口假设，已同步为公共介绍页辅助方法，真实手柄用例继续用控制器扳机确认介绍；混合定向暴露的遗留GameMain场景加载由独立UI fixture在UnitySetUp中清理。另保留画廊方法重命名遗漏调用的编译日志，后续已修复。最终扩展定向62/62和PlayMode最终全量均使用这些修正。EditMode定向第二次的Unity在XML保存、输入与许可关闭后退出停滞，确认是本次自建批处理后停止该进程；始终只有一个Unity实例。',
'', '原有TacticalUIGallery与ModeOneScreenshotAdvance同步支持介绍页；画廊各模式预览使用独立服务状态，避免移动靶预览使主菜单路由提前离开。最终画廊工具批处理退出码0，生成10帧；主菜单及四页介绍PNG与七张主证据中的对应文件逐字节一致，见gallery-tool-validation-r2.log与screenshot-manifest.json。', '', '## 截图清单', '',
'所有PNG均为1920×1080、独立预览场景直接Camera.Render，仅包含生成的UI与原UI背景，没有训练场景遮挡；未重绘或拼接截图。右列特写由相机放大该列，保持横向画幅。','']
for name,label in [('Screen_MainMenu.png','主菜单全景'),('Screen_MainMenu_Closeup.png','右侧入口列特写'),('Screen_ModeIntro_Zeroing.png','模式一介绍'),('Screen_ModeIntro_MovingTarget.png','模式二介绍'),('Screen_ModeIntro_Trench.png','模式三介绍'),('Screen_ModeIntro_Urban.png','模式四介绍'),('Screen_MainMenu_Hover.png','01悬停高亮')]:lines.append('- ['+label+']('+rel(name)+')')
lines+=['','悬停状态还见[按钮sprite证据]('+rel('hover-sprite-state.txt')+')；文本尺寸、首选高度、溢出状态见[UI组件检查]('+rel('gallery-inspection.txt')+')。截图尺寸与SHA256见[screenshot-manifest.json]('+rel('screenshot-manifest.json')+')。',
'', '## 修改文件', '']
files=subprocess.check_output(['git','diff','--name-only','-z']).decode('utf-8').split('\0')
files+=[ 'Assets/VRShooting/Tests/PlayMode/ModeIntroTestActions.cs','Assets/VRShooting/Tests/PlayMode/ModeIntroTestActions.cs.meta','docs/BDD/screens/37-各模式操作介绍.feature.md']
for f in sorted(set(f for f in files if f)):lines.append('- `'+f+'`')
lines+=['','证据目录另包含渲染辅助脚本存档、串行批跑脚本、完整日志/XML、比较脚本/CSV/JSON、清理记录和本报告生成脚本；临时Editor渲染源与meta已删除。',
'', '## 工作区清理与实机验收', '',
'批跑前按文件快照保护资源与历史证据，结束后按快照恢复，未使用Git恢复命令。根目录Evidence/P3-production-M67-in-flight.png不在轻量快照范围，已确认它开始时干净，并用只读git show 9a1c012读取原始字节写回、逐字节复核；没有使用checkout/reset/stash。'+str(cleanup['protected_file_count'])+'个非本任务既有文件通过SHA256核对；TrainingChinese.asset、Open XR Package Settings.asset及被测试修改的Prefab/历史截图均恢复。被跟踪InitTestScene*.unity保留，临时新增场景仅在不属于快照时移除，详见[cleanup.json]('+rel('cleanup.json')+')。',
'',
'开始时工作区已有未跟踪 `docs/codex-reports/pico-apk-2026-11-16.md`，原样保留。最终除该已有文件外，仅有本任务源码、测试、文档及证据变更；HEAD保持9a1c012。',
'',
'无VR自动化与纯UI渲染已完成。VR实机仍需人工确认：四个入口的真实射线悬停/离开、每页中文可读性与舒适度、双按钮点击、超长正文滚动、取枪前后原训练流程；当前没有真实头显验收证据，自动化通过不替代实机结论。','']
(ROOT/'docs/codex-reports/mode-intro-pages-2026-11-16.md').write_text('\n'.join(lines),encoding='utf-8')
print('Report written')
