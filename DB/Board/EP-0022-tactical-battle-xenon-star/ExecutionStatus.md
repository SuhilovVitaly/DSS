# Исполнение EP-0022

Скин Xenon Star покрывает тактическую карту, карточки пилотов и сборки, движение, стрельбу, полёт и результаты залпа, итог и подтверждение выхода. Настройки применяют пакет при следующем открытии боя. Сборка с `XenonStarSource` использует десять срезов Kodiak Graphics из локального набора. В исходной ветке находятся код, манифест и документация. [Эпик](Documentation.md) · [Карта тикетов](Tickets.md)

## Публикация

Ветка `origin/codex/ep-0022-tactical-battle-xenon-star` от `7fdaddaf4f039ff03ce9a69a94e637e11d38707d`. Для каждой строки выполнялись отдельный commit, немедленный push и сверка `git ls-remote`.

| Тикет | Подтверждённый SHA |
|---|---|
| EP-0022-US-0001-TK-0001 | `8132054394508453c7d5f58d7b8507c7ff4e8030` |
| EP-0022-US-0002-TK-0001 | `e5ce22552f2dea541e82ac79695d797cefd7c814` |
| EP-0022-US-0003-TK-0001 | `9c39f32ec8fa7ed7954ad83c283c375a505fd442` |
| EP-0022-US-0004-TK-0001 | `4d8e9f24680498b4d008d3852082e6558edb7a33` |
| EP-0022-US-0005-TK-0001 | `5ae693ad82ee2e78ec844f901953e397bdc4108a` |
| EP-0022-US-0006-TK-0001 | `11b7da8a8733a1d006a7a953a86b9bcec8a694e2` |
| EP-0022-US-0001-TK-0002 | `75b667bb4f7ce75209d85cf5df470419342f1a4a` |
| EP-0022-US-0003-TK-0002 | `8653fa89bf0a339c4c578e19bdf710f5f55a0326` |
| EP-0022-US-0007-TK-0001 | SHA завершающего коммита определяется по полному ID через `git log` и сверяется с tip origin после push. |

## Автоматические проверки

- По порядку основных функциональных тикетов Release прошёл 9/9, 25/25, 19/19, 19/19, 47/47 и 46/46 целевых тестов. Scoped `dotnet format whitespace --verify-no-changes` и `git diff --check` прошли.
- Release с локальным набором и без дополнительного флага прошёл тактическую регрессию 428/428 после исправления тестового ожидания. Фильтр включал `DeepSpaceSaga.Client.Tests.TacticalBattle` за вычетом `ContentDataTests` и `ScenarioModuleCleanupTests`, которым нужен стандартный путь дерева исходников. [TRX](../../../../artifacts/validation/ep-0022/20261110165338/review-tactical-regression/sushi_VITA-CASTLE_2026-11-10_19_14_22_net8.0.trx)
- Последняя правка палитры прошла 19/19. Проверены парные кадры, шкалы, факт установки и деградация при повреждении. [TRX](../../../../artifacts/validation/ep-0022/20261110165338/meter-tests/sushi_VITA-CASTLE_2026-11-10_19_17_54_net8.0.trx)
  Дефект цвета воспроизведён на [нативном кадре до исправления](../../../../artifacts/validation/ep-0022/20261110165338/native-xenon-1920-smoke/native.fire.png).
- Полная Client suite до двух поздних исправлений дала 2151/2301. Основная часть из 150 отказов пришлась на предположения о расположении исходников относительно `AppContext.BaseDirectory` при обязательном вынесенном каталоге сборки. Шесть отдельных проверок выбора и сведений объектов карты мира также упали. Полная suite остаётся открытой. [TRX](../../../../artifacts/validation/ep-0022/20261110165338/final-tests/sushi_VITA-CASTLE_2026-11-10_19_06_18_net8.0.trx)
- Чистая Release-сборка без набора прошла 2/2 теста поставки. В новом каталоге publish без свойства доступны Classic и Contrast. Publish со свойством содержит манифест и десять PNG. Хэши всех PNG и манифеста совпали с соответствующими исходниками. [Локальная сборка с пакетом](../../../../artifacts/validation/ep-0022/20261110165338/final-publish/client)

В командах применялись `dotnet test -c Release --artifacts-path <absolute-build> --results-directory <absolute-run>` с лимитом дерева 300000 мс. Опция `-p:XenonStarSource` использовалась для лицензионного режима. [Команда publish и таблица срезов](../../../src/DeepSpaceSaga.Client/Skins/README.md#xenon-star)

## Нативный Windows

OpenGL на Intel Arc 140V. Для изолированных профилей указан монитор 2 с режимом 2560×1440, начало −2560,0. Largest 3440×1440 исключён guard `DSS_VALIDATION_SMALL_MONITOR=1` до показа окна. `DeepSpaceSaga.Performance --tactical-window` пропускал клавиатурные события через нативный роутер и мышь через экран. Проверены выбор скина через Settings, вход в бой, pan/zoom, выбор пилота, E/D, переходы, повтор, итог, новый бой и F5/F9 после выхода. В полном сценарии обе темы дали одинаковый итог, включая 19 ходов, 31 выстрел, 18 попаданий и 309 вызовов RNG.

| Случай | Результат | Evidence |
|---|---|---|
| Classic 1920×1080, русский, масштаб 1,5 | 41 проверка прошла | [Отчёт](../../../../artifacts/validation/ep-0022/20261110165338/native-classic-1920/native.json) |
| Xenon Star 1920×1080, русский, масштаб 1,5 | 39 проверок прошли | [Отчёт](../../../../artifacts/validation/ep-0022/20261110165338/final-native-1920/native.json) · [Движение](../../../../artifacts/validation/ep-0022/20261110165338/final-native-1920/native.move.png) · [Стрельба](../../../../artifacts/validation/ep-0022/20261110165338/final-native-1920/native.fire.png) · [Залп](../../../../artifacts/validation/ep-0022/20261110165338/final-native-1920/native.salvo.png) · [Результаты](../../../../artifacts/validation/ep-0022/20261110165338/final-native-1920/native.results.png) · [Итог](../../../../artifacts/validation/ep-0022/20261110165338/final-native-1920/native.outcome.png) |
| Xenon Star 1280×720, английский, масштаб 1 | 39 проверок прошли | [Отчёт](../../../../artifacts/validation/ep-0022/20261110165338/final-native-1280/native.json) · [Результаты](../../../../artifacts/validation/ep-0022/20261110165338/final-native-1280/native.results.png) |

Замер 1920×1080 при выключенном VSync и частоте дисплея 59 Гц включал 120 кадров прогрева и 600 интервалов на случай. Для Xenon Star замер предшествует коррекции трёх цветов шкал. Harness собрал все выборки и затем исчерпал 125-секундный лимит на повторном сценарии. Завершение и Save/Load подтверждены двумя отдельными прогонами выше. [Исходный отчёт замера Xenon Star](../../../../artifacts/validation/ep-0022/20261110165338/native-xenon-1920/native.json)

| p99 между завершениями swap, мс | Classic | Xenon Star |
|---|---:|---:|
| Покой | 30,85 | 139,39 |
| Pan | 77,25 | 139,32 |
| Zoom | 130,14 | 123,13 |
| Полёт залпа | 125,60 | 122,57 |

Обе темы вышли за бюджет 12,5 мс. В большинстве случаев длинные интервалы совпали с ожиданием swap. Метрика submit в Xenon Star составила p99 8,42/10,61/65,31/10,07 мс в том же порядке. Физический вывод и пользовательский ввод отдельно не проверялись.

## Инвентаризация документации

| Область | Результат ревизии |
|---|---|
| [Скины](../../Documentation/04-Engineering/TacticalBattleSkins.md) и [asset workflow](../../../src/DeepSpaceSaga.Client/Skins/README.md) | Описаны пакет, установка, путь сборки, авторство, роли, деградация и срезы. |
| [Экран боя](../../Documentation/02-FirstRelease/Screens/TacticalBattle.md), [Settings](../../Documentation/02-FirstRelease/Screens/Settings.md), [инструменты UI](../../Documentation/06-Tooling/GameSessionScreenUI.md) | Описаны охват скина, эффективный выбор и подключение. |
| [Требования Engine](../../Documentation/01-Requirements/EngineRequirements.md), [архитектура](../../Documentation/00-Process/CLAUDE.md), [бой](../../Documentation/04-Engineering/TacticalBattle.md), [Save20](../../Documentation/04-Engineering/SaveFormat.md) | Проверены по итоговому diff. Изменения в источниках требований и схемах не требуются, так как diff ограничен отображением и сборкой клиента. |
| [README документации](../../Documentation/README.md), [режим нативной проверки](../../../tools/DeepSpaceSaga.Performance/README.md) и исторические EP-0016/0019/0020 | Существующие ссылки ведут к актуальному контракту. Поддерживаемые команды инструмента и датированное evidence остаются действительными. Корневой README отсутствует. |
| [Graphify](../../Documentation/06-Tooling/Graphify.md), [перестройка](../../Documentation/06-Tooling/GraphifyRebuild.md) и Board | EP-0022 включён в корпус. [Отчёт графа](../../../../artifacts/validation/ep-0022/20261110165338/graph/GRAPH_REPORT.md) и [визуализация](../../../../artifacts/validation/ep-0022/20261110165338/graph/graph.html) хранятся в workspace artifacts. |

## Открытая приёмка

- G-01. Подтверждение условий публичного распространения конкретной лицензии и требуемых уведомлений. Локальный publish использует переданный набор.
- G-02 и G-03. Художественная приёмка владельцем по фазам, итогу и диалогу выхода. Нативный Linux, физический ввод, отдельный визуальный прогон восьми карточек и измерение физической частоты не выполнялись.
- Полная Client suite и 80 FPS остаются открыты. Прохождение ограниченной тактической регрессии и сценариев Windows зафиксировано отдельно.
