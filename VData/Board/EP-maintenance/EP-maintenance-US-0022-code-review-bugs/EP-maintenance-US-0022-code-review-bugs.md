---
epic: EP-maintenance
story: EP-maintenance-US-0022-code-review-bugs
title: Баги по код-ревью EP-0024…EP-0028
stage: draft
implementation_status: not_started
dependencies: []
created: "2026-11-15T12:00:00Z"
source_request: "проведи код ревью последних пяти эпиков из папки боард; найденные проблемы сохрани как тикеты в юзерстори баги в эпике поддержка"
revision: 1
---

# Баги по код-ревью EP-0024…EP-0028

[Эпик «Исправления и доделки»](../Documentation.md)

## Входное техническое задание

«возьми последние пять эпиков из папки боард и проведи код ревью. найденные проблемы сохрани как тикеты в юзерстори баги в эпике поддержка»

## User story

Как игрок и разработчик, я хочу, чтобы дефекты, найденные ревью кода последних эпиков, были исправлены: подписи боя не противоречат правилам, новая игра не застревает, интерфейс Tabletop читается, а тесты ловят регрессии.

## Охват ревью

Ревью только чтением кода на main `113b8506`, без сборки и тестов. Проверены изменения кода эпиков:

| Эпик | Изменения |
|---|---|
| [EP-0024](../../EP-0024-battle-field-redesign/Documentation.md) | merge #111 и #112 (`d7895f73..7b3c7e95`) и позднейшие правки тех же файлов |
| [EP-0025](../../EP-0025-modular-character-portraits/Documentation.md) | черновик, кода нет — не проверялся |
| [EP-0026](../../EP-0026-tabletop-client-ui/Documentation.md) | merge #113 (`7b3c7e95..ac989d41`), включая вошедшие в него коммиты EP-maintenance |
| [EP-0027](../../EP-0027-tutorial-start/Documentation.md) | merge #114 (`ac989d41..d8ab8f71`) |
| [EP-0028](../../EP-0028-battle-tutorial/Documentation.md) | merge #115 (`d8ab8f71..113b8506`) |

Критических дефектов нет. Серьёзных (major): 2, остальные minor.

## Acceptance criteria

- **AC1.** [Бросок попадания показан рядом с другим шансом](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0001-hit-roll-chance/EP-maintenance-US-0022-TK-0001-hit-roll-chance.md). Подпись итога выстрела показывает тот шанс, с которым движок сравнил бросок попадания: при неудачном броске он не меньше шанса, при попадании меньше шанса.
- **AC2.** [«Новая игра» без сохранений не должна застревать при сбое обучения](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0002-tutorial-start-fallback/EP-maintenance-US-0022-TK-0002-tutorial-start-fallback.md). Если обучающий бой не стартовал, игрок без сохранений всё равно может начать игру: «Новая игра» открывает выбор сценария (или карту), причина сбоя пишется в InterfaceLog.
- **AC3.** [Ввод в главном меню во время запуска обучения](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0003-tutorial-start-input/EP-maintenance-US-0022-TK-0003-tutorial-start-input.md). Пока запускается обучение, главное меню не принимает повторные команды, а «Выход» выполняется. После открытия боя не появляется плашка «действия мира заблокированы» из-за кликов, сделанных до старта.
- **AC4.** [Синхронное чтение сохранений и сценариев при нажатии «Новая игра»](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0004-new-game-io/EP-maintenance-US-0022-TK-0004-new-game-io.md). «Новая игра» не читает содержимое сохранений и чужих сценариев в потоке окна: наличие сохранения проверяется по списку файлов, путь обучения ищется без загрузки остальных сценариев, ошибка ввода-вывода не превращает кнопку в бездействие.
- **AC5.** [Брошенное обучение оставляет отчёт боя в статусе active](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0005-tutorial-abandon-metrics/EP-maintenance-US-0022-TK-0005-tutorial-abandon-metrics.md). Выход из обучения в главное меню записывает отчёт боя со статусом `exited`, и журнал обучения отмечает отказ, так что брошенный бой отличается от сбоя.
- **AC6.** [Сценарий обучающего боя задан неявно](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0006-tutorial-scenario-id/EP-maintenance-US-0022-TK-0006-tutorial-scenario-id.md). Обучающий бой всегда стартует со сценарием `initial` при сиде `TutorialBattleSeed` независимо от порядка сценариев в каталоге.
- **AC7.** [Торговля: тонкие полосы и полоска выбора теряют цвет](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0007-trade-thin-fills/EP-maintenance-US-0022-TK-0007-trade-thin-fills.md). Полоска выбора строки видна золотом, полосы заполнения (бак, количество, груз, топливо) показывают цвет по всей высоте, малые изменения в предпросмотре видны цветом.
- **AC8.** [Карта мира: жетон станции стал мелким и почти без цвета](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0008-station-token/EP-maintenance-US-0022-TK-0008-station-token.md). Жетон станции на карте мира не меньше прочих маркеров (около 10 px) и заметно окрашен цветом станции; видимая фишка совпадает с областью наведения.
- **AC9.** [Карта: прицел выбора и прицел наведения почти одного цвета](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0009-reticle-colours/EP-maintenance-US-0022-TK-0009-reticle-colours.md). Прицел выбранного объекта и прицел объекта под курсором различимы на неподвижном кадре по цвету или форме; тест это проверяет.
- **AC10.** [Выпадающий список в боевых скинах Classic и Xenon Star](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0010-dropdown-skin-fallback/EP-maintenance-US-0022-TK-0010-dropdown-skin-fallback.md). В бою со скином без роли `control.dropdown` выпадающий список рисуется в стиле этого скина, подпись не заходит под шеврон.
- **AC11.** [Окно сохранения без акцентов окна загрузки](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0011-save-screen-accents/EP-maintenance-US-0022-TK-0011-save-screen-accents.md). В окне сохранения подтверждение удаления окрашено цветом опасности, кнопка «Сохранить» — основная, как «Загрузить» в окне загрузки.
- **AC12.** [Недоступный контрол вне боя выглядит как доступный](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0012-disabled-field/EP-maintenance-US-0022-TK-0012-disabled-field.md). Вне боевого скина недоступные DropDown, TabStrip, Toggle и ScrollTextPanel отличаются от доступных не только цветом подписи; всплывающая подсказка не получает золотую рамку фокуса.
- **AC13.** [Метрики боя копятся в профиле пользователя, тест ничего не проверяет](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0013-battle-metrics-export/EP-maintenance-US-0022-TK-0013-battle-metrics-export.md). Экспорт метрик боя не растёт без ограничений в профиле игрока, тесты Client пишут только во временный каталог, тест отчётов проверяет их содержимое.
- **AC14.** [Растровый замер: firstMs не измеряет первое открытие](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0014-raster-first-open/EP-maintenance-US-0022-TK-0014-raster-first-open.md). Отчёт `TabletopClientRaster` в поле первого открытия показывает время первого рендера после смены масштаба на холодных кэшах; тёплые прогоны помечены как тёплые.
- **AC15.** [Навигационные тесты боя не проходят по скину Tabletop](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0015-tabletop-navigation-tests/EP-maintenance-US-0022-TK-0015-tabletop-navigation-tests.md). Ключевые маршруты боя (настройка, отмена, движение, залп, повтор, итог, возврат) автоматически проверяются и для скина по умолчанию нового профиля (Tabletop), и для Classic.
- **AC16.** [Длинная реплика диалога обрезается без признака](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0016-dialogue-overflow/EP-maintenance-US-0022-TK-0016-dialogue-overflow.md). Реплика любой длины читается целиком: поле реплики прокручивается или уменьшает шрифт, а переполнение видно.
- **AC17.** [Перетаскивание прокрутки журнала боя прерывается](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0017-battle-log-drag/EP-maintenance-US-0022-TK-0017-battle-log-drag.md). Перетаскивание ползунка журнала боя продолжается при смене высоты панелей, журнал не перестраивает перенос строк при наведении на юниты.
- **AC18.** [Колесо мыши над картой у узкой колонки прокручивает колонку](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0018-slim-column-wheel/EP-maintenance-US-0022-TK-0018-slim-column-wheel.md). В узкой колонке Tabletop колесо прокручивает колонку только над карточками и панелью; над картой правее карточек оно меняет масштаб. Полоса прокрутки колонки не рисуется поверх панели.
- **AC19.** [Лист спрайтов с числом кадров не 32 проходит проверку](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0019-sheet-frame-count/EP-maintenance-US-0022-TK-0019-sheet-frame-count.md). Пакет скина с листом корабля, где число кадров не 32, либо отклоняется при загрузке с предупреждением, либо кадр по курсу считается для фактического числа направлений.
- **AC20.** [Облака Tabletop: нет проверки SKSurface.Create на null](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0020-cloud-surface-null/EP-maintenance-US-0022-TK-0020-cloud-surface-null.md). Если поверхность для пересборки облаков не создалась, кадр рисуется без пересборки (прежнее изображение или без облаков), без исключения в цикле отрисовки.
- **AC21.** [Замер облаков падает на бою без аномалий](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0021-cloud-evidence-tool/EP-maintenance-US-0022-TK-0021-cloud-evidence-tool.md). Замер `DSS_TACTICAL_CLOUD_TIMING=1` на бою без сгустков записывает «skipped», при вырожденном охвате не даёт бесконечного масштаба, при отказе поверхности не падает.
- **AC22.** [Обучение на узком экране: журнал залезает на правую колонку, реплики обрезаются](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0022-tutorial-narrow-screen/EP-maintenance-US-0022-TK-0022-tutorial-narrow-screen.md). На поддерживаемых соотношениях вплоть до 5:4 журнал связи не перекрывает правую колонку боя, а каждая реплика помещается в панель связи без многоточия на обоих языках.
- **AC23.** [Урок «под огнём» закрывается защитой чужого дрона](./EP-maintenance-US-0022-TK-0023-under-fire-lesson/EP-maintenance-US-0022-TK-0023-under-fire-lesson.md). Шаг `ctx.under-fire` засчитывается, только когда уклонение (E) или ловушку (D) получил тот дрон, на которого указывает реплика.
- **AC24.** [Журнал обучения пишется синхронно в кадре и повторяет сбойную запись](./EP-maintenance-US-0022-TK-0024-tutorial-metrics-io/EP-maintenance-US-0022-TK-0024-tutorial-metrics-io.md). Запись метрик обучения не выполняет файловый ввод-вывод в кадре отрисовки и после первой ошибки записи больше не пытается писать.
- **AC25.** [Свёрнутая панель связи ловит клики шире кнопок](./EP-maintenance-US-0022-TK-0025-comms-chip-hit/EP-maintenance-US-0022-TK-0025-comms-chip-hit.md). Свёрнутая панель связи перехватывает клики только по значку и кнопке «Подсказки»; зазор между ними и полосы над и под кнопкой пропускают клик на карту.
- **AC26.** [Тесты, которые не могут упасть](./EP-maintenance-US-0022-TK-0026-vacuous-tests/EP-maintenance-US-0022-TK-0026-vacuous-tests.md). Перечисленные тесты проверяют текущее поведение и могут упасть при регрессии; пропуск по платформе виден в отчёте.
- **AC27.** [UiSpec EP-0024 расходится с кодом после исправлений](./EP-maintenance-US-0022-TK-0027-ep24-uispec-drift/EP-maintenance-US-0022-TK-0027-ep24-uispec-drift.md). UiSpec EP-0024 описывает действующее поведение Tabletop или явно отмечает пункты, заменённые исправлениями EP-maintenance, со ссылками на них.

## Non-goals

Новые функции, переработка оформления сверх описанного в тикетах, стилистические замечания без влияния на поведение.

## Dependencies

Нет. Тикеты независимы; порядок исполнения — по серьёзности.

## Approved ticket map

| Порядок | Тикет | Серьёзность | Слой | Источник | Критерии |
|---|---|---|---|---|---|
| 1 | [EP-maintenance-US-0022-TK-0001-hit-roll-chance](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0001-hit-roll-chance/EP-maintenance-US-0022-TK-0001-hit-roll-chance.md) | major | engine | EP-maintenance-US-0016-TK-0004/TK-0005 (вошло в merge #113) | AC1 |
| 2 | [EP-maintenance-US-0022-TK-0002-tutorial-start-fallback](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0002-tutorial-start-fallback/EP-maintenance-US-0022-TK-0002-tutorial-start-fallback.md) | major | client | EP-0027-US-0001-TK-0001 (AC2, AC6) | AC2 |
| 3 | [EP-maintenance-US-0022-TK-0003-tutorial-start-input](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0003-tutorial-start-input/EP-maintenance-US-0022-TK-0003-tutorial-start-input.md) | minor | client | EP-0027-US-0001-TK-0001 | AC3 |
| 4 | [EP-maintenance-US-0022-TK-0004-new-game-io](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0004-new-game-io/EP-maintenance-US-0022-TK-0004-new-game-io.md) | minor | client | EP-0027-US-0001-TK-0001 | AC4 |
| 5 | [EP-maintenance-US-0022-TK-0005-tutorial-abandon-metrics](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0005-tutorial-abandon-metrics/EP-maintenance-US-0022-TK-0005-tutorial-abandon-metrics.md) | minor | client | EP-0027-US-0001-TK-0001 (AC3), EP-0028-US-0006-TK-0001 | AC5 |
| 6 | [EP-maintenance-US-0022-TK-0006-tutorial-scenario-id](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0006-tutorial-scenario-id/EP-maintenance-US-0022-TK-0006-tutorial-scenario-id.md) | minor | client | EP-0027-US-0001-TK-0001 | AC6 |
| 7 | [EP-maintenance-US-0022-TK-0007-trade-thin-fills](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0007-trade-thin-fills/EP-maintenance-US-0022-TK-0007-trade-thin-fills.md) | minor | client | EP-0026-US-0005-TK-0001-station-trade | AC7 |
| 8 | [EP-maintenance-US-0022-TK-0008-station-token](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0008-station-token/EP-maintenance-US-0022-TK-0008-station-token.md) | minor | client | EP-0026-US-0003-TK-0001-world-field | AC8 |
| 9 | [EP-maintenance-US-0022-TK-0009-reticle-colours](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0009-reticle-colours/EP-maintenance-US-0022-TK-0009-reticle-colours.md) | minor | client | EP-0026-US-0003-TK-0001-world-field | AC9 |
| 10 | [EP-maintenance-US-0022-TK-0010-dropdown-skin-fallback](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0010-dropdown-skin-fallback/EP-maintenance-US-0022-TK-0010-dropdown-skin-fallback.md) | minor | client | EP-0026-US-0002-TK-0001-menu-chrome | AC10 |
| 11 | [EP-maintenance-US-0022-TK-0011-save-screen-accents](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0011-save-screen-accents/EP-maintenance-US-0022-TK-0011-save-screen-accents.md) | minor | client | EP-0026-US-0002-TK-0001-menu-chrome | AC11 |
| 12 | [EP-maintenance-US-0022-TK-0012-disabled-field](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0012-disabled-field/EP-maintenance-US-0022-TK-0012-disabled-field.md) | minor | client | EP-0026-US-0001-TK-0001-steel-controls | AC12 |
| 13 | [EP-maintenance-US-0022-TK-0013-battle-metrics-export](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0013-battle-metrics-export/EP-maintenance-US-0022-TK-0013-battle-metrics-export.md) | minor | client | EP-maintenance-US-0019-TK-0003 (вошло в merge #113) | AC13 |
| 14 | [EP-maintenance-US-0022-TK-0014-raster-first-open](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0014-raster-first-open/EP-maintenance-US-0022-TK-0014-raster-first-open.md) | minor | tooling | EP-0026-US-0009-TK-0001-route-validation | AC14 |
| 15 | [EP-maintenance-US-0022-TK-0015-tabletop-navigation-tests](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0015-tabletop-navigation-tests/EP-maintenance-US-0022-TK-0015-tabletop-navigation-tests.md) | minor | client | EP-0026-US-0007-TK-0001-battle-transition | AC15 |
| 16 | [EP-maintenance-US-0022-TK-0016-dialogue-overflow](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0016-dialogue-overflow/EP-maintenance-US-0022-TK-0016-dialogue-overflow.md) | minor | client | EP-0026-US-0006-TK-0001-dialogue-panels | AC16 |
| 17 | [EP-maintenance-US-0022-TK-0017-battle-log-drag](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0017-battle-log-drag/EP-maintenance-US-0022-TK-0017-battle-log-drag.md) | minor | client | EP-maintenance-US-0018-TK-0005-battle-log (вошло в merge #113) | AC17 |
| 18 | [EP-maintenance-US-0022-TK-0018-slim-column-wheel](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0018-slim-column-wheel/EP-maintenance-US-0022-TK-0018-slim-column-wheel.md) | minor | client | EP-0024-US-0012-TK-0001-slim-layout, EP-0024-US-0013-TK-0001-panel-frame | AC18 |
| 19 | [EP-maintenance-US-0022-TK-0019-sheet-frame-count](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0019-sheet-frame-count/EP-maintenance-US-0022-TK-0019-sheet-frame-count.md) | minor | client | EP-0024-US-0001-TK-0002-tabletop-mode, EP-0024-US-0002-TK-0001-sprite-renderer | AC19 |
| 20 | [EP-maintenance-US-0022-TK-0020-cloud-surface-null](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0020-cloud-surface-null/EP-maintenance-US-0022-TK-0020-cloud-surface-null.md) | minor | client | EP-0024-US-0004-TK-0002-clouds-cache | AC20 |
| 21 | [EP-maintenance-US-0022-TK-0021-cloud-evidence-tool](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0021-cloud-evidence-tool/EP-maintenance-US-0022-TK-0021-cloud-evidence-tool.md) | minor | tooling | EP-0024-US-0004-TK-0002 | AC21 |
| 22 | [EP-maintenance-US-0022-TK-0022-tutorial-narrow-screen](../Done/EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-TK-0022-tutorial-narrow-screen/EP-maintenance-US-0022-TK-0022-tutorial-narrow-screen.md) | minor | client | EP-0028-US-0002-TK-0005, EP-0028-US-0002-TK-0002 | AC22 |
| 23 | [EP-maintenance-US-0022-TK-0023-under-fire-lesson](./EP-maintenance-US-0022-TK-0023-under-fire-lesson/EP-maintenance-US-0022-TK-0023-under-fire-lesson.md) | minor | client | EP-0028-US-0003-TK-0004, EP-0028-US-0001-TK-0002 | AC23 |
| 24 | [EP-maintenance-US-0022-TK-0024-tutorial-metrics-io](./EP-maintenance-US-0022-TK-0024-tutorial-metrics-io/EP-maintenance-US-0022-TK-0024-tutorial-metrics-io.md) | minor | client | EP-0028-US-0006-TK-0001 | AC24 |
| 25 | [EP-maintenance-US-0022-TK-0025-comms-chip-hit](./EP-maintenance-US-0022-TK-0025-comms-chip-hit/EP-maintenance-US-0022-TK-0025-comms-chip-hit.md) | minor | client | EP-0028-US-0006-TK-0004 | AC25 |
| 26 | [EP-maintenance-US-0022-TK-0026-vacuous-tests](./EP-maintenance-US-0022-TK-0026-vacuous-tests/EP-maintenance-US-0022-TK-0026-vacuous-tests.md) | minor | client | EP-0024-US-0003-TK-0001, EP-0024-US-0009-TK-0002, EP-0028-US-0004-TK-0001, EP-0028-US-0005-TK-0002 | AC26 |
| 27 | [EP-maintenance-US-0022-TK-0027-ep24-uispec-drift](./EP-maintenance-US-0022-TK-0027-ep24-uispec-drift/EP-maintenance-US-0022-TK-0027-ep24-uispec-drift.md) | minor | docs | EP-0024 UiSpec; EP-maintenance 91dc15bc, 09a7c2ae, 42c8082b, 63db8c1e | AC27 |

## Gaps and backlog

Отсеяно при ревью как недоказанное или не дефект: утечка `[ThreadStatic]` SKPaint в `TabletopStyle` (одна кисть на поток, ограничено), многоточие в кнопках Type A при переназначенной клавише (не измерено со шрифтом Verdana). Живые сценарии отказа GPU для облаков не воспроизводились.

## Review log

2026-11-15: история и 27 тикетов заведены по итогам код-ревью пяти последних эпиков. Находки проверены чтением кода; ключевые (бросок и шанс, старт обучения, предикат урока, колесо колонки, заливки торговли) перепроверены вручную. Тикеты в стадии draft.

## Статус

Не начата.
