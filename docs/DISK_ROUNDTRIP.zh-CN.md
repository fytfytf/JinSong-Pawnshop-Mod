# D4独立新测试文件读写

用户2026-11-10明确授权只新建独立测试档做一次保存／读取。实际完成一轮逻辑保存、回读，没有打开任何现有存档，也没有追加重试或第二轮存档测试。

原PlayerStore.SaveGame除编码库存外还更新全局索引、游玩时长及历史轮转。为保持既有存档系统不动，本专项在临时内存店铺中使用原EncodeSaveItem/SaveManager与ES3原生数据编解码，把PlayerStore和StoreStation放入独立测试归档；磁盘读写只调用游戏官方ModHelper专属文件接口。文件位于临时助手自己的ModData目录，测试前必须不存在，不能覆盖。正常InitialSave、SaveGame、WriteSlotFile、PreLoadStore、LoadGame入口全程仍被拦截。

准备时直接ES3文件接口会要求通用files能力；未执行该方案或写文件。最终改用原ES3内存序列化与官方专属文件接口，助手24方法空能力扫描通过，没有给生产MOD或助手增加权限。

实际记录：evidence/runs/20261110T041818439Z-disk-roundtrip/runtime.log，SHA256为6fe5d03ba9308ef105d54a76c8158542ce8dfbf1018b3e9bf3e399639c60eed7。受管issue为alpha217-independent-disk，首次实际专项passed/count0，无专家。原生物品详情0.9.3同时为running=True/Match，前后原游戏、依赖及生产DLL身份均一致。

| 回读对象 | 实际结果 |
|---|---|
| 原数据类 | PlayerStore、StoreStation均由原ES3反序列化恢复 |
| 库存编码 | 与写入前原EncodeSaveItem JSON逐字相同；磁盘payload逐字节相同 |
| 物品解码 | 原SaveManager恢复water_purifier与med_bottle_small_pink原ID |
| 容器关系 | 壶仍在净水器内 |
| 液体与容量 | 原壶150000内部单位液量及150000容量保持，即150mL |
| 测试现金／任务变量／日期 | 6173／EleanorSavings317／day29写入后恢复；仅临时测试夹具值，不改变真实进度 |
| 中文显示 | 原解码物件显示青瓷净水器、青瓷接水壶 |
| 存档保护 | saveSlot=-1，现有LoadGame尝试0；存档槽与索引未操作 |

新文件为alpha217_d4_20261110T0420.es3。完成后复制校验到releases/alpha217-local-candidate/d4-independent-test.es3，再只移除助手目录中这一个新文件；归档SHA256为38b45c508e634bfa3aefa9656d9fc2aec21be2bc6526c363546b804c52b4ec21。没有删除目录或任何其他文件。游戏已退出，助手已移除，测试文件不在发行payload内。

这是新文件数据编解码/磁盘回读范围，不冒称正常菜单存档槽、索引、历史轮转或旧版本迁移全通过；用户现有存档从未打开。这些边界按D12另列，不再为本轮重跑。
