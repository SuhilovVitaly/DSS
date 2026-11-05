---
epic: EP-0015-tactical-carrier-battle
title: "Тактический бой V4: носитель и крыло дронов вместо боя реального времени"
stage: in-progress
created: 2026-11-05
source_request: "Сделать в DB/Board эпик 15 по образу эпиков, переводящий боевую систему на созданную в VData/Planning/V4.0; бой начинается клавишей в тактической карте; данные V4.0 — JSON в контенте проекта"
revision: 3
baseline: "main @ fb39a0cf08298a9322c84fa982bc7db614f64464"
design_source: "VData/Planning/V4.0 (README, CombatSystem.md, Spec/, Data/, Reference/)"
validation_status: in-progress
story_count: 17
ticket_count: 78
---

# EP-0015 — Тактический бой V4

[Порядок исполнения](Tickets.md)

## Статус

Исполнение начато 2026-11-05 по прямому поручению пользователя. US-0001 TK-0001 реализован и проверен; остальные тикеты в работе по зависимостям. Новый бой ещё не подключён к игре. Фактические проверки и публикация — в ExecutionStatus.md. Historical planning review revision 3 сохранён ниже; он не отменяет текущего разрешения исполнения.

## Разрешение публикации и запуск исполнения — 2026-11-05

Текущее прямое указание пользователя: «Разрешаю выполнять эпик и пушить в текущий GitHub origin, ограничение D-USER-09/R-06 отменяю».

Это указание заменяет запрет внешнего remote в D-USER-09, G-04, R-06 и связанных Definition of Done всех карточек. Разрешён текущий `origin`: `https://github.com/SuhilovVitaly/DSS`. Каждый завершённый тикет по-прежнему получает отдельный коммит и немедленный push с проверкой удалённого SHA. Исторические формулировки ниже сохранены как контекст планирования и не являются действующим запретом GitHub.

Preflight выявил отсутствующие обязательные исходные материалы D-USER-07/G-02; подробности и условия продолжения — [ExecutionStatus.md](ExecutionStatus.md). Реализация тикетов ещё не начата; отмена ограничения публикации не означает завершения критериев эпика.

## Уточнение источников — 2026-11-05

- **D-USER-14.** Источник материалов V4 — `D:/DeepSpaceSaga/DSS/VData/Planning/V4.0`; пользователь сообщил, что справочная реализация не нужна. Её наличие, Python-экспорт, сравнение с `wing4.py` и запуск Python-тестов больше не являются prerequisites. Правила реализуются по Spec; независимые ожидаемые результаты — существующие Markdown Reference.
- **D-USER-15.** На уточнение об отсутствии JSON пользователь ответил: «Да, создать JSON из Markdown». Восстановить JSON контента по таблицам Data/README, Frames/README, Modules/*/README и машиночитаемые ожидаемые результаты по Markdown Reference. Не генерировать ожидаемые исходы новым C# движком. Округлённые до шести знаков данные trace проверяются с точностью исходной записи, а не выдаются за исходные IEEE-hex. Отсутствующие в Markdown поля эталонов не выдумываются; их coverage фиксируется отдельно.
- **A-14 (техническое решение).** `tools/tactical-battle/Export-Content.ps1` экспортирует JSON из Markdown и проверяет актуальность через `-Check`; замена отсутствующих `v4_update.py`/`v4_reference.py` не создаёт симулятор. Источники V4.0 (14 существующих Markdown) включаются в Git вместе с первым тикетом для воспроизводимости экспорта на новом checkout; прочие VData/Planning не затрагиваются.

Эти текущие указания заменяют противоречащие им зависимости от Python и исходного JSON ниже и в карточках. Linux parity R-05 остаётся отдельным требованием проверки платформы; отсутствие Linux не блокирует независимую историю контента US-0001.

## Источник и границы полномочий

Пользователь, 2026-11-05: «по образу и подобию эпиков сделай в DB/Board эпик номер 15 который переводит боевую систему в ту которую мы создали сейчас. количество юзерсторис неограничено. симулятор боя делать не нужно. тактический бой должен начинаться по нажатию кнопки … в тактической карте. … вся информация о модулях, дронах и прочем которая есть у нас в VData/Planning/V4.0 должна быть помещена в проект в соответственные места в виде джейсонов».

- **Образец формата.** Эпики `DB/Board/EP-0012…EP-0014` из ветки `codex/hex-wing-simulator` репозитория (использован клон только для чтения вне репозитория; локальный `DB/Board` совпадает с ним). Навык `$epic-writer` (`D:/DeepSpaceSaga/.agents/skills/epic-writer`) в этой среде отсутствует; формат взят из существующих эпиков и `EpicExecutionPrompt.md`.
- **«Симулятор боя делать не нужно».** Отдельный инструмент-симулятор не входит в эпик: справочная реализация уже есть (`tools/DeepSpaceSaga.CarrierWingSim`, режим V4). Эпик переносит бой в игру.
- Этим поручением созданы только файлы Board. Код, контент и `DB/Documentation` не менялись.

## Решения пользователя

| ID | Решение (ответы 2026-11-05) |
|---|---|
| D-USER-01 | **Заменить старый бой полностью.** Бой реального времени EP-0006…EP-0014 (торпеды в точку, ПРО, уклонение, ИИ «Мух», encounter, журнал и отчёты боя) удаляется; бой V4 — единственная боевая система. |
| D-USER-02 | **Кто воюет:** бой сценария из данных V4 (сейчас «Начальный бой»: 4 дрона и носитель игрока против 6 тагов и их носителя), независимо от объектов тактической карты. |
| D-USER-03 | **Песочница:** бой не меняет мир; после боя — возврат на тактическую карту; сохранение посреди боя не нужно. |
| D-USER-04 | **JSON V4.0 переносятся в контент проекта задачей эпика** (US-0001), не этим поручением. |
| D-USER-05 | **F9 остаётся быстрой загрузкой.** Бой запускается другой клавишей. |
| D-USER-06 | **Клавиша боя — F8** (подтверждено 2026-11-05). Задаётся в данных UI. |
| D-USER-07 | `DB/Planning/` и `tools/DeepSpaceSaga.CarrierWingSim` пользователь коммитит сам до исполнения. |
| D-USER-08 | **Эпик исполняется на другом компьютере.** Каждый тикет — отдельный коммит и push сразу после тикета, как требует `EpicExecutionPrompt.md`. |
| D-USER-09 | **Исполнение — на другом VDI.** Push — только в одобренный внутренний remote, настроенный на этом VDI (`*.amat.com`, например Azure DevOps `il-app-128.amat.com`, или внутренний IP). Первый шаг исполнителя — `git remote -v`; если `origin` внешний (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (политика организации). |
| D-USER-10 | **Дрон без приказа летит по предложению ИИ** (правило Engine). Экран показывает у нетронутых дронов предложение ИИ и отправляет приказы только тронутых; поведение справочной страницы «прямо по умолчанию» (`play4.html:308`) не переносится. |
| D-USER-11 | **Язык пока русский.** Ключи боя — в `Russian.json`; в `English.json` те же ключи черновиком без вычитки; смена языка, как и сейчас, — после перезапуска; английские имена данных, вычитка и проверка на английском — вне эпика. |
| D-USER-12 | **Просмотр боёв ИИ остаётся вне эпика.** Отрисовщик карты и панели строятся от неизменяемого кадра, чтобы просмотр можно было добавить позже. |
| D-USER-13 | **Эталоны V4.0 можно перегенерировать**, если порт тригонометрии на Windows не совпадает с glibc (US-0002 AC-06, вариант 2), с записью в `V4.0-archive/Changes` и новой целью в §1. |

## Assumptions и gaps

- **A-02.** Правила, ИИ, данные и эталоны — `VData/Planning/V4.0` на 2026-11-05 (цель «Начального боя» 80.9 / 57.2 на сидах 1…1000). Порт обязан совпасть с `Reference/` **бой в бой**, включая число вызовов генератора.
- **A-03.** Бой — непрозрачный модальный экран поверх тактической карты (ScreenStack), мир на паузе Speed0; Engine ведёт бой отдельной сессией с собственным потоком mulberry32 (не именованные SplitMix64-потоки мира: правила V4 требуют mulberry32). RNG живёт в Engine; прогнозу Client он не нужен.
- **A-04.** Шанс попадания для прогноза Client и для Engine — одна функция в Motion; Engine добавляет только броски.
- **A-05.** Журнал боя в Contracts — типизированные события; тексты формирует Client по локали (тексты V4 — шаблоны Spec/Display §6).
- **G-01.** Закрыт: клавиша F8 (D-USER-06).
- **G-02.** На baseline `DB/Planning/` и `tools/DeepSpaceSaga.CarrierWingSim` не под git. Пользователь коммитит их до исполнения (D-USER-07); исполнитель проверяет их наличие в checkout первым шагом.
- **G-03.** Бит-точность: `Spec/Combat.md` §1.1 предполагает glibc для `atan2`/`cos`/`sin`. .NET на Windows использует CRT, результат может отличаться в последнем бите и разойтись с эталоном. Python на Windows тоже берёт UCRT, поэтому JSON-эталоны собираются только на Linux/glibc (WSL2 на VDI исполнения). Решение — по замеру US-0002 TK-0006: сначала корректно округлённые функции в Motion, затем, если нужно, перегенерация эталонов (D-USER-13).
- **G-04.** Публикация: в checkout планирования `origin` = `https://github.com/SuhilovVitaly/DSS.git` — внешний хост, push туда запрещён политикой организации. Эпик исполняется на другом VDI (D-USER-09): push только в одобренный внутренний remote; без него тикеты коммитятся локально, а push записывается в ExecutionStatus как NOT RUN с причиной. Среда планирования ничего не коммитит и не публикует.
- **G-05.** Границы удаления старого боя (DefeatScreen, `ship.mukha`/`ship.shmel`, CombatLaser, Scanner, DebugTuning) доказываются инвентаризацией consumers (US-0011 TK-0001), не текстовым поиском. Удаление — сверху вниз (Client → LocalClient → Engine → Motion → Contracts → контент и инструменты), каждый тикет компилируемый.
- **G-06.** Закрыт D-USER-11: язык русский, имена из данных V4 показываются как есть по id.
- **G-07.** `tools/DeepSpaceSaga.CombatBalanceSim` и `tools/HexWingSimulator` на baseline тоже не под git; их судьба — решение пользователя, эпик их не трогает.
- **G-08.** Ошибка данных боя не блокирует игру: мир грузится, бой недоступен с причиной (US-0001 AC-03). Данные читаются при создании Engine вместе со всем контентом, а не на каждый бой (отличие от Spec/Display §1).

## Grounding (baseline main @ fb39a0cf)

| Факт | Доказательство |
|---|---|
| Бой реального времени: точка подрыва, профили, ПРО, уклонение; encounter Idle/Active/Won/Lost | `DB/Documentation/04-Engineering/PointAimCombat.md`; `DB/Documentation/01-Requirements/EngineRequirements.md:108`; `src/DeepSpaceSaga.Contracts/CombatEncounterSnapshot.cs` |
| Отдельного экрана боя нет: бой внутри `GameSessionScreen` | `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.{Combat,Aiming,Encounter,ThreatPanel,...}.cs` |
| F5 — быстрое сохранение, F9 — быстрая загрузка, F10 — снимок, F12 — debug-панель | `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs:688,786-790,1482-1518`; `src/DeepSpaceSaga.Client/Data/UI/combat-input.json:3` |
| Отслеживаемые клавиши — фиксированный список (F8 отсутствует) | `src/DeepSpaceSaga.Client/UI/KeyboardEdgeTracker.cs:12` |
| Валидация боевых клавиш запрещает I, F5, F9, F10 | `src/DeepSpaceSaga.Client/CombatInputSettings.cs:46` |
| Контент: `Settings.json` → `typeData` (Data/Modules каталогами по категориям, Ships, Combat/combat-rules.json) | `src/DeepSpaceSaga.Client/Settings.json:2-16`; `src/DeepSpaceSaga.Client/Data/Modules/*` |
| Сохранение формата 19, миграция 18 | `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:35`; `DB/Documentation/04-Engineering/SaveFormat.md` |
| Экраны — стек с overlay (Push/Pop) | `src/DeepSpaceSaga.Client/UI/ScreenStack.cs:6,46,68` |
| Дронов, носителей и ангаров в игре нет | поиск по `src`, `DB/Documentation` |
| Локаль — плоские ключи RU/EN | `src/DeepSpaceSaga.Client/Data/Locale/{Russian,English}.json` |
| Бой V4: спецификация, данные (рамы, модули по файлу), эталоны 90 боёв, ход боя, серия | `VData/Planning/V4.0/{README.md,CombatSystem.md,Spec,Data,Reference}` |
| Модальная пауза Speed0, `_modalDepth`; стек рисуется снизу вверх | `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs:346-353,997-1040` |
| Глобальна только F10; F5/F9 и клавиши карты разбирает `GameSessionScreen` | `SkiaWindow.cs:682-700`; `GameSessionScreen.cs:762-766` |
| Локаль грузится один раз, смена — после перезапуска | `src/DeepSpaceSaga.Client/Localization.cs:5-13` |
| Engine/LocalClient не видят InterfaceLog | `src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs:84-85` |
| `IGameSessionConnection` расширяется default-методами (≥ 10 тестовых реализаций) | `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs:12-29` |
| `XenonComboBox` — только отрисовщик | `src/DeepSpaceSaga.Client/UI/Controls/XenonComboBox.cs:6-10` |
| Сценарий `Default` ставит модули торпед и контрмер | `src/DeepSpaceSaga.Client/Scenarios/Default/scenario.json:192,209` |
| Клавиши маневрирования EP-0013 — в `combat-input.json` | `src/DeepSpaceSaga.Client/Data/UI/combat-input.json:14-18` |
| Фикстур Save18/Save19 в tests нет; категории тестов — только `Balance`, `ForecastBudget` | поиск по `tests` |
| CI в репозитории нет | нет `.github`, yml-пайплайнов |
| Справочная реализация V4 (Python) и бой/просмотр в браузере | `tools/DeepSpaceSaga.CarrierWingSim/carrierwing/{wing4,play4,replay4}.py`, `play4.html` |

## 1. Цель

Заменить бой реального времени на пошаговый тактический бой V4 «носитель и крыло на плоскости». Игрок на тактической карте нажимает клавишу боя, готовит дронов и ведёт бой: **движение → планирование стрельбы → результаты стрельбы**. Правила, числа и ИИ совпадают с `VData/Planning/V4.0` бой в бой.

Целевой результат (эталон `VData/Planning/V4.0/Reference/series.md`, сиды 1…1000):

| Тактика игрока | Победа | ≤ 1 потери | Без потерь |
|---|---|---|---|
| Схватка (по умолчанию) | 96.3% | 80.9% | 57.2% |

## 2. Что заменяется и что остаётся

| Сейчас | После EP-0015 | История |
|---|---|---|
| Бой реального времени внутри тактической карты | Модальный пошаговый экран боя по F8, мир на паузе | US-0005…US-0010 |
| Торпеды, ПРО, уклонение, ИИ Мух, encounter, журнал/отчёты боя | Удалены сверху вниз | US-0011…US-0014 |
| Модули корабля TorpedoLauncher / CountermeasureLauncher, combat-rules | Удалены; модули дронов V4 — `Data/TacticalBattle/Modules/{Weapons,Engines,Shields,Armor}` | US-0001, US-0011, US-0014 |
| Save19 (+ миграция 18) с боевым состоянием | Save20 без боевых секций и боевых модулей; миграции 18/19 → 20 | US-0011 |
| DebugTuning с боевыми оверрайдами | Только небоевые (движение) | US-0012, US-0013 |
| Клавиши маневрирования в `combat-input.json` | `maneuver-input.json` | US-0011 |
| Python-модель `src/DeepSpaceSaga.CombatSim` | Удалена; справочная реализация — `tools/DeepSpaceSaga.CarrierWingSim` | US-0014 |

Остаются: тактическая карта, ручное маневрирование (EP-0013), панели небоевых модулей, экономика, сохранения мира, F5/F9.

## 3. Архитектура

- **Content-data:** `src/DeepSpaceSaga.Client/Data/TacticalBattle/` — дерево `VData/Planning/V4.0/Data`: `rules.json`, `ai.json`, `pilots.json`, `layouts.json`, `scenarios.json`, `Frames/<id>.json`, `Modules/Weapons|Engines|Shields|Armor/<id>.json`. Ключ `Settings.json` `typeData.tacticalBattle`. Синхронизация с V4.0 — `v4_update.py --export-game` и тест.
- **Motion:** `TacticalBattle/{PyMath, TacticalGeometry, TacticalLimits, TacticalHitChance}` (и `DeterministicMath`, если нужен) — общая детерминированная математика (Combat §1.1, R-01) для Engine и прогноза Client.
- **Engine:** `TacticalBattle/` — `Mulberry32`, загрузчик и проверки, сборка боя, движение, залп, исход, ИИ, сессия фаз; `SimulationEngine.TacticalBattle.cs` — команды, отказы и блок команд мира.
- **Contracts:** снимок боя, options, запросы, коды отказа; методы боя default-методами `IGameSessionConnection`; флаг боя в `AuthoritativeSnapshot`.
- **Engine.LocalClient:** `LocalGameSessionConnection` выполняет вызовы боя последовательно с тиком Engine.
- **Client:** `GameSessionHandle` (методы боя, InterfaceLog); ввод (клавиши, модификаторы, повтор, мышь, `tactical-battle-input.json`) и элементы интерфейса (`Slider`, `Toggle`, `TabStrip`, `DropDown`, `ScrollTextPanel`); непрозрачные модальные экраны подготовки и боя (`PushModalAsync` — пауза мира, замена внутри модального уровня); раскладка, фазы, снаряды, вкладки — по `VData/Planning/V4.0/Spec/Display.md` §2–§6 от неизменяемого кадра; политика клавиш во время боя; локаль.

## Истории и порядок

| ID | История | Зависит от | Тикетов |
|---|---|---|---|
| [EP-0015-US-0001-content-and-loader](EP-0015-US-0001-content-and-loader/EP-0015-US-0001-content-and-loader.md) | Данные тактического боя в контенте игры | none | 4 |
| [EP-0015-US-0002-deterministic-core](EP-0015-US-0002-deterministic-core/EP-0015-US-0002-deterministic-core.md) | Детерминированное ядро и общая математика боя | none | 6 |
| [EP-0015-US-0003-battle-rules](EP-0015-US-0003-battle-rules/EP-0015-US-0003-battle-rules.md) | Правила боя: сборка, движение, залп, урон, исход | 0002-deterministic-core, 0001-content-and-loader | 4 |
| [EP-0015-US-0004-battle-ai](EP-0015-US-0004-battle-ai/EP-0015-US-0004-battle-ai.md) | ИИ сторон и сверка с эталонными боями | 0003-battle-rules | 4 |
| [EP-0015-US-0005-session-contracts](EP-0015-US-0005-session-contracts/EP-0015-US-0005-session-contracts.md) | Контракты, сессия, транспорт боя | 0004-battle-ai | 6 |
| [EP-0015-US-0006-client-input-and-controls](EP-0015-US-0006-client-input-and-controls/EP-0015-US-0006-client-input-and-controls.md) | Ввод и элементы интерфейса для экранов боя | 0005-session-contracts | 6 |
| [EP-0015-US-0007-launch-and-setup](EP-0015-US-0007-launch-and-setup/EP-0015-US-0007-launch-and-setup.md) | Запуск из тактической карты и подготовка боя | 0006-client-input-and-controls | 4 |
| [EP-0015-US-0008-battle-map](EP-0015-US-0008-battle-map/EP-0015-US-0008-battle-map.md) | Поле боя и приказы движения | 0007-launch-and-setup | 5 |
| [EP-0015-US-0009-fire-and-results](EP-0015-US-0009-fire-and-results/EP-0015-US-0009-fire-and-results.md) | Планирование стрельбы, результаты и прогноз | 0008-battle-map | 4 |
| [EP-0015-US-0010-battle-end](EP-0015-US-0010-battle-end/EP-0015-US-0010-battle-end.md) | Конец боя, новый бой и возврат на карту | 0009-fire-and-results | 2 |
| [EP-0015-US-0011-legacy-removal-prep](EP-0015-US-0011-legacy-removal-prep/EP-0015-US-0011-legacy-removal-prep.md) | Подготовка удаления старого боя: инвентаризация, сохранения, небоевое | 0010-battle-end | 4 |
| [EP-0015-US-0012-legacy-client-removal](EP-0015-US-0012-legacy-client-removal/EP-0015-US-0012-legacy-client-removal.md) | Удаление старого боя в Client | 0011-legacy-removal-prep | 6 |
| [EP-0015-US-0013-legacy-engine-removal](EP-0015-US-0013-legacy-engine-removal/EP-0015-US-0013-legacy-engine-removal.md) | Удаление старого боя в Engine.LocalClient и Engine | 0012-legacy-client-removal | 10 |
| [EP-0015-US-0014-legacy-foundation-removal](EP-0015-US-0014-legacy-foundation-removal/EP-0015-US-0014-legacy-foundation-removal.md) | Удаление старого боя в Motion, Contracts, контенте и инструментах | 0013-legacy-engine-removal | 5 |
| [EP-0015-US-0015-localization](EP-0015-US-0015-localization/EP-0015-US-0015-localization.md) | Язык тактического боя: покрытие | 0014-legacy-foundation-removal | 1 |
| [EP-0015-US-0016-balance-and-native-acceptance](EP-0015-US-0016-balance-and-native-acceptance/EP-0015-US-0016-balance-and-native-acceptance.md) | Приёмка баланса, производительности и нативная проверка | 0015-localization | 3 |
| [EP-0015-US-0017-project-documentation](EP-0015-US-0017-project-documentation/EP-0015-US-0017-project-documentation.md) | Документация проекта | 0001-content-and-loader, 0002-deterministic-core, 0003-battle-rules, 0004-battle-ai, 0005-session-contracts, 0006-client-input-and-controls, 0007-launch-and-setup, 0008-battle-map, 0009-fire-and-results, 0010-battle-end, 0011-legacy-removal-prep, 0012-legacy-client-removal, 0013-legacy-engine-removal, 0014-legacy-foundation-removal, 0015-localization, 0016-balance-and-native-acceptance | 4 |

Порядок строго последовательный по зависимостям: US-0001 ∥ US-0002 → US-0003 → US-0004 (эталоны) → US-0005 (сессия и транспорт) → US-0006 (ввод и элементы) → US-0007 → US-0008 → US-0009 → US-0010 (новый бой готов) → US-0011 (инвентаризация, фикстуры, Save20, перенос небоевого) → US-0012 (Client) → US-0013 (LocalClient, Engine) → US-0014 (Motion, Contracts, контент, инструменты) → US-0015 (локаль) → US-0016 (баланс и нативная проверка) → **US-0017 (документация) — последняя**. Удаление старого боя идёт только после готового нового и **сверху вниз**: потребители удаляются раньше того, что они используют, поэтому каждый опубликованный коммит собирается.

## Правила исполнения

- **R-01. C#-семантика порта.** Каждое выражение Spec считается в `double` (Python `/` — всегда деление с дробью): деление между целыми полями или литералами запрещено. `clamp`/`min`/`max` — Python-семантики (`PyMath`), не `Math.Clamp` (бросает при lo > hi) и не `Math.Min`/`Math.Max` (другое поведение на ±0.0). Остаток — `%` (fmod), не `Math.IEEERemainder`. `List.Sort`/`Array.Sort` запрещены (неустойчивы) — `OrderBy`/`ThenBy`. «Минимум» — первый минимальный в порядке списка. Уникализация — упорядоченная (`dict.fromkeys`), не `HashSet`. Сравнение имён файлов и id — `StringComparer.Ordinal`. Порядок обхода — списки и порядок ключей JSON, не `Dictionary`. mulberry32 — `unchecked` uint. Показ чисел — `MidpointRounding.ToEven`, не `ToString("F0")`. Входы игрока с NaN/±∞ — отказ.
- **R-02. Эталоны.** Порт проверяется `VData/Planning/V4.0/Reference` (JSON, читаются из V4.0, без копий). Расхождение — дефект порта.
- **R-03. Размер тикета.** Новый код: один тикет — один слой и matching test project, ≤ 5 файлов кода (файлы локали R-07 не считаются). Удаление: один тикет — один компилируемый срез одного слоя вместе с его тестами; число файлов задаёт инвентаризация (US-0011 TK-0001), причина записывается в карточке. Копия данных (US-0001 TK-0001) — исключение. `files_touched` — число позиций Code context (каталог и маска — одна позиция).
- **R-04. Проверки.** Процессы тестов с process-tree cap 300000 ms; длинные серии — категория `LongRunning`, по тактике на запуск. Результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/` (на Windows `<artifacts>` = `D:/DeepSpaceSaga/artifacts`).
- **R-05. Платформы.** Эталонные тесты (US-0002, US-0004, US-0016) проходят на VDI исполнения (Windows) и на Linux — WSL2 Ubuntu на том же VDI или Linux-хост (CI в репозитории нет). JSON-эталоны пишутся только на Linux/glibc. Если Linux-среды нет — US-0002 блокируется, исполнитель сообщает пользователю.
- **R-06. Публикация.** Каждый тикет — отдельный коммит; push сразу после тикета и только в одобренный внутренний remote (D-USER-08, D-USER-09). Внешний `origin` — не пушить, остановиться и сообщить; pre-push hook не обходить. Пустые коммиты запрещены.
- **R-07. Локаль.** Каждый UI-тикет добавляет свои ключи в `Russian.json` и теми же ключами черновиком в `English.json` (D-USER-11); постоянные нелокализованные строки запрещены. US-0015 только проверяет покрытие.
- **R-08. Журнал.** Команды боя, отказы и недоступность данных пишет в InterfaceLog **Client** (`GameSessionHandle`); Engine и LocalClient возвращают типизированные причины.
- **R-09. Компилируемость.** Перед каждым коммитом: сборка решения и все тестовые проекты зелёные (кроме `LongRunning`). Несобираемый коммит не публикуется.

## Тесты, которые сломаются

Удаление старого боя (US-0011…US-0014) удаляет или меняет тесты точечного боя, контрмер, уклонения, ИИ Мух, encounter, журнала и отчётов боя, combat-input, DebugTuning-боя, Save19-продолжения, сценариев `CombatMukha*`, Performance-доказательств. На baseline на старый бой ссылаются ≈ 109 из 234 файлов `Client.Tests`, 100 из 179 `Engine.Tests`, 24 из 48 `Contracts.Tests`, 13 из 21 `Motion.Tests`; инвентаризация (US-0011 TK-0001) раскладывает их по тикетам удаления. Правило: тесты удаляемого кода удаляются в том же тикете; общие тесты (загрузка контента, сохранения, панели, ввод) переписываются на новый контракт, их проверки не ослабляются.

## Проверка и будущая приёмка

- Для этого изменения (planning-only): уникальность ID, совпадение папок и frontmatter, обязательные разделы, локальные ссылки, ацикличность зависимостей. Build/test/игра не нужны.
- При реализации: matching owning test project для каждого слоя; эталоны V4 — обязательный набор тестов (US-0004 AC-04, US-0005 AC-06); процессы тестов с process-tree cap 300000 ms; результаты в `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/<unique-run>/`. Automated, native и human/product приёмка фиксируются раздельно.

## Review log

- 2026-11-05, revision 1: черновик (14 историй, 48 тикетов) по ответам D-USER-01…05.
- 2026-11-05, revision 2: ответы D-USER-06…08 (F8, коммит V4.0 пользователем, исполнение на другом компьютере с коммитом и push каждого тикета); независимое ревью полноты (агент, сверка с V4.0 Spec/Data/Reference, справочной реализацией и кодом baseline). Внесено: транспорт `IGameSessionConnection` (C1), исправленный путь `LocalGameSessionConnection.cs` (C2), пауза через существующий модальный механизм Client (C3), правила C#-семантики R-01 (C4), порт CPython `vector_norm` для `dist` (C5), ввод и мышь (C7), глобальные клавиши во время боя (C8), конкретные списки и нарезка удаления по слоям (C9), вычистка боевых модулей из старых сохранений (C10), лимиты и шанс в Motion раньше (I1), модульная проверка исхода (I2), полная статистика в эталонах JSON (I3), эталоны читаются из V4.0 (I4), полнота снимка (I5), доступность боя при ошибке данных (I6), элементы интерфейса (I7), детали страницы игрока (I8), последовательный порядок историй и локаль в каждом тикете (I9), измеримые пороги производительности (I10), явные шаги ИИ и залпа (I11), заготовки ExecutionStatus/AcceptanceCoverage/FinalValidation/DocumentationImpact (M1), отсутствующий `src/graphify-out` (M3), неотслеживаемые инструменты (M4), InterfaceLog, подсказка F8, смена языка, банковское округление, версия данных (M5), платформенно-нейтральный путь артефактов (M6). Итог: 15 историй, 64 тикета.
- 2026-11-05, revision 3: второе глубокое ревью (три независимых агента: правила/ИИ/детерминизм против `wing4.py`; интерфейс против `play4.html` и Display.md; интеграция и удаление против кода baseline) и ответы D-USER-09…13. Внесено: push только во внутренний remote, внешний `origin` — стоп (D-USER-09, G-04, R-06); удаление старого боя сверху вниз компилируемыми срезами в четырёх историях (US-0011…US-0014, R-09) с фикстурами Save18/Save19 до удаления, Save20 до удаления persistence, переносом клавиш маневрирования, чисткой сценариев после удаления боевого кода Engine и до удаления определений модулей, слоем Engine.LocalClient и пропущенными потребителями (Contracts-поля, `CombatRulesDefinition`, EconomyBalance, Performance.Tests, graphify `rebuild.py`, ресурсы отчётов, `TurnHold`/`SpeedHold`/`RuntimeMotion` — чистить, не удалять); эталоны только на Linux/glibc, IEEE-hex, версия CPython для `vector_norm`, броски по этапам для всех 90 боёв, все аргументы тригонометрии, схема `series.json`, структурная причина, `roleChanges`; порядок US-0002 (эталоны раньше тестов) и US-0005 (сессия раньше транспорта); mulberry32 в Engine; полный список проверок `wing4.validate`, порядок ключей JSON, `rangeBonus`, закрытый список констант; типы исхода `Timeout` и ничья; поэтапный API; команда start с проверками; приказы: нетронутый — ИИ (D-USER-10), повторное ограничение, отказы NaN/чужих id; тест «игрок = ИИ» на 90 боях в двух режимах; default-методы `IGameSessionConnection` и `GameSessionHandle`; InterfaceLog только в Client (R-08); навигация модальных экранов и непрозрачный модальный экран; политика клавиш в экранах боя; `DropDown`, `ScrollTextPanel`, `tactical-battle-input.json` с проверкой; блокировка ввода во время анимации и запроса; верх правой панели; детали карты, приказов, стрельбы, результатов, прогноза и карточек; цвета от `playerSide`; язык русский (D-USER-11), смена языка на лету снята; просмотр боёв вне эпика с заделом кадра (D-USER-12); перегенерация эталонов разрешена (D-USER-13); документация — удалять/переписывать устаревшее, полный список документов; graphify без пустого коммита; `LongRunning` вводится тикетом; native evidence и профилирование кадра. Итог: 17 историй, 78 тикетов. Утверждения историй не получено; первая для review — US-0001.

## Backlog и границы

Не входят:
- просмотр боёв ИИ в игре (D-USER-12; есть в справочной реализации `/replay`; оценка ≈ 5–6 тикетов: кадры без трат генератора, шкала залпа, управление проигрыванием, намерения ИИ, вкладка «Серия»);
- английская локаль боя: вычитка, английские имена данных, проверка на английском (D-USER-11);
- смена языка без перезапуска;
- запуск боя по встрече с врагом на карте (D-USER-02);
- последствия боя для мира и сохранение посреди боя (D-USER-03);
- изменение баланса (только через V4.0 и экспорт);
- новые сценарии боя.

## Current numerical platform measurement — 2026-11-05

US-0002: Windows and Ubuntu/glibc runs pass all 1088 independent geometry vectors bit for bit, plus all 34 published shot chances at six-decimal source precision. Keep .NET Sin/Cos/Atan2 and CPython 3.12.3 vector_norm n=2 port; no trig mismatch or new expected battle outcomes were observed/introduced. Details and coverage limits: [PlatformValidation.md](PlatformValidation.md). G-03 is measured for available D-USER-15 vectors and reopens if later golden battles expose a numerical difference; original all-battle trig arguments are absent from MD.