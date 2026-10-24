---
epic: EP-0010-combat-reports
title: "Панель графических боевых докладов экипажа"
stage: acceptance-open
created: 2026-10-16
revision: 1
depends_on_epic: EP-0009-module-combat
validation_status: headless-passed-native-open
story_count: 12
ticket_count: 30
---

# EP-0010 — Панель графических боевых докладов

Как капитан Тетрара, я получаю графические доклады экипажа о дальности, пусках и результатах боя с тремя Мухами. Справа под информацией об объектах находится аккордеон: одна текущая карточка открыта, прошлые доступны по заголовкам. Существующий боевой журнал остаётся отдельным источником подробностей.

## Канонический контракт

[Полное ТЗ](https://github.com/SuhilovVitaly/DSS/blob/aa8750dbac35f25b82b3ec44cbe5011fc02740a0/Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md) — продуктовые R01–R24 и события E01–E07; технические A01–A10 явно отделены от решений пользователя. [Grounding и ссылки](Documentation.md), [порядок и трассировка](Tickets.md), [статус реализации](ImplementationStatus.md).

## Статус и полномочия

12 историй /30 тикетов (27 исходных +3 corrective). Production implementation и8 PNG готовы; Engine2197, Contracts210, Client1973 headless cases PASS (baseline trading timeout исключён и подтверждён отдельно). Каждый тикет публикуется отдельным commit/push; финальная документация/Graphify — US12. Полный эпик остаётся **acceptance-open**: native matrix, human art/gameplay и FPS OPEN. Самостоятельный запуск игры запрещён пользователем. [Полный статус](ImplementationStatus.md), [тикет → SHA → push](Publication.md), [инвентаризация](DocumentationInventory.md). EP-0009 переиспользуется как baseline, незавершённые native/balance gates предшественника не считаются закрытыми этим эпиком.

## Scope

E01 первый вход известного enemy в configured range; E02 вражеский пуск; E03 наш торпедный пуск; E04 наш пуск ПРО; E05 успешный перехват; E06 попадание/уничтожение enemy; E07 повреждение модулей игрока. Восемь иллюстраций, captured докладчики, local delivery, последние200 карточек, Save/Load range marks, right-column layout и отдельная приёмка.

## Non-goals

Озвучка, ответы, автопауза, фильтры, новая модель обнаружения, изменение оружия/AI/баланса, замена журнала. Публикация и Graphify разрешены execution-поручением; граф используется для навигации, не как доказательство runtime acceptance.

## Истории

| Story | Пользовательский результат | Тикеты | Требования |
|---|---|---|---|
| US0001 | [Авторитетный контракт боевого доклада](EP-0010-US-0001-report-contracts/EP-0010-US-0001-report-contracts.md) | 2 | R11, R12, R16, R17 |
| US0002 | [Первый доклад о входе противника в дальность](EP-0010-US-0002-range-reports/EP-0010-US-0002-range-reports.md) | 2 | R08, R16, R18, R20, R21 |
| US0003 | [Доклады экипажа о фактических пусках](EP-0010-US-0003-launch-reports/EP-0010-US-0003-launch-reports.md) | 2 | R09, R11, R12, R16, R18 |
| US0004 | [Подтверждённые исходы и повреждения](EP-0010-US-0004-combat-result-reports/EP-0010-US-0004-combat-result-reports.md) | 2 | R10, R11, R12, R16, R21 |
| US0005 | [Сохранение отметок и чистая загрузка](EP-0010-US-0005-report-save-continuation/EP-0010-US-0005-report-save-continuation.md) | 2 | R17, R20, R21 |
| US0006 | [Доставка докладов при пропуске snapshots](EP-0010-US-0006-reliable-report-delivery/EP-0010-US-0006-reliable-report-delivery.md) | 2 | R07, R16, R17, R19, R20 |
| US0007 | [История карточек и локализованные доклады](EP-0010-US-0007-report-history-and-text/EP-0010-US-0007-report-history-and-text.md) | 2 | R03, R04, R05, R07, R11, R12, R13, R19, R20 |
| US0008 | [Графические доклады в стиле DSS](EP-0010-US-0008-report-illustrations/EP-0010-US-0008-report-illustrations.md) | 4 | R12, R14, R15, R22 |
| US0009 | [Карточки аккордеона и чтение докладов](EP-0010-US-0009-report-accordion/EP-0010-US-0009-report-accordion.md) | 2 | R04, R05, R06, R07, R13, R15, R22 |
| US0010 | [Правая колонка и жизненный цикл экрана](EP-0010-US-0010-tactical-screen-integration/EP-0010-US-0010-tactical-screen-integration.md) | 3 | R01, R02, R03, R06, R13, R20, R21, R22 |
| US0011 | [Воспроизводимая приёмка боя и интерфейса](EP-0010-US-0011-report-acceptance/EP-0010-US-0011-report-acceptance.md) | 6 | R08, R09, R10, R11, R17, R19, R20, R21, R22, R23, R24 |
| US0012 | [Документация и честный статус поставки](EP-0010-US-0012-documentation-and-handoff/EP-0010-US-0012-documentation-and-handoff.md) | 1 | R01, R02, R03, R04, R05, R06, R07, R08, R09, R10, R11, R12, R13, R14, R15, R16, R17, R18, R19, R20, R21, R22, R23, R24 |

## Порядок и Definition of Done

Выполнять по DAG в Tickets.md; один тикет — один layer и максимум 5 файлов (включая тесты/новые assets). Для каждого критерия есть тикет и именованные проверки. Art8/8, E01–E07, Save/Load, доставка, UI/input и runtime output должны быть подтверждены отдельно. Документация синхронизирована с реальным поведением. Native/human/performance принимаются по evidence, а не по созданию плана. Любые оставшиеся gates явно OPEN/NOT RUN.

## Risks and backlog

- Старые pre-feature Save17 не содержат исторических range marks; A07 задаёт безопасный исходный baseline.
- Ship crew сейчас не имеет portrait field; role fallback обязателен, расширение crew model не требуется.
- Накопление всех событий в journal не заменяет ограниченную очередь новых report deltas.
- На малой высоте правой колонки требуется совместное распределение с Object Info, а не простое добавление ещё одной панели поверх HUD.
- Визуальный стиль и читаемость всей серии требуют реального просмотра; native/FPS состояние предшественников остаётся открытым.
