---
epic: EP-0023-pilot-card-commands
story: EP-0023-US-0004-salvo-in-card
ticket: EP-0023-US-0004-TK-0001-salvo-block
title: Блок залпа и итога выстрела в карточке
stage: draft
layer: client-ui
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0023-US-0001-TK-0001-column-layout]
files_touched: 4
serves: [US4-AC1, US4-AC2]
created: 2026-11-11T14:30:00Z
revision: 1
---

# Блок залпа и итога выстрела в карточке

## Why

Раскрытая карточка должна вести себя одинаково во всех фазах хода: раскрыт тот пилот, о котором сейчас идёт речь.

## Decisions

Применяются [D-01–D-04](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md); всё, что там не названо, остаётся как в EP-0019, EP-0020 и EP-0022. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-04.

## Code context

Строки — `main` @ `8b0bf6ce`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardSalvoBlock.cs` | — | Новый файл: высота и отрисовка блока UiSpec §7. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs` | функция высоты блока | Для полёта залпа и результатов — высота блока у выбранной карточки. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ResultsPhaseTests.Decoys.cs` | результаты с ловушками | Ожидания строки итога в карточке. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/PilotCardSalvoBlockTests.cs` | — | Пробы UiSpec §10.4. |

## Public API after the change

`internal static class PilotCardSalvoBlock { float Height(...); void Draw(...); }`.

## Implementation steps

1. Реализовать блок по UiSpec §7 для полёта и результатов.
2. Подключить высоту к раскладке колонки.
3. Тесты на сценах `3-salvo-flight`, `4-salvo-results`, `4-anomaly-absorbed`, результатах с ловушкой и уклонением.

## Out of scope

Правая панель залпа.

## Invariants

Данные выстрела — из `TacticalSalvoFrame` и проецированного снимка (`TacticalBattleResults.Project`).

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `PilotCardSalvoBlockTests`
- `ResultsPhaseTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PilotCardSalvoBlockTests|FullyQualifiedName~ResultsPhaseTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0023/<run>/us4-tk1" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardSalvoBlock.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US4-AC1, US4-AC2 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Геометрия и тексты — UiSpec §7.

## Исполнение

Не начато.
