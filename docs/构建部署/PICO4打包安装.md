# PICO 4 打包与安装

Unity菜单：VR Shooting → PICO 4。
- 配置 Android 与 OpenXR：ARM64 / IL2CPP / GLES3、Android API 29–33、官方PICO OpenXR插件。
- 打包 APK：平台切换与重编译完成后自动恢复构建，包含Build Settings启用场景且MainScene为入口。
- 打包并安装启动：成功构建后，对唯一ADB已授权设备执行install -r并启动。
- 检查设备 / 安装已有 APK 并启动：分别查询ADB状态、部署已生成APK。

输出：Builds/PICO4/VRTraining-PICO4.apk；包名com.vrtraining.pico4。默认开发构建方便查看日志，不自动卸载已有应用或删除训练数据，不生成AAB。

需要：Unity 2022.3.62f3c1 Android Build Support，以及对应SDK、NDK r23b、JDK11。PICO OpenXR SDK固定为官方仓库提交3aa3e62bff41df618529eeb60ff02c29a515dafe。PICO 4必须启用USB调试并在头显中允许电脑调试。
MTP位置“此电脑/PICO 4”不是可执行安装的文件系统路径；APK安装使用ADB。

2026-10-07设备检查：已授权PICO A8110，Android API29，/data可用43GB。尚未生成APK，安装/头显启动不能标记成功。未运行测试用例、未提交。

## 2026-10-07 本机交付验证
- Android ARM64/IL2CPP/OpenGLES3 APK构建Succeeded。
- 输出：Builds/PICO4/VRTraining-PICO4.apk，实际压缩文件438460539字节（约418MB）；BuildReport统计的1786150025字节为构建数据量。
- ADB install -r成功；设备pm path确认包com.vrtraining.pico4已安装；monkey与am start成功启动UnityPlayerActivity。
- 应用进程PID16686持续存在，日志确认OpenXR 1.0实例成功创建、会话进入FOCUSED并有合成帧统计；未发现应用FATAL EXCEPTION/Unity Exception。
- 头显未佩戴/失去焦点时会进入VISIBLE/Paused。ADB普通截图为黑色，不能作为双眼画面正常的证明。画面可读性、手柄输入、玩法及性能仍需要头显实机确认。
- 已保存Builds/PICO4/device-startup.log及deployment-result.txt（含SHA256），APK启动检查不等同完整VR验收。未运行测试用例，未提交/推送。
