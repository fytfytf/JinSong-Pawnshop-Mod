# 本轮设备素材的原版输入范围

Alpha86–89使用原版工厂名称、图态键和原画布规格制作内容。以下为静态输入来源记录，不是实机验收，也不是全游戏目录刷新。

| 文件 | 旧目录快照 SHA256 | 本轮读取文件 SHA256 |
|---|---|---|
| Assembly-CSharp.dll | d9abc029f6b390619c31175df5735901aec6dc071c35c50ce5b3fdf92e3ede46 | 725cb999d019dff1ff31bed353c7f9bb8d81a7605a15de1f80f5a3a6cf7b1d05 |
| resources.assets | 566709c2c3ee2a08cb466a2dfc6b44df6bfa951b7bcd5d4c719f02b065d894c0 | a2672baa6a67bd954e1e5fed264525fdb27f9c5129c40a691149237c20353757 |

文件身份不同的原因尚未确定；不能据此断言游戏已更新，也不能沿用旧build号声称当前二进制全部兼容。旧ModuleDirectory的66项、FurnitureItemDirectory的28项均指旧快照，不代表已重建当前完整目录。

本轮仅用现有ItemsIL/DseCecil读取选中设备的8个工厂及售卖柜图态选择方法，用现有UnityPy读取19个指定Sprite引用的键、pathID和矩形尺寸。选中引用的尺寸和pathID与既有输入相同。没有提取像素、扫描形状、运行游戏、读取存档或重跑旧通路。REA服务不可用，本记录属于普通静态证据，不冒称REA工具执行证据。

| 素材 | 原画布 | 处理边界 |
|---|---|---|
| makeshift_reactor | 80×48 | Items/items_ship主图；同名36×36地图图标不替换 |
| projector、fridge | 各48×48 | 各自主图 |
| cassette_player | 80×80 | 主图；播放内容及音乐不改 |
| 两种售卖机枢core | 共用32×32原Sprite | 按不同物品identifier分别映射 |
| vending0/10/25/50/75/90/100 | 各64×112 | 保留原七档选择器、货物和收款 |
| fountain0/10/25/50/75/90/100 | 各64×112 | 保留原七档选择器、饮品和原生附层 |

售货柜和售饮柜的后缀来自原版分档：0、非零至14、15–35、36–65、66–80、81–97、98及以上。图中存货或液位只表达大致档位，不承诺显示值恰好等于后缀百分数，不加入固定容量说明。原版SetSpriteAndShape、库存、定价、货币、饮品颜色、插槽及音频继续使用原实现。

本机输入证据位于`evidence/content-prep/remaining-facilities/`：`focused-read.json`、`resource-identity.json`、`current-sprite-inputs.json`及对应IL片段。各批`specs.json`保留引用；仅补充来源元数据不要求重打已交付包。
