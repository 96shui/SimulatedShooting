# PICO 4 APK 构建与侧载

适用于本机 Windows PowerShell、Unity 2022.3.62f3c1 中国版和仓库 `E:\新建文件夹 (3)\SimulateShooting`。本文以新入口 `BuildPicoApk.Build` 为准；`Assets/SimulatedShooting/Editor/Pico4BuildTool.cs` 是另一套既有交互/异步工具，不能混同输出。

## 当前状态与验证边界

- 本机缺 Android Build Support 和 Android SDK，**目前不能实际构建 APK**；缺失模块导致构建失败是预期结果。
- 本次验证：同一 Unity 版本及仓库对应版本 XR 包（含 PICO）在隔离临时工程中 batchmode 编译通过；使用与本文相同的 `-buildTarget Android` 启动 batchmode，调用 `BuildPicoApk.Build` 因缺 Android 模块按预期以退出码 1 结束。隔离工程的签名分支测试覆盖默认 debug、拒绝非法模式、release 环境变量配置与设置恢复，测试退出码 0。
- 上述结果**不代表原项目已编译、真实 keystore 签名成功、APK 已生成或真机验收通过**。原项目编辑器正打开；尚未对原项目并行启动 Unity。

## 前置条件

1. 编辑器固定为 `C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe`。需已登录 Unity Hub，且该版本已有有效 Unity 许可。安装模块需要网络、磁盘空间和目录写权限；SDK 等许可可能需要用户交互确认。
2. 为**同一编辑器版本**安装 Android Build Support、Android SDK & NDK Tools 和 OpenJDK。Unity 2022.3 对应 OpenJDK 11、NDK r23b（23.1.7779620）；SDK 使用 Hub 提供版本，2022.3 手册列出的 Build-tools 最低版本为 32。确认 `SDK\platforms\android-33` 存在。依据：[Unity Android 环境](https://docs.unity3d.com/cn/2022.3/Manual/android-sdksetup.html)、[Unity 依赖版本表](https://docs.unity.com/en-us/engine/6000.3/manual/platform-specific/android/getting-started/sdksetup/supported-dependency-versions)。
3. 确保仓库依赖可解析，包括 Git 固定的 PICO OpenXR 包。Git 客户端须可用并位于 `PATH`；网络受限时按本地网络策略配置代理，不要修改锁定版本。参见 [Unity Git 依赖](https://docs.unity3d.com/cn/2022.3/Manual/upm-git.html)。
4. PICO 4 依次进入 **设置 → 通用 → 关于本机**，连续点击软件版本号直至出现 **开发者**，打开 **USB 调试**。以数据线连接电脑，并在头显弹窗中允许此电脑调试。[PICO 官方步骤](https://developer-cn.picoxr.com/document/unity/set-up-the-development-environment/)。
5. 本项目 `minSdk=29`、`targetSdk=33`。设备 Android API 不低于最终 APK 的 minSdk 是安装的必要条件；**targetSdk 33 不意味着设备必须是 Android 13 或 API 33**。targetSdk 主要指定兼容行为；满足 minSdk 不保证运行时和插件兼容。[Android uses-sdk 说明](https://developer.android.com/guide/topics/manifest/uses-sdk-element)。
6. 设备固件要求须单独核实：仓库锁定提交 `3aa3e62bff41df618529eeb60ff02c29a515dafe` 的 [PICO 随包 README](https://github.com/Pico-Developer/PICO-Unity-OpenXR-SDK/blob/3aa3e62bff41df618529eeb60ff02c29a515dafe/README.md) 是 **SDK v1.4.0** 说明，列出 PICO system **5.13.0 或更高**；同一提交的 [package.json](https://github.com/Pico-Developer/PICO-Unity-OpenXR-SDK/blob/3aa3e62bff41df618529eeb60ff02c29a515dafe/package.json) 自报 **1.4.1**。尚未找到针对 1.4.1 的明确最低系统版本，不能把 v1.4.0 说明断言为 1.4.1 的最低值；这是 SDK 要求，不是 targetSdk 33 的通用规则。请向 PICO 核实 1.4.1 的固件支持及适用商店规则。

## 安装 Android 模块

本次交付不执行安装。用户确认许可、网络和空间后，可在 PowerShell 执行如下 [Unity Hub CLI](https://docs.unity.com/en-us/hub/hub-cli-reference) 命令。`-m android` 指定 Android Build Support，`--childModules` 请求安装子模块；官方列出的子模块包括 `android-sdk-ndk-tools` 和 `android-open-jdk`。

```powershell
& 'C:\Program Files\Unity Hub\Unity Hub.exe' -- --headless install-modules --version 2022.3.62f3c1 -m android --childModules
```

上述语法来自官方 Hub CLI；本机 Hub 3.3.6 **对中国版 `2022.3.62f3c1` 的解析、无交互许可处理及命令能否完成尚未证实**。先前只读 headless help 未在等待时间内返回，未执行安装。如果找不到版本、长时间无输出、要求许可或安装不完整，请查看 Hub 日志和许可状态，并由用户在 Unity Hub 的 **安装 → 该编辑器的管理 → 添加模块** 中勾选上述三个组件，按提示完成。官方指出追加模块要求编辑器由 Hub 安装，模块可用性取决于版本。不要因此重装编辑器或安装 Android Studio。

本版本下载和安装大小尚未测量。[Unity 官方 2021.3.7f1 示例](https://connect-prd-cdn.unity.com/20221020/eb12f246-a0f4-4960-b5ab-c0f2bc0a8af0/Meta%20Quest%20Deployment%20Notes.pdf#page=5)的 Android 三项合计约 **2 GB 下载、6 GB 以上安装空间**，仅供量级参考；还应预留导入与 IL2CPP 构建空间。

安装后进行只读检查；以下 AndroidPlayer 子目录和工具在模块安装前并不存在。目录应全部为 `True`，JDK 应为 11，NDK 为 23.1.7779620，Android 33 平台应存在：

```powershell
$androidPlayer = 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Data\PlaybackEngines\AndroidPlayer'
'SDK', 'NDK', 'OpenJDK' | ForEach-Object { $path = Join-Path -Path $androidPlayer -ChildPath $_; '{0}: {1}' -f $path, (Test-Path -LiteralPath $path -PathType Container) }
& 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\java.exe' -version
Get-Content -LiteralPath 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Data\PlaybackEngines\AndroidPlayer\NDK\source.properties'
Test-Path -LiteralPath 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platforms\android-33' -PathType Container
```

若缺 Android 33 平台，安装模块后可由用户执行下列 PowerShell 块：它在该编辑器 SDK 内查找 `sdkmanager.bat`，临时使用 Unity 附带的 JDK 安装 API 33 平台，并在提示时由用户阅读和接受许可。需要联网；向 `Program Files` 写入可能需要在管理员 PowerShell 中执行。如果找不到工具，请回到 Hub 补齐 Android SDK & NDK Tools；不要假定 batchmode 会自动完成下载。

```powershell
& {
    $androidPlayer = 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Data\PlaybackEngines\AndroidPlayer'
    $sdkRoot = Join-Path $androidPlayer 'SDK'
    $sdkManager = Get-ChildItem -LiteralPath $sdkRoot -Filter 'sdkmanager.bat' -File -Recurse -ErrorAction Stop | Select-Object -First 1
    if ($null -eq $sdkManager) { throw '未找到 sdkmanager.bat，请先安装 Android SDK & NDK Tools。' }
    $previousJavaHome = $env:JAVA_HOME
    try {
        $env:JAVA_HOME = Join-Path $androidPlayer 'OpenJDK'
        & $sdkManager.FullName "--sdk_root=$sdkRoot" '"platforms;android-33"'
        if ($LASTEXITCODE -ne 0) { throw 'Android 33 平台安装失败，请检查网络和许可。' }
    } finally { $env:JAVA_HOME = $previousJavaHome }
}
```

安装后重新运行上面的 `Test-Path` 检查。

## 选择签名

默认使用 Unity debug keystore，可用于测试侧载，无须仓库预置 keystore。若同一 PowerShell 会话之前启用过 release，先运行：

```powershell
$env:PICO_BUILD_SIGNING = 'debug'
```

可选：使用已有 release keystore。交互输入真实 keystore 绝对路径和别名，密码输入不回显；不会创建密钥或将密码写入仓库。然后在**同一 PowerShell 会话**执行构建。

```powershell
$env:PICO_BUILD_SIGNING = 'release'
$env:PICO_KEYSTORE_PATH = Read-Host '请输入已有 keystore 文件的绝对路径'
$env:PICO_KEY_ALIAS = Read-Host '请输入 key alias'
$env:PICO_KEYSTORE_PASSWORD = [System.Net.NetworkCredential]::new('', (Read-Host '请输入 keystore 密码' -AsSecureString)).Password
$env:PICO_KEY_ALIAS_PASSWORD = [System.Net.NetworkCredential]::new('', (Read-Host '请输入 key alias 密码' -AsSecureString)).Password
```

release 必须提供以上全部变量且路径指向已有文件。密码仍以明文环境变量存在于当前进程和子进程，勿粘贴到聊天、日志或版本库。构建后可清除当前进程的这些变量，**不会删除 keystore 文件**：

```powershell
'PICO_BUILD_SIGNING', 'PICO_KEYSTORE_PATH', 'PICO_KEYSTORE_PASSWORD', 'PICO_KEY_ALIAS', 'PICO_KEY_ALIAS_PASSWORD' | ForEach-Object { [Environment]::SetEnvironmentVariable($_, $null, 'Process') }
```

## 批处理构建

**先保存并关闭当前打开的本项目 Unity 编辑器**，确认没有其他 Unity 进程占用同一项目，再在 PowerShell 复制执行这一整行。Unity 同项目锁会阻止第二进程；`Start-Process -Wait` 等待结束并读取真实退出码，避免构建仍在进行时侧载。

```powershell
$p=$null; New-Item -ItemType Directory -Path 'E:\新建文件夹 (3)\SimulateShooting\Logs' -Force -ErrorAction Stop | Out-Null; $p=Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe' -ArgumentList @('-quit','-batchmode','-nographics','-projectPath','"E:\新建文件夹 (3)\SimulateShooting"','-buildTarget','Android','-executeMethod','BuildPicoApk.Build','-logFile','"E:\新建文件夹 (3)\SimulateShooting\Logs\pico-build.log"') -WindowStyle Hidden -Wait -PassThru -ErrorAction Stop; $p.ExitCode
```

必须保留 `-buildTarget Android`。[Unity 2022.3 官方说明](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/EditorUserBuildSettings.SwitchActiveBuildTarget.html)指出 `SwitchActiveBuildTarget` 不能在 batchmode 切换平台，必须在启动命令中预选；脚本发现 batchmode 未预选 Android 时会退出并提示此参数。在 Windows PowerShell 中直接 `& Unity.exe ...` 可能立即返回且 `$LASTEXITCODE` 为空，不应以此判定成功。

脚本使用现有启用场景，首场景必须为 `Assets/Scenes/MainScene.unity`；设置包名 `com.vrtraining.pico4`、IL2CPP、仅 ARM64、minSdk 29、targetSdk 33，关闭 APK expansion，生成单 APK。脚本启用 Android OpenXR loader、PICO Support、PICO Extensions 和 PICO4 控制器 feature，禁用冲突 Android loader/feature 并保存 XR 配置；实际构建会改动项目设置，开始前请保存工作区。

退出码 **0** 且日志确认构建成功后，脚本打印 APK **绝对路径和文件字节数**。输出文件格式为 `E:\新建文件夹 (3)\SimulateShooting\Build\PICO\SimulateShooting-pico-<yyyyMMdd-HHmm>.apk`；同一分钟重复构建可能复用文件名。非零退出码时查看 `E:\新建文件夹 (3)\SimulateShooting\Logs\pico-build.log`，不要把旧文件当作本次产物。

## 侧载和启动

安装模块后，ADB 预期位于该编辑器的 `AndroidPlayer\SDK\platform-tools\adb.exe`。只连接一台设备；检查状态并读取 Android API。若显示 `unauthorized`，在头显中确认授权。

```powershell
& 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe' devices -l
& 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe' shell getprop ro.build.version.sdk
```

仅在上节构建成功且仍在同一 PowerShell 会话时运行以下块：它从本次日志读取脚本报告的 APK 路径，不靠旧文件时间猜测。`adb install -r` 在签名兼容时覆盖安装并保留应用数据，参见 [Android ADB 文档](https://developer.android.com/tools/adb)。

```powershell
& {
    if ($null -eq $p -or $p.ExitCode -ne 0) { throw '本会话没有成功的构建；请先完成批处理构建。' }
    $apkLine = Select-String -LiteralPath 'E:\新建文件夹 (3)\SimulateShooting\Logs\pico-build.log' -Pattern '\[BuildPicoApk\] APK: (.+) \(([0-9]+) bytes\)' | Select-Object -Last 1
    if ($null -eq $apkLine) { throw '日志中没有成功的 APK 路径；不要安装旧文件。' }
    $apkPath = $apkLine.Matches[0].Groups[1].Value
    if (-not (Test-Path -LiteralPath $apkPath -PathType Leaf)) { throw '日志中的 APK 不存在。' }
    & 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe' install -r $apkPath
    if ($LASTEXITCODE -ne 0) { throw 'ADB 安装失败；请查看上方错误。' }
}
```

`Success` 只说明安装完成。佩戴头显后在应用库按项目显示名称寻找；部分固件可能把侧载应用放在其他或未知来源分类，具体入口**尚未对目标固件验证**。找不到时先在设备应用管理中确认安装状态。MTP 里的设备位置不是电脑本地可执行路径。

若本机已有 PICO 助手、PICO 开发者中心或其他安装器，可查阅其对应版本说明并选择日志中的 APK；本次未证实这些工具是否有任意 APK 安装按钮及具体入口，也不要求额外安装它们。[PICO 开发者中心](https://developer-cn.picoxr.com/document/pico-unity-quickstart/pdc-basic-info/)集成 ADB 调试；其文档所列系统版本属于该工具的使用要求，不应套用为本 APK 的 targetSdk 要求。

## 常见问题

- **项目被占用或日志不可写**：关闭同项目编辑器，检查日志目录和权限；不要并行运行两个项目进程。
- **Android 模块或 API 33 缺失**：先完成模块和路径检查；许可、Hub 中国版解析及 SDK 更新可能需要用户处理。
- **签名冲突或版本降级**：同包名应用可能使用不同证书或更高版本号。核对现有签名、数据和版本，使用相同证书及兼容版本；不要自动卸载或清数据。
- **非 ARM64 或不是单 APK**：确认调用新入口而非旧工具，检查日志和最终 APK 是否真的包含 ARM64 原生库。
- **OpenXR 初始化失败或 PICO feature 缺失**：确认锁定包可解析，检查脚本日志、Android OpenXR loader、PICO Support、PICO Extensions、PICO4 控制器及冲突项，核对设备固件并实机验收；编译通过不能保证 XR 会话正常。
- **targetSdk 与固件或商店规则**：读取设备 API，检查最终 APK 的 minSdk、SDK 固件要求及实际错误。API 低于 33 不自动意味着 targetSdk 33 不能安装；特定商店或固件的额外限制须查官方规则。
- **Git 依赖拉取失败**：确认 Git 在 `PATH`、锁定提交可访问、证书及代理设置符合网络策略；不要随意更新 PICO 包。
- **首次 IL2CPP 较慢或下载失败**：首次导入和原生编译可能较久；缺失 SDK 平台、Gradle 或包依赖可能触发额外下载和许可提示。检查网络、磁盘及日志，不假定每次都必须下载。
- **怀疑 Library 或 obj 缓存异常**：先关闭 Unity、备份并确认需要保留的数据，结合日志定位；如需处理 `Library/` 或 `obj/`，由用户确认后手动备份或移走，让 Unity 重建。不要自动删除项目数据。

## 实机验收

安装后在 PICO 4 上确认应用启动、OpenXR 会话初始化、双手柄追踪和交互，并检查双眼画面、主要训练流程与性能。编译成功、ADB 安装成功或应用进程存在均不能代替佩戴头显的验收。
