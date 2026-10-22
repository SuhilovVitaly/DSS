---
epic: EP-0014-module-panels
story: EP-0014-US-0005-snapshot-compatibility
title: "Совместимость панелей и продолжение сохранённой игры"
stage: draft
dependencies: [EP-0014-US-0001, EP-0014-US-0002, EP-0014-US-0004]
---

# Совместимость панелей и продолжение сохранённой игры

## Связь с техническим заданием

§8, M07/M09, P12, исходная US5. Полный источник, текущие факты и D/A/G см. в [Documentation.md](../Documentation.md).
Требования ниже — целевой результат; это не утверждение о наличии реализации.

## User story

Как разработчик интеграции, я получаю snapshot с актуальными сведениями панелей без устаревших обещаний о guided combat и автоматической ПРО. После перевода панелей лишние поля удаляются без поломки действующих consumers. Как игрок, я загружаю текущее сохранение или поддерживаемое Save18 и продолжаю тот же бой с сохранёнными циклами. Авторитет Engine и границы собственного знания сохраняются.

## Зависимости

EP-0014-US-0001, EP-0014-US-0002, EP-0014-US-0004. Нужны согласованные общие правила и завершённые результаты предшествующих историй.

## Completion evidence

- **AC-01.** В snapshot доступны точное penetration своего аппарата и authoritative destroyed limit своего корабля; данные другого владельца не раскрываются сверх текущего player knowledge.

- **AC-02.** После проверки consumers удалены legacy launcher SpeedKmS/TurnRateDegPerSec/Maneuverability/ModeId/IsBrokenUntilDock, installed TorpedoMaxFlightTimeMs, defense Accuracy/SpeedKmS/TurnRateDegPerSec/MaxFlightTimeMs/Barrels/PendingBurstShots/ManualRangeKm/AutoEnabled. ActiveTorpedoIds удаляется только при доказанном отсутствии потребителей; иначе решение сохранения документируется.

- **AC-03.** Serializer не публикует удалённые snapshot members; semantic search по типизированным consumers не находит оставшихся обращений. Одноимённые поля иных DTO не считаются ошибкой и не удаляются по одному grep.

- **AC-04.** Save19 загружается, Save18 проходит поддерживаемую миграцию; при продолжении сохраняются RNG/counters, captured flight data, reload deadlines, overrides и очереди. Нужные legacy save members остаются в save DTO.

- **AC-05.** Matching Contracts/Engine/Client проверки и сборка подтверждают целостность итогового контракта; неверные комментарии отсутствуют. Эта история поставляется после panel migration, без обратной зависимости панелей на cleanup.

Это план будущих наблюдений, а не выполненная проверка. Automated/render,
native/input и product/human acceptance фиксируются раздельно.

## Ограничения и gaps

G-01 и G-05. Смешанные Contracts+Engine тикеты исходника недопустимы: последующая детализация должна разделить owning layers и matching test projects в пределах пяти тикетов. Точный impact positional constructors пока не определён.

Тикеты отсутствуют. Будущая нарезка: максимум пять тикетов, один production
layer и matching test project, максимум пять implementation files на тикет.
Engine не вызывается из Render; скрытые NPC данные не используются.
Проверки при реализации имеют process-tree cap300000ms и абсолютные
output paths в D:/DeepSpaceSaga/artifacts/validation/ep-0014-module-panels/<unique-run>/.

## Review

Draft; human approval отсутствует. Решения и точные сообщения согласования
будут записаны в review log эпика.
## Execution ticket map2026-10-22

Execution authorized through epic-executor; see [current ticket map](../Tickets.md). Earlier planning-only/no-tickets statements above are historical. Stage remains distinct from human acceptance. Final US-0006 must audit and update all impacted project documentation and rebuild Graphify after final code and text.
