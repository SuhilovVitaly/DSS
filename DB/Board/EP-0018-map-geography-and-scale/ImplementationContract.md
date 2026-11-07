# Контракт переделки карты — EP-0018

Это планируемое поведение нового мира, не описание уже поставленного кода. Source of truth до исполнения — данный Board вместе с Documentation.md. Нормативные общие единицы/ownership остаются в EngineRequirements.

## C-01 — расстояния и география
Опорная скорость generationReferenceSpeedKmS берётся из operational стартового двигателя тем же правилом SolarSystemGenerator.OperationalMaxSpeedKmS; значение замораживается в descriptor и не зависит от будущей замены модулей.
unitsPerDay = generationReferenceSpeedKmS * 86400 / 300 * 10.
distanceDays = hypot(dx,dy) / unitsPerDay.
Границы: minStationDays=1; neighborMinDays=1; neighborMaxDays=3. Допуск абсолютный 1e-6 world units + относительный 1e-10 от длины; не округлять дни до сравнения.

Минимум относится к каждой паре человеческих Station в ClusterMap, в том числе к разным кластерам и preserved IDs. AI planetary/orbital bases исключены из human membership. Nearest — минимум по другим станциям собственного кластера, на старте 1–3d и неизменен при rigid orbit. Межкластерные nearest не обещаются в каждом будущем календарном году.

Формация: одна circular орбита на кластер, общий epoch/period/direction; все станции включая legacy располагаются заново по стабильному ID порядку и seeded ориентации на дуге. В пределах кластера проектировать одинаковый радиус, общий период; neighbor chord 1.2d как исходный ориентир. Проверять фактические хорды, весь diameter 10–16d, а не длину дуги. Не требовать максимум 3d для любых пар — только nearest.
Дополнительные кластеры занимают радиальные полосы в том же поясе; min расстояние между радиусами станций разных кластеров >=1d с запасом для ресурсов. Это аналитическое доказательство глобального min при любых фазах, а не только конечная выборка. beltWidthFraction=0.20, center nearest at epoch 30–50d — исходные настройки. Если placement невозможен, конечная детерминированная ошибка до публикации.

## C-02 — версии и descriptor
Планируемое additive Contracts:
public sealed record ClusterGeographyRules(
 double ReferenceSpeedKmS, double MinimumStationDays,
 double NeighbourMinDays, double NeighbourMaxDays);
StationClusterMapSnapshot получает optional ClusterGeographyRules? Geography = null, explicit camelCase JsonPropertyName.
Новый мир: SolarSystemMapSnapshot.GeneratorVersion=2, StationClusterMapSnapshot.RulesVersion=2, Geography обязательно. Legacy v1 принимает отсутствие Geography. Конфигурация schemaVersion остаётся 1; generatorVersion поддерживает 1 и 2; ClusterGenerationConfig получает optional double MinimumStationDays=1 в конце конструктора.
Запретить unknown versions, inconsistent solar/cluster pair, nonfinite/<=0 speed, min<1, neighborMin<min, max<min. Версия 1 не получает новые ограничения координат.

Новый алгоритм географии использует отдельный stage "geography/v2" внутри именованного генератора и стабильную сортировку; streams старых миров и v1 golden vectors не переписываются. Повторяемость определяется seed + scenario + config + version. Время и единицы не меняются.

## C-03 — bootstrap, ресурсы и сценарии
В v2 получить station identities/roles/topology из legacy trading request без materialization старых offsets. Для совместимости экономических политик сохранить TradingMap descriptors/flows/risk/RNG, но в v2 координаты/орбиты принадлежат ClusterMap; legacy template offsets не являются вторым географическим контрактом.
Планируемый Engine internal helper StationGeographyV2 отвечает за deterministic role materialization/placement; SimulationEngine выбирает v1 либо v2 pipeline до генерации ресурсов. Генерация ресурсов после окончательного placement; перенос уже объявленных ресурсов — по delta владельца, с сохранением manifest/RNG и последующим BindResources. Не генерировать одни и те же астероиды второй раз.

Start anchor: station TradingMapGeneration.StartStationObjectId; для сценария без него — docking parent либо определённая до размещения ближайшая station по (distance,ObjectId). Если исходных станций нет, создать стартовый cluster около фактического player position. Docked получает parent+(1,1); свободный корабль сохраняет локальный вектор относительно anchor; исходные свободные сценарные объекты сохраняют свой локальный вектор относительно стартового окружения. Станции после этого НЕ возвращать в прежние координаты.
Фактическая стартовая дистанция player->Sun 50–75d проверяется после всех стадий. Ресурсная локальность 2.5–4.5км остаётся отдельным правилом station-resource-fields.json; в межстанционный минимум ресурсы не входят.

## C-04 — persistence и торговля
Load никогда не вызывает генератор. v1 мир проходит исторические проверки и продолжает прежнюю геометрию. v2 проверяет материализованные identities, seed/catalog, human membership, geography descriptor, rigid orbit и min distances; никаких координат из текущего JSON/шаблонов.
TradingMap.ValidateMaterialized получает overload/context для v2; только legacy coordinate/template-offset checks заменяются проверкой canonical ClusterMap geometry. Catalog/profile/risk/cargo/RNG/cross-reference checks остаются обязательными. Не отключать всю trading validation.
Старые Short/Medium/Long и maxTravel ограничения из генератора часов не должны отвергать легальные v2 рейсы на дни. Проекция CurrentVoyageMap использует текущие позиции и текущий движок, существующие fuel/risk правила; сохранённые текущие рейсы не пересчитываются задним числом и не списывают ресурсы повторно.
Save20 остаётся writer baseline; добавление optional descriptor проверяется strict JSON roundtrip, Save18/19 migration regression. Если фактическая схема требует иного writer, отдельный записанный пересмотр Board до изменения версии.

## C-05 — локальный масштаб
Новая Client policy выбирает Local по охвату мира, а не порядковому номеру scale preset. Local приёмка: центр player, baseline 4км/с, short side <=4 игровых часов. Для aspect ratio 4:3…32:9 одновременно весь diagonal <1 игрового дня; чтобы гарантировать это, MaximumPpu = max(configuredMaximumPpu, requiredPpuFor4HoursShortSide, requiredPpuForLessThan1DayDiagonal). Это presentation constraint, мир и скорости не масштабируются. Для UI/window размеров за матрицей использовать то же ограничение диагонали, не неопределённое поведение.
При max zoom обе границы соблюдаются. 100м/px остаётся shipped baseline. При очень большом viewport допускается большее приближение; при этом WorldToScreen/ScreenToWorld остаются обратными.
Диагональный запас: считать requiredPpuForLessThan1DayDiagonal по пределу 0.95 суток, чтобы strict <1d не зависело от округления double.
Опорная скорость для шкалы времени — snapshot.ClusterMap.Geography.ReferenceSpeedKmS; legacy fallback explicit settings referenceSpeedKmS=4, не скрытая константа в UI. Километры — основная линейка; время подписывать как «при опорной скорости», не выдавать за ETA Dock.
New Game открывает локальный вид около игрока; обзор системы доступен явной командой. Не сбрасывать пользовательскую камеру при очередном snapshot. Resize/DPI/zoom transition пересчитывают границы безопасно; selected/nav target и pan/focus сохраняют ожидаемое поведение. TacticalBattle camera не меняется.

## C-06 — уровни детализации
Планируемый Client internal enum MapDetailLevel { Local, Region, Cluster, System } и MapDetailPolicy, pure function snapshot+camera+viewport+previousLevel.
Пороги по short-side span при reference speed: Local <=4ч, Region до4д, Cluster до30д, System выше; hysteresis 10% на выход/вход, тестировать граничные переходы. Допустима перекалибровка только записанным изменением assumptions с native evidence.
Local: отдельные local objects/ресурсы, ближайшая станция, selected и nav target. Никакого отсева реальных близких станций только ради выполнения E18-AC-06.
Region: станции и компактные resource fields; Cluster: все station members, selected connections и имя кластера; System: доменные cluster markers, планеты, Солнце, пояса. Экранные spatial cells — лишь средство снижения плотности; они не меняют membership.
Selected/player/nav target сохраняют индивидуальную видимость и hit target; агрегация не создаёт фиктивную station identity. На дальнем масштабе click marker -> fit cluster; station -> local focus через явное действие. Переключение LOD не переносит world objects.
Орбиты: planet orbits на overview при ShowOrbits; station orbit — выбранной станции; asteroid orbit — выбранного asteroid только при включённом ShowOrbits. Учитывать родительскую орбиту docked target; deduplicate identical curves. Planet/body bounds и whole belts входят в FitSystem.
Не раскрывать скрытые composition/mass или NPC plans ради подписи/агрегации.

## C-07 — измерения и приёмка
Корпус 100 seed на сценарий делится на точные тестовые случаи по сценарию/диапазону seed; каждый процесс <=300000ms. Шесть enabled scenarios, boundary min/max planets/belts/counts/start50/75, Vmax2/4км/с. Проверять feasibility каждой конфигурации, finite outputs и bounded failure; таймаут не PASS.
Analytic proof min между radial lanes + независимые проверки epoch/1/30/365д. Start/selected station/cluster/system screenshots для fixed seeds1,42,100; разрешения1280x720,1920x1080,3440x1440; дополнительно UI scale1/1.5/2 и aspect32:9. Screen scale/geometry вычислять из camera state, не OCR.
Нативный сценарий: Default start->zoom out->cluster->station focus->Approach/Dock->Save/Load->return. Input, hit target, labels, pan/resize, pause/fast speeds проверять отдельно. Архивировать seed/config/HEAD/settings/viewport/scale, distances/nearest, min violations, screenshots и timings.
80FPS измерять отдельно на явной машине: target80 соответствует12.5ms/frame; записать median/p95 и sample count, warmup. Отсутствие baseline/hardware/native evidence означает OPEN, не PASS. Никакой human acceptance по одному scripted input.

## I — обязательные инварианты
- I-01 Engine authoritative; Client/LOD не меняет world state.
- I-02 100м/world unit, calendar/motion300; движение не умножается вторично.
- I-03 min1d для всех human pairs; nearest1–3d внутри своего rigid cluster.
- I-04 seed/config/scenario/version deterministic, retries конечны.
- I-05 v1 Load сохраняет прежний мир; v2 Load materialized, не generation.
- I-06 references/IDs/explicit inventory/docking сохраняются.
- I-07 new session публикуется атомарно; failure сохраняет прежнюю session.
- I-08 selected/player/nav target доступны при любом LOD; знание не расширяется.
- I-09 test/native/product acceptance разделены; evidence не выдумывается.

## Уточнение исполнения C-03 — 2026-11-07

Объявленные поля сохраняют captured rules и knowledge; append не меняет ихRNG.
V2 не резервирует static полосы legacy trading template edges: неизменность всех
source owner-relative offsets иначе несовместима с произвольной сменой направления
торговых хорд. Corridors остаютсяv1правилом; v2 всё равно проверяет annulus,
stationclearance/asteroidspacing, ids/owner/rules/composition/mass/manifest/RNG.
Это техническое решение исполнения, не дополнительная цитата пользователя.
Воспроизведение и evidence — US0003TK0001; учесть в финальной текущей документации.