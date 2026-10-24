# Топливо двигателя

Fuel хранится только в баке двигателя. Вместимость бака и эффективность
задаются implementation двигателя; они не выводятся из массы cargo-единицы.
Заправка использует текущий станционный рынок, доступный бюджет и authoritative
результат. Выливание топлива и продажа обратно из бака не подразумеваются UI.
Маршрутный расход резервируется для voyage, затем выполняется settlement/refund;
это не заменяет расходы отдельных команд. Load обязан сохранять уже созданную
резервацию и не списывать её повторно. Актуальные численные параметры — в Data/Modules/Engine.

Источники: [SimulationEngine.VoyageFuel.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.VoyageFuel.cs), [modules-engine.json](../../../../src/DeepSpaceSaga.Client/Data/Modules/Engine/modules-engine.json).
