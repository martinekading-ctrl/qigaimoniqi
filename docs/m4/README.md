# M4 早餐、开局内容、基础休息与存档

> Unity 6.3 LTS **6000.3.25f1**（e1dba0a9aba4）｜C#｜Windows x64｜原M1灰盒场景继续使用｜待负责人审核和Windows人工试玩

负责人在下一版范围说明后明确要求“直接开始下一版的开发吧”。本版按[总规划M4](../PROJECT_PLAN.md)、[P0五幕规格](../p0/P0_SPEC.md)、[决策D-01及本轮记录](../p0/REVIEW_AND_DECISIONS.md)和[验收清单](../p0/ACCEPTANCE.md)继续开发，不重建工程，不改变七区20点位、原地图、碰撞、镜头、测试重量或废品价格。

M3 PR #5在本轮开工时仍未合并。本工作分支从最新main建立后快进至M3已交付的`61209ee`，继续追加M4；暂以M3分支为PR基线，避免在M4差异中重复审核M3。未代替负责人合并任何PR。M1/M2/M3历史记录保留，M4回归结果以[本轮验证](VALIDATION.md)为准。

## 打开与运行

Unity Hub选择仓库根目录下的`Unity/`，使用准确的`6000.3.25f1`。打开`Assets/Scenes/M1Greybox.unity`并Play，仍从A区原铺盖出生。工程没有第三方插件、新依赖或新的启动场景。

Windows完整试玩目录为仓库本机`Unity/Builds/M4-Windows/`，压缩包为`Unity/Builds/Reborn1985-M4-Windows-x64.zip`。解压整个包，保持`Reborn1985.exe`、`UnityPlayer.dll`、`Reborn1985_Data/`等相对位置，双击exe运行；不能单独复制exe。完整包由本机生成，未提交Git。保留目录内`试玩说明.txt`，关闭窗口退出。

每次启动先进入新运行；如需继续上次进度，请按F9并确认读取。没有自动保存，退出前请按F5。

## 操作与路线

| 操作 | 按键/界面 |
| --- | --- |
| 移动、朝向 | WASD或方向键，沿用M1 |
| 近距离交互 | E；拾取/搜索、陈伯、林婶、环境点、原铺盖共用入口 |
| 确认、返回 | Enter确认当前可执行操作，Esc关闭窗口；走远也会关闭对应地点的交互 |
| 背包与丢弃 | Tab/I开关；1—4选行，X丢弃普通废品一件；也可点击丢弃按钮。家书行的按钮改为阅读，X仍不能丢弃家书 |
| 家书、线索 | L查看家书摘要，J查看已经记录的线索；均可跳过，不阻挡行动 |
| 保存、读取 | F5手动保存；F9打开读取确认，再Enter/点击确认。左上有同等按钮 |

可自由先去C区林婶，无钱也能询问；或先按M2路线取B-02/D-01两纸、D-02/D-03两瓶，共3kg，在G-04陈伯秤前E询价、Enter确认，得到44分。回C-01交互位置购买26分基础餐，直接进食、记录一份已购餐，现金剩18分。不是库存中的可倒卖食物，不增加饥饿/健康/心情系统。所有金额继续整数分计算；26分与废品价都是暂定测试值，并非考证的1985年物价。

钱少于26分、且帮工餐未领时，可在林婶窗口点“询问一次性帮工”，到**既有C-04**按E并确认整理摊具，获得并吃下一份基础餐，不发现金。领取后不会因重复对话、重按确认、离开返回或读取已领取存档重新发放。读取领取前的旧档会完整回到未完成状态，在该状态中仍只能完成一次。具体整理动作/表现是本轮最小原型，待试玩确认。

A-02旧报纸、A-03积水、B-01公告可选查看；E-01旧壶和E-02居民只记录消息，没有旧壶商品、价格、议价、购买或转卖。开局只用简短提示表现现代创业失败后的陌生处境，迅速保留控制。**家书正文尚未确认，本版显示明确标注的已确认背景摘要；不是擅写的信件终稿。** 具体月日未冻结，显示“1985年·第N日·清晨”，没有自行编造日历或实时钟。

回到**A-01原有破旧铺盖**，E后确认免费休息，日序加1，不扣钱，不代表有住宅。一次打开窗口只能确认一次休息，关闭再主动开启可再次休息。**M4休息后来源领取标记、现存地面物保留；资源次日有限刷新、上日丢弃物清理与完整经济失败恢复留M5。** 界面会说明当前限制，不能把本版当成已完成跨日循环的P0。

## 存档规则与数据

仅一个手动槽。正常Windows路径为`%USERPROFILE%/AppData/LocalLow/QigaiPrototype/Reborn1985/Saves/save.json`，旁有上一份有效`save.json.bak`；实际路径也记录在运行验证JSON的`normalSavePath`。从首次正式存档版起固定产品名`Reborn1985`，后续里程碑沿用该路径。旧M1/M2/M3没有正式磁盘存档，未假装提供旧档迁移。

保存字段包含：场景与参数签名、位置/朝向、日序、现金/累计废品收入/餐费、背包独立物品身份、来源领取状态、地面身份/位置/产生日期、已售身份与收据、开局/家书/年代/积水/旧壶标记、已购餐数、帮工接受/完成/领取标记。临时打开的窗口、未确认交易报价、提示倒计时和碰撞对象不写入；读取后关闭临时交易/对话，再从完整数据重建，不能继续兑现旧报价。

结构化数据使用JSON、版本和SHA-256完整性校验；哈希用于识别损坏，不是防修改加密。先完整校验物品唯一性、重量、来源、账目、一次性标记、版本/参数、真实地图安全位置，再替换当前运行状态。写入经过同目录临时文件、Flush及原子替换，保留上一份有效备份；损坏的主档不覆盖已有有效备份。主档无效时尝试备份；两者均无效则提示并保留当前运行，拒绝猜测部分恢复；残留`.tmp`不作为存档读取。

磁盘载荷版本1、运行快照版本3。当前不迁移未来版本或不同价格/重量配置，明确拒绝并尝试备份。M5接入刷新/清理时应继续维护唯一身份、地面日期、来源与事件标记；如改变结构，应显式更新兼容策略和验证，不能只重置领取状态。

## 重建与重复验证

工程已有场景附着M4组件，无需重新生成场景。编辑器菜单：

- `Reborn 1985/M4/Verify life save and M1 M2 M3 regression`：包含原33项M1、56项M2、39项M3和M4状态/真实磁盘故障检查，输出`Unity/Logs/`。
- `Reborn 1985/M4/Build Windows x64 development player`：先验证，再构建同一场景到`Unity/Builds/M4-Windows/`，输出`M4BuildResult.json`。

批处理入口为`Reborn1985.Editor.M4ProjectTools.Verify`及`Reborn1985.Editor.M4ProjectTools.BuildWindows`。使用锁定版本编辑器，以`Unity/`为projectPath，不在同一工程同时启动两个编辑器。

Windows开发包包含明确启用才运行的M4检查器。先运行write，等进程完全退出后，再运行read；两个阶段使用相同且专用的输出目录和存档目录。示例在包所在目录的PowerShell执行：

```powershell
$m4Executable = Join-Path (Get-Location) 'Reborn1985.exe'
$m4Output = Join-Path $env:TEMP ('qigaimoniqi-m4-check-' + [Guid]::NewGuid().ToString('N'))
$m4SaveDir = Join-Path $m4Output 'Saves'
New-Item -ItemType Directory -Path $m4Output | Out-Null
$m4Common = '-force-d3d11 -screen-width 1280 -screen-height 720 -screen-fullscreen 0 -m4-smoke -m4Output "' + $m4Output + '" -m4SaveDir "' + $m4SaveDir + '"'
Start-Process -FilePath $m4Executable -ArgumentList ($m4Common + ' -m4Phase write -logFile "' + (Join-Path $m4Output 'write.log') + '"') -WindowStyle Hidden -Wait
Start-Process -FilePath $m4Executable -ArgumentList ($m4Common + ' -m4Phase read -logFile "' + (Join-Path $m4Output 'read.log') + '"') -WindowStyle Hidden -Wait
Get-Content (Join-Path $m4Output 'write-result.json'), (Join-Path $m4Output 'read-result.json')
```

检查器使用专用目录，并故意破坏其中的测试档以验证备份/拒绝恢复，**不要把`-m4SaveDir`指向自己的正常存档目录**。它驱动同一个Rigidbody2D、E边沿和uGUI回调，真实Windows程序退出后新进程读取；不注入虚构测试结果。`-m2-smoke -m2Output <专用目录>`和`-m3-smoke -m3Output <专用目录>`仍可在本版独立运行原验证器，不与M4参数混用。

实际执行结果、截图、包指纹及尚未手工验证部分见[VALIDATION.md](VALIDATION.md)。本版交付后停止，等待审核和人工试玩；不进入M5或P1。
