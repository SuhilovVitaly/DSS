---
epic: EP-0034-demo-transport-contracts
story: EP-0034-US-0007-documentation
ticket: EP-0034-US-0007-TK-0001-current-contracts
title: Актуальные механики и технические контракты
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: tooling
matching_test_project: tools/DeepSpaceSaga.Performance.Tests
depends_on: [EP-0034-US-0001-TK-0004-client, EP-0034-US-0002-TK-0003-client, EP-0034-US-0003-TK-0003-client, EP-0034-US-0004-TK-0003-client, EP-0034-US-0005-TK-0002-transport-flow, EP-0034-US-0006-TK-0004-client]
files_touched: 21
serves: [AC-01, AC-02, AC-04]
created: 2026-11-16T20:32:46.441Z
revision: 2
---

# Актуальные механики и технические контракты

## Why

Документация проекта по итогам эпика. Тикет 1/2 даёт проверяемый результат своего слоя. [Story и полный пользовательский результат](../EP-0034-US-0007-documentation.md).

## Decisions

Поручение владельца: «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Время записи 2026-11-16T20:32:46.441Z, не доказанное время отправки. Разрешены тикеты и необходимые дополнительные истории. Human approval технических деталей/баланса не заявляется; stage approved означает автоматическую готовность workflow планирования. Исполнение и публикация требуют отдельного поручения.

## Assumptions

- Используется сокращённая демо на 60–120 минут, один кластер, готовый Tetrarch; backlog полного продукта не включается.
- Технические имена/новые API ниже — планируемые, не утверждения о текущем коде. Численные A-решения в шагах — baseline для owning плейтеста, не доказанный баланс.
- Сначала prerequisites: EP-0034-US-0001-TK-0004-client, EP-0034-US-0002-TK-0003-client, EP-0034-US-0003-TK-0003-client, EP-0034-US-0004-TK-0003-client, EP-0034-US-0005-TK-0002-transport-flow, EP-0034-US-0006-TK-0004-client. При drift исходников пересмотреть allowlist в Board до расширения diff.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `DB/Documentation/02-FirstRelease/Mechanics/DemoTransportContracts.md` | Новый файл этого тикета; на момент grounding отсутствует. | Только актуальные описания/ссылки по шагам ниже. |
| `DB/Documentation/04-Engineering/DemoTransportContracts.md` | Новый файл этого тикета; на момент grounding отсутствует. | Только актуальные описания/ссылки по шагам ниже. |
| `DB/Documentation/01-Requirements/EngineRequirements.md` | `DB/Documentation/01-Requirements/EngineRequirements.md:1` — # Требования к движку и игровым контрактам | Только актуальные описания/ссылки по шагам ниже. |

**Расширение scope 2026-11-17 (исполнение, EpicExecutionPrompt §1/§6):** инвентаризация по итоговому diff `83782f77..e65bd5df` нашла 18 других документов `DB/Documentation`, которые описывали Contracts как оболочку, не знали демо-manifest, резерва трюма, проводок контрактов, новых разделов сохранения и кнопки J/Ctrl+J. Каноническое правило «один контракт в одном месте, зависимые сводки обновляются» (DocumentationSystem.md) требует их правки; allowlist расширен до 21 документа (2 новых + 19 изменённых, список в «Результат исполнения»). Production-код, тесты и данные не меняются.

Исходный план: разрешены только эти 3 implementation/documentation files. Board evidence/status этого тикета обновляются отдельно как каноническая история исполнения; не дают разрешения править произвольный production. Read-only архитектурные источники: `DB/Documentation/00-Process/CLAUDE.md`, `DB/Documentation/01-Requirements/EngineRequirements.md` и [общая карта](../../../EP-0041-demo-validation-and-release/TicketPlan.md).

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

## Результат исполнения (2026-11-17)

Implementation: выполнено; только Markdown в `DB/Documentation` и эта карточка. Сверка с исходниками на `e65bd5df`: контракты (`StationJobsContracts.cs`, `PassengerCapacityContracts.cs`, `PassengerContractsContracts.cs`, `TransportFailureContracts.cs`, `CargoContractsContracts.cs`), Engine (`SimulationEngine.StationJobs/PassengerCapacity/PassengerContracts/CargoContracts/TransportFailure.cs`, порядок шага и dispatch в `SimulationEngine.DemoCampaign.cs`, Load-проверки в `SimulationEngine.cs`, `TradeExecution.cs`, `RecordVoyageAmount`), save-разделы `DemoCampaignData.cs`, content `jobs.json`/`demo-starter-carrier.json`/`Scenarios/Demo/scenario.json`, Client (`ContractsScreen.cs`, `ShipScreen.cs`, `GameSessionScreen.cs` J/Ctrl+J, `SkiaWindow.OpenContractsAsync`, locale `Demo.*`/`Finance.Passenger*`), story validation US-0001…US-0006 и TK-0006.

### Инвентаризация влияния (AC-01)

| Документ | Что изменилось в результате эпика | Обновление | Проверка |
|---|---|---|---|
| `02-FirstRelease/Mechanics/DemoTransportContracts.md` | Новый игровой цикл перевозок | Создан: каюты, доска, принятие, доставка, отмена/просрочка/долг, журнал, сохранение | ссылки, факты по исходникам |
| `04-Engineering/DemoTransportContracts.md` | Новые DTO, команды, коды, формулы, порядок шага, ledger, save-разделы | Создан технический контракт и список ограничений | ссылки, сигнатуры |
| `01-Requirements/EngineRequirements.md` | Пассажирские контракты больше не UI-shell | Раздел «Перевозки демо-кампании», исправлена граница требований | ссылки |
| `01-Requirements/FirstReleaseRequirements.md` | Contracts реализован в демо | Таблица Hire/Contracts разделена, статус EP-0034 | ссылки |
| `02-FirstRelease/Mechanics/CrewAndHabitation.md` | Каюты демо-носителя, реальные пассажиры | Заменён placeholder-абзац текущим правилом | ссылки |
| `02-FirstRelease/Mechanics/CargoHold.md` | Опечатанный груз резервирует трюм | Добавлен резерв и ограничения | ссылки |
| `02-FirstRelease/Mechanics/Money.md` | Выплаты, штрафы, долг по контрактам | Добавлен абзац | ссылки |
| `02-FirstRelease/Mechanics/Trading.md` | Buy ограничен резервом | Добавлена ссылка-сводка | ссылки |
| `02-FirstRelease/Screens/Contracts.md` | Заглушка заменена доской и журналом | Переписан экран | ссылки, тексты по presentation |
| `02-FirstRelease/Screens/Ship.md` | Разбиение кают под экипажем | Добавлен абзац, снят статус Contracts-оболочки | ссылки |
| `02-FirstRelease/Screens/ScreenCatalog.md` | Назначение Contracts | Строка таблицы и сводка | ссылки |
| `02-FirstRelease/Screens/Station.md` | Contracts — рабочий экран | Исправлено утверждение | ссылки |
| `02-FirstRelease/Screens/GameSession.md` | Кнопка J и Ctrl+J | Добавлен абзац панели механик | исходник `GameSessionScreen.cs` |
| `02-FirstRelease/Screens/Finance.md` | Подписи «Выплаты/Штрафы по контрактам» | Добавлен абзац | locale |
| `02-FirstRelease/Screens/Trade.md` | Доступный cargo после резерва | Уточнено | ссылки |
| `03-Design/TabletopClient.md` | «Contracts показывают оболочки» устарело | Исправлено | — |
| `04-Engineering/DemoCampaign.md` | Демо-manifest, секция Transport | Обновлены старт и абзац секций | ссылки |
| `04-Engineering/SaveFormat.md` | 4 новых необязательных раздела, версия Save22 прежняя | Добавлен абзац | `DemoCampaignData.cs` |
| `04-Engineering/TradingAndSolarSystem.md` | `PlayerFreeHoldKg`, postings `passenger:`/`cargo:`/`penalty:` | Добавлен абзац | `TradeExecution.cs`, `VoyageLedger.cs` |
| `00-Process/CLAUDE.md` | Перечень текущих контрактов | Добавлены DemoCampaign и DemoTransportContracts; архитектурный абзац актуален | ссылки |
| `README.md` (Documentation) | Новые документы, название Contracts | Навигация | ссылки |

Проверено без изменений: остальные 43 документа `DB/Documentation` (в т.ч. `TetrarchClass.md` — класс и модули задаются content, демо-manifest описан в DemoCampaign; `Docking.md`; `Graphify*.md` — решение в TK-0002) и 13 Markdown вне `DB` (`AGENTS.md`, `CLAUDE.md`, `.claude/commands/where.md`, `src/.../Skins/README.md`, W4 character-generator `SKILL.md`/`assembly-contract.md`, `tests/.../Fixtures/Saves/README.md` — датированная запись Save18/19, `tools/*/README.md`, `EP0013.md`, два `stories/*.md` EP-0029) — перевозок не описывают. Комментарии в коде, ссылающиеся на `Screens/Contracts.md`, остаются верными. Board-документы — TK-0002.

### Проверки

- Относительные ссылки всех изменённых/новых `.md` (скрипт `D:/DeepSpaceSaga/artifacts/validation/EP-0034/us0007-20261117T161504/check_links.py`): 21 файл, 252 ссылки, 0 битых.
- В `DB/Documentation` только `.md` (0 файлов иных форматов); `git diff --check` чисто; trailing whitespace в новых файлах нет.
- Build/test не запускались: документационный тикет; полная solution suite — отдельный прогон оркестратора.

Assumptions: A-01 (technical) — описания вне EP-0034 (Hire-оболочка) оставлены как в этой ветке; параллельный EP-0033 в `origin/main` переписал те же строки (`Ship.md`, `ScreenCatalog.md`, `Station.md`, `CrewAndHabitation.md`, `FirstReleaseRequirements.md`, `DemoCampaign.md`) — при слиянии оставить формулировки обоих эпиков (merge watch в ExecutionStatus). A-02 (technical) — численные значения content не копируются, кроме четырёх кают (решение DemoPlan) и имён ключей jobs.json. Native/product gates: NOT RUN / OPEN. Только self-review; независимое review не заявляется.
