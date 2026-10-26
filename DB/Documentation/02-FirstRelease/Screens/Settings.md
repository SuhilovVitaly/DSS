# Настройки

Выбор монитора, языка English/Russian и UI scale 0.8/1.0/1.2/1.5. Параметры применяются через callbacks хозяина окна. Этот экран не является редактором боевых каталогов или доказательством завершения DebugTuning panel.

См. [каталог экранов](ScreenCatalog.md) и [основные требования](../../01-Requirements/EngineRequirements.md).

Preferences читаются и записываются в AppPaths.Current.Settings —
пользовательский config либо portable installation. Комплектный Settings.json
служит шаблоном; contentRoot управляется приложением, остальные preferences
сохраняются. [Размещение файлов](../../06-Tooling/Ubuntu.md#настройки-и-данные).

Источники: [SettingsScreen.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/Settings/SettingsScreen.cs).
