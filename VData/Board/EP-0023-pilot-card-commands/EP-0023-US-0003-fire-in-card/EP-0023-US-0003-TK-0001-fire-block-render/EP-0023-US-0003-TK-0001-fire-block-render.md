---
epic: EP-0023-pilot-card-commands
story: EP-0023-US-0003-fire-in-card
ticket: EP-0023-US-0003-TK-0001-fire-block-render
title: Блок стрельбы в карточке: состояния и подробности
stage: done
layer: client-ui
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0023-US-0001-TK-0003-card-commands-input]
files_touched: 8
serves: [US3-AC1, US3-AC2]
created: 2026-11-11T14:30:00Z
revision: 1
---

# Блок стрельбы в карточке: состояния и подробности

## Why

Пользователь потребовал, чтобы выбор цели, ловушка и уклонение были видны только на панели выбранного дрона (D-02), а подробности переехали туда же (D-03).

## Decisions

Применяются [D-01–D-04](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md); всё, что там не названо, остаётся как в EP-0019, EP-0020 и EP-0022. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-07, A-08, A-09. Строки «под огнём» и подсказки обрезаются многоточием после подгонки до 6,5; список целей показывает три строки.

## Code context

Строки — `main` @ `8b0bf6ce`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardFireBlock.cs` | — | Новый файл: высота и отрисовка блока по UiSpec §6 для четырёх состояний; регистрация команд `target:<id>`, `decoy`, `hold`, `evade`. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.Fire.cs` | :64-91 `FireCaption`, `DustText`, `FireSummary`, `ThreatText`, `DecoyText` | Переиспользовать статические функции текстов (без изменений смысла). |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleDesign.cs` | :20-56 `DecoyRow`, `EvadeButton` | Переиспользовать; при необходимости параметр размера шрифта для высоты строки 18/17. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs` | функция высоты блока | Для фазы стрельбы — высота блока стрельбы у выбранной карточки. |
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | ключи `TacticalBattle.*` | Новые строки UiSpec §6.6. |
| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | ключи `TacticalBattle.*` | Те же ключи на английском. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/PilotCardFireBlockTests.cs` | — | Пробы UiSpec §10.3 для четырёх состояний. |

PilotCardColumnTests.cs также обновляет прямоугольники выбранной карточки стрельбы, сохраняя проверки вертикальных шкал.

## Public API after the change

`internal static class PilotCardFireBlock { float Height(TacticalFirePlanRow row, TacticalBattleSnapshot battle); void Draw(...); }`.

## Implementation steps

1. Реализовать состояния «есть цели», «уклоняется», «нет целей», «в пыли» по UiSpec §6.1–§6.5.
2. Подключить высоту блока к раскладке колонки.
3. Добавить строки локализации и проверить их покрытие (`LocalizationCoverageTests`).
4. Тесты проб и текстов на сценах EP-0019/EP-0020 (seed 1, ход 4; сцены уклонения, пыли, «нет целей», пустой ловушки).

## Out of scope

Обработка кликов и колеса (TK-0002); правая панель (US-0005).

## Invariants

Шансы, урон, заряды и угрозы берутся из `TacticalFirePlan` и снимка боя, не вычисляются заново.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `PilotCardFireBlockTests`
- `LocalizationCoverageTests`
- `GlyphCoverageTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PilotCardFireBlockTests|FullyQualifiedName~LocalizationCoverageTests|FullyQualifiedName~GlyphCoverageTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0023/<run>/us3-tk1" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardFireBlock.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US3-AC1, US3-AC2 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Геометрия, состояния и тексты — UiSpec §6; источники текстов перечислены в Code context.

## Исполнение

US3-AC1/AC2: четыре состояния, геометрия/урон, цвет и величина поглощения, защита, угроза, заряд и цена уклонения показаны в выбранной карточке. Пробы UiSpec §10.2/§10.3 и локализации прошли. Build, 21/21 тестов, scoped format и diff check: D:/DeepSpaceSaga/artifacts/validation/ep-0023/20261111-execution-01/us3-tk1-02/. Самопроверка diff выполнена. Снятие дублирования справа и итоговые растры идут по зависимостям.
