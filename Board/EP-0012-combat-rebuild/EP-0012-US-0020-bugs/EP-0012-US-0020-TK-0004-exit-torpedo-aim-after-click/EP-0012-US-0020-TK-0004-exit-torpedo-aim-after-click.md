---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0020-bugs"
ticket: "EP-0012-US-0020-TK-0004-exit-torpedo-aim-after-click"
title: "Завершать выбор точки торпеды после клика пуска"
stage: "draft"
layer: "client"
depends_on: []
files_touched: 3
serves: ["US20-AC4"]
created: "2026-10-21T12:02:46.488Z"
revision: 1
---

# TK-0004 — Завершать выбор точки торпеды после клика пуска

[История](../EP-0012-US-0020-bugs.md) · [Контракт эпика](../../Documentation.md)

## Why

По описанию и скриншоту 4 после выбора точки ЛКМ режим выбора/preview остаётся активным. Покрывается US20-AC4.

## Decisions / Assumptions

D20-04: прямой запрос пользователя 2026-10-21; описание и скриншоты сохранены в истории.

При активном торпедном прицеливании один ЛКМ по допустимой точке без дополнительного предупреждения фиксирует координаты и инициирует ровно одну команду. После успешной постановки отправки PointAim и preview очищаются сразу. Перенос мыши не выбирает новую точку, следующий обычный ЛКМ не запускает вторую торпеду. Ожидание/ошибка команды остаются видны в панели оружия. Новый пуск требует явного входа в прицеливание. Действующие проверки дальности, готовности и friendly-fire сохраняются: до обязательного подтверждения или при невозможности начать отправку выбор остаётся активным.

A20: точные визуальные параметры и внутренние helpers — технические предложения, не одобренные пользователем числа. Причину дефекта проверить при реализации; скриншот не доказывает диагноз.

## Code context

Пути относительно DSS; evidence относится к baseline 89e084c6. Разрешены только эти файлы; всё вне allowlist запрещено. Board не входит в allowlist реализации.

| File | Current state / evidence | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Aiming.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Aiming.cs) | 43: CancelPointAim; 154: HandlePointAimMouse не вызывает отмену после отправки | Изменить для US20-AC4 в пределах описанного результата |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.TorpedoAimPreview.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.TorpedoAimPreview.cs) | 60: ConfirmPointFriendlyFire; 80: DrawTorpedoAimPreview | Изменить для US20-AC4 в пределах описанного результата |
| tests/DeepSpaceSaga.Client.Tests/PointCombatUS0020TK0004Tests.cs (NEW) | NEW — предлагаемый файл | Именованные проверки ниже |

## Public API after the change

No public API change. Contracts/Engine/Motion/save schema сохраняются. Внутренние Client helpers допускаются в allowlist; новые публичные поля не требуются.

## Implementation steps

1. Проверить симптом и актуальность перечисленных точек интеграции.
2. Реализовать описанный результат в allowlist, используя display-admitted state.
3. Добавить named cases ниже в matching Client test file, проверить соседние регрессии.
4. Зафиксировать automated evidence и отдельно native/product acceptance.

## Out of scope / Invariants

Только US20-AC4. Не менять AI, наведение, RNG, урон, баланс, Engine-команды и сохранения. Сохранить паузу и общий scene clock. Соседние тикеты не исполняются неявно.

## Tests

- Torpedo_click_submits_once_and_exits: допустимая точка; одна команда с захваченными координатами, PointAim/preview очищены.
- Mouse_after_launch_does_not_repeat: перенос мыши/второй ЛКМ/задержанное подтверждение не меняют endpoint и не дают второй команды.
- Invalid_click_or_friendly_warning_keeps_aim: вне дальности/перезарядка/первое friendly-fire предупреждение не отправляют команду и не завершают выбор.
- Failure_and_session_reset_do_not_restore_aim: ошибка доступна в панели, aiming автоматически не возвращается; новая сессия очищает pending.

План при реализации: dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter FullyQualifiedName~PointCombatUS0020TK0004Tests. Hard cap каждого процесса 300000 ms с завершением дерева. Затем релевантные Client regressions, owning build, scoped format и git diff --check.

Native: повторить исходный пользовательский сценарий, проверить обычный масштаб, zoom/pan и resize; сверить ожидаемый результат. Визуальные критерии требуют отдельной человеческой проверки.

## Definition of Done

US20-AC4 доказан named tests; owning build/scoped format/diff проходят. Ручной результат и ограничения записаны отдельно. Сейчас implementation NOT STARTED; automated/native/product NOT RUN. Текущее поручение — только planning, без игры и commit/push.

## Execution — 2026-10-21

Successful torpedo send setup immediately clears PointAim and both previews. Pending/errors remain associated with the last submitted module and displayed by its weapon panel; no automatic reentry. Invalid/range/reload/friendly confirmation paths retain aiming. Countermissile behavior unchanged. Self-review completed.

Validation: Client tests filter `FullyQualifiedName~PointCombatUS0020TK0004Tests|FullyQualifiedName~PointCombatUS0011|FullyQualifiedName~BasicCombatUiFlowTests`: 56/56 passed (4 new; existing real rejection, delayed admission, reload, friendly-fire and full UI flow retained). Client build passed. Scoped format exit 0; diff check exit 0. Hard cap 300000ms. Native/product NOT RUN/OPEN.
