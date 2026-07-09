# Unity UGUI Sprite Workflow Notes

更新时间：2026-07-08

## 结论

当前项目的 P1 UI 应使用真实 UGUI 组件组合，而不是整张参考图贴底：

- 将按钮、面板、HUD 胶囊框、警示框等切成独立 PNG。
- PNG 在 Unity 中导入为 `Sprite (2D and UI)`。
- 可伸缩矩形使用 9-slice：Sprite 设置 `border`，UGUI `Image.type = Sliced`。
- `Button` 的 `targetGraphic` 指向按钮 `Image`，可用 Sprite Swap 为 Normal / Highlighted / Pressed / Disabled 配不同 Sprite。
- 截图式参考图只作为视觉目标，不直接出现在运行时 UI 层级。

## 官方资料摘要

Unity 的 `Image` 组件要求 `Source Image` 是以 Sprite 形式导入的纹理。`Image.type` 可用 Simple、Sliced、Tiled、Filled 等模式，其中 Sliced 用于可伸缩的装饰 UI 矩形。

9-slice 的核心是把 Sprite 分成九个区域：四角保持原尺寸，四边和中心按需要伸缩或平铺，使同一张面板/按钮图可以适配不同宽高，而不拉坏角部细节。

Unity 的 Sprite Editor 可以为 Sprite 设置 left/top/right/bottom border；UGUI 的 `Image.Type.Sliced` 正常工作需要 Sprite 有 `border`。

UGUI Button 支持 Sprite Swap transition：Normal 使用 target graphic 当前 sprite，Highlighted / Pressed / Disabled 可指定不同 sprite。

Canvas 多分辨率适配应使用 `CanvasScaler.ScaleWithScreenSize` 和统一参考分辨率；本项目当前 `1920 x 1080` 设置是合理的。

## 本项目应用规范

- 参考分辨率：1920 x 1080。
- 资产目录：`Assets/VRShooting/Art/UI/TrainingGenerated/`。
- 运行时加载：优先通过 Editor 导入设置和序列化引用；当前脚本生成 UI 可用 `AssetDatabase.LoadAssetAtPath<Sprite>`（Editor）加 `Resources.Load<Sprite>` 兜底。
- 面板类 Sprite：
  - `Image.type = Sliced`
  - `pixelsPerUnitMultiplier = 1`
  - border 建议 28-48 px，取决于外框厚度。
- 按钮类 Sprite：
  - 使用三态：normal / highlighted / pressed
  - disabled 可使用同一 sprite 加低 alpha/灰色，或单独 disabled sprite。
- 图标：
  - 独立透明 PNG，`Image.type = Simple`，`preserveAspect = true`。
- 动态数据文本：
  - 继续使用 TextMeshPro，不烘焙进图片。

## 来源

- Unity Manual: Sprite (2D and UI)
  https://docs.unity3d.com/2022.1/Documentation/Manual/texture-type-sprite.html
- Unity UI Manual: Image
  https://docs.unity3d.com/2022.3/Documentation/Manual/script-Image.html
- Unity Scripting API: Image.Type.Sliced
  https://docs.unity3d.com/560/Documentation/ScriptReference/UI.Image.Type.Sliced.html
- Unity Manual: Sprite Editor
  https://docs.unity3d.com/2021.2/Documentation/Manual/SpriteEditor.html
- Unity UI Manual: Selectable Transition / Sprite Swap
  https://docs.unity3d.com/2022.3/Documentation/Manual/script-SelectableTransition.html
- Unity Manual: Designing UI for Multiple Resolutions
  https://docs.unity3d.com/460/Documentation/Manual/HOWTO-UIMultiResolution.html
