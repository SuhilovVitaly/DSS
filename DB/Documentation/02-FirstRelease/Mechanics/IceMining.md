# Добыча и обследование

Drilling Unit и Scanner адресуются как обычные команды конкретных модулей.
Параметры цикла, стоимость запуска, цель и результат берутся из content.
Engine проверяет доступность ресурса, состояние модуля и cargo capacity;
визуальная кнопка не должна сама добавлять добытый товар.
Структурное сканирование раскрывает разрешённые сведения об астероиде;
ресурсные поля материализуются из station-resource-fields.json.
Это не обещание полной универсальной добычи всех ресурсов или производства
на декоративных POI. Полные условия команды задаёт её текущий JSON.

Источники: [modules-drillingunit.json](../../../../src/DeepSpaceSaga.Client/Data/Modules/DrillingUnit/modules-drillingunit.json), [station-resource-fields.json](../../../../src/DeepSpaceSaga.Client/Data/World/station-resource-fields.json), [Commands](../../../../src/DeepSpaceSaga.Client/Data/Commands).
