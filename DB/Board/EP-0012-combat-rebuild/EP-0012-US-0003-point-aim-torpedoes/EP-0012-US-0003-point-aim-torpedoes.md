---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0003-point-aim-torpedoes"
title: "Торпеда в точку"
stage: "implemented"
dependencies: ["EP-0012-US-0001-contracts-and-schema", "EP-0012-US-0002-combat-catalog-v3"]
created: "2026-10-20T07:48:46.640Z"
source_request: "Приложенное ТЗ EP-0012 revision2; внешние ссылки исключены"
current_review: "implementation-self-reviewed-acceptance-separate"
revision: 1
---

# US-0003 — Торпеда в точку

## Входное техническое задание

[Эпик](../EP-0012-combat-rebuild.md) · [D/A/G и полный контракт](../Documentation.md) · [Межисторийный ticket DAG](../Tickets.md)


**User story.** Как игрок, я выбираю точку и профиль и пускаю торпеду, а она взрывается там и наносит урон всем, кто оказался в радиусе.

**Acceptance criteria**
- **US3-AC1:** Чистая математика в Motion:
  - спад урона D03;
  - σ разброса D05;
  - положение снаряда на прямой в момент t;
  - время подрыва.
  - Аналитические контрольные точки из blast-math заменяют внешний симулятор.
- **US3-AC2:** `torpedo.launch` принимается, только если аппарат готов, не отключён, не уничтожен, у него есть оператор, точка не дальше 70 км и профиль известен. Иначе — отказ с кодом US1-TK2. После пуска перезарядка начинается заново, точка подрыва смещается разбросом из потока `launch_scatter`, профиль и урон захватываются.
- **US3-AC3:** В `t_det` каждый живой корабль в R получает `round(200 × k)` урона в один модуль по массе (бронепробитие 20 %), модуль отключается на 8 с, и проверяется правило гибели. Свои корабли тоже получают урон.
- **US3-AC4:** `torpedo.detonate` по своей живой торпеде взрывает её в текущей позиции по правилам US3-AC3. По чужой торпеде — отказ `not_own_projectile`.
- **US3-AC5:** В движке нет самонаведения, перепланирования маршрута, броска попадания, режимов и поломки. Удалены `CombatChanceMath`, `InterceptionMath` (с переносом проверки сохранений) и `FireModeMath`. Удалены и их тесты.



## Non-goals / Invariants

Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Scope этой истории ограничен её AC и allowlists; соседние истории не выполняются неявно. D-USER-02 имеет приоритет над исходным<=100ms projectile требованием.

## Dependencies

Execution order задаётся ticket DAG, который допускает чередование слоёв между историями. Stories — тематические контейнеры, не атомарные build units; metadata dependencies перечисляет тематические зависимости; до начала тикета обязательна готовность его API/schema prerequisites по актуальному DAG; обязательные межслойные depends_on отдельных тикетов дополняют этот список. Входящие prerequisite stories: EP-0012-US-0017-event-and-command-integration, EP-0012-US-0018-legacy-combat-cleanup. Нативные открытые gates EP11 не объявляются закрытыми EP12.

## Ticket map

| Order / Ticket | Layer | Depends on | Serves | Files |
|---|---|---|---|---:|
| 8 / [EP-0012-US-0003-TK-0001-blast-math](EP-0012-US-0003-TK-0001-blast-math/EP-0012-US-0003-TK-0001-blast-math.md) | motion | EP-0012-US-0017-TK-0004-turn-and-summary-command-contracts | US3-AC1 | 2 |
| 11 / [EP-0012-US-0003-TK-0002-torpedo-point-launch](EP-0012-US-0003-TK-0002-torpedo-point-launch/EP-0012-US-0003-TK-0002-torpedo-point-launch.md) | engine | EP-0012-US-0018-TK-0004-shared-point-forecast | US3-AC2 | 8 |
| 12 / [EP-0012-US-0003-TK-0003-torpedo-area-detonation](EP-0012-US-0003-TK-0003-torpedo-area-detonation/EP-0012-US-0003-TK-0003-torpedo-area-detonation.md) | engine | EP-0012-US-0003-TK-0002-torpedo-point-launch | US3-AC3 | 4 |
| 13 / [EP-0012-US-0003-TK-0004-manual-detonation](EP-0012-US-0003-TK-0004-manual-detonation/EP-0012-US-0003-TK-0004-manual-detonation.md) | engine | EP-0012-US-0003-TK-0003-torpedo-area-detonation | US3-AC4 | 4 |
| 64 / [EP-0012-US-0003-TK-0005-remove-homing-roll-modes](EP-0012-US-0003-TK-0005-remove-homing-roll-modes/EP-0012-US-0003-TK-0005-remove-homing-roll-modes.md) | motion-engine-client-tools | EP-0012-US-0018-TK-0001-remove-interception-helper | US3-AC5 | 159 |

## Decisions / Assumptions

D01–D18/O1–O12 входного документа и D-USER-01/02, A01–A06 и A-PLAN-01–12 из Documentation обязательны в указанном статусе. Новые типы, partial и tests — proposed.

## Gaps and backlog

G-01–G-06 из Documentation. Все missing consumer references блокируют закрытие соответствующего тикета, без неограниченных проходов. Native/product проверяются отдельно; planning не даёт их evidence.

## Decision / review log

2026-10-20T07:48:46.640Z: создано по поручению пользователя. Разделение слоёв/<=5 файлов — техническая декомпозиция. Stage draft, implementation NOT STARTED, проверки кода не запускались.


## Итог технического исполнения — 2026-10-21

Все дочерние тикеты реализованы и опубликованы отдельными коммитами; [общий ledger](../PublicationStatus.md) и [фактические проверки/ограничения](../ImplementationStatus.md). Это technical implemented/self-review, не независимое review или user acceptance. Применимые human/native/product gates остаются в итоговом отчёте; эта отметка не закрывает их зелёными unit tests.
