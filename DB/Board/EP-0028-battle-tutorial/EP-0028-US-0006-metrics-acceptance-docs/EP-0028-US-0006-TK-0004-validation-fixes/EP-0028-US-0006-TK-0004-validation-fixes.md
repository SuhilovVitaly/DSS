---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0006-metrics-acceptance-docs
ticket: EP-0028-US-0006-TK-0004-validation-fixes
title: Исправления по итогам валидации (Phase 4)
stage: implemented
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
- `TutorialSession`/`TutorialJournal` — тесты кэша (`HeardStepsIsCachedUntilHeardChanges`, `JournalRowsAreCachedUntilLanguageOrContentChanges`; портрет — `TutorialCommsPanelTests.ExpandedPanelShowsOfficerLineAndButtonsInCurrentLanguage` и `TutorialScreenIntegrationTests.ScreenActivationRecomposesThePortraitOutsideRenderFrames`).

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

## Результат

- **Записи Board** (071b0d2c): инцидент `git reset --hard` записан в «Журнал» US-0006-TK-0001; три расширения объёма — в «Статус исполнения» `Documentation.md` эпика, все «ожидает принятия пользователем».
- **Переключатель у значка**: `TutorialCommsPanel.DrawChip` при включённых подсказках рисует справа от значка кнопку «Подсказки: выкл» (`tutorial:hints`, `Tutorial.Panel.HintsOff` — существующий ключ RU/EN, паритет сохранён), `Bounds` — объединение значка и кнопки; `Hit` → `hints-off` → `SwitchHints(false)` → `PersistHints`. Свёрнутая реплика при этом закрывается как `dismissed`.
- **Правая/средняя кнопка**: `TutorialSession.OnMouseDown` возвращает `false` для не-левой кнопки (панель, значок, журнал); `TacticalBattleScreen.BlocksPan` исключает области обучения из проверки перед `BeginPan` — правый drag с панели панорамирует камеру (наблюдение получает `Interacting`).
- **`fire.target`**: `TutorialDirector.TargetChosen` — другой не-null `TargetId` либо «Не стрелять» без уклонения; дроны, замеченные в уклонении в текущей паре (фаза, ход), засчитываются только при выборе цели.
- **Тексты** (bef0f19c): `Tutorial.Line.CtxAnomaly` RU «Облака на поле — не декорация: из пыли дрон не стреляет, а жар может поглотить залп.», EN "Those clouds aren't scenery: a drone in dust can't fire, and heat zones can swallow shots."; `Tutorial.Line.FireSubmit` RU «Шанс попадания — на карточке выбранного пилота. Расклад устраивает? Огонь — Enter.», EN "Hit chance is on the selected pilot's card. Like the odds? Press Enter to open fire." Обе — не более 2 строк панели (`EveryLineFitsTwoPanelLinesInBothLanguages`).
- **Производительность**: `HeardSteps` кэшируется до роста `Heard`; `TutorialJournal.Entries` — кэш перенесённых строк, пересборка (с одной кистью) только при новой реплике или смене текста/языка; `Draw` не собирает портрет — `TutorialSession.Prepare()` вызывается из `TacticalBattleScreen.OnActivated`. Запись метрик не менялась.
- **Документация**: `Screens/Settings.md` (Y строк 216/278/340/402, выход 455, зазор 4 px), `Screens/TacticalBattle.md` и `04-Engineering/TacticalBattle.md` (кнопка у значка, проброс правой кнопки, правило `target-chosen`, кэши и портрет).

Проверки 2026-11-14 (`D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0006-tk0004/`):

- Красный: `red/compile-red.log` (нет `Prepare`/`Entries`), затем `red/red.log` — 18 падений новых/изменённых тестов по причине поведения; `red/green2.log` — после кода остались только 2 теста текстов; `red/content.log` — 30/30 после правки текстов.
- Целевой набор (фильтр тикета) — **263/263** (`targeted.log`, `targeted/targeted.trx`).
- Полный `DeepSpaceSaga.Client.Tests` (один раз): 2658 / прошло 2650 / упало 8 (`client-full.log`, `client-full/client-full.trx`); все 8 — базовые из `us0006-tk0001-base/failed-base.txt` (карта/камера), `client-new-failures.txt` — **0 новых**.
- `ForbiddenCallsTests`: `forbidden-scan.ps1` (US-0001-TK-0004) по рабочему дереву — 183 находки, как на базе (`validate/forbidden-base-sorted.txt`); `forbidden/forbidden-diff.txt` пуст — правка `TacticalBattleScreen.cs` находок не добавила.
- Нативный запуск игры не выполнялся (по условию).

## Журнал

2026-11-14T18:46:13Z — тикет создан по итогам валидации эпика (Phase 4), исполнение без остановок по указанию пользователя.

2026-11-14 — реализовано (TDD), `stage: implemented`. Коммиты: Board и записи журнала — 071b0d2c; код — 0f04fc3b; тексты — bef0f19c; документация — коммит документации (этот).

Узкие решения исполнителя: (1) кнопка у значка использует существующий ключ `Tutorial.Panel.HintsOff` — новый ключ не нужен, паритет RU/EN уже проверен; (2) «Не стрелять» после отменённого уклонения не отличим по плану огня от отмены уклонения и шаг не закрывает (консервативно); (3) кисть журнала создаётся только при пересборке строк и сразу освобождается — кадр её не выделяет; (4) проброс правой кнопки сделан в `TacticalBattleScreen.cs` (файл в allowlist), без правки `TacticalBattleScreen.Overlay.cs`; (5) существующие проверки «правая кнопка захватывается» в `TutorialCommsPanelTests`, `TutorialHintsToggleTests`, `TutorialJournalTests`, `TutorialScreenIntegrationTests` и ожидание «значок = вся область захвата» заменены на новое требуемое поведение.
