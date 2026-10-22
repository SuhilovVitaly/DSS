# Отладочные параметры: текущий backend

EP-0013 находится в исполнении. Описанные DTO/Engine/persistence контракты
существуют в текущем коде; это не утверждение о готовности панели или приёмке эпика.

`DebugTuningPatch` содержит массив ModuleTypeId / Field / Value и optional
ProfileId. `debug.tuning.apply` — авторитетная атомарная команда. Snapshot
передаёт Revision, текущие Values, неизменяемые исходные Defaults и StartupError.
Ключи полей/профилей чувствительны к регистру. Неизвестный параметр,
недопустимое значение и неработающая сессия имеют отдельные reason codes.
Реализация retiming разделена на ship, projectile и reload обработчики.

Overlay принадлежит сессии; исходные catalog defaults не перезаписываются.
Save19 сохраняет optional tuning и flight provenance. Стартовый файл
`DebugTuning/debug-tuning.json` ищется рядом с executable. Program передаёт
его фабрикам New Game, выбранного сценария и Load. Чтение bootstrap-only,
без live watcher. Неверный файл отклоняется с предупреждением/StartupError.
Общий `DebugTuningFileFormat` требует schemaVersion 1 и проверяет ключи по defaults.

Окончательная схема пользовательской панели, экспорта и human/native-приёмка —
в [EP-0013](../../Board/EP-0013-debug-tuning-panel/Documentation.md).
Они не объявляются выполненными только на основании backend-кода.

Источники: [DebugTuning.cs](../../src/DeepSpaceSaga.Contracts/DebugTuning.cs), [DebugTuningFileFormat.cs](../../src/DeepSpaceSaga.Contracts/DebugTuningFileFormat.cs), [DebugTuningState.cs](../../src/DeepSpaceSaga.Engine/DebugTuningState.cs), [SimulationEngine.DebugTuningStartup.cs](../../src/DeepSpaceSaga.Engine/SimulationEngine.DebugTuningStartup.cs), [SimulationEngine.DebugTuningPersistence.cs](../../src/DeepSpaceSaga.Engine/SimulationEngine.DebugTuningPersistence.cs), [Program.cs](../../src/DeepSpaceSaga.Client/Program.cs).
