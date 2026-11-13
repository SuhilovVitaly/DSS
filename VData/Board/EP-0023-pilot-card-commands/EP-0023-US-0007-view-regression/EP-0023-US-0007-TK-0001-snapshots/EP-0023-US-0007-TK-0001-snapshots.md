---
epic: EP-0023-pilot-card-commands
story: EP-0023-US-0007-view-regression
ticket: EP-0023-US-0007-TK-0001-snapshots
title: Пересоздание снимков и пробы приёмки
stage: draft
layer: client-tests
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0023-US-0006-TK-0002-skin-scenes]
files_touched: 3
serves: [US7-AC1, US7-AC2]
created: 2026-11-11T14:30:00Z
revision: 1
---

# Пересоздание снимков и пробы приёмки

## Why

После смены колонки и панели все снимки боя устаревают.

## Decisions

Применяются [D-01–D-04](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md); всё, что там не названо, остаётся как в EP-0019, EP-0020 и EP-0022. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Принятие снимков — только после просмотра каждого PNG и проверки неизменности карты.

## Code context

Строки — `main` @ `8b0bf6ce`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Snapshots` | 15 PNG | Пересоздать через `DSS_EP0019_ACCEPT_SNAPSHOTS=1`. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Ep0019Acceptance.cs` | пробы EP-0019 | Пробы UiSpec §10. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Ep0019Snapshots.cs` | :10-40 сравнение | Без изменения алгоритма; при необходимости режим проверки неизменности карты по образцу `DSS_EP0020_VERIFY_M4_PANELS`. |

## Public API after the change

—

## Implementation steps

1. Прогнать сравнение до принятия и сохранить отчёт отличий по сценам.
2. Проверить неизменность области карты.
3. Принять снимки, просмотреть каждый PNG, записать список.
4. Добавить пробы UiSpec §10 и прогнать категории `Ep0019View`, `Ep0020View`.

## Out of scope

Сцены скинов (US-0006).

## Invariants

Сцены и бои не меняются.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `Ep0019ViewTests`
- `Ep0019Acceptance`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
$env:DSS_EP0019_ACCEPT_SNAPSHOTS = "1"; dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "Category=Ep0019View|Category=Ep0020View" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0023/<run>/us7-accept"; Remove-Item Env:DSS_EP0019_ACCEPT_SNAPSHOTS
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "Category=Ep0019View|Category=Ep0020View|FullyQualifiedName~Ep0019Acceptance" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0023/<run>/us7-verify" --logger "trx;LogFileName=client.trx"
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US7-AC1, US7-AC2 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Каталог снимков и режим принятия указаны; пробы — UiSpec §10.

## Исполнение

Не начато.
