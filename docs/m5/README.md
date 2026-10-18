# M5 跨日有限刷新、清理与P0功能回归

> Unity **6000.3.25f1**（e1dba0a9aba4）｜C#｜Windows x64 / Mono开发版｜版本0.5.0｜待PR审核和Windows人工试玩，不进入P1

M4交付后负责人明确要求“继续下一版的开发”。本轮实施[总规划M5](../PROJECT_PLAN.md)、[P0规格5.4/7](../p0/P0_SPEC.md)、[决策第10节](../p0/REVIEW_AND_DECISIONS.md)与[验收R/S用例](../p0/ACCEPTANCE.md)。继续使用原`Unity/Assets/Scenes/M1Greybox.unity`、七区20点位、相机、碰撞、M2拾荒背包、M3称重及M4生活/存档，没有重建工程、选择新引擎或安装依赖。

开工时main为`7d8f8bc`，M3 PR #5、M4 PR #6均未合并。M5分支从最新main建立后快进到M4交付`f667327`，暂以M4分支作为PR基线，只显示M5差异；依赖合并后再调整基线，不代为合并任何PR。

## 打开与试玩

Unity Hub添加仓库的`Unity/`，准确使用6000.3.25f1，打开`Assets/Scenes/M1Greybox.unity`并Play。无需生成场景或运行附着工具。原地图仅改阶段标题，未移动建筑、NPC或点位。

本机完整目录：`Unity/Builds/M5-Windows/`；压缩包：`Unity/Builds/Reborn1985-M5-Windows-x64.zip`。完整解压后双击`Reborn1985.exe`，保留UnityPlayer、Data目录等全部文件。包未提交Git，包内有中文`试玩说明.txt`；关闭窗口退出。

| 操作 | 键位/行为 |
| --- | --- |
| 移动/朝向 | WASD或方向键，与M1相同 |
| 近距离交互 | E；废品、搜索、陈伯、林婶、环境线索和原铺盖共用 |
| 确认/取消 | Enter确认当前可执行操作，Esc返回；走远自动关闭地点交互 |
| 背包/丢弃 | Tab/I开关；1—4选行，X丢弃普通物品一件；家书按钮只阅读，不能丢弃 |
| 家书/线索 | L / J，可选查看，不作为移动或其他交互的前置条件 |
| 保存/读取 | F5手动保存；F9后Enter/点击确认读取；左上亦有按钮 |
| 免费进入下一日 | A-01原有破旧铺盖处E，再确认休息；不代表拥有住宅 |

启动仍先进入新运行。继续上次进度需要主动F9读取，退出前请F5保存，没有新增自动保存。

## 本轮跨日规则

**暂定测试方案：原七个来源每点每轮1件，交付周期1日。** B-02/D-01废纸，D-02/D-03玻璃瓶，F-01搜索废纸，F-02搜索玻璃瓶，F-03重金属。点位、物品、重量、价格未变，没有新来源、随机掉落、累积补货或无限同日搜索。

周期配置在`Assets/Settings/M2Settings.asset`的`sourceRefreshDays`，当前为1；最终周期/数量仍待人工平衡，调整测试配置后应新开运行，旧档参数签名不匹配时拒绝读取。自动测试只用临时配置副本验证2日周期，不更改交付值。每次刷新仍只有每点一件，未领取的旧轮机会不堆积成多件。

在A-01免费休息时，一次确认完成一次日序推进，同时：

1. 清理刚结束那一天仍在地面的普通丢弃物，移除显示与可拾取记录；保留清理身份以核对资源守恒。
2. 保留背包中的普通物品、现金、家书、交易/餐费记录、已读/线索及一次性帮工状态。
3. 到新周期时让原来源各提供一份新物品。昨天的旧背包实物与今天的新物品身份不同，可以共存、准确计重与出售。

同日反复搜索、离开返回、F5/F9或丢弃重拾不能增加当前轮额度。昨天带在背包里的纸如果今天才丢弃，今天仍能重拾，今天结束时才清理；清理依据落地日期，不误按物品产生日期删除旧背包。丢弃物仍没有阻挡碰撞体。

没有实时钟、日期惩罚、额外住宿费或强制日程。仍显示1985年与日序/清晨，具体月日与时间流速待确认。

## 建议手工验收路线

- 正常：A→B/D收集两纸两瓶3kg→G-04陈伯称重确认得44分→C区买26分餐剩18分；取消/反复确认均不得多扣货、多加钱或多扣款。
- 失败恢复：先无钱找林婶，接受一次帮工，到C-04整理领餐；把当天七个来源都拾取后丢弃，不卖钱。回A-01免费休息，检查地面旧物消失、背包/帮工保留、七点重新可取，再拾荒卖货买餐。
- 背包保留：带着废纸休息，次日再取同来源纸，背包应有2份/2kg；放下一份、当天重拾，数量不增加；只留下地面那份过夜时应清理，背包那份仍在。
- 保存：分别在丢弃后、重拾后、休息清理后保存，完全退出并重开读取。已清理物不得复现，帮工不得重领。自由路线仍可走A→B→D→C→D→G→D→F→D→E→D→B→A。

完整逐项状态与尚未验收项目见[覆盖矩阵](ACCEPTANCE_MATRIX.md)，实际自动结果与截图见[VALIDATION.md](VALIDATION.md)。自动检查不能替代负责人键鼠手感和P0最终验收。

## 存档升级与实现边界

正常路径继续是`%USERPROFILE%/AppData/LocalLow/QigaiPrototype/Reborn1985/Saves/save.json`，旁有`.bak`有效备份。从M4开始固定产品名，M5沿用同一槽，没有另建一套玩家存档。

外层文件/载荷版本仍1，运行快照由schema3升为**schema4**，参数签名加入刷新周期。每份物品增加产生轮日序；首日身份不变，后续用来源及轮日序区分。来源领取历史与背包、地面、已售和已清理身份严格对应，互不重复；账目仍精确到分。

只支持已知的M4 schema3迁移：先校验原文件哈希、旧版本数据与旧参数，再转换独立副本并做当前校验。保留现金、原背包、交易与一次性状态；旧M4档中已经跨日却留下的地面物按M5规则转入清理记录，当前日可用来源按新轮判断。首次M5保存保留原M4文件作为备份，备份也可迁移。未来版本、损坏数据或不匹配参数不猜测恢复。迁移不是把两份进度相加，也不额外发家书或餐食。

原单槽JSON、完整状态验证、临时写入Flush/原子替换和有效备份继续使用。超过现有1,000,000字节读取上限的写入会在替换前拒绝，保留旧档；本轮只验证P0与60日压力范围，不承诺跨年无限交易历史。历史收据/清理身份不在本轮压缩或删除。

`fixtures/m4-save-schema3.json`来自上一版真实Windows运行生成的有效文件，SHA-256为`bd9924a0d457cf604e40bbd40cd99a06bfe30d3459465b434b9717963d77dce4`；其状态为第2日、18分、帮工已领、已售4份、原M4未清理的第1日地面纸1份。该夹具禁止Git自动换行转换，以保留历史文件字节。构建复制一份到`Verification/M4Schema3.json`供显式开发测试，正常游戏不会自动读取该夹具。

主要实现为`M5DailyRules`、`M5SaveUpgrade`及原状态/交互/存档的增量修改；`M4LegacyValidation`保留旧档校验。原M4检查器仅更新跨日预期、补测新日实物，其历史构建记录不覆盖。

## 重复构建与验证

编辑器菜单`Reborn 1985/M5/Verify daily cycle save migration and P0 regression`执行原M1/M2/M3、更新后的M4和新增M5检查，输出`Unity/Logs/`。`Reborn 1985/M5/Build Windows x64 development player`先验证再构建到`Unity/Builds/M5-Windows/`。批处理入口为`Reborn1985.Editor.M5ProjectTools.Verify`或`BuildWindows`，projectPath为`Unity/`；不要并发打开同一个Unity工程。

开发包只有加`-m5-smoke`才启动验证驱动。write完成并退出后，read用同一专用目录重新启动；migrate使用另一专用目录，故意损坏测试文件来测备份，**不可指向自己的正常存档**。

```powershell
$m5Executable = Join-Path (Get-Location) 'Reborn1985.exe'
$m5Output = Join-Path $env:TEMP ('qigaimoniqi-m5-check-' + [Guid]::NewGuid().ToString('N'))
$m5Common = '-force-d3d11 -screen-width 1280 -screen-height 720 -screen-fullscreen 0 -m5-smoke -m5Output "' + $m5Output + '" -m5SaveDir "' + (Join-Path $m5Output 'Saves') + '"'
New-Item -ItemType Directory -Path $m5Output | Out-Null
Start-Process -FilePath $m5Executable -ArgumentList ($m5Common + ' -m5Phase write -logFile "' + (Join-Path $m5Output 'write.log') + '"') -WindowStyle Hidden -Wait
Start-Process -FilePath $m5Executable -ArgumentList ($m5Common + ' -m5Phase read -logFile "' + (Join-Path $m5Output 'read.log') + '"') -WindowStyle Hidden -Wait
Get-Content (Join-Path $m5Output 'write-result.json'), (Join-Path $m5Output 'read-result.json')
```

迁移检查使用相同参数结构，另设`-m5Output`/`-m5SaveDir`并指定`-m5Phase migrate`。原`-m2-smoke/-m2Output`、`-m3-smoke/-m3Output`及更新跨日预期后的`-m4-smoke/-m4Phase write|read/-m4Output/-m4SaveDir`可独立运行，不混用不同里程碑的启动参数。

本版交付后停止，等待M5审核和人工试玩；不宣称完整P0最终验收通过，不启动P1旧货交易。
