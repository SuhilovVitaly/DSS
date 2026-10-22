# GameSession

> **Контракт EP-0013.** EP-0013: F12 переключает правую360px панель отладки; Esc сначала закрывает прицел. Мышь над панелью не проходит к карте; игра не ставится на паузу. [Панель/файлы](../../04-Engineering/DebugTuningPanel.md), [WASD/стрелки/Shift и E/Z/C](../../04-Engineering/ManualManeuver.md) являются текущим экранным контрактом. Сфокусированные доклады сохраняют клавиатурную навигацию.

## US20 current behavior — 2026-10-21

Selected navigation reaches its point and continues to the edge; ship image/fields are on the right, module details on the left. NPC torpedo map visuals contain only current marker, short rear tail and dark admitted past trail. Torpedo send exits aiming; own reload completion emits one journal/report/readiness cue. Combat offers pause/1×/2×/5×/10× with real multipliers and Save18 continuation. Earlier conflicting summaries below are superseded for these six changes. See [current contract](../../04-Engineering/PointAimCombat.md) and its execution link; native/audio/human limits remain explicit.

## Current EP-0012 contract — 2026-10-21

The active tactical session shows point aim/CM, threats, own armor/structure tiles, public enemy badges, journal/report facts and encounter status. Enemy module details/numeric damage and foreign projectile endpoint/deadline/target are never displayed. Active locks x5+, banner/frame and real-time timer; Won is nonmodal with Continue acknowledgement; Lost freezes Engine and routes actual statistics to Defeat. UI responds immediately; projectile birth waits for authoritative display-admitted scene, with no ghost. Observed NPC forecast is now reused by current point preview consumers. Native24/24 matrix plus clean probe passed only stated checks; physical OS input, normal transport latency, hearing, deuteranopia and manual usability remain OPEN. Historical combat summaries below do not override this contract. [Point-aim combat contract](../../04-Engineering/PointAimCombat.md).

## Будущая линия NPC — 2026-10-18

[Наблюдаемый прогноз](../../04-Engineering/NpcObservedTrajectories.md) использует три подтверждённые позы на секундной физической сетке: два равных поворота дают дугу, иначе прямую от отображаемой позы. History не зависит от выбора/камеры. Старые правила отображения выбранной цели, цвета и player navigation сохранены. Combat/launch forecasts не переведены на эту модель. Headless draw проверен; настоящее окно/GPU/input NOT RUN.

## Актуализация EP-0009 — 2026-10-12

Выбранный известный корабль показывает броню, структуру и время отключения каждого модуля; карта выводит агрегат модульного состояния вместо Hull HP. Журнал показывает броски попадания/перехвата, повреждения, уничтожение, пропущенную очередь, перегрев и причину гибели. Неизвестные модули противника скрыты. Это проверено headless; реальное окно/ввод NOT RUN по указанию пользователя. [Модульный бой](../../04-Engineering/ModuleCombat.md) · [нативная матрица](../../../Board/EP-0009-battle/evidence/native.md).

## Актуализация EP-0008 — 2026-10-12

Две независимые ПРО PlayerShipOnly создают7 командных панелей. Preview/Fire, pending identity, hover ranges, remaining lifetime и журнал читают authoritative snapshots. Footer и PR labels учитывают панели, HUD и viewport/UI scale. Terminal Defeated переводит экран в Defeat; Escape/отмена Load не возобновляют погибший мир. Автоматические click/session проверки покрывают1280x720/1920x1080 при UI100/120/150%; дальнейшая native проверка отложена.

[Текущий контракт](../../01-Requirements/EngineRequirements.md#phase-three-combat) · [Проверки и ограничения](../../04-Engineering/PhaseThreeCombatAcceptance.md).


## Сверка реализации — 2026-10-10

Обновление после исправлений: F01–F05 закрыты кодом и регрессиями; EP-0004 включён в текущий рабочий набор. Баланс, 80 FPS и человеческая приёмка остаются открытыми. Текущий статус — `Documentation/04-Engineering/EpicReview20261010/Fixes.md`.

EP-0002–0003 реализованы в base-fight: seeded система и общая орбитальная математика, кластеры/ресурсы/торговые направления, текущие ETA и materialized Save/Load. EP-0004 интегрирован в текущий незакоммиченный рабочий набор; native80FPS FAILED. F02–F04 исправлены и проверены новыми регрессиями. Панели показывают состояние рейса; карта потребляет snapshot и общую Motion-проекцию, не вызывает Engine из Render.

[Текущее устройство](../../04-Engineering/TradingAndSolarSystem.md) · [Ревью, дефекты и остаток](../../04-Engineering/EpicReview20261010/README.md). Датированные записи ниже сохраняют историческое значение; они не являются новым подтверждением готовности.

Статус: экран уже существует в коде.

Код: `src/DeepSpaceSaga.Client/UI/Screens/GameSession/`.

## Назначение

Основной экран первого релиза. На нем игрок видит тактическую карту, управляет кораблем Tetrarch Class, выбирает объекты, открывает игровые панели и выполняет основные действия игрового цикла.

## Уже существующая функциональность

- Тактическая карта с камерой и масштабированием.
- Отрисовка объектов, маркеров, подписей, орбитальных/траекторных элементов и трейлов.
- Панель скоростей: `Speed0`, `Speed1`, `Speed2`, `Speed3`, `Speed4`.
- Панель масштаба карты.
- Панель команд модулей корабля.
- Команды двигателя, включая маневрирование, синхронизацию скорости/направления и orbit.
- Выбор и hover объектов через `ActiveObjectId` и `SelectedObjectId`.
- Информационная панель и панель игрокского корабля.
- Quick Save / Quick Load через клавиши.
- `Escape` открывает `GameMenu` как modal screen.

## Функциональность первого релиза

- Игрок должен уметь долететь до станции, астероида или выбранного celestial object.
- Игрок должен видеть состояние грузового отсека и запас `Fuel` в баках двигателя.
- После успешной команды `navigation.dock` игрок должен получить доступ к экрану станции, а с него - к торговле и диалогам станции.
- При достижении астероида игрок должен получить доступ к добыче льда через Drilling Unit, если условия добычи выполнены.
- Игрок должен иметь доступ к диалогам экипажа с этого экрана или через отдельную панель/экран.
- Из `Session` должны открываться окна `Game Menu`, `Station`, `Ship`, `Loot`, `Character Communication`, `Cargo` и `Dialog` согласно минимальной схеме первого релиза.

## Требующие проектирования элементы UI

- Базовые шесть панелей (семь в PlayerShipOnly): Navigation, Maneuver, Engine, Space Control, Torpedo Launcher, Countermeasure Launcher (реализованы).
- Команды `navigation.dock` и `navigation.stationsList` навигационного компьютера.
- Команды Scanner: `scanner.generalScan`, `scanner.structuralScan`, `scanner.nearbySignatures`.
- Команды Drilling Unit: `mining.extractIce`, `mining.stopExtraction`.
- Панель грузового отсека.
- Панель `Fuel`.
- Вход в окно `Ship`.
- Вход в окно `Cargo`.
- Вход в окно `Loot` для результатов добычи/подбора.
- Вход в диалоги экипажа.
- Вход в диалоги представителей станции.
- Состояние стыковки/синхронизации с объектом.
- Состояние добычи льда и причины недоступности mining commands.

## Связанные механики

- `TetrarchClass`.
- `CommandPanels`.
- `TacticalMapAndManeuvering`.
- Docking.
- Trading.
- `CargoHold`.
- `CrewDialogues`.
- `StationDialogues`.
- `IceMining`.
- `Fuel`.


## Боевой этап EP-0007

Экран отображает назначенных операторов, самоуничтожение торпеды и автоматическую защиту. ПР выбирается на карте, но недоступна для Fire. Frozen chance/breakdown приходят в snapshot; UI не бросает RNG. Сворачиваемый журнал сохраняет authoritative историю. Круг100км виден у выбранного пирата. [Контракт, Save/Load и проверки](../../04-Engineering/CountermeasureCombat.md).

## Актуализация EP-0004 — 2026-10-08

Карта показывает AI diamond/owner/type, движущиеся defence/patrol circles, Radiation/Dust/Debris и известные POI. Toolbar переключает четыре слоя; выбор перекрытий циклический. Поля показывают отсутствие эффектов, POI — отсутствие исследования. База недоступна для стыковки/торговли и в UI, и в Engine. Панель AI/POI/field компактна и прокручивается на малом viewport; выбор descriptors не отправляет engine command. Native interactions8/8 PASS;80FPS acceptance OPEN.

[Текущий технический контракт и evidence](../../04-Engineering/AiMapEnvironment.md).

## Боевые доклады экипажа — 2026-10-16

[EP-0010](../../../Board/EP-0010-combat-reports/ImplementationStatus.md) добавляет панель справа под фактическим Object Info. История200 карточек newest-first, ровно одна раскрыта; новый report раскрывается и прокручивает список наверх. Click/drag/wheel поглощаются панелью, карта/команды/скорость не меняются; keyboard Up/Down/Enter действует только при её focus. Новое событие не перехватывает focus. Правая колонка исключена из свободного map viewport, journal/footer не перекрываются. На компактной высоте тело Object Info прокручивается.

UI получает immutable captured факты E01–E07 из Engine независимо от свежести world snapshot. RU/EN formatter и8 иллюстраций/portrait role fallback подготовлены до Render; Draw не загружает файлы. New Game/Load создают чистую историю без replay, modal очищает focus с сохранением истории; закрытие/Defeated освобождает ресурсы. [Полный текущий контракт и limits](../../04-Engineering/CombatReports.md). Headless matrix проверена; native/human/FPS OPEN, самостоятельный запуск игры агентом запрещён пользователем.
