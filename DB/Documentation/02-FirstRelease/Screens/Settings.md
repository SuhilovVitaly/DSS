# Настройки

Доступны монитор, язык English/Russian, UI scale 0.8/1.0/1.2/1.5 и скин пошагового боя. Список скинов показывает эффективный выбор, включая штатный Classic, поставляемые Contrast/Tabletop и доступные внешние пакеты. Новый выбор применяется к следующему окну активного боя и сохраняется в Settings.json. При отказе записи отображается локализованное сообщение, прежний выбор остаётся активным. [Установка и формат пакетов](../../04-Engineering/TacticalBattleSkins.md)

Xenon Star появляется в списке, когда установлен исправный пакет. Для недоступного сохранённого ID Settings показывает Classic. [Сборка с локальным Xenon Star](../../04-Engineering/TacticalBattleSkins.md#xenon-star)

См. [каталог экранов](ScreenCatalog.md) и [основные требования](../../01-Requirements/EngineRequirements.md).

GameSession renderer дополнительно принимает параметр UI scale 2.0 в tooling/constructor.
Обычный SkiaWindow и меню по-прежнему принимают перечисленные выше значения.
UI scale изменяет панели, а zoom и ограничения Local используют raw viewport.
Параметры tacticalMap ReferenceSpeedKmS/MaxLocalHours валидируются отдельно;
при наличии geography descriptor опорная скорость берётся из сохранённого мира.

Источники: [SettingsScreen.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/Settings/SettingsScreen.cs).

Активный тактический бой масштабирует макет по высоте H/720, независимо от menu UI scale. Выбор скина по-прежнему применяется при следующем открытии боя.
