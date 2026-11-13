---
epic: EP-0023-pilot-card-commands
story: EP-0023-US-0005-compact-panel
ticket: EP-0023-US-0005-TK-0001-compact-panel
title: Сжатая правая панель движения и стрельбы
stage: draft
layer: client-ui
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0023-US-0002-TK-0001-move-block, EP-0023-US-0003-TK-0002-fire-block-input]
files_touched: 6
serves: [US5-AC1, US5-AC2]
created: 2026-11-11T14:30:00Z
revision: 1
---

# Сжатая правая панель движения и стрельбы

## Why

Команды и подробности теперь в карточке; правая панель должна их не дублировать (D-02, D-03).

## Decisions

Применяются [D-01–D-04](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md); всё, что там не названо, остаётся как в EP-0019, EP-0020 и EP-0022. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-11.

## Code context

Строки — `main` @ `8b0bf6ce`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.cs` | :18-53 `Draw`: `_db` по фазе, ветки `Move`/`Fire` | Для `Move` и `Fire` — `_db = 32` и только `StandardAction` (итог, статус, кнопка) при любом выборе. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.Overlay.cs` | :17-20 `OverlayBottom` | Для движения и стрельбы — `120 + 32`. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.cs` | :99 зона подписей справа | Проверить, что она следует `Panel.Bottom` (без правки, если так). |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/OverlayShellTests.cs` | границы панели по фазам | Новые границы UiSpec §8. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BuildCardRenderTests.cs` | положение «Пилот и сборка» | Новые координаты (`158…458`). |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/HoverCardTests.Anomalies.cs` | положение карточек наведения | Новые координаты. |

## Public API after the change

Без изменения публичного API.

## Implementation steps

1. Сжать панель в движении и стрельбе по UiSpec §8.
2. Обновить `OverlayBottom` и проверить положение зависимых карточек.
3. Обновить тесты границ панели, «Пилот и сборка» и карточек наведения.

## Out of scope

Удаление старого кода панели (TK-0002).

## Invariants

Панели залпа, результатов и итога не меняются.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `OverlayShellTests`
- `BuildCardRenderTests`
- `HoverCardTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~OverlayShellTests|FullyQualifiedName~BuildCardRenderTests|FullyQualifiedName~HoverCardTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0023/<run>/us5-tk1" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.Overlay.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US5-AC1, US5-AC2 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Геометрия — UiSpec §8; затронутые места перечислены в Code context.

## Исполнение

Не начато.
