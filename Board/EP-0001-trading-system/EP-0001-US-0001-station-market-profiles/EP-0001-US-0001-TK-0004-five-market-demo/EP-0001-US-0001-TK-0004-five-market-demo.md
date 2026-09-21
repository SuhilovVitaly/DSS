---
epic: EP-0001-trading-system
story: EP-0001-US-0001-station-market-profiles
ticket: EP-0001-US-0001-TK-0004-five-market-demo
title: Пять профилей и демонстрационный сценарий
stage: approved
layer: content-data
depends_on: [EP-0001-US-0001-TK-0001-market-profile-schema, EP-0001-US-0001-TK-0002-profile-market-bootstrap, EP-0001-US-0001-TK-0003-market-catalog-content]
files_touched: 4
serves: [AC-02, AC-04, AC-05]
created: 2026-09-21T08:33:04Z
revision: 1
---

# Пять профилей и демонстрационный сценарий

## Why

Пять data-driven ролей становятся наблюдаемым набором реальных рынков. Проверка проходит через существующую сессию, docking dialogue и торговые команды, а не только сравнение JSON.

Matching test project: D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj.
Пути ниже относительны D:/DeepSpaceSaga/DSS.

## Decisions

D-01, 2026-09-21T08:33:04Z: «Сделай тикеты для D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0001-station-market-profiles\EP-0001-US-0001-station-market-profiles.md эпик D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\Documentation.md».
Конкретные bootstrap количества ниже — явные planning assumptions, не новые ответы пользователя.

## Assumptions

- Списки supply/demand взяты из эпика Documentation.md:112–116. Профильные flows пока не выполняются.
- Medium stocks: supply = 1.5× role target; demand = 0.5× target; Transit = 1× target. Target baseline resource120/good80 × role 0.9/1.1/1/1.2/0.8. В profile JSON записываются готовые количества, не target/rate.
- InitialCredits = 12000 × role budget 0.8/1.2/1/1.6/1.4; стартовый Fuel для Medium 200 кг, Transit 400 кг. Fuel — отдельное поле service stock.
- Каждая демонстрация рынка — новый старт того же scenario с выбором нужного Dock target. Повторная стыковка/Undock не требуются.
- Начальная транзитная станция Large; остальные Medium. Исходные Default/Docked остаются неизменными.
- Small demo geometry нужна только для проверки UI, не является будущей экономической картой/маршрутами.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/Settings.json | :2–10 typeData, :12 legacy stamp, :13 defaultScenario | Добавить только typeData.stationMarketProfiles |
| src/DeepSpaceSaga.Client/Data/Markets/station-market-profiles.json | Новый файл; recursive copy уже есть в src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj:264–266 | Полный schema v1 content пяти профилей |
| src/DeepSpaceSaga.Client/Scenarios/MarketProfiles/scenario.json | Новый файл; recursive scenario copy :270–271 того же csproj; пример полей Docked/scenario.json:1–175 | Один независимый демонстрационный сценарий по blueprint ниже |
| tests/DeepSpaceSaga.Client.Tests/StationMarketDemoContentTests.cs | Новый файл; LocalGameSessionConnection.CreateFromScenarioFile(settingsPath, scenarioPath, saveDirectory=null) уже публичный (src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs:65–70) | Проверки shipped data и async integration через публичную границу |

## Public API after the change

No API change. Settings: "stationMarketProfiles": "Data/Markets/station-market-profiles.json".

Dependency schema из TK-0001: root schemaVersion=1, sizeFactors={Outpost:500,Medium:1000,Large:1500,Huge:2000}, profiles[]; каждый profile содержит typeId, displayName, supplyItemTypeIds[], demandItemTypeIds[], initialInventory[{itemTypeId,quantity}], initialCredits, refuelStockKg. Все поля обязательны; initialInventory ровно union supply/demand, без Fuel. Item IDs ниже имеют обязательный префикс item.; profile IDs market.*.

| Profile ID / displayName | Supply (quantity каждого) | Demand (quantity каждого) | InitialCredits | RefuelStockKg |
|---|---|---|---:|---:|
| market.mining / Mining | ice=162, iron-ore=162, magnesium-ore=162, carbon-ore=162 | water=36, food-rations=36, energy-cells=36, steel=36 | 9600 | 200 |
| market.industrial / Industrial | steel=132, energy-cells=132, electronics=132 | iron-ore=66, magnesium-ore=66, carbon-ore=66, silicon=66, water=44, food-rations=44 | 14400 | 200 |
| market.hydroponic / Hydroponic | water=120, protein-mass=120, food-rations=120 | ice=60, energy-cells=40, steel=40, electronics=40 | 12000 | 200 |
| market.transit / Transit | пустой список | water=96, food-rations=96, energy-cells=96, steel=96, electronics=96 | 19200 | 400 |
| market.scientific-military / Scientific/Military | electronics=96 | silicon=48, energy-cells=32, steel=32, water=32, food-rations=32 | 16800 | 200 |

При Large Transit результат: каждый товар 144, Fuel 600, Credits 28800 до docking fee. Uranium ни в один профиль не включать. Числа будущих rate/target/maxStock не добавлять.

Scenario blueprint (все поля/ID фиксированы; дополнительные asset paths не нужны):

- scenarioMetadata: scenarioId="market-profiles", name="Five Station Markets", description="Choose a station and dock to compare its market. Start this scenario again to inspect another station; continuous voyages are outside this demo."
- gameState: gameTimeMs=0, currentSpeed="Speed0", masterSeed=20260921, playerShipObjectId="SPC-MARKET-PLAYER", playerTokens=100000; focus={mode:"Attached",objectId:"SPC-MARKET-PLAYER"}.
- Игрок: ObjectType PlayerShip, persistenceType Permanent, name="Market Surveyor", position (10000,10000), speedMps=0, directionDegrees=0, movementType Stationary, isKnown=true, isDocked=false; dockedStationObjectId отсутствует.
- hullLayout: width=9,height=9,cells=[{x:4,y:0},{x:4,y:1},{x:4,y:2}].
- modules: MOD-MARKET-NAV / module.bridge.navigation.computer.basic / occupiedCells[(4,0)] / structurePoints80; MOD-MARKET-CARGO / module.container.basic / [(4,1)] / structurePoints400; MOD-MARKET-ENGINE / module.engine.basic / [(4,2)] / structurePoints100 / fuelAmountKg100. Все powerState On, operationalState Ready, activeCycle null. Cargo модуля CARGO: water10, food-rations10, energy-cells10, electronics10, все с item. префиксом; у остальных cargo=[].
- Три module types и пределы подтверждены Data/Modules/NavigationComputer/modules-navigationcomputer.json:4–10, Container/modules-container.json:4–10, Engine/modules-engine.json:4–16. Вместимость cargo100000кг/tank1000кг позволяет все проверки.
- Пять станций: objectType Station, persistenceType Permanent, movementType Stationary, speedMps0, directionDegrees0, isKnown=true, portFeeCreditsPerDay100, modules=[]; без explicit credits/inventory, producingModules, events и security zone. Поля необязательной картинки/crew не нужны.
- SPC-MARKET-MINING: name="Mining — Local Market", position(9900,10000), Medium, market.mining.
- SPC-MARKET-INDUSTRIAL: name="Industrial — Local Market", position(10100,10000), Medium, market.industrial.
- SPC-MARKET-HYDROPONIC: name="Hydroponic — Local Market", position(10000,9900), Medium, market.hydroponic.
- SPC-MARKET-TRANSIT: name="Transit — Local Market", position(10000,10100), Large, market.transit.
- SPC-MARKET-SCIENTIFIC: name="Scientific/Military — Local Market", position(10100,10100), Medium, market.scientific-military.
- В JSON использовать camelCase поля positionX/positionY, stationSize и marketProfileId (TK-0002). Не записывать saveFormatVersion, fingerprint или runtime receipts для new-game.

## Implementation steps

1. Добавить profiles JSON ровно по таблице, заполнить все массивы union и required fields. Загрузить через settings path.
2. Создать demo JSON по blueprint. Не копировать uranium/explicit stocks старых сценариев; не менять defaultScenario и legacyCatalogFingerprint.
3. В matching test проверить profile table, все пять assigned IDs, повторяемость initial state, Medium/Large quantities и отсутствие uranium. Существующие Default/Docked требуют прежние семь quantities и Large; defaults не назначать им профиль.
4. Async integration для каждой из пяти станций создаёт новую LocalGameSessionConnection.CreateFromScenarioFile с реальными settings/demo. Работать как IGameSessionConnection; брать snapshots из ReadSnapshotsAsync с CancellationToken timeout до 15 секунд, без бесконечных ожиданий и sleep.
5. Отправить PlayerCommand(uniqueId, sequence, "SPC-MARKET-PLAYER", "MOD-MARKET-NAV", "navigation.dock", TargetObjectId: stationId). На snapshot.ActiveDialogue использовать его InstanceId/Revision и последовательно DialogueCommand(uniqueId, DialogueAction.Choose, instanceId, revision, ChoiceId:"truthful_id"), затем "accept_fee". Переходы подтверждать новым snapshot, не угадывать revision. Эти choice ID заданы Data/Dialogues/station-docking.json:12,21; тип команды — Contracts/DialogueCommand.cs:6–14.
6. После IsDocked snapshot.DockedStationTrade.StationObjectId должен быть target. Купить 1 representative supply item (Mining ice, Industrial steel, Hydroponic water, Transit water, Scientific electronics); продать 1 water из исходного cargo; заправить 1 Fuel. Buy/Sell адресовать MOD-MARKET-CARGO, Refuel — MOD-MARKET-ENGINE; ItemTypeId и Quantity заданы явно, CommandId уникален, ClientSequence растёт. Assert ExecutedQuantity и дельты stocks/cargo/tank/player credits по authoritative unit price; учесть docking fee до сделок.
7. Проверить, что цена единицы берётся из snapshot, бюджет не раскрывается; initial budget можно проверить через Engine fixture/capture в этом же test file, не через новый production API. Каждый сценарий/connection dispose.
8. Invalid-content case создаёт в temp копию profile JSON с item.missing и settings со ссылками на реальные абсолютные пути остальных файлов; CreateFromScenarioFile синхронно отклонён, диагностическое сообщение содержит profile path/id/item. Repo files не модифицировать.

## Out of scope

Engine/LocalClient/Client production C#, csproj, новые экраны, Undock, runtime replenishement и balancing. Новые тестовые файлы/отдельные demo variants сверх Code context не создавать.

## Invariants

Default/Docked explicit priority — EngineRequirements.md:5315–5317. Fuel только refuel — :5128. Все станции находятся в пределах 200 км от спавна — Commands/NavigationComputer/commands.json:12. Dock снова не разрешён при IsDocked — SimulationEngine.cs:1774; новый старт является явной частью demo.

## Tests

StationMarketDemoContentTests:
- Shipping_profiles_match_five_role_baselines (AC-02): exact supply/demand/stock/budget/fuel table.
- Demo_assigns_five_profiles_and_is_deterministic (AC-02, AC-05).
- Default_and_docked_explicit_inventories_are_unchanged (AC-04).
- Every_demo_station_docks_and_trades_via_session (AC-05), theory из пяти target IDs, полный dialogue → Buy/Sell/Refuel и state deltas.
- Unknown_shipping_profile_item_blocks_session_creation (AC-02/граница AC-03, дополнительное покрытие).
- Demo_and_profiles_are_copied_to_output (AC-05).

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj --no-restore --filter FullyQualifiedName~StationMarketDemoContentTests
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Client\DeepSpaceSaga.Client.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

Ручной smoke: New Game → Five Station Markets → выбрать одну станцию → Dock → truthful identity → принять fee → Station → Trade. Сверить строку supply, Buy/Sell=1 и Refuel=1. Для другой станции начать сценарий заново. UI units окончательно проверяются после TK-0005.

## Definition of Done

- Все implementation steps выполнены только в Code context; files_touched не превышен.
- Каждый AC из serves покрыт указанными named tests в пределах вклада этого тикета.
- Named tests и проверки matching project проходят; конкретные исходные failures записаны отдельно, успех не заявляется при пропуске.
- Public API, invariants, out-of-scope и dependency contracts соблюдены.
- Нет незаписанных assumptions, блокирующих вопросов или скрытой работы вне allowlist.
- Независимо проверяемый результат подтверждён тестовым выводом и diff; этот planning-документ сам по себе не является evidence реализации.

## Self-containment check

Четыре файла, полные profile values и scenario blueprint, команды/choice IDs и проверяемые state deltas перечислены. Тесты проходят через существующую session API; не требуется реализовывать docking/Undock или искать дополнительные продуктовые решения. End state: каждый из пяти поставляемых рынков действительно доступен и торгуется.

