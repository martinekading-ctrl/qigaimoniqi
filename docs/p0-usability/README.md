# M5阶段：P0体验完善版0.5.1

> Unity **6000.3.25f1**（e1dba0a9aba4）｜C#｜Windows x64 / Mono Development｜继续M5，待审核与人工试玩，不进入M6/P1

负责人明确选择“先继续完善P0，保留M5阶段”。本轮依据[总规划](../PROJECT_PLAN.md)、[P0规格](../p0/P0_SPEC.md)、[决策第11节](../p0/REVIEW_AND_DECISIONS.md#11-2026-10-19继续完善p0保留m5)和[正式验收](../p0/ACCEPTANCE.md)，改善现有灰盒的文字遮挡、目标发现、操作提示及存档反馈；不新增经营、剧情、地图或数值规则。

开工时main为`7d8f8bc`，M3 #5、M4 #6、M5 #7仍待合并。从最新main创建`codex/p0-usability-pass`后快进接上M5交付`062d0f1`，使用M5分支作为PR基线，只展示本轮增量；不代为合并任何依赖PR。

## 打开与Windows试玩

Unity Hub添加仓库中的`Unity/`，使用准确版本6000.3.25f1，打开原场景`Assets/Scenes/M1Greybox.unity`并Play。无需重新生成场景或安装依赖。编辑器GUI内手动Play仍需人工确认，已执行的批处理和WindowsPlayer验证见[实际记录](VALIDATION.md)。

本机完整目录为`Unity/Builds/M5.1-Windows/`；试玩包为`Unity/Builds/Reborn1985-M5.1-Windows-x64.zip`。完整解压后双击`Reborn1985.exe`，保留所有Data目录、UnityPlayer和配套文件。关闭窗口退出。压缩包未提交Git，包内有中文`试玩说明.txt`。

| 操作 | 键位与行为 |
| --- | --- |
| 移动/朝向 | WASD或方向键，保留M1行为 |
| 近距离交互 | E；同一套距离、墙体遮挡和目标优先级 |
| 确认/返回 | Enter确认可执行操作；Esc返回；只读信件/公告不显示虚假的确认提示 |
| 背包 | Tab/I开关，1—4选择，X丢弃普通物品一件；家书按钮只阅读 |
| 可选说明 | F1或左上“操作”按钮；F1/Esc关闭，不锁定移动、不规定五幕顺序 |
| 家书/线索 | L / J，与原版相同，家书仍是标明待定正文的背景摘要 |
| 手动保存 | F5或“保存”；左上显示实际保存、未保存变动或失败状态 |
| 主动读取 | F9/“读取”，再显式确认；每次启动先进入新运行 |
| 次日恢复 | A-01原铺盖E并确认免费休息；仍不是主角住宅 |

## 本轮改变

- 完整P0界面中移除重复的大型常驻区域/NPC文字，减轻角色与道路遮挡。原M1调试叠加模式仍保留；地图、七区、20个点位与碰撞未改。
- 附近只显示一个简短行动提示。可拾物、搜索点、林婶、陈伯、原铺盖及既有环境线索复用原交互条件；远距离/隔墙不显示可取提示。菜单打开时隐藏，位置避开实际角色精灵。
- F1说明集中列出键位、七区用途、自由探索及失败恢复路线。说明可随时关闭；打开背包、交易或生活窗口会关闭说明，减少重叠。
- 左上持续显示未保存、已保存/读取、未保存变化、备份恢复或保存/读取失败。状态来自实际操作与当前运行/位置/朝向，不把短暂提示消失当作已保存。失败不会丢掉当前进度；备份恢复后建议手动保存。
- 长反馈可分两行显示；只读、已完成和可确认界面分别给出真实操作提示。已用完一次帮工后，缺钱提示指向拾荒与免费休息，避免再次建议不可领取的餐食。

没有新增任务追踪、自动保存、自动导航、奖励、资源或消费。仍为灰盒，不把UI可读性检查当作最终视觉验收。A—G功能点与概念图审核状态见[原规格](../p0/P0_SPEC.md)和[参考图说明](../p0/references/README.md)。

## 原规则与存档兼容

整数克、整数分、5kg承重、44分基础收入、26分基础餐、一次帮工、保护家书、免费休息和每点每日一份的暂定刷新均保持M5原值。完整跨日规则与M4旧档升级说明见[M5说明](../m5/README.md)。测试价格仍非1985年史实价格，最终平衡未冻结。

正常手动槽仍是`%USERPROFILE%/AppData/LocalLow/QigaiPrototype/Reborn1985/Saves/save.json`和有效`.bak`。外层/载荷版本1、运行schema4、参数签名均未改变；可直接读M5档，也继续支持唯一的已知M4 schema3升级。保存状态提示是临时界面数据，不写进档案，不新增迁移。

`fixtures/m5-save-schema4.json`来自上一版实际Windows M5运行生成的文件，SHA-256为`44f049e413e9b8721c5b1838609a10e773842fdd9fd35ad88e0be5a4fb8652ad`：第4日、30分、仅一封家书、已清理8份/已售5份、帮工已领。文件禁止Git换行转换。构建复制到`Verification/M5Schema4.json`，与原M4夹具一起只供显式开发测试，正常游戏不会自动读取。

## 可重复验证

编辑器菜单`Reborn 1985/P0 usability/Verify retained M5 and presentation contract`执行M1至M5原检查和本轮界面契约；`Build Windows x64 development player`先验证再构建到`Builds/M5.1-Windows/`。批处理入口为`Reborn1985.Editor.P0UsabilityProjectTools.Verify`或`BuildWindows`，projectPath为`Unity/`；不同时打开同一工程。

Windows开发包显式加`-p0-usability-smoke`才启用自动驱动；write结束退出后，用同一隔离目录启动新read进程。它会故意写坏隔离目录中的文件检查失败反馈，**不得指向自己的正常存档**。

```powershell
$p0Executable = Join-Path (Get-Location) 'Reborn1985.exe'
$p0Output = Join-Path $env:TEMP ('qigaimoniqi-p0-ui-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $p0Output | Out-Null
$p0Common = '-force-d3d11 -screen-width 1280 -screen-height 720 -screen-fullscreen 0 -p0-usability-smoke -p0Output "' + $p0Output + '" -p0SaveDir "' + (Join-Path $p0Output 'Saves') + '"'
Start-Process -FilePath $p0Executable -ArgumentList ($p0Common + ' -p0Phase write -logFile "' + (Join-Path $p0Output 'write.log') + '"') -WindowStyle Hidden -Wait
Start-Process -FilePath $p0Executable -ArgumentList ($p0Common + ' -p0Phase read -logFile "' + (Join-Path $p0Output 'read.log') + '"') -WindowStyle Hidden -Wait
Get-Content (Join-Path $p0Output 'write-result.json'), (Join-Path $p0Output 'read-result.json')
```

另起新的隔离目录、改`-screen-width 1024 -screen-height 768`可复测4:3。原M2/M3、M4 write/read、M5 write/read/migrate驱动继续保留，运行方法见[M5验证说明](../m5/README.md#重复构建与验证)。截图来自真实WindowsPlayer主相机和camera-space uGUI，不是AI图片，也不是人工键鼠试玩的替代。

交付后等待审核及Windows人工试玩。P0最终人工、历史文案与美术验收尚未完成，本轮不启动M6旧货交易。
