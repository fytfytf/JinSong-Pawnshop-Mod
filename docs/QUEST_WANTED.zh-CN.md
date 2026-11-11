# 其余原通缉与关联人物

8.1.6 当前主入口、周期、主要物品／状态条件及保留归属已整理。卢另见QUEST_LOU。本批仅源记录，不改通缉、举报、捕获、枪击、罚没、奖励、任务或角色身份，不执行原方法或打开存档，不宣称全部自然分支实机通过。

## 原ID与交易入口

| 原ID | 原身份键／入口 | 原保留数据与分支 |
|---|---|---|
| wanted1 | name_james_bailey | 300原通缉值；原售货与-33批售参数、拘捕回应；周期返访另见下 |
| wanted2 | name_the_butcher | 450原通缉值、-25批售参数、原举报改善阵营设置；屠夫捕获／幸存／狩猎分支独立 |
| wanted3 | name_arthur_lafleur | 原义军归属，100通缉值、FOOD／MATERIAL采购；原预算计算保持，不把构造中的每个数都算奖励 |
| wanted4 | name_odin | 300通缉值、原谈话与wanted4Normal后续；不是Juan，不以新称呼改原ID |
| wanted6 | name_li_bei_wen | 400通缉值、50批售参数、原注射器售卖及之后的炼材供给线 |
| wanted7 | name_juan | 125通缉值、-20批售参数、原已见面状态与首次举报挽留／拦截回调 |
| wanted8 | name_john_smith | 125通缉值、原FOOD采购／每周续访、举报后返回与破坏待访 |
| wanted9 | name_leroy | 300通缉值、原可拘捕及reputationPenalty=0；原SEC_VIP相关伪装查验对白／角色条件保持 |

表中的通缉值与批售参数是不同原字段，不据中文题名或立绘改变身份，不把它们都当固定发给玩家的现金。日常随机通缉选择继续经过原候选有效性与队列检查，不为换装强制刷新人物。

## 贝利与屠夫续访

贝利：已记录jamesBaileyMetDay，今日晚于初遇，且差值为14的倍数，wanted1未为原已捕获值1时，可安排原返回。

屠夫：已记录butcherMetDay、今日晚于初遇，按原ButcherDelay(7)周期，并且未被原捕获条件判定截断时，可安排返回。原测试首访入口保持关闭状态，不为验收启用。原被捕后的破坏、斩刀和狩猎字段独立保留；索耶援助要求原狩猎进行中、未给过援助、已记录斩刀日、达到斩刀日+ButcherDelay(1)，并且索耶未死亡。原500援助费用、原难度／随机取值与屠夫中枪后续均不改。完整执法处置入口仍按对应原回调，不把射击一概等同成功捕获。

## 李的原炼材供给线

首次未被举报的离场回调安排3日后原Supply访。供给访用原接受物品与回调计数：精确ID `restricted_chemical` 计入restricted，其余被原交易接受的物品计入common；不按新名字或成交金额计任务分。

有有效供给后，下一Finale延迟为 `max(2,17-4×common-7×restricted)`；若没有交付，则7日后重试Supply。计算达到最短2日时保留原够量对白和离场拦截切换。原调度读取liNextVisitDay／liNextVisit，到期先消费原待访日，再依据wanted6捕获状态决定是否加入原供给／收尾顾客。原配方纸件、谢礼、举报和枪击回调继续沿原函数，本MOD不新增配方、物品取得或角色复活。

## John Smith的举报后续

原每周来访：已记录初遇日、今日晚于初遇、差值为7倍数，且wanted8尚未原捕获、johnSmithReportedDay尚未记录。原首次举报回调记录johnSmithReportedDay；重要脚本入口在其+2日及以后、尚未排过返回时，一次性排CreateWanted8Return。

原返回后的再次举报记录johnSmithReturnReportedDay。破坏后续沿原生成／周期业务；本次已定位的 `HandleJohnSmithVandalVisit` 只在isJohnSmithVandalVisitPending为真时先清除待访，再用AddNextClient排原Vandal人物，不能用这个消费入口冒称已经验证所有破坏触发条件。原物品移除、钱款、警告与剧情后果保持，后续执法流程对应记录不靠清单状态绕过它们。

当前来源：`evidence/content-prep/original-task-flow/StoreClientListWanted.il`、`StoreClientData.il`、`StoreClientManager-HandleAlwayClient.il`、`StoreClientManager-HandleImportantScriptedClient.il`、`StoreClientManager-HandleWantedClient.il`、`StoreClientManager-HandleJohnSmithVandalVisit.il`，程序集与静态引用身份沿当前source-manifest。8.1父项仍保留未完成，下一8.1.7原宗教人物、组织与相关服务入口；八级整体验收不由本表自动完成。

后续源补充：当前PlayerStore.HandleVandalism在johnSmithReturnReportedDay已记录、isJohnSmithVandalismDone为假且今日≥再次举报日+1时，一次性置破坏完成及待访、重置daySinceLastVandalism为0、置isVandalized并写原夜报。之后原HandleJohnSmithVandalVisit消费待访并排下一客。屠夫的独立一次性破坏在今日≥butcherArrestedDay+ButcherDelay(7)-1时设置，晨访日为今日+1；未破坏且距上次至少2日时，原通用阵营破坏仍按声望≤-20与RNG::Roll(abs(rep)/5)判定。输入PlayerStore-HandleVandalismnative.il只读保存，没有运行或改写破坏回调。以上补齐先前待访标记写入的源边界，不追记自然流程实机通过。
