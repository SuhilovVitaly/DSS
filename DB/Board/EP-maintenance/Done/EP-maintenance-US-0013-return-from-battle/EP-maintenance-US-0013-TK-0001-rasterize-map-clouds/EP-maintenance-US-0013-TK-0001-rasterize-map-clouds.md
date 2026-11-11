---
epic: EP-maintenance
story: EP-maintenance-US-0013-return-from-battle
ticket: EP-maintenance-US-0013-TK-0001-rasterize-map-clouds
title: Завершать фоновый расчёт боя без остановки отладчика
stage: done
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: []
files_touched: 4
---

# Завершать фоновый расчёт боя без остановки отладчика

[История](../EP-maintenance-US-0013-return-from-battle.md) · [Эпик](../../../Documentation.md) · [Контекст EP-0022](../../../../EP-0022-tactical-battle-xenon-star/ExecutionStatus.md)

Первичная интерпретация стека связывала зависание с вызовом Skia `DrawCircle` в `EnvironmentFieldRenderer.DrawCloudField`. Повторный инцидент опроверг эту атрибуцию: отладчик приостанавливает все потоки на штатном исключении отмены фонового расчёта, поэтому главный поток может быть остановлен в любом месте рендера.

## Code context

Разрешённые файлы: `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/AnomalyFieldLayer.cs`, `InteractiveAnomalyField.cs` в той же папке, `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/AnomalyFieldLayerTests.cs`, `src/DeepSpaceSaga.Client/UI/Screens/GameSession/EnvironmentFieldRenderer.cs` (отмена прежнего неподтверждённого обхода). Сохранить изображение, геометрию, палитру и данные мира. Штатная отмена фоновой растеризации должна завершаться без выбрасывания исключений, в том числе first-chance, без публикации частичной картинки и без ожидания фоновой задачи в UI. Реальные ошибки не подавлять. Повторный Dispose безопасен.

## Повторное открытие 2026-11-10

Пользователь сообщил повторное зависание после выхода. PID 25092 остановлен по его просьбе после Mini dump. Новый дамп показывает `Debug Suspend Pending`, `Sync Suspended`, Visual Studio Debugger.Runtime и `System.OperationCanceledException` на фоне `AnomalyFieldLayer.Build -> Parallel.For`. Главный поток остановлен в `DrawObjectTrails`. Это отменяет прежнюю атрибуцию причины конкретной операции Skia. Дамп: `D:/DeepSpaceSaga/artifacts/validation/tactical-exit-recurrence/20261110-run1/`. Проверить отмену до/во время расчёта и деактивацию активного renderer с подпиской FirstChanceException. Нативная проверка: повторные выходы с активной refinement-задачей, последующая работа карты с zoom/pan, несколько тысяч кадров и отсутствие cancellation exceptions. Обычный Debug клиент должен получить исправленные DLL/PDB. Отдельный запуск под Visual Studio не подменять standalone-проверкой.

## Приёмка и проверки

Запустить `EnvironmentFieldRenderingTests`, `MapBackgroundPaletteTests` и `BattleNavigationTests` в Debug с лимитом 300000 мс. Проверить diff и формат. В нативном окне на меньшем мониторе вернуть управление и отрисовку карты после выхода из фазы движения.

## Первая попытка — отозвана после повторного инцидента

Две неизменяемые растровые текстуры вычисляются один раз на CPU. GPU рисует текстуры с прежним Screen blending и отсечением. Обход убирает из горячего пути нативный вызов рисования круга с радиальным шейдером.

- 22/22 целевых тестов, сборка Debug, scoped форматирование и diff check прошли.
- В OpenGL 1920×1080 на меньшем мониторе прошли 6/6 выходов из раннего боя с Xenon Star. Масштаб карты `9.13439E-06` взят из журнала инцидента. В окне присутствовали три поля облаков, после каждого выхода карта представила четыре кадра и восстановила скорость 5×.
- Доказательства хранятся в `D:/DeepSpaceSaga/artifacts/validation/tactical-exit-fix/20261110174859/native-incident/stdout.log` и `D:/DeepSpaceSaga/artifacts/validation/tactical-exit-live/20261110174424/stacks.log`. Ручная проверка на исходной конфигурации дисплея остаётся открытой. Конкретный объект ожидания в нативном стеке неизвестен.

## Исправление после повторного инцидента

`AnomalyFieldLayer.Build` проверяет отмену до работы, на границах строк, каждые 64 пикселя и перед созданием изображения. Отмена возвращает null, а `Parallel.For` останавливается через Stop без cancellation token. Неполный растр не публикуется. `InteractiveAnomalyField` не переводит Task в Canceled, освобождает поздний результат без ожидания UI, повторный Dispose безопасен. Ошибки вычисления по-прежнему передаются через faulted Task. Неподтверждённый обход в EnvironmentFieldRenderer полностью отменён; исходная картинка сохранена.

- Воспроизводящий тест до исправления упал с сообщением `Normal screen exit must complete refinement without cancellation exceptions`.
- После исправления 30/30 целевых тестов: AnomalyFieldLayerTests, BattleNavigationTests, EnvironmentFieldRenderingTests, MapBackgroundPaletteTests. Проверены отмена до старта/во время расчёта, три деактивации с pending refinement, отсутствие first-chance исключения и повторный Dispose. Пиксельные проверки прежнего изображения прошли.
- OpenGL Xenon Star 1920×1024 (monitor 1): 8/8 выходов с активным расчётом, 5205 кадров карты после выходов. Classic 2560×1372 (monitor 2): 8/8, 5246 кадров. Каждый цикл включает seed 1 или 51682, начальную фазу или исполненный ход, подтверждение выхода через SkiaWindow.OnMouseDown, восстановление 5×, восемь секунд zoom/pan карты. Масштабы включают исходный `9.13439E-06`. Оба монитора меньше 3440×1440; placement guard включён до показа окна.
- Во всех 16 циклах first-chance cancellation = 0. После завершения проверок при закрытии всего тестового приложения в каждом процессе зарегистрированы 11 отдельных отмен Engine/LocalClient; они происходят после восьмого цикла и не относятся к выходу из тактического экрана. Их не скрываем и не считаем отсутствующими.
- Исходный инцидент подтверждён дампом процесса под Visual Studio. Повторные нативные прогоны standalone; повторного ручного запуска под Visual Studio не было. Защита проверяет отсутствие самого исключения, на котором останавливался отладчик.
- Текущие доказательства: `D:/DeepSpaceSaga/artifacts/validation/tactical-exit-recurrence/20261110-run1/` (`stacks.log`, `rawstack.log`, `modules.log`, `baseline.out.log`, `fixed.out.log`, `native-xenon/stdout.log`, `native-classic/stdout.log`).
- Полная Client suite с корректным `DSS_VALIDATION_ROOT`: 2296/2304, восемь отказов вне изменённого пути. Те же имена тестов уже присутствуют в TRX EP-0022 от 19:06:18; у двух ApproachCameraIntegrationTests прежнее падение было раньше в setup, поэтому идентичность причины не утверждается. Текущие отказы: TerritoryRenderingTests.OverlapPreservesSelectionAndInformationalText; три параметризации ClusterInteractionEvidenceTests.EveryStationSelectableAtMaximumNetwork; FullMapRenderEvidenceTests.FullMapInteractionMatrix; MapLayerControlsTests.BaseAndFieldInfoStayReadable; ApproachCameraIntegrationTests.Approach_can_be_retargeted_after_pan_with_player_offscreen и Long_approach_survives_delayed_speed100_snapshot_without_event_overflow. Полная suite не объявляется зелёной. TRX: `D:/DeepSpaceSaga/artifacts/validation/tactical-exit-recurrence/20261110-run1/client-full-configured-tests/sushi_VITA-CASTLE_2026-11-10_20_09_37_net8.0.trx`.
- Scoped formatter Client и Client.Tests, а также `git diff --check` прошли. Обычные Debug DLL/PDB обновлены, локальный Xenon Star сохранён. Итог и команды: `D:/DeepSpaceSaga/artifacts/validation/tactical-exit-recurrence/20261110-run1/Report.md`.

В полном прогоне все 440 тактических тестов прошли (0 отказов).
