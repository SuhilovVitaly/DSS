---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0001-contracts-and-schema"
title: "Контракты и схема данных"
stage: "implemented"
dependencies: []
created: "2026-10-20T07:48:46.640Z"
source_request: "Приложенное ТЗ EP-0012 revision2; внешние ссылки исключены"
current_review: "implementation-self-reviewed-acceptance-separate"
revision: 2
---

# US-0001 — Контракты и схема данных

## Входное техническое задание

[Эпик](../EP-0012-combat-rebuild.md) · [D/A/G и полный контракт](../Documentation.md) · [Межисторийный ticket DAG](../Tickets.md)


**User story.** Как разработчик движка и клиента, я работаю с контрактами и данными, описывающими торпеду в точку, противоракету, уклонение и боевое столкновение, а движок отвергает некорректные данные при загрузке.

**Acceptance criteria**
- **US1-AC1:** Снапшоты различают свой и чужой снаряд:
  - свой снаряд: позиция, старт, точка подрыва, время подрыва, профиль, R, урон, бронепробитие;
  - чужой снаряд: позиция, единичный вектор направления, скорость, профиль, R.
  - Полей цели, маршрута, шанса и `CapturedAttack` нет.
- **US1-AC2:** Есть команды `torpedo.launch`, `torpedo.detonate`, `defense.launch` и `maneuver.evade`, у каждой определены коды отказа. Команды `torpedo.fire`, `torpedo.setMode`, `torpedo.selfDestruct`, `defense.fire`, `defense.enable` и `defense.disable` удалены.
- **US1-AC3:** Есть снапшот боевого столкновения: состояние `{Idle, Active, Won, Lost}`, время начала, длительность и статистика (US8-AC3).
- **US1-AC4:** Правила боя и аппараты с профилями загружаются из JSON. На каждое невалидное значение — `ContentException` с указанием файла и поля:
  - `blastCoreFraction` вне (0, 1);
  - `hitDisableMs` < 0;
  - профиль без скорости > 0 или без R > 0;
  - пустой список профилей;
  - `scatterPercent` < 0;
  - дальность ≤ 0.



## Non-goals / Invariants

Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Scope этой истории ограничен её AC и allowlists; соседние истории не выполняются неявно. D-USER-02 имеет приоритет над исходным<=100ms projectile требованием.

## Dependencies

Execution order задаётся ticket DAG, который допускает чередование слоёв между историями. Stories — тематические контейнеры, не атомарные build units; metadata dependencies перечисляет тематические зависимости; до начала тикета обязательна готовность его API/schema prerequisites по актуальному DAG; обязательные межслойные depends_on отдельных тикетов дополняют этот список. Входящие prerequisite stories: нет. Нативные открытые gates EP11 не объявляются закрытыми EP12.

## Ticket map

| Order / Ticket | Layer | Depends on | Serves | Files |
|---|---|---|---|---:|
| 1 / [EP-0012-US-0001-TK-0001-projectile-contracts](EP-0012-US-0001-TK-0001-projectile-contracts/EP-0012-US-0001-TK-0001-projectile-contracts.md) | contracts | baseline | US1-AC1 | 4 |
| 2 / [EP-0012-US-0001-TK-0002-command-contracts](EP-0012-US-0001-TK-0002-command-contracts/EP-0012-US-0001-TK-0002-command-contracts.md) | contracts | EP-0012-US-0001-TK-0001-projectile-contracts | US1-AC2 | 5 |
| 3 / [EP-0012-US-0001-TK-0003-encounter-contracts](EP-0012-US-0001-TK-0003-encounter-contracts/EP-0012-US-0001-TK-0003-encounter-contracts.md) | contracts | EP-0012-US-0001-TK-0002-command-contracts | US1-AC3 | 4 |
| 4 / [EP-0012-US-0001-TK-0004-combat-rules-schema](EP-0012-US-0001-TK-0004-combat-rules-schema/EP-0012-US-0001-TK-0004-combat-rules-schema.md) | engine | EP-0012-US-0001-TK-0003-encounter-contracts | US1-AC4 | 9 |
| 5 / [EP-0012-US-0001-TK-0005-launcher-profile-schema](EP-0012-US-0001-TK-0005-launcher-profile-schema/EP-0012-US-0001-TK-0005-launcher-profile-schema.md) | engine-and-contracts | EP-0012-US-0001-TK-0004-combat-rules-schema | US1-AC4 | 7 |

## Decisions / Assumptions

D01–D18/O1–O12 входного документа и D-USER-01/02, A01–A06 и A-PLAN-01–12 из Documentation обязательны в указанном статусе. Новые типы, partial и tests — proposed.

## Gaps and backlog

G-01–G-06 из Documentation. Все missing consumer references блокируют закрытие соответствующего тикета, без неограниченных проходов. Native/product проверяются отдельно; planning не даёт их evidence.

## Decision / review log

### Исполнение и self-review — 2026-10-20

Пять исходных contract/schema тикетов реализованы в их уточнённом scope; актуальные SHA и evidence — в [журнале](../ImplementationStatus.md). TK-0004 scope 9 файлов включает nullable Chance consumers; TK-0005 scope 7 файлов включает необходимый общий profile DTO и matching tests двух слоёв. Архитектурные направления зависимостей не менялись. Новый content loader и Contracts проверены: 39/39 profile Engine cases, 387/387 bounded Engine regression, 239/239 полный Contracts; solution build и scoped format PASS.

История остаётся **in_progress**, а не полностью принятой: US1-AC2 требует окончательного удаления legacy команд после переноса consumers; new runtime, masking и encounter producers реализуются по interleaved DAG. Дополнительный full Engine test process превысил 300s, полный Engine gate OPEN до bounded integration checks. Native/product/balance OPEN. Self-review не является независимым одобрением. Исходная planning-запись ниже сохранена как история.

2026-10-20T07:48:46.640Z: создано по поручению пользователя. Разделение слоёв/<=5 файлов — техническая декомпозиция. Stage draft, implementation NOT STARTED, проверки кода не запускались.


## Итог технического исполнения — 2026-10-21

Все дочерние тикеты реализованы и опубликованы отдельными коммитами; [общий ledger](../PublicationStatus.md) и [фактические проверки/ограничения](../ImplementationStatus.md). Это technical implemented/self-review, не независимое review или user acceptance. Применимые human/native/product gates остаются в итоговом отчёте; эта отметка не закрывает их зелёными unit tests.
