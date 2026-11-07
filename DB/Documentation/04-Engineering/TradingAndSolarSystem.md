# Экономика и процедурный мир

Статические данные приходят из Settings.json, а runtime состояние принадлежит
Engine. Рынки используют профили и события; физические рейсы используют тот же
экономический контур, а не отдельную симуляцию для карты.

TradeQuoteCalculator считает цену партии и executable quantity; Engine связывает
quote с текущим визитом/revision и выдаёт durable receipt после атомарной сделки.
MarketTime и MarketEvents обрабатывают календарные границы. Удалённые наблюдения
рынка содержат возраст и revision, но не дают remote quote. CargoCost хранит
provenance стоимости; VoyageLedger отделяет выручку, себестоимость, топливо,
сборы и прочие проводки. Неизвестная себестоимость не превращается в нулевую.

Система, орбиты, кластеры, ресурсы и AiMap материализуются при New Game из
раздельных seeded streams. В поставленной конфигурации 3–7 планет, 2–5 поясов,
3–5 кластеров по 10–12 станций. Стартовая география и диапазоны длительностей
не гарантируют одинаковое ETA во всех будущих эпохах. Орбитальная геометрия
разделяется Engine и Client; свободное движение не умножается на календарные 300.

## География новых миров v2

Генератор v2 переставляет также исходные станции Default и других включённых
сценариев, сохраняя ID, роли, рынки и payload. Он захватывает operational Vmax
стартового корабля в `clusterMap.geography.referenceSpeedKmS`. Опорный день в
world units равен `referenceSpeedKmS × 86400 / 300 × 10`: 100 м на world unit
и календарь/motion=300 не меняются. Поставленный engine.basic имеет Vmax 6 км/с;
клиентский fallback 4 км/с не подменяет каталог.

Минимум между всеми человеческими станциями — один опорный день, nearest внутри
своего кластера — 1–3 дня, диаметр — 10–16 дней. Группа имеет общую круговую
геометрию, период, эпоху и направление, поэтому внутренние расстояния постоянны.
Раздельные радиальные полосы дают аналитический минимум между группами при любых
фазах. Центры ближайших кластеров размещаются в 30–50 днях при стартовой эпохе;
этот диапазон не обещан во все будущие эпохи. Ширина пояса задана текущим JSON.
Дистанция Солнце→фактический игрок при старте — 50–75 дней. Docked pose равна
станция+(1,1); свободные локальные смещения сценария сохраняются.

Граф v2 использует действительные позиции и supply/demand: локальные связи 1–3 дня,
межкластерные переходы, сильную связность и циклы. Legacy-only дальние рёбра
не попадают в v2 voyage projection. Текущая скорость влияет на ETA, но не на
frozen geography. Прямой полёт не является полным маршрутом с манёврами и Dock.

Detached bounded pipeline проверяет общий мир до его замены. Ресурсы сохраняют
ID, состав, RNG и локальные смещения относительно владельца; неизвестный состав
не раскрывается привязкой. Аннулус/clearance и расстояния ресурсов проверяются.
Legacy straight trading-template corridor strips в v2 не резервируются: прямые
шаблонов не сохраняются после общей переориентации станций. AI/environment/POI
размещаются после финального мира своими потоками/валидаторами; конечный горизонт
AI-проверки не является бесконечным доказательством для AI.

Ресурсное поле станции — реальные объекты для обследования/добычи, не второй
станционный склад и не автоматическое производство рынка. Структурное
сканирование раскрывает разрешённые сведения через Engine. Базы ИИ не торгуют
с игроком; informational fields и POI не добавляют скрытые эффекты.

Load использует сохранённые descriptors и continuation, не повторяет генерацию
по новым значениям JSON. v1 не переразмещается в v2. Solar generatorVersion и
cluster rulesVersion согласуются; frozen descriptor обязателен в v2 и отсутствует/null
в v1. Новый writer и compatibility описаны в [SaveFormat](SaveFormat.md).
Игровой цикл — [Trading](../02-FirstRelease/Mechanics/Trading.md), география —
[SolarSystemMapConcept](../02-FirstRelease/Mechanics/SolarSystemMapConcept.md).

Источники: [SimulationEngine.TradeQuotes.cs](../../../src/DeepSpaceSaga.Engine/SimulationEngine.TradeQuotes.cs), [SimulationEngine.MarketTime.cs](../../../src/DeepSpaceSaga.Engine/SimulationEngine.MarketTime.cs), [SimulationEngine.MarketEvents.cs](../../../src/DeepSpaceSaga.Engine/SimulationEngine.MarketEvents.cs), [SimulationEngine.CargoCost.cs](../../../src/DeepSpaceSaga.Engine/SimulationEngine.CargoCost.cs), [solar-system.json](../../../src/DeepSpaceSaga.Client/Data/Maps/solar-system.json).
