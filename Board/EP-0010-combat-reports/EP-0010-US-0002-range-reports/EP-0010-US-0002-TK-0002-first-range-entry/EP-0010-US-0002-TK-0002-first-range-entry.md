---
epic: EP-0010-combat-reports
story: EP-0010-US-0002-range-reports
ticket: EP-0010-US-0002-TK-0002-first-range-entry
title: "E01 по физическому состоянию мира"
stage: implemented
status: implemented
layer: engine
depends_on: ["EP-0010-US-0002-TK-0001-report-outbox"]
files_touched: 4
serves: ["US02-AC2"]
created: 2026-10-16
revision: 1
validation_status: automated-passed
---

# E01 по физическому состоянию мира

## Why

Сделать дальность самостоятельным авторитетным фактом. [История](../EP-0010-US-0002-range-reports.md), [ТЗ](../../../../Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R08, R16, R18, R20, R21.

## Decisions

Пользователь поручил сохранить ТЗ и создать эпик с историями/тикетами (2026-10-16). Execution, art и отдельные commit/push поручены EpicExecutionPrompt2026-10-16. Самостоятельный запуск игры запрещён последующей явной инструкцией пользователя; native/human/FPS OPEN. Продуктовые решения — §§1–10 ТЗ; не выдавать A01–A10 за дополнительные ответы пользователя.

## Assumptions

Технические A01–A10 ТЗ. Code context ниже фиксирует исходный planning baseline; актуальные API и результаты приведены в Execution evidence и engineering CombatReports.md. Максимум 5 файлов из таблицы; Board evidence не разрешает скрытые изменения production. Layer `engine`; matching coverage `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Для purely art/evidence runtime source не меняется.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.CombatReports.cs` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |
| `src/DeepSpaceSaga.Engine/SimulationEngine.CombatReportRange.cs` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |
| `tests/DeepSpaceSaga.Engine.Tests/CombatReportRangeTests.cs` | NEW — proposed; файл отсутствует на planning baseline | Создать/дополнить тесты только этого тикета |

Read-only общий [grounding](../../Documentation.md#grounding) содержит текущие точки входа и ограничения. Остальные файлы не разрешены к изменению. Зависимости: EP-0010-US-0002-TK-0001-report-outbox; их новый код/DTO считаются доступными только после завершения.

## Public API after the change

No public API change.

## Implementation steps

1. Реализовать range hook: known Enemy alive, player alive, исправный Scanner, хотя бы один установленный исправный launcher; range из registry, distance<=range в Engine units.
2. Сортировать несколько одновременно подошедших целей стабильным ID; запоминать выданный ID, не зависеть от cooldown/боезапаса/selection; исходный NewGame in-range даёт событие.
3. После восстановления сканера выдавать только ещё не доложенный текущий контакт; временно недоступный сканер не помечает цель как уже доложенную. Захватить доступное имя и reporter по A04/A05.

## Out of scope

Изменение баланса/конфигурации оружия, формул попадания, RNG, часов, поведения AI, discovery rules, посторонних журналов и незакоммиченных изменений. Никакой публикации или автоматического выполнения следующего тикета без execution-поручения. Root save version не менять в обход A07.

## Invariants

- Engine/renderer separation: `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs:10`; immutable snapshot: `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:11`. Через boundary нет Skia/GL.
- Физическое время/модульный бой — EngineRequirements §Phase Four Module Combat и §§7–8 ТЗ; UI не меняет исходы/часы/sequence журнала.
- Captured identity, privacy, once-per-target и ровно одна открытая карточка проверяются в соответствующих layers; этот тикет не ослабляет смежные гарантии.

## Tests

- `ThreeMukhaHaveIndependentRangeReports` (планируемый сценарий проверки).
- `BoundaryUsesConfiguredRange` (планируемый сценарий проверки).
- `ReloadAndSelectionDoNotAffectRange` (планируемый сценарий проверки).
- `UnknownDestroyedAndDisabledAreExcluded` (планируемый сценарий проверки).
- `ReentryDoesNotRepeat` (планируемый сценарий проверки).
- `NewGameInRangeEmitsOnce` (планируемый сценарий проверки).

Команды ниже — исходный план воспроизведения из корня репозитория; реальные результаты — в Execution evidence и ImplementationStatus:

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~CombatReportRangeTests"
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/SimulationEngine.CombatReports.cs src/DeepSpaceSaga.Engine/SimulationEngine.CombatReportRange.cs tests/DeepSpaceSaga.Engine.Tests/CombatReportRangeTests.cs
git diff --check
```

## Definition of Done

- Шаги выполнены только в 3 разрешённых файлах, выбранный layer соблюдён.
- Критерии US02-AC2 подтверждены именованными проверками выше и ссылками на evidence.
- Focused проверки и соответствующий build/format проходят либо исходное падение записано отдельно; prose/art gates проверены подходящим способом.
- Сохранены public API/invariants/out-of-scope; нет скрытой работы или нерешённых блокирующих допущений.
- Native/human/FPS не помечены PASS по headless; статус NOT RUN/OPEN сохраняется до реального прогона.
- Обновлён status/evidence тикета, выполнен diff check; не заявлять реализацию на основании этого planning-документа.

## Self-containment check

Вход: зависимости по ID, ТЗ, scope и Code context. Выход: Сделать дальность самостоятельным авторитетным фактом. Проверки конкретизированы. Если текущий diff требует шестого файла или второго production layer, сначала переразбить/обновить тикет, не расширять allowlist молча.

## Scope adjustment before tests - 2026-10-16

Add tests/DeepSpaceSaga.Engine.Tests/CombatReportOutboxTests.cs to allowlist: its infrastructure fixture must exclude discoverable enemies once E01 is enabled, keeping queue assertions independent from range generation. Four files total; same Engine layer.

## Execution evidence - 2026-10-16

Seven range cases plus four outbox cases PASS. Fourteen existing combat journal, hostile path, module combat and player defeat regressions PASS. Engine compilation PASS as part of focused tests; scoped whitespace corrected. Full Engine run was cancelled after no completion output; not counted as PASS. Range uses catalogue TorpedoRangeKm, operational modules and physical paths; per-target IDs persist for session and reset on New Game. Reporter capture will be centralized by US0003.
