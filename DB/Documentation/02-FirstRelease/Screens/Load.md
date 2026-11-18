# Загрузка игры

> После saved tuning bootstrap применяет валидный debug-tuning.json обычной командой; отсутствующие ключи частичного файла берутся из каталога. Invalid файл отвергается целиком. Перед чтением файла заменяющей сессией предыдущий writer завершается; Load сбрасывает физический ввод. [Текущий контракт](../../04-Engineering/DebugTuningPanel.md#файлы-и-продолжение).

Показывает слоты, запускает новую локальную сессию только после проверки файла. Load начинает на паузе, восстанавливает materialized состояние и прекращает старые клиентские worker/observation histories. При ошибке файл не переписывается. Save18–21 мигрируют в Save22 по [SaveFormat](../../04-Engineering/SaveFormat.md). Перед заменой сессии прежний autosave приостанавливается и дожидается записи; кандидат не пишет autosave до принятия. При отказе прежняя сессия активируется снова.

Слот с pending encounter восстанавливает тот же бой с начала и сразу открывает
его модальный экран; свободная карта между Load и боем недоступна. Уже captured
или активная встреча не позволяет загрузить другой мир для ухода от боя.

См. [каталог экранов](ScreenCatalog.md) и [основные требования](../../01-Requirements/EngineRequirements.md).

Источники: [LoadScreen.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/Load/LoadScreen.cs).

## Возврат на карту добычи

RecoverBattleFlowAsync в реальном Load/Continue пути принимает Open loot независимо от PendingBattle и показывает frozen Over state без BeginEncounterBattle. Closed loot возвращает мировую карту. Некорректная загрузка не заменяет прежнюю сессию. [Контракт](../../04-Engineering/DemoRewards.md).
