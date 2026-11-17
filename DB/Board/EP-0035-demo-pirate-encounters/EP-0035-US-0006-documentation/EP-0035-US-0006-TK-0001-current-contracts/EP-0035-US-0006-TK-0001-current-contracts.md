---
epic: EP-0035-demo-pirate-encounters
story: EP-0035-US-0006-documentation
ticket: EP-0035-US-0006-TK-0001-current-contracts
title: Актуальные механики и технические контракты
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: tooling
matching_test_project: tools/DeepSpaceSaga.Performance.Tests
depends_on: [EP-0035-US-0001-TK-0004-client, EP-0035-US-0002-TK-0003-client, EP-0035-US-0003-TK-0003-client, EP-0035-US-0004-TK-0002-encounter-ui, EP-0035-US-0005-TK-0003-client, EP-0035-US-0007-TK-0004-sensor-runtime, EP-0035-US-0008-TK-0003-console-and-map, EP-0035-US-0009-TK-0004-crew-presentation, EP-0035-US-0010-TK-0005-dialogue-client, EP-0035-US-0011-TK-0005-generation-engine, EP-0035-US-0012-TK-0005-debug-client, EP-0035-US-0013-TK-0004-content, EP-0035-US-0015-TK-0004-save-engine, EP-0035-US-0008-TK-0005-session-overrides, EP-0035-US-0016-TK-0003-recovery-client, EP-0035-US-0017-TK-0002-world-step]
files_touched: 5
serves: [AC-01, AC-02, AC-04]
created: 2026-11-16T20:32:46.441Z
revision: 1
---

> Current authority: D-35-50 и текущая сводка эпика supersede исторические G/A ниже. Закрытые product gaps не блокируют исполнение автоматически; actual source/allowlist review и acceptance остаются обязательными. Implementation NOT STARTED.

# Актуальные механики и технические контракты

## Why

Документация проекта по итогам эпика. Тикет 1/2 даёт проверяемый результат своего слоя. [Story и полный пользовательский результат](../EP-0035-US-0006-documentation.md).

## Decisions

Поручение владельца: «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Время записи 2026-11-16T20:32:46.441Z, не доказанное время отправки. Разрешены тикеты и необходимые дополнительные истории. Human approval технических деталей/баланса не заявляется; stage approved означает автоматическую готовность workflow планирования. Исполнение и публикация требуют отдельного поручения.

## Assumptions

- Используется сокращённая демо на 60–120 минут, один кластер, готовый Tetrarch; backlog полного продукта не включается.
- Технические имена/новые API ниже — планируемые, не утверждения о текущем коде. Численные A-решения в шагах — baseline для owning плейтеста, не доказанный баланс.
- Сначала prerequisites: EP-0035-US-0001-TK-0004-client, EP-0035-US-0002-TK-0003-client, EP-0035-US-0003-TK-0003-client, EP-0035-US-0004-TK-0002-encounter-ui, EP-0035-US-0005-TK-0003-client. При drift исходников пересмотреть allowlist в Board до расширения diff.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `DB/Documentation/02-FirstRelease/Mechanics/DemoEncounters.md` | Новый файл этого тикета; на момент grounding отсутствует. | Только актуальные описания/ссылки по шагам ниже. |
| `DB/Documentation/04-Engineering/ApproachRoutes.md` | `DB/Documentation/04-Engineering/ApproachRoutes.md:1` — # Approach: маршрут сближения | Только актуальные описания/ссылки по шагам ниже. |
| `DB/Documentation/01-Requirements/EngineRequirements.md` | `DB/Documentation/01-Requirements/EngineRequirements.md:1` — # Требования к движку и игровым контрактам | Только актуальные описания/ссылки по шагам ниже. |

| DB/Documentation/04-Engineering/SaveFormat.md | Существующий owner. | Pending encounter, replay, atomic durable outcome и совместимость без новой конкурирующей writer version. |
| DB/Documentation/04-Engineering/TacticalBattle.md | Существующий owner. | Несколько носителей стороны, Disabled после последнего, outcome/settlement и recovery. |

Разрешены только эти 5 implementation/documentation files. Board evidence/status этого тикета обновляются отдельно как каноническая история исполнения; не дают разрешения править произвольный production. Read-only архитектурные источники: `DB/Documentation/00-Process/CLAUDE.md`, `DB/Documentation/01-Requirements/EngineRequirements.md` и [общая карта](../../../EP-0041-demo-validation-and-release/TicketPlan.md).

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

## Уточнение грумминга 2026-11-17

Обновить текущие документы сенсоров, наблюдаемости и DebugConsole после US-0007/0008. G-35-01 не выдавать за принятое правило.

## Уточнение оператора и доклада 2026-11-17

Документировать sensor skill/assignment и dialog stay-paused; G-35-03/05/06 не выдавать за закрытые решения.

## Уточнение сигнатур и скоростей 2026-11-17

Обновить contract signature profiles, counts 6/10, speed ratios/frozen reference и no-class-details report после implementation.

## D-35-17 — потеря контакта и возврат на патруль

D-35-17: описать единую LostContact → last observed position → return patrol модель, active scanning по пути, private knowledge и persisted waypoint. Старые escape/max-pursuit/cooldown-gating правила явно пометить заменёнными; capture radius остаётся открытым.

## D-35-18 — общая физическая дистанция перехвата

D-35-18 (пользователь): «давай возьмем 100 пикселей на самом приближенном масштабе. сколько это будет? Одинаковая дистанция да».
Оба пиратских типа используют одинаковый capture radius в физических world units. Пиксели — запрос калибровки фиксированного радиуса, не runtime условие. Дистанция боя не зависит от текущего zoom, размера окна, DPI/framebuffer scale или типа пиратского носителя.
A-35-10 [SUPERSEDED D-35-19]: первоначально предлагалось 10 км = 100 world units по базовым 100 логическим px. Пользователь позднее выбрал 20 км = 200 world units; 10 км больше не active baseline.
G-35-10 [CLOSED D-35-19]: утверждены одинаковые 20 км = 200 world units. Историческая калибровка 100 px не используется runtime; dynamic max zoom не меняет physical radius.
Grounding: src/DeepSpaceSaga.Client/Settings.json:30–31 — metersPerPixel[0]=100; src/DeepSpaceSaga.Client/UI/MapScalePolicy.cs:14–20 — max(settings.MaximumPpu, shortSide, diagonal); src/DeepSpaceSaga.Client/UI/SkiaWindow.cs:365–378 — framebuffer/window scale и RenderScreenStack с windowSize, то есть логические UI pixels. EngineRequirements: 1 world unit=100 м.
D-35-19: captureRadiusWorldUnits=200 в content; boundary tests проверяют <200, =200, >200 для обоих типов. Число 100 world units из старого A не использовать.

## D-35-19 — перехват на 20 км

D-35-19 (пользователь): «давай 20 километров что у нас еще не понятно для эпика?».
Одинаковая физическая дистанция перехвата для Логова и Танкреда — ровно 20 км = 200 world units между центрами носителей. Условие distance <= 200 world units включает границу; при distance > 200 перехвата по дистанции нет. Runtime не зависит от zoom, window size, DPI или screen pixels.
G-35-10 закрыт. A-35-10 с предложением 10 км superseded этим решением; история пиксельной калибровки сохранена только как история, не конфигурация.
Content: captureRadiusWorldUnits=200, один общий параметр обоих типов. Engine использует authoritative centre-to-centre world geometry; Client получает подтверждённый результат. Не создавать два радиуса/ветки по типу.

## D-35-20 — стартовые радиусы сенсоров

D-35-20 (пользователь): «Давай 200 км у пиратов, 240 у игрока и 264 с Ириной. Добавляй в тикеты. Теперь давай разберем повторные обнаружения: не хочется, чтобы диалог постоянно ставил игру на паузу».
Точные стартовые радиусы демо: оба пиратских сенсора 200 км = 2000 world units; базовый модуль игрока 240 км = 2400 world units; effective radius с назначенной Ириной 264 км = 2640 world units. Формула ровно 2400 + 0.10*2400, один раз. Пиратские демо-контакты используют 2000 world units, без неоговорённой надбавки.
Численная дальность G-35-02 закрыта. Отдельный G-35-02M — технические параметры установки модуля (масса, footprint, прочность, питание), которые надо записать перед content implementation; это не неизвестный радиус и не повторный вопрос о дальности.
Capture radius D-35-19 остаётся 20 км = 200 world units. Для sensor detection граница distance<=effective radius; для capture <=200. Показ debug-маркера вне радиуса не вызывает sensor acquisition.
Текущее уточнение: G-35-04 CLOSED D-35-21/33 — один automatic report на stable group за кампанию, все новые группы одного шага в одной сводке. Manual report вне утверждённого объёма демо.

## Интеграция D-35-21…26 с эскадрой
Нормативное текущее уточнение supersedes прежнюю single-carrier-only трактовку: stable pirate group может быть singleton или demo squad ровно2xЛогово, одна карта-фишка и один automatic report за кампанию. Member identities/loadouts независимы,6 drones каждому,12 squad total; скрытые классы не попадают в world report. Aggregate/runtime/schema принадлежат US0012; multi-carrier assembly US0013; rules US0014; UI/settlement/save US0015. Эти продолжения не заменять объединением drone lists под одним carrier.
Первый automatic sensor report ставит pause; повторные acquisitions не ставят pause и не открывают modal. consumed flag сохраняется по campaign/group. Simultaneous batching CLOSED D-35-33; manual report — отложенное A.
При уничтожении ПОСЛЕДНЕГО carrier стороны оставшиеся surviving drones получают existing Disabled/Speed0, Destroyed не меняются; до этого автоматического отключения нет. Disable не значит pilot death. D-35-26 закрывает orphan G-35-14.
Старые tickets дают prerequisite singleton behavior; итоговая acceptance EP35 требует US0012–0015. Capture20км и sensor200/240/264км остаются утверждёнными; aggregate reference/range policy согласована D-35-32: один group point, max operational radius.
## D-35-47: восстановление captured боя с начала
Пользователь: «Да, принимаем восстановление того же боя с начала после закрытия приложения или сбоя».
Durable prebattle pending checkpoint ДО tactical start: committed capture, fixed group/member/drone composition, exact player/company prebattle state, stable encounter/battleId и replay seed. Continue после close/crash запускает тот же бой с начала, не navigable world. Ordinary midbattle save/load/menu retreat запрещены; perturn resume не вводится.
Win/loss settlement exactly once, затем durable finished checkpoint до map/end. Failed write удерживает pending result/retry storage, не restart/reapply. Crash при atomic persistence даёт либо old durable pending battle replay, либо durable finished state. Intermediate damage persistence не обещается.
G-35-19 CLOSED. US0016 owns engine checkpoint, local-client durable storage и client recovery. Legacy aborted settlement не используется для ordinary EP35 unfinished battle; debug/tutorial context отдельно.
Tests NOT RUN: checkpoint-before-start; same composition/playerstate/seed replay; no navigable escape; failedwrite retryonly; atomicoldpending-orfinished; exactlyonce rewards/losses; no pending loss via autosave.

### Impact inventory / scope expansion
Final diff requires DemoEncounters mechanic; EngineRequirements and FirstReleaseRequirements; DemoCampaign/SaveFormat/TacticalBattle/DebugConsole/ApproachRoutes; Station/Ship/Contracts/GameSession/Load/Save/TacticalBattle screens and ScreenCatalog; TacticalMapSpecification/NpcObservedTrajectories; README and tooling V4 fixture paths. Dependencies EP34 transport are merged and must no longer be described as UI shells. Inventory/evidence remains Board. Expand allowlist accordingly; current descriptions replace stale claims, dated histories remain historical. Full suite follows publication of both documentation tickets.

Current contracts updated across requirements, mechanics, screens, architecture, persistence and tooling. Review identified and corrected stale transport shells, campaign abort/restore-speed statements, and missing legacy-source caveats. Geometry correction preserves old saved coordinates. US5 final expanded regression172/172 PASS (us5-outer-values-tests.log); native NOT RUN. Content/diff checks performed; full suite follows US6TK2 publication.
