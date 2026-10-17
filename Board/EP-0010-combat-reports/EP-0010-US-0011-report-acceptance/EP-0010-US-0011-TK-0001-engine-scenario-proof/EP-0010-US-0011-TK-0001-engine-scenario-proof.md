---
epic: EP-0010-combat-reports
story: EP-0010-US-0011-report-acceptance
ticket: EP-0010-US-0011-TK-0001-engine-scenario-proof
title: "Сценарные доказательства всех семи типов"
stage: acceptance-open
status: implemented
layer: engine
depends_on: ["EP-0010-US-0010-TK-0003-session-reset-and-defeat"]
files_touched: 2
serves: ["US11-AC1"]
created: 2026-10-16
revision: 1
validation_status: automated-passed
---

# Сценарные доказательства всех семи типов

## Why

Проверить реальную цепочку Engine вместо синтетического рендера DTO. [История](../EP-0010-US-0011-report-acceptance.md), [ТЗ](../../../../Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R08, R09, R10, R11, R17, R19, R20, R21, R22, R23, R24.

## Decisions

Пользователь поручил сохранить ТЗ и создать эпик с историями/тикетами (2026-10-16). Execution, art и отдельные commit/push поручены EpicExecutionPrompt2026-10-16. Самостоятельный запуск игры запрещён последующей явной инструкцией пользователя; native/human/FPS OPEN. Продуктовые решения — §§1–10 ТЗ; не выдавать A01–A10 за дополнительные ответы пользователя.

## Assumptions

Технические A01–A10 ТЗ. Code context ниже фиксирует исходный planning baseline; актуальные API и результаты приведены в Execution evidence и engineering CombatReports.md. Максимум 5 файлов из таблицы; Board evidence не разрешает скрытые изменения production. Layer `engine`; matching coverage `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Для purely art/evidence runtime source не меняется.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Engine.Tests/CombatReportsEndToEndTests.cs` | NEW — proposed; файл отсутствует на planning baseline | Создать/дополнить тесты только этого тикета |
| `tests/DeepSpaceSaga.Engine.Tests/CombatReportsScenarioFixtures.cs` | NEW — proposed; файл отсутствует на planning baseline | Создать/дополнить тесты только этого тикета |

Read-only общий [grounding](../../Documentation.md#grounding) содержит текущие точки входа и ограничения. Остальные файлы не разрешены к изменению. Зависимости: EP-0010-US-0010-TK-0003-session-reset-and-defeat; их новый код/DTO считаются доступными только после завершения.

## Public API after the change

No public API change.

## Implementation steps

1. Загрузить production CombatMukha со стабильным seed; для редких исходов использовать отдельные fixtures, не менять production сценарий/каталоги и баланс.
2. Проверить три E01, actual launches, intercept, hit/destroyed, module damage, simultaneous different projectiles, masks/Scanner disable, SaveLoad before/after события.
3. Сравнить motion/RNG/outcomes с baseline при включённом формировании докладов; terminal момент и запрет Defeated save остаются. Повтор одного seed+commands даёт идентичные report sequence/time.

## Out of scope

Изменение баланса/конфигурации оружия, формул попадания, RNG, часов, поведения AI, discovery rules, посторонних журналов и незакоммиченных изменений. Никакой публикации или автоматического выполнения следующего тикета без execution-поручения. Root save version не менять в обход A07.

## Invariants

- Engine/renderer separation: `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs:10`; immutable snapshot: `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:11`. Через boundary нет Skia/GL.
- Физическое время/модульный бой — EngineRequirements §Phase Four Module Combat и §§7–8 ТЗ; UI не меняет исходы/часы/sequence журнала.
- Captured identity, privacy, once-per-target и ровно одна открытая карточка проверяются в соответствующих layers; этот тикет не ослабляет смежные гарантии.

## Tests

- `CombatMukhaUsesProductionScenario` (планируемый сценарий проверки).
- `EveryReportKindHasDeterministicProof` (планируемый сценарий проверки).
- `SameSeedReportsAreIdentical` (планируемый сценарий проверки).
- `ReportProjectionDoesNotChangeCombatOutcome` (планируемый сценарий проверки).
- `SaveLoadAndTerminalReportMatrix` (планируемый сценарий проверки).

Команды ниже — исходный план воспроизведения из корня репозитория; реальные результаты — в Execution evidence и ImplementationStatus:

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~CombatReportsEndToEndTests"
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include tests/DeepSpaceSaga.Engine.Tests/CombatReportsEndToEndTests.cs tests/DeepSpaceSaga.Engine.Tests/CombatReportsScenarioFixtures.cs
git diff --check
```

## Definition of Done

- Шаги выполнены только в 2 разрешённых файлах, выбранный layer соблюдён.
- Критерии US11-AC1 подтверждены именованными проверками выше и ссылками на evidence.
- Focused проверки и соответствующий build/format проходят либо исходное падение записано отдельно; prose/art gates проверены подходящим способом.
- Сохранены public API/invariants/out-of-scope; нет скрытой работы или нерешённых блокирующих допущений.
- Native/human/FPS не помечены PASS по headless; статус NOT RUN/OPEN сохраняется до реального прогона.
- Обновлён status/evidence тикета, выполнен diff check; не заявлять реализацию на основании этого planning-документа.

## Self-containment check

Вход: зависимости по ID, ТЗ, scope и Code context. Выход: Проверить реальную цепочку Engine вместо синтетического рендера DTO. Проверки конкретизированы. Если текущий diff требует шестого файла или второго production layer, сначала переразбить/обновить тикет, не расширять allowlist молча.

## Execution evidence - 2026-10-16

Production CombatMukha and production registry seed1 executed unchanged through commands to terminal outcome; all three independent E01 and actual launches verified. Separate calibrated fixtures prove every kind and both E06 variants. Same seed+commands yields identical report sequence/time. Disabling scanner reporting changes report count but leaves complete combat journal, outcome, physical time and object motion identical; emission helper regression separately compares all non-report save/RNG state. Save/load and final damage/defeat matrix PASS. Five E2E cases plus52 report cases PASS (57), compile/scoped format/diff PASS. Rare outcome fixtures do not alter shipped balance.
