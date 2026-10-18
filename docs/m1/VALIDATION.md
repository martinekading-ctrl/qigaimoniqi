# M1 验证记录与已知问题

> 2026-10-18｜Unity 6000.3.25f1（e1dba0a9aba4）｜Windows x64｜仅M1，待负责人审核

## 已执行检查

| 检查 | 方法与结果 |
| --- | --- |
| 工程初始化与编译 | 使用本机已激活的Unity 6.3编辑器，批处理创建、导入并编译工程；成功 |
| 保存场景 | 真正序列化的Isometric Grid、地面/障碍Tilemap、20点位、角色和正交相机；打开场景无需运行生成器 |
| 编辑器场景/物理检查 | 33项通过，见[原始检查输出](evidence/edit-mode-checks.txt)；包含七区可站立入口、A到各区及返回的碰撞清晰路径、编号分类、斜向速度、朝向、真实Physics2D墙体阻挡及停止行为 |
| Windows构建 | Unity BuildPipeline构建Windows x64 Mono开发版成功；[构建及本机试玩包校验记录](evidence/build-info.json)；未提交Library或引擎构建二进制到Git |
| Windows运行 | 实际WindowsPlayer启动，以同一个Rigidbody2D控制器完成A→B→D→C→D→G→D→F→D→E→D→B→A；无记录到的运行错误，见[运行输出](evidence/runtime-result.json) |
| 运行截图 | Windows运行版相机通过RenderTexture渲染实际场景，检查非空并逐图查看；[总览](screenshots/00-seven-zone-overview.png)、[出生](screenshots/01-A-start.png)、[C](screenshots/zone-C.png)、[G](screenshots/zone-G.png)、[F](screenshots/zone-F.png)、[E](screenshots/zone-E.png) |
| 范围 | 没有拾取、背包、负重、经济、餐食/帮工、休息、存档、时间、战斗或后期经营实现；AI参考图没有放入Unity的Assets |

验证入口是编辑器菜单 **Reborn 1985 / M1 / Verify scene and physics**。批处理可执行 `Reborn1985.Editor.M1ProjectTools.Verify`；构建方法为 `Reborn1985.Editor.M1ProjectTools.BuildWindows`。Windows开发版加 `-m1-smoke -m1Output "输出目录"` 可复现运行测试，正常启动不启用测试驱动。

路径检查不是仅比较策划矩形：从已保存场景的Tilemap与实际Collider出发，使用脚底半径的重叠/扫掠检查，再由Windows运行版逐步移动实际刚体完成往返。测试不以传送角色或关闭碰撞代替移动。

## 验证限制

- 自动运行测试使用开发验证输入驱动同一角色控制器，**没有声称已完成实体键盘逐键手工测试或编辑器GUI内手动Play测试**。普通键盘/焦点、长时间手感与玩家体验仍需负责人按[试玩说明](README.md)验收。
- Windows隐藏窗口的屏幕回读曾得到黑图；这些失败图没有归档。正式截图来自实际运行场景的摄像机离屏渲染，**不包含OnGUI说明面板**，也不是AI图或其他引擎模拟画面。
- 总览临时拉远相机用于说明七区关系；正常试玩默认正交半高3.25，近景跟随。自动行走使用三倍模拟速度以缩短检查，不是游戏日历/时间系统。
- 只验证本机Windows x64 / Direct3D11环境，不声称其他GPU、分辨率、平台、帧率或手柄已经通过。

## 原型已知问题与后续验收

1. 所有方块、人物和标记均为几何占位，尚无正式1985年广东县城美术。A区是小巷和铺盖占位，不是住宅；NPC不具备对话或交易功能。
2. 远景总览中的区域文字较小，近景UI和场景文字还需要人工可读性确认；屋顶遮挡、最终像素密度、角色比例与建筑投影待后续设计。
3. 地图60×42、地块1×0.5、人物图24×40和相机尺度均为可调参数；区域与障碍/点位调整后必须重建并复验。尚未冻结最终地图规格。
4. 近景早餐摊的揭示节奏需要手工试玩确认；M1仅有柜台/NPC占位，未制作蒸汽、棚布或完整早餐摊美术。
5. 所有20点位保留为稳定ID，但当前只显示位置说明。原铺盖免费休息、拾荒与失败恢复路线属于后续P0实现，不在M1中提前实现。
6. 可按源码在Unity重建Windows版；本机生成的完整试玩目录和ZIP在`Unity/Builds`，不提交到Git。M1审核通过后仍须明确批准才能进入M2。
