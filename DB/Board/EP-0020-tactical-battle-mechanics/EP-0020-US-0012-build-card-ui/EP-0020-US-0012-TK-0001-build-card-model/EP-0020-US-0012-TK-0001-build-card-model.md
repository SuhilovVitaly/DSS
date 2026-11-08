---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0012-build-card-ui
ticket: EP-0020-US-0012-TK-0001-build-card-model
title: Модель карточки «Пилот и сборка»
stage: draft
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0020-US-0011-TK-0002-slot-snapshot, EP-0020-US-0010-TK-0004-anomaly-view-regression]
files_touched: 3
serves: [US12-AC1]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Модель карточки «Пилот и сборка»

## Why

Логика выбора дрона и текстов отделена от рисования и тестируется без растра.

## Decisions

Применяются [D-34–D-42](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-35: показываемый дрон — свой дрон под курсором, иначе выбранный свой дрон фазы (движение и стрельба — выбранный; залп — активный стрелок, если он свой, иначе последний выбранный; результаты — пилот показанного выстрела; итог боя — нет карточки). Если под курсором противник, носитель или аномалия — их прежняя карточка.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BuildCardModel.cs` | нет | Модель: дрон, заголовок, ячейки, итоги. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/HoverCard.cs` | после US-0010 TK-0003 | Выбор между карточкой сборки и прежними карточками. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BuildCardModelTests.cs` | нет | Тесты. |

## Public API after the change

`BuildCardModel.From(TacticalBattleSnapshot, string? hoveredId, string? selectedId)`.

## Implementation steps

1. Правило A-35.
2. Ячейки и тексты §23.
3. Итоги §24.
4. Тесты.

## Out of scope

Рисование.

## Invariants

Без своего дрона в фокусе поведение карточки наведения как раньше.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `BuildCardModelTests.Shows_hovered_own_then_selected`
- `BuildCardModelTests.Enemy_hover_keeps_old_card`
- `BuildCardModelTests.Cells_and_texts_follow_slots`
- `BuildCardModelTests.Damaged_slots_are_alerts`
- `BuildCardModelTests.Names_are_ellipsized_to_cell_width`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~BuildCardModelTests|FullyQualifiedName~HoverCardTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us12-tk1" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BuildCardModel.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/HoverCard.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US12-AC1 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Правило и тексты заданы.

## Исполнение

Не начато.
