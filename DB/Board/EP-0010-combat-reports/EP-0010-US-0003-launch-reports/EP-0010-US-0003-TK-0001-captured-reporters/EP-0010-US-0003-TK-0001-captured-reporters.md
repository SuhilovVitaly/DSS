---
epic: EP-0010-combat-reports
story: EP-0010-US-0003-launch-reports
ticket: EP-0010-US-0003-TK-0001-captured-reporters
title: "Докладчики и безопасные имена участников"
stage: implemented
status: implemented
layer: engine
depends_on: ["EP-0010-US-0002-TK-0002-first-range-entry"]
files_touched: 4
serves: ["US03-AC1"]
created: 2026-10-16
revision: 1
validation_status: automated-passed
---

# Докладчики и безопасные имена участников

## Why

Переиспользовать фактические назначения и не расширять видимость. [История](../EP-0010-US-0003-launch-reports.md), [ТЗ](https://github.com/SuhilovVitaly/DSS/blob/aa8750dbac35f25b82b3ec44cbe5011fc02740a0/Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R09, R11, R12, R16, R18.

## Decisions

Пользователь поручил сохранить ТЗ и создать эпик с историями/тикетами (2026-10-16). Execution, art и отдельные commit/push поручены EpicExecutionPrompt2026-10-16. Самостоятельный запуск игры запрещён последующей явной инструкцией пользователя; native/human/FPS OPEN. Продуктовые решения — §§1–10 ТЗ; не выдавать A01–A10 за дополнительные ответы пользователя.

## Assumptions

Технические A01–A10 ТЗ. Code context ниже фиксирует исходный planning baseline; актуальные API и результаты приведены в Execution evidence и engineering CombatReports.md. Максимум 5 файлов из таблицы; Board evidence не разрешает скрытые изменения production. Layer `engine`; matching coverage `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Для purely art/evidence runtime source не меняется.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.CombatReportIdentity.cs` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |
| `src/DeepSpaceSaga.Engine/SimulationEngine.CombatReportRange.cs` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |
| `src/DeepSpaceSaga.Engine/SimulationEngine.CombatReports.cs` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |
| `tests/DeepSpaceSaga.Engine.Tests/CombatReportIdentityTests.cs` | NEW — proposed; файл отсутствует на planning baseline | Создать/дополнить тесты только этого тикета |

Read-only общий [grounding](../../Documentation.md#grounding) содержит текущие точки входа и ограничения. Остальные файлы не разрешены к изменению. Зависимости: EP-0010-US-0002-TK-0002-first-range-entry; их новый код/DTO считаются доступными только после завершения.

## Public API after the change

Internal CaptureReportIdentity(owner, moduleId, role) and CaptureKnownParticipant(...); no crew schema changes.

## Implementation steps

1. Выделить захват report identity из range hook в общий helper; оператор weapons — по OperatorCrewId и Crew, капитан — из owner runtime.
2. У текущего CrewMemberRuntime нет portrait field: передавать null и роль, не менять crew schema. CaptainPortraitImage переиспользовать при наличии. Scanner без назначенного crew — служба сканирования.
3. Captured names/identity больше не читают live objects при форматировании; при нескольких Scanner использовать стабильный moduleId. Не раскрывать неизвестную цель.

## Out of scope

Изменение баланса/конфигурации оружия, формул попадания, RNG, часов, поведения AI, discovery rules, посторонних журналов и незакоммиченных изменений. Никакой публикации или автоматического выполнения следующего тикета без execution-поручения. Root save version не менять в обход A07.

## Invariants

- Engine/renderer separation: `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs:10`; immutable snapshot: `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:11`. Через boundary нет Skia/GL.
- Физическое время/модульный бой — EngineRequirements §Phase Four Module Combat и §§7–8 ТЗ; UI не меняет исходы/часы/sequence журнала.
- Captured identity, privacy, once-per-target и ровно одна открытая карточка проверяются в соответствующих layers; этот тикет не ослабляет смежные гарантии.

## Tests

- `AssignedOperatorIsCaptured` (планируемый сценарий проверки).
- `MissingScannerIdentityUsesService` (планируемый сценарий проверки).
- `MissingCrewPortraitStaysNull` (планируемый сценарий проверки).
- `CaptainPortraitIsCaptured` (планируемый сценарий проверки).
- `HiddenNamesAreNotProjected` (планируемый сценарий проверки).

Команды ниже — исходный план воспроизведения из корня репозитория; реальные результаты — в Execution evidence и ImplementationStatus:

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~CombatReportIdentityTests"
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/SimulationEngine.CombatReportIdentity.cs src/DeepSpaceSaga.Engine/SimulationEngine.CombatReportRange.cs src/DeepSpaceSaga.Engine/SimulationEngine.CombatReports.cs tests/DeepSpaceSaga.Engine.Tests/CombatReportIdentityTests.cs
git diff --check
```

## Definition of Done

- Шаги выполнены только в 4 разрешённых файлах, выбранный layer соблюдён.
- Критерии US03-AC1 подтверждены именованными проверками выше и ссылками на evidence.
- Focused проверки и соответствующий build/format проходят либо исходное падение записано отдельно; prose/art gates проверены подходящим способом.
- Сохранены public API/invariants/out-of-scope; нет скрытой работы или нерешённых блокирующих допущений.
- Native/human/FPS не помечены PASS по headless; статус NOT RUN/OPEN сохраняется до реального прогона.
- Обновлён status/evidence тикета, выполнен diff check; не заявлять реализацию на основании этого planning-документа.

## Self-containment check

Вход: зависимости по ID, ТЗ, scope и Code context. Выход: Переиспользовать фактические назначения и не расширять видимость. Проверки конкретизированы. Если текущий diff требует шестого файла или второго production layer, сначала переразбить/обновить тикет, не расширять allowlist молча.

## Execution evidence - 2026-10-16

Central capture helper freezes module crew identity, captain portrait and role-only fallback. Known-participant projection masks absent/hidden objects; no schema changes. Five identity tests and 11 prior report cases PASS (16 total); Engine compilation PASS. Scoped format and diff checked before commit. Native/human not applicable to helper.
