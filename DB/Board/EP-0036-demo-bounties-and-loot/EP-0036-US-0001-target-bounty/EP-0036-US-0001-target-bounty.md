---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0001-target-bounty
title: Заказ на конкретную пиратскую группу
stage: approved
plan_status: approved
approval_kind: workflow
current_review: complete
priority: P0
workstream: feature
dependencies: [EP-0034-US-0006-station-job-board, EP-0035-US-0003-world-battle]
created: 2026-11-16T19:49:59Z
revision: 4
implementation_status: not-started
source_request: "Груминг EP-0036; зафиксировать решения пользователя через сабагента"
---

# Заказ на конкретную пиратскую группу

## Входное техническое задание

[Эпик и общий контракт](../Documentation.md). Текущий груминг заменяет прежний запрет выплаты за уничтоженную до принятия цель. Поручение 2026-11-16 создать эпики/истории, затем тикеты сохраняется как историческое workflow approval; сегодняшние продуктовые решения приведены дословно ниже. Реализация и публикация не разрешались.

## User story

Как игрок, я вижу станционные заказы на конкретных пиратов и могу отслеживать их без срока, залога или штрафа за отмену. После собственной победы я вижу право получить награду у заказчика, даже если не принимал заказ заранее.

## Grounding

- `src/DeepSpaceSaga.Contracts/StationJobsContracts.cs:13` уже объявляет StationJobTypes.Bounty, но `SimulationEngine.StationJobs.cs:235` возвращает Unsupported для bounty acceptance.
- `src/DeepSpaceSaga.Contracts/DemoCampaign.cs:4` — DemoCampaignCommand(CommandId, ExpectedRevision); :11 — snapshot; DemoRewardsSnapshot пока пуст.
- `src/DeepSpaceSaga.Engine/SimulationEngine.DemoCampaign.cs:21` — ExecuteDemoCampaign: hash, revision, candidate validation, deferred commit; :85 — TickDemoCampaign, :105 — BuildDemoCampaignProjection.
- `src/DeepSpaceSaga.Engine/Scenario/DemoCampaignData.cs:48` — EncounterResults; `Scenario/WorldEncounterData.cs` валидирует history/group identity.
- `src/DeepSpaceSaga.Engine/SimulationEngine.WorldEncounterBattle.cs:45` — PrepareEncounterSettlement фиксирует binding/PlayerVictory/units; одного факта Retired недостаточно для награды.
- `src/DeepSpaceSaga.Engine/Content/DemoCampaignDefinitions.cs:31` — DemoJobsDefinition без bounty settings; `SimulationEngine.PirateGroups.cs:41` генерирует устойчивые pirate:01…04.
- `src/DeepSpaceSaga.Engine/SimulationEngine.VoyageLedger.cs:147` поддерживает EventCost/PassengerPayout/PassengerPenalty, не bounty. Его passenger category нельзя использовать как замаскированную награду.
- `src/DeepSpaceSaga.Client/UI/Screens/Contracts/ContractsScreen.cs:25` — существующий экран; `UI/SkiaWindow.cs:1722` — OpenContractsAsync.

## Acceptance criteria / Completion evidence

- **AC-01:** Доска станции показывает её конкретные bounty, цель, разрешённый район поиска, награду и место выплаты; срок/залог отсутствуют.
- **AC-02:** Уничтоженная игроком цель до принятия доступна как требование к выплате; уничтоженная/недоступная цель без доказанной победы не выдаётся как невыполнимый активный заказ.
- **AC-03:** Принять/отменить означает включить/выключить отслеживание одного BountyId; отмена без штрафа/выплаты и не уничтожает право на награду. Повтор не создаёт копий.
- **AC-04:** Save/Load и повторные команды сохраняют исходные issuer/target/reward/Tracking; платёжная eligibility передаётся в US-0002, UI не обещает ретроактивную оплату за постороннюю группу.

## Decisions / Review log

- **D-36-01:** «Давай на станции заказчика. Зафиксируй это в тикетах через сабагента». Выплата только при подтверждённой стыковке со станцией заказчика; ранее записано 2026-11-17T18:59:55Z.
- **D-36-29:** «Да, без срока и залога».
- **D-36-30:** «Да, свободная отмена без штрафа и награды». Отмена сама по себе не выдаёт деньги.
- **D-36-31:** «Да: станция выплатит награду за уже уничтоженную группу».
- **D-36-32:** «За конкретную группу, на которую у этой станции есть заказ; достаточно подтверждённой победы игрока».
- **D-36-33:** «Да, отмена только убирает заказ из активного журнала; право на однократную награду сохраняется».

D-36-29…33 записаны 2026-11-17T19:19:15Z (время записи, не отправки). Предварительное принятие и непрерывное отслеживание не являются условиями выплаты.

Superseded: старые AC «уничтожение до принятия исключает offer» и A-01/A-04 не разрешают скрывать подтверждённую победу и лишать выплаты после Cancel. Теперь запрет относится только к созданию невыполнимого Active, а не к Claimable. История прежнего планирования сохранена этим пояснением; активный контракт только здесь и в обновлённых тикетах.

## Assumptions

- **A-B01:** один persistent BountyId на конкретную группу в каталоге кампании, один issuer; дубликаты TargetGroupId запрещены при загрузке. Ключ выплаты — BountyId в сохранённой кампании, не CommandId/ContractId/визит/acceptance attempt. Повторное принятие меняет только Tracking.
- **A-B02:** определения BountyId, issuer, target и обещанная сумма материализуются однократно в campaign state и не перегенерируются при открытии доски, отмене или календарной публикации перевозок. Новое сохранение без bounty section получает определения из совместимого каталога, включая сопоставление с уже сохранёнными победами; несовместимый каталог не публикуется.
- **A-B03:** сумма — целые неотрицательные Tokens, конфигурация требует положительную награду. Нет deadline/deposit/cancellation fee; временные поля перевозок не переиспользуются с фиктивным бесконечным сроком.
- **A-B04:** состояние отслеживания отдельно от eligibility и Paid. Погибшая цель без подтверждённой победы игрока отображается недоступной, не создаёт невыполнимый активный заказ. Клиент не определяет eligibility.

A-B05 — только стартовый content baseline: bounty:pirate:01 / SPC-0002 / 1500 Tokens; bounty:pirate:02 / SPC-0006 / 2000 Tokens. Это параметры будущего плейтеста, не отдельные решения пользователя. Дополнительных целей эти записи не создают.

## Dependencies

- [EP-0034-US-0006-station-job-board](../../EP-0034-demo-transport-contracts/EP-0034-US-0006-station-job-board/EP-0034-US-0006-station-job-board.md)
- [EP-0035-US-0003-world-battle](../../EP-0035-demo-pirate-encounters/EP-0035-US-0003-world-battle/EP-0035-US-0003-world-battle.md)

## Approved ticket map

| Order / ID | Layer | Dependencies | Served criteria |
|---|---|---|---|
| 1. [EP-0036-US-0001-TK-0001-contracts](EP-0036-US-0001-TK-0001-contracts/EP-0036-US-0001-TK-0001-contracts.md) | contracts | EP-0034-US-0006-TK-0004-client, EP-0035-US-0003-TK-0003-client | AC-01…04 |
| 2. [EP-0036-US-0001-TK-0005-bounty-schema](EP-0036-US-0001-TK-0005-bounty-schema/EP-0036-US-0001-TK-0005-bounty-schema.md) | engine | EP-0036-US-0001-TK-0001-contracts | AC-01…04 |
| 3. [EP-0036-US-0001-TK-0004-localized-feedback](EP-0036-US-0001-TK-0004-localized-feedback/EP-0036-US-0001-TK-0004-localized-feedback.md) | content-data | EP-0036-US-0001-TK-0005-bounty-schema | AC-01…04 |
| 4. [EP-0036-US-0001-TK-0002-engine](EP-0036-US-0001-TK-0002-engine/EP-0036-US-0001-TK-0002-engine.md) | engine | EP-0036-US-0001-TK-0001-contracts, EP-0036-US-0001-TK-0004-localized-feedback | AC-01…04 |
| 5. [EP-0036-US-0001-TK-0003-client](EP-0036-US-0001-TK-0003-client/EP-0036-US-0001-TK-0003-client.md) | client | EP-0036-US-0001-TK-0002-engine | AC-01…04 |

US1 имеет 5 тикетов: добавлен TK0005 engine schema, существующий TK0004 владеет content/locale. Схема → content → runtime; циклов content/runtime нет.

## Non-goals

Универсальная награда за любую пиратскую группу, новые спавны/VIP, репутация, deadline/залоги/штрафы, второй кошелёк, лут/пленные и установка трофеев. Соседние механики остаются у собственных историй.

## Invariants

Frozen BountyId/issuer/target/reward; Tracking не entitlement. Engine владеет доказательством победы и cash. Claim на issuer ровно один раз; history binding и terminal posting переживают Save/Load. Нет фиктивного пассажирского дохода: US2 ведёт отдельную bounty-проводку и меняет тот же PlayerCredits, US4 читает эту проводку для отчёта.

## Gaps and backlog

Продуктовые правила bounty согласованы D-36-01/29…33. Баланс A-B05 и native/product acceptance OPEN до исполнения/плейтеста. Финальная агрегация полной прибыльности относится к EP-0037; не обещать её наличия из одного receipt. Перед исполнением проверить drift actual files; schemas/API выше проектируемые, не факт готовности.

## Статус

Planning workflow complete для этой истории; implementation NOT STARTED; automated/native/product acceptance NOT RUN. Проверки Markdown/ссылок не заменяют проверки игры. Груминг записан 2026-11-17T19:19:15Z.
