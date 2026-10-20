---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0004-point-aim-countermissiles"
title: "Противоракеты"
stage: "in_progress"
dependencies: ["EP-0012-US-0001-contracts-and-schema", "EP-0012-US-0002-combat-catalog-v3"]
created: "2026-10-20T07:48:46.640Z"
source_request: "Приложенное ТЗ EP-0012 revision2; внешние ссылки исключены"
current_review: "planning-structural-checked"
revision: 1
---

# US-0004 — Противоракеты

## Входное техническое задание

[Эпик](../EP-0012-combat-rebuild.md) · [D/A/G и полный контракт](../Documentation.md) · [Межисторийный ticket DAG](../Tickets.md)


**User story.** Как игрок, я сбиваю вражеские торпеды противоракетой, наведённой в точку на их трассе.

**Acceptance criteria**
- **US4-AC1:** `defense.launch` принимается, если аппарат готов, у него есть оператор, точка не дальше 30 км и профиль известен. Разброс 8 % из потока `launch_scatter`. Перезарядка 12,5 с.
- **US4-AC2:** В момент подрыва противоракеты все ракеты (торпеды и противоракеты любой стороны), находящиеся в R, уничтожаются без взрыва. Корабли не получают урона. В журнал пишется `CountermissileDetonated {projectileId, x, y, destroyed[]}`.
- **US4-AC3:** Удалены авто-перехват, арбитраж (`NextDefenseLaunch`, `CompareDefenseLaunch`), очередь стволов, бросок перехвата, «coasting» после промаха, команды `defense.fire`, `defense.enable` и `defense.disable`, а также их тесты.



## Non-goals / Invariants

Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Scope этой истории ограничен её AC и allowlists; соседние истории не выполняются неявно. D-USER-02 имеет приоритет над исходным<=100ms projectile требованием.

## Dependencies

Execution order задаётся ticket DAG, который допускает чередование слоёв между историями. Stories — тематические контейнеры, не атомарные build units; metadata dependencies перечисляет тематические зависимости; до начала тикета обязательна готовность его API/schema prerequisites по актуальному DAG; обязательные межслойные depends_on отдельных тикетов дополняют этот список. Входящие prerequisite stories: EP-0012-US-0003-point-aim-torpedoes. Нативные открытые gates EP11 не объявляются закрытыми EP12.

## Ticket map

| Order / Ticket | Layer | Depends on | Serves | Files |
|---|---|---|---|---:|
| 14 / [EP-0012-US-0004-TK-0001-countermissile-point-launch](EP-0012-US-0004-TK-0001-countermissile-point-launch/EP-0012-US-0004-TK-0001-countermissile-point-launch.md) | engine | EP-0012-US-0003-TK-0004-manual-detonation | US4-AC1 | 4 |
| 15 / [EP-0012-US-0004-TK-0002-countermissile-detonation](EP-0012-US-0004-TK-0002-countermissile-detonation/EP-0012-US-0004-TK-0002-countermissile-detonation.md) | engine | EP-0012-US-0004-TK-0001-countermissile-point-launch | US4-AC2 | 4 |
| 16 / [EP-0012-US-0004-TK-0003-remove-auto-pd](EP-0012-US-0004-TK-0003-remove-auto-pd/EP-0012-US-0004-TK-0003-remove-auto-pd.md) | engine | EP-0012-US-0004-TK-0002-countermissile-detonation | US4-AC3 | 4 |

## Decisions / Assumptions

D01–D18/O1–O12 входного документа и D-USER-01/02, A01–A06 и A-PLAN-01–12 из Documentation обязательны в указанном статусе. Новые типы, partial и tests — proposed.

## Gaps and backlog

G-01–G-06 из Documentation. Все missing consumer references блокируют закрытие соответствующего тикета, без неограниченных проходов. Native/product проверяются отдельно; planning не даёт их evidence.

## Decision / review log

2026-10-20T07:48:46.640Z: создано по поручению пользователя. Разделение слоёв/<=5 файлов — техническая декомпозиция. Stage draft, implementation NOT STARTED, проверки кода не запускались.
