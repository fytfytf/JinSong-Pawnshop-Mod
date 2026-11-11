# 容器与书信界面范围

5.4.2和5.6.1的入口与保留范围已整理。本次只读本项目源码与既有记录，没有改动窗口位置、大小、占格、容量、物品分类、服务回调或保存方式，没有运行游戏。已通过的显示和交互直接复用；范围整理不等于全部状态重新验收。

## 容器窗口的实际归属

| 窗口 | 现有显示实现 | 本轮处理 |
|---|---|---|
| invWindow主仓库 | ConceptFixtures按原格宽高放置；ShopMenuSkin给主仓库格底及分隔线换材质 | 保留；旧格底与转移记录复用 |
| frontInvWindow前交易格 | 热点8、PinGrid、原格数及格宽高；TintStockGrid调色 | 保留；不推断全部顾客/成交状态均已验收 |
| backInvWindow后交易格 | 热点24、现有我方柜台布局、原格数；同上 | 保留；旧counter-layout覆盖转移、预览归位和布局 |
| hiddenWindow暗格 | 热点17、PinGrid及原GridBounds | 保留；不能据静态绑定宣称全部暗格状态或扩容通过 |
| showcaseWindow展示柜 | 热点19、壁龛位置、原货格与背景调色 | 保留；旧showcase-customers中的存取和两次扩容复用 |
| docInvWindow公文位置 | 热点18、PinItem；内置公文匣contentWindow另由ShopMenuSkin换边框/底图 | 保留；根窗口和物品内置弹窗分开，不将二者算成一个通用弹窗 |
| trashInvWindow废篓位置 | 热点21、PinItem；废篓contentWindow另换边框/底图 | 保留；旧打开关闭及提示色复用 |
| faucetWindow供水位置 | 热点22、PinItem，固定槽提示描边 | 保留；提示色通过不等于供水业务全流程通过 |
| drainInvWindow排水位置 | 热点23、PinItem，固定槽提示描边 | 保留；同上 |
| cassettePlayerWindow曲牌盒位置 | CassettePin与tunesArt；其contentWindow另换边框/格底/按钮，仍播放原曲目 | 保留；不改播放和音乐 |

ShopMenuSkin的通用边框只作用于废篓、公文匣、曲牌盒三个内置contentWindow；格底还包括主仓库以及属于这些窗口的货格。前后交易格与壁龛展示柜的部分颜色来自ConceptFixtures，不据此宣称所有物品容器和全部设备窗口都有同款边框。

分类与排序、容量及溢出处理没有被本项目替换；相关原文和业务保留。选中和兼容色继续由原版触发，本项目仅将公文、废篓、供水、排水四类固定槽的提示改为描边。已落位物品与拖拽预览的缩放归位沿用既有补丁和counter-layout旧记录，不重跑。

没有发现足以要求本轮改坐标、重做格底或改写原版选择/溢出逻辑的证据。5.4.2的范围整理完成；5.4父项保留未完成，未知容器组合、所有分类和溢出状态不作穷举通过声明。理货9条动态行仍原文，主菜单MOD排序不属于店铺容器批次。

## 书信名录与服务

Correspondence已具备12个中文显示键：contacts、help、empty、send、cancel、regular、bulk、unavailable、introduce、sent、failed、tooltip，均在现有CSV中。沿用已接入的6种名片、原供应商身份及原GetLocalizedDisplayName；不为改变措辞重复制作同一界面。

Alpha55汇总中的`ledger`模式17项是书信名录测试，run为`evidence/runs/20261108T081739805Z-ledger/run.json`。实际覆盖：收名片先登记、信箱打开名录并隐藏拨号界面、寄出/取消确认、取消不执行服务、初次寄信触发原引介和冷却、冷却不能重复寄出、普通与大宗订货共用确认页、大宗选择原第二档货物、Wild信件打开原服务。

它不是余额字体测试。余额20%字号与长数字沿用`ledger-size`17项，字体和货币显示另有submenus/fixture-text记录；已同步修正界面交付索引。书信17项包含共同就绪项，也没有覆盖每个供应商的全部服务和失败分支，不将17算作17个独立服务。

5.6.1入口/名录/既有通过范围整理完成；5.6父项仍未全部完成。原Register、Known、GetDelivery、RunDelivery、Send、Cancel与Restore均保留，不改变服务选择、费用、冷却、触发条件、存储字段或失败后状态。未知的全部供应商分支不为凑完成而自动测试。

## 本轮新增与后续

Alpha93另交付19条静态中文：铺租、夜间服务说明、铺面吸引力、报纸入废篓、整批采购说明及店铺操作提示。供能、夜盗归还、清扫、违禁品、买卖及鉴定规则保持原实现。100%和其余动态格式行原样保留。详见[界面交付](SHOP_UI_DELIVERIES.zh-CN.md)。

5.7.1已完成本批店铺内操作提示。下一项5.7.2整理主菜单与公共弹窗的实际入口、字体及现有记录；当前FixtureText只在店铺场景映射UI/Interactable表，本批不能声称已替换主菜单、所有选项窗口或其他场景的同名文字。5.8的未覆盖交互随后列入相同范围表，已通过点击与拖放不重复。
