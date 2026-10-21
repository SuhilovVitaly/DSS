# Load

## Current EP-0012 contract — 2026-10-21

Current writer is SaveFormatVersion18. A real Save17 or earlier is rejected atomically without migration/current-world replacement; NewGame scenario version0 is a separate path. Point flights, reload/captured operators, named scatter/dodge RNG, evasion/AI/encounter and fractional motion remainder survive load. Defeated sessions cannot overwrite a slot; error/cancel remains recoverable. Version17/16/15 text below records earlier stages. Existing modal pause, file IO and menu navigation semantics remain applicable. Headless continuation/rejection passed; native physical Save/Load menu acceptance is NOT RUN. [Point-aim combat contract](../../04-Engineering/PointAimCombat.md).

## Актуализация EP-0009 — 2026-10-12

Load принимает только SaveFormatVersion17; версии16 и ниже отклоняются без миграции до замены мира. Валидируются defense3/combat2/hostile2, состояния модулей, очереди, полёты, AI и RNG. Успешная загрузка из Defeat создаёт Running-сессию на паузе. Указания Save16/15/11 ниже сохранены как история. [Требования](../../01-Requirements/EngineRequirements.md#phase-four-module-combat) · [проверки](../../04-Engineering/ModuleCombat.md).

## Актуализация EP-0008 — 2026-10-12

На этапе EP-0008 Load принимал SaveFormatVersion16; версии15/11 отклонялись без миграции. Version0 NewGame не являлся поддержкой старого save. Полная staged validation defense2/hostile1/combat1 предшествовала замене мира, captured параметры не регенерировались из каталога. Ошибки отображались локализованно и допускали повтор. Загрузка прежнего файла из Defeat создавала новую Running-сессию на паузе; отмена/Escape оставляла Defeat. Текущий формат17 описан выше.

[Текущий контракт](../../01-Requirements/EngineRequirements.md#phase-three-combat) · [Проверки и ограничения](../../04-Engineering/PhaseThreeCombatAcceptance.md).


## Сверка реализации — 2026-10-10

Обновление после исправлений: F01–F05 закрыты кодом и регрессиями; EP-0004 включён в текущий рабочий набор. Баланс, 80 FPS и человеческая приёмка остаются открытыми. Текущий статус — `Documentation/04-Engineering/EpicReview20261010/Fixes.md`.

Load восстанавливает materialized solarSystem/clusterMap и экономику без генерации нового мира. Legacy отсутствие географических блоков сохраняется. Негативный probe выявил недостающую проверку наложения станций (F02); общий атомарный load не доказывает полноту всех геометрических проверок.

[Текущее устройство](../../04-Engineering/TradingAndSolarSystem.md) · [Ревью, дефекты и остаток](../../04-Engineering/EpicReview20261010/README.md). Датированные записи ниже сохраняют историческое значение; они не являются новым подтверждением готовности.

Статус: экран реализован.

Код: `src/DeepSpaceSaga.Client/UI/Screens/Load/`.

## Визуальный стиль

- Внешняя оболочка и заголовок используют `GenericWindowTypeA`.
- Список сохранений занимает единственную внутреннюю панель на всю доступную ширину;
  отдельной правой action-панели нет.
- `CLOSE`, `DELETE/CONFIRM?` и `LOAD` используют `GenericButtonTypeA` и расположены
  одной горизонтальной группой внизу окна.

## Назначение

Окно загрузки сохраненной игры. В требованиях первого релиза используется название `Load`.

## Функциональность первого релиза

- Показывает список доступных save slots.
- Позволяет выбрать слот и загрузить игровую сессию.
- При открытии из `Main Menu` заменяет текущий flow на `Session`.
- При открытии из `Game Menu` загружает выбранную сессию и возвращает игрока на `Session`.
- Должно явно показывать отсутствие сохранений.

## Архитектурные требования

- Загрузка должна идти через `IGameSessionFactory`/session boundary.
- Пока окно открыто поверх сессии, действует modal pause rule.
- Client не должен самостоятельно менять authoritative state без создания/замены session connection.
- После загрузки восстанавливаются склады, бюджеты, `marketRevision`, активные события, `masterSeed` и торговая сеть.
- Загрузка не должна повторно применять последний экономический интервал или торговый receipt.


## EP-0007 — актуальное дополнение от 2026-10-04

Формат11 восстанавливает DefenseState атомарно. Неизвестные версии/несогласованные ссылки, фазы, шансы и маршруты отклоняются до замены мира. Legacy v10 без оператора не получает выдуманное назначение; уже летящая торпеда без rating мигрирует с30 и legacy provenance. Восстанавливаются выбор живой ПР и журнал, старые анимации не проигрываются. [Технический контракт и приёмка](../../04-Engineering/CountermeasureCombat.md).

## Актуализация EP-0004 — 2026-10-08

Текущий SaveFormatVersion=15; формат11 выше относится к исторической поставке EP-0007. AiMap восстанавливается из materialized save без генерации по текущему content; legacy отсутствие остаётся null. Версии, finite geometry, parent references и единое case-insensitive пространство ID валидируются атомарно до замены мира. Реальный Local file Load начинается на паузе; проверены файловый round-trip и торговое продолжение.

[Текущий технический контракт и evidence](../../04-Engineering/AiMapEnvironment.md).
