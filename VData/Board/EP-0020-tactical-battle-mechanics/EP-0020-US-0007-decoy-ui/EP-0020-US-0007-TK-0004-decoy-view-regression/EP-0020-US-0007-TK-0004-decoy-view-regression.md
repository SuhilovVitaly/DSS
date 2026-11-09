---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0007-decoy-ui
ticket: EP-0020-US-0007-TK-0004-decoy-view-regression
title: Проверки вида EP-0019 после ловушек
stage: draft
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

Не начато.
