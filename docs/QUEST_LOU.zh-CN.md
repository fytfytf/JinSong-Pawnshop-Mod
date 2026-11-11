# 卢、线人、箱子与勒索原线

8.1.5 已完成当前原入口、主要状态分支和后果的源流程记录。依据 `StoreClientData`、`StoreClientListWanted`、`LouOperativeHelper`、`LouBombHelper`、`LouToyBoxHelper` 原CIL及回调，不修改、调用或压缩这些方法。本页不是整线实机通过；没有游戏或存档操作。

## 初访与原后续日程

正常模式下，卢初访要求原营业许可。`ScheduleLouFirstVisit` 保留原许可取得时机参数：原资格日取今日或次日，此后在原“诱人时机”安排首访，或到资格日+14时安排。诱人时机包括现金<75，或原房租／按揭到期且无法足额支付。不是统一固定第14日，也不是本MOD增设的75交易门槛。原首访还要求未见过卢，且wanted5未标记为已捕获。

| 原后续 | 正常延迟与资格 |
|---|---|
| wanted5morning | 原被捕日+3及以后、尚未来过，且卢恶作剧线未停止 |
| 药包来访 | 原夜间安排、isLouBombVisitPending有效且原线未停止；由原AddNextClient排队 |
| 箱子放下 | 药包访日+4及以后、尚未放下、原线未停止 |
| 箱子收取 | 放箱日+1及以后、尚未收取、原线未停止 |
| 易容来访 | 收箱日+4及以后、尚未易容来访、原线未停止 |
| 勒索结算 | 易容访日+7及以后、尚未结算、原线未停止 |

原 `IsLouGamesStopped` 接受射杀日、线人捕获日或识破日已记录三种情况；原初次普通拘捕与后续线人捕获分别存放，不混成同一个标记。正常延迟由原LouDelay给出，本轮不启用其测试模式。

`LouBombHelper.OnNight` 先按原lou_bomb精确ID与LOU_BOMB_ARMED_TAG解除店中该类任务药包的武装，保留原假药包题名／说明；若原晨访已发生、尚未放置且线未停止，再按原晨访日+LouDelay(2)-1条件安排原后续。它与 `LouToyBoxHelper.PlantRealBomb` 的失败后果是不同入口，本MOD没有把两个对象合并，也没有把真失败道具一并解除。

## 线人与传讯器

未发原传讯器时，射杀卢，或普通拘捕后尚未收箱／识破，会触发原线人引介队列。原传讯器显示沿Alpha192直建显示来源与既有图文，不改其标识、原所有权或搬动回调。

原识破检测接受前柜、后柜和原backInvinvElementCounter；并要求当前顾客原ID属于wanted5、wanted5morning、louBombVisit、louBoxDrop、louBoxCollect、louDisguise、louDemandCollect之一。直接将传讯器移到这些柜台会安排原0.3秒后识破，不能把所有普通顾客也当卢。

原容器捕获入口查找精确ID `republic_transmitter`；匹配成功后按原GraphUtils结果销毁传讯器、记录louCapturedDay、取消待发药包来访并关闭wanted5通缉。这里不是凭“传讯器”中文名称、外观或任意电子物判断。原队列在捕获次日之后、奖励未领时安排线人奖励；原PayReward只发一次750信用点。其他失败／盘问队列保留原状态条件，不因有线人立绘就一律发奖励。

## 箱内物件、完整／偷看／拿取

原玩具箱记录初始LOU_TOY_COUNT_INT，并检查原OPENED_TAG与当前筛选所得数量。数量不足为Taken(2)；数量完整且打开过为Peeked(1)；完整且未打开为Untouched(0)。整箱不在店时还按原isLouBoxNotPlaced分支返回Taken或Untouched。本MOD保留原箱子、物件、打开标记、返回前柜台和销毁／收取次序，不把换图后的视觉“看起来未开”当状态。

原Taken回应结束会调用失败药包／爆炸后果入口；Peeked与Untouched保留各自对白及原礼物选择。箱子收取中也保留传讯器捕获检查。没有重新模拟偷看、拿取、爆炸或发奖来证明改写后的图像本身正确。

## 寄物柜、偿付与勒索

射杀后，原次晨可留刻纹弹；满足后续条件时原第3日及以后发寄物柜。原寄物柜只在前柜台上、原尚未结算时，才由离店或夜间的CollectLocker处理。先检查传讯器捕获，再按原筛选结果合计 `max(0,GetNegociatedValue())`，最后沿原销毁和结算流程。

柜内要求金额为 `GetLouDemandValue()/2` 的原整数结果；GetLouDemandValue读取当前DifficultyRules.louDemandValue，不能把旧常量500当所有难度的固定金额。足额记录原付款日并排入次日原要员遇刺新闻；不足记录isLouLockerShort。柜子未解决、遗失时保留原一次性补发；到原期限或不足状态时仍走原失败后果。本MOD不自动装物、付钱或销毁其他容器。

勒索同样读取原难度金额。原已交物累计使用物品原议定价值；FIREARM且不属MAKESHIFT／LOWTECH的物品另有原偏好标记。少付结算分支保留“已交价值＋可付现金”与原最低额（总要求的一半）的比较，不引入66%规则，不将显示售价写回物品。原现金扣除、余款、足额、失败新闻与结算日回调均保留。

原感谢物按陷阱胜利、寄物柜足额或勒索结算等不同日期派生，且有一次性／过期检查；并非任意结束卢线都奖励同一物。原线人的盘问、怀疑、识破、捕获及失败标志互相独立，不能用“见过线人”代替结局条件。

源输入在 `evidence/content-prep/original-task-flow/` 的上述五个原类CIL及当前source-manifest。本批只记录，不扩展D11原图保留、不改任务后果；宋国刺客标板仍待当前需求清单之后。下一8.1.6其余通缉与关联人物原线。
