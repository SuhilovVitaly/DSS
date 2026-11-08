---
epic: EP-0020-tactical-battle-mechanics
stage: draft
created: 2026-11-08T11:46:28Z
---

# EP-0020 — карта тикетов

[Эпик и решения](Documentation.md) · [UiSpec](UiSpec.md) · 13 историй, 44 тикетов. Механика M1 «Уклонение и сознание» — US-0001…US-0004; M2 «Ловушки» — US-0005…US-0007; M3 «Аномалии» — US-0008…US-0010; M4 «Пилот и сборка» — US-0011…US-0012; US-0013 — документация, всегда последняя. Порядок строк соблюдает зависимости.

**Весь эпик исполняется после полного выполнения и приёмки [EP-0019](../EP-0019-tactical-battle-ui-redesign/Tickets.md).**

## Истории

| ID | Файл | Тикетов | Dependencies |
|---|---|---|---|
| US-0001 | [EP-0020-US-0001-consciousness](EP-0020-US-0001-consciousness/EP-0020-US-0001-consciousness.md) | 2 | EP-0019 |
| US-0002 | [EP-0020-US-0002-evasion-resolution](EP-0020-US-0002-evasion-resolution/EP-0020-US-0002-evasion-resolution.md) | 5 | US-0001 |
| US-0003 | [EP-0020-US-0003-evasion-calibration](EP-0020-US-0003-evasion-calibration/EP-0020-US-0003-evasion-calibration.md) | 1 | US-0002 |
| US-0004 | [EP-0020-US-0004-evasion-ui](EP-0020-US-0004-evasion-ui/EP-0020-US-0004-evasion-ui.md) | 4 | US-0002, EP-0019 |
| US-0005 | [EP-0020-US-0005-decoy-module](EP-0020-US-0005-decoy-module/EP-0020-US-0005-decoy-module.md) | 2 | US-0002 |
| US-0006 | [EP-0020-US-0006-decoy-resolution](EP-0020-US-0006-decoy-resolution/EP-0020-US-0006-decoy-resolution.md) | 5 | US-0005 |
| US-0007 | [EP-0020-US-0007-decoy-ui](EP-0020-US-0007-decoy-ui/EP-0020-US-0007-decoy-ui.md) | 4 | US-0006, US-0004 |
| US-0008 | [EP-0020-US-0008-anomaly-field](EP-0020-US-0008-anomaly-field/EP-0020-US-0008-anomaly-field.md) | 3 | US-0006 |
| US-0009 | [EP-0020-US-0009-anomaly-resolution](EP-0020-US-0009-anomaly-resolution/EP-0020-US-0009-anomaly-resolution.md) | 5 | US-0008 |
| US-0010 | [EP-0020-US-0010-anomaly-ui](EP-0020-US-0010-anomaly-ui/EP-0020-US-0010-anomaly-ui.md) | 4 | US-0009, US-0007 |
| US-0011 | [EP-0020-US-0011-build-slots](EP-0020-US-0011-build-slots/EP-0020-US-0011-build-slots.md) | 2 | US-0009 |
| US-0012 | [EP-0020-US-0012-build-card-ui](EP-0020-US-0012-build-card-ui/EP-0020-US-0012-build-card-ui.md) | 3 | US-0011, US-0010 |
| US-0013 | [EP-0020-US-0013-project-documentation](EP-0020-US-0013-project-documentation/EP-0020-US-0013-project-documentation.md) | 4 | US-0003, US-0004, US-0006, US-0007, US-0009, US-0010, US-0012 |

## Тикеты

| ID | Название | Слой | Зависимости | Критерии |
|---|---|---|---|---|
| [EP-0020-US-0001-TK-0001-rules-content](EP-0020-US-0001-consciousness/EP-0020-US-0001-TK-0001-rules-content/EP-0020-US-0001-TK-0001-rules-content.md) | Правила сознания и уклонения в данных | content-data | [] | [US1-AC1] |
| [EP-0020-US-0001-TK-0002-consciousness-state](EP-0020-US-0001-consciousness/EP-0020-US-0001-TK-0002-consciousness-state/EP-0020-US-0001-TK-0002-consciousness-state.md) | Сознание в состоянии боя и снимках | engine | [EP-0020-US-0001-TK-0001-rules-content] | [US1-AC2, US1-AC3] |
| [EP-0020-US-0002-TK-0001-evasion-contracts](EP-0020-US-0002-evasion-resolution/EP-0020-US-0002-TK-0001-evasion-contracts/EP-0020-US-0002-TK-0001-evasion-contracts.md) | Контракты уклонения | contracts | [EP-0020-US-0001-TK-0002-consciousness-state] | [US2-AC1, US2-AC6] |
| [EP-0020-US-0002-TK-0002-evasion-hit-term](EP-0020-US-0002-evasion-resolution/EP-0020-US-0002-TK-0002-evasion-hit-term/EP-0020-US-0002-TK-0002-evasion-hit-term.md) | Слагаемое уклонения в шансе попадания | motion | [EP-0020-US-0002-TK-0001-evasion-contracts] | [US2-AC2] |
| [EP-0020-US-0002-TK-0003-salvo-evasion](EP-0020-US-0002-evasion-resolution/EP-0020-US-0002-TK-0003-salvo-evasion/EP-0020-US-0002-TK-0003-salvo-evasion.md) | Уклонение в разрешении залпа и правило ИИ | engine | [EP-0020-US-0002-TK-0002-evasion-hit-term] | [US2-AC2, US2-AC3, US2-AC4] |
| [EP-0020-US-0002-TK-0004-session-evasion](EP-0020-US-0002-evasion-resolution/EP-0020-US-0002-TK-0004-session-evasion/EP-0020-US-0002-TK-0004-session-evasion.md) | Сессия: приказ уклонения и открытие фазы стрельбы | engine | [EP-0020-US-0002-TK-0003-salvo-evasion] | [US2-AC1, US2-AC5, US2-AC6] |
| [EP-0020-US-0002-TK-0005-evasion-regression](EP-0020-US-0002-evasion-resolution/EP-0020-US-0002-TK-0005-evasion-regression/EP-0020-US-0002-TK-0005-evasion-regression.md) | Регрессия боя с уклонением | engine | [EP-0020-US-0002-TK-0004-session-evasion] | [US2-AC7] |
| [EP-0020-US-0003-TK-0001-evasion-calibration](EP-0020-US-0003-evasion-calibration/EP-0020-US-0003-TK-0001-evasion-calibration/EP-0020-US-0003-TK-0001-evasion-calibration.md) | Прогон калибровки и отчёт | tooling | [EP-0020-US-0002-TK-0005-evasion-regression] | [US3-AC1, US3-AC2, US3-AC3] |
| [EP-0020-US-0004-TK-0001-fire-orders-evasion](EP-0020-US-0004-evasion-ui/EP-0020-US-0004-TK-0001-fire-orders-evasion/EP-0020-US-0004-TK-0001-fire-orders-evasion.md) | Модель приказов: уклонение, клавиша E, сознание на карточке | client | [EP-0020-US-0002-TK-0004-session-evasion, EP-0019-tactical-battle-ui-redesign] | [US4-AC1, US4-AC2] |
| [EP-0020-US-0004-TK-0002-fire-panel-evasion](EP-0020-US-0004-evasion-ui/EP-0020-US-0004-TK-0002-fire-panel-evasion/EP-0020-US-0004-TK-0002-fire-panel-evasion.md) | Панель стрельбы, кольцо уклонения, строки | client | [EP-0020-US-0004-TK-0001-fire-orders-evasion] | [US4-AC2] |
| [EP-0020-US-0004-TK-0003-results-evasion-reveal](EP-0020-US-0004-evasion-ui/EP-0020-US-0004-TK-0003-results-evasion-reveal/EP-0020-US-0004-TK-0003-results-evasion-reveal.md) | Раскрытие уклонения в залпе и результатах | client | [EP-0020-US-0004-TK-0002-fire-panel-evasion] | [US4-AC3] |
| [EP-0020-US-0004-TK-0004-view-regression](EP-0020-US-0004-evasion-ui/EP-0020-US-0004-TK-0004-view-regression/EP-0020-US-0004-TK-0004-view-regression.md) | Проверки вида EP-0019 после уклонения | tests | [EP-0020-US-0004-TK-0003-results-evasion-reveal] | [US4-AC4] |
| [EP-0020-US-0005-TK-0001-decoy-content](EP-0020-US-0005-decoy-module/EP-0020-US-0005-TK-0001-decoy-content/EP-0020-US-0005-TK-0001-decoy-content.md) | Модуль ловушек, слот рамы и порог ИИ в данных | content-data | [EP-0020-US-0001-TK-0001-rules-content] | [US5-AC1, US5-AC2] |
| [EP-0020-US-0005-TK-0002-decoy-state](EP-0020-US-0005-decoy-module/EP-0020-US-0005-TK-0002-decoy-state/EP-0020-US-0005-TK-0002-decoy-state.md) | Модуль и заряды в состоянии боя, масса, снимки | engine | [EP-0020-US-0005-TK-0001-decoy-content, EP-0020-US-0002-TK-0005-evasion-regression] | [US5-AC3, US5-AC4] |
| [EP-0020-US-0006-TK-0001-decoy-contracts](EP-0020-US-0006-decoy-resolution/EP-0020-US-0006-TK-0001-decoy-contracts/EP-0020-US-0006-TK-0001-decoy-contracts.md) | Контракты ловушки | contracts | [EP-0020-US-0005-TK-0002-decoy-state, EP-0020-US-0002-TK-0001-evasion-contracts] | [US6-AC1, US6-AC5] |
| [EP-0020-US-0006-TK-0002-decoy-hit-transform](EP-0020-US-0006-decoy-resolution/EP-0020-US-0006-TK-0002-decoy-hit-transform/EP-0020-US-0006-TK-0002-decoy-hit-transform.md) | Преобразование шанса ловушкой | motion | [EP-0020-US-0006-TK-0001-decoy-contracts] | [US6-AC2] |
| [EP-0020-US-0006-TK-0003-salvo-decoy](EP-0020-US-0006-decoy-resolution/EP-0020-US-0006-TK-0003-salvo-decoy/EP-0020-US-0006-TK-0003-salvo-decoy.md) | Ловушки в разрешении залпа и правило ИИ | engine | [EP-0020-US-0006-TK-0002-decoy-hit-transform] | [US6-AC2, US6-AC3, US6-AC4, US6-AC5] |
| [EP-0020-US-0006-TK-0004-session-decoy](EP-0020-US-0006-decoy-resolution/EP-0020-US-0006-TK-0004-session-decoy/EP-0020-US-0006-TK-0004-session-decoy.md) | Сессия: приказ ловушки и скрытие до результатов | engine | [EP-0020-US-0006-TK-0003-salvo-decoy] | [US6-AC1, US6-AC5] |
| [EP-0020-US-0006-TK-0005-decoy-regression](EP-0020-US-0006-decoy-resolution/EP-0020-US-0006-TK-0005-decoy-regression/EP-0020-US-0006-TK-0005-decoy-regression.md) | Регрессия боя с ловушками и отчёт | engine | [EP-0020-US-0006-TK-0004-session-decoy] | [US6-AC6] |
| [EP-0020-US-0007-TK-0001-fire-orders-decoy](EP-0020-US-0007-decoy-ui/EP-0020-US-0007-TK-0001-fire-orders-decoy/EP-0020-US-0007-TK-0001-fire-orders-decoy.md) | Модель приказов: ловушка, клавиша D, заряды на карточке | client | [EP-0020-US-0006-TK-0004-session-decoy, EP-0020-US-0004-TK-0004-view-regression] | [US7-AC1, US7-AC2] |
| [EP-0020-US-0007-TK-0002-fire-panel-decoy](EP-0020-US-0007-decoy-ui/EP-0020-US-0007-TK-0002-fire-panel-decoy/EP-0020-US-0007-TK-0002-fire-panel-decoy.md) | Строка ловушки, значок карточки, метка на карте, строки | client | [EP-0020-US-0007-TK-0001-fire-orders-decoy] | [US7-AC2] |
| [EP-0020-US-0007-TK-0003-salvo-decoy-reveal](EP-0020-US-0007-decoy-ui/EP-0020-US-0007-TK-0003-salvo-decoy-reveal/EP-0020-US-0007-TK-0003-salvo-decoy-reveal.md) | Ловушки в полёте залпа и результатах | client | [EP-0020-US-0007-TK-0002-fire-panel-decoy] | [US7-AC3] |
| [EP-0020-US-0007-TK-0004-decoy-view-regression](EP-0020-US-0007-decoy-ui/EP-0020-US-0007-TK-0004-decoy-view-regression/EP-0020-US-0007-TK-0004-decoy-view-regression.md) | Проверки вида EP-0019 после ловушек | tests | [EP-0020-US-0007-TK-0003-salvo-decoy-reveal] | [US7-AC4] |
| [EP-0020-US-0008-TK-0001-anomaly-rules-content](EP-0020-US-0008-anomaly-field/EP-0020-US-0008-TK-0001-anomaly-rules-content/EP-0020-US-0008-TK-0001-anomaly-rules-content.md) | Правила аномалий в данных | content-data | [EP-0020-US-0005-TK-0001-decoy-content] | [US8-AC1] |
| [EP-0020-US-0008-TK-0002-anomaly-math](EP-0020-US-0008-anomaly-field/EP-0020-US-0008-TK-0002-anomaly-math/EP-0020-US-0008-TK-0002-anomaly-math.md) | Математика поля аномалий | motion | [EP-0020-US-0008-TK-0001-anomaly-rules-content] | [US8-AC2] |
| [EP-0020-US-0008-TK-0003-anomaly-generator](EP-0020-US-0008-anomaly-field/EP-0020-US-0008-TK-0003-anomaly-generator/EP-0020-US-0008-TK-0003-anomaly-generator.md) | Генерация аномалий из seed боя | engine | [EP-0020-US-0008-TK-0002-anomaly-math, EP-0020-US-0006-TK-0005-decoy-regression] | [US8-AC3] |
| [EP-0020-US-0009-TK-0001-anomaly-contracts](EP-0020-US-0009-anomaly-resolution/EP-0020-US-0009-TK-0001-anomaly-contracts/EP-0020-US-0009-TK-0001-anomaly-contracts.md) | Контракты поглощения и пыли | contracts | [EP-0020-US-0008-TK-0003-anomaly-generator] | [US9-AC3, US9-AC5] |
| [EP-0020-US-0009-TK-0002-salvo-anomalies](EP-0020-US-0009-anomaly-resolution/EP-0020-US-0009-TK-0002-salvo-anomalies/EP-0020-US-0009-TK-0002-salvo-anomalies.md) | Поглощение и пыль в залпе | engine | [EP-0020-US-0009-TK-0001-anomaly-contracts] | [US9-AC1, US9-AC2, US9-AC3] |
| [EP-0020-US-0009-TK-0003-ai-dust-avoidance](EP-0020-US-0009-anomaly-resolution/EP-0020-US-0009-TK-0003-ai-dust-avoidance/EP-0020-US-0009-TK-0003-ai-dust-avoidance.md) | ИИ: не заканчивать ход в пыли | engine | [EP-0020-US-0009-TK-0002-salvo-anomalies] | [US9-AC4] |
| [EP-0020-US-0009-TK-0004-session-anomalies](EP-0020-US-0009-anomaly-resolution/EP-0020-US-0009-TK-0004-session-anomalies/EP-0020-US-0009-TK-0004-session-anomalies.md) | Сессия: отказ в пыли и журнал | engine | [EP-0020-US-0009-TK-0003-ai-dust-avoidance] | [US9-AC3, US9-AC5] |
| [EP-0020-US-0009-TK-0005-anomaly-regression](EP-0020-US-0009-anomaly-resolution/EP-0020-US-0009-TK-0005-anomaly-regression/EP-0020-US-0009-TK-0005-anomaly-regression.md) | Регрессия боя с аномалиями и отчёт | engine | [EP-0020-US-0009-TK-0004-session-anomalies] | [US9-AC6] |
| [EP-0020-US-0010-TK-0001-anomaly-field-layer](EP-0020-US-0010-anomaly-ui/EP-0020-US-0010-TK-0001-anomaly-field-layer/EP-0020-US-0010-TK-0001-anomaly-field-layer.md) | Слой поля аномалий на карте | client | [EP-0020-US-0009-TK-0005-anomaly-regression, EP-0020-US-0007-TK-0004-decoy-view-regression] | [US10-AC1] |
| [EP-0020-US-0010-TK-0002-anomaly-move-fire](EP-0020-US-0010-anomaly-ui/EP-0020-US-0010-TK-0002-anomaly-move-fire/EP-0020-US-0010-TK-0002-anomaly-move-fire.md) | Аномалии в движении и стрельбе | client | [EP-0020-US-0010-TK-0001-anomaly-field-layer] | [US10-AC2] |
| [EP-0020-US-0010-TK-0003-anomaly-salvo-results](EP-0020-US-0010-anomaly-ui/EP-0020-US-0010-TK-0003-anomaly-salvo-results/EP-0020-US-0010-TK-0003-anomaly-salvo-results.md) | Аномалии в залпе, результатах и карточке наведения | client | [EP-0020-US-0010-TK-0002-anomaly-move-fire] | [US10-AC3] |
| [EP-0020-US-0010-TK-0004-anomaly-view-regression](EP-0020-US-0010-anomaly-ui/EP-0020-US-0010-TK-0004-anomaly-view-regression/EP-0020-US-0010-TK-0004-anomaly-view-regression.md) | Проверки вида EP-0019 после аномалий | tests | [EP-0020-US-0010-TK-0003-anomaly-salvo-results] | [US10-AC4] |
| [EP-0020-US-0011-TK-0001-slot-contracts](EP-0020-US-0011-build-slots/EP-0020-US-0011-TK-0001-slot-contracts/EP-0020-US-0011-TK-0001-slot-contracts.md) | Контракт слотов | contracts | [EP-0020-US-0009-TK-0005-anomaly-regression] | [US11-AC1] |
| [EP-0020-US-0011-TK-0002-slot-snapshot](EP-0020-US-0011-build-slots/EP-0020-US-0011-TK-0002-slot-snapshot/EP-0020-US-0011-TK-0002-slot-snapshot.md) | Заполнение слотов в снимке | engine | [EP-0020-US-0011-TK-0001-slot-contracts] | [US11-AC2, US11-AC3] |
| [EP-0020-US-0012-TK-0001-build-card-model](EP-0020-US-0012-build-card-ui/EP-0020-US-0012-TK-0001-build-card-model/EP-0020-US-0012-TK-0001-build-card-model.md) | Модель карточки «Пилот и сборка» | client | [EP-0020-US-0011-TK-0002-slot-snapshot, EP-0020-US-0010-TK-0004-anomaly-view-regression] | [US12-AC1] |
| [EP-0020-US-0012-TK-0002-build-card-render](EP-0020-US-0012-build-card-ui/EP-0020-US-0012-TK-0002-build-card-render/EP-0020-US-0012-TK-0002-build-card-render.md) | Отрисовка карточки и силуэтов рам | client | [EP-0020-US-0012-TK-0001-build-card-model] | [US12-AC2] |
| [EP-0020-US-0012-TK-0003-build-card-view-regression](EP-0020-US-0012-build-card-ui/EP-0020-US-0012-TK-0003-build-card-view-regression/EP-0020-US-0012-TK-0003-build-card-view-regression.md) | Проверки вида EP-0019 после карточки сборки | tests | [EP-0020-US-0012-TK-0002-build-card-render] | [US12-AC3] |
| [EP-0020-US-0013-TK-0001-mechanics-documentation](EP-0020-US-0013-project-documentation/EP-0020-US-0013-TK-0001-mechanics-documentation/EP-0020-US-0013-TK-0001-mechanics-documentation.md) | Документация уклонения и сознания | documentation | [EP-0020-US-0004-TK-0004-view-regression] | [US13-AC1, US13-AC2] |
| [EP-0020-US-0013-TK-0002-decoy-documentation](EP-0020-US-0013-project-documentation/EP-0020-US-0013-TK-0002-decoy-documentation/EP-0020-US-0013-TK-0002-decoy-documentation.md) | Документация ловушек | documentation | [EP-0020-US-0013-TK-0001-mechanics-documentation, EP-0020-US-0006-TK-0005-decoy-regression, EP-0020-US-0007-TK-0004-decoy-view-regression] | [US13-AC2, US13-AC3] |
| [EP-0020-US-0013-TK-0003-anomaly-documentation](EP-0020-US-0013-project-documentation/EP-0020-US-0013-TK-0003-anomaly-documentation/EP-0020-US-0013-TK-0003-anomaly-documentation.md) | Документация аномалий | documentation | [EP-0020-US-0013-TK-0002-decoy-documentation, EP-0020-US-0009-TK-0005-anomaly-regression, EP-0020-US-0010-TK-0004-anomaly-view-regression] | [US13-AC2, US13-AC4] |
| [EP-0020-US-0013-TK-0004-build-card-documentation](EP-0020-US-0013-project-documentation/EP-0020-US-0013-TK-0004-build-card-documentation/EP-0020-US-0013-TK-0004-build-card-documentation.md) | Документация слотов и карточки сборки | documentation | [EP-0020-US-0013-TK-0003-anomaly-documentation, EP-0020-US-0012-TK-0003-build-card-view-regression] | [US13-AC2, US13-AC5] |

## Контроль передачи

- **US-0001:** данные и состояние сознания без изменения исходов боёв.
- **US-0002:** механика в движке; ожидания исторических тестов обновляются только с записанной причиной.
- **US-0003:** отчёт калибровки для штрафа −20; данные не меняются.
- **US-0004:** интерфейс по UiSpec; проверки вида и регрессионные снимки EP-0019 зелёные, пробы UiSpec §7 проходят.
- **US-0005:** данные модуля и заряды; ожидания исторических тестов обновлены с причиной «масса модуля».
- **US-0006:** ловушки в движке; отчёт о действии ловушек; данные не меняются.
- **US-0007:** интерфейс по UiSpec §8–§14; проверки вида EP-0019 пересчитаны на новых данных seed 1.
- **US-0008:** правила, математика и генерация аномалий; основной поток RNG и исходы не меняются.
- **US-0009:** поглощение, пыль и ИИ в движке; отчёт о влиянии аномалий.
- **US-0010:** интерфейс по UiSpec §15–§21; проверки вида EP-0019 пересчитаны на бое с аномалиями.
- **US-0011:** слоты сборки в снимке; исходы не меняются.
- **US-0012:** карточка «Пилот и сборка» по UiSpec §22–§26; проверки вида EP-0019 обновлены.
- **US-0013:** документация и ExecutionStatus.

Блокирующие решения пользователя: [открытые вопросы](Documentation.md#открытые-вопросы).
