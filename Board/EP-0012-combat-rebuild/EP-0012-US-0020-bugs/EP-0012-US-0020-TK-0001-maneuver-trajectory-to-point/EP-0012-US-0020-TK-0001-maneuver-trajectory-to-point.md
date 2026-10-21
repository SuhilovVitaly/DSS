---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0020-bugs"
ticket: "EP-0012-US-0020-TK-0001-maneuver-trajectory-to-point"
title: "Прогноз манёвра до точки и дальше за экран"
stage: "draft"
layer: "client"
depends_on: []
files_touched: 5
serves: ["US20-AC1"]
created: "2026-10-21T12:02:46.488Z"
revision: 1
---

# TK-0001 — Прогноз манёвра до точки и дальше за экран

[История](../EP-0012-US-0020-bugs.md) · [Контракт эпика](../../Documentation.md)

## Why

На скриншоте 1 после Ctrl+ЛКМ видна короткая дуга, не достигающая выбранной точки. Покрывается US20-AC1.

## Decisions / Assumptions

D20-01: прямой запрос пользователя 2026-10-21; описание и скриншоты сохранены в истории.

После Ctrl+ЛКМ прогноз выбранного манёвра достигает указанной точки и непрерывно продолжается по прямой до выхода за экран. Дальний выбор не обрезается горизонтом 5 секунд. Геометрия поворота соответствует текущей модели движения; нельзя заменить манёвр выдуманной прямой от корабля к точке.

A20: точные визуальные параметры и внутренние helpers — технические предложения, не одобренные пользователем числа. Причину дефекта проверить при реализации; скриншот не доказывает диагноз.

## Code context

Пути относительно DSS; evidence относится к baseline 89e084c6. Разрешены только эти файлы; всё вне allowlist запрещено. Board не входит в allowlist реализации.

| File | Current state / evidence | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatTrajectoryProjector.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatTrajectoryProjector.cs) | 10: BuildOwnPath ограничен 5000 ms | Изменить для US20-AC1 в пределах описанного результата |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs) | 15: DrawOwnCombatPath | Изменить для US20-AC1 в пределах описанного результата |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs) | 1868: DrawExternalPlayerTrajectory; 1931: CaptureDrawnTrajectory | Изменить для US20-AC1 в пределах описанного результата |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/TrajectoryViewportGeometry.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/TrajectoryViewportGeometry.cs) | TrajectoryViewportGeometry | Изменить для US20-AC1 в пределах описанного результата |
| tests/DeepSpaceSaga.Client.Tests/PointCombatUS0020TK0001Tests.cs (NEW) | NEW — предлагаемый файл | Именованные проверки ниже |

## Public API after the change

No public API change. Contracts/Engine/Motion/save schema сохраняются. Внутренние Client helpers допускаются в allowlist; новые публичные поля не требуются.

## Implementation steps

1. Проверить симптом и актуальность перечисленных точек интеграции.
2. Реализовать описанный результат в allowlist, используя display-admitted state.
3. Добавить named cases ниже в matching Client test file, проверить соседние регрессии.
4. Зафиксировать automated evidence и отдельно native/product acceptance.

## Out of scope / Invariants

Только US20-AC1. Не менять AI, наведение, RNG, урон, баланс, Engine-команды и сохранения. Сохранить паузу и общий scene clock. Соседние тикеты не исполняются неявно.

## Tests

- Maneuver_reaches_target_beyond_five_seconds: дальняя допустимая точка; путь достигает точки.
- Maneuver_extends_straight_outside_viewport: после точки прямой сегмент выходит за viewport без разрыва.
- Maneuver_pause_zoom_pan_retarget: пауза не меняет физический путь; zoom/pan и новая точка обновляют отображение без старого хвоста.

План при реализации: dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter FullyQualifiedName~PointCombatUS0020TK0001Tests. Hard cap каждого процесса 300000 ms с завершением дерева. Затем релевантные Client regressions, owning build, scoped format и git diff --check.

Native: повторить исходный пользовательский сценарий, проверить обычный масштаб, zoom/pan и resize; сверить ожидаемый результат. Визуальные критерии требуют отдельной человеческой проверки.

## Definition of Done

US20-AC1 доказан named tests; owning build/scoped format/diff проходят. Ручной результат и ограничения записаны отдельно. Сейчас implementation NOT STARTED; automated/native/product NOT RUN. Текущее поручение — только planning, без игры и commit/push.

## Execution — 2026-10-21

Current authorization: user invokes EpicExecutionPrompt; earlier planning-only limitations are historical. Implemented full selected Orbit trajectory via shared predictor at steering boundaries and shared arrival test for analytic locked leg, followed by viewport continuation. Non-navigation own forecast retains five seconds. Client-only, no rules/save changes. Self-review: escape phases remain sampled, never treated as final course; malformed/nonconvergent steering bounded at 20000 iterations.

Validation: `dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~TrajectoryViewportTests|FullyQualifiedName~NavigationTrajectory|FullyQualifiedName~PointCombatUS0020TK0001Tests"`: 41 passed (including 3 named new tests). Client and dependency builds succeeded as part of test. Scoped `dotnet format ... --verify-no-changes --no-restore --include <three changed Client files>`: exit 0. `git diff --check`: exit 0. Each check bounded at 300000ms. Native/product: NOT RUN, human visual acceptance OPEN.
