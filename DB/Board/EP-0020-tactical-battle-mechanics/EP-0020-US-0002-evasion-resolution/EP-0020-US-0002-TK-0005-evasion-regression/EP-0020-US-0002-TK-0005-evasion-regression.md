---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0002-evasion-resolution
ticket: EP-0020-US-0002-TK-0005-evasion-regression
title: Регрессия боя с уклонением
stage: draft
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0020-US-0002-TK-0004-session-evasion]
files_touched: 3
serves: [US2-AC7]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Регрессия боя с уклонением

## Why

Новая механика меняет ход боёв, где ИИ уклоняется. Нужно доказать, что разные пути исполнения совпадают, и обновить исторические ожидания осознанно.

## Decisions

Применяются [D-01–D-13](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Числа исторических боёв не являются оракулом новой механики (`GoldenBattleTests.cs:15`).

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/GoldenBattleTests.cs` | :23-29, :32-55, :57-88 | Явный путь использует `DefaultPlan`; обновить ожидаемые числа с записью причины. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/SeriesParityTests.cs` | :13-53 | Подписи серий после изменения; прямой = явный. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/InitiativeReferenceOracle.cs` | оракул инициативы | Поддержать `Evading` в ручных случаях, если они затронуты. |

## Public API after the change

No API change.

## Implementation steps

1. Прогнать весь `DeepSpaceSaga.Engine.Tests`.
2. Обновить ожидания только там, где изменение объяснено уклонением ИИ или открытием фазы; список причин записать.
3. Сохранить подписи серий до и после в artifacts.

## Out of scope

Баланс.

## Invariants

Прямой и явный пути совпадают.

## Tests

Проект `tests/DeepSpaceSaga.Engine.Tests`. Именованные проверки:

- `GoldenBattleTests.NewRuleBattlesRepeatAcrossDirectAndExplicitPaths`
- `GoldenBattleTests.HandComputedInitiativeCasesMatchEngineAndOracle`
- `SeriesParityTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~DeepSpaceSaga.Engine.Tests.TacticalBattle" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us2-tk5" --logger "trx;LogFileName=engine.trx"
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US2-AC7 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Затронутые тесты перечислены.

## Исполнение

Не начато.
