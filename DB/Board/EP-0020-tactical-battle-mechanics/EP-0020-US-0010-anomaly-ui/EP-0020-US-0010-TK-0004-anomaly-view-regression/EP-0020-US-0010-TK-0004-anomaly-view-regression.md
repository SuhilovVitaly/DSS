---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0010-anomaly-ui
ticket: EP-0020-US-0010-TK-0004-anomaly-view-regression
title: Проверки вида EP-0019 после аномалий
stage: done
layer: tests
test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0020-US-0010-TK-0003-anomaly-salvo-results]
files_touched: 4
serves: [US10-AC4]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Проверки вида EP-0019 после аномалий

## Why

Аномалии меняют бой seed 1 и добавляют слой на карту всех сцен EP-0019.

## Decisions

Применяются [D-24–D-33](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-32: раскладка панелей EP-0019 не меняется, кроме областей UiSpec §20; данные, строки и регрессионные снимки всех сцен пересчитываются из прогона движка. Сцены аномалий строятся на аномалиях seed 1; если в seed 1 нет нужной ситуации (линия огня через температурную зону, свой дрон в пыли), сцена берёт первый seed ≥ 1, где она есть; выбор записать.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Ep0019ViewTests.cs` | после US-0007 TK-0004 | Ожидания всех сцен из нового прогона; маски и пробы §20. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Ep0019Scenes.cs` | после US-0007 TK-0004 | Сцены «поглощение», «в пыли», «наведение на аномалию», «поглощённый выстрел». |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/battle-data-seed1.txt` | данные seed 1 | Перезаписать с причиной «аномалии». |
| `DB/Board/EP-0019-tactical-battle-ui-redesign/Documentation.md` | review log EP-0019 | Запись об обновлении (M3). |

## Public API after the change

No API change.

## Implementation steps

1. Проверить раскладку панелей вне областей §20; иначе блокер.
2. Перезаписать данные и ожидания строк из прогона; записать изменения.
3. Добавить сцены и пробы §20; заменить регрессионные снимки рендером production.
4. Прогнать все проверки вида.

## Out of scope

Новые механики.

## Invariants

Ожидания меняются только по шагам выше с записанной причиной.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `Ep0019ViewTests.Scene_layout_matches_design_spec (7)`
- `Ep0019ViewTests.Scene_texts_match_design_spec (7)`
- `Ep0019ViewTests.Scene_probes_pass (7)`
- `Ep0019ViewTests.Scene_matches_regression_snapshot (7)`
- `Ep0019ViewTests.Seed1_battle_data_matches_assets`
- `AnomalyFirePanelTests`
- `AnomalyFieldLayerTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~Ep0019ViewTests|FullyQualifiedName~AnomalyFirePanelTests|FullyQualifiedName~AnomalyFieldLayerTests|FullyQualifiedName~TacticalBattle" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us10-tk4" --logger "trx;LogFileName=client.trx"
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US10-AC4 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Порядок обновления задан.

## Исполнение

В процессе. Scope включает все затронутые view partial/tests/snapshotassets, не только первоначальные4файла. Перед заменой7productionM3снимков выполняется более строгая сверка прежнего M2 вида на реальном withoutAnomalies contentfixture без масок (flag VERIFY_M3_LAYOUT). Затем literal данные/тексты/геометрия и пробы обновляются по реальному Engine; field/map/sector pixels проверяются формулой Screen+Lighten, самостоятельная32pixel oracle в TK1 сохраняется. Новые реальные SceneFactory cases проходят production handlers, без sourcecopy.

Промежуточные подтверждения: old-m2-layout7/7 без масок PASS; full-initial2200PASS/20FAIL прежних literal/view ожиданий; затем literals-probes36/36PASS и новыеproduction SceneFactory5/5PASS. Исходный capture режим отмечен как создание reference, не независимая проверка. AA border уточнение: при выбранном Юне top93×1.5=139.5 пикселя бинарный Yellow считал50% вместо60% при правильной1.8обводке. Проба теперь сравнивает ту же площадь с независимым literal PanelBox1.8/5/glow50 AA raster; Δ40 и60% сохранены. Sector/map reference включает предписанное Screen field, те же площади/50%/95%;32independent scalar pixels TK1 сохранены. Полныйstrict baseline ожидается после замены.

Фактический seed1M3: первый Fire4, выбранЮн→Таг6 73%/73.4дистанция/0.4°, own3выстрела+enemy1; Results4shots/3hits, Марта→Таг1miss55→34%, ЮнArmor80→46, Tag6Hull0. OutcomeA23turn42shots20hitsLoss0:6; cardsВера130/0,Юн322/−34,Марта51/−34,Олег209/−40. Изменение — разрешённые аномалии/AI dust/effective target/RNG absorption. У input-теста недопустимой цели клик перенесён с нового допустимого Таг6 на недопустимый Таг2; отказ и hold assertions сохранены.

Завершено. Полная Client2230/2230 uniquePASS; все29исходныхEp0019checksPASS;5новыхhandler-сценPASS;12strict regression comparisons pixel-identical (different=0,maxTile=0) при всехcapture/transitionflags=false. До обновления7старых M2снимков проверены без масок. Новые5сцен: move-dust seed1Move3, absorption seed1Fire4Олег→Таг1, dust seed6Fire5Олег, hoverHeat seed1Move1, absorbed seed1MartaFire11 (FlowMove12). Setup seed6 введён production Tab/Backspace/TextInput с доказанно неизменным loadout; выбор/targetclick/движение/залп реальные. SourceSnapshots12 иseed1data обновлены после независимых36literal/probe+5handlerPASS; capture-mode не заявлен независимой приёмкой. Scopedformat/diffcheckPASS, contentfixturesDispose удалены. Evidence `D:/DeepSpaceSaga/artifacts/validation/ep-0020/20261108-execution/us10-tk4/coverage-audit.json` и full/tests.trx. AC4 выполнен, US10завершена технически; human/art/native/independentreview не закрыты. Исправлена опечатка отчёта TK2 о числе locale проверок, фактический31PASS сохранён.
