# Ассеты и данные карты и боя

Настройки карты находятся в Settings.gameSettings.tacticalMap, назначения клавиш — в [Data/UI](../../../src/DeepSpaceSaga.Client/Data/UI/). Данные тактического боя поставляются отдельным [каталогом](../../../src/DeepSpaceSaga.Client/Data/TacticalBattle/) с русскими именами, а подписи UI — в [Data/Locale](../../../src/DeepSpaceSaga.Client/Data/Locale/).

Панели и карты работают по принятым снимкам и локальному UI. Рендер и обработка мыши используют общий viewport. Кнопочные PNG экспортируются [Export-TacticalButtonAssets.ps1](../../../tools/Export-TacticalButtonAssets.ps1); правила размеров и состояний — в [стиле кнопок](../03-Design/TacticalButtons/STYLE.md). Шрифты боя должны поддерживать кириллицу и символы из BattleFormat; запасные глифы проверяются отдельно от наличия ключа локали.

Пакеты оформления активного боя поставляются отдельно в Skins. Для авторов доступны [формат манифеста и карта ролей](../04-Engineering/TacticalBattleSkins.md).

[Инструменты Performance](../../../tools/DeepSpaceSaga.Performance/README.md) разделяют CPU/raster, реальное окно с OpenGL и приёмку физического ввода. Порядок повторной проверки данных V4 — в [руководстве по источникам](CarrierWingSim.md).