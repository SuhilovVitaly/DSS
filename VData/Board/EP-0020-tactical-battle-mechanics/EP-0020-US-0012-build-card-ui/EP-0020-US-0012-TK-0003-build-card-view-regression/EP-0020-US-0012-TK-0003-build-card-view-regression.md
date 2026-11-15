---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0012-build-card-ui
ticket: EP-0020-US-0012-TK-0003-build-card-view-regression
title: Проверки вида EP-0019 после карточки сборки
stage: done
layer: tests
test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0020-US-0012-TK-0002-build-card-render]
files_touched: 3
serves: [US12-AC3]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Проверки вида EP-0019 после карточки сборки

## Why

Карточка появляется во всех сценах с выбранным своим дроном и меняет правую колонку и раскладку подписей карты.

## Decisions

Применяются [D-34–D-42](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-39: раскладка панелей фаз не меняется; область карточки и подписи карты, сдвинутые занятой областью, пересчитываются; регрессионные снимки заменяются рендером production.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Ep0019ViewTests.cs` | после US-0010 TK-0004 | Ожидания областей и строк карточки; маски §25. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Ep0019Scenes.cs` | после US-0010 TK-0004 | Сцены «сборка выбранного», «сборка под курсором», «повреждения» (ручная сцена). |
| `DB/Board/EP-0019-tactical-battle-ui-redesign/Documentation.md` | review log EP-0019 | Запись об обновлении (M4). |

## Public API after the change

No API change.

## Implementation steps

1. Проверить раскладку панелей вне области карточки; иначе блокер.
2. Обновить ожидания карточки и подписей карты; записать причины.
3. Добавить сцены и пробы §25; заменить регрессионные снимки.
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
- `BuildCardRenderTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~Ep0019ViewTests|FullyQualifiedName~BuildCardRenderTests|FullyQualifiedName~TacticalBattle" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us12-tk3" --logger "trx;LogFileName=client.trx"
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US12-AC3 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Порядок обновления задан.

## Исполнение

В процессе. Scope расширен на Ep0019Acceptance/Snapshots, tracked baselines, Ep0020BuildScenesTests и native evidence harness: реальная карточка меняет own hover и занятые label areas; regression требует актуальных ожиданий и native E/D. До замены baseline проверяются неизменные left pilot panels и phase/action panels; полная регрессия затем без masks/capture flags.

Full Client под нагрузкой выявил 3 исключения thread-local test locale при async continuation и старое ожидание5overlay вместо6. Scope расширен на Localization test seam и regression test: AsyncLocal сохраняет тестовую локаль в логическом async flow, production language selection не меняется; nested Task.Run/restore проверяются отдельно. Scope также включает текущие тесты EP20 (перенос locale scopes после async fixtures) и OverlayShell assertion6 плюс click inside build card. Это исправление воспроизведённой нестабильности, а не ослабление locale assertions.

Завершено. Полная Client suite2252/2252PASS (включая29исходных EP19,15strict unmasked snapshots,3новые build scenes). До baseline update12сцен доказали pixel-exact прежних пилотных/phase/action panels; граница правой проверки RB+4 лежит в чистом зазоре перед AA новой карточки RB+6. Итог15snapshot different0/maxTile0; capture/masksOFF. Изменены6из12старых baseline только в сценах с build card, добавлены3новых; прежние enemy/anomaly cards не менялись. Повреждённая сцена явно ручная DTO projection, selection/hover реальные handlers. Client Debug/Performance Release buildPASS, scoped format/diffPASS. Самопроверка/визуальный осмотр3карточек выполнены. Evidence: `D:/DeepSpaceSaga/artifacts/validation/ep-0020/20261108-execution/us12-tk3/view-audit.json` и `client-final/tests.trx`. Native и полный geography corpus — последующий общий gate перед документацией, здесь не заявлены PASS.
