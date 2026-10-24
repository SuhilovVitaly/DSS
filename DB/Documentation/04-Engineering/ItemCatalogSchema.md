# Каталог товаров и совместимость

Каталог загружается EngineContentLoader по Settings.typeData.itemTypes.
Схема item-файлов — 2, экономическая catalogVersion — 1; прежняя форма
поддерживается только через явную нормализацию loader, не произвольный fallback.

| Поле | Контракт |
|---|---|
| typeId | Стабильный непустой доменный ключ |
| catalogCode | Необязательный уникальный дополнительный код |
| displayName | Отображение, не identity |
| unitMassKg | Положительная масса Cargo; неотрицательная для FuelTank |
| basePriceCredits | Положительная цена или отсутствие для неторгуемого товара |
| tradeCategory | Resource / Good |
| tradeUnit | Kilogram / Piece / Ration / EnergyCell |
| storageKind | Cargo / FuelTank |
| buyQuantityStep / sellQuantityStep | Текущие правила требуют 1 |

Kilogram Cargo имеет массу ровно 1 кг. Ration/EnergyCell — Good/Cargo.
FuelTank — Good/Kilogram; `item.fuel` не занимает cargo. Явная позиция
станционного склада должна быть торгуемой. Дубликаты ключей, неизвестные
recipe/item ссылки и некорректные enum/числа отклоняются до публикации registry.

CatalogCompatibility связывает сохранение с версиями и экономическим
fingerprint. Переименование displayName не меняет экономическую identity;
изменение цены, массы, единиц, хранения или состава каталога влияет на
совместимость. Не пересчитывай legacy fingerprint для обхода проверки.
Текущий вход сохранений ограничен [SaveFormat](SaveFormat.md); описания старых
save migrations не расширяют этот список.

Источники: [ItemCatalogValidation.cs](../../../src/DeepSpaceSaga.Engine/Content/ItemCatalogValidation.cs), [EngineContentLoader.cs](../../../src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs), [CatalogCompatibilityData.cs](../../../src/DeepSpaceSaga.Engine/Scenario/CatalogCompatibilityData.cs), [Settings.json](../../../src/DeepSpaceSaga.Client/Settings.json).
