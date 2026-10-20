---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0014-balance-and-acceptance"
title: "Приёмка и баланс"
stage: "draft"
dependencies: ["EP-0012-US-0001-contracts-and-schema", "EP-0012-US-0002-combat-catalog-v3", "EP-0012-US-0003-point-aim-torpedoes", "EP-0012-US-0004-point-aim-countermissiles", "EP-0012-US-0005-snapshot-privacy", "EP-0012-US-0006-threats-and-evasion", "EP-0012-US-0007-mukha-ai-v3", "EP-0012-US-0008-encounter-and-time", "EP-0012-US-0009-scenarios-and-saves", "EP-0012-US-0010-tactical-map", "EP-0012-US-0011-aiming-and-launch", "EP-0012-US-0012-maneuver-and-threat-ui", "EP-0012-US-0013-combat-state-and-feedback", "EP-0012-US-0016-combat-localization", "EP-0012-US-0017-event-and-command-integration", "EP-0012-US-0018-legacy-combat-cleanup", "EP-0012-US-0019-scenario-and-client-regressions"]
created: "2026-10-20T07:48:46.640Z"
source_request: "Приложенное ТЗ EP-0012 revision2; внешние ссылки исключены"
current_review: "planning-structural-checked"
revision: 1
---

# US-0014 — Приёмка и баланс

## Входное техническое задание

[Эпик](../EP-0012-combat-rebuild.md) · [D/A/G и полный контракт](../Documentation.md) · [Межисторийный ticket DAG](../Tickets.md)


**User story.** Как автор, я вижу, что бой в движке даёт те же результаты, что эталон v3, а UI проверен в реальном окне.

**Acceptance criteria**
- **US14-AC1:** `CombatBalanceReport` работает на production-реестре. Бот игрока в движке повторяет политики симулятора `dodge` и `static`: прицел по дуге, ПРО на рубеже, уклонение через `maneuver.evade`. На 1000 сидах сценария «3 Мухи»:
  - `dodge` даёт 72–88 % побед, `static` — ≤ 10 %;
  - P90 длительности ≤ 5,5 мин реального времени.
  - Если результат вне коридора, тикет фиксирует цифры и расхождения с эталоном и не подбирает баланс вслепую. Решение о балансе принимает автор.
- **US14-AC2:**
  - `tools/DeepSpaceSaga.CombatAnalyzer` добавлен в репозиторий (вне `DeepSpaceSaga.sln` или в отдельном solution folder) и переведён на новые команды.
  - Сборка закреплена `global.json`.
- **US14-AC3:** Чек-лист нативной приёмки по всем UI-критериям этого эпика пройден в реальном окне, либо пункт помечен `NOT RUN` с причиной.



## Non-goals / Invariants

Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Scope этой истории ограничен её AC и allowlists; соседние истории не выполняются неявно. D-USER-02 имеет приоритет над исходным<=100ms projectile требованием.

## Dependencies

Execution order задаётся ticket DAG, который допускает чередование слоёв между историями. Stories — тематические контейнеры, не атомарные build units; metadata dependencies перечисляет полностью завершённые истории до начала этой; обязательные межслойные depends_on отдельных тикетов дополняют этот список. Входящие prerequisite stories: EP-0012-US-0016-combat-localization. Нативные открытые gates EP11 не объявляются закрытыми EP12.

## Ticket map

| Order / Ticket | Layer | Depends on | Serves | Files |
|---|---|---|---|---:|
| 66 / [EP-0012-US-0014-TK-0001-production-balance-harness](EP-0012-US-0014-TK-0001-production-balance-harness/EP-0012-US-0014-TK-0001-production-balance-harness.md) | engine | EP-0012-US-0016-TK-0001-combat-localization | US14-AC1 | 4 |
| 67 / [EP-0012-US-0014-TK-0002-tools-analyzer](EP-0012-US-0014-TK-0002-tools-analyzer/EP-0012-US-0014-TK-0002-tools-analyzer.md) | tooling | EP-0012-US-0014-TK-0001-production-balance-harness | US14-AC2 | 4 |
| 68 / [EP-0012-US-0014-TK-0003-native-acceptance](EP-0012-US-0014-TK-0003-native-acceptance/EP-0012-US-0014-TK-0003-native-acceptance.md) | tooling | EP-0012-US-0014-TK-0002-tools-analyzer | US14-AC3 | 1 |

## Decisions / Assumptions

D01–D18/O1–O12 входного документа и D-USER-01/02, A01–A06 и A-PLAN-01–12 из Documentation обязательны в указанном статусе. Новые типы, partial и tests — proposed.

## Gaps and backlog

G-01–G-06 из Documentation. Все missing consumer references блокируют закрытие соответствующего тикета, без неограниченных проходов. Native/product проверяются отдельно; planning не даёт их evidence.

## Decision / review log

2026-10-20T07:48:46.640Z: создано по поручению пользователя. Разделение слоёв/<=5 файлов — техническая декомпозиция. Stage draft, implementation NOT STARTED, проверки кода не запускались.
