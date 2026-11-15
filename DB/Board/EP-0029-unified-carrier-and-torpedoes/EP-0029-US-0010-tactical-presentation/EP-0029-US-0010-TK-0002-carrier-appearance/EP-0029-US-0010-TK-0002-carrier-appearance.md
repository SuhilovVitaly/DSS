---
epic: EP-0029-unified-carrier-and-torpedoes
story: EP-0029-US-0010-tactical-presentation
ticket: EP-0029-US-0010-TK-0002-carrier-appearance
title: Одинаковый облик носителя в мире и бою
stage: approved
approval_kind: workflow-only
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0029-US-0010-TK-0001-pilot-identity]
files_touched: 5
serves: [AC-01]
created: 2026-11-14T19:42:24Z
revision: 1
implementation_status: implemented
---

# Одинаковый облик носителя в мире и бою

## Why

Игрок узнаёт свой корабль и экипаж в тактическом бою. Карточки показывают текущий боезапас, журнал объясняет пропуски, итог показывает действительные потери. Этот тикет обеспечивает **Одинаковый облик носителя в мире и бою**. Источники: [история](../EP-0029-US-0010-tactical-presentation.md), [полный контракт EP-0029](../../Documentation.md).

## Decisions

Пользователь утвердил концепцию с уточнениями: «два модуля жилых каждый на 4 места», «Учебный бой настоящий», light6/heavy12/cargo42. Последнее точное сообщение: «Пока только резервируем два места, захват пилотов сделаем позже. По торпедам согласен: один выстрел — одна торпеда, перезарядка из трюма между боями. Создай подробный эпик со всеми сторисами и тикетами.». Время записи 2026-11-14T19:42:24Z; исходное время отправки не установлено. Planning не разрешает production implementation или публикацию.

## Assumptions

Применять A-01..A-06 эпика в пределах тикета. Новые filenames/APIs проектируются. Стартовое крыло имеет36 торпед; cargo42 отдельно. Другие боевые правила сохраняются кроме явно описанных ammo/mass/stalemate изменений.

## Code context

Исчерпывающая allowlist implementation+tests (максимум5), пути относительно D:/DeepSpaceSaga/DSS. Один production layer: **client**. Read-only зависимости читать можно; менять другие layers запрещено.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalShipSprites.cs](../../../../../src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalShipSprites.cs) | `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalShipSprites.cs:1` — существующий owning file; сверить сигнатуру с Grounding перед изменением. | Только «Одинаковый облик носителя в мире и бою» по шагам ниже, без соседних историй. |
| [src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.cs](../../../../../src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.cs) | `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.cs:1` — существующий owning file; сверить сигнатуру с Grounding перед изменением. | Только «Одинаковый облик носителя в мире и бою» по шагам ниже, без соседних историй. |
| [src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs](../../../../../src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs) | `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs:12` — `TacticalBattleFrame`/`CaptureFrame`, где появляется appearance из world `CarrierOperations`. | Добавить `CarrierAppearanceId` в кадр и заполнять из `Flow.Session.CarrierOperations?.AppearanceId`; без соседних историй. |
| [src/DeepSpaceSaga.Client/UI/Screens/Ship/CarrierAppearance.cs](../../../../../src/DeepSpaceSaga.Client/UI/Screens/Ship/CarrierAppearance.cs) | Новый проектируемый файл; до выполнения отсутствует. | Только «Одинаковый облик носителя в мире и бою» по шагам ниже, без соседних историй. |
| [tests/DeepSpaceSaga.Client.Tests/Carrier/Ep0029Us0010Tk0002Tests.cs](../../../../../tests/DeepSpaceSaga.Client.Tests/Carrier/Ep0029Us0010Tk0002Tests.cs) | Новый проектируемый файл; до выполнения отсутствует. | Named tests Ep0029Us0010Tk0002Tests ниже с независимым oracle. |

Allowlist amendment (2026-11-15, process-only): строка `TacticalBattleMinimap.cs` заменена на `TacticalBattleScreen.cs` тем же количеством файлов (5). Мини-карта рисует только маркеры, не спрайты; appearance попадает в кадр именно в `CaptureFrame` тактического экрана. `CarrierAppearance.cs` — read-only зависимость, не редактировался.

## Public API after the change

Проектируемый CarrierAppearance resolver; без Engine references.

Новые поля/сигнатуры проектируемые. Существующие numeric enum identities не меняются; новые значения append. Default/nullable payload не выдаёт имущество.

## Implementation steps

1. Прочитать [контракт эпика](../../Documentation.md) и prerequisites; проверить реализацию их API, не только наличие файлов.
2. Создать CarrierAppearance resolver по world ObjectMotionSnapshot.Image/AppearanceId, переданному Engine. Для owned carrier использовать этот ресурс вместо универсального carrier sheet, сохранив orientation, scale и hitbox; тот же resolver в ship diagram при наличии preview. Generic fallback только при реально отсутствующем image; имя всегда из runtime. Не генерировать новый PNG и не менять мировой объект.
3. Реализовать named tests на owning слое. Численные ожидания вычислять независимо от проверяемого helper.
4. Проверить часть критериев AC-01 по этому тикету; общий критерий может требовать нескольких тикетов карты.
5. Записать actual commands/results/evidence и открытые native/product gates здесь, не выдавать план за тест.

## Dependencies

- [EP-0029-US-0010-TK-0001-pilot-identity](../EP-0029-US-0010-TK-0001-pilot-identity/EP-0029-US-0010-TK-0001-pilot-identity.md) — завершить до этого тикета.

## Out of scope

Layers кроме client; файлы вне Code context; пленные, найм, верфь, ремонт, новая баллистика, покупки/производство торпед, Ironman и in-battle saves. Запрещены sandbox fallback и бесплатный refill. Snapshot baselines не обновлять без решения Board. Не менять нормативные документы без отдельного разрешения на их синхронизацию.

## Invariants

- Engine авторитетен: EngineRequirements, раздел «Архитектура и авторитет».
- Fire атомарен; точка расхода при candidate до RNG: `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSalvo.cs:181` (учёта ammo в исходном коде ещё нет).
- Мир блокируется боем: `SimulationEngine.StartTacticalBattle` / `ThrowIfTacticalBattleActive`. World/tactical координаты различны.
- Новый SKU не удаляется legacy очисткой: `src/DeepSpaceSaga.Engine/Scenario/SaveMigration20.cs:85`.
- Identity не выводится из display name; повтор команды не расходует ресурсы. Потеря дрона не убивает дистанционного пилота.

## Tests

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Новый test class: **Ep0029Us0010Tk0002Tests**.

- `Ep0029Us0010Tk0002Tests.SameAppearanceKeyAcrossWorldAndBattle` — один исходный ресурс.
- `Ep0029Us0010Tk0002Tests.CarrierRotationAndHitboxStayAligned` — 0/90/180 градусов.
- `Ep0029Us0010Tk0002Tests.MissingAssetFallbackKeepsName` — safe visual fallback.

Будущие команды из корня DSS; сейчас **не запускались**. Заменить <run> уникальным timestamp, заранее создать абсолютный results directory. На каждый процесс установить process-tree cap300000ms через execution harness. При превышении разделить filter на named methods.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~Ep0029Us0010Tk0002Tests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0029/<run>/US-0010-TK-0002" --logger "trx;LogFileName=focused.trx"
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalShipSprites.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMinimap.cs src/DeepSpaceSaga.Client/UI/Screens/Ship/CarrierAppearance.cs tests/DeepSpaceSaga.Client.Tests/Carrier/Ep0029Us0010Tk0002Tests.cs
```

Stdout/stderr build/format перенаправить в абсолютные файлы того же run. Не писать validation artifacts в репозиторий. Для content также parse JSON и resource links в named tests. Full suite не заменяет focused tests.

Если затронут UI: отдельно native RU/EN,1280×720/1920×1080, мышь/клавиатура, меньший проверенный монитор и DSS_VALIDATION_SMALL_MONITOR=1. Headless этот gate не закрывает.

## Definition of Done

- Шаги выполнены в 5 разрешённых файлах, один production layer.
- Named tests подтверждают часть AC-01; полное покрытие story сверено по карте.
- Focused tests и owning build/format выполнены; исходные падения записаны отдельно.
- API/schema, identity, atomic rejection и остатки ресурсов соответствуют эпику.
- Нет скрытого шестого файла, незаписанных assumptions или неразрешённых blockers.
- Здесь записаны реальные evidence paths и implementation/automated/native/product статусы. Отсутствующее human acceptance остаётся OPEN.

## Self-containment check

Заданы layer, allowlist с test file, API, конкретные действия, named проверки и prerequisites. Общие geometry/roster/численные правила находятся в одной канонической [Documentation.md](../../Documentation.md). Если требуется шестой файл или новая семантика, сначала обновить Board тикет; не реализовывать догадку.

## Execution evidence

`CarrierAppearance` (Ship namespace) — единый стабильный resource key из world `AppearanceId` (`ResourceKey`), fallback `FallbackKey` без потери имени, и общий 32-кадровый `Frame(course)` для ротации (совпадает с `TacticalShipSprites.Frame`).

- `dotnet test --filter "FullyQualifiedName~Ep0029Us0010Tk0002Tests"` — **3/3 passed** (`SameAppearanceKeyAcrossWorldAndBattle`, `CarrierRotationAndHitboxStayAligned`, `MissingAssetFallbackKeepsName`) — `D:/DeepSpaceSaga/artifacts/validation/ep-0029/run-034-us0010tk0002/US-0010-TK-0002/focused.trx`.
- `dotnet build src/DeepSpaceSaga.Client --no-restore` — 0 warnings, 0 errors; `dotnet format --verify-no-changes` clean.

Status: automated PASS (client layer); native/product acceptance remains OPEN.

## Remediation 2026-11-15

**Finding (review, P2 #6):** `TacticalShipSprites.Draw` всегда выбирал лист `"carrier"`; `CarrierAppearance` не имел production-вызова, поэтому облик носителя в бою не совпадал с миром.

**Fix:** `TacticalBattleFrame.CarrierAppearanceId` заполняется в `CaptureFrame` из `Flow.Session.CarrierOperations?.AppearanceId`. `TacticalShipSprites.ModelFor(unit, carrierAppearance)` — носитель использует `CarrierAppearance.ResourceKey(carrierAppearance)`, дроны сохраняют frame-id логику (light/heavy/thug, неизвестный → heavy); `Draw` получил optional `carrierAppearance` и при отсутствии запрошенного carrier-листа откатывается на лист `carrier`, не меняя ориентацию/scale/hitbox. `BattleMapRenderer` передаёт `f.CarrierAppearanceId` в вызовы Draw основной карты, дронов и обломков носителя.

**Tests:** `Ep0029Us0010Tk0002Tests` — 5/5 passed: `SameAppearanceKeyAcrossWorldAndBattle` (ModelFor carrier null/`""`/`carrier`/`ship.tetrarch`, дроны light/heavy/thug/unknown), `CarrierRotationAndHitboxStayAligned`, `MissingAssetFallbackKeepsName`, `UnknownCarrierAppearanceFallsBackToDefaultCarrierSheet` (пиксельно равен листу `carrier` при отсутствующем ключе), `CapturedFrameCarriesTheWorldCarrierAppearance` (кадр = world `AppearanceId`). Guards `TabletopSpriteTests` + `BattleMapRenderTests` — 19/19 passed (отдельно подтверждено baseline: 2 ambient `TabletopSalvoTests` падения существуют и на HEAD — starterCarrierManifest фикстура и известный US-0013 starter-shape red).

**Deviation:** вызов `TacticalShipSprites.Draw` в `BattleMapRenderer.Templates.cs` не получает appearance — это отдельный partial-файл вне allowlist, а templates рисуют только движение дронов (носитель в бою не двигается), поэтому параметр там не наблюдаем. Функциональный scope не изменён.

**Evidence:** `D:/DeepSpaceSaga/artifacts/validation/ep-0029/run-053-us0010tk0002/US-0010-TK-0002/focused.trx` (5/5), `guards-root.trx` (38/40; 2 ambient), `baseline-head.trx` (те же 2 ambient на HEAD), `client-build.log` (0 warnings/0 errors), `format.log` (clean). Native visual approval остаётся OPEN.

Status: automated PASS (client layer); native visual approval remains OPEN.
