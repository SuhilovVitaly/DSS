# Сохранение игры

> **Контракт EP-0013.** Текущий контракт EP-0013: writer Save19, явная миграция Save18, сохранение не-каталожного tuning и provenance retimed flight. Сохраняется sampled pose; клавишные удержания и Shift latch не сохраняются. [Подробности](../../04-Engineering/DebugTuningPanel.md#файлы-и-продолжение).

Работает со слотами через session/factory и SaveSlotNaming. Формат данных, очередь команд и combat/economy continuation принадлежат Engine. Нельзя перезаписывать игровое сохранение из устаревшего callback после Defeat. Текущий формат — Save19.

См. [каталог экранов](ScreenCatalog.md) и [основные требования](../../01-Requirements/EngineRequirements.md).

Источники: [SaveScreen.cs](../../../src/DeepSpaceSaga.Client/UI/Screens/Save/SaveScreen.cs).
