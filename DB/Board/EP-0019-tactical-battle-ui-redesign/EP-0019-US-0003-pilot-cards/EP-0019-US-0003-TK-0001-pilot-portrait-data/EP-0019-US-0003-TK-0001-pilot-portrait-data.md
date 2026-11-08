---
epic: EP-0019-tactical-battle-ui-redesign
story: EP-0019-US-0003-pilot-cards
ticket: EP-0019-US-0003-TK-0001-pilot-portrait-data
title: Портреты и пол пилотов
stage: done
layer: content-data
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0019-US-0002-TK-0004-localization-strings]
files_touched: 4
serves: [US3-AC4]
created: 2026-11-08T09:31:05Z
revision: 1
---

# Портреты и пол пилотов

## Why

Пилоты боя не имеют портретов в Contracts. Дизайн показывает портрет в карточке и в карточке наведения.

## Decisions

Применяются [D-01–D-09](../../Documentation.md#решения-пользователя). Нормативный вид — [DesignSpec](../../DesignSpec.md). Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-07. Значения — DesignSpec §11.

## Code context

Строки — `main` @ `8c6f4e2a`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/UI/tactical-battle-pilots.json` | нет | `{ "version": 1, "pilots": { "vera": { "portrait": "W4/Portraits/CHR-20260915-102445-3C5C5D", "female": true }, … } }` — значения DesignSpec §11; `portrait` — идентификатор портрета персонажа в каталоге `Images/Persons` клиента, расширение файла добавляет загрузчик. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotPortraitCatalog.cs` | нет | Загрузка JSON из `AppContext.BaseDirectory`, кэш `SKBitmap`, `TryGet(pilotId)`, `IsFemale(pilotId)`; ошибка файла — пустой каталог и запись в лог. |
| `src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj` | `Data/UI/*.json` копируются? | Убедиться, что новый JSON попадает в output. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/PilotPortraitCatalogTests.cs` | нет | Тесты. |

## Public API after the change

`internal sealed class PilotPortraitCatalog { static Load(string baseDir); bool TryGet(string pilotId, out SKBitmap); bool IsFemale(string pilotId); }`.

## Implementation steps

1. Создать JSON с четырьмя пилотами.
2. Реализовать каталог; кроп портрета `170,70…854,754` делает отрисовка, не каталог.
3. Тесты: четыре портрета загружаются, неизвестный пилот → false, повреждённый JSON → пустой каталог без исключения.

## Out of scope

Портреты противника; редактор портретов.

## Invariants

Engine-данные `pilots.json` не меняются.

## Tests

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `PilotPortraitCatalogTests.Loads_reference_pilots`
- `PilotPortraitCatalogTests.Missing_or_broken_file_falls_back`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PilotPortraitCatalogTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0019/<run>/us3-tk1" --logger "trx;LogFileName=focused.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotPortraitCatalog.cs --verify-no-changes
dotnet format whitespace tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --include tests/DeepSpaceSaga.Client.Tests/TacticalBattle/PilotPortraitCatalogTests.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US3-AC4 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты; ожидания DesignSpec не изменены.

## Self-containment check

Пути портретов указаны полностью.

## Исполнение

Не начато.

## Проверенное исполнение 2026-11-08 (продолжение)

Load pilot portraits and grammatical gender from client data with safe fallback

Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0019/20261108-resume/us3-tk1. Self-review выполнен; это не независимое review. Build в составе test run, scoped whitespace и diff-check проверены перед публикацией. Остаточные native/OS gates отражаются в ExecutionStatus.
