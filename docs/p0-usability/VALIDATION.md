# M5阶段0.5.1实际验证记录

> 2026-10-19｜Unity **6000.3.25f1**（e1dba0a9aba4）｜Windows x64 / Mono Development｜Direct3D11｜1280×720、1024×768｜保留M5，待审核与人工试玩

基线为M5交付`062d0f1`；开工时main为`7d8f8bc`，M3 #5、M4 #6、M5 #7仍未合并。本轮从最新main建立独立分支并接上M5，不合并依赖PR。授权与边界见[决策第11节](../p0/REVIEW_AND_DECISIONS.md#11-2026-10-19继续完善p0保留m5)，操作/复现见[README](README.md)。

## 最终构建实际结果

| 项目 | 结果与原始记录 |
| --- | --- |
| Unity编译与Windows构建 | **成功**，日志无C#错误或警告；原场景启动、Windows x64 / Mono开发版、0.5.1，BuildReport量**147,276,571字节**。[构建记录](evidence/build-info.json) |
| 原M1编辑器/物理 | **33/33通过**，[原始记录](evidence/m1-edit-regression.txt)；七区、20点、Isometric、墙阻挡、移动/朝向 |
| M2编辑器 | **56/56通过**，[记录](evidence/m2-edit-regression.txt)；整数克、超重、有限搜索、丢弃重拾、家书保护 |
| M3编辑器 | **39/39通过**，[记录](evidence/m3-edit-regression.txt)；44/48分结算、取消/旧报价/重复确认保护 |
| M4编辑器 | **41/41通过**，[记录](evidence/m4-edit-regression.txt)；餐食/帮工/线索、基础保存、数据及位置验证 |
| M5编辑器/磁盘/压力 | **49/49通过**，[记录](evidence/m5-edit-regression.txt)；跨日清理/刷新、旧背包、60日账目、已知旧档升级及超大写入保护 |
| 本轮编辑器契约 | **7/7通过**，[记录](evidence/edit-mode-checks.txt)；schema4不变、新开局未保存、完整HUD关闭重复调试文字、只读与确认提示、取消不发奖励 |
| 本轮Windows操作 | **两种尺寸各34/34通过，错误0**，[1280 write](evidence/runtime-write.json)、[1024 write](evidence/runtime-1024x768-write.json) |
| 本轮完全退出后新进程 | **两种尺寸各12/12通过，错误0**，[1280 read](evidence/runtime-read.json)、[1024 read](evidence/runtime-1024x768-read.json)；实际冷读取及真实旧M5兼容 |
| 最终版M2/M3 Windows回归 | **93/93、36/36通过，错误0**，[M2](evidence/m2-runtime-regression.json)、[M3](evidence/m3-runtime-regression.json)；包含M1 13站路线、取舍、掉落安全和有限资源交易 |
| 最终版M4 Windows回归 | **write42/42、退出后read19/19通过，错误0**，[write](evidence/m4-runtime-write.json)、[read](evidence/m4-runtime-read.json) |
| 最终版M5 Windows回归 | **write37/37、退出后read41/41通过，错误0**，[write](evidence/m5-runtime-write.json)、[read](evidence/m5-runtime-read.json)；原失败恢复、跨日保留/清理、重新谋生与13站往返 |
| 真实M4旧档升级 | **8/8通过，错误0**，[迁移记录](evidence/runtime-m4-migration.json)；继续支持原schema3升级/备份恢复和新日拾取 |
| 地图/范围核对 | 两张Tilemap及CompositeCollider三个序列化块与`062d0f1`完全一致，场景只改阶段标题；PlayerMover、CameraFollow、M1World、参数资产、经济/日刷新规则、Packages、ProjectVersion及参考图文件未改 |
| Windows试玩包 | `Unity/Builds/Reborn1985-M5.1-Windows-x64.zip`，**61,892,629字节**，SHA-256 `0f5fb869bf6cb04c21011720083aeae97a8eb3d46fb4451c42d50f5b13783ae5`。[包内核对](evidence/package-info.json)：234个条目，exe、UnityPlayer、实际游戏程序集、中文说明与M4/M5夹具逐项匹配测试目录 |

本表共225个编辑器断言、368个Windows运行断言，包含不同窗口与读写进程中重复执行的断言，不代表593种独立玩法。所有归档JSON的passed均为true，错误数组均为空。

首轮视觉检查发现操作说明末行被裁剪，调整排版并增加真实Text矩形高度断言；补齐背包关闭说明卡的检查。旧M5读档后的边界测量最初发生在camera-space Canvas完成渲染更新前，移到实际渲染/截图后测量，保留原屏幕边界及角色不重叠断言，并记录几何日志。之后从最终源文件重新编译、构建并重跑本表全部Windows流程；归档仅使用该最终构建的结果与截图。

## 本轮操作与数据实测

| 情形 | 实际执行与结果 |
| --- | --- |
| 场景可读性 | 完整HUD运行中TextMesh常驻标签为0；附近出现单个E提示。两种窗口的出生点、废纸点与旧档加载位置均检查实际屏幕边界及角色/朝向精灵不重叠 |
| 隔墙与远距离 | 在纸和角色之间暂设真实实体墙，纸不可达且不显示可取纸提示；移除临时墙后原E处理器正常拾取。走到无附近目标的主街，不虚构可交互对象。测试墙仅运行时存在 |
| 操作说明 | 实际uGUI按钮打开说明，全部控制/恢复文字能容纳于矩形；说明中仍可用原Rigidbody移动。开背包或生活窗口自动收起说明，附近标签隐藏，玩法快照无额外变更 |
| 阅读与保护 | 通过实际背包家书按钮打开阅读，背包关闭、只读提示不宣称Enter确认；家书始终1封0g。阅读已有可选标记会使存档变脏，单纯看线索日志不会 |
| 保存反馈 | 新运行显示尚未保存；实际保存后干净，移动后显示有未保存进度；没有档时主动读取得到可理解的先保存说明，当前进度保留 |
| 帮工与失败提示 | 走到林婶和C-04完成原一次帮工，0现金/1kg纸不变；再买餐失败无扣款。底部提示不再提供不可执行的帮工操作，反馈指出拾荒/免费休息 |
| 备份与失败 | 在独立测试目录连续保存、写坏主文件，实际回退有效备份并持续显示建议保存；再写坏备份，读取失败，运行快照不变；恢复有效文件并保存，清除错误提示 |
| 完全退出读取 | writer保存纸1kg、帮工已领及位置后退出；新进程仍先是新运行，主动确认读取后恢复原数据和精确位置，显示已读取且无未保存变化 |
| M5档直接兼容 | 实际复制上一版生成的[M5夹具](fixtures/m5-save-schema4.json)到隔离槽，读取出第4日/30分/已清理8/已售5/家书1/帮工已领；仍schema4，再存再读保留账目和唯一状态 |
| 原P0闭环 | 最终版M5回归仍完成零现金七源耗尽→免费休息清理/刷新→新进程拾荒3kg售44分→买26分餐余18分，继续跨日保留旧背包并正确清理地面物，最后第4日30分/仅家书/清理8/已售5 |

自动驱动使用实际WindowsPlayer、原Rigidbody2D步行、E按下边沿、真实处理器和uGUI按钮回调，不通过直接加钱/补物或关闭碰撞制造通过。截图来自当时真实主相机与camera-space uGUI，附有非空画面检查；未拼图、未使用AI生成场景。故障注入仅操作专用测试目录，未向正常玩家槽写测试档。

## 实际Windows截图

- [01 完整操作说明及自由探索/恢复提示](screenshots/01-optional-controls.png)
- [02 可选家书阅读、未保存变化与只读返回提示](screenshots/02-letter-and-unsaved-status.png)
- [03 靠近废纸的单一行动提示](screenshots/03-nearby-paper.png)
- [04 移动后有未保存变化，无附近物时不虚构提示](screenshots/04-unsaved-movement.png)
- [05 帮工已用后的缺钱恢复建议](screenshots/05-used-help-recovery-advice.png)
- [06 实际主文件损坏后恢复备份的状态](screenshots/06-backup-status.png)
- [07 完全退出后主动读档的状态](screenshots/07-cold-read-status.png)
- [08 旧M5档第4日/30分加载及提示位置](screenshots/08-m5-save-compatible.png)
- [09 1024×768操作说明完整显示](screenshots/09-controls-1024x768.png)
- [10 1024×768缺钱反馈和只读/操作布局](screenshots/10-recovery-advice-1024x768.png)
- [11 最终版M5回归：两纸两瓶3kg背包](screenshots/11-m5-3kg-regression.png)
- [12 最终版M5回归：失败恢复后买餐余18分](screenshots/12-m5-recovery-regression.png)

11/12归档文件名加前缀以区分本轮界面流程，分别对应M5原始截图04/05。先查看两种窗口的文字容纳和反馈，再检查目标提示及旧档画面；逐图查看未发现本轮可见的裁字或角色覆盖问题。

## 尚未验证与已知限制

- 本机确实执行了WindowsPlayer，**自动驱动不等于实体键盘/鼠标人工试玩**。负责人仍需验证F1/Esc实际按键焦点、鼠标命中、混合输入、新玩家能否识别区域及操作舒适性；编辑器GUI手动Play未执行。
- 本轮只验证Windows x64 / Direct3D11及两种窗口。其他显示缩放/DPI、超宽屏、低分辨率、其他设备和手柄未验，不能保证任意分辨率下最终界面质量。
- 保留灰盒、静态NPC与占位物；没有完成早餐摊蒸汽/棚布、正式像素密度、整图美术、年代招牌/食品考证或家书正文。参考图审核状态未改。详见[P0覆盖矩阵](../m5/ACCEPTANCE_MATRIX.md)及[原参考图说明](../p0/references/README.md)。
- 保存仍需玩家主动执行，错误/备份提示仅反映最近操作和当前运行变化，不会自动保存或监控用户在外部改文件。沿用单槽、已知M4升级和1MB上限；60日压力通过不代表可无限跨年。
- 刷新周期/产量、重量容量、单价和餐价仍是测试方案，未冻结平衡或确认为史实。

本轮交付维持M5。完整P0最终人工、视觉、文案验收仍待负责人确认；不自动合并PR，不启动M6/P1。
