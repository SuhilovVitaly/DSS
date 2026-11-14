---
epic: EP-0027-tutorial-start
story: EP-0027-US-0001-tutorial-start
title: Обучающий старт через тактический бой
stage: implemented
dependencies: []
---

# Обучающий старт через тактический бой

## Связь с техническим заданием

Покрывает D-01–D-05 [эпика](../Documentation.md#исходное-техническое-задание).

## User story

Как новый игрок без сохранений, я нажимаю «Новая игра» и сразу попадаю в обучающий Tabletop-бой. До его завершения я могу выйти только в главное меню и не могу сохраниться. После боя кнопка «На карту» открывает солнечную систему, где игра продолжается как обычно.

## Критерии приёмки

1. Сценарий Tutorial (`scenarioId: "tutorial"`, `hidden: true`) — копия Default; он запускается в процедурной солнечной системе и не попадает в список выбора сценариев.
2. «Новая игра» без сохранений открывает бой обучения без экрана подготовки; при наличии сохранений открывается выбор сценария.
3. В бою обучения Esc открывает подтверждение «Выйти в главное меню»; подтверждение закрывает сессию и открывает главное меню без зависания.
4. В бою обучения сохранение невозможно; обычный бой по F8 по-прежнему блокирует выход в меню и возвращает на карту.
5. После боя обучения нет кнопки нового боя; основная кнопка и Esc ведут на карту, после чего F8 и сохранение работают.
6. Ошибка запуска закрывает сессию и возвращает в главное меню с записью в InterfaceLog.

## Completion evidence

Все тесты обучения прошли в полном прогоне решения 2026-11-14 (`D:/DeepSpaceSaga/artifacts/validation/ep0027-tutorial-start/final/`):

| Критерий | Проверки |
|---|---|
| 1 | `ScenarioMetadataHiddenTests`, `TutorialScenarioTests` (скрыт, совпадает с Default, стартует в сгенерированной системе), `ScenarioModuleCleanupTests`, `AllScenarioSystemContentTests` |
| 2 | `TutorialFlowTests.NewGameWithoutSavesStartsTutorialBattleAndNeverShowsTheMap`, `NewGameWithSavesOpensScenarioSelectAsBefore` |
| 3 | `TutorialEscapeConfirmationLeadsToMainMenu`, `TutorialMainMenuEventEndsTheBattleSessionCleanly`, `ConfirmExitToMenuIsLocalized` |
| 4 | `TutorialBattleBlocksSavingAndEngineRefusesToSave`, `RegularBattleStillBlocksMainMenu` |
| 5 | `FinishedTutorialOffersOnlyTheMapThenTheMapIsNormal` |
| 6 | `TutorialStartFailureReturnsToMainMenuAndDisposesSession` |

Единственное новое падение (`ScenarioGroupTranslationTests.AllShippedScenariosCovered`, папка `Tutorial`) закрыто. Остальные 633 падения полного прогона существовали до этих изменений — см. [тикет](EP-0027-US-0001-TK-0001-tutorial-flow/EP-0027-US-0001-TK-0001-tutorial-flow.md#результат). Нативная проверка окна не выполнялась; приёмку выполняет пользователь.

## Ограничения

Механика боя и симуляция Engine не меняются. Существующие сценарии и сохранения сохраняют формат: флаг `hidden` при null не пишется.

## Тикеты

- [EP-0027-US-0001-TK-0001-tutorial-flow](EP-0027-US-0001-TK-0001-tutorial-flow/EP-0027-US-0001-TK-0001-tutorial-flow.md)
