---
epic: EP-0010-combat-reports
story: EP-0010-US-0011-report-acceptance
ticket: EP-0010-US-0011-TK-0003-native-visual-and-performance
title: "Визуальная приёмка и frame-time evidence"
stage: draft
status: evidence-recorded-open
layer: client
depends_on: ["EP-0010-US-0011-TK-0005-hud-and-fixture-regressions"]
files_touched: 4
serves: ["US11-AC3"]
created: 2026-10-16
revision: 1
validation_status: partial-native-user-policy
---

# Визуальная приёмка и frame-time evidence

## Why

Заполнить отдельные gates наблюдением настоящего клиента. [История](../EP-0010-US-0011-report-acceptance.md), [ТЗ](../../../../Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R08, R09, R10, R11, R17, R19, R20, R21, R22, R23, R24.

## Decisions

Пользователь поручил сохранить ТЗ и создать эпик с историями/тикетами (2026-10-16). Выполнение этого тикета, commit/push, генерация art или запуск игры пока не поручены. Продуктовые решения — §§1–10 ТЗ; не выдавать A01–A10 за дополнительные ответы пользователя.

## Assumptions

Технические A01–A10 ТЗ. Proposed API ниже — ожидаемый результат, не утверждение о текущем коде. Максимум 5 файлов из таблицы; Board evidence не разрешает скрытые изменения production. Layer `client`; matching coverage `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Для purely art/evidence runtime source не меняется.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `Board/EP-0010-combat-reports/evidence/NativeAcceptance.md` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |
| `Board/EP-0010-combat-reports/evidence/Performance.md` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |

Read-only общий [grounding](../../Documentation.md#grounding) содержит текущие точки входа и ограничения. Остальные файлы не разрешены к изменению. Зависимости: EP-0010-US-0011-TK-0002-client-integration-proof; их новый код/DTO считаются доступными только после завершения.

## Public API after the change

No public API change.

## Implementation steps

1. Запустить CombatMukha и воспроизводимые visual fixtures при разрешённом execution; пройти1280x720/1920x1080, UI 100/120/150, Russian/English, collapsed/open Object Info,0/200 карточек, missing resources.
2. Зафиксировать читаемость8 иллюстраций/портретов, click/wheel/keys/focus, auto-expand, clipping и Defeat. Сохранить доказательства/пути/параметры, реальные PASS/FAIL по каждому пункту.
3. Измерить frame intervals на одинаковом seed/скорости/viewport с 0/200 докладами; baseline и new p50/p95/p99/max, target12.5 ms, run duration/hardware. Не заявлять80 FPS по одной средней величине. Не запускать игру на стадии создания Board.

## Out of scope

Изменение баланса/конфигурации оружия, формул попадания, RNG, часов, поведения AI, discovery rules, посторонних журналов и незакоммиченных изменений. Никакой публикации или автоматического выполнения следующего тикета без execution-поручения. Root save version не менять в обход A07.

## Invariants

- Engine/renderer separation: `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs:10`; immutable snapshot: `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:11`. Через boundary нет Skia/GL.
- Физическое время/модульный бой — EngineRequirements §Phase Four Module Combat и §§7–8 ТЗ; UI не меняет исходы/часы/sequence журнала.
- Captured identity, privacy, once-per-target и ровно одна открытая карточка проверяются в соответствующих layers; этот тикет не ослабляет смежные гарантии.

## Tests

- `Native checklist and measured performance evidence, or explicit NOT RUN/OPEN with reason` (планируемый сценарий проверки).

Команды выполнять из корня репозитория **при реализации**, сейчас NOT RUN:

```powershell
dotnet run --project src/DeepSpaceSaga.Client --no-restore
# Вручную выполнить evidence/NativeAcceptance.md и evidence/Performance.md. Только во время execution.
```

## Definition of Done

- Шаги выполнены только в 2 разрешённых файлах, выбранный layer соблюдён.
- Критерии US11-AC3 подтверждены именованными проверками выше и ссылками на evidence.
- Focused проверки и соответствующий build/format проходят либо исходное падение записано отдельно; prose/art gates проверены подходящим способом.
- Сохранены public API/invariants/out-of-scope; нет скрытой работы или нерешённых блокирующих допущений.
- Native/human/FPS не помечены PASS по headless; статус NOT RUN/OPEN сохраняется до реального прогона.
- Обновлён status/evidence тикета, выполнен diff check; не заявлять реализацию на основании этого planning-документа.

## Self-containment check

Вход: зависимости по ID, ТЗ, scope и Code context. Выход: Заполнить отдельные gates наблюдением настоящего клиента. Проверки конкретизированы. Если текущий diff требует шестого файла или второго production layer, сначала переразбить/обновить тикет, не расширять allowlist молча.

## Execution scope/order adjustment - 2026-10-16

Run corrective TK0004 after TK0002 and before this native ticket. Confirmed free-map viewport regression must be fixed first.

## Native harness scope before implementation - 2026-10-16

Add tools/DeepSpaceSaga.Performance/CombatReportsNativeEvidence.cs and tools/DeepSpaceSaga.Performance/Program.cs to the two evidence Markdown files (four source/doc files total). Reuse actual SkiaWindow OpenGL frame callback, prepare reports through its production loop, exercise visual fixtures in real native window and record baseline0/history200 frame intervals. Generated JSON/PNG evidence lives under this epic evidence/native. No production runtime changes in this native ticket. Baseline station-timeout comparison uses isolated af594de checkout, not a code fix.

## Execution evidence and user constraint - 2026-10-16

Prepared and Release-built native evidence harness (0 warnings/errors). One already-started1280/UI100/EN sample finished64 frames; layout/history/focus checks PASS, screenshots retained. User then explicitly forbade autonomous game launches. Verified no remaining native reports process; no more native/game/performance runs. Full native matrix, native lifecycle, human art/playtest and600-frame performance are OPEN/NOT RUN by this explicit constraint. This ticket records evidence and limitation; it does not claim complete acceptance. Headless coverage remains separate. Baseline station timeout reproduced on af594de in isolated checkout, see regression/station-baseline.trx versus station-current.trx.
