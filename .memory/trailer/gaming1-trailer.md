# Gaming1 引擎内宣传片

## 需求与交付
- 用户要求用当前 48 小时 Game Jam 项目的已有场景、美术、动效与 shader，在 Unity 内制作超过 30 秒的宣传片；交付新场景，运行自动播放，全程仅展示主场景和主流程内容。参考根目录 `demo.mp4`，主入口为 `Assets/Scenes/Gaming1.unity`。
- 新场景：`Assets/Scenes/Gaming1_Trailer.unity`。打开后按 Play 自动播放 **77 秒、21 个镜头**，最后定格在原 BeginPanel 的高楼背景与 Start 标题节点，音乐淡出。再次观看时停止并重新 Play。
- 镜头：原 Logo → 全景 → 程序/美术/音效房间特写 → 五组预算商店翻页及实际购买 → 周事件与选择 → 三类房间行动/火焰悬停/行动点消耗 → 周结算与愿望单数字特写 → 四种结局从坏到好翻页 → 最佳结局弹性缩放与烟花 → 工作室渐暗 → 升空穿云 → 原标题定格和呼吸灯。

## Dialogue Distillation
- 后续要求商店展示多组道具及效果，通过翻页衔接；结尾从工作室渐暗后升空，增加速度线和 2D 云，接 Gaming1 标题界面的 Bg 高楼背景，复用 BeginPanel/Start 节点并上浮，定格后外围呼吸灯循环数秒。
- 最终节奏纠正：**五组，每组停留 1 秒，不额外留阅读效果的时间**。翻页 0.32 秒；商店总镜头 7 秒，含最初弹窗入场。结尾升空最终为 4 秒，标题定格 7 秒（呼吸周期 2.4 秒，影片结束后仍循环）。

- 新增要求：片头展示原 Logo；结算后四种结局由坏到好翻页和缓慢放大，最佳结局弹性回原尺寸并加像素烟花，之后再渐暗升空。
- 最后纠正：用户不接受云雾 shader，恢复原面片云并减少到 4 片；增加速度虚化，保留 4 秒慢—快—慢曲线。云雾 shader 不再参与渲染，旧序列化字段和方法仅为兼容保留。

## 实现入口
- `Assets/Scripts/Trailer/GamingTrailerDirector.cs`：实时导播、实际流程驱动、77 秒分镜、相机推近、UI 特写、字幕、原音乐音效、16:9 构图与 1920×1080 源 RenderTexture。
- 场景包含 Gaming1 的完整副本。原始主场景和现有脚本 API 未修改。只在宣传片内关闭玩家输入和原相机控制；关闭独立全局启动 Logo，改用相同 Resources/Logo.png 在影片内计时播放 3 秒。
- 用原有 GameFlowRunner 自动完成首月作为展示进度，再进入真实商店、事件和行动；不设置虚构分数；四结局段直接展示原结局美术，流程不进入会写排行榜的 EndingState。商店与事件沿用游戏随机逻辑，每次播放的具体内容和数值可能不同。事件优先选择行动点损失较少的真实分支，为三房间演示保留行动点。
- 商店通过 `RefreshBudgetShopBuffOffers(15)` 建立同一个合法候选池，分五页、每页三件调用原 UI 的 `InjectData` 和 `OpenIntroductionWindow`。当前池只有 13 种可抽取道具，最后一页循环补足三件，不伪造道具或属性。购买仍走原按钮回调，选择最后一页可购买的道具。
- `Assets/Editor/GamingTrailerBuilder.cs`：菜单 `Tools/Trailer/Build Gaming1 Trailer`、`Validate Trailer Scene`、`Play and Capture Review`、`Write Runtime Report`。镜头时长和取景参数序列化在导演组件中；镜头动作由 Shot.beat 绑定，重排时保留 beat：Logo=-1；原流程=0–12；结局=15–18；工作室收尾=14；升空=19；标题定格=20。旧的下一周 beat=13 保留但本版未使用。
- `Assets/Trailer/BuffCard_Trailer.prefab`：原 Buff 卡牌的宣传片专用副本，补上原预制体缺少的价格文本引用，不修改源预制体。
- 字体使用现有 fusion pixel；BGM 使用 `Assets/ArtRes/后.mp3`；音效使用现有选项、收银机、盖章等素材。未新增外部图片或视频。
- `TrailerPresentation.cs`：截取前一页 RenderTexture，通过 `Assets/Trailer/TrailerPageTurn.shader` 制作卷页与折面阴影；工作室渐暗、源相机上升、原 Bg 的位移/缩放衔接、原 Start（包含文字子节点）的上浮。标题节点保留美术，关闭交互。
- `TrailerCloudGraphic.cs`：4 个稀疏像素面片云，36 条垂直速度线；云片采用 9 次加权纵向采样几何拖影，速度越快虚化越强。`TrailerVerticalMotionBlur.shader` 对高楼背景做随速度变化的 9 次纹理采样纵向模糊，减速后清晰。`TrailerTitleGlow.shader` 绘制 Start 外围呼吸灯。活动 shader 引用均保存在场景里。
- 升空总长 4 秒，统一五次平滑曲线驱动相机距离、云位移和速度线，慢—快—慢；暗场中段加速，标题入画后减速。背景与 Start 的最终位置、缩放恢复原值。
- `TrailerEndingGallery.cs`：从 Gaming1 的 EndPanelAnimator 读取原 result/resultBg/idle 的 12 张图，按 storm → buggy → boutique → hit 重排展示；前 3 张各 2.4 秒并缓慢放大，最佳结局 4.8 秒，先放大至 1.10，再以衰减弹簧曲线回到 1。`TrailerFireworksGraphic.cs` 绘制 7 次像素烟花爆发。结局翻页 0.55 秒。

## 关键处理
- 源 UI 转为 ScreenSpaceCamera，Canvas 排序 20000；世界卡牌排序 21000，解决原精灵遮挡弹窗。
- 卡牌在特写中按实际 RectTransform 边界重定位，保留原扇形展开、浮动、火焰与悬停响应；HUD 金币和 Next 上移，避免和字幕重叠。
- 播放计时从 Start 后独立测量，避免编辑器启动的长首帧跳过片头。每帧计时增量上限 0.1 秒，严重卡顿时会延长实际播放时间而不会跳过镜头。
- 16:9 film 根节点使用 RectMask2D，升空时放大的背景和云层不会溢出有效画幅。
- 编辑器工具通过项目自带 Unity MCP 的 localhost TCP 6400 接口执行；会话未直接提供 Unity MCP 工具列表。该连接不属于运行时依赖。

## 当前版本验证（2026-10-12）
- Unity 2022.3.62f1c1 编译通过，场景 77 秒 / 21 镜头，Missing scripts=0；Logo、12 张结局图、原标题 Bg/Start 和活动 shader 引用齐全。
- 最新完整播放结束：Finished=True，Elapsed=Duration=77.00001，Shot=20。日志确认四结局按 storm → buggy → boutique → hit 展示，无运行时异常或脚本/shader 编译错误。
- 实际购买后 buffs=1；三个房间依次消耗 AP 至 3、2、0。最终愿望单 370016（随机结果，不作为固定值断言），流程停留 WeekResolve，不写排行榜。
- 逐帧检查原 Logo、四结局、翻页中间态、最佳结局缩放回弹和烟花；检查 4 秒升空的暗场、加速虚化、减速恢复清晰、Start 上浮与呼吸灯。运行时查到 TrailerCloudGraphic 实例恰好 4 个，没有云雾层。
- 截图比较确认标题呼吸轮廓持续变化，8 张升空截图均在 16:9 画幅内；最高速截图可见纵向云拖影与建筑虚化，最终定格清晰。
- 修复了审核截图与每两秒常规截图同帧竞争导致遗漏的问题：关键截图优先，普通截图顺延。最新预览 `outputs/trailer-review/logo-endings-flight.jpg`，商店补充预览 `shop-and-ending.jpg`，结尾单图 `title-finale.png`。
- `outputs/trailer-review/runtime.txt`、`scene-validation.txt`、`playback-validation.txt` 为最终校验；`work/trailer_review.py` 整理预览并校验完整镜头、结局顺序、动态呼吸灯和画幅边界。
- 未执行独立 Player 打包；交付为 Unity 场景。编辑器停止播放后保留 Gaming1_Trailer，按 Play 从头播放。原 55 秒/68 秒版本已经替代。
- 参考文档：`Docs/GameFlowSystem.md` 用于核对公开流程接口，实际绑定以当前脚本和场景为准。
