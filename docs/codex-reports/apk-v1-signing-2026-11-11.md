# PICO APK v1 签名交付报告

日期：2026-11-11。基线提交为 `2a2ada6ce338452d055c9fbccc13ef9ece912c77`；本次改动未提交、未推送。

## 代码和文档改动

- `Assets/Editor/BuildPicoApk.cs`：`BuildApk` 在 Unity 出包成功且 APK 非空后调用 `EnsureAllSigningSchemes`。该方法从 `EditorApplication.applicationContentsPath` 定位 Unity 附带的 Android OpenJDK 和 SDK build-tools，以数值比较选择最高版本的 build-tools；找不到工具时明确报错。预发布版本中的数字也按数值比较，例如 `rc10` 排在 `rc2` 之后。
- debug 补签使用用户目录 `.android/debug.keystore` 及 Android 默认 debug alias/密码；release 复用现有四个 `PICO_KEYSTORE_*` / `PICO_KEY_ALIAS*` 环境变量。既有 `SigningConfiguration.Apply` 和 `Dispose` 的设置保存、恢复逻辑保持不变。
- 补签进程启用 v1、v2、v3，并关闭只对应临时文件名的 v4 `.idsig` 边车；标准输出和错误输出并发读取，参数按 Windows 规则引用及处理末尾反斜杠。临时 APK 经 `verify --min-sdk-version 21 --verbose` 确认 v1/v2/v3 全部为 `true` 后才通过同目录 `File.Replace` 原子替换原文件；失败时保留已生成的临时 APK。日志记录三项结果、最终字节数和 SHA-256。
- `docs/构建/打包PICO-APK.md`：更新 Android 模块已安装及实测出包的状态，解释 PICO 对 v1 的要求和手工校验方式；批处理示例改用 `C:\vrproj` 联接且不使用 `-nographics`。

## 最终构建

- APK：`E:\新建文件夹 (3)\SimulateShooting\Build\PICO\SimulateShooting-pico-20261111-2124.apk`，也可通过 `C:\vrproj\Build\PICO\SimulateShooting-pico-20261111-2124.apk` 访问。
- 大小：`420025294` 字节。
- SHA-256：`76d32098d44631304fe30ddb0e25a7487639a9d306d18dbebfb50b4233057a9e`。
- Unity 2022.3.62f3c1，debug 签名，`-batchmode -projectPath "C:\vrproj" -buildTarget Android -executeMethod BuildPicoApk.Build`，未使用 `-nographics`。Unity 退出码 `0`，从启动到退出耗时 **155.04 秒**；其中 Unity 日志所记 Gradle 阶段约 **28.68 秒**。
- 本次最终构建日志未发现 Gradle 依赖解析、下载、超时或连接失败，也没有因网络问题重试。环境中已有 `HTTP_PROXY` / `HTTPS_PROXY`，未另外设置 `GRADLE_OPTS` 或 `JAVA_TOOL_OPTIONS` 指向 `127.0.0.1:7890`；未验证 Gradle 是否使用了继承的代理。
- 构建日志为 `Logs/pico-build-v1-delivery-20261111.log`；独立签名检查原始输出为 `Logs/pico-v1-delivery-apksigner.txt`。`Logs/` 和 `Build/` 为本地构建产物，不作为本报告的 Git 修改。

使用 Unity 附带的 build-tools 34.0.0 独立执行 `java -jar apksigner.jar verify --min-sdk-version 21 --verbose --print-certs <apk>`，退出码 `0`，输出包括：

```text
Verifies
Verified using v1 scheme (JAR signing): true
Verified using v2 scheme (APK Signature Scheme v2): true
Verified using v3 scheme (APK Signature Scheme v3): true
Verified using v3.1 scheme (APK Signature Scheme v3.1): false
Verified using v4 scheme (APK Signature Scheme v4): false
Verified for SourceStamp: false
Number of signers: 1
Signer #1 certificate DN: C=US, O=Android, CN=Android Debug
Signer #1 certificate SHA-256 digest: 7e50267bfcf3d179a540788617aa8e9b5e4cc8af0b73a7dc52ef26149f19206f
```

该证书摘要与用户此前确认可安装的 `SimulateShooting-pico-20261111-v123.apk` 一致。检查输出另有以下两条警告，在此前的 `v123` 包上也能复现：

```text
WARNING: META-INF/com/android/build/gradle/app-metadata.properties not protected by signature. Unauthorized modifications to this JAR entry will not be detected. Delete or move the entry outside of META-INF/.
WARNING: META-INF/com.android.games.engine.build_fingerprint not protected by signature. Unauthorized modifications to this JAR entry will not be detected. Delete or move the entry outside of META-INF/.
```

警告涉及两个未受 JAR 签名保护的 `META-INF` 元数据条目；本包的 v2/v3 校验均通过。目标设备的实际安装和验证行为仍需实机确认。

`aapt dump badging` 和 `zipalign -c -p 4` 检查结果：

```text
package: name='com.vrtraining.pico4' versionCode='1' versionName='0.1' platformBuildVersionName='13' platformBuildVersionCode='33' compileSdkVersion='33' compileSdkVersionCodename='13'
sdkVersion:'29'
targetSdkVersion:'33'
native-code: 'arm64-v8a'
zipalign exit code: 0
```

最终 APK 没有产生对应的临时 `.idsig` 边车。

## 测试和工作区

- 用已存在的 `v123` 包和同一 debug 证书生成临时 v2-only 对照包：`apksigner verify --min-sdk-version 21` 返回退出码 `1`，报告 `Missing META-INF/MANIFEST.MF`；以 min SDK 29 检查时明确显示 v1=`false`、v2=`true`。最终 APK 的同一检查则成功。
- 在系统临时目录使用 Unity 附带的 Mono/Roslyn 编译真实 `BuildPicoApk.Build` 和 Unity、签名工具替身，**12 项通过**：debug/release 参数及设置还原、中文路径和含引号/末尾反斜杠的参数、build-tools 数值排序、并发读取大量标准输出/错误输出、v1/v2/v3 分别校验失败及 v3 缺失、工具及进程失败、原 APK 和失败临时文件保留。该隔离测试使用模拟 release 凭据，不代表已用真实 release keystore 构建；测试代码位于 `%TEMP%\pico-signing-public-api-tests`，未添加到仓库。
- Unity EditMode 冒烟测试 `VRShooting.Tests.EditMode.Infrastructure.Infrastructure_ServiceResultTests`：**3/3 通过**，结果位于 `Logs/pico-signing-editmode.xml`。这是现有服务契约冒烟测试；签名专用覆盖来自上述隔离测试和实际 APK 构建/独立校验。
- 已恢复本次 Unity 运行自动改写的 URP、XR 和 `ProjectSettings/ProjectSettings.asset` 文件；构建前工作区为空。保留用户原有的 `v123` 包，清理本次中间构建包及其孤立边车。

## 尚待验证

- 新的 `2124` APK 尚未在 PICO 4 上安装或运行；请由用户确认安装、启动、OpenXR/手柄交互和训练流程。此前可安装的是相同证书重签的 `v123` 包，不代表本次 APK 已通过实机验收。
- 未使用真实 release keystore 进行完整构建；该分支仅通过隔离替身测试。
