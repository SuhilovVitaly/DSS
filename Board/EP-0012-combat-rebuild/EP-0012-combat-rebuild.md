---
epic: "EP-0012-combat-rebuild"
title: "Ребилд тактического боя"
stage: "draft"
created: "2026-10-20T07:48:46.640Z"
source_request: "Создать эпик, истории и тикеты по приложению, игнорируя внешние файлы"
revision: 1
depends_on_epic: "EP-0011-short-fixes"
story_count: 19
ticket_count: 73
validation_status: "planning-only"
---

# EP-0012 — Ребилд тактического боя

## Исполнение — 2026-10-20

По новому прямому поручению начато автономное исполнение по [EpicExecutionPrompt](../EpicExecutionPrompt.md). Текущие проверки, ограничения и публикации ведутся в [журнале исполнения](ImplementationStatus.md). Исходные planning-записи ниже сохраняют исторический статус подготовки; они не отменяют новых полномочий. Тикеты пока не реализованы, приёмка OPEN.

Торпеды и противоракеты летят в выбранную точку; исход определяют геометрия взрыва, ручная ПРО и уклонение. Новый UI даёт прицеливание, панель угроз, состояние модулей и итоги боя.

[Полный контракт, D/A/G и grounding](Documentation.md) · [Порядок тикетов и трассировка](Tickets.md)

**19 историй / 73 тикетов**, все draft. Созданы только planning-документы. Внешние файлы не требуются. UI feedback<=100мс; снаряд появляется на общих подтверждённых часах сцены (ответ пользователя «1»).

## Истории

| Story | Результат | Tickets |
|---|---|---:|
| [US-0001](EP-0012-US-0001-contracts-and-schema/EP-0012-US-0001-contracts-and-schema.md) | Контракты и схема данных | 5 |
| [US-0002](EP-0012-US-0002-combat-catalog-v3/EP-0012-US-0002-combat-catalog-v3.md) | Каталог v3 | 5 |
| [US-0003](EP-0012-US-0003-point-aim-torpedoes/EP-0012-US-0003-point-aim-torpedoes.md) | Торпеда в точку | 5 |
| [US-0004](EP-0012-US-0004-point-aim-countermissiles/EP-0012-US-0004-point-aim-countermissiles.md) | Противоракеты | 3 |
| [US-0005](EP-0012-US-0005-snapshot-privacy/EP-0012-US-0005-snapshot-privacy.md) | Сокрытие информации | 2 |
| [US-0006](EP-0012-US-0006-threats-and-evasion/EP-0012-US-0006-threats-and-evasion.md) | Угрозы и уклонение | 3 |
| [US-0007](EP-0012-US-0007-mukha-ai-v3/EP-0012-US-0007-mukha-ai-v3.md) | ИИ Мухи v3 | 3 |
| [US-0008](EP-0012-US-0008-encounter-and-time/EP-0012-US-0008-encounter-and-time.md) | Боевое столкновение и время | 3 |
| [US-0009](EP-0012-US-0009-scenarios-and-saves/EP-0012-US-0009-scenarios-and-saves.md) | Сценарии и сохранения | 5 |
| [US-0010](EP-0012-US-0010-tactical-map/EP-0012-US-0010-tactical-map.md) | Тактическая карта | 4 |
| [US-0011](EP-0012-US-0011-aiming-and-launch/EP-0012-US-0011-aiming-and-launch.md) | Прицеливание и пуск | 4 |
| [US-0012](EP-0012-US-0012-maneuver-and-threat-ui/EP-0012-US-0012-maneuver-and-threat-ui.md) | Манёвр и угрозы в UI | 3 |
| [US-0013](EP-0012-US-0013-combat-state-and-feedback/EP-0012-US-0013-combat-state-and-feedback.md) | Состояние, панели, обратная связь | 5 |
| [US-0014](EP-0012-US-0014-balance-and-acceptance/EP-0012-US-0014-balance-and-acceptance.md) | Приёмка и баланс | 3 |
| [US-0015](EP-0012-US-0015-project-documentation/EP-0012-US-0015-project-documentation.md) | Документация (обязательная завершающая история) | 5 |
| [US-0016](EP-0012-US-0016-combat-localization/EP-0012-US-0016-combat-localization.md) | Локализация боевого интерфейса | 1 |
| [US-0017](EP-0012-US-0017-event-and-command-integration/EP-0012-US-0017-event-and-command-integration.md) | Контракты событий, серверные доклады и доставка | 4 |
| [US-0018](EP-0012-US-0018-legacy-combat-cleanup/EP-0012-US-0018-legacy-combat-cleanup.md) | Удаление старой боевой модели и регрессии | 5 |
| [US-0019](EP-0012-US-0019-scenario-and-client-regressions/EP-0012-US-0019-scenario-and-client-regressions.md) | Совместимость сценариев и старых клиентских поверхностей | 5 |

## Границы и статус

Базовая модель HP/брони/гибели, determinism, physical clock, EP10 reports и наблюдаемые NPC trajectories переиспользуются; оружие/AI/бой/save18/UI перепроектируются в пределах AC. Basic engine не меняется, Tetrarch получает отдельный; CombatShmel удаляется при реализации, CombatSim не меняется; HostileCombat пока не скрывается.

Полномочия этого поручения — Board planning. Код, игра, build/test, commit/push не выполнялись. Implementation/automated/native/product/balance OPEN. Перед исполнением обязательна сверка всех breaking consumers по G-02; draft не объявляется полностью execution-ready.
