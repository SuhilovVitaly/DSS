---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0020-bugs"
ticket: "EP-0012-US-0020-TK-0003-npc-torpedo-observed-trail"
title: "NPC-торпеда: точка, короткий хвост и тёмный пройденный след"
stage: "draft"
layer: "client"
depends_on: []
files_touched: 5
serves: ["US20-AC3"]
created: "2026-10-21T12:02:46.488Z"
revision: 1
---

# TK-0003 — NPC-торпеда: точка, короткий хвост и тёмный пройденный след

[История](../EP-0012-US-0020-bugs.md) · [Контракт эпика](../../Documentation.md)

## Why

Скриншоты 3–4 показывают длинную яркую линию и пунктир вперёд от NPC-торпеды. Покрывается US20-AC3.

## Decisions / Assumptions

D20-03: прямой запрос пользователя 2026-10-21; описание и скриншоты сохранены в истории.

NPC-торпеда отображается только точкой с небольшим активным хвостом позади, длина которого зависит от скорости, и едва заметной почти чёрной линией уже пройденного пути. Не показываются стрелка направления, будущий луч, прогноз пути или точка назначения. История строится только из display-admitted позиций; скрытые или будущие кадры не используются. Стиль собственной торпеды и логика угроз сохраняются.

A20: точные визуальные параметры и внутренние helpers — технические предложения, не одобренные пользователем числа. Причину дефекта проверить при реализации; скриншот не доказывает диагноз.

## Code context

Пути относительно DSS; evidence относится к baseline 89e084c6. Разрешены только эти файлы; всё вне allowlist запрещено. Board не входит в allowlist реализации.

| File | Current state / evidence | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.PointProjectiles.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.PointProjectiles.cs) | 13: DrawPointProjectile; geometry.Prediction рисуется также для observed | Изменить для US20-AC3 в пределах описанного результата |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs) | 37: DrawPointProjectile; 137: маркер projectile | Изменить для US20-AC3 в пределах описанного результата |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs) | GameSessionScreen — render states и lifecycle | Изменить для US20-AC3 в пределах описанного результата |
| [src/DeepSpaceSaga.Client/UI/CombatVisualSettings.cs](../../../../src/DeepSpaceSaga.Client/UI/CombatVisualSettings.cs) | 179: ForeignTrace | Изменить для US20-AC3 в пределах описанного результата |
| tests/DeepSpaceSaga.Client.Tests/PointCombatUS0020TK0003Tests.cs (NEW) | NEW — предлагаемый файл | Именованные проверки ниже |

## Public API after the change

No public API change. Contracts/Engine/Motion/save schema сохраняются. Внутренние Client helpers допускаются в allowlist; новые публичные поля не требуются.

## Implementation steps

1. Проверить симптом и актуальность перечисленных точек интеграции.
2. Реализовать описанный результат в allowlist, используя display-admitted state.
3. Добавить named cases ниже в matching Client test file, проверить соседние регрессии.
4. Зафиксировать automated evidence и отдельно native/product acceptance.

## Out of scope / Invariants

Только US20-AC3. Не менять AI, наведение, RNG, урон, баланс, Engine-команды и сохранения. Сохранить паузу и общий scene clock. Соседние тикеты не исполняются неявно.

## Tests

- Foreign_torpedo_has_no_forward_geometry: летящая NPC-торпеда; нет сегментов будущей линии или стрелки.
- Foreign_tail_scales_with_speed: при одном масштабе более быстрая торпеда имеет более длинный ограниченный хвост; при нулевой скорости хвост нулевой.
- Foreign_trace_uses_presented_history: будущий snapshot не добавляет след до admission; load/reset/повтор ID не соединяют старую историю.
- Own_torpedo_visuals_remain: собственные прогноз и подрыв доступны.

План при реализации: dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter FullyQualifiedName~PointCombatUS0020TK0003Tests. Hard cap каждого процесса 300000 ms с завершением дерева. Затем релевантные Client regressions, owning build, scoped format и git diff --check.

Native: повторить исходный пользовательский сценарий, проверить обычный масштаб, zoom/pan и resize; сверить ожидаемый результат. Визуальные критерии требуют отдельной человеческой проверки.

## Definition of Done

US20-AC3 доказан named tests; owning build/scoped format/diff проходят. Ручной результат и ограничения записаны отдельно. Сейчас implementation NOT STARTED; automated/native/product NOT RUN. Текущее поручение — только planning, без игры и commit/push.

## Execution scope update

Include Data/UI/combat-visuals.json (production override of ForeignTrace) and existing PointCombatUS0010TK0002Tests.cs (old foreign profile labels replaced by point-only presentation).

Execution: displayed NPC geometry is empty forward path plus a speed-scaled rear tail (0–18px), no profile/ETA labels, dark confirmed trail with bounded 4096 positions per live object. History resets on session/sequence/time rollback or disappearance; no pre-launch bootstrap. Threat classification retained.

Client scoped tests: 87/87 passed including 4 new cases and US10/US12 regressions. Build succeeded. Formatter initially found new initializer whitespace; corrected scoped. Native/product NOT RUN/OPEN. Self-review completed. Minor scope extension: trim trailing blank lines introduced in two directly related TK2 tests; prior TK2 diff-check statement was inaccurate (one trailing blank line), corrected here.
