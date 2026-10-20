---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0019-scenario-and-client-regressions"
title: "Совместимость сценариев и старых клиентских поверхностей"
stage: "in_progress"
dependencies: ["EP-0012-US-0001-contracts-and-schema", "EP-0012-US-0002-combat-catalog-v3"]
created: "2026-10-20T07:48:46.640Z"
source_request: "Приложенное ТЗ EP-0012 revision2; внешние ссылки исключены"
current_review: "planning-structural-checked"
revision: 1
---

# US-0019 — Совместимость сценариев и старых клиентских поверхностей

## Входное техническое задание

[Эпик](../EP-0012-combat-rebuild.md) · [D/A/G и полный контракт](../Documentation.md) · [Межисторийный ticket DAG](../Tickets.md)

**User story.** Как игрок, я запускаю доступные сценарии и пользуюсь боевыми панелями после обновления без ссылок на удалённую схему.

**Acceptance criteria**
- **US19-AC1:** Все доступные сценарии загружаются с v3 schema; Shmel отсутствует в discovery и актуальных fixtures. CombatMukhaSingle сохраняет один enemy, не становится новым балансным сценарием.
- **US19-AC2:** Старые клиентские tests/forecast/cache/projector переведены на новый контракт без скрытых данных или fake authoritative motion.


## Non-goals / Invariants

Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Scope этой истории ограничен её AC и allowlists; соседние истории не выполняются неявно. D-USER-02 имеет приоритет над исходным<=100ms projectile требованием.

## Dependencies

Execution order задаётся ticket DAG, который допускает чередование слоёв между историями. Stories — тематические контейнеры, не атомарные build units; metadata dependencies перечисляет тематические зависимости; до начала тикета обязательна готовность его API/schema prerequisites по актуальному DAG; обязательные межслойные depends_on отдельных тикетов дополняют этот список. Входящие prerequisite stories: EP-0012-US-0002-combat-catalog-v3, EP-0012-US-0009-scenarios-and-saves, EP-0012-US-0017-event-and-command-integration. Нативные открытые gates EP11 не объявляются закрытыми EP12.

## Ticket map

| Order / Ticket | Layer | Depends on | Serves | Files |
|---|---|---|---|---:|
| 29 / [EP-0012-US-0019-TK-0003-client-forecast-cache-migration](EP-0012-US-0019-TK-0003-client-forecast-cache-migration/EP-0012-US-0019-TK-0003-client-forecast-cache-migration.md) | client | EP-0012-US-0017-TK-0003-local-client-delivery | US19-AC2 | 5 |
| 55 / [EP-0012-US-0019-TK-0005-maneuver-command-catalog](EP-0012-US-0019-TK-0005-maneuver-command-catalog/EP-0012-US-0019-TK-0005-maneuver-command-catalog.md) | content-data | EP-0012-US-0002-TK-0005-combat-rules-v3 | US19-AC1, US6-AC2, US6-AC3, US8-AC1 | 4 |
| 58 / [EP-0012-US-0019-TK-0001-migrate-other-combat-scenarios](EP-0012-US-0019-TK-0001-migrate-other-combat-scenarios/EP-0012-US-0019-TK-0001-migrate-other-combat-scenarios.md) | content-data | EP-0012-US-0009-TK-0002-remove-shmel-and-migrate-scenarios | US19-AC1, US9-AC2 | 35 |
| 59 / [EP-0012-US-0019-TK-0002-scenario-engine-fixtures](EP-0012-US-0019-TK-0002-scenario-engine-fixtures/EP-0012-US-0019-TK-0002-scenario-engine-fixtures.md) | engine | EP-0012-US-0019-TK-0001-migrate-other-combat-scenarios | US19-AC1 | 4 |
| 60 / [EP-0012-US-0019-TK-0004-client-combat-regressions](EP-0012-US-0019-TK-0004-client-combat-regressions/EP-0012-US-0019-TK-0004-client-combat-regressions.md) | client | EP-0012-US-0019-TK-0002-scenario-engine-fixtures | US19-AC1, US19-AC2 | 5 |

## Decisions / Assumptions

D01–D18/O1–O12 входного документа и D-USER-01/02, A01–A06 и A-PLAN-01–12 из Documentation обязательны в указанном статусе. Новые типы, partial и tests — proposed.

## Gaps and backlog

G-01–G-06 из Documentation. Все missing consumer references блокируют закрытие соответствующего тикета, без неограниченных проходов. Native/product проверяются отдельно; planning не даёт их evidence.

## Decision / review log

2026-10-20T07:48:46.640Z: создано по поручению пользователя. Разделение слоёв/<=5 файлов — техническая декомпозиция. Stage draft, implementation NOT STARTED, проверки кода не запускались.
