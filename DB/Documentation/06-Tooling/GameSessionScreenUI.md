# Ассеты и данные карты и боя

World map использует camera-based Local/Region/Cluster/System и отдельные
ReferenceSpeedKmS/MaxLocalHours в tacticalMap. Frozen скорость берётся из geography
descriptor; fallback применяется к миру без него. Локализация линейки явно
отличает опорное время от текущей оценки полёта. [Дизайн карты](../03-Design/TacticalMapSpecification.md).
UI 2.0 поддержан renderer для матрицы проверки; menu options остаются отдельным
контрактом [Settings](../02-FirstRelease/Screens/Settings.md).

Настройки карты находятся в Settings.gameSettings.tacticalMap, назначения клавиш — в [Data/UI](../../../src/DeepSpaceSaga.Client/Data/UI/). Данные тактического боя поставляются отдельным [каталогом](../../../src/DeepSpaceSaga.Client/Data/TacticalBattle/) с русскими именами и обязательным initiative у каждого пилота, а подписи UI — в [Data/Locale](../../../src/DeepSpaceSaga.Client/Data/Locale/).

Панели и карты работают по принятым снимкам и локальному UI. Рендер и обработка мыши используют общий viewport. Кнопочные PNG экспортируются [Export-TacticalButtonAssets.ps1](../../../tools/Export-TacticalButtonAssets.ps1); правила размеров и состояний — в [стиле кнопок](../03-Design/TacticalButtons/STYLE.md). Шрифты боя должны поддерживать кириллицу и символы из BattleFormat; запасные глифы проверяются отдельно от наличия ключа локали.

Пакеты оформления активного боя устанавливаются в `Skins` рядом с приложением. Срезы Xenon Star подключаются при локальной сборке. [Формат манифеста, роли и подключение](../04-Engineering/TacticalBattleSkins.md)

[Инструменты Performance](../../../tools/DeepSpaceSaga.Performance/README.md) разделяют CPU/raster, реальное окно с OpenGL и приёмку физического ввода. Порядок повторной проверки данных V4 — в [руководстве по источникам](CarrierWingSim.md).

Активный бой использует файловые DejaVu Sans Regular/Bold с единым TextPaint и клиентский Data/UI/tactical-battle-pilots.json для портретов/рода. Тепловое/пылевое поле создаётся процедурно из captured clumps через общий Motion, без отдельных PNG ассетов и без RNG рендера. Цвета/фактура/тень относятся к визуализации, порог и поглощение — к правилам Engine/Motion. Карточка сборки строится из Slots/Build/Limits, а силуэты — кодовые многоугольники light/heavy/thug с fallback light; дополнительный bitmap не нужен. Строки сборки RU/EN находятся в TacticalBattle.Build.*, а строки геометрии и результата выстрела — в TacticalBattle.Card.*. Иконки трёх показателей пилота рисуются контурами. Карта имеет отдельный от мира масштаб H/720, компактные overlays и hit-testing в тех же координатах. [Контракт презентации и растровых сцен](../04-Engineering/TacticalBattle.md#клиентская-презентация).
