---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0008-ship-engineer
title: Стартовый инженер и личный навык демонтажа
stage: approved
plan_status: prepared
approval_kind: workflow
current_review: complete
revision: 1
created: 2026-11-17T19:18:11Z
source_request: "Интерактивный груминг EP-0036 и запись принятых решений через сабагентов."
dependencies: [EP-0033-US-0005-crew-upkeep]
implementation_status: implemented
---

# Стартовый инженер и личный навык демонтажа

## Current execution status

Implementation is complete under the explicit owner execution request. Planning-only authorizations and NOT STARTED statements below are dated planning history, superseded by this execution. Automated evidence, independent reviews and remaining native/product gates: [ExecutionStatus](../ExecutionStatus.md). Native/game execution NOT RUN — owner only.


## Входное техническое задание

[Эпик и журнал решений](../Documentation.md). Пользователь: «Да, отдельный член экипажа с назначенной должностью инженера»; «Да, с базовым шансом 25%; инженер повышает его до 35–40%». На вариант личного бонуса +10…15 процентных пунктов, определяемого при создании персонажа: «1 и можешь задавать мне сразу несколько вопросов?».

## User story

Как игрок, я вижу отдельного инженера в стартовом экипаже и его личный навык. Engine применяет этот навык к демонтажу и сбору металлолома, а сохранение/загрузка не меняют бонус.

## Acceptance criteria

- **AC-01** — Demo New Game содержит отдельного Engineer с уникальным CrewId; его каюта, питание и жалованье учитываются как у обычного члена экипажа.
- **AC-02** — Личный целочисленный бонус 10…15 п.п. определяется один раз при создании и сохраняется. Без действующего инженера бонус0; со своим инженером10…15 п.п.
- **AC-03** — Engine предоставляет один authoritative getter бонуса для лута; UI не вычисляет дополнительный независимый навык и не складывает нескольких инженеров.
- **AC-04** — Корабельный список экипажа показывает инженера и бонус в RU/EN; Load сохраняет личность/навык. Старые saves не получают нового члена экипажа автоматически.

## Decisions / review log

Пользователь: «Да, отдельный член экипажа с назначенной должностью инженера»; «Да, с базовым шансом 25%; инженер повышает его до 35–40%». На вариант личного бонуса +10…15 процентных пунктов, определяемого при создании персонажа: «1 и можешь задавать мне сразу несколько вопросов?».

Время записи 2026-11-17T19:18:11Z, не время отправки. Детализация создана по явному запросу пользователя через сабагента; workflow approval не является human review технических API или доказательством реализации.

## Grounding

- `src/DeepSpaceSaga.Contracts/CarrierOperations.cs:10 CrewRole, :64 CrewMemberSnapshot`.
- `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:367 ShipCrewMemberData`.
- `src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs:639 проверка sensor skill`.
- `src/DeepSpaceSaga.Engine/SimulationEngine.cs:1994 ResolveShipCrew, :1364 BuildSaveCrewMember, :4124 CrewMemberRuntime`.
- `src/DeepSpaceSaga.Engine/SimulationEngine.Carrier.cs:98 BuildCrewProjection, :133 occupancy`.
- `src/DeepSpaceSaga.Client/Data/Ships/demo-starter-carrier.json:151 crew стартового носителя`.
- `src/DeepSpaceSaga.Client/UI/Screens/Ship/ShipCrewPanel.cs:8 read-only crew presentation`.

## Dependencies

- EP-0033-US-0005-crew-upkeep

Ticket-level prerequisites ниже определяют реальный порядок. Текущий код prerequisites проверяется перед исполнением; planned files обозначены в Code context.

## Approved ticket map

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [EP-0036-US-0008-TK-0001-contracts](EP-0036-US-0008-TK-0001-contracts/EP-0036-US-0008-TK-0001-contracts.md) | Роль инженера и навык в crew projection | contracts | none | AC-02, AC-03, AC-04 |
| [EP-0036-US-0008-TK-0002-identity-and-skill](EP-0036-US-0008-TK-0002-identity-and-skill/EP-0036-US-0008-TK-0002-identity-and-skill.md) | Создание и сохранение личного навыка инженера | engine | EP-0036-US-0008-TK-0001-contracts | AC-01, AC-02, AC-03 |
| [EP-0036-US-0008-TK-0003-crew-projection](EP-0036-US-0008-TK-0003-crew-projection/EP-0036-US-0008-TK-0003-crew-projection.md) | Проекция реального инженера без второго состояния | engine | EP-0036-US-0008-TK-0002-identity-and-skill | AC-01, AC-03, AC-04 |
| [EP-0036-US-0008-TK-0004-starter-content](EP-0036-US-0008-TK-0004-starter-content/EP-0036-US-0008-TK-0004-starter-content.md) | Отдельный инженер в стартовом составе демо | content-data | EP-0036-US-0008-TK-0002-identity-and-skill | AC-01, AC-04 |
| [EP-0036-US-0008-TK-0005-crew-ui](EP-0036-US-0008-TK-0005-crew-ui/EP-0036-US-0008-TK-0005-crew-ui.md) | Показать инженера и его бонус в экипаже | client | EP-0036-US-0008-TK-0003-crew-projection, EP-0036-US-0008-TK-0004-starter-content | AC-04 |

## Assumptions

A-08-01: целые значения 10,11,12,13,14,15 равновероятны; отдельный named seed по shipId/crewId не расходует боевой RNG. A-08-02: назначение в демо задаёт роль Engineer у одного стартового персонажа; отдельное меню назначения, найм инженеров и прокачка не входят. При нескольких Engineer данные отклоняются при загрузке вместо суммирования. A-08-03: имя Alex Morgan, CrewId CHR-PLAYER-ENGINEER и уже существующий portrait asset — рабочий content baseline, не художественное согласование.

## Non-goals

Production implementation в этом груминге, универсальный кадровый менеджмент, новые денежные награды, новые ассеты, пытки/вербовка и другие будущие взаимодействия не входят. Существующие IDs и gameplay владельцы сохраняются.

## Gaps and backlog

Продуктовых блокеров установленного объёма нет. Баланс навыка и выбранный портрет требуют будущей продуктовой приёмки. Найм/смена инженера — backlog. Тесты и код не выполнены.

Planning prepared; implementation NOT STARTED; automated/native/product acceptance NOT RUN / OPEN. Эти статусы нельзя заменить наличием Markdown.
