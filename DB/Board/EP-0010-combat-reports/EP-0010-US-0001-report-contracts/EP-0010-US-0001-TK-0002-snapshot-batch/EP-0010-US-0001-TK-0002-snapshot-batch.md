---
epic: EP-0010-combat-reports
story: EP-0010-US-0001-report-contracts
ticket: EP-0010-US-0001-TK-0002-snapshot-batch
title: "Пакет докладов в snapshot"
stage: implemented
status: implemented
layer: contracts
depends_on: ["EP-0010-US-0001-TK-0001-immutable-report"]
files_touched: 2
serves: ["US01-AC2"]
created: 2026-10-16
revision: 1
validation_status: passed
---

# Пакет докладов в snapshot

## Why

Передавать доклады через существующую async границу. [История](../EP-0010-US-0001-report-contracts.md), [ТЗ](https://github.com/SuhilovVitaly/DSS/blob/aa8750dbac35f25b82b3ec44cbe5011fc02740a0/Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R11, R12, R16, R17.

## Decisions

Пользователь поручил сохранить ТЗ и создать эпик с историями/тикетами (2026-10-16). Execution, art и отдельные commit/push поручены EpicExecutionPrompt2026-10-16. Самостоятельный запуск игры запрещён последующей явной инструкцией пользователя; native/human/FPS OPEN. Продуктовые решения — §§1–10 ТЗ; не выдавать A01–A10 за дополнительные ответы пользователя.

## Assumptions

Технические A01–A10 ТЗ. Code context ниже фиксирует исходный planning baseline; актуальные API и результаты приведены в Execution evidence и engineering CombatReports.md. Максимум 5 файлов из таблицы; Board evidence не разрешает скрытые изменения production. Layer `contracts`; matching coverage `tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj`. Для purely art/evidence runtime source не меняется.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs` | src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:11 — `public sealed record AuthoritativeSnapshot` | Изменение только для шагов этого тикета; соседний scope запрещён |
| `tests/DeepSpaceSaga.Contracts.Tests/CombatReportBatchTests.cs` | NEW — proposed; файл отсутствует на planning baseline | Создать/дополнить тесты только этого тикета |

Read-only общий [grounding](../../Documentation.md#grounding) содержит текущие точки входа и ограничения. Остальные файлы не разрешены к изменению. Зависимости: EP-0010-US-0001-TK-0001-immutable-report; их новый код/DTO считаются доступными только после завершения.

## Public API after the change

AuthoritativeSnapshot(..., CombatReportBatchSnapshot? CombatReports = null).

## Implementation steps

1. Добавить optional CombatReportBatchSnapshot? CombatReports = null в конец AuthoritativeSnapshot, не меняя существующих параметров.
2. Закрепить семантику отсутствующего пакета, baseline bootstrap и sequence в XML contract; Render не делает synchronous calls.
3. Проверить JSON старого snapshot, empty и непустой пакет, дробное физическое время и отдельность SnapshotSequence.

## Out of scope

Изменение баланса/конфигурации оружия, формул попадания, RNG, часов, поведения AI, discovery rules, посторонних журналов и незакоммиченных изменений. Никакой публикации или автоматического выполнения следующего тикета без execution-поручения. Root save version не менять в обход A07.

## Invariants

- Engine/renderer separation: `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs:10`; immutable snapshot: `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:11`. Через boundary нет Skia/GL.
- Физическое время/модульный бой — EngineRequirements §Phase Four Module Combat и §§7–8 ТЗ; UI не меняет исходы/часы/sequence журнала.
- Captured identity, privacy, once-per-target и ровно одна открытая карточка проверяются в соответствующих layers; этот тикет не ослабляет смежные гарантии.

## Tests

- `LegacySnapshotHasNoReports` (планируемый сценарий проверки).
- `BatchBaselineAndLatestRoundTrip` (планируемый сценарий проверки).
- `ReportSequenceIsIndependentFromSnapshotSequence` (планируемый сценарий проверки).

Команды ниже — исходный план воспроизведения из корня репозитория; реальные результаты — в Execution evidence и ImplementationStatus:

```powershell
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore --filter "FullyQualifiedName~CombatReportBatchTests"
dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --no-restore
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs tests/DeepSpaceSaga.Contracts.Tests/CombatReportBatchTests.cs
git diff --check
```

## Definition of Done

- Шаги выполнены только в 2 разрешённых файлах, выбранный layer соблюдён.
- Критерии US01-AC2 подтверждены именованными проверками выше и ссылками на evidence.
- Focused проверки и соответствующий build/format проходят либо исходное падение записано отдельно; prose/art gates проверены подходящим способом.
- Сохранены public API/invariants/out-of-scope; нет скрытой работы или нерешённых блокирующих допущений.
- Native/human/FPS не помечены PASS по headless; статус NOT RUN/OPEN сохраняется до реального прогона.
- Обновлён status/evidence тикета, выполнен diff check; не заявлять реализацию на основании этого planning-документа.

## Self-containment check

Вход: зависимости по ID, ТЗ, scope и Code context. Выход: Передавать доклады через существующую async границу. Проверки конкретизированы. Если текущий diff требует шестого файла или второго production layer, сначала переразбить/обновить тикет, не расширять allowlist молча.

## Execution evidence — 2026-10-16

Добавлен последний optional параметр CombatReports; прежние параметры AuthoritativeSnapshot сохранены. XML contract различает legacy null, пустой пакет, фиксированный bootstrap baseline и новую sequence. Все3 именованных теста PASS: legacy JSON, default/empty/nonempty batch round-trip, дробное физическое время и отдельные snapshot/report/journal identities.

`dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore --nologo --verbosity minimal -p:UseSharedCompilation=false --filter FullyQualifiedName~CombatReportBatchTests` —3/3; тот же project без filter —210/210. `dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --no-restore --nologo -p:UseSharedCompilation=false` —0 warnings/errors. `dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs tests/DeepSpaceSaga.Contracts.Tests/CombatReportBatchTests.cs --verbosity quiet` и `git diff --check` —PASS. Dependency TK0001 опубликован a49debeb6a6df8b01b150487b6d002420a53cfcd, remote SHA проверен перед реализацией.
