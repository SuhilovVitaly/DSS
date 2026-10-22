# Energy Cells и электричество

Energy Cells — отдельный cargo item, торгуемый в единицах EnergyCell.
Они не заменяют Fuel и не хранятся в баке. Командный content поддерживает
activationEnergyCellsCost; это не то же самое, что полная электрическая сеть.
Принятое требование первого релиза: генератор расходует одну Energy Cell за
календарный час, active modules требуют работающего генератора; полноценное
распределение мощности не входит в этот MVP. Наличие module.generator.basic,
powerConsumptionW и товара само по себе не доказывает реализацию этого контура.
Не описывай принятое решение как завершённое поведение без проверки Engine.

Источники: [CommandDefinition.cs](../../../src/DeepSpaceSaga.Engine/Content/CommandDefinition.cs), [modules-generator.json](../../../src/DeepSpaceSaga.Client/Data/Modules/Generator/modules-generator.json).
