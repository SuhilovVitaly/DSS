# Исправления по ревью EP-0001–0004 — 2026-10-10

Это продолжение [исходного ревью](README.md) по просьбе исправить найденные проблемы. Исходные F/G сохраняют номера; первоначальные измерения в README/Validation относятся к состоянию до исправлений.

## Исправленный код

| Finding | Изменение | Регрессионная проверка |
|---|---|---|
| F01 | Оба balance runner создают реальную конфигурацию cargo capacity в Engine registry. Обычный runner больше не умножает аналитическую вместимость после выдачи quote. Расчёт использует Int128 до checked conversion. | `Cargo_upgrade_increases_authoritative_capacity_quote_and_executed_batch`: два обычных cases и отдельный ограниченный трюм; увеличиваются authoritative capacity, quote maximum и реально купленная партия. |
| F02 | Load сравнивает аналитические координаты станций в общей эпохе и отклоняет совпадающие орбиты до публикации мира. Исторические тесные исходные группы остаются допустимы. | `Coincident_station_orbits_are_rejected_atomically_even_with_distinct_serialized_positions`: согласованно повреждённые orbit/descriptor, оба варианта serialized positions, исходный мир неизменен. |
| F03 | Тип двигателя определяется по content, operational state и canonical ModuleId order. Генераторы системы/кластеров, snapshot Vmax и команды движения поддерживают custom engine TypeId. | `Custom_engine_type_uses_content_speed_and_executes_motion`: 1.4 km/s, рабочий/выключенный/разрушенный двигатель, реальное ускорение; существующие EngineCommand/Approach/cluster voyage tests. |
| F04 | Вход генератора ограничен: attempts <=256, asteroids/belt <=1024, decorations/belt <=65536; attempts² × maxBelts × asteroids <=10 000 000. Произведение учитывает внешний retry и внутренние попытки астероида. | `Generation_work_is_bounded_before_replacing_the_world`: границы, превышения, int.MaxValue, совместный бюджет и atomic rejection. |
| F05 | Ожидание snapshot отменяет сам enumerator и дожидается MoveNextAsync. Таймаут содержит ожидаемый predicate и время последнего snapshot; DisposeAsync больше не гоняется с незавершённым чтением. | `Snapshot_timeout_completes_pending_read_before_disposal`; весь Client suite, включая пять сценариев docking/trade. |

Дополнительно Finance показывает authoritative PlayerCredits и понятную подсказку перехода к ценам на Trade вместо устаревших `not available yet`.

## Интеграция EP-0004

G01 закрыт для локальной рабочей копии: delta `4233f52..76f500a` из `codex/ep-0004-ai-territories` перенесена в `D:/DeepSpaceSaga/DSS`. Интегрированы Contracts, Engine, Client, content, tests, tooling, Board и актуальная документация. Частичный EP-0008 и его незавершённые Board-изменения сохранены. Это незакоммиченный рабочий набор; merge commit/push не выполнялись. Старый graphify-out ветки EP-0004 не копируется поверх актуального графа.

## Оставшиеся критерии приёмки

- G02: native 80 FPS остаётся OPEN. Свежие измерения и ограничения описаны в `fix-validation.json`; CPU submit и ожидание swap представлены отдельно. Изменение порога или отключение VSync ради зелёного отчёта не выполнялось.
- G03: после F01 полная неизменённая 24-case/240h матрица всё ещё даёт 125 нарушений: 108 route_margin_dominance, 12 short_margin_band, 2 event_did_not_change_leader, 2 route_always_best, 1 long_upgrade_crossover. Пороговые значения evaluator сохранены. Свободный трюм порядка 98 800 кг; небольшие бюджеты/запасы ограничивают партии раньше него. Пользователю предложен выбор масштаба экономики (100-тонный трюм и крупные рынки либо небольшой стартовый трюм). Без выбранного направления продуктовый баланс не объявляется исправленным.
- G04: 100-дневный cluster runner подтверждает correctness/continuation, использует единичные партии и удержание в пути. Его результат не является экономической приёмкой; целевые показатели прибыльности дальнего рейса пока не определены.
- G05: scripted native interaction и автоматические проверки не заменяют человеческое прохождение, GPU execution и физический scanout. Эти критерии не объявляются пройденными.

## Проверки

Машиночитаемые результаты, source hashes и команды: [fix-validation.json](fix-validation.json). Финальные сборки и проверки выполняются на объединённом рабочем наборе. Промежуточные неуспешные проверки при подготовке регрессий не считаются финальными результатами. Финальный Graphify включает интегрированный EP-0004 и исправления; он служит навигацией, а не runtime evidence.
