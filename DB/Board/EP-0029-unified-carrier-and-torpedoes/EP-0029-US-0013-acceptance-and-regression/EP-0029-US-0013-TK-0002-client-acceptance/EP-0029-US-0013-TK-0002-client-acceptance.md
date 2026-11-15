---
epic: EP-0029-unified-carrier-and-torpedoes
story: EP-0029-US-0013-acceptance-and-regression
ticket: EP-0029-US-0013-TK-0002-client-acceptance
title: Реальный session flow и визуальная проверка
stage: approved
approval_kind: workflow-only
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0029-US-0013-TK-0001-engine-acceptance]
files_touched: 6
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-14T19:42:24Z
revision: 1
implementation_status: implemented
---

# Реальный session flow и визуальная проверка

## Why

Игрок может пройти цепочку обучение → карта → корабль → перезарядка → второй бой → сохранение/загрузка без потери identity и без выдачи ресурсов. Доказательства отдельно подтверждают расчёты, UI и продуктовый баланс. Этот тикет обеспечивает **Реальный session flow и визуальная проверка**. Источники: [история](../EP-0029-US-0013-acceptance-and-regression.md), [полный контракт EP-0029](../../Documentation.md).

## Decisions

Пользователь утвердил концепцию с уточнениями: «два модуля жилых каждый на 4 места», «Учебный бой настоящий», light6/heavy12/cargo42. Последнее точное сообщение: «Пока только резервируем два места, захват пилотов сделаем позже. По торпедам согласен: один выстрел — одна торпеда, перезарядка из трюма между боями. Создай подробный эпик со всеми сторисами и тикетами.». Время записи 2026-11-14T19:42:24Z; исходное время отправки не установлено. Planning не разрешает production implementation или публикацию.

## Assumptions

Применять A-01..A-06 эпика в пределах тикета. Новые filenames/APIs проектируются. Стартовое крыло имеет36 торпед; cargo42 отдельно. Другие боевые правила сохраняются кроме явно описанных ammo/mass/stalemate изменений.

## Code context

Исчерпывающая allowlist implementation+tests (максимум5), пути относительно D:/DeepSpaceSaga/DSS. Один production layer: **client**. Read-only зависимости читать можно; менять другие layers запрещено.

| File | Current state | Allowed change |
|---|---|---|
| [tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialBattleWinnabilityTests.cs](../../../../../tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialBattleWinnabilityTests.cs) | `tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialBattleWinnabilityTests.cs:1` — существующий owning file; сверить сигнатуру с Grounding перед изменением. | Обновить test path под owned contract, не ослабляя исходы. |
| [tests/DeepSpaceSaga.Client.Tests/Carrier/Ep0029Us0013Tk0002Tests.cs](../../../../../tests/DeepSpaceSaga.Client.Tests/Carrier/Ep0029Us0013Tk0002Tests.cs) | Новый проектируемый файл; до выполнения отсутствует. | Named tests Ep0029Us0013Tk0002Tests ниже с независимым oracle. |
| [tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SetupScreenTests.cs](../../../../../tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SetupScreenTests.cs) | Ожидания старого editable F8 loadout (gun/plate/shield dropdowns, Loadout в start request); после US-0012 TK-0002 red. | Process-only amendment (2→6 files): ожидания owned read-only wing (0 editable controls, empty Loadout, SourceShipId/revision, units use owned equipment), legacy editable-path проверки не ослабляют production. |
| [tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Ep0019Scenes.cs](../../../../../tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Ep0019Scenes.cs) | F8 setup expectation на encounter loadout; legacy content fixture не содержит starter manifest. | Process-only amendment (2→6 files): owned read-only setup expectation, adapter alias→owned DroneId для выбора слотов, копия `Data/Ships/starter-carrier.json` в content fixture. |
| [tests/DeepSpaceSaga.Client.Tests/LocalSessionIntegrationTests.cs](../../../../../tests/DeepSpaceSaga.Client.Tests/LocalSessionIntegrationTests.cs) | Temp-копии Default/Docked/Undocked загружаются без contentRoot; Default теперь содержит starterCarrierManifest reference. | Process-only amendment (2→6 files): передать `contentRoot: Path.GetDirectoryName(settingsPath)` при загрузке temp-копии; остальные assertions без изменений. |
| [tests/DeepSpaceSaga.Client.Tests/TacticalBattle/LocalizationCoverageTests.cs](../../../../../tests/DeepSpaceSaga.Client.Tests/TacticalBattle/LocalizationCoverageTests.cs) | Battle-screen language test сравнивает own cards с encounter drone rows. | Process-only amendment (2→6 files): own cards читают identity/equipment из CarrierOperations, enemy rows остаются encounter-based; locale coverage сохраняется. |

## Public API after the change

No production API change; test harness и существующий winnability path.

Новые поля/сигнатуры проектируемые. Существующие numeric enum identities не меняются; новые значения append. Default/nullable payload не выдаёт имущество.

## Implementation steps

1. Прочитать [контракт эпика](../../Documentation.md) и prerequisites; проверить реализацию их API, не только наличие файлов.
2. Добавить integration test real LocalGameSessionConnection/GameSessionHandle/SkiaWindow для новых сценариев. Обновить существующий winnability test на owned entry и actual manifest. Snapshot baselines не принимать автоматически: сначала numerical probes/визуальный просмотр, затем отдельное Board решение. Выполнить manual checklist из эпика на меньшем мониторе; если доступен только headless, оставить native gate OPEN. Не маскировать known baseline failures как success.
3. Реализовать named tests на owning слое. Численные ожидания вычислять независимо от проверяемого helper.
4. Проверить часть критериев AC-01, AC-02, AC-03, AC-04 по этому тикету; общий критерий может требовать нескольких тикетов карты.
5. Записать actual commands/results/evidence и открытые native/product gates здесь, не выдавать план за тест.

## Dependencies

- [EP-0029-US-0013-TK-0001-engine-acceptance](../EP-0029-US-0013-TK-0001-engine-acceptance/EP-0029-US-0013-TK-0001-engine-acceptance.md) — завершить до этого тикета.

## Out of scope

Layers кроме client; файлы вне Code context; пленные, найм, верфь, ремонт, новая баллистика, покупки/производство торпед, Ironman и in-battle saves. Запрещены sandbox fallback и бесплатный refill. Snapshot baselines не обновлять без решения Board. Не менять нормативные документы без отдельного разрешения на их синхронизацию.

## Invariants

- Engine авторитетен: EngineRequirements, раздел «Архитектура и авторитет».
- Fire атомарен; точка расхода при candidate до RNG: `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSalvo.cs:181` (учёта ammo в исходном коде ещё нет).
- Мир блокируется боем: `SimulationEngine.StartTacticalBattle` / `ThrowIfTacticalBattleActive`. World/tactical координаты различны.
- Новый SKU не удаляется legacy очисткой: `src/DeepSpaceSaga.Engine/Scenario/SaveMigration20.cs:85`.
- Identity не выводится из display name; повтор команды не расходует ресурсы. Потеря дрона не убивает дистанционного пилота.

## Tests

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Новый test class: **Ep0029Us0013Tk0002Tests**.

- `Ep0029Us0013Tk0002Tests.OwnedTutorialRearmSecondBattle` — UI commands проходят реальный transport.
- `Ep0029Us0013Tk0002Tests.TutorialEnterAndHintsReportActualOutcome` — seed1 без искусственного выигрыша.
- `Ep0029Us0013Tk0002Tests.AmmoReplayDoesNotShowFutureState` — frame-consistent counter.
- `Ep0029Us0013Tk0002Tests.RussianEnglishNamesAndReservedSeats` — 6 people/2 reserves.

Будущие команды из корня DSS; сейчас **не запускались**. Заменить <run> уникальным timestamp, заранее создать абсолютный results directory. На каждый процесс установить process-tree cap300000ms через execution harness. При превышении разделить filter на named methods.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~Ep0029Us0013Tk0002Tests|FullyQualifiedName~TutorialBattleWinnabilityTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0029/<run>/US-0013-TK-0002" --logger "trx;LogFileName=focused.trx"
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialBattleWinnabilityTests.cs tests/DeepSpaceSaga.Client.Tests/Carrier/Ep0029Us0013Tk0002Tests.cs
```

Stdout/stderr build/format перенаправить в абсолютные файлы того же run. Не писать validation artifacts в репозиторий. Для content также parse JSON и resource links в named tests. Full suite не заменяет focused tests.

Если затронут UI: отдельно native RU/EN,1280×720/1920×1080, мышь/клавиатура, меньший проверенный монитор и DSS_VALIDATION_SMALL_MONITOR=1. Headless этот gate не закрывает.

## Definition of Done

- Шаги выполнены в 6 разрешённых файлах (allowlist amendment 2→6, process-only, зафиксировано в Code context), один production layer.
- Named tests подтверждают часть AC-01, AC-02, AC-03, AC-04; полное покрытие story сверено по карте.
- Focused tests и owning build/format выполнены; исходные падения записаны отдельно.
- API/schema, identity, atomic rejection и остатки ресурсов соответствуют эпику.
- Нет скрытого шестого файла, незаписанных assumptions или неразрешённых blockers.
- Здесь записаны реальные evidence paths и implementation/automated/native/product статусы. Отсутствующее human acceptance остаётся OPEN.

## Self-containment check

Заданы layer, allowlist с test file, API, конкретные действия, named проверки и prerequisites. Общие geometry/roster/численные правила находятся в одной канонической [Documentation.md](../../Documentation.md). Если требуется шестой файл или новая семантика, сначала обновить Board тикет; не реализовывать догадку.

## Execution evidence

NOT STARTED. Создание planning-документа не является implementation, build, test или game launch.

## Execution evidence 2026-11-15

Implementation status: **implemented**. Automated tests are **NOT RUN** in this batch по прямому указанию пользователя (no `dotnet test`, no TDD red/green); named tests written and deferred to the final full-suite run. PASS не заявляется.

Изменено (allowlist expansion 2→6, process-only; production-код не менялся):
- `tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialBattleWinnabilityTests.cs` — `Play` стартует owned carrier из production loader (`SourceShipId`/`ExpectedCarrierRevision`), Enter-only и hint-following orders сохранены, реальная win-проверка не ослаблена; `Result` дополнен settlement consumption, отчёт пишет consumed.
- `tests/DeepSpaceSaga.Client.Tests/Carrier/Ep0029Us0013Tk0002Tests.cs` — новый класс:
  - `OwnedTutorialRearmSecondBattle` — real LocalGameSessionConnection/GameSessionHandle: owned Tutorial → exit (settlement) → rearm через handle → second battle с теми же ShipId/DroneId/CrewId и восстановленным магазином.
  - `TutorialEnterAndHintsReportActualOutcome` — seed1 Enter-only и hints, реальный outcome/losses/consumption в отчёт, победа не форсируется.
  - `AmmoReplayDoesNotShowFutureState` — frame-consistent replay: кадр читает свой step magazine, не финальный/future.
  - `RussianEnglishNamesAndReservedSeats` — 6 crew, 8 cabins, 2 reserved/0 prisoners, locale-независимые имена/портреты, совпадение с tactical build в RU и EN.
- `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SetupScreenTests.cs` — owned read-only ожидания: 0 editable loadout controls, пустой Loadout, ShipId/revision из carrier snapshot, юниты используют owned equipment; scenario/seed/tactics coverage сохранён; legacy row test адаптирован к owned contract без ослабления production.
- `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Ep0019Scenes.cs` — owned F8 setup expectation, alias→owned DroneId mapping, starter manifest скопирован в legacy content fixture.
- `tests/DeepSpaceSaga.Client.Tests/LocalSessionIntegrationTests.cs` — `contentRoot` для temp-копий Default/Docked/Undocked; прочие assertions не менялись.
- `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/LocalizationCoverageTests.cs` — own battle cards читают identity/equipment из CarrierOperations, enemy rows остаются encounter-based.

Known final-suite risk (не скрывается): seed-1 owned Tutorial может больше не быть победой; существующий `TutorialBattleWinnabilityTests` сохраняет реальное win assertion, поэтому финальный прогон должен это показать (checkpoint C1).

Statuses: implementation **implemented**; automated **NOT RUN (deferred to final suite)**; native RU/EN UI gate **OPEN**; product/human acceptance **OPEN**.
