---
epic: EP-0010-combat-reports
story: EP-0010-US-0011-report-acceptance
ticket: EP-0010-US-0011-TK-0002-client-integration-proof
title: "Интеграционная матрица доставки и UI"
stage: acceptance-open
status: implemented
layer: client
depends_on: ["EP-0010-US-0011-TK-0001-engine-scenario-proof"]
files_touched: 2
serves: ["US11-AC2"]
created: 2026-10-16
revision: 1
validation_status: automated-passed
---

# Интеграционная матрица доставки и UI

## Why

Проверить всю Client цепочку и регрессии соседних панелей. [История](../EP-0010-US-0011-report-acceptance.md), [ТЗ](https://github.com/SuhilovVitaly/DSS/blob/aa8750dbac35f25b82b3ec44cbe5011fc02740a0/Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R08, R09, R10, R11, R17, R19, R20, R21, R22, R23, R24.

## Decisions

Пользователь поручил сохранить ТЗ и создать эпик с историями/тикетами (2026-10-16). Execution, art и отдельные commit/push поручены EpicExecutionPrompt2026-10-16. Самостоятельный запуск игры запрещён последующей явной инструкцией пользователя; native/human/FPS OPEN. Продуктовые решения — §§1–10 ТЗ; не выдавать A01–A10 за дополнительные ответы пользователя.

## Assumptions

Технические A01–A10 ТЗ. Code context ниже фиксирует исходный planning baseline; актуальные API и результаты приведены в Execution evidence и engineering CombatReports.md. Максимум 5 файлов из таблицы; Board evidence не разрешает скрытые изменения production. Layer `client`; matching coverage `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Для purely art/evidence runtime source не меняется.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Client.Tests/CombatReportsAcceptanceTests.cs` | NEW — proposed; файл отсутствует на planning baseline | Создать/дополнить тесты только этого тикета |
| `tests/DeepSpaceSaga.Client.Tests/CombatReportsAcceptanceFixtures.cs` | NEW — proposed; файл отсутствует на planning baseline | Создать/дополнить тесты только этого тикета |

Read-only общий [grounding](../../Documentation.md#grounding) содержит текущие точки входа и ограничения. Остальные файлы не разрешены к изменению. Зависимости: EP-0010-US-0011-TK-0001-engine-scenario-proof; их новый код/DTO считаются доступными только после завершения.

## Public API after the change

No public API change.

## Implementation steps

1. Создать fixture connection/handle/buffer/screen с real report batches: медленный receiver/UI, many reports до frame, re-delivery и разные bootstrap.
2. Проверить лимиты200 истории/4096 pending, long wrapped body, locale keys, портрет/asset fallback, viewport/scale/input matrix, старые journal/object panels.
3. Запустить focused report suites и релевантные существующие Client/Engine suites; build и scoped format/diff evidence. Существующие посторонние failures описать отдельно, не исправлять вне scope.

## Out of scope

Изменение баланса/конфигурации оружия, формул попадания, RNG, часов, поведения AI, discovery rules, посторонних журналов и незакоммиченных изменений. Никакой публикации или автоматического выполнения следующего тикета без execution-поручения. Root save version не менять в обход A07.

## Invariants

- Engine/renderer separation: `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs:10`; immutable snapshot: `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:11`. Через boundary нет Skia/GL.
- Физическое время/модульный бой — EngineRequirements §Phase Four Module Combat и §§7–8 ТЗ; UI не меняет исходы/часы/sequence журнала.
- Captured identity, privacy, once-per-target и ровно одна открытая карточка проверяются в соответствующих layers; этот тикет не ослабляет смежные гарантии.

## Tests

- `TransportToPanelKeepsEveryPendingReport` (планируемый сценарий проверки).
- `HistoryAndDeliveryLimitsAreDifferent` (планируемый сценарий проверки).
- `AllSupportedViewportsAndLocales` (планируемый сценарий проверки).
- `NoMapInputLeakOrFocusSteal` (планируемый сценарий проверки).
- `LegacyJournalAndObjectInfoRemainUsable` (планируемый сценарий проверки).

Команды ниже — исходный план воспроизведения из корня репозитория; реальные результаты — в Execution evidence и ImplementationStatus:

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~CombatReportsAcceptanceTests"
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include tests/DeepSpaceSaga.Client.Tests/CombatReportsAcceptanceTests.cs tests/DeepSpaceSaga.Client.Tests/CombatReportsAcceptanceFixtures.cs
git diff --check
```

## Definition of Done

- Шаги выполнены только в 2 разрешённых файлах, выбранный layer соблюдён.
- Критерии US11-AC2 подтверждены именованными проверками выше и ссылками на evidence.
- Focused проверки и соответствующий build/format проходят либо исходное падение записано отдельно; prose/art gates проверены подходящим способом.
- Сохранены public API/invariants/out-of-scope; нет скрытой работы или нерешённых блокирующих допущений.
- Native/human/FPS не помечены PASS по headless; статус NOT RUN/OPEN сохраняется до реального прогона.
- Обновлён status/evidence тикета, выполнен diff check; не заявлять реализацию на основании этого planning-документа.

## Self-containment check

Вход: зависимости по ID, ТЗ, scope и Code context. Выход: Проверить всю Client цепочку и регрессии соседних панелей. Проверки конкретизированы. Если текущий diff требует шестого файла или второго production layer, сначала переразбить/обновить тикет, не расширять allowlist молча.

## Execution evidence - 2026-10-16

Real production CombatMukha report snapshots coalesced through mailbox, async connection/handle, buffer and prepared screen; every pending event reaches history once.4096 delivery versus200 UI retention proven.48 viewport/locale/open-collapse/empty-history combinations exercised offscreen; long names wrap, footer/journal separation and focus/camera/speed isolation verified. Ten acceptance test cases plus report/reliability/basic combat UI regressions PASS (82 total). Compile/scoped format/diff PASS. Full Engine regression launched separately; native matrix still OPEN.
