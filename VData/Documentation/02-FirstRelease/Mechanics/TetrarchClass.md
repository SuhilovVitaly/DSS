# Корабль Tetrarch

Класс ship.tetrarch и установленные модули задаются ship/scenario content.
Не считай старые численные Vmax, размеры, массу и расположение модулей
универсальным контрактом: текущий implementation двигателя и сценарий являются
источником параметров. Критические роли bridge/engine относятся к боевой модели.
Cargo capacity, fuel tank, жилая вместимость и вооружение принадлежат своим
модулям. Tactical Object Info показывает подтверждённые данные; отдельный
ShipScreen пока является оболочкой, не полноценным редактором корабля.

Источники: [ship-classes.json](../../../../src/DeepSpaceSaga.Client/Data/Ships/ship-classes.json), [modules-engine.json](../../../../src/DeepSpaceSaga.Client/Data/Modules/Engine/modules-engine.json), [ShipScreen.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/Ship/ShipScreen.cs).
