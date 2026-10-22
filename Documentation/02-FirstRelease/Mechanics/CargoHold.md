# Грузовой отсек

Вместимость Cargo задаётся container-модулями и измеряется по массе товаров.
Количество торговых единиц и килограммы — разные величины. Fuel в cargo не
помещается. Покупка, добыча и перенос проверяют доступную вместимость в Engine.
Стоимость приобретения и provenance хранятся отдельно от количества; неизвестная
история не превращается в нулевую себестоимость. Груз и cost basis сохраняются.
Схема единиц — в [ItemCatalogSchema](../../04-Engineering/ItemCatalogSchema.md).

Источники: [SimulationEngine.CargoCost.cs](../../../src/DeepSpaceSaga.Engine/SimulationEngine.CargoCost.cs), [modules-container.json](../../../src/DeepSpaceSaga.Client/Data/Modules/Container/modules-container.json).
