# EP-0007: операторы и противоракетная защита

Дата: 2026-10-04. Исходный HEAD: `5fa0595816569b4c8aefc198fdc6a3856dedeabb`. Реализация находится в рабочем дереве; commit/push не выполнялись. EP-0007 существовал как untracked Board-план до выполнения.

## Impact audit документации

Инвентаризация выполнена перед изменением канонических документов по итоговому diff, поиску weapon/torpedo/combat/save и связанным экранам/механикам.

| Путь относительно репозитория | Раздел / причина | Действие и проверка |
|---|---|---|
| Documentation/01-Requirements/EngineRequirements.md | §57 loadout; контракт боя, время и Save v11 | Обновить; сверить с ScenarioLoader, Engine и тестами |
| Documentation/01-Requirements/FirstReleaseRequirements.md | Tetrarch, панели, объём боя | Обновить ссылки и состав |
| Documentation/02-FirstRelease/Mechanics/TetrarchClass.md | 12 клеток, 8 модулей, 2 оператора | Обновить таблицы по пяти scenario.json |
| Documentation/02-FirstRelease/Mechanics/PassengerContracts.md | Нет свободной стартовой каюты после второго оператора | Обновить только стартовую вместимость, пассажирская механика остаётся планом |
| Documentation/02-FirstRelease/Mechanics/CrewAndHabitation.md | Два оператора занимают обе стартовые каюты | Обновить стартовый состав и вместимость |
| Documentation/02-FirstRelease/Mechanics/CommandPanels.md | Шесть панелей, Fire/SelfDestruct, auto toggle | Обновить по CommandsPanel |
| Documentation/02-FirstRelease/Mechanics/TacticalMapAndManeuvering.md | Выбор ПР, дальность и шанс | Добавить ссылку на новый этап |
| Documentation/02-FirstRelease/Screens/GameSession.md | Боевые панели и журнал | Обновить существующий экран |
| Documentation/02-FirstRelease/Screens/Save.md | Persisted defense payload | Указать v11 и физические таймеры |
| Documentation/02-FirstRelease/Screens/Load.md | Миграция и отсутствие replay UI эффектов | Указать legacy policy |
| Documentation/03-Design/TacticalMapSpecification.md | Blue marker, trail, chance, radius | Дополнить спецификацию |
| Documentation/06-Tooling/GameSessionScreenUI.md | Части экрана и authoritative источник | Дополнить навигацию |
| Documentation/04-Engineering/BasicTorpedoCombat.md | Исторический этап EP6 отличается от текущего | Сохранить прошлые результаты, добавить ссылку |
| Documentation/README.md | Навигация | Добавить runbook и graph report |
| Board/EP-0007-countermeasure-combat/Documentation.md, Tickets.md | Результат всех 35 шагов | Дополнить summary без автоматического APPROVED |
| Сводные Documentation.md и Tickets.md EP-0006 | Исторический план первого этапа | Добавить ссылку на EP7, сохранить историю |
| Documentation/00-Process/CLAUDE.md | Engine → Contracts → Client | Архитектура не меняется; правка не требуется |
| Documentation/04-Engineering/ItemCatalogSchema.md | Каталог товаров и экономические версии | Не затронут: версия каталога не равна SaveFormatVersion |
| Documentation/05-Backlog/, датированные CodeReview/TradeUI отчёты | Исторические результаты | Не переписывать |
| Остальные release screens, portraits/design и tooling | Нет изменений соответствующих механик | Не затронуты |

## Реализованный контракт

Источник истины — [EngineRequirements](../01-Requirements/EngineRequirements.md#countermeasure-combat). Default, Default_500, Docked, Undocked и PlayerShipOnly содержат торпедный аппарат в (3,2), противоракетный в (5,2) и двух разных назначенных операторов. Навыки — 50; базовые рейтинги — 30. Старые scenario/save без назначения остаются без оператора; фиктивный экипаж не создаётся.

`WeaponOperatorSnapshot` фиксирует CrewId, имя, тип навыка, навык, базовый и эффективный рейтинг. `R=base*(skill/50m)`. Навык 0 допустим и отличается от отсутствия оператора. Fire без оператора отклоняется до изменения мира. Запущенная торпеда сохраняет рейтинг и provenance.

`CountermeasureSnapshot` содержит Guiding/MissedCoast, маршрут, полный аналитический след, цель, frozen chance, breakdown, момент запуска/встречи и исход. `DefenseSnapshot` содержит AutoEnabled, оператор, Ready/Guiding/Reloading/NoOperator, active ID, абсолютный reload deadline и RangeKm.

`ChanceTenths=Round(clamp(50+Rdef-Rtorp,0,100)*10, AwayFromZero)`. Один равномерный roll 1..1000 при первом контакте на 500 м; успех при roll<=ChanceTenths. 0% не запускает ПР и не расходует попытку/RNG; 100% всё равно расходует roll. Поток `combat.countermeasure.intercept.v1`: отдельный SplitMix64 v1, rejection sampling modulo1000 (threshold616), сохраняются state и счётчик встреч. Golden seed42: 801,820,781,237,617,381,400,470.

ПР стартует из центра носителя по его курсу; собственная скорость 12 км/с, поворот до 90°/с. Защита включена у игрока и пирата; NPC не запускает наступательные торпеды. Дальность 100 км включительно (1000 world units); геометрия учитывает кривой маршрут торпеды и контакт раньше её первого попадания в корпус. Вход в дальность ищется внутри интервала; запуск привязан к первой целой физической миллисекунде после входа. На паузе автопуска нет. Выбор входящей цели: ближайшее прогнозное попадание, затем ordinal ID. Одна попытка резервируется при запуске; смена auto/reload/load её не сбрасывает.

На успехе исчезают оба снаряда без hull damage, reload начинается сразу и длится 10 физических секунд. На промахе ПР летит прямо 2 физические секунды, затем исчезает и начинается reload10. Потеря цели в Guiding завершает ПР без roll; в MissedCoast не сокращает остаточный полёт. При потере носителя ПР удаляется без reload. Выключение auto блокирует только новые пуски. При совпадении времён hull impact имеет приоритет; события ПР упорядочиваются по времени, ID ПР и ID цели.

`torpedo.selfDestruct` содержит захваченный active projectile ID. Работает на паузе, снимает busy и не наносит урон. Устаревшая команда не может уничтожить следующий снаряд. ПР не участвуют в обычных столкновениях и недоступны как Fire target, но выбираются мышью.

## Интерфейс и журнал

Torpedo Launcher: оператор/рейтинг, Fire и Самоуничтожение. Countermeasure Launcher: оператор/рейтинг, auto on/off, состояние и физический countdown. Шесть заголовков доступны в адаптивном layout. ПР — голубой маркер 5 canvas px, solid confirmed trail, dashed forecast, encounter marker; шанс рядом с ПР и прогнозом. Tooltip/info используют frozen breakdown. У пирата показан статус; выбранный пират получает круг 100 км, масштабируемый камерой.

Палитра `Data/UI/combat-visuals.json`: countermeasure, countermeasureTrail, countermeasurePrediction, countermeasureIntercept, defenseRange, defenseText. Отсутствующее/невалидное новое поле использует default; старые обязательные поля продолжают валидироваться.

`CombatJournalEntry` приходит из Engine: Launch, Intercept, Miss, Hit, Destroyed, SelfDestruct, TargetLost с event ID, физическим временем, object IDs, координатами и числовыми фактами. Перехват содержит шанс, roll и оба рейтинга. Сворачиваемый журнал хранит всю историю с прокруткой. UI не делает новый бросок.

Кольцо/terminal trail самоуничтожения и надпись Перехват/Промах живут 2 реальные секунды по monotonic clock независимо от скорости/паузы. Обычные торпедные trails остаются скрытыми. Загрузка устанавливает watermark журнала, сбрасывает эфемерные эффекты и восстанавливает выбранную активную ПР, в том числе при первом snapshot после создания экрана.

## Save v11 и миграция

`GameStateData.DefenseState` / `CountermeasureStateData` v1 сохраняет launchers, PR flights/routes/trails, attempted torpedo IDs, frozen ratings/chance, miss/reload deadlines, RNG v1 state/counter, projectile/journal sequences, журнал и SelectedProjectileId. JSON record-поля DefenseState используют имена C# (Version, Launchers и т.д.). Loader нормализует ссылки с OrdinalIgnoreCase и отклоняет неизвестные версии, orphan/duplicate IDs, несогласованные фазы/busy, шанс, маршрут/след и RNG counter. Валидация выполняется до замены мира.

Сохранения v10 и старше без defense state загружаются без выдуманных назначений. Уже летящая legacy-торпеда без frozen rating получает 30 с явным legacy provenance. Отсутствующие операторы не назначаются автоматически. Save/Load не повторяет RNG resolution, урон или старые анимации.

## Автоматическое доказательство

- Contracts: сериализация DTO/default fields и termination/event payloads.
- Motion: curved predictive route, 500 м до centre rendezvous, launch pose, ограничение поворота и partition invariant prediction.
- Engine: operator schema/runtime, bootstrap/content, pause/range/zero chance, named RNG golden vectors, success/miss/lost target, журнал, malformed/atomic load, четыре фазы Save/Load, настоящий сценарий с включённой защитой.
- `CountermeasureEndToEndTests`: реальный PlayerShipOnly + production registry, seed0/42 для success/miss и три настоящих попадания, обычные команды и save continuation.
- `CountermeasureUiFlowTests`: реальные клики → session → Engine; disable/enable и SelfDestruct на паузе при 1280×720 UI100/150% и 1920×1080 UI120%.
- `CountermeasureSessionRoundtripTests`: настоящий LocalGameSessionConnection Save/Load, выбранная ПР, история и отсутствие replay эффекта.
- Старые fixtures без защиты явно отключают auto; проверки урона/торговли/еды не ослаблены. Тесты исходного состава и потребления еды обновлены под второго человека с разрешения пользователя.

Фактически выполнено: Engine **1153/1153**, Client **1568/1568**, failed=0, skipped=0. Contracts **103/103**, Motion **137/137**. Сборка solution — 0 warnings/0 errors; scoped dotnet format --verify-no-changes и git diff --check — exit0. Итого **2961** автоматических проверки.

Команды из корня DSS:

```powershell
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Motion.Tests/DeepSpaceSaga.Motion.Tests.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet build DeepSpaceSaga.sln --no-restore
# Include содержит только изменённые/новые C# файлы эпика.
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include <changed C# files>
git diff --check
```

Дополнительные регрессии перед финализацией подтвердили и закрыли три дефекта: повреждённый frozen rating/provenance в Save v11 теперь отклоняется атомарно; перепланирование ПР сохраняет настроенные скорость и поворот модуля; длинная запись журнала прокручивается по строкам до последней строки. Legacy regression использует настоящий формат10 без DefenseState. Полные Engine/Client наборы повторно пройдены после исправлений.

## Нативная проверка и статус приёмки

**NOT RUN, исключена из текущего исполнения по прямому ответу пользователя 2026-10-04 «Продолжать без нативной проверки».** Попытка запуска через Computer Use вернула `Computer Use app approval timed out`. Это не PASS и не подтверждение GPU/input качества. Унаследованный EP6 native gate также не выдаётся за пройденный. Автоматическое APPROVED не выставляется.

Чеклист отдельной визуальной приёмки:

1. Собрать Client, открыть NEW GAME → Player Ship and Pirate. Не менять координаты/HP; ускорять штатными 1/2/3/4, пауза Space.
2. При 1280×720 и 1920×1080, UI100/120/150%, нескольких zoom проверить доступность шести заголовков, обеих weapon panels, полного текста кнопок/операторов.
3. На паузе выбрать пирата, Fire, SelfDestruct; HP не меняется, busy снимается, red ring и terminal trail исчезают через 2 реальные секунды. Повторить при ускорении.
4. Выполнить новый пуск, приблизиться до зоны защиты. Проверить голубую ПР, solid/dashed линии, chance/hover breakdown, выбор ПР/info и запрет Fire по ней. Проверить круг 100 км при zoom.
5. Наблюдать success и miss; для miss прямой полёт 2 физических секунды, затем reload10. Пауза останавливает физические таймеры; Перехват/Промах исчезает через 2 реальные секунды.
6. Disable/enable auto не удаляет текущую ПР. Проверить NoOperator на отдельном сценарии без назначения.
7. Открыть/свернуть/прокрутить журнал; Save в новый отдельный слот в Guiding, MissedCoast, Reloading; Load сохраняет фазу/выбор/числа без replay. Не перезаписывать пользовательские слоты.
8. Проверить EP6 HP450→300→150→wreck при промахах защиты. Зафиксировать framebuffer, OS DPI, UI scale, zoom, speed, активное движение и время эффектов.
9. После выхода поменять новый цвет в копии выходного combat-visuals.json, запустить без пересборки, подтвердить применение и вернуть файл.

## Навигация

[План эпика](../../Board/EP-0007-countermeasure-combat/Tickets.md), [решения](../../Board/EP-0007-countermeasure-combat/Documentation.md), [исторический EP6](BasicTorpedoCombat.md), [перестройка графа](../06-Tooling/CountermeasureGraphify.md).
