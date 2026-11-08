---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0010-anomaly-ui
ticket: EP-0020-US-0010-TK-0002-anomaly-move-fire
title: Аномалии в движении и стрельбе
stage: draft
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0020-US-0010-TK-0001-anomaly-field-layer]
files_touched: 6
serves: [US10-AC2]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Аномалии в движении и стрельбе

## Why

Правила пересечения должны читаться до приказа.

## Decisions

Применяются [D-24–D-33](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Строки RU/EN — UiSpec §21.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs` | оверлеи движения и стрельбы | §17, §18.1–18.2. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.Fire.cs` | после US-0007 TK-0002 | §18.3–18.5. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardModel.cs` | значки | «в пыли», итоговый шанс. |
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | `TacticalBattle.*` | §21. |
| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | `TacticalBattle.*` | §21. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/AnomalyFirePanelTests.cs` | нет | Тексты через модели и пробы §20. |

## Public API after the change

No public API change.

## Implementation steps

1. Выноска конца пути.
2. Стрелка, пунктир участков, выноска поглощения.
3. Панель плана выстрела и «огонь невозможен».
4. Карточки, итог действий, строки.
5. Пробы §20.

## Out of scope

Залп и результаты.

## Invariants

Без аномалий вид фаз движения и стрельбы как после US-0007.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `AnomalyFirePanelTests.Shot_plan_shows_effective_chance_and_absorption`
- `AnomalyFirePanelTests.Dust_drone_panel_and_marks`
- `AnomalyFirePanelTests.Move_end_in_dust_callout`
- `LocalizationCoverageTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~AnomalyFirePanelTests|FullyQualifiedName~FirePanelDecoyTests|FullyQualifiedName~FirePanelEvasionTests|FullyQualifiedName~LocalizationCoverageTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us10-tk2" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.Fire.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardModel.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US10-AC2 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Вид задан UiSpec §17–§18, §21.

## Исполнение

Не начато.
