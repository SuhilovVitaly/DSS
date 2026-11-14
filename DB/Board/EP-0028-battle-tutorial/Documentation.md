---
epic: EP-0028-battle-tutorial
title: Обучение в тактическом бою
stage: approved
created: 2026-11-14T15:04:43Z
source_request: "новый эпик «Обучение»: научить игрока управлять Tabletop-боем в первые ходы; учит экипаж корабля игрока; ненавязчиво, но информативно; можно отключить; зависит от языка игры. Создай тикеты, затем выполни весь эпик. Коммит и push после каждого тикета."
current_review: complete
epic_approval: "План EP-0028 (Full mode) утверждён пользователем в сессии оркестратора 2026-11-14; точная формулировка сообщения в Board не передана — см. Review log."
epic_approval_utc: 2026-11-14
---

# EP-0028 — Обучение в тактическом бою

В первом бою новой игры (бой обучения из [EP-0027](../EP-0027-tutorial-start/Documentation.md)) офицер носителя «Скиталец» по связи подсказывает, как управлять Tabletop-боем: выбрать пилота, проложить курс, выбрать цель, пережить залп и понять итог. Подсказки не блокируют ввод, звучат в паузах, их можно отключить в Настройках и прямо в бою, а все тексты берутся из файлов локализации RU/EN.

## Исходное техническое задание

Запрос пользователя 2026-11-14 (пересказ из утверждённого контракта оркестратора): новый эпик «Обучение»; научить игрока управлять Tabletop-боем в первые ходы; подсказки даёт экипаж корабля игрока; ненавязчиво, но информативно; можно отключить; должно зависеть от языка игры. Создать тикеты, затем выполнить весь эпик. **Коммит и push после каждого тикета.**

Инструкциями для исполнителя являются решения D-01…D-08 ниже, правила [DB/Documentation/00-Process/AGENTS.md](../../Documentation/00-Process/AGENTS.md) и [EpicExecutionPrompt.md](../EpicExecutionPrompt.md). Черновой список уроков, исследовательские принципы и примеры реплик — продуктовый материал: исполнитель может уточнять формулировки в пределах тона и лимитов.

## Решения пользователя (обязательные)

- **D-01. Говорящий.** Единственный новый персонаж — **женщина-офицер носителя «Скиталец»** (не пилоты дронов). Нужны имя RU/EN, портрет и гайд по тону.
- **D-02. Рекомендательные подсказки.** Подсказки никогда не блокируют ввод. Шаг завершается, когда игрок сам выполнил действие. Никакого жёсткого гейтинга и soft-lock.
- **D-03. Только Tabletop.** Бой обучения всегда идёт в скине Tabletop: подсказки размечены под него.
- **D-04. Простое вкл/выкл.** Без уровней. Переключатель в Настройках главного меню и кнопка на панели связи в бою. Хранится в `Settings.json` → `gameSettings`.
- **D-05. «Повторить обучение» отложено.** Бывшая US-0007 в эпик не входит (см. Backlog).
- **D-06. Зависимость от языка.** Все строки игроку (реплики, имя офицера, кнопки, журнал, настройки) — из `Data/Locale/{Russian,English}.json`; RU и EN полные и в паритете (тесты паритета); строка разрешается в текущем языке игры; тексты помещаются в панель на обоих языках.
- **D-07. Только клиент.** Без изменений Engine, Contracts и формата сохранений; отрисовка не вызывает Engine синхронно; не потребляет RNG и не подсматривает будущие FireSteps ([04-Engineering/TacticalBattle.md](../../Documentation/04-Engineering/TacticalBattle.md#снимки-и-воспроизведение)).
- **D-08. Публикация.** Отдельный коммит и `git push` после каждого тикета, ветка `codex/ep-0028-battle-tutorial`.

## Grounding (факты по коду на базе d8ab8f71)

- Engine знает только фазы `Move, Fire, Over` (`src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs:6`). Salvo/Results/Outcome — клиентские состояния представления: `PhasePanel.Phase` (`src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.cs:14`).
- Управление: выбор пилота кликом по карточке/дрону или Tab/Shift+Tab, курс стрелками (±5°, ±2 скорости), перетаскивание конца пути и кнопки карточки, Enter/основная кнопка — отправка хода (`TacticalBattleScreen.cs:197-236`, `TacticalBattleScreen.Overlay.cs:81-136`). Стрельба: клик по врагу или список целей на карточке, «Не стрелять», E — уклонение, D — ловушка (`TacticalBattleScreen.cs:217-235`). Залп: Enter/Space пропуск, «Повтор» (`TacticalBattleScreen.cs:192-196, 393-399`). Over: «На карту»/Esc (`TacticalBattleScreen.cs:190`). В Tabletop фаза Move начинается без выбранного пилота (`TacticalBattleScreen.cs:285-286`).
- Неотредактированные дроны летят по тактике ИИ; бой обучения доходит до Over одним Enter (`tests/DeepSpaceSaga.Client.Tests/TacticalBattle/TutorialFlowTests.cs:175-185`).
- Бой `initial` (`src/DeepSpaceSaga.Client/Data/TacticalBattle/scenarios.json:2-20`): сторона A — носитель «Скиталец» (ID юнита не локализуется) и дроны Вера/Юн/Марта/Олег с `decoy.t1`; сторона B — «Логово» и Таг-1…6. Обучение стартует с seed 1, комплектацией по умолчанию, без подготовки (`src/DeepSpaceSaga.Client/UI/SkiaWindow.cs:1080-1121`, запрос — `:1107`).
- Модели экипажа в бою нет; движок диалогов привязан к Engine и в бою заблокирован (`src/DeepSpaceSaga.Engine/Dialogue/SimulationEngine.Dialogue.cs:15`, шлюз `SkiaWindow.cs:810-817`). Переиспользуемые визуальные средства: `DialoguePortraitComposer.Compose(string?, PersonSex, string, string?)` (`src/DeepSpaceSaga.Client/UI/Portraits/DialoguePortraitComposer.cs:25-55`), головы W4 обнаруживаются из папки `Portraits` (`src/DeepSpaceSaga.Client/Portraits/PortraitAssetRepository.cs:21,66-74`), портреты пилотов — `Data/UI/tactical-battle-pilots.json` + `PilotPortraitCatalog` (`PilotPortraitCatalog.cs:10-39`).
- Отрисовка: `TacticalBattleScreen.RenderFrame` (`TacticalBattleScreen.cs:153-178`) → `CaptureFrame` → карта → `DrawOverlay` (`TacticalBattleScreen.Overlay.cs:35-50`) → подтверждение выхода; макет 1280×720. Якоря UI записываются в `BattleLayout.Rects` через `BattleDesign.Recording` (`BattleDesign.cs:84`, `BattleLayout.cs:6-11`) и доступны как `screen.Layout` (`TacticalBattleScreen.cs:49,165-166`). Ключи: `pilot:<unitId>` (`PilotCardColumn.cs:208`), `card-command:<action>` (`PilotCardColumn.cs:48`), `endpoint:<unitId>` (`BattleMapRenderer.Movement.cs:35`, `BattleMapRenderer.Templates.cs:49`), `reachable` (`BattleMapRenderer.Templates.cs:23`), `candidate:<id>` (`BattleMapRenderer.Fire.cs:194`, `BattleMapRenderer.FireTemplates.cs:395`), `fire-chevron:<id>` (`BattleMapRenderer.FireTemplates.cs:284`), `evade-ring:<id>` (`BattleMapRenderer.cs:169`), `button:<id>` (`PhasePanel.cs:61`), `minimap` (`TacticalBattleMinimap.cs:56`). Панели захватывают ввод через `OverlayRects`/`OverlayHit` (`TacticalBattleScreen.Overlay.cs:21-34`).
- Точки наблюдения: `OnBattleUpdated(before, response)` (`TacticalBattleScreen.cs:256-276`), `ObserveOrders` (`:278-296`), `TacticalBattleFrame` с `SelectedDroneId`, `Movement` (`TacticalOrderPreview.Touched`), `FirePlan` (`TacticalFirePlanRow.TargetId`/`Group.DefaultTargetId`/`Evade`/`Decoy`), `AimingMovement` (`TacticalBattleScreen.cs:11-17,99-134`; `TacticalBattleOrders.cs:8-16`; `TacticalBattleFireOrders.cs:10-24`). `TacticalBattleFlow.IsTutorial` — клиентский признак (`TacticalBattleFlow.cs:26-27`), уже передаётся в `PhasePanel.Tutorial` (`TacticalBattleScreen.Overlay.cs:39`). Окно боя создаётся в `SkiaWindow.CreateTacticalBattleView` с предпочтительным скином (`SkiaWindow.cs:1208-1209`).
- Настройки: `Settings.json` рядом с exe, группа `gameSettings` (`language`, `uiScale`, `tacticalBattleSkinId`). Образец записи предпочтения: `TacticalBattleSkinPreference` (JsonNode-merge, атомарная запись temp+Move, сообщение об ошибке) (`TacticalBattleSkinPreference.cs:11-113`). Экран настроек: `SettingsScreen.cs:86-111` (внутренний конструктор со скинами), `:290-324` (Render); геометрия — `SettingsLayout.cs:25-88`; открывается только из главного меню (`MainMenuScreen.cs:60`, `SkiaWindow.cs:1353-1391`). Каталог скинов: `GetPreferred` по умолчанию `tabletop`, `TryGet(id, out skin)` (`TacticalBattleSkinCatalog.cs:56-66`); пакеты ищутся в `Skins` рядом с `Settings.json` (`SkiaWindow.cs:111-113`).
- Локализация: плоские ключи; `Localization.Get` возвращает ключ при отсутствии (`src/DeepSpaceSaga.Client/Localization.cs:16-18`), `UseLocaleForTests` (`:52`), `LoadLocaleFile` (`:99-114`). Существующий паритет RU/EN проверяет только префикс `TacticalBattle.` (`tests/DeepSpaceSaga.Client.Tests/TacticalBattle/LocalizationCoverageTests.cs:110-143,218-226`). Безопасность глифов и перенос: `ControlText.SafeSymbols`/`Wrap` (`src/DeepSpaceSaga.Client/UI/Controls/DropDown.cs:264-320`).
- Метрики боя: `TacticalBattleMetrics.DirectoryPath` (`DSS_VALIDATION_ROOT/battle-metrics` или LocalAppData) и `ReportPath(battleId, dir)` (`TacticalBattleMetrics.cs:17-20,61-65`), запись из `TacticalBattleFlow.ExportMetrics` (`TacticalBattleFlow.cs:108-116`).
- Нативный ввод клавиш идёт через опрос `KeyboardEdgeTracker.StandardKeys` + клавиши манёвра (`src/DeepSpaceSaga.Client/UI/KeyboardEdgeTracker.cs:9-16,26-35`; `Data/UI/maneuver-input.json`). Клавиш `H` и `E` в этом списке нет (D приходит из `turnRight`).
- `ForbiddenCallsTests.ProductionTacticalBattleSourceUsesOnlyApprovedOperations` (`tests/DeepSpaceSaga.Motion.Tests/TacticalBattle/ForbiddenCallsTests.cs:14-26`) сканирует все `src/**/*.cs`, путь которых содержит `/TacticalBattle/`: запрещены `Math.Min/Max/Clamp/IEEERemainder`, `Array.Sort`, `.Sort/.Distinct/.DistinctBy(`, `HashSet<`, `ToString("F0"/"N0")`, `/` без явного double-операнда. На базе тест уже падает (например `TacticalBattleScreen.cs:318,325`, `TacticalBattleScreen.Overlay.cs:43,147`).

## Архитектурное размещение

- Весь новый код обучения — в `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/` (namespace `DeepSpaceSaga.Client.UI.Screens.Tutorial`). Путь не содержит `/TacticalBattle/`, поэтому `ForbiddenCallsTests` его не сканирует; тем не менее новый код не использует RNG и Engine.
- Правки внутри `UI/Screens/TacticalBattle/` — только тонкие хуки (наблюдение кадра, отрисовка, маршрутизация ввода, `OverlayRects`) и **не добавляют находок** `ForbiddenCallsTests`: без `Math.Min/Max/Clamp`, сортировок, `Distinct`, `HashSet<`, `F0/N0`, целочисленного `/` (при необходимости — `PyMath` из `DeepSpaceSaga.Motion.TacticalBattle`, явный `double`). Проверка: мультимножество `<файл>: <причина>` из сообщения теста после правки не больше базового (номера строк игнорируются).
- Данные уроков — `src/DeepSpaceSaga.Client/Data/Tutorial/battle-tutorial.json` (копируется правилом `Data\**\*.json`, `DeepSpaceSaga.Client.csproj:296`); тексты — только ключи `Tutorial.*` и `Settings.TutorialHints*` в Locale.
- Тесты — `tests/DeepSpaceSaga.Client.Tests/Tutorial/` (namespace `DeepSpaceSaga.Client.Tests.Tutorial`); тесты, переключающие язык, входят в коллекцию `"Battle localization"` (`LocalizationCoverageTests.cs:16-19`).

## Допущения

- **A-01.** Подсказки работают только в бою обучения (`TacticalBattleFlow.IsTutorial`); бой по F8 панели связи не показывает и не меняется.
- **A-02.** Отсутствующий или некорректный `gameSettings.tutorialHints` означает «включено»; поставляемый `src/DeepSpaceSaga.Client/Settings.json` не меняется.
- **A-03.** Подписи кнопок называют действие: при включённых подсказках кнопка «Подсказки: выкл»; при выключенных панель сворачивается в компактный значок «Подсказки: вкл», которым их можно вернуть в том же бою.
- **A-04.** Журнал связи (H) доступен и при выключенных подсказках; содержит только реплики, показанные в текущем бою. Esc закрывает открытый журнал первым, иначе прежняя семантика Esc.
- **A-05.** Офицер по умолчанию: «лейтенант Ирина Вольская» / "Lieutenant Irina Volskaya", «офицер тактической связи „Скитальца“» / "tactical comms officer, Skitalets". В EN носитель — "Skitalets" (ID юнитов в бою не локализуются, `scenarios.json:20`). Имя может уточнить narrative-designer в US-0003-TK-0001 без изменения кода.
- **A-06.** Портрет — существующая голова W4 `Images/Persons/W4/Portraits/CHR-20260915-104309-12BAB0.png` (не используется пилотами Вера/Марта и `CharacterPortraits.DefaultFemale`), композиция `DialoguePortraitComposer.Compose(path, PersonSex.Female, "tutorial.officer")` 300×300; новых бинарных ассетов нет. Если композиция вернула null — нейтральная заглушка панели (рамка + инициал имени).
- **A-07.** Если пакета Tabletop нет, бой обучения открывается в предпочтительном скине с записью в InterfaceLog; подсказки работают, подсветка цели может отсутствовать.
- **A-08.** Метрики шагов — локальный JSON рядом с отчётами боя, без персональных данных и сети.
- **A-09.** `Data/Locale/*.json` и `Data/Tutorial/*.json` — слой content-data; общие UI-строки обучения вынесены в отдельный тикет US-0002-TK-0001, чтобы кодовые тикеты оставались в слое client.

## Gaps

- **G-01.** E (уклонение) обрабатывается экраном (`TacticalBattleScreen.cs:220`), но не опрашивается нативно (`KeyboardEdgeTracker.cs:9-16`): по коду клавиша E в окне может не работать. В эпике не исправляется; урок называет кнопку на карточке, клавиши упоминаются только после подтверждения в US-0006-TK-0002; при подтверждении дефекта — тикет в [EP-maintenance](../EP-maintenance/Documentation.md).
- **G-02.** H не опрашивается нативно — добавление H в `StandardKeys` входит в US-0002-TK-0005.
- **G-03.** Базовые падения: `ForbiddenCallsTests` и 633 теста полного прогона падают на базе (см. [EP-0027](../EP-0027-tutorial-start/Documentation.md#статус-исполнения)). Каждое падение классифицировать относительно базы.
- **G-04.** Выигрываемость боя обучения на seed 1 при игре «по подсказкам» не проверена — US-0005-TK-0002.

## Истории

| История | Результат | Зависимости |
|---|---|---|
| [EP-0028-US-0001-hint-framework](EP-0028-US-0001-hint-framework/EP-0028-US-0001-hint-framework.md) | Формат уроков, директор шагов, сигналы боя, подключение к экрану обучения | none |
| [EP-0028-US-0002-comms-panel](EP-0028-US-0002-comms-panel/EP-0028-US-0002-comms-panel.md) | Тексты UI, панель связи, подсветка цели, правила «не перебивать», журнал H | US-0001 |
| [EP-0028-US-0003-officer-lessons](EP-0028-US-0003-officer-lessons/EP-0028-US-0003-officer-lessons.md) | Офицер «Скитальца» и уроки: движение, стрельба и залп, контекст, разбор | US-0001, US-0002 |
| [EP-0028-US-0004-hints-toggle](EP-0028-US-0004-hints-toggle/EP-0028-US-0004-hints-toggle.md) | Вкл/выкл в Настройках и в бою, хранение в Settings.json | US-0001, US-0002 |
| [EP-0028-US-0005-reliable-first-battle](EP-0028-US-0005-reliable-first-battle/EP-0028-US-0005-reliable-first-battle.md) | Принудительный Tabletop и гарантированно выигрываемый первый бой | US-0001 |
| [EP-0028-US-0006-metrics-acceptance-docs](EP-0028-US-0006-metrics-acceptance-docs/EP-0028-US-0006-metrics-acceptance-docs.md) | Метрики шагов, нативная приёмка, документация (последняя история) | US-0001…US-0005 |

Всего 6 историй и 22 [тикета](Tickets.md). Порядок исполнения — последовательно по [Tickets.md](Tickets.md); каждый тикет зависит только от тикетов выше по списку.

## Известные последствия для существующих тестов

Заранее внесены в `files_touched` соответствующих тикетов:

| Тест | Причина | Тикет |
|---|---|---|
| `TacticalBattle/TutorialFlowTests.cs` | Сессия обучения в бою обучения; фикстура пишет скин `classic` без папки `Skins` (`:288-289`); seed 1 в `:36`; принудительный Tabletop | US-0001-TK-0004, US-0004-TK-0003, US-0005-TK-0001, US-0005-TK-0002 |
| `SettingsScreenTests.cs` | Новая строка подсказок; проверка геометрии 640×480 (`:104-115`), `ExitY == 396` (`:35`) | US-0004-TK-0002 |
| `TacticalBattle/SkinBattleIntegrationTests.cs` | Клики по геометрии настроек (`:177-197`); регрессия «F8 использует предпочтение» | US-0004-TK-0002, US-0005-TK-0001 |
| `ScreenEventTests.cs` | Клик «Выход» по `SettingsLayout.ExitY` (`:248-291`); `ExitY` не меняется — регрессия | US-0004-TK-0002 |
| `Tutorial/TutorialScreenIntegrationTests.cs`, `Tutorial/TutorialDirectorTests.cs`, `Tutorial/TutorialContentTests.cs`, `Tutorial/TutorialLessonFlowTests.cs` | Новые файлы, дополняются несколькими тикетами подряд | см. `files_touched` тикетов |
| `Motion.Tests/TacticalBattle/ForbiddenCallsTests.cs` | Только запуск и сравнение находок с базой, файл не меняется | все тикеты с правками в `UI/Screens/TacticalBattle/` |
| `KeyboardEdgeTrackerTests.cs` | H добавляется в опрашиваемые клавиши | US-0002-TK-0005 |

## Вне объёма и backlog

- «Повторить обучение» из меню (бывшая US-0007) — отложено решением D-05.
- Подсказки в обычных боях по F8 и уровни подробности — нет (A-01, D-04).
- Исправление нативной клавиши E (G-01) — EP-maintenance после подтверждения.
- Озвучка, анимация портрета, новые арты — нет.

## Проверки

Артефакты — только `D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/<ticket>-<run>/`. Каждый тестовый процесс ограничен process-tree cap 300000 ms; `--results-directory` — абсолютный путь в этой папке. Игру не запускать на самом большом мониторе: `DSS_VALIDATION_SMALL_MONITOR=1` и явный меньший монитор. Полные `dotnet build DeepSpaceSaga.sln` и `dotnet test DeepSpaceSaga.sln` — один раз после последнего функционального тикета (US-0006-TK-0001), с классификацией падений относительно базы.

## Review log

2026-11-14 — Grounding и план (Full mode) подготовлены оркестратором и утверждены пользователем в сессии оркестратора; контракт сохранён в `D:\Claude\Antropic\stories\story-20261114-ep0028-battle-tutorial.md`. Точная формулировка сообщения пользователя в Board не передана; здесь она не цитируется.

2026-11-14T15:04:43Z — истории и тикеты материализованы на Board по утверждённому плану (workflow approval тикетов, без отдельного человеческого согласования каждого тикета).

## Epic review log

Состав из 6 историй взят из раздела «Planned stories» утверждённого плана без изменений; документационная часть — последний тикет US-0006 (требование [EpicExecutionPrompt.md](../EpicExecutionPrompt.md) §6).

## Статус исполнения

2026-11-14: Board-артефакты созданы; реализация не начата.
