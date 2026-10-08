# Базы ИИ, территории, поля и POI — EP-0004

Актуальный контракт и результат на 2026-10-08. Функциональная реализация поставлена; полная приёмка эпика **OPEN** из-за измеренного недостижения 80 FPS. [Board и журнал исполнения](../../Board/EP-0004-ai-territories-and-map-environment/ImplementationStatus.md). Self-review не является независимым одобрением.

## Источник состояния и единицы

[Контракты](../../src/DeepSpaceSaga.Contracts/AiMapEnvironment.cs): optional `AiMapEnvironmentSnapshot? AiMap` в authoritative snapshot и save содержит `RulesVersion=1`, `Bases`, `Territories`, `Fields`, `PointsOfInterest`. Все JSON-свойства явно camelCase; default ImmutableArray читается существующим default-конвертером. Legacy отсутствие AiMap допустимо.

Engine единолично создаёт и проверяет materialized descriptors. Client читает локальный SnapshotBuffer, использует общую Motion-математику и не обращается синхронно к Engine из Render. World unit=100м; территориальные радиусы в км переводятся множителем10. Период орбиты — календарные ms, epoch орбиты/движения — физические ms. Speed1 даёт300 календарных секунд за физическую секунду; Speed0 замораживает движение. Камера и слои не меняют мир/RNG.

`AiBaseMapData` указывает реальную Station: Known, owner `AiEnemy`; descriptor owner=`Ai`, BaseType=`Planetary` или `Orbital`. Planetary следует родительской планете с offset, Orbital имеет свою орбиту; ровно один anchor. Базы не входят в человеческие кластеры, не получают человеческие рынки, производство и инвентарь. Не каждая планета имеет базу.

`TerritoryMapData` хранит стабильный ID, BaseObjectId, DefenceRadiusKm и PatrolRadiusKm; `0 < defence <= patrol`, все значения finite. Оба круга движутся с текущей базой. Пересечения не суммируют интенсивность заливки; выбранная территория выделяет контур. Патрули, урон, бой и дипломатия в этот эпик не входят.

`EnvironmentFieldData`: Radiation/Dust/Debris, intensity[0,1], геометрия `0 <= inner < outer`, sweep(0,360], углы clockwise от направления вверх. Parent/Orbit/Sun — взаимоисключающие anchors. Sun допускает статический кольцевой сектор Dust, а не только осесимметричный круг. Parent обязан ссылаться на реальный объект; descriptor cycles запрещены. Radiation — круг у Солнца; Dust — сектор30–90° в ширине пояса; Debris чередует привязку к планете и собственной орбите. DecorationSeed меняет только рисунок.

`PointOfInterestData` — metadata ObjectId/Name/Description плюс parent или своя orbit и offset. Это не SpaceObject и не источник награды/квеста. Генерация предпочитает реальные canonical resource asteroids; при их отсутствии использует собственную орбиту внутри пояса. POI известны сразу, без engine actions.

## Генерация и content

[Конфигурация](../../src/DeepSpaceSaga.Client/Data/Maps/solar-system.json) используется только для New Game: solar system → clusters/resources → AI → fields → POI; готовый мир публикуется атомарно. Seeded потоки AI counts/placement, environment kind и POI template независимы; шаблоны сортируются ordinal. ID стабильны: SYS-AI-n, SYS-TERRITORY-n, SYS-FIELD-KIND-n, SYS-POI-ordinal.

| Параметры поставки | Значение |
|---|---|
| schemaVersion / generatorVersion / AiMap rulesVersion | 1 / 1 / 1 |
| Сценарии | Default, Default_500, Docked, Undocked, MarketProfiles, PlayerShipOnly |
| Планеты / пояса / расстояние старта | 3–7 / 2–5 / 50–75 календарных дней |
| Human clusters / stations per cluster | 3–5 / 10–12 |
| ai.minBases / maxBases | 2 / 4 |
| defenceRadiusKm / patrolRadiusKm / maxPlacementAttempts | 200 / 1000 / 64 |
| radiationCount / dustCount / debrisCount | 1 / 2 / 2 |
| radiationRadiusKm / dustWidthKm / debrisRadiusKm / intensity | 5000 / 100 / 250 / 0.5 |
| asteroidsPerBelt / decorationSamplesPerBelt | 24 / 2048 |
| orbitSpeedFraction / beltWidthFraction / orbitClearanceWorld | 0.001 / 0.08 / 100 |

AI count bounds2..1024, attempts1..4096; radii finite and safe after conversion. Field counts0..64, positive finite geometry. `poiTemplates` находится в этом же JSON (отдельный poi-templates.json не создавался), максимум128 уникальных case-insensitive ID с непустыми name/description. Поставлены abandoned-relay и abandoned-platform; actions/reward/cost/production отклоняются строгим loader.

[AiTradePlacementValidator](../../src/DeepSpaceSaga.Engine/Scenario/AiTradePlacementValidator.cs) проверяет горизонт365 календарных дней: 0,1,7,30,100,365 и аналитические conjunction/opposition epochs с floor/ceil физического ms. Игрок проверяется на старте; человеческие станции и локальные торговые отрезки — на всём горизонте с консервативной интервальной оценкой расстояния/скоростей, рекурсией и отказом при неопределённости. Касание патрульного круга недопустимо.

Межкластерная связность проверяется visibility graph в объявленных/критических эпохах внутри SystemRadius и вне Sun exclusion `max(1,0.01*SystemRadius)`, с64 описанными вершинами на круг. Это инженерная геометрическая резервация, не игровое столкновение. Ограничения8192 epochs/nodes,200000 checks,100000 subdivisions, depth32 и2млн edges защищают bounded work. Непрерывная межкластерная безопасность между эпохами и бесконечный горизонт не доказаны. Невозможное размещение вызывает ограниченные повторы только AI и атомарный отказ, не сдвиг человеческой экономики.

## Авторитетный доступ и сохранение

`station_access_denied` проверяется Engine для hostile Dock (включая dialogue grant), legacy/quoted trade, quote, refuel и forged dock/quote paths. DockedStationTrade не публикуется для базы ИИ. UI показывает owner/type/radii и недоступность торговли/стыковки; одного disabled UI недостаточно. Открытая география не открывает удалённые котировки и не отменяет survey/scan policy.

Текущий [SaveFormat](../../src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs) — **15**, AiMap rulesVersion1. Save сохраняет materialized anchors/geometry/text/seeds; Load не генерирует слои заново по изменённому content. Legacy AiMap=null остаётся null. Неизвестная версия, malformed geometry/anchor, неправильный parent или case-insensitive alias между objects/belts/clusters/links/territories/fields/POI отклоняется до замены мира. Сохранённые информационные overlap/radii не запускают повторный new-game placement proof. Local file Load начинает на паузе. Проверены реальные JSON-файлы, buy → in-flight save/load → arrival → sell → day continuation и atomic invalid ingress.

## Отображение и управление

MapLayers: Orbits=1, Territories=2, Fields=4, PointsOfInterest=8, All=15. По умолчанию всё включено. При AiMap появляется вторая строка toolbar для территорий/полей/POI; legacy остаётся с8 кнопками. Локальное выключение слоя исключает его descriptors из hit-test и снимает скрытый descriptor selection, не меняя world/quotes/real selection.

Кандидаты выбора: станции → player → NPC → другие реальные объекты → POI → fields; внутри группы расстояние и ordinal ID. Реальные маркеры имеют30 raw px hit-radius (также учитываются hull/plaque), POI15px, поля — фактическую геометрию. Повторный клик в пределах3px при неизменном списке циклически выбирает перекрытия. Ctrl пропускает поля для команд свободной карте. LOD cluster aggregate не перехватывает видимый POI, Sun или Planet. Выбор одиночного игрока включает Follow; при overlap cycling камера не скачет.

SelectedFieldId/SelectedPoiId — client-local, отдельно от Engine SelectedObjectId. Панель выбранного descriptor имеет приоритет перед hover; после перехода по циклу реальных объектов также сохраняется выбранный. У полей есть видимое предупреждение об отсутствии эффектов; у POI — описание и отсутствие исследования. Target-модульные команды запрещены и в enabled-state, и при dispatch. Правый map click снимает выбор. Свободный обычный клик не переносит камеру; drag панорамирует. Ctrl+free click отправляет `engine.orbit`.

Слои рисуются под реальными маркерами: fields → territories → links/trails → POI → aggregates/real objects → labels/UI. Planet markers остаются перед overlays. Debris использует кэшированный локальный LCG рисунок (обычно64 samples, bound1..256), а не Engine RNG или новые entities. Числа configured decoration samples в evidence не обещают, что все они видимы на каждом LOD.

На узком logical viewport панель Object Info компактная, обе строки прокручиваются; её нижняя граница резервирует место toolbar. Колесо над панелью прокручивает её, не масштабирует карту. Diagnostic X рисуется последним и имеет приоритет UI hit-test. Для экстремальных допустимых радиусов >1e8 экранных px territory edge может опускаться ради численной устойчивости; обычная поставленная конфигурация этот предел не достигает.

## Проверки и ограничения приёмки

Автоматические Release suites: Engine1803/1803, Client1758/1758, Contracts173/173, Motion141/141, Performance tooling9/9 — **3884/3884 PASS**. Corpus:4800 solar+4800 cluster+1200 full-map=10800 первичных миров, дополнительно повторения/continuations/negative cases. [Correctness evidence](../../Board/EP-0004-ai-territories-and-map-environment/evidence/us8-correctness-summary.json).

[Performance evidence](../../Board/EP-0004-ai-territories-and-map-environment/evidence/us8-performance-summary.json): свежие последовательные baseline/full600+600 миров,18+18 raster views. Full medians generation44.6164ms, snapshot1.2844ms, save6.51055ms; raster p997.3695–19.4668ms. Все matched full worlds добавили только4 AI entities. CPU raster не является GPU FPS; улучшение производительности не заявляется.

[Native evidence](../../Board/EP-0004-ai-territories-and-map-environment/evidence/us8-native-summary.json): фактическое окно Skia/OpenGL, Intel Arc140V, driver32.0.101.8860, VSync, reported100Hz, .NET8.0.26. Final interactions8/8 PASS и8 PNG проверены: min/max,1280x720/1920x1080,UI1/1.2/1.5,system/belt/cluster/base/field/POI, реальные кнопки выбора/слоёв/паузы/прокрутки. Каждый case120 warmup+600 measured frames. Финальный p99 swap-completion intervals50.5839–54.4007ms при цели12.5ms: **80 FPS FAILED, acceptance OPEN**. Baseline и пустое VSync окно тоже не достигают цели; точная причина driver/OS не доказана. Временный fine-timer experiment удалён. GPU execution/physical scanout не измерены, human playthrough NOT RUN. Нативная автоматизация не заменяет человеческую продуктовую приёмку.

Промежуточные failures и module IDs сохранены в summary; семь финальных cases измерены до последнего исправления diagnostic X, повторён затронутый восьмой. Raw native JSON/PNG удалены после review; summary хранит их SHA256 и компактные результаты. [Команды воспроизведения](../../tools/DeepSpaceSaga.Performance/README.md). Полная таблица тикет/SHA/push и оставшаяся работа ведутся в Board execution registry.

[Финальный граф и provenance](../06-Tooling/AiMapGraphify.md) служит навигации; runtime acceptance подтверждается evidence выше.
