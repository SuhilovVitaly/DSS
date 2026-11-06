---
epic: EP-0016-tactical-battle-skins
story: EP-0016-US-0003-select-skin-in-settings
ticket: EP-0016-US-0003-TK-0001-persist-skin-selection
title: Хранение выбранного скина
stage: approved
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0016-US-0001-TK-0002-discover-and-load-packs, EP-0016-US-0002-TK-0005-theme-information-tabs]
files_touched: 2
serves: [US3-AC2, US3-AC3]
created: 2026-11-06T08:02:55Z
revision: 1
---

# Хранение выбранного скина

## Why

Предпочтение игрока должно пережить перезапуск и быть единым для всех слотов.

## Decisions

Применяются [D-01–D-04](../../Documentation.md#подтверждённые-требования) и [утверждение истории](../EP-0016-US-0003-select-skin-in-settings.md#решения-и-согласование). Разрешение касается планирования. Статус тикета присвоен по workflow epic-writer.

## Assumptions

A-09. Используется Settings.json, gameSettings.tacticalBattleSkinId. Изменение выполняется на UI-потоке. Ошибка записи оставляет прежний фактический выбор.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleSkinPreference.cs` | Новый файл. SkiaWindow.cs:1372, :1431–1477 и :1514–1535 задают существующий путь и формат gameSettings. | Чтение выбранного ID и безопасная запись одного свойства. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SkinPreferenceTests.cs` | Новый файл. Тесты пишут реальные временные файлы, отдельно от runtime settings. | Проверить round-trip, отсутствующие значения, повреждения и ошибку записи. |

## Public API after the change

Internal static TacticalBattleSkinPreference с Read(string path, Action<string>? warning = null) → string? и TryWrite(string path, string id, Action<string>? warning = null) → bool. Read возвращает сохранённый валидный ID или null. Разрешение доступности ID принадлежит каталогу.

## Implementation steps

1. Использовать существующее дерево gameSettings. При отсутствии файла или gameSettings создать нужный объект. Прочие поля сохранить. Если существующий документ повреждён или root/gameSettings неверного типа, отказать в записи с диагностикой.
2. Писать во временный соседний файл и заменять целевой только после успешной записи. Удалять временный файл при отказе. Не менять файл, если ID уже сохранён. Проверять формат ID по правилам US-0001, включая classic.
3. Проверить повторное чтение с новым объектом/процессным состоянием, сохранение соседних полей и fallback при чтении. Закрепить отказ записи без изменения исходного файла.

## Out of scope

Правила сохранения мира, перенос настроек дисплея/языка и UI переключателя.

## Invariants

Settings.json принадлежит клиенту (SkiaWindow.cs:1372). Сохранение мира исключает параметры временного боя (EngineRequirements.md:108–117).

## Tests

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `SkinPreferenceTests.Selected_id_round_trips_without_rewriting_other_settings`
- `SkinPreferenceTests.Missing_or_invalid_value_uses_classic_and_failed_write_preserves_file`

Будущие команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms и сохранять его вывод рядом с TRX.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~SkinPreferenceTests.Selected_id_round_trips_without_rewriting_other_settings|FullyQualifiedName~SkinPreferenceTests.Missing_or_invalid_value_uses_classic_and_failed_write_preserves_file" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0016/<run>/us3-tk1" --logger "trx;LogFileName=focused.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleSkinPreference.cs --verify-no-changes
dotnet format whitespace tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --include tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SkinPreferenceTests.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги внутри allowlist.
- Для US3-AC2, US3-AC3 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены с фактическими результатами. Исходное падение при наличии записано отдельно.
- Соблюдены инварианты и границы изменений. Реальные команды и результат проверок записаны в evidence исполнения.

## Self-containment check

Путь передаёт caller, формат и стратегия отказа заданы. Каталог и runtime интеграция принадлежат другим тикетам.
