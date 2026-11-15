---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0004-evasion-ui
ticket: EP-0020-US-0004-TK-0004-view-regression
title: Проверки вида EP-0019 после уклонения
stage: done
layer: tests
test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0020-US-0004-TK-0003-results-evasion-reveal]
files_touched: 4
serves: [US4-AC4]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Проверки вида EP-0019 после уклонения

## Why

Главный критерий EP-0019 — экран точно по спецификации — должен остаться истинным. Уклонение меняет строку кнопок панели стрельбы и доигрывание боя seed 1, поэтому часть ожиданий и регрессионных снимков EP-0019 обновляется контролируемо (D-09, A-12).

## Decisions

Применяются [D-01–D-13](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-12. Сцены движения, полёта залпа и результатов EP-0019 не меняются. Сцены стрельбы («2-fire», «2-fire-hover-own») меняются только в областях UiSpec §7. Сцена итога и данные seed 1 меняются из-за открытия фазы на ходу 13 и уклонения ИИ.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Ep0019ViewTests.cs` | EP-0019: раскладка, строки, пробы, регрессионные снимки семи сцен | Ожидания раскладки и строк сцен стрельбы — по UiSpec §3; маски UiSpec §7 при сверке со старыми снимками; ожидания сцены итога и данных seed 1 — по новому прогону движка с записанной причиной; пробы UiSpec §7. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Ep0019Scenes.cs` | EP-0019: построение сцен | Сцены «план уклонения» (выбрана Марта, E) и «нет целей» (выбрана Вера) для проб UiSpec §7. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/battle-data-seed1.txt` | EP-0019: значения снимков seed 1 | Обновить данные хода, затронутого D-10, и итога, если движок их изменил. |
| `DB/Board/EP-0019-tactical-battle-ui-redesign/Documentation.md` | review log EP-0019 | Запись об обновлении ожиданий и регрессионных снимков EP-0019 эпиком EP-0020 с причинами. |

## Public API after the change

No API change.

## Implementation steps

1. Прогнать проверки вида EP-0019: сцены без стрельбы должны пройти без изменений; иначе остановиться и записать блокер.
2. Сцены стрельбы: регрессионные снимки сверяются с масками UiSpec §7 — вне масок правило DesignSpec §12.5 EP-0019 выполняется. Затем ожидания строк и раскладки обновляются по UiSpec §3, снимки заменяются рендером production.
3. Сцена итога: сначала сверить данные (итог, ход, потери) с прогоном движка и обновить ожидания строк DesignSpec §12.3 EP-0019 в тестах; записать, что и почему изменилось; заменить регрессионный снимок.
4. Добавить сцены уклонения и пробы UiSpec §7.
5. Прогнать все проверки вида и регрессионные снимки.

## Out of scope

Новые регрессионные снимки результатов с уклонением.

## Invariants

Ожидания и снимки меняются только для перечисленных сцен и только по шагам выше.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `Ep0019ViewTests.Scene_layout_matches_design_spec (7)`
- `Ep0019ViewTests.Scene_texts_match_design_spec (7)`
- `Ep0019ViewTests.Scene_probes_pass (7)`
- `Ep0019ViewTests.Scene_matches_regression_snapshot (7)`
- `Ep0019ViewTests.Seed1_battle_data_matches_assets`
- `FirePanelEvasionTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~Ep0019ViewTests|FullyQualifiedName~FirePanelEvasionTests|FullyQualifiedName~TacticalBattle" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us4-tk4" --logger "trx;LogFileName=client.trx"
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US4-AC4 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Порядок обновления и маски заданы в UiSpec §7.

## Исполнение

В процессе. Перед правкой scope расширен на фактические partial-файлы Ep0019Acceptance.cs/Ep0019Snapshots.cs и три разрешённых regression assets: код EP-0019 разделён по partial-файлам. Пороги не меняются. Сначала независимые строки/геометрия/пробы и masked compare старых снимков; затем замена только двух Fire и Outcome.

Полная Client suite выявила второй literal Outcome в PhaseOutcomeTests.cs; scope расширен на него по той же причине D-10/AI. Четыре ожидания заменены на независимо проверенные 28/38/24/2:6; проверки navigation/exit сохранены.

Итог: независимые проверки27/27 PASS; старые masked snapshots6/6 PASS; обновлены только два Fire и Outcome. Полная Client suite2182/2182 PASS, в том числе все29 Ep0019, FirePanel, Results, navigation, session/save границы. Build/scoped-format/diff-check PASS. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0020/20261108-execution/us4-tk4/client-final/tests.trx, independent/, masked/, format.log, format-outcome.log. Визуальная самопроверка production render выполнена; independent/human/native acceptance не заявляется. US4-AC4 выполнен. Пороги не ослаблены; переходный режим масок явно включается только env-флагом и отсутствует в финальной suite.
