---
epic: EP-0010-combat-reports
story: EP-0010-US-0008-report-illustrations
ticket: EP-0010-US-0008-TK-0004-catalog-and-cache
title: "Каталог, доставка ресурсов и ограниченный кеш"
stage: draft
status: implemented
layer: client
depends_on: ["EP-0010-US-0008-TK-0003-damage-art"]
files_touched: 4
serves: ["US08-AC2"]
created: 2026-10-16
revision: 1
validation_status: automated-passed
---

# Каталог, доставка ресурсов и ограниченный кеш

## Why

Подключить готовую графику с безопасными заменами и без I/O в Draw. [История](../EP-0010-US-0008-report-illustrations.md), [ТЗ](../../../../Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R12, R14, R15, R22.

## Decisions

Пользователь поручил сохранить ТЗ и создать эпик с историями/тикетами (2026-10-16). Выполнение этого тикета, commit/push, генерация art или запуск игры пока не поручены. Продуктовые решения — §§1–10 ТЗ; не выдавать A01–A10 за дополнительные ответы пользователя.

## Assumptions

Технические A01–A10 ТЗ. Proposed API ниже — ожидаемый результат, не утверждение о текущем коде. Максимум 5 файлов из таблицы; Board evidence не разрешает скрытые изменения production. Layer `client`; matching coverage `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Для purely art/evidence runtime source не меняется.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatReportAssets.cs` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |
| `src/DeepSpaceSaga.Client/Data/UI/combat-report-visuals.json` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |
| `src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj` | src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj:264 — `Data\**\*.json` | Изменение только для шагов этого тикета; соседний scope запрещён |
| `tests/DeepSpaceSaga.Client.Tests/CombatReportAssetsTests.cs` | NEW — proposed; файл отсутствует на planning baseline | Создать/дополнить тесты только этого тикета |

Read-only общий [grounding](../../Documentation.md#grounding) содержит текущие точки входа и ограничения. Остальные файлы не разрешены к изменению. Зависимости: EP-0010-US-0008-TK-0003-damage-art; их новый код/DTO считаются доступными только после завершения.

## Public API after the change

Internal CombatReportAssets.Prepare/Resolve/Dispose; no Engine resource loading.

## Implementation steps

1. Каталог содержит event/variant->path/semantic accent; ресурсные пути относительные Client output. Добавить copy rule только для новой image папки; Data JSON уже копируется рекурсивно.
2. Preload/decode иллюстрации и известные нужные portrait вне Draw; max8 illustration + ограниченный portrait cache (например32 LRU), disposal при lifecycle cleanup.
3. Отсутствующие/повреждённые файлы дают рисуемый тематический значок/знак должности; тесты проверяют output файлы, 8 mappings и отсутствие decode/I/O во время повторного render consumption.

## Out of scope

Изменение баланса/конфигурации оружия, формул попадания, RNG, часов, поведения AI, discovery rules, посторонних журналов и незакоммиченных изменений. Никакой публикации или автоматического выполнения следующего тикета без execution-поручения. Root save version не менять в обход A07.

## Invariants

- Engine/renderer separation: `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs:10`; immutable snapshot: `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:11`. Через boundary нет Skia/GL.
- Физическое время/модульный бой — EngineRequirements §Phase Four Module Combat и §§7–8 ТЗ; UI не меняет исходы/часы/sequence журнала.
- Captured identity, privacy, once-per-target и ровно одна открытая карточка проверяются в соответствующих layers; этот тикет не ослабляет смежные гарантии.

## Tests

- `AllEightVariantsExistInBuildOutput` (планируемый сценарий проверки).
- `MissingAndCorruptAssetUsesFallback` (планируемый сценарий проверки).
- `PortraitCacheIsBoundedAndDisposed` (планируемый сценарий проверки).
- `RepeatedDrawDoesNotReadOrDecodeFiles` (планируемый сценарий проверки).

Команды выполнять из корня репозитория **при реализации**, сейчас NOT RUN:

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~CombatReportAssetsTests"
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatReportAssets.cs src/DeepSpaceSaga.Client/Data/UI/combat-report-visuals.json src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj tests/DeepSpaceSaga.Client.Tests/CombatReportAssetsTests.cs
git diff --check
```

## Definition of Done

- Шаги выполнены только в 4 разрешённых файлах, выбранный layer соблюдён.
- Критерии US08-AC2 подтверждены именованными проверками выше и ссылками на evidence.
- Focused проверки и соответствующий build/format проходят либо исходное падение записано отдельно; prose/art gates проверены подходящим способом.
- Сохранены public API/invariants/out-of-scope; нет скрытой работы или нерешённых блокирующих допущений.
- Native/human/FPS не помечены PASS по headless; статус NOT RUN/OPEN сохраняется до реального прогона.
- Обновлён status/evidence тикета, выполнен diff check; не заявлять реализацию на основании этого planning-документа.

## Self-containment check

Вход: зависимости по ID, ТЗ, scope и Code context. Выход: Подключить готовую графику с безопасными заменами и без I/O в Draw. Проверки конкретизированы. Если текущий diff требует шестого файла или второго production layer, сначала переразбить/обновить тикет, не расширять allowlist молча.

## Execution evidence - 2026-10-16

Eight mapping catalogue with semantic accents, explicit output/publish copy. UI-thread Prepare loads eight illustrations once and bounded32 portrait LRU (including negative lookups); large portraits reduced to128px. Resolve is memory-only. Dispose releases native bitmaps. Four asset cases plus24 prior Client report cases PASS (28); includes build-output1024x576 images, corrupt/missing fallback, eviction/disposal and repeated consumption with no additional decode. Client compile/scoped format/diff PASS.
