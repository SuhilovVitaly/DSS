---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0011-build-slots
ticket: EP-0020-US-0011-TK-0002-slot-snapshot
title: Заполнение слотов в снимке
stage: done
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0020-US-0011-TK-0001-slot-contracts]
files_touched: 2
serves: [US11-AC2, US11-AC3]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Заполнение слотов в снимке

## Why

Список слотов строится из состояния дрона по одному правилу.

## Decisions

Применяются [D-34–D-42](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-36: состояния только из существующих систем повреждений (`Jammed`, множитель двигателя); щиты, плиты и модуль всегда `Ok`.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSnapshotBuilder.cs` | :20-33 `Build`, `Units` | `Slots`, `EngineSlots`, `ModuleSlots`. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/SlotSnapshotTests.cs` | нет | Тесты. |

## Public API after the change

No new API beyond US11-AC1.

## Implementation steps

1. Построить список по US11-AC2.
2. Тесты: порядок и пустые слоты для рам `light`, `heavy`, `thug`; заклинившая пушка; повреждённый двигатель; носитель пустой; сверка с прежними полями на seed 1…20; исходы и `RandomCalls` прежние.

## Out of scope

Клиент.

## Invariants

Исходы боёв и RNG не меняются.

## Tests

Проект `tests/DeepSpaceSaga.Engine.Tests`. Именованные проверки:

- `SlotSnapshotTests.Slots_follow_frame_order_with_empty_slots`
- `SlotSnapshotTests.Damaged_systems_are_marked`
- `SlotSnapshotTests.Slots_agree_with_existing_fields`
- `SeriesParityTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~SlotSnapshotTests|FullyQualifiedName~SeriesParityTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us11-tk2" --logger "trx;LogFileName=engine.trx"
dotnet format whitespace src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore --include src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSnapshotBuilder.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US11-AC2, US11-AC3 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Правило построения задано.

## Исполнение

В процессе. Scope расширен на Golden/Series test signatures, два legacy hash tests и client reference data: удаляются только новые Slots/EngineSlots/ModuleSlots из исторического signature oracle, прежние outcome/units/RNG assertions сохраняются. Полные direct/explicit подписи сравниваются со слотами; нормализация только для byte-exact сравнения5000старыхM3подписей из US9. Данные client baseline обновляются только добавлением metadata;12растров должны остаться идентичны. armor.none остаётся установленной записью каталога с прежним Build.ArmorId; пустые slot определяются отсутствием установленной записи (shield/module/engine).

Завершено. SlotSnapshotTests:24PASS (3 рамы, повреждения/истощённый модуль, все фазы20seeds); focused128PASS,5000direct/explicit пар с полными SlotsPASS; нормализованные5000подписей byte-exact прежним US9. Client34PASS: metadata-only JSON comparisonPASS,12растров без изменений. Build включён в тесты, scoped format/diffPASS. Evidence: `D:/DeepSpaceSaga/artifacts/validation/ep-0020/20261108-execution/us11-tk2/` (final-focus/tests.trx,series-comparison.json,metadata-comparison.json,client-view/tests.trx). Self-review: только DTO projection; пустые слоты, состояния, отсутствие RNG mutation подтверждены. US11-AC2/3 покрыты полностью автоматизированными проверками.
