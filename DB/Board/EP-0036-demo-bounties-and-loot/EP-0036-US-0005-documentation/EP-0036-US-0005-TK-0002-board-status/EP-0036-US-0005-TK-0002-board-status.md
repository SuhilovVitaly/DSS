---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0005-documentation
ticket: EP-0036-US-0005-TK-0002-board-status
title: Board, реестр и состояние демо
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: tooling
matching_test_project: tools/DeepSpaceSaga.Performance.Tests
depends_on: [EP-0036-US-0005-TK-0001-current-contracts]
files_touched: 3
serves: [AC-01, AC-03, AC-04]
created: 2026-11-16T20:32:46.441Z
revision: 2
---

# Board, реестр и состояние демо

## Current execution status

Implementation is complete under the explicit owner execution request. Planning-only authorizations and NOT STARTED statements below are dated planning history, superseded by this execution. Automated evidence, independent reviews and remaining native/product gates: [ExecutionStatus](../../ExecutionStatus.md). Native/game execution NOT RUN — owner only.


## Why

Документация проекта по итогам эпика. Тикет 2/2 даёт проверяемый результат своего слоя. [Story и полный пользовательский результат](../EP-0036-US-0005-documentation.md).

## Decisions

Поручение владельца: «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Время записи 2026-11-16T20:32:46.441Z, не доказанное время отправки. Разрешены тикеты и необходимые дополнительные истории. Human approval технических деталей/баланса не заявляется; stage approved означает автоматическую готовность workflow планирования. Исполнение и публикация требуют отдельного поручения.

## Assumptions

- Используется сокращённая демо на 60–120 минут, один кластер, готовый Tetrarch; backlog полного продукта не включается.
- Технические имена/новые API ниже — планируемые, не утверждения о текущем коде. Численные A-решения в шагах — baseline для owning плейтеста, не доказанный баланс.
- Сначала prerequisites: EP-0036-US-0005-TK-0001-current-contracts. При drift исходников пересмотреть allowlist в Board до расширения diff.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `DB/Board/EP-0036-demo-bounties-and-loot/Documentation.md` | `DB/Board/EP-0036-demo-bounties-and-loot/Documentation.md:18` — # EP-0036 — Боевые заказы и простая добыча | Только актуальные описания/ссылки по шагам ниже. |
| `DB/Board/README.md` | `DB/Board/README.md:1` — # Board: реестр эпиков | Только актуальные описания/ссылки по шагам ниже. |
| `DB/Board/EP-0031-board-and-demo-scope/DemoPlan.md` | `DB/Board/EP-0031-board-and-demo-scope/DemoPlan.md:1` — # Сокращённый план первой демо — ревизия 2 | Только актуальные описания/ссылки по шагам ниже. |

Разрешены только эти 3 implementation/documentation files. Board evidence/status этого тикета обновляются отдельно как каноническая история исполнения; не дают разрешения править произвольный production. Read-only архитектурные источники: `DB/Documentation/00-Process/CLAUDE.md`, `DB/Documentation/01-Requirements/EngineRequirements.md` и [общая карта](../../../EP-0041-demo-validation-and-release/TicketPlan.md).

## Public API after the change

No API change.

## Implementation steps

1. Сверить завершение всех story/ticket IDs эпика, проверки и фактический dependency order; сохранить честные OPEN/NOT RUN/BLOCKED без наследования старых PASS.
2. Синхронизировать README и DemoPlan с количеством/результатами; ссылки на evidence только в workspace artifacts или canonical Board. Исторические решения не переписывать.
3. Graphify обновить штатным процессом после implementation либо явно NOT RUN с причиной. Отсутствие графа не подменяет source review; не генерировать его в Documentation.

## Served acceptance criteria

- **AC-01:** Инвентаризация влияния: по итоговому diff, контрактам, командам, конфигурации и версиям схем перечислены все затронутые документы с результатом проверки.
- **AC-03:** Board эпика, реестр DB/Board/README.md и DemoPlan синхронизированы; ссылки, ID и количества проверены; Graphify перестроен либо помечен NOT RUN с причиной.
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

Проверки: BoardIdsMatch, DependencyGraphIsAcyclic, EvidencePathsAreCanonical. Это документационный tooling ticket: production/test API не меняются, matching project `tools/DeepSpaceSaga.Performance.Tests` указан для классификации; build/test запуск не требуется. Проверить существование Markdown links, совпадение ID, актуальные signatures и `git diff --check`. Новые незатреканные файлы дополнительно проверять на trailing whitespace, поскольку обычный diff их не видит.

## Definition of Done

- Все документационные шаги выполнены только в allowlist; ссылки и фактические утверждения проверены по owning sources.
- Served AC имеют evidence вклада этого слоя; story end-to-end/native/product gates не закрыты автоматически.
- Для documentation-only изменения проходят проверки локальных ссылок, ID/зависимостей, согласованности с owning sources и git diff --check; build/test/format игры не требуются.
- Public API, units, persistence, idempotency и ограничения соблюдены; нет скрытого follow-up вне Board.
- Evidence в `D:/DeepSpaceSaga/artifacts/validation/<ticket>/<unique-run>/`, status/решения в Board, актуальные контракты в Documentation.
- Удалены только собственные disposable scripts/caches/results; сохранены необходимые ссылки evidence, нет мусора в корнях workspace/repo.

## Self-containment check

Prerequisites, allowlist, новые схемы/команды, concrete steps, критерии и named tests перечислены здесь. Для новых файлов указан создатель; их наличие до исполнения dependency не предполагается. Чтение источников допустимо для проверки drift, но новый продуктовый выбор или расширение files требует явной правки Board.

## Execution scope and evidence

Draft allowlist expanded to all canonical EP36 status cards, Tickets/AcceptanceCoverage/DocumentationImpact/ExecutionStatus and tools/graphify/rebuild.py plus graph guides. Board: 12 stories, 48 tickets, zero missing links/dependencies or ticket cycles (board-validation.json). Graph rebuild includes EP36, excludes evolving publication ledgers to avoid circular hashes. Final thematic test plan is recorded in ExecutionStatus before execution; native/game NOT RUN. Shared DemoPlan owner edits remain unstaged; only execution paragraph was committed with manual-save regression correction.
