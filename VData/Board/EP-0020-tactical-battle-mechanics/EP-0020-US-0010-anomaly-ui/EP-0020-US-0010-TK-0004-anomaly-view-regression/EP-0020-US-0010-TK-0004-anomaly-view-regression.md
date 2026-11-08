---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0010-anomaly-ui
ticket: EP-0020-US-0010-TK-0004-anomaly-view-regression
title: Проверки вида EP-0019 после аномалий
stage: draft
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

Не начато.
