---
epic: EP-0032-demo-campaign-continuity
story: EP-0032-US-0006-documentation
ticket: EP-0032-US-0006-TK-0001-current-contracts
title: Актуальные механики и технические контракты
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: tooling
matching_test_project: tools/DeepSpaceSaga.Performance.Tests
depends_on: [EP-0032-US-0001-TK-0003-client, EP-0032-US-0002-TK-0003-client, EP-0032-US-0003-TK-0004-terminal-save-policy, EP-0032-US-0004-TK-0004-client, EP-0032-US-0005-TK-0002-continue-menu, EP-0032-US-0007-TK-0005-campaign-client]
files_touched: 3
serves: [AC-01, AC-02, AC-04]
created: 2026-11-16T20:32:46.441Z
revision: 1
---

# Актуальные механики и технические контракты

## Why

Документация проекта по итогам эпика. Тикет 1/2 даёт проверяемый результат своего слоя. [Story и полный пользовательский результат](../EP-0032-US-0006-documentation.md).

## Decisions

Поручение владельца: «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Время записи 2026-11-16T20:32:46.441Z, не доказанное время отправки. Разрешены тикеты и необходимые дополнительные истории. Human approval технических деталей/баланса не заявляется; stage approved означает автоматическую готовность workflow планирования. Исполнение и публикация требуют отдельного поручения.

## Assumptions

- Используется сокращённая демо на 60–120 минут, один кластер, готовый Tetrarch; backlog полного продукта не включается.
- Технические имена/новые API ниже — планируемые, не утверждения о текущем коде. Численные A-решения в шагах — baseline для owning плейтеста, не доказанный баланс.
- Сначала prerequisites: EP-0032-US-0001-TK-0003-client, EP-0032-US-0002-TK-0003-client, EP-0032-US-0003-TK-0004-terminal-save-policy, EP-0032-US-0004-TK-0004-client, EP-0032-US-0005-TK-0002-continue-menu, EP-0032-US-0007-TK-0005-campaign-client. При drift исходников пересмотреть allowlist в Board до расширения diff.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `DB/Documentation/04-Engineering/SaveFormat.md` | `DB/Documentation/04-Engineering/SaveFormat.md:1` — # Сохранения и восстановление | Только актуальные описания/ссылки по шагам ниже. |
| `DB/Documentation/04-Engineering/TacticalBattle.md` | `DB/Documentation/04-Engineering/TacticalBattle.md:1` — # Тактический бой носителей | Только актуальные описания/ссылки по шагам ниже. |
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

- Все шаги выполнены только в allowlist, тесты проверяют указанные исходы и ошибки.
- Served AC имеют evidence вклада этого слоя; story end-to-end/native/product gates не закрыты автоматически.
- Focused checks и owning build/format проходят либо конкретное исходное падение записано как блокер; timeout не PASS.
- Public API, units, persistence, idempotency и ограничения соблюдены; нет скрытого follow-up вне Board.
- Evidence в `D:/DeepSpaceSaga/artifacts/validation/<ticket>/<unique-run>/`, status/решения в Board, актуальные контракты в Documentation.
- Удалены только собственные disposable scripts/caches/results; сохранены необходимые ссылки evidence, нет мусора в корнях workspace/repo.

## Self-containment check

Prerequisites, allowlist, новые схемы/команды, concrete steps, критерии и named tests перечислены здесь. Для новых файлов указан создатель; их наличие до исполнения dependency не предполагается. Чтение источников допустимо для проверки drift, но новый продуктовый выбор или расширение files требует явной правки Board.

## Scope expansion before documentation

Impact inventory expands scope to Documentation README, process architecture, FirstReleaseRequirements, CrewAndHabitation, MainMenu/ScenarioSelect/Load/Ship/ScreenCatalog, DebugTuning and a single owning DemoCampaign engineering contract. Save22, profile selection, memorial and autosave changed these current descriptions. Historical Board reports remain historical.

## Execution — 2026-11-17

Current contracts synchronized; inventory in ../../DocumentationImpact.md. Changed Markdown file links checked; Documentation Markdown-only; diff check passed. Solution Release build succeeded with 0 warnings/errors (D:/DeepSpaceSaga/artifacts/validation/ep-0032/20261117-final/build.log). No manual acceptance claimed; user explicitly deferred manual checks.

Review correction documentation: synchronize DemoCampaign, SaveFormat and MainMenu with living continuation, independent F5/F9, load baseline, bounded replay, stable cluster fingerprint and profile isolation. Record focused evidence in ReviewCorrections.md and link from ExecutionStatus/Tickets. No new manual acceptance.
