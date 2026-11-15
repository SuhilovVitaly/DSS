---
epic: EP-0023-pilot-card-commands
story: EP-0023-US-0006-skins
ticket: EP-0023-US-0006-TK-0002-skin-scenes
title: Сцены скинов с новой карточкой
stage: done
layer: client-tests
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0023-US-0006-TK-0001-skin-roles]
files_touched: 3
serves: [US6-AC2]
created: 2026-11-11T14:30:00Z
revision: 1
---

# Сцены скинов с новой карточкой

## Why

Проверка вида в каждом скине защищает от нечитаемых элементов (как в EP-0022 «восстановление контраста шкал»).

## Decisions

Применяются [D-01–D-04](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md); всё, что там не названо, остаётся как в EP-0019, EP-0020 и EP-0022. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-12.

## Code context

Строки — `main` @ `8b0bf6ce`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Ep0022SkinScenesTests.cs` | :11-20 список сцен (`1-move-no-hover`, `2-fire-hover-own`, `2-evasion-plan`, `2-decoy-empty`, `3-salvo-flight` …) | Пробы новой карточки и сжатой панели в каждом скине; проверка контраста значений над шкалами. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/RasterProbe.cs` | пробы | Расширить при необходимости. |

AnomalySceneSearch.cs получает масштаб растрового вывода, чтобы рендерить те же игровые кадры в 1280×720 и 1920×1080.

## Public API after the change

—

## Implementation steps

1. Обновить ожидания сцен скинов под новую карточку и панель.
2. Добавить проверку контраста значений шкал для Xenon Star.
3. Сохранить отчёт сцен в artifacts.

## Out of scope

Эталонные снимки classic (US-0007).

## Invariants

Сцены строятся из тех же боёв (seed 1, ход 4) и тех же паков.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `Ep0022SkinScenesTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~Ep0022SkinScenesTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0023/<run>/us6-tk2" --logger "trx;LogFileName=client.trx"
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US6-AC2 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Сцены перечислены в Code context; ожидания — UiSpec §9–§10.

## Исполнение

US6-AC2: 12 сцен проверены в Classic, Contrast и установленном Xenon Star при 1280×720 и 1920×1080. Сравнены координаты, тексты, отличия цветов, контраст значений и отрезание текстуры. Покрыт запасной вид частичного пользовательского пака. Build, 28/28 тестов, scoped format и diff check: D:/DeepSpaceSaga/artifacts/validation/ep-0023/20261111-execution-01/us6-tk2/ и D:/DeepSpaceSaga/artifacts/validation/ep-0023/20261111-execution-01/us6-tk2-format/. Самопроверка diff выполнена.
