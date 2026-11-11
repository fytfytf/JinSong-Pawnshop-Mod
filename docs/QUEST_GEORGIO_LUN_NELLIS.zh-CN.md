# 乔治欧、伦与内利斯原线

8.1.4 原调度、主要交易／状态分支及登记归属已按当前CIL整理。本批不改原任务，不执行其工厂或回调，不打开存档，不生成空版本，也不把源码记录当完整实机。

## 乔治欧

以G=原georgio锚点（名义6）计，当前 `CheckStoryLineGeorgio` 的日程如下。

| 原节点 | 日期 | 资格／处理 |
|---|---|---|
| Intro、Intro2、Backpack | G、G+3、G+6 | 原引介、交易与背囊节点 |
| Assistance | G+9 | 原肢体残缺特质时不安排此访；接受后先尝试启动原georgio_assistance夜间事件，成功才扣原报价 |
| Georgio2、3 | G+12、G+16 | 原续访交易 |
| LuckIntro、LuckSale | G+21、G+26 | 原好运瓶引介／售卖，瓶的鉴定和运气规则保持 |
| Georgio4、Loan | G+31、G+37 | 原续访与借款选择 |
| 核废物支线 | 今日≥3、原nuclearWasteDumped>0、尚未来过 | 设置原一次性来访标记并加入原核废物回应，不据物品题名判断 |

借款选项在原对白回调中读取当时现金，提供整数除法算出的现金1/4、现金1/2或拒绝。接受按原金额扣钱，记录loanOfferedToGeorgio、借款日与RunStats，并发原信物和借据；拒绝不伪造借款。

原还款并非固定“几天后必到”：人物线入口在有未偿借款、当前不是租金到期日、今日>借款日+25时安排；原租金／按揭到期且现金不足、有借款、今日>借款日+5时还有提前还款入口。具体房租与按揭后续仍归原CheckRentDay。还款对白开始回调按原借款×4加现金、记统计，再把原未偿借款清零。这里没有新增高利贷业务或更改玩家现金。

## 伦与其关联人物

以L=原lun锚点（名义11）计：

| 原节点 | 日期 | 原交易／后果 |
|---|---|---|
| OfficerLunVisit | L | 50预算购买FOOD；原上柜物品对错回应保持 |
| OfficerLunShot | L+1 | 100预算购买MEDICAL／NARCOTIC；实际卖给他的NARCOTIC物品回调使原成瘾值+1 |
| LunCriminel关联人物 | L+2 | 原150通缉值、25批售参数与警械／弹药上柜；与伦本人字段分开，不凭函数名改身份 |
| BackOnDuty | L+5 | 94预算与原NARCOTIC需求；black_injector精确ID使原成瘾值-10，其他NARCOTIC使其+2 |
| Surplus | L+9 | 仅成瘾值>0时；原33批售参数、100通缉值、武器／弹药／震击器上柜及原拘捕回应 |
| Inspection | L+13 | 仅成瘾值>0且伦未被捕；原200通缉值与查验／拘捕分支 |

黑色注射器分支使用原ID，不把所有医疗品或外观近似药品当替代；减10也不擅自钳制成0。原条件检测“成瘾值>0”，与新题名、图像或角色称呼无关。

原查验回调枚举店铺库存、前柜、后柜和陈列柜的直接childItems，用NARCOTIC标签寻找物件；不是递归检查全部容器。找到后走原对白并销毁该原列表，未找到走原清白回应。Surplus／Inspection的拘捕对白回调设置officerLunArrested，再继续原执法收尾。本MOD不扩大没收范围，也不替换原物品／身份判定。

## 内利斯的当前入口归属

原 `CheckStoryLineNellis` 函数仍在程序集：名义锚点21，Nellis1／2／3／4分别为S、S+4、S+8、S+12；前两访分别以120预算买TREAT／FOOD、140预算买TREAT／ALCOHOL，第三访用原50批售参数售出3个corrupt与2个ruined模组，第四访以160预算买TREAT／ALCOHOL。

但**当前原HandleCharacterLine没有调用CheckStoryLineNellis，当前静态方法引用索引也未发现该检查函数的调用者**。这只说明本次出厂托管入口归属，不能排除反射、第三方或未来入口。本MOD保留原函数及四个工厂，不擅自插入日常顾客队列，也不把这些日程写成已自然到访或改造成新结局。原检查方法本身不使用前两次购买成绩去解锁后两访；本批不额外发明任务点数。

源输入：`evidence/content-prep/original-task-flow/StoreClientUniqueList.il`（原工厂与嵌套回调）、`StoreClientManager-CheckStoryLineGeorgio.il`、`StoreClientManager-CheckStoryLineOfficerLun.il`、`StoreClientManager-CheckStoryLineNellis.il`、`StoreClientManager-HandleCharacterLine.il`、`StoreClientManager-CheckRentDay.il`；当前静态方法引用来自 `evidence/current-content-scope/managed-calls.json`。下一8.1.5卢及相关线人、药包与勒索流程。
