---
epic: EP-0029-unified-carrier-and-torpedoes
story: EP-0029-US-0013-acceptance-and-regression
ticket: EP-0029-US-0013-TK-0001-engine-acceptance
title: Сквозные Engine сценарии и миграции
stage: approved
approval_kind: workflow-only
layer: engine
test_project: DeepSpaceSaga.Engine.Tests
depends_on: [EP-0029-US-0012-TK-0004-lifecycle-locale]
files_touched: 3
serves: [AC-01, AC-02]
created: 2026-11-14T19:42:24Z
revision: 1
implementation_status: implemented
---

# Сквозные Engine сценарии и миграции

## Why

Игрок может пройти цепочку обучение → карта → корабль → перезарядка → второй бой → сохранение/загрузка без потери identity и без выдачи ресурсов. Доказательства отдельно подтверждают расчёты, UI и продуктовый баланс. Этот тикет обеспечивает **Сквозные Engine сценарии и миграции**. Источники: [история](../EP-0029-US-0013-acceptance-and-regression.md), [полный контракт EP-0029](../../Documentation.md).

## Decisions

Пользователь утвердил концепцию с уточнениями: «два модуля жилых каждый на 4 места», «Учебный бой настоящий», light6/heavy12/cargo42. Последнее точное сообщение: «Пока только резервируем два места, захват пилотов сделаем позже. По торпедам согласен: один выстрел — одна торпеда, перезарядка из трюма между боями. Создай подробный эпик со всеми сторисами и тикетами.». Время записи 2026-11-14T19:42:24Z; исходное время отправки не установлено. Planning не разрешает production implementation или публикацию.

## Assumptions

Применять A-01..A-06 эпика в пределах тикета. Новые filenames/APIs проектируются. Стартовое крыло имеет36 торпед; cargo42 отдельно. Другие боевые правила сохраняются кроме явно описанных ammo/mass/stalemate изменений.

## Code context

Исчерпывающая allowlist implementation+tests (максимум5), пути относительно D:/DeepSpaceSaga/DSS. Один production layer: **engine**. Read-only зависимости читать можно; менять другие layers запрещено.

| File | Current state | Allowed change |
|---|---|---|
| [tests/DeepSpaceSaga.Engine.Tests/Carrier/Ep0029Us0013Tk0001Tests.cs](../../../../../tests/DeepSpaceSaga.Engine.Tests/Carrier/Ep0029Us0013Tk0001Tests.cs) | Новый проектируемый файл; до выполнения отсутствует. | Named tests Ep0029Us0013Tk0001Tests ниже с независимым oracle. |
| [tests/DeepSpaceSaga.Engine.Tests/ScenarioEngineTests.cs](../../../../../tests/DeepSpaceSaga.Engine.Tests/ScenarioEngineTests.cs) | Устаревшие ожидания старого 12-cell/6-module starter; pre-existing red `Can_create_engine_from_real_settings_file` (item indices 2/5) зафиксирован в grounding. | Process-only amendment (1→3 files): обновить ожидания реального starter (24 cells/9 modules/6 crew/cargo 3 stacks) и резолвить item stacks по registry typeId вместо жёстких индексов. Production-код не меняется. |
| [tests/DeepSpaceSaga.Engine.Tests/ModuleHealthBootstrapTests.cs](../../../../../tests/DeepSpaceSaga.Engine.Tests/ModuleHealthBootstrapTests.cs) | `Default_modules_have_structure` ожидает SP 150 у всех модулей старого starter. | Process-only amendment (1→3 files): medium bays 250 SP, остальные starter modules 150 SP, все intact. Production-код не меняется. |

## Public API after the change

No production API change; только named integration tests.

Новые поля/сигнатуры проектируемые. Существующие numeric enum identities не меняются; новые значения append. Default/nullable payload не выдаёт имущество.

## Implementation steps

1. Прочитать [контракт эпика](../../Documentation.md) и prerequisites; проверить реализацию их API, не только наличие файлов.
2. Создать fixtures тестового нового carrier через production loader, пройти реальные Start/Move/Fire/Exit/Rearm/Capture/Load. Независимый oracle: начальные78 = magazines+hold+fired+lostWithDestroyedDrones; settlement не смешивает fired и lost. Сценарии с целевыми engine fixtures могут детерминированно обеспечить поражение, но не менять production правила ради теста. Проверить both destroyed draw с player terminal flag.
3. Реализовать named tests на owning слое. Численные ожидания вычислять независимо от проверяемого helper.
4. Проверить часть критериев AC-01, AC-02 по этому тикету; общий критерий может требовать нескольких тикетов карты.
5. Записать actual commands/results/evidence и открытые native/product gates здесь, не выдавать план за тест.

## Dependencies

- [EP-0029-US-0012-TK-0004-lifecycle-locale](../../EP-0029-US-0012-campaign-lifecycle/EP-0029-US-0012-TK-0004-lifecycle-locale/EP-0029-US-0012-TK-0004-lifecycle-locale.md) — завершить до этого тикета.

## Out of scope

Layers кроме engine; файлы вне Code context; пленные, найм, верфь, ремонт, новая баллистика, покупки/производство торпед, Ironman и in-battle saves. Запрещены sandbox fallback и бесплатный refill. Snapshot baselines не обновлять без решения Board. Не менять нормативные документы без отдельного разрешения на их синхронизацию.

## Invariants

- Engine авторитетен: EngineRequirements, раздел «Архитектура и авторитет».
- Fire атомарен; точка расхода при candidate до RNG: `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSalvo.cs:181` (учёта ammo в исходном коде ещё нет).
- Мир блокируется боем: `SimulationEngine.StartTacticalBattle` / `ThrowIfTacticalBattleActive`. World/tactical координаты различны.
- Новый SKU не удаляется legacy очисткой: `src/DeepSpaceSaga.Engine/Scenario/SaveMigration20.cs:85`.
- Identity не выводится из display name; повтор команды не расходует ресурсы. Потеря дрона не убивает дистанционного пилота.

## Tests

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Новый test class: **Ep0029Us0013Tk0001Tests**.

- `Ep0029Us0013Tk0001Tests.CampaignRoundTripConservesSeventyEight` — полный цикл.
- `Ep0029Us0013Tk0001Tests.DestroyedHullCannotBeRecreated` — terminal persists.
- `Ep0029Us0013Tk0001Tests.ZeroAmmoDrawAndDuplicateSettlement` — нет зависания/дублирования.
- `Ep0029Us0013Tk0001Tests.Save18To21PreservesEconomy` — no grants/no fingerprint bypass.

Будущие команды из корня DSS; сейчас **не запускались**. Заменить <run> уникальным timestamp, заранее создать абсолютный results directory. На каждый процесс установить process-tree cap300000ms через execution harness. При превышении разделить filter на named methods.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --filter "FullyQualifiedName~Ep0029Us0013Tk0001Tests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0029/<run>/US-0013-TK-0001" --logger "trx;LogFileName=focused.trx"
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include tests/DeepSpaceSaga.Engine.Tests/Carrier/Ep0029Us0013Tk0001Tests.cs
```

Stdout/stderr build/format перенаправить в абсолютные файлы того же run. Не писать validation artifacts в репозиторий. Для content также parse JSON и resource links в named tests. Full suite не заменяет focused tests.

Если затронут UI: отдельно native RU/EN,1280×720/1920×1080, мышь/клавиатура, меньший проверенный монитор и DSS_VALIDATION_SMALL_MONITOR=1. Headless этот gate не закрывает.

## Definition of Done

- Шаги выполнены в 3 разрешённых файлах (allowlist amendment 1→3, process-only), один production layer.
- Named tests подтверждают часть AC-01, AC-02; полное покрытие story сверено по карте.
- Focused tests и owning build/format выполнены; исходные падения записаны отдельно.
- API/schema, identity, atomic rejection и остатки ресурсов соответствуют эпику.
- Нет скрытого шестого файла, незаписанных assumptions или неразрешённых blockers.
- Здесь записаны реальные evidence paths и implementation/automated/native/product статусы. Отсутствующее human acceptance остаётся OPEN.

## Self-containment check

Заданы layer, allowlist с test file, API, конкретные действия, named проверки и prerequisites. Общие geometry/roster/численные правила находятся в одной канонической [Documentation.md](../../Documentation.md). Если требуется шестой файл или новая семантика, сначала обновить Board тикет; не реализовывать догадку.

## Execution evidence

NOT STARTED. Создание planning-документа не является implementation, build, test или game launch.

## Execution evidence 2026-11-15

Implementation status: **implemented**. Automated tests are **NOT RUN** in this batch по прямому указанию пользователя (no `dotnet test`, no TDD red/green); named tests written and deferred to the final full-suite run. Build evidence записывается один раз в конце батча (`dotnet build DeepSpaceSaga.sln --no-restore`); PASS не заявляется.

Изменено (allowlist expansion 1→3, process-only):
- `tests/DeepSpaceSaga.Engine.Tests/Carrier/Ep0029Us0013Tk0001Tests.cs` — новый acceptance class поверх real ClientRoot Settings/content и shipped starter:
  - `CampaignRoundTripConservesSeventyEight` — production Start/Move/Fire/Exit/Rearm/CaptureSaveState/LoadScenario; независимый oracle `78 = magazines + hold + fired + lost-with-destroyed`; Save21 round-trip не переприменяет manifest; duplicate exit после reload = NoBattle без второго settlement.
  - `DestroyedHullCannotBeRecreated` — terminal hull0 сохраняется в save/load, повторный Start = `CarrierNotReady`, 78 торпед не перевыдаются.
  - `ZeroAmmoDrawAndDuplicateSettlement` — нулевые магазины обеих сторон дают Draw/`NoCombatCapability` без зависания, ExitKind `Stalemate`, consumed36, duplicate exit не пересчитывает settlement.
  - `Save18To21PreservesEconomy` (Default-Save18, Default-Save18-combat) — миграция 18→21 без выдачи carrier/starter assets (нет wing/hangar/42 hold/6 starter crew, 12-cell legacy hull сохранён), economy continuation идентична после save/load, tampered fingerprint отвергается.
- `tests/DeepSpaceSaga.Engine.Tests/ScenarioEngineTests.cs` — starter-shape ожидания: 24 hull cells, 9 modules (два four-cabin living quarters (3,1)/(5,1), два medium bay (1,2)/(6,2) 2×2), cargo 3 stacks (energy1000/food200/torpedo42), 6 crew, snapshot 6 crew/8 cabins/2 reserved; item stacks резолвятся по registry typeId (без индексов 2/5).
- `tests/DeepSpaceSaga.Engine.Tests/ModuleHealthBootstrapTests.cs` — `Default_modules_have_structure`: medium bays 250 SP, остальные starter modules 150 SP, все intact.

Statuses: implementation **implemented**; automated **NOT RUN (deferred to final suite)**; build/focused/format **NOT RUN in this batch**; native/product acceptance **OPEN**.
