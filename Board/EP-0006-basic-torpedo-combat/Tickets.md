# Тикеты EP-0006 — Первый торпедный бой

Создано 2026-10-02T08:22:58Z: **8 историй, 27 тикетов**, все `stage: draft`. [Контракт эпика и журнал решений](Documentation.md).
Каждая история содержит не более пяти тикетов; каждый тикет — один production layer и максимум пять разрешённых файлов с тестами.

## Порядок и gates

US-0001 → US-0002 → US-0003 → US-0004 → US-0005 → US-0006 → US-0007 → US-0008.
Внутри — TK-0001 и далее. Все зависимости выражены полными ticket IDs в frontmatter; межисторийная цепочка также явная.
Переход к следующему тикету после реализации/review предыдущего; Board approval не подменяет наличие runtime. Планирование не является выполнением тикетов.

## US-0001 — Настраиваемые характеристики Тетрарха и торпеды

[История](EP-0006-US-0001-combat-configuration/EP-0006-US-0001-combat-configuration.md). Решения: D01, D04, D17.

| Тикет | Название | Layer | Files | Depends on | Serves |
|---|---|---|---:|---|---|
| [EP-0006-US-0001-TK-0001-combat-contract](EP-0006-US-0001-combat-configuration/EP-0006-US-0001-TK-0001-combat-contract/EP-0006-US-0001-TK-0001-combat-contract.md) | Контракт корабля, торпеды и занятости аппарата | contracts | 5 | none | AC-0001, AC-0002, AC-0003 |
| [EP-0006-US-0001-TK-0002-combat-content-loader](EP-0006-US-0001-combat-configuration/EP-0006-US-0001-TK-0002-combat-content-loader/EP-0006-US-0001-TK-0002-combat-content-loader.md) | Загрузка параметров класса корабля и торпедного модуля | engine | 5 | EP-0006-US-0001-TK-0001-combat-contract | AC-0001, AC-0002, AC-0003 |
| [EP-0006-US-0001-TK-0003-launcher-content](EP-0006-US-0001-combat-configuration/EP-0006-US-0001-TK-0003-launcher-content/EP-0006-US-0001-TK-0003-launcher-content.md) | Категория, реализация и команда торпедного аппарата | content-data | 4 | EP-0006-US-0001-TK-0002-combat-content-loader | AC-0002, AC-0003 |
| [EP-0006-US-0001-TK-0004-tetrarch-class-content](EP-0006-US-0001-combat-configuration/EP-0006-US-0001-TK-0004-tetrarch-class-content/EP-0006-US-0001-TK-0004-tetrarch-class-content.md) | Настройки прочности класса Тетрарх | content-data | 3 | EP-0006-US-0001-TK-0003-launcher-content | AC-0001, AC-0003 |

## US-0002 — Тетрарх получает помещение торпедного аппарата

[История](EP-0006-US-0002-tetrarch-launcher-room/EP-0006-US-0002-tetrarch-launcher-room.md). Решения: D01, D03, D17.

| Тикет | Название | Layer | Files | Depends on | Serves |
|---|---|---|---:|---|---|
| [EP-0006-US-0002-TK-0001-combat-bootstrap](EP-0006-US-0002-tetrarch-launcher-room/EP-0006-US-0002-TK-0001-combat-bootstrap/EP-0006-US-0002-TK-0001-combat-bootstrap.md) | Инициализация класса, HP и аппарата в Engine | engine | 5 | EP-0006-US-0001-TK-0004-tetrarch-class-content | AC-0001, AC-0002, AC-0003 |
| [EP-0006-US-0002-TK-0002-primary-scenario-loadout](EP-0006-US-0002-tetrarch-launcher-room/EP-0006-US-0002-TK-0002-primary-scenario-loadout/EP-0006-US-0002-TK-0002-primary-scenario-loadout.md) | Аппарат в боевом и основном сценариях | content-data | 4 | EP-0006-US-0002-TK-0001-combat-bootstrap | AC-0001, AC-0002 |
| [EP-0006-US-0002-TK-0003-remaining-tetrarch-loadouts](EP-0006-US-0002-tetrarch-launcher-room/EP-0006-US-0002-TK-0003-remaining-tetrarch-loadouts/EP-0006-US-0002-TK-0003-remaining-tetrarch-loadouts.md) | Согласование остальных стандартных Тетрархов | content-data | 4 | EP-0006-US-0002-TK-0002-primary-scenario-loadout | AC-0003 |

## US-0003 — Ручной пуск торпеды с упреждением

[История](EP-0006-US-0003-manual-guided-launch/EP-0006-US-0003-manual-guided-launch.md). Решения: D02, D04, D05, D06, D07.

| Тикет | Название | Layer | Files | Depends on | Serves |
|---|---|---|---:|---|---|
| [EP-0006-US-0003-TK-0001-torpedo-guidance](EP-0006-US-0003-manual-guided-launch/EP-0006-US-0003-TK-0001-torpedo-guidance/EP-0006-US-0003-TK-0001-torpedo-guidance.md) | Общая математика наведения на центр с упреждением | motion | 3 | EP-0006-US-0002-TK-0003-remaining-tetrarch-loadouts | AC-0001, AC-0002 |
| [EP-0006-US-0003-TK-0002-authoritative-launch](EP-0006-US-0003-manual-guided-launch/EP-0006-US-0003-TK-0002-authoritative-launch/EP-0006-US-0003-TK-0002-authoritative-launch.md) | Авторитетный пуск и занятость оператора | engine | 4 | EP-0006-US-0003-TK-0001-torpedo-guidance | AC-0001, AC-0002, AC-0003 |
| [EP-0006-US-0003-TK-0003-launcher-command-panel](EP-0006-US-0003-manual-guided-launch/EP-0006-US-0003-TK-0003-launcher-command-panel/EP-0006-US-0003-TK-0003-launcher-command-panel.md) | Отдельная панель аппарата и кнопка Пуск | client | 5 | EP-0006-US-0003-TK-0002-authoritative-launch | AC-0002, AC-0003 |

## US-0004 — Три попадания превращают Тетрарх во врек

[История](EP-0006-US-0004-impact-and-wreck/EP-0006-US-0004-impact-and-wreck.md). Решения: D08, D09, D14.

| Тикет | Название | Layer | Files | Depends on | Serves |
|---|---|---|---:|---|---|
| [EP-0006-US-0004-TK-0001-swept-impact-math](EP-0006-US-0004-impact-and-wreck/EP-0006-US-0004-TK-0001-swept-impact-math/EP-0006-US-0004-TK-0001-swept-impact-math.md) | Столкновение с центром между тиками | motion | 2 | EP-0006-US-0003-TK-0003-launcher-command-panel | AC-0001, AC-0004 |
| [EP-0006-US-0004-TK-0002-impact-event-contract](EP-0006-US-0004-impact-and-wreck/EP-0006-US-0004-TK-0002-impact-event-contract/EP-0006-US-0004-TK-0002-impact-event-contract.md) | Данные завершения полёта для интерфейса | contracts | 3 | EP-0006-US-0004-TK-0001-swept-impact-math | AC-0001, AC-0003 |
| [EP-0006-US-0004-TK-0003-authoritative-impacts](EP-0006-US-0004-impact-and-wreck/EP-0006-US-0004-TK-0003-authoritative-impacts/EP-0006-US-0004-TK-0003-authoritative-impacts.md) | Однократное попадание, урон и освобождение аппарата | engine | 4 | EP-0006-US-0004-TK-0002-impact-event-contract | AC-0001, AC-0002, AC-0004 |
| [EP-0006-US-0004-TK-0004-wreck-lifecycle](EP-0006-US-0004-impact-and-wreck/EP-0006-US-0004-TK-0004-wreck-lifecycle/EP-0006-US-0004-TK-0004-wreck-lifecycle.md) | Новый неподвижный врек и очистка ссылок на погибший корабль | engine | 4 | EP-0006-US-0004-TK-0003-authoritative-impacts | AC-0003, AC-0004 |

## US-0005 — Игрок видит торпеду, прочность, взрыв и врек

[История](EP-0006-US-0005-combat-map-feedback/EP-0006-US-0005-combat-map-feedback.md). Решения: D10, D11, D12, D14, D15, D16.

| Тикет | Название | Layer | Files | Depends on | Serves |
|---|---|---|---:|---|---|
| [EP-0006-US-0005-TK-0001-combat-palette-content](EP-0006-US-0005-combat-map-feedback/EP-0006-US-0005-TK-0001-combat-palette-content/EP-0006-US-0005-TK-0001-combat-palette-content.md) | Отдельный файл цветовой схемы боя | content-data | 2 | EP-0006-US-0004-TK-0004-wreck-lifecycle | AC-0001, AC-0002, AC-0003, AC-0004 |
| [EP-0006-US-0005-TK-0002-combat-palette-loading](EP-0006-US-0005-combat-map-feedback/EP-0006-US-0005-TK-0002-combat-palette-loading/EP-0006-US-0005-TK-0002-combat-palette-loading.md) | Загрузка палитры при запуске | client | 4 | EP-0006-US-0005-TK-0001-combat-palette-content | AC-0004 |
| [EP-0006-US-0005-TK-0003-combat-markers-and-hp](EP-0006-US-0005-combat-map-feedback/EP-0006-US-0005-TK-0003-combat-markers-and-hp/EP-0006-US-0005-TK-0003-combat-markers-and-hp.md) | Торпеда, врек и зелёная полоска HP | client | 5 | EP-0006-US-0005-TK-0002-combat-palette-loading | AC-0001, AC-0002 |
| [EP-0006-US-0005-TK-0004-impact-animation](EP-0006-US-0005-combat-map-feedback/EP-0006-US-0005-TK-0004-impact-animation/EP-0006-US-0005-TK-0004-impact-animation.md) | Двухсекундное кольцо взрыва на реальном времени | client | 4 | EP-0006-US-0005-TK-0003-combat-markers-and-hp | AC-0003 |
| [EP-0006-US-0005-TK-0005-torpedo-inspection](EP-0006-US-0005-combat-map-feedback/EP-0006-US-0005-TK-0005-torpedo-inspection/EP-0006-US-0005-TK-0005-torpedo-inspection.md) | Выделение торпеды и врека, данные торпеды | client | 5 | EP-0006-US-0005-TK-0004-impact-animation | AC-0004 |

## US-0006 — Пройденный путь, прогноз и предварительное наведение

[История](EP-0006-US-0006-torpedo-trajectories/EP-0006-US-0006-torpedo-trajectories.md). Решения: D10, D11, D13, D15.

| Тикет | Название | Layer | Files | Depends on | Serves |
|---|---|---|---:|---|---|
| [EP-0006-US-0006-TK-0001-authoritative-flight-history](EP-0006-US-0006-torpedo-trajectories/EP-0006-US-0006-TK-0001-authoritative-flight-history/EP-0006-US-0006-TK-0001-authoritative-flight-history.md) | Полная история полёта для отображения и сохранений | engine | 3 | EP-0006-US-0005-TK-0005-torpedo-inspection | AC-0001, AC-0003, AC-0004 |
| [EP-0006-US-0006-TK-0002-live-flight-trajectories](EP-0006-US-0006-torpedo-trajectories/EP-0006-US-0006-TK-0002-live-flight-trajectories/EP-0006-US-0006-TK-0002-live-flight-trajectories.md) | Постоянные линии полёта и точка встречи | client | 4 | EP-0006-US-0006-TK-0001-authoritative-flight-history | AC-0001, AC-0003, AC-0004 |
| [EP-0006-US-0006-TK-0003-launch-hover-preview](EP-0006-US-0006-torpedo-trajectories/EP-0006-US-0006-TK-0003-launch-hover-preview/EP-0006-US-0006-TK-0003-launch-hover-preview.md) | Серый прогноз при наведении на Пуск | client | 4 | EP-0006-US-0006-TK-0002-live-flight-trajectories | AC-0002, AC-0004 |

## US-0007 — Продолжение боя после сохранения и загрузки

[История](EP-0006-US-0007-combat-save-load/EP-0006-US-0007-combat-save-load.md). Решения: D12, D13, D17.

| Тикет | Название | Layer | Files | Depends on | Serves |
|---|---|---|---:|---|---|
| [EP-0006-US-0007-TK-0001-combat-save-schema](EP-0006-US-0007-combat-save-load/EP-0006-US-0007-TK-0001-combat-save-schema/EP-0006-US-0007-TK-0001-combat-save-schema.md) | Версия и проверка схемы сохранения боя | engine | 4 | EP-0006-US-0006-TK-0003-launch-hover-preview | AC-0001, AC-0002, AC-0003 |
| [EP-0006-US-0007-TK-0002-combat-persistence](EP-0006-US-0007-combat-save-load/EP-0006-US-0007-TK-0002-combat-persistence/EP-0006-US-0007-TK-0002-combat-persistence.md) | Атомарное сохранение и восстановление реального боя | engine | 4 | EP-0006-US-0007-TK-0001-combat-save-schema | AC-0001, AC-0002, AC-0003 |
| [EP-0006-US-0007-TK-0003-combat-presentation-resume](EP-0006-US-0007-combat-save-load/EP-0006-US-0007-TK-0003-combat-presentation-resume/EP-0006-US-0007-TK-0003-combat-presentation-resume.md) | Возобновление карты без повторных взрывов | client | 4 | EP-0006-US-0007-TK-0002-combat-persistence | AC-0001, AC-0004 |

## US-0008 — Полный бой в существующем сценарии

[История](EP-0006-US-0008-basic-combat-proof/EP-0006-US-0008-basic-combat-proof.md). Решения: D01–D17.

| Тикет | Название | Layer | Files | Depends on | Serves |
|---|---|---|---:|---|---|
| [EP-0006-US-0008-TK-0001-three-hit-integration](EP-0006-US-0008-basic-combat-proof/EP-0006-US-0008-TK-0001-three-hit-integration/EP-0006-US-0008-TK-0001-three-hit-integration.md) | Регрессионное доказательство трёх пусков | engine | 2 | EP-0006-US-0007-TK-0003-combat-presentation-resume | AC-0001, AC-0002 |
| [EP-0006-US-0008-TK-0002-native-combat-acceptance](EP-0006-US-0008-basic-combat-proof/EP-0006-US-0008-TK-0002-native-combat-acceptance/EP-0006-US-0008-TK-0002-native-combat-acceptance.md) | Проверка интерфейса и инструкция воспроизведения | client | 2 | EP-0006-US-0008-TK-0001-three-hit-integration | AC-0001, AC-0002, AC-0003 |

## Покрытие решений

| Решение | Основные истории |
|---|---|
| D01 — модуль, помещение, панель | US-0001, US-0002, US-0003 |
| D02 — один полёт, busy, infinite ammo | US-0003, US-0004, US-0007 |
| D03 — существующий сценарий, движение и пассивный пират | US-0002, US-0003, US-0008 |
| D04 — speed3, turn90, центр/курс, упреждение | US-0001, US-0003, US-0006 |
| D05 — выбранная цель, no self, target lock | US-0003, US-0005, US-0006 |
| D06 — pause/acceleration/paused fire | US-0003, US-0007, US-0008 |
| D07 — no range/TTL/self-destruct | US-0003 |
| D08 — collision500m, obstacles, no RNG | US-0004 |
| D09 — hull450/damage150/three hits/new wreck | US-0001, US-0004, US-0007 |
| D10 — видимая торпеда/линии/target path | US-0005, US-0006 |
| D11 — red ring и trail expiry2real sec | US-0005, US-0006 |
| D12 — persistence и отсутствие replay explosion | US-0007 |
| D13 — gray hover preview, полный путь | US-0006, US-0007 |
| D14 — HP/wreck/select/continue/no sounds | US-0004, US-0005, US-0008 |
| D15 — отдельный colors JSON/restart | US-0005, US-0006, US-0008 |
| D16 — torpedo info100% | US-0005 |
| D17 — параметры у своих content owners | US-0001, US-0007 |

## Статус проверки

Planning: PASS — проверены 37 файлов, 8 историй, 27 тикетов, 236 локальных ссылок, IDs/frontmatter/counts, dependency DAG, layer boundaries, allowlists и coverage.
Runtime: **не запускался**, поскольку эта задача создаёт только Board artifacts. Native smoke, integration run и Save/Load proof — будущие gates US-0008, а не факт текущей поставки.

