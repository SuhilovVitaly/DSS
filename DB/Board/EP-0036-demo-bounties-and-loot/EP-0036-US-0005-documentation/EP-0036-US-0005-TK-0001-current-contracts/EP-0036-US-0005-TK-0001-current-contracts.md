---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0005-documentation
ticket: EP-0036-US-0005-TK-0001-current-contracts
title: Актуальные механики и технические контракты
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: tooling
matching_test_project: tools/DeepSpaceSaga.Performance.Tests
depends_on: [EP-0036-US-0001-TK-0003-client, EP-0036-US-0002-TK-0003-client, EP-0036-US-0004-TK-0002-reward-report, EP-0036-US-0004-TK-0003-restored-loot-route, EP-0036-US-0008-TK-0005-crew-ui, EP-0036-US-0009-TK-0005-custody-ui, EP-0036-US-0006-TK-0005-trade-names, EP-0036-US-0010-TK-0002-postbattle-presentation, EP-0036-US-0010-TK-0004-jettison-ui, EP-0036-US-0011-TK-0002-starmap-area, EP-0036-US-0012-TK-0002-console-commands, EP-0036-US-0009-TK-0006-prisoner-portrait]
files_touched: 3
serves: [AC-01, AC-02, AC-04]
created: 2026-11-16T20:32:46.441Z
revision: 2
---

# Актуальные механики и технические контракты

## Current execution status

Implementation is complete under the explicit owner execution request. Planning-only authorizations and NOT STARTED statements below are dated planning history, superseded by this execution. Automated evidence, independent reviews and remaining native/product gates: [ExecutionStatus](../../ExecutionStatus.md). Native/game execution NOT RUN — owner only.


## Why

Документация проекта по итогам эпика. Тикет 1/2 даёт проверяемый результат своего слоя. [Story и полный пользовательский результат](../EP-0036-US-0005-documentation.md).

## Decisions

Поручение владельца: «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Время записи 2026-11-16T20:32:46.441Z, не доказанное время отправки. Разрешены тикеты и необходимые дополнительные истории. Human approval технических деталей/баланса не заявляется; stage approved означает автоматическую готовность workflow планирования. Исполнение и публикация требуют отдельного поручения.

## Assumptions

- Используется сокращённая демо на 60–120 минут, один кластер, готовый Tetrarch; backlog полного продукта не включается.
- Технические имена/новые API ниже — планируемые, не утверждения о текущем коде. Численные A-решения в шагах — baseline для owning плейтеста, не доказанный баланс.
- Сначала prerequisites: EP-0036-US-0001-TK-0003-client, EP-0036-US-0002-TK-0003-client, EP-0036-US-0003-TK-0004-client, EP-0036-US-0004-TK-0002-reward-report. При drift исходников пересмотреть allowlist в Board до расширения diff.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `DB/Documentation/02-FirstRelease/Mechanics/DemoBountiesAndLoot.md` | Новый файл этого тикета; на момент grounding отсутствует. | Только актуальные описания/ссылки по шагам ниже. |
| `DB/Documentation/04-Engineering/DemoRewards.md` | Новый файл этого тикета; на момент grounding отсутствует. | Только актуальные описания/ссылки по шагам ниже. |
| `DB/Documentation/01-Requirements/EngineRequirements.md` | `DB/Documentation/01-Requirements/EngineRequirements.md:1` — # Требования к движку и игровым контрактам | Только актуальные описания/ссылки по шагам ниже. |

Разрешены только эти 3 implementation/documentation files. Board evidence/status этого тикета обновляются отдельно как каноническая история исполнения; не дают разрешения править произвольный production. Read-only архитектурные источники: `DB/Documentation/00-Process/CLAUDE.md`, `DB/Documentation/01-Requirements/EngineRequirements.md` и [общая карта](../../../EP-0041-demo-validation-and-release/TicketPlan.md).

## Public API after the change

No API change.

## Implementation steps

1. По итоговому implementation diff сверить команды, схемы, настройки, ограничения и описания с фактическими source signatures. Описать пользовательский цикл и точки отказа этого эпика.
2. Обновить указанные канонические документы вместо исторических appendices; численные настройки ссылками на content. Убрать заменённые описания, сохранить явную грань реализовано/план/не принято.
3. Не помещать сюда отчёты, логи, JSON или screenshots. Неподтверждённый native/product статус хранить в Board. Если impact inventory выявит иной нормативный owner, сначала явно уточнить allowlist этого документационного тикета.

## Served acceptance criteria

- **AC-01:** Инвентаризация влияния: по итоговому diff, контрактам, командам, конфигурации и версиям схем перечислены все затронутые документы с результатом проверки.
- **AC-02:** Обновлены канонические документы DB/Documentation (требования, механики, экраны, сохранения) и затронутые документы вне Documentation; исторические отчёты сохранены с пометкой о замене.
- **AC-04:** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

Вклад этого тикета ограничен слоем `tooling`; полный AC принимается после всей карты истории, включая реальный пользовательский путь. DTO/test scaffold сам по себе не закрывает AC.

## Out of scope

Полная верфь, другие носители, фракционная симуляция, пленные, тактическое отступление, сохранение внутри боя, универсальный квестовый движок и публикация. Не переписывать поставленный EP-0029, не ремонтировать несвязанные файлы и не расширять лимит пяти файлов молча.

## Invariants

- `DB/Documentation/01-Requirements/EngineRequirements.md`, «Архитектура и авторитет» / «Команды, события и воспроизводимость»: Engine владеет исходами/временем/RNG/деньгами; render не мутирует мир.
- `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs:10`: асинхронный transport. `src/DeepSpaceSaga.Engine/SimulationEngine.SortieSettlement.cs:17`: использовать единый settlement, не создавать второй inventory.
- `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:40`: на grounding writer Save21; переход к Save22 принадлежит EP-0032-US-0007. Последующие тикеты не назначают конкурирующие версии writer.
- Reject/duplicate не даёт частичных денег, груза, награды, опыта или повторного звука. Pause не двигает deadlines; calendar и motion units не смешиваются.
- Реализация gameplay-инвариантов — в owning engine ticket; клиентский/content/tooling тикет их не дублирует.

## Tests

Проверки: LinksResolve, StatementsMatchSource, OnlyMarkdownInDocumentation. Это документационный tooling ticket: production/test API не меняются, matching project `tools/DeepSpaceSaga.Performance.Tests` указан для классификации; build/test запуск не требуется. Проверить существование Markdown links, совпадение ID, актуальные signatures и `git diff --check`. Новые незатреканные файлы дополнительно проверять на trailing whitespace, поскольку обычный diff их не видит.

## Definition of Done

- Все документационные шаги выполнены только в allowlist; ссылки и фактические утверждения проверены по owning sources.
- Served AC имеют evidence вклада этого слоя; story end-to-end/native/product gates не закрыты автоматически.
- Для documentation-only изменения проходят проверки локальных ссылок, ID/зависимостей, согласованности с owning sources и git diff --check; build/test/format игры не требуются.
- Public API, units, persistence, idempotency и ограничения соблюдены; нет скрытого follow-up вне Board.
- Evidence в `D:/DeepSpaceSaga/artifacts/validation/<ticket>/<unique-run>/`, status/решения в Board, актуальные контракты в Documentation.
- Удалены только собственные disposable scripts/caches/results; сохранены необходимые ссылки evidence, нет мусора в корнях workspace/repo.

## Self-containment check

Prerequisites, allowlist, новые схемы/команды, concrete steps, критерии и named tests перечислены здесь. Для новых файлов указан создатель; их наличие до исполнения dependency не предполагается. Чтение источников допустимо для проверки drift, но новый продуктовый выбор или расширение files требует явной правки Board.

Груминг 2026-11-17T19:24:04Z : документация включает current bounty postfact entitlement, sale-only modules, per-wreck scrap, postbattle frozen restore, engineer и common cabin/rations custody. Generic durability/установка и interactions prisoners вне демо; не описывать их как реализованные.
Execution scope expansion: current contracts require replacing stale six-crew/two-reserve and combat-only reward descriptions across requirements, DemoCampaign/SaveFormat/TacticalBattle, CrewAndHabitation, transport/services, cargo/money/trade/catalog, relevant screens, debug and portrait guidance; Documentation README and root README navigation. DocumentationImpact.md records each owning document. This authorized final documentation story replaces the draft three-file allowlist.

## Completion evidence
31 current Markdown documents updated; exact inventory in DocumentationImpact.md. All relative links in changed current docs resolve (documentation-links.json). Independent review corrected offer capacity formula and passenger-only ration gate; discovered missing manual loot hotkeys and cabin prisoner summary were fixed in owning US4/US9 tickets. Native/product acceptance remains OPEN.
