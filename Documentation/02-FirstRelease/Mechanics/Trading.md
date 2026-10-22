# Торговля

Торговый цикл: Dock/grant → Station → Trade → quote → подтверждение сделки →
выход/undock → физический рейс → прибытие и новый визит. Рынок конечен:
запасы, target/capacity, бюджет, производство и события принадлежат Engine.
Котировка привязана к текущему визиту/revision; UI не пересчитывает её как
авторитетную. Результат содержит durable receipt. Fuel покупается в бак через
Refuel, не как cargo. Sell может быть ограничен доступными условиями рынка.
Удалённое знание рынка — наблюдение с возрастом, не удалённая live quote.
Себестоимость груза и проводки рейса попадают в Finance; неизвестная прибыль
не показывается как доказанный ноль. Подробнее: [технический контракт](../../04-Engineering/TradingAndSolarSystem.md).

Источники: [TradeQuoteCalculator.cs](../../../src/DeepSpaceSaga.Engine/TradeQuoteCalculator.cs), [TradeModel.cs](../../../src/DeepSpaceSaga.Client/UI/Screens/Trade/TradeModel.cs).
