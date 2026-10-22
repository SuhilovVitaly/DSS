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

Ресурсное поле станции — реальные объекты для обследования/добычи, не второй
станционный склад и не автоматическое производство рынка. Структурное
сканирование раскрывает разрешённые сведения через Engine. Базы ИИ не торгуют
с игроком; informational fields и POI не добавляют скрытые эффекты.

Load использует сохранённые descriptors и continuation, не повторяет генерацию
по новым значениям JSON. Новый writer и compatibility описаны в [SaveFormat](SaveFormat.md).
Игровой цикл — [Trading](../02-FirstRelease/Mechanics/Trading.md), география —
[SolarSystemMapConcept](../02-FirstRelease/Mechanics/SolarSystemMapConcept.md).

Источники: [SimulationEngine.TradeQuotes.cs](../../src/DeepSpaceSaga.Engine/SimulationEngine.TradeQuotes.cs), [SimulationEngine.MarketTime.cs](../../src/DeepSpaceSaga.Engine/SimulationEngine.MarketTime.cs), [SimulationEngine.MarketEvents.cs](../../src/DeepSpaceSaga.Engine/SimulationEngine.MarketEvents.cs), [SimulationEngine.CargoCost.cs](../../src/DeepSpaceSaga.Engine/SimulationEngine.CargoCost.cs), [solar-system.json](../../src/DeepSpaceSaga.Client/Data/Maps/solar-system.json).
