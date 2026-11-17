---
epic: EP-0035-demo-pirate-encounters
title: Пиратские встречи и избегание перехвата
stage: approved
created: 2026-11-16T19:49:59Z
source_request: "План одобряю. Оформи его в Board: создай необходимые эпики и пользовательские истории с приоритетами и зависимостями, учитывая уже сделанное в EP-0021 и EP-0029. Пока без тикетов и реализации."
current_review: complete
plan_status: approved
approval_scope: "Одобренный план; отдельное human approval детализации историй не заявляется"
epic_approval: "План одобряю. Оформи его в Board: создай необходимые эпики и пользовательские истории с приоритетами и зависимостями, учитывая уже сделанное в EP-0021 и EP-0029. Пока без тикетов и реализации."
epic_approval_utc: 2026-11-16T19:49:59Z
priority: P0
workstream: feature
dependencies: [EP-0032-demo-campaign-continuity, EP-0034-demo-transport-contracts, EP-0033-demo-company-services]
implementation_status: not-started
---

## Текущий нормативный итог грумминга после D-35-50
Эта сводка — текущая точка входа. Следующие старые разделы сохраняют chronology решений; старые «G открыт», «proposal не принят» и численные A применяются только там, где НЕ superseded текущими D. Все ранее закрытые G01…19 не требуют повторного продуктового согласования; конкретные остатки перечислены ниже.
Объём **17 историй /66 тикетов**. [US0016 recovery](EP-0035-US-0016-encounter-recovery/EP-0035-US-0016-encounter-recovery.md) добавляет3tickets послеUS15. US8 теперь5tickets; US6 docs после всехfeatures.
- Initial4ordinary groups,45/45/10; squad2Логово одна точка, minVmax, maxsensor. Norespawn. START800км hard safezone; остальные400км spawnonly.
- Pirate200/player240/Irina264/script192км; Irina activecrew+assignment, disabledmodule0. Accuracy Irina±1/±2,script±2/±4; один integer estimate и exacttype, hidden classes.
- Light/heavy50/50, sword/spear50/50,4existing layouts по25%; totals6/10/12. Composition fixed before battle checkpoint, no reroll/replay changes.
- Patrol waypoints; lostcontact→lastknown→returnpatrol. Capture<=20км сpiratecontact, неизбежный бой послеreport/pause/explicitcontinue. Earliest continuous capture arbitration, no neighboringmerge; мир frozen вбою/paused после, resume безimmunity.
- Once/group/campaign report; newgroups currentstep единыйbatch, atomicflags/pending; ETA earliestentry20км currentobservedvectors, realduration at1×=4motionseconds.
- Normalfogtrue; owner explicitdebugfalse onlyprojection. Overrides processmemory only, fogcurrent/new/load, chance nextNewGame; restart defaults true/10.
- Lastcarrier destroyed=>surviving dronesDisabled (не pilotdeath), firstcarrierdeath не отключает. Normalexit win/loss only. Durableprebattle→samebattlefromstart afterclose/crash; settledresult exactlyonce/durablebefore release.
### Остаток продуктового аудита
G-35-20 Draw CLOSED D-35-48 и G-35-20T Timeout CLOSED D-35-49: оба player defeat без искусственного damage. Открытых обязательных product вопросов нет.
D-35-50 закрывает A-35-20: после завершённого боя участвовавшая пиратская группа удаляется с карты независимо от исхода, включая Draw/Timeout и живые носители. Это retirement группы, а не фиктивное уничтожение: фактические hull/status/pilot state сохраняются в результате. Pending/replay бой не считается завершённым; retirement и settlement фиксируются вместе ровно один раз, до durable release. Остальные группы сохраняются.
Остальное — технический owner review: module footprint/power; waypoint coordinates/feasible motion; numerical tolerances; serialization/version drift; firstcaptured encounter снеработающим player sensor должен получить fixed composition ДОcheckpoint (не reroll). Narrative внеscope, новая fifthgroup не утверждалась.
Implementation NOT STARTED; build/tests/native/product NOT RUN. Structural planning checks не являются acceptance.
## Актуальная карта историй после аудита

| История | Тикетов | Зависимости начала истории |
|---|---:|---|
| [EP-0035-US-0001-visible-threats](EP-0035-US-0001-visible-threats/EP-0035-US-0001-visible-threats.md) | 5 | EP-0032-US-0001-campaign-roundtrip, EP-0032-US-0004-demo-campaign-start, EP-0035-US-0007-carrier-sensors |
| [EP-0035-US-0002-map-evasion](EP-0035-US-0002-map-evasion/EP-0035-US-0002-map-evasion.md) | 4 | EP-0035-US-0001-visible-threats |
| [EP-0035-US-0003-world-battle](EP-0035-US-0003-world-battle/EP-0035-US-0003-world-battle.md) | 4 | EP-0035-US-0002-map-evasion, EP-0032-US-0003-persistent-outcomes, EP-0035-US-0011-pirate-signatures |
| [EP-0035-US-0004-encounter-continuation](EP-0035-US-0004-encounter-continuation/EP-0035-US-0004-encounter-continuation.md) | 2 | EP-0035-US-0003-world-battle |
| [EP-0035-US-0005-route-threat-choice](EP-0035-US-0005-route-threat-choice/EP-0035-US-0005-route-threat-choice.md) | 4 | EP-0035-US-0001-visible-threats, EP-0034-US-0002-passenger-contract |
| [EP-0035-US-0006-documentation](EP-0035-US-0006-documentation/EP-0035-US-0006-documentation.md) | 2 | EP-0035-US-0001-visible-threats, EP-0035-US-0002-map-evasion, EP-0035-US-0003-world-battle, EP-0035-US-0004-encounter-continuation, EP-0035-US-0005-route-threat-choice, EP-0035-US-0007-carrier-sensors, EP-0035-US-0008-fog-of-war-debug, EP-0035-US-0009-sensor-operator, EP-0035-US-0010-sensor-contact-dialogue, EP-0035-US-0011-pirate-signatures, EP-0035-US-0012-pirate-squadrons, EP-0035-US-0013-squad-battle-assembly, EP-0035-US-0014-squad-battle-rules, EP-0035-US-0015-squad-battle-continuation, EP-0035-US-0016-encounter-recovery, EP-0035-US-0017-world-integration |
| [EP-0035-US-0007-carrier-sensors](EP-0035-US-0007-carrier-sensors/EP-0035-US-0007-carrier-sensors.md) | 4 | EP-0032-US-0004-demo-campaign-start |
| [EP-0035-US-0008-fog-of-war-debug](EP-0035-US-0008-fog-of-war-debug/EP-0035-US-0008-fog-of-war-debug.md) | 5 | EP-0035-US-0007-carrier-sensors, EP-0035-US-0001-visible-threats |
| [EP-0035-US-0009-sensor-operator](EP-0035-US-0009-sensor-operator/EP-0035-US-0009-sensor-operator.md) | 5 | EP-0035-US-0007-carrier-sensors, EP-0033-US-0004-pilot-recruitment |
| [EP-0035-US-0010-sensor-contact-dialogue](EP-0035-US-0010-sensor-contact-dialogue/EP-0035-US-0010-sensor-contact-dialogue.md) | 5 | EP-0035-US-0001-visible-threats, EP-0035-US-0009-sensor-operator, EP-0035-US-0011-pirate-signatures, EP-0035-US-0012-pirate-squadrons |
| [EP-0035-US-0011-pirate-signatures](EP-0035-US-0011-pirate-signatures/EP-0035-US-0011-pirate-signatures.md) | 5 | EP-0035-US-0001-visible-threats, EP-0035-US-0009-sensor-operator |
| [EP-0035-US-0012-pirate-squadrons](EP-0035-US-0012-pirate-squadrons/EP-0035-US-0012-pirate-squadrons.md) | 5 | EP-0035-US-0001-visible-threats, EP-0035-US-0011-pirate-signatures |
| [EP-0035-US-0013-squad-battle-assembly](EP-0035-US-0013-squad-battle-assembly/EP-0035-US-0013-squad-battle-assembly.md) | 4 | EP-0035-US-0012-pirate-squadrons, EP-0035-US-0003-world-battle |
| [EP-0035-US-0014-squad-battle-rules](EP-0035-US-0014-squad-battle-rules/EP-0035-US-0014-squad-battle-rules.md) | 3 | EP-0035-US-0013-squad-battle-assembly |
| [EP-0035-US-0015-squad-battle-continuation](EP-0035-US-0015-squad-battle-continuation/EP-0035-US-0015-squad-battle-continuation.md) | 4 | EP-0035-US-0014-squad-battle-rules, EP-0035-US-0004-encounter-continuation |
| [EP-0035-US-0016-encounter-recovery](EP-0035-US-0016-encounter-recovery/EP-0035-US-0016-encounter-recovery.md) | 3 | EP-0035-US-0015-squad-battle-continuation, EP-0035-US-0017-world-integration |
| [EP-0035-US-0017-world-integration](EP-0035-US-0017-world-integration/EP-0035-US-0017-world-integration.md) | 2 | EP-0033-US-0005-crew-upkeep |

Это карта входных зависимостей историй. Точный порядок исполнения задаётся depends_on тикетов: US0017-TK0001 выполняется как ранняя база, US0017-TK0002 — после gameplay/report, затем recovery и US0006 documentation. Номер истории не задаёт порядок.



[Результаты проверки и карта покрытия](GroomingAudit.md).

## Исторический журнал и исходные карты

# EP-0035 — Пиратские встречи и избегание перехвата

Д5 одобренного плана. **Объём: 17 пользовательских историй, 66 тикетов.** Порядок определяется зависимостями конкретных историй, а не номером эпика.

## Исходный запрос и граница поручения

> План одобряю. Оформи его в Board: создай необходимые эпики и пользовательские истории с приоритетами и зависимостями, учитывая уже сделанное в EP-0021 и EP-0029. Пока без тикетов и реализации.

Запрос разрешает создание/уточнение Board. Ранее приложенный расширенный план — продуктовый материал, заменённый одобренной сокращённой версией; он не разрешает исполнение или публикацию. Исходное поручение ограничивало этап историями. Текущее поручение ниже разрешает тикеты и дополнительные истории; реализация и публикация не запрашивались.

## Grounding

`DB/Documentation/04-Engineering/AiMapEnvironment.md` раздел о полях и patrol radius прямо отделяет территорию от патрульного AI. `src/DeepSpaceSaga.Engine/SimulationEngine.TacticalBattle.cs` — StartTacticalBattle(StartTacticalBattleRequest request) и StartOwnedTacticalBattle задают вход в существующий бой.

## Переиспользование и граница нового объёма

Использовать географию мира и owned battle EP-0029. Новый объём — существующая в мире группа и её жизненный цикл, а не ещё один боевой движок.

[Матрица соответствия старым эпикам](../EP-0031-board-and-demo-scope/DemoPlan.md#переиспользование-ep-0021-и-ep-0029) определяет владельцев без дублирования. Старые тикеты не копируются и не становятся автоматически разрешением на реализацию демо.

## Целевой результат

- Наблюдаемая угроза на маршруте
- Манёвр ухода от преследования
- Перехват запускает бой с конкретной группой
- Сохранение преследования и результата встречи

Результаты оцениваются в общем цикле рейса. [Полный сокращённый контракт](../EP-0031-board-and-demo-scope/DemoPlan.md) содержит лимиты, исключения и контрольные точки.

## Историческая карта первых шести историй

| ID | Результат | Приоритет | Обязательные story dependencies |
|---|---|---|---|
| [EP-0035-US-0001-visible-threats](EP-0035-US-0001-visible-threats/EP-0035-US-0001-visible-threats.md) | Наблюдаемая угроза на маршруте | P0 | EP-0032-US-0001-campaign-roundtrip, EP-0032-US-0004-demo-campaign-start |
| [EP-0035-US-0002-map-evasion](EP-0035-US-0002-map-evasion/EP-0035-US-0002-map-evasion.md) | Манёвр ухода от преследования | P0 | EP-0035-US-0001-visible-threats |
| [EP-0035-US-0003-world-battle](EP-0035-US-0003-world-battle/EP-0035-US-0003-world-battle.md) | Перехват запускает бой с конкретной группой | P0 | EP-0035-US-0002-map-evasion, EP-0032-US-0003-persistent-outcomes |
| [EP-0035-US-0004-encounter-continuation](EP-0035-US-0004-encounter-continuation/EP-0035-US-0004-encounter-continuation.md) | Сохранение преследования и результата встречи | P0 | EP-0035-US-0003-world-battle |
| [EP-0035-US-0005-route-threat-choice](EP-0035-US-0005-route-threat-choice/EP-0035-US-0005-route-threat-choice.md) | Выбор маршрута с учётом угрозы и срока | P0 | EP-0035-US-0001-visible-threats, EP-0034-US-0002-passenger-contract |
| [EP-0035-US-0006-documentation](EP-0035-US-0006-documentation/EP-0035-US-0006-documentation.md) | Документация проекта по итогам эпика | P0 | EP-0035-US-0001-visible-threats, EP-0035-US-0002-map-evasion, EP-0035-US-0003-world-battle, EP-0035-US-0004-encounter-continuation, EP-0035-US-0005-route-threat-choice, EP-0035-US-0017-world-integration |

Aggregate dependencies в frontmatter — перечень внешних источников результатов, не барьер «весь эпик завершён». Независимые истории могут разрабатываться после своих prerequisites. Наличие старого implemented статуса не закрывает сквозную приёмку.

## Ограничения, допущения и gaps

- **EP-0035-US-0001-visible-threats:** Позиции, обнаружение и уровень угрозы не должны раскрывать скрытые будущие приказы.
- **EP-0035-US-0002-map-evasion:** G-01: численные пороги и критерий правильного манёвра нужно определить до тикетов; сравнение реальной/предельной скорости нельзя выбирать молча. Отступление из тактического боя не входит.
- **EP-0035-US-0003-world-battle:** Существующий технический abort/выход в меню не должен давать обход последствий; политику прерывания закрепить до тикетов.
- **EP-0035-US-0004-encounter-continuation:** Никакой бесконечной эскалации по дням или репутации.

Общий backlog: пленные, сопровождение, VIP-спавн, полная верфь, пустой старт, фракционная эскалация, бой с базой, большая прокачка, универсальный квестовый движок и тактическое отступление. Тикеты уточняют прежние gaps рабочими A-assumptions; баланс и приёмочные gates остаются открытыми до исполнения.

## Review log

2026-11-16T19:49:59Z — зафиксирован дословный запрос выше (время записи, не время отправки). Уже данное одобрение плана учтено; повторный approval того же объёма не запрашивался. Истории нарезаны как draft-detail под approved план; индивидуальные подтверждения не выдуманы.

- EP-0035-US-0001-visible-threats: авторская проверка ценности, границ, зависимостей и наблюдаемого evidence; отдельное human review — не проводилось.
- EP-0035-US-0002-map-evasion: авторская проверка ценности, границ, зависимостей и наблюдаемого evidence; отдельное human review — не проводилось.
- EP-0035-US-0003-world-battle: авторская проверка ценности, границ, зависимостей и наблюдаемого evidence; отдельное human review — не проводилось.
- EP-0035-US-0004-encounter-continuation: авторская проверка ценности, границ, зависимостей и наблюдаемого evidence; отдельное human review — не проводилось.
- 2026-11-16T20:02:53Z — сверка с сокращённым планом по поручению «на основе этого пройди по созданными эпикам и сторисам … и реши каких сторисов или эпиков нам не хватает. там же добавь недостающие» (время записи): добавлены истории без владельца в плане и обязательная завершающая документационная история; отдельное human review — не проводилось.


## Epic review log

2026-11-16T19:49:59Z — состав сопоставлен с Д5, сокращённым объёмом и существующими EP-0021/0029. Тикеты не созданы. Проверка ID, локальных ссылок и ацикличности выполняется для всего набора; результаты фиксируются в [общем плане](../EP-0031-board-and-demo-scope/DemoPlan.md).

## Статусы поставки

Planning: подготовлено по одобренному плану. Implementation: NOT STARTED. Automated/runtime/native/product acceptance: NOT RUN / OPEN. Никаких новых свидетельств готовности игры этот документ не создаёт.


## Тикетная декомпозиция первого демо

2026-11-16T20:32:46.441Z — «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Workflow approval, без приписывания human review деталей. Подготовлено 21 тикетов; implementation NOT STARTED.

| Story | Тикетов | Текущие prerequisites |
|---|---:|---|
| [EP-0035-US-0001-visible-threats](EP-0035-US-0001-visible-threats/EP-0035-US-0001-visible-threats.md) | 5 | EP-0032-US-0001-campaign-roundtrip, EP-0032-US-0004-demo-campaign-start |
| [EP-0035-US-0002-map-evasion](EP-0035-US-0002-map-evasion/EP-0035-US-0002-map-evasion.md) | 4 | EP-0035-US-0001-visible-threats |
| [EP-0035-US-0003-world-battle](EP-0035-US-0003-world-battle/EP-0035-US-0003-world-battle.md) | 4 | EP-0035-US-0002-map-evasion, EP-0032-US-0003-persistent-outcomes |
| [EP-0035-US-0004-encounter-continuation](EP-0035-US-0004-encounter-continuation/EP-0035-US-0004-encounter-continuation.md) | 2 | EP-0035-US-0003-world-battle |
| [EP-0035-US-0005-route-threat-choice](EP-0035-US-0005-route-threat-choice/EP-0035-US-0005-route-threat-choice.md) | 4 | EP-0035-US-0001-visible-threats, EP-0034-US-0002-passenger-contract |
| [EP-0035-US-0006-documentation](EP-0035-US-0006-documentation/EP-0035-US-0006-documentation.md) | 2 | EP-0035-US-0001-visible-threats, EP-0035-US-0002-map-evasion, EP-0035-US-0003-world-battle, EP-0035-US-0004-encounter-continuation, EP-0035-US-0005-route-threat-choice, EP-0035-US-0017-world-integration |

Порядок ниже — локальная проекция глобального dependency order (не числовая сортировка): EP-0035-US-0001-visible-threats → EP-0035-US-0002-map-evasion → EP-0035-US-0003-world-battle → EP-0035-US-0004-encounter-continuation → EP-0035-US-0005-route-threat-choice → EP-0035-US-0006-documentation. Документационная история исполняется после всех функциональных, включая добавленные с большим ID.

[Глобальная карта, полные пути, release gates и открытые допущения](../EP-0041-demo-validation-and-release/TicketPlan.md). Новые API/файлы в тикетах — проект, а не текущая реализация.

## Grooming 2026-11-17

Время записи 2026-11-17T11:23:54Z, не время сообщения.

> сенсор модуль носителя. у него есть радиус. у игрока радиус должен быть на 20% выше чем у пиратов в начале игры. если нужно добавить тикеты с сенсорами в этот эпик то добавь. Дальше как двигаются пираты? У них какой-то курс. Они стоят на месте? Они в свободном поиске целей? Они пока видны на карте но это только визуализация выключающая туман войны. Туман войны должен быть параметром игры всегда фолс с возможностью изменить через консоль дебага. Добавляй нужные тикеты и отвечай на вопросы - мы продолжаем грумминг

D-35-01 (пользователь): сенсор — установленный модуль носителя с радиусом.
D-35-02 (пользователь): стартовый R_player = 1.20 * R_pirate обоих обычных составов. Это стартовая комплектация, не вечный бонус игроку.
D-35-03 (пользователь): SUPERSEDED D-35-37: normal FogOfWarEnabled=true; explicit owner debug override=false через консоль. Открытая карта — визуализация, не обнаружение и не знание AI.
D-35-04: поручено добавить тикеты; продолжается грумминг, production implementation не запрошен.

A-35-01: пассивный всенаправленный сенсор, обнаружение при R>0 и distance<=R без RNG. Disabled/destroyed/отсутствующий модуль даёт R=0; несколько рабочих модулей дают max. Активное сканирование и новая модель энергопотребления вне scope.
A-35-02 SUPERSEDED D-35-37: normal defaulttrue, explicit debugfalse; lifetime override D-35-40.
A-35-03: fog фильтрует динамические пиратские маркеры, не известные станции/географию. Last-known маркеры пока вне scope.
D-35-05: патруль по точкам принят для демо. Остаток G-35-01: конкретные точки/стоянки и выбор других целей ещё обсуждаются. Потеря контакта/возврат приняты D-35-17; скорости приняты D-35-15/16.
G-35-02 [CLOSED D-35-20]: радиусы 200/240/264 км утверждены. G-35-02M отдельно: физические параметры установки модуля ещё нужно записать перед content implementation; это не неопределённая дальность.

### Grounding

- src/DeepSpaceSaga.Engine/Content/ModuleTypeDefinition.cs:5 — каталог модулей; sensor radius пока отсутствует.
- src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs:922 — ModuleImplementationDto.
- src/DeepSpaceSaga.Client/Data/Ships/starter-carrier.json:80 — установлен Scanner ресурсов; сохранить его.
- src/DeepSpaceSaga.Engine/Content/DemoCampaignDefinitions.cs:23 — DemoPirateGroup с SightRadius/PatrolSpeed; это схема, не подтверждение движения.
- src/DeepSpaceSaga.Engine/Scenario/StarterCarrierManifest.cs:13 — manifest установленных модулей.
- src/DeepSpaceSaga.Engine/SimulationEngine.DemoCampaign.cs:83 — projection и hooks sections.
- src/DeepSpaceSaga.Client/UI/DebugConsole/DebugCommandCatalog.cs:6 — CreateDefault; fog-команды нет.
- src/DeepSpaceSaga.Client/UI/DebugConsole/DebugCommand.cs:21 — IDebugCommand, async session context.
- EP-0030 Documentation.md:16 — поставленная консоль, её реализацию не дублировать.

### Добавленные истории

| Story | Тикетов | Prerequisites |
|---|---:|---|
| [EP-0035-US-0007-carrier-sensors](EP-0035-US-0007-carrier-sensors/EP-0035-US-0007-carrier-sensors.md) | 4 | EP-0032-US-0004-demo-campaign-start |
| [EP-0035-US-0008-fog-of-war-debug](EP-0035-US-0008-fog-of-war-debug/EP-0035-US-0008-fog-of-war-debug.md) | 3 | EP-0035-US-0007-carrier-sensors, EP-0035-US-0001-visible-threats |

Актуальный порядок: US-0007 → US-0001 → (US-0008 и US-0002 → US-0003 → US-0004); US-0005 после US-0001 и EP-0034-US-0002; US-0006 после всех. Старые таблицы выше — исходная декомпозиция, новые prerequisites frontmatter имеют приоритет. Теперь 8 историй / 28 тикетов. Новые истории draft, content draft до технического G-35-02M (радиусы закрыты D-35-20); ранее утверждённые D-решения сохраняются. Реализация не начата, проверки/native/product OPEN.

## Оператор и доклад 2026-11-17

Время записи 2026-11-17T11:37:31Z, не время отправки.

> Ты можешь выбрасывать создание тикетов которые понятны в сабтаски а тут продолжать со мной архитектурный диалог работы над эпиком просто сообщая когда тикеты будут нарезаны? нам нужен биалог окно при обнаружении пиратов через сенсор. оператор член экипажа на сенсоре. возможно его нужно добавить в сценарий и в команду. с сообщением, пеленгом на цель, дальностью, временем подлета и примерной оценкой числа дронов на носителе пиратов
> Ответ - пока по точкам для демо устроит

D-35-05: для демо принят патруль по точкам: «Ответ - пока по точкам для демо устроит». Это не одобрение поиска у последней позиции, автоматического возврата, выбора других целей или бесконечной погони.
D-35-06: при обнаружении пиратов сенсором нужно диалоговое окно с докладом оператора — реального члена экипажа на сенсоре.
D-35-07: доклад содержит сообщение, пеленг, дальность, время подлёта и приблизительную оценку числа дронов пиратского носителя.
D-35-08: понятную нарезку можно выполнять в подзадачах параллельно архитектурному диалогу; авторизация только Board grooming.
D-35-11: назначить существующую Irina Volskaya (CHR-PLAYER-DEFENSE) оператором сенсоров. Пользователь подтвердил имя/назначение. Существующие экипаж, каюты и payroll не дублируются; совмещение/замена остаются G-35-06.
A-35-05: связь sensor moduleId ↔ crewId хранится как явное назначение кампании; role Officer не переименовывается молча в SensorOperator.
G-35-03 CLOSED D-35-29: earliest entry<=20км по observed current vectors; отображение real duration at1×=4*motion seconds, отсутствие пересечения=>нет прогноза.
D-35-09: обнаружение автоматически ставит Speed0; закрытие доклада оставляет паузу, курс задаётся на паузе, движение после явного resume. G-35-04 остаётся только для очередности нескольких контактов и повторного обнаружения.
G-35-05 starter bounds CLOSED D-35-28/31: Irina±1/±2,script±2/±4; один integer estimate, точный тип, hidden classes запрещены. Прогрессия вне demo scope.
G-35-06 CLOSED D-35-41: crew+assignment, без смен/усталости; broken module0.
G-35-01 (остаток): конкретные координаты/dwell и target selection ещё обсуждаются. Waypoint patrol, скорости D-35-15/16 и lost-contact/return D-35-17 приняты.

D-35-09 (новое решение пользователя): sensor acquisition автоматически ставит симуляцию на паузу. После закрытия доклада игра остаётся на Speed0; игрок оценивает ситуацию и задаёт новый курс, движение только после явного resume.
Дословно: «Да, делаем автопаузу при обнаружении. После закрытия диалога игра остается на паузе, чтобы можно было оценить ситуацию и изменить курс. Добавляй это в тикеты.»
G-35-04 теперь открыт только для очередности нескольких контактов и повторного обнаружения; pause/resume больше не открытый вопрос.
Grounding: Dialogue/SimulationEngine.Dialogue.cs:227–229 сейчас восстанавливает ResumeSpeed; SimulationEngine.cs:177 позволяет изменить отложенный resume во время диалога. Для sensor report нужен явный stay-paused policy, не наследование обычного resume.

D-35-10 (новое решение пользователя): «Оператору сенсора нужно дать определенный скилл. В примере единственного сценария демо этот скилл должен давать прибавку еще в 10% от базовой дистанции сенсора».
Проект SensorRangeBonusPercent=10 у стартового оператора; bonus=0.10*baseRadius установленного сенсора, один раз. R_player_module=1.20*R_pirate_module; effective player=1.10*R_player_module=1.32*R_pirate_module. Пиратские operator bonuses не установлены.
A-missing: без действительного назначенного оператора бонус 0, базовый сенсор работает; до решения G-35-06 это assumption. Legacy crew default skill 0.

### Grounding

- src/DeepSpaceSaga.Client/Data/Ships/starter-carrier.json:130 — crew содержит капитана, офицера Irina Volskaya / CHR-PLAYER-DEFENSE и четыре пилота; отдельный новый человек пока не нужен.
- src/DeepSpaceSaga.Client/Scenarios/Demo/scenario.json:6 — текущий Demo ссылается на общий starter-carrier.json; после merge EP-0034 сверить фактический manifest.
- src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:362 — ShipCrewMemberData хранит identity/name/portrait/role, assignment sensor отсутствует.
- src/DeepSpaceSaga.Contracts/CarrierOperations.cs:64 — CrewMemberSnapshot, единая crew identity.
- src/DeepSpaceSaga.Engine/SimulationEngine.Carrier.cs:132 — занятые каюты считаются по реальному crew/passengers.
- src/DeepSpaceSaga.Engine/SimulationEngine.CrewUpkeep.cs:16 — employment создаётся для реального ship.Crew; :40 — food по crew/passengers.
- src/DeepSpaceSaga.Contracts/DialogueState.cs:5 — DialogueState с ParticipantId, speaker, portrait, Parameters.
- src/DeepSpaceSaga.Engine/Dialogue/SimulationEngine.Dialogue.cs:68 — StartDialogue; :77 выбирает StationCrew, :83–84 задаёт speaker, :85–88 ставит modal pause. Для crew speaker нужен явный bounded extension.
- src/DeepSpaceSaga.Engine/Dialogue/DialogueRuntime.cs:25 — DialogueSaveState сохраняет ActiveDialogue и ResumeSpeed.
- src/DeepSpaceSaga.Client/UI/Screens/Dialogue/DialogueScreen.cs:11 — snapshot-only dialogue screen.
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs:2183 — ActiveDialogue открывает диалог; SkiaWindow.cs:1164–1168 уже маршрутизирует экран.
- src/DeepSpaceSaga.Client/UI/Screens/Ship/ShipViewModel.cs:28 — CrewRow показывает Id/name/role, assignment пока отсутствует.

### Текущая карта дополнения

| Story | Тикетов | Prerequisites |
|---|---:|---|
| [EP-0035-US-0009-sensor-operator](EP-0035-US-0009-sensor-operator/EP-0035-US-0009-sensor-operator.md) | 5 | US-0007, EP-0033-US-0004 |
| [EP-0035-US-0010-sensor-contact-dialogue](EP-0035-US-0010-sensor-contact-dialogue/EP-0035-US-0010-sensor-contact-dialogue.md) | 5 | US-0001, US-0009 |

Теперь 10 историй / 38 тикетов. Текущий порядок: US-0007 → US-0001; US-0009 после US-0007 и EP-0033-US-0004; US-0010 после US-0001/0009; US-0008 после US-0001/0007; US-0002→0003→0004 после US-0001; US-0005 после US-0001 и EP-0034-US-0002; US-0006 последняя. Патруль по точкам уточняет существующие US-0001 tickets, не создаёт дубликат. Новые 10 тикетов draft из-за применимых G; готовность нарезки не равна готовности реализации.

## Сигнатуры и скорости — уточнение 2026-11-17

> Да, назначаем Ирину оператором сенсоров. Добавляй в тикеты. Давай теперь обсудим, как навык оператора влияет на точность оценки количества дронов у пиратов. Число дронов должно оцениваться по сигнатуре корабля пиратов. пока пусть будет два вида. Логово как сейчас и Танкред на 4 дрона больше. Тяжелые-средние дроны должны генерироваться случайно при обнаружении игроком пирата. показываться в отчете оператора сенсора не должны - только общее число и тип пиратского корабля

> я просто не помню сколько в Логово. возьми это число и сделай у Танкреда на 4 больше. Теперь давай по скорости. скорость танкреда должна быть на 20% меньше чем у игрока а у Логова на 10% больше

D-35-11: «Да, назначаем Ирину оператором сенсоров. Добавляй в тикеты». Существующая Irina Volskaya / CHR-PLAYER-DEFENSE теперь прямое решение, не A.
D-35-12: число дронов оценивается по сигнатуре пиратского корабля. Для демо два вида: Логово как сейчас и Танкред на 4 дрона больше.
D-35-13: тяжёлые/средние дроны генерируются случайно при обнаружении игроком пирата. В докладе показывать только общее число (оценку) и тип пиратского корабля, не классы/состав дронов.
Grounding count: Data/TacticalBattle/scenarios.json:69–132 — initial.side B carrier.id «Логово», шесть drones Таг-1…Таг-6. Поэтому стартовый total профиля Логово=6, Танкред=10. «Логово» сейчас id конкретного экземпляра, отдельный signature type пока отсутствует.
A-35-06: создавать состав один раз на первом фактическом sensor acquisition, сохранять и не reroll на reacquisition/Load; это рекомендованная защита от повторов, не дословное дополнительное решение пользователя.
G-35-07 weights CLOSED D-35-27: independent50/50 per slot без caps/forced mix. Equipment/layout pool CLOSED D-35-39; existing light/heavy frames по D-35-38, G-35-08 CLOSED.
G-35-08 CLOSED D-35-38: existing light/heavy frames; новый body class не нужен.
G-35-05 starter bounds CLOSED D-35-28/31; +10% range не accuracy%. Distribution/stability остаются A, future progression отложена.
G-35-09: смысл total при частичных потерях/изменении состава и перехват до первого player detection ещё требует явной политики; не менять легитимный existing loadout молча.

D-35-14: фактическое total Логова = текущие 6, Танкред = 6+4 = 10; это фиксированное начальное число, не переменная загрузка/вместимость. Случайны light/heavy классы, не total.
D-35-15: «скорость танкреда должна быть на 20% меньше чем у игрока а у Логова на 10% больше».
D-35-16 (пользователь подтвердил): опорная скорость — operational максимальная скорость фактического стартового носителя игрока, захваченная один раз при Demo New Game; V_tancred=0.80*V_ref, V_lair=1.10*V_ref. Не мгновенная текущая скорость и не адаптивный scaling каждый tick; при торможении/повороте игрока пираты не меняют Vmax. Ускорение и поворот остаются в motion model.
Grounding: текущий Demo scenario.json:6 ссылается на starter-carrier.json, :100–106 устанавливает module.engine.basic; Data/Modules/Engine/modules-engine.json:10 даёт maxSpeedMps=6000. SolarSystemGenerator.OperationalMaxSpeedKmS:13 и SimulationEngine.GetMaxSpeedKmS:3387 вычисляют operational Vmax. На текущем старте пример 6/4.8/6.6 км/с, но не hardcode: после EP-0034 сверить фактический Demo manifest.

Добавлена [EP-0035-US-0011-pirate-signatures](EP-0035-US-0011-pirate-signatures/EP-0035-US-0011-pirate-signatures.md), 5 draft tickets. Всего EP-0035: **11 историй, 43 тикета**. US-0011 после US-0001/0009, US-0010 после US-0011, US-0003 battle integration использует US-0011 generation; US-0006 последняя после всех. Принятые D реализуются после закрытия owning G, без нового production в этом grooming.

### D-35-16 — подтверждение опорной скорости

> Да, скорости фиксируем относительно максимальной скорости стартового носителя. Добавляй в тикеты. Давай теперь обсудим, как пираты преследуют игрока и что делают, когда теряют его из радиуса сенсоров

Frozen baseline максимальной скорости стартового носителя теперь D, не A. Танкред 0.80, Логово 1.10; последующее торможение/апгрейд игрока baseline не меняют. Уточнено позднее D-35-17: last observed position → return patrol принят, без поискового круга. Живой lead-intercept ещё требует уточнения; capture radius закрыт D-35-19 (20 км). Cooldown immunity не вводится.

## Историческая карта до эскадр — 11 историй / 43 тикета

Эта карта заменяет старые таблицы состава выше; история решений и исходные quantities сохранены. Порядок ниже topological по story prerequisites; внутри US-0009 TK-0005 идёт после TK-0002 и до TK-0003.

| Story | Tickets | Prerequisites |
|---|---:|---|
| [EP-0035-US-0007-carrier-sensors](EP-0035-US-0007-carrier-sensors/EP-0035-US-0007-carrier-sensors.md) | 4 | EP-0032-US-0004-demo-campaign-start |
| [EP-0035-US-0001-visible-threats](EP-0035-US-0001-visible-threats/EP-0035-US-0001-visible-threats.md) | 5 | EP-0032-US-0001-campaign-roundtrip, EP-0032-US-0004-demo-campaign-start, EP-0035-US-0007-carrier-sensors |
| [EP-0035-US-0009-sensor-operator](EP-0035-US-0009-sensor-operator/EP-0035-US-0009-sensor-operator.md) | 5 | EP-0035-US-0007-carrier-sensors, EP-0033-US-0004-pilot-recruitment |
| [EP-0035-US-0011-pirate-signatures](EP-0035-US-0011-pirate-signatures/EP-0035-US-0011-pirate-signatures.md) | 5 | EP-0035-US-0001-visible-threats, EP-0035-US-0009-sensor-operator |
| [EP-0035-US-0010-sensor-contact-dialogue](EP-0035-US-0010-sensor-contact-dialogue/EP-0035-US-0010-sensor-contact-dialogue.md) | 5 | EP-0035-US-0001-visible-threats, EP-0035-US-0009-sensor-operator, EP-0035-US-0011-pirate-signatures |
| [EP-0035-US-0008-fog-of-war-debug](EP-0035-US-0008-fog-of-war-debug/EP-0035-US-0008-fog-of-war-debug.md) | 3 | EP-0035-US-0007-carrier-sensors, EP-0035-US-0001-visible-threats |
| [EP-0035-US-0002-map-evasion](EP-0035-US-0002-map-evasion/EP-0035-US-0002-map-evasion.md) | 4 | EP-0035-US-0001-visible-threats |
| [EP-0035-US-0003-world-battle](EP-0035-US-0003-world-battle/EP-0035-US-0003-world-battle.md) | 4 | EP-0035-US-0002-map-evasion, EP-0032-US-0003-persistent-outcomes, EP-0035-US-0011-pirate-signatures |
| [EP-0035-US-0004-encounter-continuation](EP-0035-US-0004-encounter-continuation/EP-0035-US-0004-encounter-continuation.md) | 2 | EP-0035-US-0003-world-battle |
| [EP-0035-US-0005-route-threat-choice](EP-0035-US-0005-route-threat-choice/EP-0035-US-0005-route-threat-choice.md) | 4 | EP-0035-US-0001-visible-threats, EP-0034-US-0002-passenger-contract |
| [EP-0035-US-0006-documentation](EP-0035-US-0006-documentation/EP-0035-US-0006-documentation.md) | 2 | EP-0035-US-0001-visible-threats, EP-0035-US-0002-map-evasion, EP-0035-US-0003-world-battle, EP-0035-US-0004-encounter-continuation, EP-0035-US-0005-route-threat-choice, EP-0035-US-0007-carrier-sensors, EP-0035-US-0008-fog-of-war-debug, EP-0035-US-0009-sensor-operator, EP-0035-US-0010-sensor-contact-dialogue, EP-0035-US-0011-pirate-signatures, EP-0035-US-0017-world-integration |

## Потеря контакта — D-35-17

D-35-17 (пользователь): «Для демо пусть долетают до последней известной позиции и, если не нашли игрока, возвращаются на патруль. Добавляй в тикеты».
После потери собственного сенсорного контакта пират идёт к последней действительно наблюдённой позиции игрока. Сенсор продолжает работать по пути. При достижении этой точки без нового контакта группа возвращается к патрулю; поискового круга, прохода или дополнительного таймера поиска нет. Текущие скрытые координаты/маршрут игрока и debug visibility не используются.
A-35-08: если сенсор вновь обнаружил игрока по пути или на возврате, группа снова переходит в Pursuing; это логическое рабочее допущение, не отдельная дословная команда пользователя.
A-35-09: возвращаться к сохранённой следующей точке прерванного патруля; конкретный return waypoint policy не был отдельно одобрен.
G-35-10 [CLOSED D-35-19]: capture radius = 20 км = 200 world units, одинаковый обоим, distance <= radius между центрами.
Supersession: прежние A-01 escape radius/escapeDuration/max pursuit duration больше не завершают эту погоню. Escaped — наблюдаемый outcome после достижения lastKnown без повторного контакта, а не конкурирующая AI state machine. Старые cooldown параметры не дают иммунитет от нового настоящего контакта; duplicate events предотвращаются stable contact/encounter identity.
Уточнены owning US-0002/0004 и content US-0001, новые тикеты не требуются. Состав остаётся 11 историй / 43 тикета. Dependency order без изменений.

## Дистанция перехвата — калибровка 100 px

D-35-18 (пользователь): «давай возьмем 100 пикселей на самом приближенном масштабе. сколько это будет? Одинаковая дистанция да».
Оба пиратских типа используют одинаковый capture radius в физических world units. Пиксели — запрос калибровки фиксированного радиуса, не runtime условие. Дистанция боя не зависит от текущего zoom, размера окна, DPI/framebuffer scale или типа пиратского носителя.
A-35-10 [SUPERSEDED D-35-19]: первоначально предлагалось 10 км = 100 world units по базовым 100 логическим px. Пользователь позднее выбрал 20 км = 200 world units; 10 км больше не active baseline.
G-35-10 [CLOSED D-35-19]: утверждены одинаковые 20 км = 200 world units. Историческая калибровка 100 px не используется runtime; dynamic max zoom не меняет physical radius.
Grounding: src/DeepSpaceSaga.Client/Settings.json:30–31 — metersPerPixel[0]=100; src/DeepSpaceSaga.Client/UI/MapScalePolicy.cs:14–20 — max(settings.MaximumPpu, shortSide, diagonal); src/DeepSpaceSaga.Client/UI/SkiaWindow.cs:365–378 — framebuffer/window scale и RenderScreenStack с windowSize, то есть логические UI pixels. EngineRequirements: 1 world unit=100 м.
D-35-19: captureRadiusWorldUnits=200 в content; boundary tests проверяют <200, =200, >200 для обоих типов. Число 100 world units из старого A не использовать.
Уточнены существующие тикеты; состав остаётся 11 историй / 43 тикета. Capture radius утверждён D-35-19: 20 км = 200 world units.

## D-35-19 — окончательная дистанция перехвата

D-35-19 (пользователь): «давай 20 километров что у нас еще не понятно для эпика?».
Одинаковая физическая дистанция перехвата для Логова и Танкреда — ровно 20 км = 200 world units между центрами носителей. Условие distance <= 200 world units включает границу; при distance > 200 перехвата по дистанции нет. Runtime не зависит от zoom, window size, DPI или screen pixels.
G-35-10 закрыт. A-35-10 с предложением 10 км superseded этим решением; история пиксельной калибровки сохранена только как история, не конфигурация.
Content: captureRadiusWorldUnits=200, один общий параметр обоих типов. Engine использует authoritative centre-to-centre world geometry; Client получает подтверждённый результат. Не создавать два радиуса/ветки по типу.
Состав остаётся 11 историй / 43 тикета; новых тикетов не требуется.

## Исторические вопросы после D-35-19 (закрыты последующими D)

Эта сводка актуальнее прежних G-списков; принятые патруль по точкам, lastKnown→return, Ирина, +20%/+10%, fixed totals 6/10, frozen speed ratios, auto-pause/stay-paused и capture 20 км повторно не согласовываются.

1. **Дальность сенсора — закрыто D-35-20.** 200 км у пиратов, 240 км база игрока, 264 км с Ириной. G-35-02M — только технический baseline установки (масса/footprint/питание), не отдельный обязательный продуктовый вопрос при отсутствии влияния на продукт.
2. **Закрыто: starter accuracy D-35-28/31.** Irina±1/±2,script±2/±4, один integer estimate; future skill progression отложена.
3. **Пул оборудования CLOSED D-35-39.** Four existing layouts по25%, existing light/heavy frames.
4. **Закрыто: ETA D-35-29.** Earliest entry20км по observed vectors, реальные секунды при1×.
5. **Повторные и одновременные контакты (остаток G-35-04).** Когда повторно показывать доклад после потери/возврата, как ставить несколько докладов в очередь. Автопауза и stay-paused уже D.
6. **Остаток G-35-06: критерии unavailable/назначения/module gate.** Script192км,±2/±4 уже D-35-31; прежний fallback base240км без бонуса superseded.
7. **Крайние случаи materialization/перехвата (G-35-09).** Что делать, если перехват происходит до первого player sensor detection или первый контакт уже внутри 20 км; порядок pause/report/battle и материализации. Partial-loss continuation/legacy loadout не менять молча.

Дополнительные A, которые пока не требуют отдельной большой истории: no-reroll на reacquisition/Load (A-35-06), reacquisition→Pursuing (A-35-08), возврат к сохранённой следующей точке патруля (A-35-09), passive omnidirectional sensor (A-35-01), reset fog на Load (A-35-02). Lead-intercept по живому контакту и выбор целей кроме игрока не утверждены; расширять AI на другие цели без решения нельзя. Конкретные координаты waypoint и файловый mapping после merge — routine implementation, не самостоятельные пользовательские решения.

## D-35-20 — утверждённые радиусы сенсоров

D-35-20 (пользователь): «Давай 200 км у пиратов, 240 у игрока и 264 с Ириной. Добавляй в тикеты. Теперь давай разберем повторные обнаружения: не хочется, чтобы диалог постоянно ставил игру на паузу».
Точные стартовые радиусы демо: оба пиратских сенсора 200 км = 2000 world units; базовый модуль игрока 240 км = 2400 world units; effective radius с назначенной Ириной 264 км = 2640 world units. Формула ровно 2400 + 0.10*2400, один раз. Пиратские демо-контакты используют 2000 world units, без неоговорённой надбавки.
Численная дальность G-35-02 закрыта. Отдельный G-35-02M — технические параметры установки модуля (масса, footprint, прочность, питание), которые надо записать перед content implementation; это не неизвестный радиус и не повторный вопрос о дальности.
Capture radius D-35-19 остаётся 20 км = 200 world units. Для sensor detection граница distance<=effective radius; для capture <=200. Показ debug-маркера вне радиуса не вызывает sensor acquisition.
Повторные/одновременные доклады остаются G-35-04. Желание избежать постоянных forced pauses записано; политика «один modal на group/кампанию», batch и manual report ещё не утверждены и не вводятся этим D.
Новых тикетов нет: EP-0035 остаётся 11 историй / 43 тикета.

## D-35-21: один автоматический доклад на группу за кампанию
Пользователь: «Да, один автоматический доклад на группу за кампанию».
Сохранить consumedAutomaticReport по campaignId/stable groupId. Только первое реальное sensor acquisition создаёт modal и atomic Speed0; dismissal сохраняет pause. Recontact, повтор snapshot, Save/Load, debug fog reveal не открывают modal и не меняют speed. Эскадра — одна группа, не два независимых автоматических доклада. Flag и report creation сохраняются атомарно, не терять pending report при Load.
G-35-04 CLOSED для once/group и batching D-35-33; только точный UX-order/manual reopen — A. Немодальное уведомление при recontact тоже не обязательное D.
Tests NOT RUN: FirstGroupContact_PausesOnce; Recontact_NoModalNoPause; SaveLoad_ConsumedPersists; TwoSquadMembers_OneGroupReport; FogReveal_NoDiscovery.
## Текущее расширение эскадр — D-35-21…26
D-35-22: одна фишка группы, Vmax=min operational Vmax её носителей.
D-35-23: все носители и дроны эскадры одновременно на одном поле.
D-35-24: шанс эскадры10%, меняется debug console. Предлагаемый синтаксис pirate_squad_chance [0..100], без аргумента current; help/валидация обязательны.
D-35-25 supersedes раннее2–3/mixed DEMO: «так по поводу боя. пока эскадры только два логова и оба выводим в бой». Ровно два Логова, по6 дронов =12, плюс носитель игрока (всего3 carriers на поле). Танкред остаётся одиночным типом. Без повреждений squad Vmax=1.10 frozen start player maximum, operational min сохраняется.
D-35-26: «при уничтожении всех носителей дроны получают статус выведены из строя как при стрельбе». Гибель первого из двух не отключает дроны. После последнего носителя стороны все её surviving drones становятся существующим Disabled, speed0; Destroyed остаётся Destroyed, отключение не означает смерть пилота. Симметрично для player и pirate side. Atomic/idempotent transition, затем outcome и skip последующих shots по existing invariants.
Grounding: TacticalBattleState.cs:9 TacticalDroneStatus Air/Disabled/Destroyed; TacticalBattleSalvo.cs:237–239 shot disable задаёт Disabled,Speed0,statistics; :152–153 skip disabled/destroyed shooter.
A-35-11: независимый seeded draw на каждое создание новой группы,90% singleton/10% squad; chance действует только future creation, не sensor/load; RNG/result сохраняются без reroll.
G-35-12 CLOSED D-35-32: одна точка группы, max operational radius без сложения; report layout — A.
G-35-13 lifetime CLOSED D-35-40; ordinary group count4 CLOSED D-35-36; singleton weights CLOSED D-35-35; initial/norespawn CLOSED D-35-34.
G-35-14 CLOSED D-35-26; orphan policy больше не открыта.
G-35-15 normal retreat CLOSED/excluded D-35-46: exit onlywin/loss; crash recovery CLOSED D-35-47.
Ранние2–3/mixed варианты, выбор2vs3 и orphan proposals — superseded; не scope демо и не текущие G.
Текущий объём EP-0035: **17 историй / 66 тикетов**. Добавлены US0012 (5),US0013 (4),US0014 (3),US0015 (4). Исторические11/43 выше сохранены как журнал; текущая карта supersedes. US0012 после US1/11; US13 после US12/3; US14 после US13; US15 после US14/4; US6 documentation последняя. Board only, implementation NOT STARTED.
## Историческая карта расширения US0012–0015
US0001–0011 сохраняются; следующие истории добавлены к их prerequisite behavior.
| Story | Tickets | Result |
|---|---|---|
| [EP-0035-US-0012-pirate-squadrons](EP-0035-US-0012-pirate-squadrons/EP-0035-US-0012-pirate-squadrons.md) | 5 | Эскадра из двух Логов на карте и вероятность появления |
| [EP-0035-US-0013-squad-battle-assembly](EP-0035-US-0013-squad-battle-assembly/EP-0035-US-0013-squad-battle-assembly.md) | 4 | Два пиратских носителя и их дроны в одном бою |
| [EP-0035-US-0014-squad-battle-rules](EP-0035-US-0014-squad-battle-rules/EP-0035-US-0014-squad-battle-rules.md) | 3 | Боевые правила для двух Логов |
| [EP-0035-US-0015-squad-battle-continuation](EP-0035-US-0015-squad-battle-continuation/EP-0035-US-0015-squad-battle-continuation.md) | 4 | Интерфейс и возврат результатов боя эскадры |
## D-35-27: независимые классы дронов 50/50
Пользователь: «Давай пока независимые 50/50 без ограничений. Добавляй в тикеты».
Для КАЖДОГО drone slot отдельный независимый seeded Bernoulli: light=0.5, heavy=0.5. Нет cap, минимумов или гарантии смешанного крыла; полностью light и полностью heavy допустимы. Actual totals не случайны: Логово6,Танкред10,эскадра2xЛогово12 (6 у каждого stable carrierId).
G-35-07 CLOSED в части class weights/ограничений. Equipment/layout pool CLOSED D-35-39 (четыре exact layouts по25%); existing light frame используется по D-35-38; G-35-08 CLOSED. Эти параметры не утверждены вероятностью50/50. Starter bounds закрыты D-35-28; progression/другие уровни — остаток G-35-05.
A-35-06 сохраняется как рекомендация: материализация один раз на реальном player sensor acquisition, сохранение результата/no reroll при recontact/Load. D о50/50 не переименовывает эту A в пользовательское одобрение persistence policy. Использовать именованный seeded composition RNG stream кампании, не UI/global RNG; каждый слот получает draw и сохраняет origin carrier identity.
Content: явно задать0.5/0.5 weights и отсутствие forced-mix/cap; schema validate finite normalized weights. Runtime не использовать общий draw на корабль/эскадру.
Tests NOT RUN: FixedSeed_ReproducesSlotClasses; EachSlot_ConsumesIndependentDraw; AllLight_Allowed; AllHeavy_Allowed; Totals6_10_AndTwoTimes6Preserved; SquadDrones_KeepCarrierOwnership; SaveLoad_NoCompositionReroll (A-35-06). Проверки deterministic seeds/controlled RNG, без flaky статистических частот.
## D-35-28: точность стартового доклада Ирины
Пользователь: «Да, принимаем ±1 дрон на носитель и ±2 для эскадры. Добавляй в тикеты».
Стартовый sensor skill Ирины даёт одно целое приблизительное reported total: ошибка относительно actual total не более1 для одиночного носителя, не более2 для demo squad2xЛогово. В докладе «около N», точный тип носителя / типы обоих Логов; light/heavy классы и equipment скрыты. Не показывать симметричный диапазон вокруг actual total, из которого восстанавливается скрытый true count.
Допустимые значения: одиночное Логово5..7 при actual6; Танкред9..11 при actual10; эскадра10..14 при actual12. Это границы допустимого одного числа, НЕ UI-диапазон и НЕ случайное actual число дронов.
G-35-05 CLOSED для starter Irina accuracy bounds и формата одного приблизительного числа. Открытый остаток: прогрессия навыка/другие уровни/другие операторы; distribution ошибки не утверждён пользователем. A-35-12: равномерная целочисленная ошибка в допустимых границах, независимый named estimate RNG stream; если принята при реализации, явно сохранить как assumption. Стабильная сохранённая оценка/no reroll — рекомендованная A, не новое пользовательское D.
+10% дальности от module base — отдельный эффект навыка; не трактовать как10% точности. Module радиусы200/240/264км и capture20км без изменений. Squad report один на группу, approximate aggregate total±2, не два самостоятельных forced reports.
G-35-03 CLOSED D-35-29: до20км по текущим observed vectors, real duration при1×; историческое обсуждение цели/часов superseded.
Tests NOT RUN: IrinaSingleEstimate_WithinOne_Integer; IrinaSquadEstimate_WithinTwo_Integer; Report_OneApproximateNumberAndExactTypes; Report_NoClassOrHiddenLoadout; AccuracyDoesNotAlterActual6_10_12; RangeBonusIsNotAccuracyPercent; StableEstimateOnLoad (A). Проверять разрешённый observation DTO, не переносить private composition в client.
## D-35-29: ETA до20км по текущим курсам при1×
Пользователь: «Да, принимаем до 20 км по текущим курсам и время при 1×».
ETA — earliest nonnegative время входа в distance<=20км при постоянных НАБЛЮДАЕМЫХ текущих world velocities двух объектов. r=observed target position-player position; v=observed target velocity-player velocity; найти минимальный t>=0 с |r+v*t|<=200world units, motion units согласовать. Не использовать будущие waypoint/orders, скрытый курс, мгновенный поворот/ускорение pursuit. Поле evaluation timestamp привязано к наблюдению, UI не пересчитывает скрытые данные.
Уже внутри/на20км =>0 («на дистанции боя»). Нет пересечения/расхождение => «при текущих курсах сближения до дистанции боя нет», не infinity/NaN/выдуманное время. Zero relative speed снаружи=>нет прогноза; tangent включается; numerical near-zero/discriminant обработать устойчиво, результат finite/nonnegative.
Показывать ожидаемое реальное время ожидания при игре1×, явно пометить «при1×». Grounding EngineRequirements.md:21–31: на1× одна motion second проходит за4 real seconds; поэтому display duration=4*t_motion. Calendar factor300 здесь НЕ используется. Current pause/speed не меняет этот прогноз; pause не создаёт division by zero.
G-35-03 CLOSED: цель, формула и шкала согласованы. US0010 contracts фиксирует nullable forecast/reason/observation timestamp, telemetry считает authoritative value, dialogue content/client отображает готовое значение и единицы.
Tests NOT RUN: Closing_EntryTo20km; Tangent_Included; AlreadyInside_Zero; Diverging_NoForecast; ZeroRelativeVelocity_InsideOutside; NearZero_Finite; FirstEntry_NotClosestApproachTime; MotionSecond_DisplaysFourRealSecondsAt1x; PauseAndCurrentSpeed_DoNotChange1xForecast; FutureOrdersNotRead.
## D-35-30: скрипт вместо недоступной Ирины
Пользователь: «Вместо нее работает скрипт с 80% эффективностью».
D: если Ирина недоступна, работу выполняет скрипт с эффективностью80%. D-35-31 утвердил192км и±2/±4; G-35-06 остаётся только для unavailable/замены/module gate.
A-35-13 SUPERSEDED D-35-31: fallback192км=0.8*240км base, а не0.8*264км.
A-35-14: unavailable=отсутствует/не назначена/недееспособна по существующим crew состояниям; конкретную проверку согласовать с реальными supported statuses, не создавать фиктивный health API. Неработающий sensor module отдельно: скрипт не ремонтирует и не обходит physical module gate.
Нет фиктивной реплики/портрета Ирины: системный sender для script. A-35-15: сохраняются один automatic report/group и pause/dismissal policy; fallback/replacement не сбрасывает discovery/consumed flag. Способ presentation пока предложен как A.
G-35-06F CLOSED D-35-31: script±2 singleton/±4 squad; Irina±1/±2 сохраняется.
## D-35-31: численные параметры скрипта
Пользователь: «Да, принимаем для скрипта 192 км и погрешность ±2/±4».
Fallback radius=192км=1920world units=0.8*240км BASE установленного стартового player sensor; не0.8*264км с бонусом Ирины. A-35-13 pending superseded этим D.
Script report содержит одно целое «около N»: actual total±2 для singleton, actual aggregate total±4 для squad2xЛогово. Допустимые single values Логово4..8,Танкред8..12; squad8..16. Это НЕ отображаемые диапазоны и не изменение actual total6/10/12. Точные типы раскрываются, light/heavy/equipment остаются скрыты.
G-35-06F numerical accuracy CLOSED; G-35-06 radius base CLOSED. Starter Irina264км и±1/±2 сохраняются. ETA D-35-29 вычисляется по observed vectors до20км и отображается при1×;80% не искажает пеленг/дальность/ETA автоматически.
A-35-14 criteria unavailable/module gate и A-35-15 system speaker/once-report pause продолжают быть A: пользователь сейчас подтвердил только radius/error. Script не изображается Ириной; конкретная недоступность/замена и physical module behavior требуют согласования существующих crew/module states.
Distribution ошибки и stable sampled estimate/no reroll — прежние A, не новые D.
Tests NOT RUN: FallbackRadius1920WorldUnits; FallbackDoesNotMultiplyIrina264; SingleErrorWithinTwo; SquadErrorWithinFour; EstimateIntegerOnlyAndNoHiddenClasses; FallbackDoesNotDistortEta; SwitchingOperator_DoesNotChangeActualLoadout.
## Исторические вопросы после D-35-31 (закрыты последующими D)
Эта сводка заменяет исторические открытые формулировки выше.
- G-35-07 CLOSED D-35-39: exact four existing layouts по25%; G-35-08 CLOSED D-35-38 existing light/heavy.
- G-35-12 CLOSED D-35-32: одна group point для distance/capture/ETA, max operational sensor; two-type report layout остаётся A, не blocker геометрии.
- G-35-13 lifetime CLOSED D-35-40; ordinary group count4 CLOSED D-35-36; singleton weights CLOSED D-35-35; начальное создание/no respawn CLOSED D-35-34. Named captain deferred EP39, вне EP35 grooming и не blocker.
- G-35-04 batching CLOSED D-35-33; manual reopen/recontact notice отложены. Один automatic report/group сохраняется в одной общей сводке шага.
- G-35-06 CLOSED D-35-41: active crew+assignment; absent/unassigned=>script; broken module0.
- G-35-15: продолжение surviving world group при допустимом частичном/timeout/досрочном завершении; не вводить новую механику тактического отступления, она внеscope. Last carrier death=>Disabled уже D.
Технические значения mass/power/footprint модуля G-35-02M и waypoint coordinates не требуют отдельного продуктового вопроса без конкретного tradeoff. Skill progression beyond demo и third/mixed squad — отложенные расширения. Starter accuracy, ETA, radii, capture distance, orphan policy закрыты.
## D-35-32: общая точка и лучший действующий сенсор эскадры
Пользователь: «Да, принимаем одну общую точку эскадры и лучший действующий сенсор без сложения радиусов».
Одна authoritative group position — центр map token. Её используют world range, bearing, ETA по D-35-29 и capture<=20км по D-35-19; tactical позиции отдельных carrier не становятся отдельными map contacts. Все расчёты в world units, zoom/DPI/window не меняют distance.
Squad sensor range=max operational radius её carriers, НЕ сумма. Два исправных Логова=>200км, один неработающий sensor=>200км от другого; оба неработающие=>0. Нулевая дальность означает отсутствие detection coverage, а не detection при coincident point. Не суммировать площадь/радиусы и не создавать дополнительный sensor origin для tactical member positions.
G-35-12 geometry/reference/sensor aggregation CLOSED. A-report-shape: один отчёт группы с двумя точными типами и одним approximate aggregate total; два automatic окна по членам недопустимы вследствие D-35-21 one/group. UI layout списка членов — implementation choice.
G-35-04 simultaneous batching CLOSED D-35-33: одна сводка всех новых групп текущего observation step; сортировка/selection/scroll — A.
Tests NOT RUN: TwoLairs_Max200NotSum400; OneSensorDisabled_UsesOther; AllSensorsDisabled_NoCoverage; GroupReference_SharedByRangeBearingEtaCapture; MemberTacticalPositions_NotMapContacts; CaptureAndSensorIndependentOfZoom.
## D-35-33: одна сводка новых групп за один шаг
Пользователь: «Да, одна сводка для всех новых групп за один шаг симуляции».
В одном authoritative observation/simulation step собрать ВСЕ первые реальные sensor acquisitions ещё не reported stable groups, создать один batch report и одну autopause. Не останавливать обработку шага после первой группы; не продвигать дополнительные ticks ради ожидания новых контактов. Squad2xЛогово — одна группа. Debug visibility и уже consumed groups не входят в batch.
Persist batchId/content/pending presentation и consumed flags всех включённых groupIds атомарно: Save/Load не теряет записи и не создаёт повторный modal. Pending batch после Load доступен ровно один раз; flags не должны молча поглотить ещё не показанный доклад. Закрытие сводки сохраняет Speed0; новые контакты другого шага после explicit resume образуют новую сводку только для новых groups.
G-35-04 simultaneous batching CLOSED. Детерминированный порядок обязателен для replay; A-order: ETA затем range затем stable groupId; конкретный сортировочный UX/selection/scroll не является отдельным D. Manual reopening/recontact notice остаются необязательными A/отложенными, не повод повторять automatic modal.
US0010 contracts: collection group reports+batch identity; telemetry: collect/commit once after full current observation step; dialogue: один batch speaker/report и stay-paused; content/client: список новых групп без раскрытия hidden composition. Owning save хранит pending batch целиком.
Tests NOT RUN: TwoNewGroupsOneStep_OneSummaryOnePause; SquadIsOneGroup; ConsumedGroupExcluded; DifferentResumedSteps_SeparateNewSummaries; PendingBatchLoad_NoLossNoDuplicate; ClosingBatch_StaysPaused; NoExtraTicksForBatch; FogReveal_NoBatch.
## D-35-34: все группы на старте, без возрождения
Пользователь: «Да, все группы создаются на старте кампании, без возрождения в демо».
Ordinary pirate world groups материализуются seeded при New Game: stable group/member IDs, типы, position и patrol route существуют и движутся до player observation. Sensor detection не создаёт world group; он отдельно материализует скрытую light/heavy composition по D-35-13/27 и формирует доклад. Не путать эти моменты RNG.
В демо нет respawn. Save/Load/revisit/уничтожение группы не создают новые группы/копии. Initial group draw использует squad chance10% (или допустимый override при старте); уже созданные группы не меняются после console command. G-35-13 initial cadence CLOSED; lifetime/pre-NewGame применения debug override остаётся открытым. Conditional singleton50/50 утверждено D-35-35; default45/45/10.
Region/safe routes исходного US1 — A-baseline. «Два обычных состава» НЕ количество world groups: ordinary group count теперь4 по D-35-36. Проверить safe start station separation/route bypass. Position/route не скрытое знание player или pirate AI; fog off не acquisition.
Deferred EP39 integration note (не blocker EP35): EP39-US0003-TK0003:52 планирует story-gated captain; пользователь вынес сюжет из текущего грумминга. EP39 не изменён; варианты появления не утверждены.
Tests NOT RUN: NewCampaign_AllOrdinaryGroupsExistBeforeObservation; UndetectedGroups_MoveOnPatrol; FirstAcquisitionDoesNotSpawnWorldGroup; FirstAcquisition_MaterializesOnlyHiddenComposition; DestroyedGroup_NoRespawn; LoadRevisit_NoDuplicates; ConsoleChange_DoesNotMutateExistingGroups; SameCampaignSeed_ReproducesInitialGroups.
## D-35-35: распределение45/45/10
Пользователь: «Да, принимаем45/45/10».
Default initial world group draw: singleton Логово45%, singleton Танкред45%, squad2xЛогово10%. При изменении squadChance=p условный выбор singleton остаётся50/50, поэтому вероятности (1-p)/2,(1-p)/2,p. Речь о независимом seeded выборе для каждой группы при старте кампании, не о точной квоте групп.
p=0 =>50/50/0; p=1 =>0/0/100%. Не повторять draw на sensor acquisition, Load/revisit. Сохранять stable chosen group/member identities; separate hidden light/heavy slot draws50/50 при первом acquisition не меняют carrier type.
G-35-13 singleton weights CLOSED. Lifetime console override CLOSED D-35-40; ordinary world group count4 утверждён D-35-36. Фраза старого плана «два обычных состава» означает два типа/набора, не одобренное число spawn groups; не подставлять автоматически две группы.
Tests NOT RUN: Defaults45_45_10; ConditionalSingletonHalfForChangedP; ChanceZero_NoSquads; ChanceHundred_OnlyTwoLairSquads; FixedSeed_KnownChoiceSequence; LoadRevisit_NoTypeReroll; IndependentGroups_NotExactQuota. Без flaky frequency tests.
## D-35-36: четыре ordinary группы на демо-кластер
Пользователь: «Да, берём четыре обычные группы на демо-кластер. Добавляй в тикеты».
Создать ровно4 ordinary world groups при старте кампании. Каждая независимо выбирается по D-35-35 (45/45/10 default); squad2xЛогово считается ОДНОЙ группой, а не двумя. Поэтому четыре группы не означает четыре carrier и не задаёт квоту типов. No respawn, Load/revisit не увеличивают число initial instances.
G-35-13 total ordinary group count CLOSED. A-35-16: named finale captain — отдельная сюжетная встреча EP39, не входит в4 ordinary; его initial existence/story activation deferred EP39, вне EP35 grooming; не утверждать пятую группу или способ появления.
Количество станций/география и ограничение выбора маршрутов пользователем пока обсуждаются; нового D о сокращении станций нет. Safe initial locations/не на стартовой станции/доступный обход остаются прежними A до конкретной балансировки.
Tests NOT RUN: NewCampaign_ExactlyFourOrdinaryGroups; SquadCountsAsOneGroup; FourIndependentTypeDraws_NoQuota; LoadRevisit_NoAdditionalGroups; DefeatedGroup_NotRespawned; SafeInitialPositions_RespectConfiguredRegion.
## D-35-37: player fog включён, owner debug обзор выключает fog
Пользователь: «туман войны для игрока будет включен. я пока хочу видеть без тумана войны. Давай в этом эпике не будем обсудать сюжет».
Normal player demo default FogOfWarEnabled=true. Owner/developer использует явный debug override false через console для обзора карты. Прежние D-35-03 universal defaultfalse и A-35-02 resetfalse SUPERSEDED; unconditional false при Load/NewGame запрещён. Без явного override normal/legacy/new session safe default=true.
No-fog меняет только разрешённую debug projection, не sensor acquisition/AI knowledge/pursuit/доклады/генерацию loadout. Returntrue скрывает out-of-sensor live positions/tooltip/hit targets. Console показывает actual value/override status.
G-35-17 CLOSED D-35-40: app process memory, survives NewGame/Load, discarded on exit; defaults true/10.
Сюжет и named captain вне текущего EP35 grooming. Прежний G-35-16 перенесён в deferred integration note EP39, НЕ обязательный blocker EP35; EP39 не изменён. Не утверждено, что captain является пятой группой, появляется заранее или является исключением. Текущее D остаётся4 ORDINARY группы на старте/no respawn.
Tests NOT RUN: NormalNewGame_FogTrue; ExplicitDebugFalse_RevealsOnlyProjection; ReturnTrue_HidesUndetectedLiveContacts; LegacyAndNewSession_DefaultTrueWithoutOverride; FogOverride_NoSensorEventsOrAiKnowledge; OverrideLifetime_AccordingToResolvedG35_17.
## D-35-38: только лёгкие и тяжёлые дроны
Пользователь: «оставим только тяжелых и легких».
Текущий pool классов light/heavy; прежнее medium/heavy SUPERSEDED. Сохраняются independent50/50 per slot, отсутствие caps/forced mix, actual totals6/10/12, скрытый composition на first actual sensor acquisition. Оператор не раскрывает class distribution.
G-35-08 CLOSED: новый medium frame не нужен. Использовать existing Frames/light.json и Frames/heavy.json. Grounding layouts.json:3,16 — heavy sword/spear с2 shields; :29,41 — light sword/spear с1 shield. Equipment/layout IDs и weights теперь CLOSED D-35-39: два existing sword/spear варианта каждого класса,50/50 equipment.
Stable ticket EP-0035-US-0011-TK-0003-medium-content сохраняет исторический slug/ID, но теперь владеет существующим light/heavy layout pool; medium frame creation удалена из allowlist/steps.
## D-35-39: четыре существующих layouts, по25%
Пользователь подтвердил предложенный pool: «да берем. дальше».
Каждый drone slot независимо: class light/heavy50/50, затем sword/spear50/50. Ровно четыре разрешённых ID с marginal probability25% каждый: light.sword.shield,light.spear,heavy.sword.shield,heavy.spear.shield. Нет forced mix, caps или квот на класс/оружие; actual totals6/10/12 неизменны.
Оборудование брать из существующего layouts.json: gun.sword.t1/gun.spear.t1,plate.medium,shield.t1,два drive.standard,decoy.t1; light один щит,heavy два. Никаких новых frames/modules/layout вариантов. Grounding Client/Data/TacticalBattle/layouts.json:2–53. Existing light.spear ID сохраняется несмотря на отсутствие слова shield в имени.
G-35-07 equipment pool/weights CLOSED D-35-39; G-35-08 CLOSED D-35-38. Schema/content валидируют exact IDs и ссылки, runtime использует seeded draws class+equipment per slot, сохраняет per-carrier origin. Hidden loadout не попадает в сенсорный доклад; actual materialization timing/persistence прежние D/A.
Tests NOT RUN: ExactFourAllowedLayoutIds; ClassThenWeapon_IndependentDeterministicDraws; EachSlotUsesItsOwnDraws; LightOneShield_HeavyTwoShields; ExistingEquipmentReferencesValid; AllSameLayoutAllowed; Totals6_10_12Unchanged; PublicReport_NoLayoutDisclosure. Проверять deterministic RNG mapping, не случайные observed frequency.
Debug override lifetime CLOSED D-35-40.
## D-35-40…42: overrides, оператор и состоявшийся перехват
D-35-40 пользователь: «Да, настройки действуют до закрытия приложения, между запусками не сохраняются».
Host хранит explicit overrides только в app process memory: fog немедленно и после NewGame/Load; chance только следующий NewGame. Console main menu read/set без session допустим. Restart=>fogtrue/chance10; saves/profiles не содержат overrides. Failed/stale active application не выдаётся за effective success. Options применяются до первого engine tick/snapshot. G-35-13 lifetime и G-35-17 CLOSED.
D-35-41 пользователь: «Да, принимаем по наличию в экипаже и назначению, без смен и усталости».
Irina activecrew+sensor assignment=>264км,±1/±2; absent/unassigned=>script192км,±2/±4. Broken/disabled sensor=>0 coverage независимо от operator/script. No shifts/fatigue/injury schema. Reassignment восстанавливает бонус, не сбрасывает consumed groups; новые контакты расширенного радиуса идут через standard batch/autopause. G-35-06 CLOSED.
D-35-42 пользователь: «Да, принимаем: если перехват состоялся, после доклада и паузы начинается неизбежный бой».
При реальном pirate sensor contact и distance<=20км authoritative atomic capture фиксирует encounter. Player detection alone не capture. Для новой группы report+pause по batch policy; dismissal сохраняет Speed0; explicitcontinue начинает бой. Уже captured нельзя отменить сменой курса на паузе. Previously reported group не создаёт повторный automatic modal; используется существующий battle transition. Save/Load сохраняет pending capture, не даёт free escape. G-35-09 normal single-group inside-capture CLOSED; остаток: simultaneous capture CLOSED D-35-45: select earliest, no merge; recovery CLOSED D-35-47.
Tests NOT RUN: MainMenuOverrides_NoSession; OverridesSurviveNewGameLoadUntilExit; Restart_NormalDefaults; Save_NoOverrides; Chance_ExistingGroupsUnchanged; IrinaAbsentUnassigned_Fallback; AssignedIrina_Bonus; BrokenSensor_NoDetection; Reassignment_NoDuplicateReport; NewlyRevealedGroup_NormalBatch; CaptureThenReport_StaysPaused; ExplicitContinue_StartsUnavoidableBattle; PausedCourse_CannotUndoCapture; LoadPendingCapture_NoEscape; PlayerDetectionOnly_NoCapture.
Placement: all human station spawn400км A-35-17, START hard800км D-35-43/A-35-19; конкретные patrol routes ещё балансируются.
## A-35-17: делегированный выбор exclusion400км
Пользователь: «я думаю что пираты не могут быть заспавнены в определенных зонах от станций. выбери разумное значение».
Пользователь делегировал численный выбор; выбранный baseline400км=4000world units вокруг КАЖДОЙ человеческой станции demo, включая стартовую. Это A-35-17 исторический делегированный выбор, затем explicit D-35-44 подтвердил400. Обоснование: максимальный стартовый player sensor264км плюс136км separation margin; pirate sensor200км тоже меньше400. Это не гарантия безопасного рейса.
При initial generation4ordinary groups: для каждой общей group point и каждой станции distance>=400км; distance<400 запрещён, boundary400 допустима. Squad2xЛогово — одна world point. Использовать station positions на момент генерации, не runtime repulsion.
Другие stations: patrol/pursuit могут входить в400км spawn exclusion. START station — исключение D-35-43/A-35-19: hard800км no-entry и pursuit abort при safe player entry.
Seeded generator проверяет все станции; bounded retry затем понятная generation failure без partial world. Никогда молча уменьшать exclusion для размещения4groups. Сохраняются initial-only/no respawn и45/45/10.
A-35-18 proposed: если actual player initial position не совпадает со стартовой станцией, дополнительный clearance>264км от игрока; это отдельная рекомендация, не утверждённое400км правило для player. При таком scenario mismatch сначала явно разрешить G start clearance, не выдать unseen старт за доказанный.
Tests NOT RUN: AllFourGroups_OutsideEveryHumanStation400km; Boundary400_Allowed; Inside400_Rejected; SquadUsesCommonPoint; FixedSeed_ReproduciblePositions; ImpossibleLayout_FailsAtomically; ExclusionNotRelaxed; OtherStationPatrolMayEnter_StartSafezoneProtected.
Safe route/patrol balance отдельно; hard no-entry800км только START station по D-35-43, остальные stations spawn-only400.
## D-35-43 / A-35-19: hard safezone стартовой станции800км
Пользователь: «Принимаем безопасную зону300км вокруг стартовой станции, в которую пираты не заходят даже при погоне? Да. Может быть даже800. Максимум потом исправим».
D-35-43: hard safezone START station, действует patrol/pursuit. A-35-19 исторический tuning800км; число затем явно подтверждено D-35-44,8000world units.
Остальные человеческие станции сохраняют только initial spawn exclusion400км A-35-17, НЕ runtime safezone. Start initial spawn minimum=max(400,800)=800км. Config хранит отдельные spawnExclusionRadius и startSafezoneRadius; изменение балансировки не требует новой механики.
Pirate group center всегда distance>=800км от start station. Player distance<=800км считается safe: никакого нового capture внутри/на boundary, даже если другая distance<=20км. При входе игрока в safezone текущая pursuit прекращается и pirates возвращаются patrol; это не телепорт и не мгновенное изменение физических скоростей. Уже committed capture D-35-42 не отменяется последующим приказом/телепортом; после capture ordinary movement не продолжается до разрешения боя.
No-entry проверяет actual swept motion segment целого шага, не только waypoint/endpoints; маршрут/поворот/торможение планировать заранее с действующими motion constraints. Нельзя пропустить пересечение круга большим dt, clamp/teleport корабль обратно или остановить инерцию мгновенно. Для невозможного patrol path нужен validated обход либо явный отказ content; радиус не уменьшать молча.
Start zone привязана к stable startstation identity; для текущего stationary demo используется её world position. Если станция станет подвижной — отдельное будущие правило, не вводить repulsion. Station turrets не входят в задачу.
Supersedes прежнее A-35-17 утверждение «нет hard safezone»: теперь исключение START station800. Для других stations это утверждение сохраняется: только initial400, позднее patrol/chase могут входить.
Tests NOT RUN: StartSpawnOutside800; OtherStationSpawnOutside400; PatrolSweptSegment_NeverEntersStart800; PursuitSweptSegment_NeverEntersStart800; SafePlayerEntry_AbortPursuitReturnPatrol; Boundary800_PlayerSafePirateNotInside; OtherStation_NoRuntimeSafehaven; CommittedCapture_CannotBeUndone; ConfigurableRadii; ImpossiblePlacementOrRoute_FailsWithoutRelaxation.
Owning US1 content/runtime validates initial placement/route; US2 motion/pursuit enforces segment/state; US4 save preserves safe return/pending capture without freeescape. Existing one-layer/max5file allowlists unchanged; при motion-owner drift сначала корректировать Board scope.
## D-35-44: явно утверждённые радиусы800/400
Пользователь: «Да, оставляем800км у стартовой и400км для спавна у остальных станций. Добавляй в тикеты».
Численные значения теперь explicit D, supersede provisional/delegated A-35-17/A-35-19: START station hard800км (spawn/patrol/pursuit no-entry); остальные человеческие станции initial spawn exclusion400км, без runtime защиты. Оба значения content parameters. Boundary/segment/safe-entry/committed capture invariants D-35-43 сохраняются.
G-35-18 CLOSED D-35-45: earliest continuous capture, deterministic tie, последовательные бои без merge; world frozen then paused, resume без immunity.
Остаток G-35-09/15: abnormal/partial battle outcomes и корректное world continuation без новой механики тактического отступления. Normal single capture/report/continue уже D-35-42.
A-35-18 стартовая позиция игрока отдельно от станции требует проверки actual scenario; отдельный clearance не утверждён. При обычном старте у start station800 уже покрывает sensor separation.
Прочие продуктовые decisions (radii,skill/ETA,equipment,spawn count/weights/norespawn,operator,fog,lifetime,batching) закрыты. Routine module physical stats/конкретные patrol waypoint coordinates остаются implementation/balance деталями; narrative deferred внеscope.
## D-35-45/46: последовательные бои и только победа/поражение
Пользователь: «Да, принимаем последовательные бои без присоединения соседних групп. Добавляй в тикеты. Давай теперь разберём выход из незавершённого боя, загрузку и сохранение последствий. Из боя выйти можно проиграв или победив».
D-35-45: несколько разных groups пересекают capture threshold=>выбрать earliest continuous crossing event; exact tie решается детерминированно по stable groupId (технический deterministic tiebreak). Только выбранная группа входит в бой, максимум2enemy carriers при squad2Логово; соседи не присоединяются/не подкрепляют. World motion frozen в бою. После результата world остаётся paused; explicitresume может позволить другой группе захватить игрока по обычным условиям. Нет postwin immunity.
G-35-18 CLOSED. Не путать arbitration с batch first-discovery: одна сводка шага может перечислить несколько новых групп, но только selected captured group участвует в текущем бою.
D-35-46: normal EP35 campaign battle заканчивается только win/loss. Нет retreat/abort/обычного выхода или загрузки другого мира, позволяющих уйти из ongoing battle. UI/command guard authoritative, не только скрытая кнопка. Debug/tutorial exit_battle существующие semantics — отдельный явный context; не разрешать ему незаметно обходить normal campaign capture и не менять tutorial tooling без owner scope.
G-35-19 CLOSED D-35-47: durable pending same-battle restart; no perturn resume, no freeworld.
Grounding LocalGameSessionConnection.cs:145–180: accepted settlement receipt cached by BattleId, повтор retry только continuation write; SortieSettlement.cs:17 применяет outcome до clear session. Переиспользовать existing exactly-once consequences; не считать Save failure повторной наградой.
Tests NOT RUN: EarliestContinuousCapture_WinsArbitration; ExactTie_StableGroupId; NeighborGroup_NotMerged; Battle_FreezesWorldMotion; ReturnWorld_Paused; Resume_AnotherCaptureAllowedNoImmunity; OngoingCampaignBattle_RejectNormalExitAndEscapeLoad; TutorialDebugContext_ExplicitSeparation; SettlementReplay_ExactlyOnce.
## D-35-47: восстановление captured боя с начала
Пользователь: «Да, принимаем восстановление того же боя с начала после закрытия приложения или сбоя».
Durable prebattle pending checkpoint ДО tactical start: committed capture, fixed group/member/drone composition, exact player/company prebattle state, stable encounter/battleId и replay seed. Continue после close/crash запускает тот же бой с начала, не navigable world. Ordinary midbattle save/load/menu retreat запрещены; perturn resume не вводится.
Win/loss settlement exactly once, затем durable finished checkpoint до map/end. Failed write удерживает pending result/retry storage, не restart/reapply. Crash при atomic persistence даёт либо old durable pending battle replay, либо durable finished state. Intermediate damage persistence не обещается.
G-35-19 CLOSED. US0016 owns engine checkpoint, local-client durable storage и client recovery. Legacy aborted settlement не используется для ordinary EP35 unfinished battle; debug/tutorial context отдельно.
Tests NOT RUN: checkpoint-before-start; same composition/playerstate/seed replay; no navigable escape; failedwrite retryonly; atomicoldpending-orfinished; exactlyonce rewards/losses; no pending loss via autosave.
## D-35-48: ничья считается поражением игрока
Пользователь: «ничья равно поражению. игрок проиграл».
Existing Draw, включая NoCombatCapability и simultaneousdefeat, отображается/учитывается как player defeat для EP35 campaign outcome. Сохранить actual hull/ammo/disabled/destroyed/pilot state; не фабриковать hull0/смерть пилотов ради label. Победные награды не выдавать; exactly-once settlement/durable boundary D-35-47 сохраняются. Поражение боя само по себе не вводит новый gameover при живом player carrier; existing destroyed-player terminal semantics сохраняются.
G-35-20 Draw CLOSED D-35-48; G-35-20T Timeout CLOSED D-35-49. Interactive SeriesTurnCap также player defeat.
Tests NOT RUN: DrawNoCombatCapability_PlayerDefeat; SimultaneousDefeat_PlayerDefeat; DrawDoesNotFabricateDamageOrPilotDeath; Defeat_NoVictoryReward; OutcomeReceiptIdempotent. Timeout policy CLOSED D-35-49; проверки только запланированы, не PASS реализации.
## D-35-49: истечение лимита ходов — поражение игрока
Пользователь ответил «да» на вопрос «Истечение лимита ходов тоже считаем поражением?».
EP35 campaign outcome/UI считает existing Timeout/SeriesTurnCap поражением игрока, как Draw по D-35-48. Сохранить actual hull/ammo/disabled/destroyed/pilot state; не фабриковать уничтожение носителя/дронов или смерть пилотов. Применить штатные defeat consequences exactlyonce, без victory rewards, затем durable finished checkpoint до map/end по D-35-47. Сам label defeat не вводит новый gameover при живом carrier.
G-35-20T CLOSED. Продуктовый грумминг EP35 завершён; технические A/allowlist/schema drift проверяет owning implementation, это не новые user blockers. Implementation NOT STARTED, tests/build/native/product acceptance NOT RUN/OPEN.
Tests NOT RUN: SeriesTurnCap_PlayerDefeat; TimeoutPreservesActualDamageAndPilotState; Timeout_NoVictoryRewards; TimeoutSettlement_ExactlyOnce; TimeoutFinishedCheckpoint_DurableBeforeRelease; TimeoutReplayReceipt_NoReapply.

## D-35-50: удаление участвовавшей группы после завершённого боя

Время записи решения: 2026-11-17; точное время сообщения не заявляется. Пользователь: «После завершённого боя убирать группу с карты».

Решение распространяется на завершённые win/loss, включая Draw и Timeout. Только участвовавшая группа получает retired и исключается из карты, патруля, сенсоров и новых перехватов; остальные группы не меняются. Retirement не изменяет фактические корпуса, состояния дронов и пилотов в результате боя. Нет возрождения. Pending checkpoint до завершения боя сохраняет ту же группу для восстановления боя с начала. Settlement, retirement и receipt входят в один сохраняемый результат; ошибка записи не разрешает карту и не применяет последствия повторно. Владельцы — US0015 settlement/save и US0016 durable recovery; US0003/0004 сохраняют общий lifecycle. Это закрывает A-35-20, не вводит retreat или фиктивное уничтожение.
