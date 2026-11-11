# 原宗教人物与两张手记

8.1.7 已完成原工厂／日程归属及两张原手记的显示处理。Alpha215只增加四条标题／正文显示，不改角色、手记数据、任务、交易或结局。没有启动游戏或打开存档。

**当前静态引用索引没有发现 `StoreClientManager.HandleClientFromStoryLine` 的原调用者**；其内部仍保留下表日程和工厂引用。GenerateClient／HandleCharacterLine当前没有直接调用它。以下是原函数内条件，不是已自然出现的来访日，也不排除反射／第三方入口。本MOD不为换装插入这个旧日程调用，不启用原未接入的剧情。

| 原函数内条件日 | 工厂 | 原行为 |
|---:|---|---|
| 3 | CreateUniquePreacher | 谈话后发第一张原HANDNOTE，再按原回调离开 |
| 5 | CreateChurchAbby1 | 150购买预算 |
| 10 | CreateUniquePreacher2 | 发第二张原HANDNOTE，再按原回调离开 |
| 12 | CreateChurchAbby2 | 200购买预算 |
| 19／26／33 | CreateChurchAbby3／4／5 | 各250购买预算 |
| 40／47 | CreateChurchUniqueEngineer／2 | 各250购买预算 |
| 43 | CreateChurchAbbyEnding | 原告别对白后离开；该回调没有结束整局游戏 |

以上采购保留原MODULE／NODE／MATERIAL分类。Abby与工匠所查普通对白结束回调为空，不据其采购额额外发明“支援分”或玩家自选结局。原AbbyEnding的回调为原DismissCurrentClient，不能因函数含Ending便把它当全局终局。

第一、二张原传道手记通过 `PreBuiltItemHelper.CreateHandnote` 写入HANDNOTE_TITLE／BODY／SIGN／FONT，物品使用进入 **HandnoteUIManager.OpenPanel**，不是PaperUIManager.OpenUI。本轮已经按这一实际入口接入，只覆盖HandnoteUIManager公开title／body的可见文字。

Alpha215把“The Neural Doctrine”两张标题与正文按D10处理为《机关玄论》及《机关玄论·其二》：原崇尚非人灵智、推演实证和组织秩序的立场保留，背景为城中机关灵智传闻，不指定宋金。正文完整匹配原文才处理两张标题／正文；用户自定义或其他MOD的不同内容不泛化改写。

原四个HANDNOTE字段、字体选择、物品ID、发纸数量、日期、声音与所有回调均未改；原原文仍在物品中。显示借用Alpha214的可逆TMP覆盖／恢复机制，英文和停用恢复仍由原文与当前所有权判定，不覆写外来后写。没有以阅读界面换词去更改物品详情的业务getter或存档。

本批只记显示交付与源归属，不称原旧宗教路线自然到访或手记实机已通过，不重跑旧通路。现有1852条NPC词条另作一次当前SharedTable源对应：全部存在于当前2544条Dialogue表内，不因源码英文fallback字面量再加重复拦截；这不是全部Dialogue键或全部活动对白的实机证明。

来源：`evidence/content-prep/original-task-flow/StoreClientListChurch.il`、`StoreClientManager-HandleClientFromStoryLine.il`、`dialogue-fallback-disposition.json`；`evidence/content-prep/reading-page-scope/PreBuiltItemHelper-CreateHandnote.il`、`HandnoteUIManager.il`。四条原文与改写在Alpha215，完整原文对应见同批native-aliases.json。下一8.1.8原组织与服务任务入口。
