---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0012-phase-three-acceptance
ticket: EP-0008-US-0012-TK-0003-native-ui-acceptance
title: "Нативная приёмка панелей, боя и поражения"
stage: draft
layer: client
depends_on: ["EP-0008-US-0012-TK-0002-end-to-end-combat"]
files_touched: 10
serves: ["AC-0001","AC-0003","AC-0004"]
created: 2026-10-07
revision: 1
---

# Нативная приёмка панелей, боя и поражения

STATUS: DRAFT

## Why

Поставка проверяется на настоящем сценарии и нескольких враждебных кораблях. Автоматические проверки, нативное окно и результаты review фиксируются отдельно. Этот тикет обеспечивает: нативная приёмка панелей, боя и поражения.

## Decisions

Решения Q01–Q51 из [Decisions.md](../../Decisions.md), итоговый [контракт и A01–A12](../../Documentation.md). Применимые критерии [истории](../EP-0008-US-0012-phase-three-acceptance.md): AC-0001, AC-0003, AC-0004. Карточка не вводит нового решения пользователя.

## Assumptions и baseline

HEAD `6907c58`, проверка 2026-10-07. Технические assumptions A01–A12 централизованы в эпике. Новые API и файлы являются планом; не утверждать их наличие до выполнения зависимостей. При новом baseline актуализировать пути и версии до изменения кода. Плановый scope — один production layer, до пяти файлов вместе с тестами. При выполнении через общий промт разрешено обоснованное расширение с записью в Code context.

## Code context

Пути относительно `D:/DeepSpaceSaga/DSS`. Это write scope; остальные первичные файлы можно читать для проверки зависимостей.

| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Client.Tests/PhaseThreeCombatUiFlowTests.cs` | Новый файл этого тикета | Только результат и шаги этой карточки |
| `DB/Documentation/04-Engineering/PhaseThreeCombatAcceptance.md` | Новый файл этого тикета | Только результат и шаги этой карточки |

## Scope refinement — 2026-10-12

Dependency commit `9c93e1d227ac32eb8319ce28dc9eb7c68cbea73c` is published and is an ancestor of live remote HEAD `817a856047589f7346c2e280450df3c28372c4b5`. Existing `Data` commit preserved. The prior publication blocker is resolved by explicit user destination authorization and live verification.

The existing native-window evidence runner needs a combat scenario and scripted UI interactions to make this ticket reproducible. Extend scope before implementation:

| File | Change |
|---|---|
| `tools/DeepSpaceSaga.Performance/CombatNativeEvidence.cs` | New native combat matrix runner using actual Engine/LocalSession/GameSessionScreen/SkiaWindow |
| `tools/DeepSpaceSaga.Performance/Program.cs` | Route the explicit combat evidence command |
| `tools/DeepSpaceSaga.Performance/README.md` | Reproduction and measurement limitations |

Board execution/status/publication evidence accompanies this ticket. Test fixtures may set initial positions/module parameters before NewGame, but never relocate or edit HP during battle. Scripted native interactions and human manual acceptance remain distinct.

## Dependencies

Шаг **42** [общего порядка](../../Tickets.md).
- [EP-0008-US-0012-TK-0002-end-to-end-combat](../EP-0008-US-0012-TK-0002-end-to-end-combat/EP-0008-US-0012-TK-0002-end-to-end-combat.md).
Предыдущий шаг должен быть выполнен и проверен; при автономном исполнении — также опубликован. Нельзя заменять отсутствующий production-контракт временным mock.

## Public API after the change

Предлагаемые DTO/методы/поля реализуют контракт EP-0008 в перечисленных production-файлах. Публичные изменения сериализуемы; Engine владеет guard/outcome, Motion — общей математикой, Client — отображением. Точные имена перечислены в шагах и могут уточняться без изменения семантики.

## Implementation steps

1. Перепроверить baseline, состояния зависимостей и реальные файлы; сохранить чужую работу.
2. Добавить automated click/session tests и выполнить реальное окно. Матрица viewport/UI/zoom/speed/pause; две панели/independent toggle/selected targets/no solution/0%, preview exact launch, countdowns/rings hover/journal scroll/defeat/load cancel+success. Использовать отдельные тестовые save slots. Зафиксировать native evidence/backend; недоступность только NOT RUN. Измерить presentation cadence тяжёлого multi-projectile теста, без подмены raster ms как GPU FPS.
3. Реализовать перечисленные ниже регрессии/проверки. Изменение прежних ожиданий допустимо только при изменённом согласованном контракте; не ослаблять независимые assertions.
4. Review относительно AC и контракта эпика; исправить подтверждённые findings и повторить затронутые проверки.
5. При исполнении через общий промт: записать evidence, отдельный commit с ID тикета и push; не смешивать другой тикет.

## Invariants и Out of scope

- Architecture и временные домены — [эпик](../../Documentation.md): snapshot immutable, Engine authoritative, physical/calendar/real-time разделены.
- Нельзя выдавать full-suite green за доказательство ненаписанного сценария; native проверяется отдельно.
- Недопустимы неоговорённые fuel/ammo/патрульные механики, user commands от имени NPC, скрытые изменения характеристик и повторные RNG после Load.
- Новая фаза требует current-format save; version0 NewGame не путать со старым save.
- Другие production layers не реализуются в этой карточке; необходимое расширение оформляется до изменения.

## Tests и проверки

Имена ниже **планируемые**, не результат выполненного тестирования:

- [ ] `Two_panels_real_click_flow`
- [ ] `Defeat_load_ui_roundtrip`
- [ ] `Preview_and_timer_native_matrix`

Команды из корня DSS после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
git diff --check
```

Для изменённых C# дополнительно `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include <точные изменённые C# пути>`. Фактические команды, backend и failures записать в evidence. Для документации проверить links/counts/schema; новые runtime tests для одной текстовой правки не требуются.

## Definition of Done

- [ ] Все AC-0001, AC-0003, AC-0004 в scope этой карточки покрыты реальным результатом/проверками.
- [ ] Все шаги выполнены, нет скрытых stub или неразрешённых зависимостей.
- [ ] Соответствующие тесты/build/format/diff пройдены; вызванные изменением регрессии исправлены.
- [ ] Review проведён, ограничения/native статус описаны честно.
- [ ] Board evidence актуально; при execution отдельный commit/push подтверждён.
- [ ] Собственное review не помечено пользовательским APPROVED.

## Self-containment check

Решения, числа, порядок событий, допущения, write scope, реальные зависимости и named checks доступны здесь и в связанных документах эпика. Для смысла коротких ответов не требуется искать чат. Предлагаемые API не являются доказательством готовой реализации.

## Native review finding and scope expansion — 2026-10-12

Actual1280x720/UI150 screenshot shows bottom map toolbar covering defense details; scale controls partly under the command panel. The toolbar avoided commands only when AiMap existed. Extend production scope to `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs` and `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs`: position bottom controls beside commands when sufficient horizontal room exists, consistently for generated and custom combat worlds. Add actual rectangle non-overlap assertions to the new UI matrix, then rerun native. No gameplay changes.

Native image review also found PR lifetime map labels could be hidden under HUD controls. Extend scope to `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Countermeasures.cs`: wrap to free map width and avoid HUD rectangles as well as other PR labels. New matrix asserts label visibility/non-overlap. Runner must wait for actual Defeat/Load layout before capturing modal pixels (the window transitions after rendering the preceding screen).

Native capture correction: reading the default framebuffer after SwapBuffers can capture the old back buffer. Add an internal optional `NativeFrameRendered` callback to `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs`, invoked after flush and before transition polling/swap. The harness captures that actual rendered surface and drives the next action there; null in normal gameplay. This avoids mislabelled modal screenshots and enables checking rendered screen identity.

Harness clock correction: fake Engine time with a wall-clock SnapshotBuffer lets x100 prediction run ahead while the fixture clock is stationary. Add internal constructor injection of SnapshotBuffer in `src/DeepSpaceSaga.Client/GameSessionHandle.cs` (public constructor retains default buffer), and pass the same deterministic timestamp provider to buffer/screen in the runner. This is test instrumentation, not a gameplay timing change. Real-clock generated battle remains covered separately.

## Execution result — 2026-10-12

Implementation, automated checks and self-review complete; native acceptance PARTIAL/OPEN. Client1855/1855; solution build0 warnings/errors; scoped format/diff PASS. Existing native artifacts:13/14 interactions complete, one incomplete; 80FPS target not accepted. See [acceptance report](https://github.com/SuhilovVitaly/DSS/blob/aa8750dbac35f25b82b3ec44cbe5011fc02740a0/Documentation/04-Engineering/PhaseThreeCombatAcceptance.md).

User instruction now prohibits game/native runs while continuing EP-0008 then EP-0009 autonomously. Remaining native/human evidence is deferred, not waived or labelled PASS; downstream review/documentation and EP-0009 may proceed after this ticket's separate publication. Source changes preserve all gameplay authority and public clock behavior.
