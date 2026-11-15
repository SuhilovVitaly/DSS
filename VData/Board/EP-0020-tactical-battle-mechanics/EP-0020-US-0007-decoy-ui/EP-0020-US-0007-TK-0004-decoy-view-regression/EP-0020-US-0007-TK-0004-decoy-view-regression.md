---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0007-decoy-ui
ticket: EP-0020-US-0007-TK-0004-decoy-view-regression
title: Проверки вида EP-0019 после ловушек
stage: done
layer: tests
test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0020-US-0007-TK-0003-salvo-decoy-reveal]
files_touched: 4
serves: [US7-AC4]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Проверки вида EP-0019 после ловушек

## Why

Масса модуля меняет весь бой seed 1, на котором стоят проверки вида EP-0019, а ловушка меняет панель плана выстрела. Ожидания и снимки обновляются контролируемо (A-22).

## Decisions

Применяются [D-15–D-23](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-22. Раскладка (координаты) EP-0019 не меняется, кроме областей UiSpec §13; меняются значения seed 1 (позиции, шансы, тексты), поэтому строки и регрессионные снимки всех сцен пересчитываются из прогона движка. Если в новом бою seed 1 на ходу 3 у Марты нет входящего выстрела, сцены ловушки строятся на первом своём дроне с входящим выстрелом; выбор записать.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Ep0019ViewTests.cs` | после US-0004 TK-0004 | Ожидания строк всех сцен — из нового прогона; маски UiSpec §13 для сцен стрельбы; пробы UiSpec §13. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Ep0019Scenes.cs` | после US-0004 TK-0004 | Сцены «ловушка включена», «нет зарядов», «полёт с ловушкой», «результаты с ловушкой». |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/battle-data-seed1.txt` | значения снимков seed 1 | Перезаписать из прогона движка с записью причины (масса модуля, ловушки ИИ). |
| `DB/Board/EP-0019-tactical-battle-ui-redesign/Documentation.md` | review log EP-0019 | Запись об обновлении ожиданий EP-0019 эпиком EP-0020 (M2) с причинами. |

## Public API after the change

No API change.

## Implementation steps

1. Проверить раскладку всех сцен EP-0019: координаты вне областей UiSpec §13 не изменились; иначе остановиться и записать блокер.
2. Перезаписать данные seed 1 и ожидания строк из прогона движка; записать, какие строки и почему изменились.
3. Сцены стрельбы: поле цели и строка ловушки по UiSpec §9; маски §13.
4. Добавить сцены ловушки и пробы §13; заменить регрессионные снимки рендером production.
5. Прогнать все проверки вида.

## Out of scope

Новые механики.

## Invariants

Ожидания меняются только по шагам выше и с записанной причиной.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `Ep0019ViewTests.Scene_layout_matches_design_spec (7)`
- `Ep0019ViewTests.Scene_texts_match_design_spec (7)`
- `Ep0019ViewTests.Scene_probes_pass (7)`
- `Ep0019ViewTests.Scene_matches_regression_snapshot (7)`
- `Ep0019ViewTests.Seed1_battle_data_matches_assets`
- `FirePanelEvasionTests`
- `FirePanelDecoyTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~Ep0019ViewTests|FullyQualifiedName~FirePanelEvasionTests|FullyQualifiedName~FirePanelDecoyTests|FullyQualifiedName~TacticalBattle" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us7-tk4" --logger "trx;LogFileName=client.trx"
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US7-AC4 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Порядок обновления и маски заданы в UiSpec §13.

## Исполнение

В процессе. Раскладка7/7PASS до обновления; старые строки/снимки/data ожидаемо изменены массой иAI. Scope: фактические partial Ep0019Acceptance/Ep0019Snapshots, связанные literal PilotCardModel/PhaseOutcome и тестовый Fixture.engineSettingsPath. Для старых masked snapshots используется реальная Engine-сцена module:null через временный JSON-content fixture под DSS_VALIDATION_ROOT (без копии source/checkout); это изолирует стиль от изменений данных. Старые payload assets остаются нетронутыми до independent data/probes PASS.

Технический gap исходной пробы DesignSpec §12.4: бинарное «броня≤10%→Track» неверно при частичном заполнении внутри фиксированной области. Новый Outcome Юна даёт смешанные пиксели и0.231Track. Проба сравнивает ожидаемый цвет каждого пикселя с геометрией полосы §5 (fill,3px round stroke), сохраняя область61…70, ΔRGB40 и80%; аналогично сознанию. Это усиление проверки реального заполнения вместо бинарной эвристики; DesignSpec уточняется. Отсутствующую при новом no-damage исходе выноску не подменяем фиктивным уроном.

FullClient2201/2206 выявил5 дополнительных owning literal тестов: ContentData count29→30(новый decoyJSON), PhaseFire distance108.1→112.2, PhaseMove77.5→77.3,PhaseSalvo3/1→2/0 иMapFireArmorcallout. Scope расширен на эти файлы; old armor callout−41 дополнительно сохранён в реальном module:null сценарии, а текущая production сцена проверяет DecoyMiss. Проверки ввода/replay/revision/допуски сохраняются.

Завершено: до замены snapshots masked7/7PASS на module:null Engine, независимые данные/геометрия/probes47/47PASS. Полная Client2206/2206PASS со строгими7новыми snapshots и captureseed1; все29Ep0019PASS. Все значения изменены по сохранённому actual прогону: движение/позиции +10кг, enemyAIdecoys и дальнейшееRNG-ветвление; итог16ходов/23выстрела/13попаданий/0:6. Frame3безincoming, Frame4проверяетстрелкутекста. Новый per-pixel oracle полос сохраняет координаты,stroke3,Δ40,80%; snapshot thresholds0.005/tile0.25 не менялись. Реальные4decoy-сцены добавлены; временныеJSON contentfixtures автоматически удалены (проверено0оставшихся). Build/scoped-format/diff-checkPASS. Самопроверка US7-AC4 и всей истории выполнена; world save/input/navigation/replay проверены полнойsuite. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0020/20261108-execution/us7-tk4/client-final/tests.trx,masked/tests.trx,independent/tests.trx,format-tests.log,format-additional.log. Native будет выполнен по итоговой реализации; human/physical-input gates открыты.
