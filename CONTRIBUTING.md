# 金宋当铺 MOD 修改与贡献指南

本指南面向想改贴图、翻译、界面或剧情的贡献者。当前源码对应 **Alpha277**，可在 Windows 上使用 Steam 正式版游戏文件构建。先从下表找到入口，再按改动范围选择验证。历史版本的实机记录保留各自的适用范围。 [项目首页](README.md)

## 从源码构建一个本地测试版

需要 Windows、PowerShell 7 和已安装的《Probably Stolen》Steam 正式版。在 Steam 库中选择游戏的「管理 → 浏览本地文件」，找到含 `Probably Stolen.exe` 的目录。构建脚本使用 Windows 自带的 .NET Framework C# 编译器和游戏 `Probably Stolen_Data/Managed` 中的程序集。新增 C# 文件时，在 `tools/build.ps1` 的编译清单里加入该文件。代码按 C# 5 编译。

在仓库根目录运行以下命令，把第一行换成你自己的游戏路径。命令将产物放在单独的暂存目录里，可以在一份干净的工作区运行。

```powershell
$gameRoot = 'E:\SteamLibrary\steamapps\common\Probably Stolen'
$modOut = Join-Path (Get-Location) 'dist/contributor-build/jinsong_poc'
if (Test-Path -LiteralPath $modOut) { throw '请换一个新的输出目录。' }

pwsh -NoProfile -ExecutionPolicy Bypass -File tools/build.ps1 `
  -GameRoot $gameRoot -OutputRoot $modOut
if ($LASTEXITCODE -ne 0) { throw 'MOD 构建失败。' }

Copy-Item -LiteralPath 'dist/jinsong_poc/manifest.xml','dist/jinsong_poc/INSTALL.zh-CN.txt' -Destination $modOut
Copy-Item -LiteralPath 'dist/jinsong_poc/Localization' -Destination $modOut -Recurse
Copy-Item -LiteralPath 'assets/v01/alpha140-shop-audio/Sounds' -Destination $modOut -Recurse
```

完成后，`jinsong_poc` 根目录应有 `manifest.xml`、`JinSongPoc.dll`、`INSTALL.zh-CN.txt`，以及 `Localization/`、`Textures/`、`Sounds/` 三个目录。常规构建从 `assets/v01/source/` 复制贴图，并按现有规则处理 `jin_npc_*` 立绘。若手头已有完整测试包、这次只改 C#，在构建命令末尾加 `-CodeOnly`，然后用新编译的 DLL 替换**测试包副本**中的 DLL。

退出游戏，把测试包的 `jinsong_poc` 放到游戏的 `Mods/`。如果已有同名本地版本，先把旧目录备份到 `Mods/` 外。启动游戏，在主菜单「改装件」启用，然后重启。测试包沿用 `jinsong_poc` ID，同 ID 的本地版会优先于工坊订阅版。 [游戏官方 MOD 指南](https://github.com/dragonpan2/probably-stolen-modding/blob/main/ModdingGuide.md#steam-workshop)

## 常见改动从哪里入手

| 想改的内容 | 修改入口 | 要一起核对的内容 |
|---|---|---|
| 商品名称、描述、状态和主图 | [`ShopGoods.cs`](src/ShopGoods.cs)、[`RemainderItemText.cs`](src/RemainderItemText.cs)、[`Localization/`](dist/jinsong_poc/Localization)、[`素材源目录`](assets/v01/source) | 同一商品可能有多种图态。用原 ID、图集和精灵名找到映射，保留真假、装配和状态差异。 |
| 人物立绘、称呼和对白 | [`RosterPortraits.cs`](src/RosterPortraits.cs)、[`OrdinaryPortraits.cs`](src/OrdinaryPortraits.cs)、[`NpcNames.cs`](src/NpcNames.cs)、[`Localization/`](dist/jinsong_poc/Localization) | 共用原立绘的人物可能属于不同身份。修改目标人物的映射，涉及通缉人物时同时查实际出场图和海报图。 |
| 当铺、窗外街景、热区和外出地点 | [`InteriorArt.cs`](src/InteriorArt.cs)、[`SceneActionArt.cs`](src/SceneActionArt.cs)、[`ShopHotspots.cs`](src/ShopHotspots.cs)、[`LocationArt.cs`](src/LocationArt.cs) | 可见背景、遮挡层、物品格与点击热区各有入口。动到布局时查点击、拖放和往返场景。 |
| 账本、窗口、按钮、每日服务和人脉簿 | [`CommonUiArt.cs`](src/CommonUiArt.cs)、[`ReportedUiSkin.cs`](src/ReportedUiSkin.cs)、[`DailyServicesText.cs`](src/DailyServicesText.cs)、[`ProgressText.cs`](src/ProgressText.cs) | 确认改的是实际打开的页面、点击项及不可用状态。沿用原生价格、解锁和回调。 |
| 指南、文书、机构名与每日新闻 | [`SerializedLabels.cs`](src/SerializedLabels.cs)、[`PaperText.cs`](src/PaperText.cs)、[`OrganizationText.cs`](src/OrganizationText.cs)、[`NewsPresentation.cs`](src/NewsPresentation.cs) | 有些文本来自原生查词，有些来自 Unity 序列化标签或事件缓存。查实际显示控件，再选对应入口。 |
| 净水、蓄能、配方与机巧加工的显示 | [`WaterPresentation.cs`](src/WaterPresentation.cs)、[`CraftingText.cs`](src/CraftingText.cs)、[`ChemicalText.cs`](src/ChemicalText.cs)、[`AccessCraftingText.cs`](src/AccessCraftingText.cs) | 保留原生水量、纯度、耗电、配方和容器状态。改显示时比对同一物品进入界面前后的实际数据。 |
| 界面和店铺音效 | [`ShopAudio.cs`](src/ShopAudio.cs)、[`DirectShopAudio.cs`](src/DirectShopAudio.cs)、[`音效源目录`](assets/v01/alpha140-shop-audio/Sounds) | 查实际播放入口。替换界面或店铺声音时留意循环声源和背景音乐。 |
| 新增任务和结局 | [`AssassinStory.cs`](src/AssassinStory.cs)、[`CityStory.cs`](src/CityStory.cs)、[`CityStoryCampaign.cs`](src/CityStoryCampaign.cs)、[`CityRules.cs`](src/CityRules.cs) | 核对来访、成交金额、离场结算、文书和结局条件。对照 [新增剧情攻略](docs/STORY_WALKTHROUGH.zh-CN.md) 和 [原任务流](docs/QUEST_FLOWS.zh-CN.md)。 |

**例子一，给青瓷净水器换皮。** 在 `ShopGoods.cs` 查 `water_purifier`，会找到 `purifier` 和 `purifier_w` 两个精灵入口。对应的 MOD 源图是 `jin_goods_purifier.png` 和 `jin_goods_purifier_w.png`。沿用两张图的尺寸、透明边界和占格。只改中文题名时，改 `zh.csv` 的 `goods_purifier_name`，保留 `water_purifier` 原 ID。

**例子二，调整沈砺的购武对白。** 查中英 CSV 中的 `assassin_weapon_request`。保留 `{0}`，它由代码填入当次预算。`AssassinStory.cs` 在原生交易成功时记录成交额，在顾客离场时按本次基准判定。若改的是门槛或分支，再验证临界金额的两侧、离场后状态和对应次日报纸。

修改 CSV 时保留原键名、占位符和富文本标记。包含逗号、引号或多行的内容按 CSV 规则保存。改显示时沿用原物品 ID、数量、数值、判定、选项和存档格式。若改动本身就是新增玩法，在 PR 里写清楚触发条件与结果。

## 已跑通的通路怎么复用

| 参考范围 | 已验证内容 | 适合复用的入口 |
|---|---|---|
| **Alpha262 工坊兼容** | 15 项临时店铺检查。中文报头与指南、店铺音效和原背景音乐、姓名牌、两个化工实时状态入口、物品详情 0.9.4 的五个接入点及净水器内水壶的名称和水量。也核对了关闭物品详情时主 MOD 能加载。 | `-WorkshopCompatibility`。 [交付记录](assets/v01/alpha262-workshop-compat/delivery.json) |
| **Alpha260 沈砺任务线** | 131 项临时店铺检查。受控推进营业日，跑通一条完整路线并覆盖四种常规结局和举报结局；包含原生买卖、离场结算、独立文书、选择和次日新闻。 | `-AssassinOnly`。 [交付记录](assets/v01/alpha260-assassin-complete/delivery.json) |
| **Alpha255–258 店铺交互** | Alpha255 验证废篓点击和展示窗存取；Alpha256 验证杰克工坊黑板商品格与往返；Alpha258 验证人脉簿的选项、金额显示和整页样式。 | 按需查 `-FixtureClicksOnly`、`-WorkshopOnly`、`-NetworkOnly` 对应的断言。 [展示窗与废篓](assets/v01/alpha255-fixture-clicks/delivery.json) · [杰克工坊](assets/v01/alpha256-workshop-board/delivery.json) · [人脉簿](assets/v01/alpha258-contacts-page/delivery.json) |
| **历史设备链** | 原验收覆盖含水容器和试纸的中英往返、真实测量与图态，以及独立的蓄能、化工、通行符筹加工。各项按各自记录的游戏版本和测试数据复用。 | [水质与试纸](docs/WATER_LOCALE.zh-CN.md) · [蓄能](docs/POWER_CHAIN.zh-CN.md) · [化工](docs/CHEMICAL_CHAIN.zh-CN.md) · [通行符筹](docs/ACCESS_CRAFTING_CHAIN.zh-CN.md) |

纯物品贴图替换保持原尺寸和占格时，沿用既有交互验证。改到点击、拖放、数据或剧情判定时，只重跑受影响的路径。中文实机通过的记录只覆盖中文。存档专项另有一条 [独立新文件读写记录](docs/DISK_ROUNDTRIP.zh-CN.md)，其范围是测试文件，日常验证使用临时内存店铺。

### 运行一项实机检查

[`ProbeUiSurvey.cs`](tools/ProbeUiSurvey.cs) 是实机检查源文件。下列示例对应 **Alpha262**。先安装并启用要测的 MOD，在主菜单切到简体中文，然后退出游戏。两份脚本目前使用维护机的 Steam 路径。如果你的安装位置不同，在本地脚本副本里把 `build-ui-survey.ps1` 的 `$managed`、`run-ui-survey.ps1` 的 `$game` 和其中 `steam.exe` 的路径改成自己的。保留脚本在 `tools/` 下，脚本会按所在位置定位仓库。旧专项在探针中核对目标版本，移植到新候选时核对预期版本和断言涉及的功能后再跑。

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File tools/build-ui-survey.ps1
if ($LASTEXITCODE -ne 0) { throw '测试助手构建失败。' }
pwsh -NoProfile -ExecutionPolicy Bypass -File tools/run-ui-survey.ps1 `
  -WorkshopCompatibility -TimeoutSeconds 55
```

脚本从 Steam 启动临时测试助手，使用 `saveSlot=-1`，守卫拦截实际存档读写，完成后清理本轮助手。结果和日志在本地 `evidence/runs/`。先看 `outcome`、`phase_failures`、`runtime_errors` 和 `load_attempts`。若游戏未启动、版本不符或缺少日志，记录环境原因。物品详情 0.9.4 启用时，`-WorkshopCompatibility` 才会运行它的接口和实际提示检查。探针依赖维护机或特定旧版本时，可以直接参考其中的操作与断言来做窄范围验证。

## 提交改动

- 写清游戏版本、MOD 版本、改动的文件和受影响的功能。给出实际操作步骤与预期结果。显示问题可以附裁好的界面截图，业务改动注明关键输入和输出。
- 在验证说明里分别写明本次实际验证的范围，以及沿用哪一版的既有记录。仅编译或静态检查时，按实际结果填写。提交源文件、语言表和相应原创素材。
- 新增入口优先用游戏的 `ModHook` 和公开 API。确需查游戏或声明的兼容 MOD 内部成员时，使用官方 `ModReflect`。Alpha277 的 `<Capabilities />` 为空。涉及新 DLL、外部依赖或反射时，先按官方指南在当前游戏中用 `mod-scan jinsong_poc` 核对。 [官方能力与扫描说明](https://github.com/dragonpan2/probably-stolen-modding/blob/main/ModdingGuide.md#checking-before-you-publish)

维护者公开仓库前应确定代码与素材的授权范围，并逐项检查将发布的文件和 Git 历史。供贡献者构建的游戏程序集由各自的正版安装提供。公开提交应剔除账号信息、存档、个人路径日志、临时助手产物、外部 MOD DLL 和未经授权的原版资源。仓库当前尚无 `LICENSE`，请以发布时的实际授权为准。 [GitHub 许可说明](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/licensing-a-repository)
