---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0009-prisoner-custody
title: Арест пиратов и содержание в общих каютах
stage: approved
plan_status: prepared
approval_kind: workflow
current_review: complete
revision: 2
created: 2026-11-17T19:18:11Z
source_request: "Интерактивный груминг EP-0036 и запись принятых решений через сабагентов."
dependencies: [EP-0036-US-0007-postbattle-loot-lifecycle]
implementation_status: implemented
---

# Арест пиратов и содержание в общих каютах

## Current execution status

Implementation is complete under the explicit owner execution request. Planning-only authorizations and NOT STARTED statements below are dated planning history, superseded by this execution. Automated evidence, independent reviews and remaining native/product gates: [ExecutionStatus](../ExecutionStatus.md). Native/game execution NOT RUN — owner only.


## Входное техническое задание

[Эпик и журнал решений](../Documentation.md). Пользователь: «В демо только арест и содержание; взаимодействия отдельным будущим эпиком»; «Одна каюта на каждого пленного»; «Убираем постоянный резерв: экипаж, пассажиры и пленные используют общий фонд; пленный занимает одну свободную каюту»; «Да, по существующей норме на человека, без жалованья».

## User story

Как игрок, после победы я могу арестовать живого пилота выведенного из строя вражеского дрона. Он занимает одну общую каюту, остаётся отдельным пленником и потребляет пайки без жалованья; повторное действие или Load не создаёт второго пленного.

## Acceptance criteria

- **AC-01** — На открытой postbattle loot session арест живого пилота Disabled enemy drone разрешён при одной доступной общей каюте. Destroyed/no pilot/wrong side/closed battle не допускаются.
- **AC-02** — Каждый пленный сохраняется с source battle/drone/pilot identity и занимает ровно1 каюту; постоянный резерв2 устранён. Пассажирские обязательства и экипаж не вытесняются.
- **AC-03** — Пленный питается по существующему расписанию/норме одного человека, без employment и жалованья. Пауза/дробление времени/SaveLoad не удваивают расход.
- **AC-04** — Окно лута показывает арест/отказ, корабль показывает пленников и фактическую вместимость; отсутствуют сдача, пытки, вербовка и выплаты за пленного.
- **AC-05** — Repeat command/повторный арест/Load не дублирует человека; неподтверждённый отказ не меняет людей, каюты и деньги.
- **AC-06** — После подтверждённого ареста карточка выбранного пилота показывает его же лицо на соответствующем теле пленного: на коленях, бёдра повёрнуты в сторону, руки за спиной; используются профили пропорций v3. Pending/отказ не изображаются как успешный арест. Повторное открытие/Load сохраняет идентичность; отсутствие ассета даёт безопасный fallback без чужого лица.

## Decisions / review log

Пользователь: «В демо только арест и содержание; взаимодействия отдельным будущим эпиком»; «Одна каюта на каждого пленного»; «Убираем постоянный резерв: экипаж, пассажиры и пленные используют общий фонд; пленный занимает одну свободную каюту»; «Да, по существующей норме на человека, без жалованья».

Время записи 2026-11-17T19:18:11Z, не время отправки. Детализация создана по явному запросу пользователя через сабагента; workflow approval не является human review технических API или доказательством реализации.

## Grounding

- `src/DeepSpaceSaga.Contracts/DemoCampaign.cs:33 DemoCompanySnapshot`.
- `src/DeepSpaceSaga.Contracts/PassengerCapacityContracts.cs:11 PassengerCapacitySnapshot`.
- `src/DeepSpaceSaga.Contracts/CarrierOperations.cs:51 CabinReservationSnapshot`.
- `src/DeepSpaceSaga.Engine/Scenario/DemoCampaignData.cs:8 campaign persistence/Validate`.
- `src/DeepSpaceSaga.Engine/SimulationEngine.DemoCampaign.cs:104 dispatch, :94 projection`.
- `src/DeepSpaceSaga.Engine/Scenario/CarrierStateData.cs:15 ReservedCabins default2`.
- `src/DeepSpaceSaga.Engine/SimulationEngine.Carrier.cs:70 writer, :96 constant2, :133 occupancy`.
- `src/DeepSpaceSaga.Engine/SimulationEngine.PassengerCapacity.cs:51–67 crew/passenger/reserved/free and ration feasibility`.
- `src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs:8 MealIntervalMs, :141 ConsumeScheduledRations`.
- `src/DeepSpaceSaga.Engine/SimulationEngine.CrewUpkeep.cs:16 Employment, :40 PeopleFed, :53 wages`.

## Dependencies

- EP-0036-US-0007-postbattle-loot-lifecycle

Ticket-level prerequisites ниже определяют реальный порядок. Текущий код prerequisites проверяется перед исполнением; planned files обозначены в Code context.

## Approved ticket map

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [EP-0036-US-0009-TK-0001-contracts](EP-0036-US-0009-TK-0001-contracts/EP-0036-US-0009-TK-0001-contracts.md) | Команда ареста и проекция пленных | contracts | EP-0036-US-0003-TK-0001-contracts | AC-01, AC-02, AC-04, AC-05 |
| [EP-0036-US-0009-TK-0002-arrest-state](EP-0036-US-0009-TK-0002-arrest-state/EP-0036-US-0009-TK-0002-arrest-state.md) | Однократный арест из подтверждённого результата боя | engine | EP-0036-US-0009-TK-0001-contracts, EP-0036-US-0003-TK-0003-engine, EP-0036-US-0007-TK-0003-postbattle-lifecycle | AC-01, AC-02, AC-05 |
| [EP-0036-US-0009-TK-0003-capacity-and-rations](EP-0036-US-0009-TK-0003-capacity-and-rations/EP-0036-US-0009-TK-0003-capacity-and-rations.md) | Общие каюты и питание без жалованья | engine | EP-0036-US-0009-TK-0002-arrest-state | AC-02, AC-03, AC-05 |
| [EP-0036-US-0009-TK-0004-localized-feedback](EP-0036-US-0009-TK-0004-localized-feedback/EP-0036-US-0009-TK-0004-localized-feedback.md) | Тексты ареста и содержания RU/EN | content-data | EP-0036-US-0009-TK-0001-contracts | AC-04 |
| [EP-0036-US-0009-TK-0005-custody-ui](EP-0036-US-0009-TK-0005-custody-ui/EP-0036-US-0009-TK-0005-custody-ui.md) | Арест в окне лута и список пленных на корабле | client | EP-0036-US-0009-TK-0003-capacity-and-rations, EP-0036-US-0009-TK-0004-localized-feedback, EP-0036-US-0007-TK-0004-postbattle-map | AC-01, AC-04, AC-05 |
| [EP-0036-US-0009-TK-0006-prisoner-portrait](EP-0036-US-0009-TK-0006-prisoner-portrait/EP-0036-US-0009-TK-0006-prisoner-portrait.md) | Изображение захваченного пилота в позе пленного | client | EP-0036-US-0009-TK-0005-custody-ui | AC-04, AC-05, AC-06 |

## Assumptions

Дополнение 2026-11-17T19:50:17Z: по прямому запросу пользователя добавлен [TK-0006 — изображение пленного](EP-0036-US-0009-TK-0006-prisoner-portrait/EP-0036-US-0009-TK-0006-prisoner-portrait.md), после TK-0005, Client, AC-04/05/06. Это шестой тикет в существующей истории: локальное исключение из обычного лимита epic-writer для сохранения уже созданных ID и связей. Workflow approval, не human visual acceptance. Точная цитата и assumptions о выборе лица/костюма записаны в тикете; семь готовых вариантов включают топ/шорты, автоматически переодевать пленного нельзя.

A-09-01: пленник отдельная запись, не CrewMember и не контрактный Passenger. При нехватке пищи используется уже существующий MissingRations, без новой модели голода/смерти. A-09-02: принятие пассажирского обязательства, ещё не посаженного на борт, не позволяет украсть обещанную каюту при аресте; свободное место учитывает такое обязательство. Арест сам по себе не требует запаса еды как дополнительного неоговорённого условия. A-09-03: не вводятся побеги, лечение, освобождение, переводы, зарплата, продажа или награда за пленного.

## Non-goals

Production implementation в этом груминге, универсальный кадровый менеджмент, новые денежные награды, новые ассеты, пытки/вербовка и другие будущие взаимодействия не входят. Существующие IDs и gameplay владельцы сохраняются.

## Gaps and backlog

Взаимодействия с пленными (включая пытки/вербовку) — отдельный будущий эпик без выбранного ID. Пилотская личность берётся из материализованного результата US7; catalog alias не выдаётся за уникального человека. Native/product acceptance OPEN.

Planning prepared; implementation NOT STARTED; automated/native/product acceptance NOT RUN / OPEN. Эти статусы нельзя заменить наличием Markdown.
