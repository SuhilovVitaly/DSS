---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0009-scenarios-and-saves"
title: "Сценарии и сохранения"
stage: "draft"
dependencies: ["EP-0012-US-0001-contracts-and-schema", "EP-0012-US-0002-combat-catalog-v3", "EP-0012-US-0004-point-aim-countermissiles", "EP-0012-US-0006-threats-and-evasion", "EP-0012-US-0007-mukha-ai-v3", "EP-0012-US-0008-encounter-and-time"]
created: "2026-10-20T07:48:46.640Z"
source_request: "Приложенное ТЗ EP-0012 revision2; внешние ссылки исключены"
current_review: "planning-structural-checked"
revision: 1
---

# US-0009 — Сценарии и сохранения

## Входное техническое задание

[Эпик](../EP-0012-combat-rebuild.md) · [D/A/G и полный контракт](../Documentation.md) · [Межисторийный ticket DAG](../Tickets.md)


**User story.** Как игрок, я запускаю бой «Тетрарх против 3 Мух» в конфигурации v3 и могу сохранить и загрузить игру посреди боя.

**Acceptance criteria**
- **US9-AC1:** `Client/Scenarios/CombatMukha/scenario.json`:
  - Тетрарх с 9 модулями в лёгкой броне (мостик, каюты, грузовой, сканер, генератор, двигатель, торпедный аппарат, 2 ПРО; массы 4, 2, 20, 2, 12, 5, 2, 2, 2 т — сверить с каталогом);
  - старт игрока: (0, 0), курс 90°, скорость 3 км/с;
  - 3 Мухи на (−40, 75), (0, 85), (40, 75) км, курс 270°, 6 км/с;
  - экипаж 20;
  - `ai` по D13–D14.
- **US9-AC2:** Сценарий `Client/Scenarios/CombatShmel` удалён вместе со ссылками на него (список сценариев, тесты, документы). Остальные боевые сценарии (пират `PlayerShipOnly`, учебные) переведены на новую схему, и каждый загружается без ошибок.
- **US9-AC3:** Формат сохранения 18. Сохраняются снаряды (своя и чужая торпеда, противоракета), состояние уклонения, базовый курс, состояние боя и статистика, потоки ГСЧ `launch_scatter` и `npc_dodge`. Сохранение и загрузка посреди боя дают тот же исход, что непрерывный бой с тем же сидом. Формат 17 отвергается.



## Non-goals / Invariants

Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Scope этой истории ограничен её AC и allowlists; соседние истории не выполняются неявно. D-USER-02 имеет приоритет над исходным<=100ms projectile требованием.

## Dependencies

Execution order задаётся ticket DAG, который допускает чередование слоёв между историями. Stories — тематические контейнеры, не атомарные build units; metadata dependencies перечисляет тематические зависимости; до начала тикета обязательна готовность его API/schema prerequisites по актуальному DAG; обязательные межслойные depends_on отдельных тикетов дополняют этот список. Входящие prerequisite stories: EP-0012-US-0019-scenario-and-client-regressions. Нативные открытые gates EP11 не объявляются закрытыми EP12.

## Ticket map

| Order / Ticket | Layer | Depends on | Serves | Files |
|---|---|---|---|---:|
| 53 / [EP-0012-US-0009-TK-0001-scenario-combat-mukha-v3](EP-0012-US-0009-TK-0001-scenario-combat-mukha-v3/EP-0012-US-0009-TK-0001-scenario-combat-mukha-v3.md) | content-data | EP-0012-US-0019-TK-0005-maneuver-command-catalog | US9-AC1 | 2 |
| 54 / [EP-0012-US-0009-TK-0002-remove-shmel-and-migrate-scenarios](EP-0012-US-0009-TK-0002-remove-shmel-and-migrate-scenarios/EP-0012-US-0009-TK-0002-remove-shmel-and-migrate-scenarios.md) | content-data | EP-0012-US-0009-TK-0001-scenario-combat-mukha-v3 | US9-AC2 | 3 |
| 55 / [EP-0012-US-0009-TK-0003-save-format-18](EP-0012-US-0009-TK-0003-save-format-18/EP-0012-US-0009-TK-0003-save-format-18.md) | engine | EP-0012-US-0009-TK-0002-remove-shmel-and-migrate-scenarios | US9-AC3 | 5 |
| 56 / [EP-0012-US-0009-TK-0004-save18-capture-and-restore](EP-0012-US-0009-TK-0004-save18-capture-and-restore/EP-0012-US-0009-TK-0004-save18-capture-and-restore.md) | engine | EP-0012-US-0009-TK-0003-save-format-18 | US9-AC3 | 5 |
| 57 / [EP-0012-US-0009-TK-0005-save18-defense-validation](EP-0012-US-0009-TK-0005-save18-defense-validation/EP-0012-US-0009-TK-0005-save18-defense-validation.md) | engine | EP-0012-US-0009-TK-0004-save18-capture-and-restore | US9-AC3, US3-AC5 | 4 |

## Decisions / Assumptions

D01–D18/O1–O12 входного документа и D-USER-01/02, A01–A06 и A-PLAN-01–12 из Documentation обязательны в указанном статусе. Новые типы, partial и tests — proposed.

## Gaps and backlog

G-01–G-06 из Documentation. Все missing consumer references блокируют закрытие соответствующего тикета, без неограниченных проходов. Native/product проверяются отдельно; planning не даёт их evidence.

## Decision / review log

2026-10-20T07:48:46.640Z: создано по поручению пользователя. Разделение слоёв/<=5 файлов — техническая декомпозиция. Stage draft, implementation NOT STARTED, проверки кода не запускались.
