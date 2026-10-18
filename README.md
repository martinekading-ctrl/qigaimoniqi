# 《重生80年代：从乞丐到富豪》（暂定名）

一款以中国广东县城为背景的 2D 斜俯视像素风生活模拟、经营养成与人生逆袭游戏。

**当前阶段：M2——拾荒、背包与负重，待审核和Windows人工试玩；不进入M3。M1已验收，PR #2/#3已合并。**

工程位于 `Unity/`，锁定 **Unity 6.3 LTS 6000.3.25f1**、C#与Windows PC，沿用 `Assets/Scenes/M1Greybox.unity`。[M2打开、操作和Windows试玩说明](docs/m2/README.md) · [M2实际验证与限制](docs/m2/VALIDATION.md) · [M1历史交付](docs/m1/README.md)。

## 项目规划

完整设计与阶段边界见 [V0.1 项目总体规划](docs/PROJECT_PLAN.md)，其中区分了完整游戏长期愿景、M0规划、首个可玩版本 P0 和最小可玩经营版本 P1，并列明已确认、暂定及待确认事项。

P0 五幕详细资料见 [开发规格](docs/p0/P0_SPEC.md)、[审查与决策记录](docs/p0/REVIEW_AND_DECISIONS.md)、[验收清单](docs/p0/ACCEPTANCE.md)和[概念参考图说明](docs/p0/references/README.md)。完整规划不代表实现全部P0的授权；当前新增M2子集，最终地图与像素规格仍待确认。

在M1七区灰盒、角色移动、镜头与2D碰撞基础上，新增七个既有点位的有限拾取/搜索、5kg测试承重、受保护家书、可开关背包及安全丢弃重拾。WASD/方向键移动，E交互，Tab/I背包，1—4选择、X丢弃或点击按钮，Esc关闭背包。尚无出售/现金、食物/帮工、正式剧情、休息、时间、跨日或存档。
