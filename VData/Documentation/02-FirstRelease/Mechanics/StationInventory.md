# Склад и рынок станции

Станция имеет явный inventory и market profile, конечные запасы, цели,
capacity, бюджет и производственные потоки. Профили определяют роль рынка;
события изменяют экономические условия по календарным границам.
Явные inventory и recipe-ссылки проверяются loader. Ресурсное поле возле
станции не является её складом и не добавляет товары без экономического потока.
Информация чужого рынка и действующая quote текущего визита различаются.
Базы ИИ не предоставляют человеческую торговлю/заправку.

Источники: [StationMarketProfileDefinition.cs](../../../../src/DeepSpaceSaga.Engine/Content/StationMarketProfileDefinition.cs), [station-market-profiles.json](../../../../src/DeepSpaceSaga.Client/Data/Markets/station-market-profiles.json).
