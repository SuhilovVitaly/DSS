---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0006-metrics-acceptance-docs
ticket: EP-0028-US-0006-TK-0004-validation-fixes
title: Исправления по итогам валидации (Phase 4)
stage: in-progress
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0028-US-0006-TK-0003-project-docs]
implementation_files: 5
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialSession.cs
  - src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialCommsPanel.cs
  - src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialDirector.cs
  - src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialJournal.cs
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs
  - src/DeepSpaceSaga.Client/Data/Locale/Russian.json
  - src/DeepSpaceSaga.Client/Data/Locale/English.json
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialDirectorTests.cs
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialCommsPanelTests.cs
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialHintsToggleTests.cs
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialJournalTests.cs
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialScreenIntegrationTests.cs
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialContentTests.cs
  - DB/Documentation/02-FirstRelease/Screens/Settings.md
  - DB/Documentation/02-FirstRelease/Screens/TacticalBattle.md
  - DB/Documentation/04-Engineering/TacticalBattle.md
  - DB/Board/EP-0028-battle-tutorial/Documentation.md
  - DB/Board/EP-0028-battle-tutorial/Tickets.md
  - DB/Board/EP-0028-battle-tutorial/EP-0028-US-0006-metrics-acceptance-docs/EP-0028-US-0006-metrics-acceptance-docs.md
  - DB/Board/EP-0028-battle-tutorial/EP-0028-US-0006-metrics-acceptance-docs/EP-0028-US-0006-TK-0001-step-metrics/EP-0028-US-0006-TK-0001-step-metrics.md
serves: [US-0002/AC-01, US-0003/AC-01, US-0004/AC-03, US-0006/AC-03]
created: 2026-11-14T18:46:13Z
revision: 1
---

# Исправления по итогам валидации (Phase 4)

[История](../EP-0028-US-0006-metrics-acceptance-docs.md) · [Эпик](../../Documentation.md) · [Тикеты](../../Tickets.md)

## Цель

Закрыть находки независимой валидации эпика (Phase 4): записи Board, доступность переключателя подсказок в бою, проброс правой кнопки мыши, ложное завершение шага `fire.target`, точность двух реплик, дешёвые оптимизации UI-потока и документация. Исполнение без остановок разрешено пользователем.

## Исправления

1. **Записи Board.** В «Журнал» US-0006-TK-0001 — инцидент со случайным `git reset --hard` (нарушение AGENTS.md:51) и его проверка; в `Documentation.md` эпика — три расширения объёма без остановки (US-0002-TK-0005, US-0002-TK-0004, US-0006-TK-0001), все — «ожидает принятия пользователем».
2. **Переключатель в бою всегда доступен.** Если подсказки включены, но реплики нет (свёрнутый значок «СВЯЗЬ · H — журнал связи»), рядом со значком — маленькая кнопка «Подсказки: выкл» (ключ `Tutorial.Panel.HintsOff`, RU/EN). Выбор сохраняется существующими `SwitchHints`/`PersistHints`.
3. **Не-левые кнопки мыши проходят сквозь панель.** `TutorialSession.OnMouseDown`: правая/средняя кнопка внутри панели, значка или журнала возвращает `false`; экран боя начинает панорамирование правой кнопкой и над областями обучения. Левая кнопка работает как раньше.
4. **Ложное завершение `fire.target`.** `TargetChosen` выполняется только при фактическом выборе цели: другой не-null `TargetId` либо явное «Не стрелять» (по дизайну строки `Tutorial.Line.FireTarget`). Уклонение E (и последующая его отмена) и ловушка D шаг не завершают.
5. **Тексты.** `Tutorial.Line.CtxAnomaly` — вероятностная формулировка про жар; `Tutorial.Line.FireSubmit` — шанс попадания показан на карточке выбранного пилота. Обе реплики — не более 2 строк панели в RU и EN.
6. **Производительность UI-потока.** `HeardSteps` кэшируется и пересчитывается при изменении `Heard`; журнал кэширует перенесённые строки (кисть создаётся только при перестроении) и перестраивает их при смене языка/текста; `Draw` больше не собирает портрет — сборка в `TutorialSession.Prepare()` при активации экрана. Запись файла метрик не меняется.
7. **Документация.** `Screens/Settings.md` — новые позиции строк (Язык 216, Масштаб UI 278, Скин 340, Подсказки 402, шаг 4 px); `Screens/TacticalBattle.md` и `04-Engineering/TacticalBattle.md` — кнопка «Подсказки: выкл» у значка и проброс правой кнопки.

## Invariants

- Только клиент: без изменений Engine/Contracts/Save; рендер не вызывает Engine; RNG не потребляется.
- Правки в `/TacticalBattle/` не добавляют находок `ForbiddenCallsTests` (`forbidden-scan.ps1` из US-0001-TK-0004).
- Подсказки по-прежнему не блокируют ввод; запись метрик не меняется.

## Verification signal

TDD по каждому поведенческому исправлению (сначала красный тест):

- `TutorialHintsToggleTests.ChipOffersHintsOffWhileNoLineIsShown` (RU/EN), свёрнутая реплика — то же;
- `TutorialCommsPanelTests.NonLeftButtonsPassThroughPanelChipAndJournal`; `TutorialScreenIntegrationTests.ClicksInsideLivePanelAreCapturedOutsideAreNot` — правый drag над панелью панорамирует;
- `TutorialDirectorTests.EvadeOrDecoyDoesNotCompleteTargetChosen`;
- `TutorialContentTests.AnomalyAndFireSubmitLinesAreAccurate` и существующий `EveryLineFitsTwoPanelLinesInBothLanguages`;
- `TutorialSession`/`TutorialJournal` — тесты кэша (`HeardStepsIsCachedUntilHeardChanges`, `JournalRowsAreCachedUntilLanguageOrContentChanges`, `DrawDoesNotRecomposeThePortraitAfterRelease`).

## Tests

```bash
RUN=D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0006-tk0004
DSS_VALIDATION_ROOT="$RUN" dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --results-directory "$RUN/targeted" --logger "trx;LogFileName=targeted.trx" --filter "FullyQualifiedName~DeepSpaceSaga.Client.Tests.Tutorial|FullyQualifiedName~TutorialFlowTests|FullyQualifiedName~SettingsScreenTests|FullyQualifiedName~KeyboardEdgeTrackerTests|FullyQualifiedName~SkinBattleIntegrationTests|FullyQualifiedName~BattleMetricsTests|FullyQualifiedName~LocalizationCoverageTests|FullyQualifiedName~MouseRoutingTests"
# один полный прогон Client.Tests в конце; падения сравнить с us0006-tk0001-base/failed-base.txt (8 базовых)
DSS_VALIDATION_ROOT="$RUN" dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --results-directory "$RUN/client-full" --logger trx
```

Нативный запуск игры не выполняется.

## Out of scope

Новые механики; изменение формата метрик; правка `TacticalBattleMetrics.cs`; Engine/Contracts.

## Definition of Done

- Все 7 исправлений выполнены; целевые тесты зелёные; полный прогон Client.Tests без новых падений относительно базы.
- Коммиты группами (Board, код, тексты, документация), после каждого — `git push`.

## Журнал

2026-11-14T18:46:13Z — тикет создан по итогам валидации эпика (Phase 4), исполнение без остановок по указанию пользователя.
