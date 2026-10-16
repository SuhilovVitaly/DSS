---
epic: EP-0010-combat-reports
story: EP-0010-US-0007-report-history-and-text
ticket: EP-0010-US-0007-TK-0002-localized-report-text
title: "Реплики и заголовки Russian/English"
stage: implemented
status: implemented
layer: client
depends_on: ["EP-0010-US-0007-TK-0001-history-state"]
files_touched: 4
serves: ["US07-AC2"]
created: 2026-10-16
revision: 1
validation_status: automated-passed
---

# Реплики и заголовки Russian/English

## Why

Сформировать короткие понятные тексты без потери privacy и captured identity. [История](../EP-0010-US-0007-report-history-and-text.md), [ТЗ](../../../../Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R03, R04, R05, R07, R11, R12, R13, R19, R20.

## Decisions

Пользователь поручил сохранить ТЗ и создать эпик с историями/тикетами (2026-10-16). Execution, art и отдельные commit/push поручены EpicExecutionPrompt2026-10-16. Самостоятельный запуск игры запрещён последующей явной инструкцией пользователя; native/human/FPS OPEN. Продуктовые решения — §§1–10 ТЗ; не выдавать A01–A10 за дополнительные ответы пользователя.

## Assumptions

Технические A01–A10 ТЗ. Code context ниже фиксирует исходный planning baseline; актуальные API и результаты приведены в Execution evidence и engineering CombatReports.md. Максимум 5 файлов из таблицы; Board evidence не разрешает скрытые изменения production. Layer `client`; matching coverage `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Для purely art/evidence runtime source не меняется.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatReportFormatter.cs` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | src/DeepSpaceSaga.Client/Data/Locale/Russian.json:1 — `{` | Изменение только для шагов этого тикета; соседний scope запрещён |
| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | src/DeepSpaceSaga.Client/Data/Locale/English.json:1 — `{` | Изменение только для шагов этого тикета; соседний scope запрещён |
| `tests/DeepSpaceSaga.Client.Tests/CombatReportFormatterTests.cs` | NEW — proposed; файл отсутствует на planning baseline | Создать/дополнить тесты только этого тикета |

Read-only общий [grounding](../../Documentation.md#grounding) содержит текущие точки входа и ограничения. Остальные файлы не разрешены к изменению. Зависимости: EP-0010-US-0007-TK-0001-history-state; их новый код/DTO считаются доступными только после завершения.

## Public API after the change

Internal CombatReportFormatter.Format(BattleReportSnapshot) -> immutable presentation model; proposed names, not existing API.

## Implementation steps

1. Добавить locale keys для panel/empty, семи событий, двух вариантов E06, ролей, fallback names и доступных damage details.
2. Formatter использует BattleReportSnapshot и Localization, не ищет live world; форматирует физическое время и participants. Не печатать null, сырой enum или отсутствующий locale key.
3. Текст цели enemy launch появляется только при явно переданном разрешённом Target; оба языка содержат одинаковые placeholder sets.

## Out of scope

Изменение баланса/конфигурации оружия, формул попадания, RNG, часов, поведения AI, discovery rules, посторонних журналов и незакоммиченных изменений. Никакой публикации или автоматического выполнения следующего тикета без execution-поручения. Root save version не менять в обход A07.

## Invariants

- Engine/renderer separation: `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs:10`; immutable snapshot: `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:11`. Через boundary нет Skia/GL.
- Физическое время/модульный бой — EngineRequirements §Phase Four Module Combat и §§7–8 ТЗ; UI не меняет исходы/часы/sequence журнала.
- Captured identity, privacy, once-per-target и ровно одна открытая карточка проверяются в соответствующих layers; этот тикет не ослабляет смежные гарантии.

## Tests

- `EveryKindHasRussianAndEnglishText` (планируемый сценарий проверки).
- `DestroyedVariantUsesSingleFinalText` (планируемый сценарий проверки).
- `MissingReporterUsesRole` (планируемый сценарий проверки).
- `HiddenDataHasNoTextPlaceholderLeak` (планируемый сценарий проверки).
- `DeletedParticipantUsesCapturedName` (планируемый сценарий проверки).
- `LocalesHaveEqualKeysAndPlaceholders` (планируемый сценарий проверки).

Команды ниже — исходный план воспроизведения из корня репозитория; реальные результаты — в Execution evidence и ImplementationStatus:

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~CombatReportFormatterTests"
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatReportFormatter.cs src/DeepSpaceSaga.Client/Data/Locale/Russian.json src/DeepSpaceSaga.Client/Data/Locale/English.json tests/DeepSpaceSaga.Client.Tests/CombatReportFormatterTests.cs
git diff --check
```

## Definition of Done

- Шаги выполнены только в 4 разрешённых файлах, выбранный layer соблюдён.
- Критерии US07-AC2 подтверждены именованными проверками выше и ссылками на evidence.
- Focused проверки и соответствующий build/format проходят либо исходное падение записано отдельно; prose/art gates проверены подходящим способом.
- Сохранены public API/invariants/out-of-scope; нет скрытой работы или нерешённых блокирующих допущений.
- Native/human/FPS не помечены PASS по headless; статус NOT RUN/OPEN сохраняется до реального прогона.
- Обновлён status/evidence тикета, выполнен diff check; не заявлять реализацию на основании этого planning-документа.

## Self-containment check

Вход: зависимости по ID, ТЗ, scope и Code context. Выход: Сформировать короткие понятные тексты без потери privacy и captured identity. Проверки конкретизированы. Если текущий diff требует шестого файла или второго production layer, сначала переразбить/обновить тикет, не расширять allowlist молча.

## Execution evidence - 2026-10-16

Formatter freezes presentation from DTO only: all eight visuals, titles/body, roles, unknown contacts, physical time and optional damage. Enemy target sentence requires explicit permitted TargetObjectId. RU/EN key/placeholder sets equal. Ten formatter cases plus 14 report/client cases PASS (24). Client compile/scoped format/diff PASS. Assertions inspect actual UI strings, not record diagnostic ToString.
