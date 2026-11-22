---
epic: EP-0043-demo-ambush-prologue
story: EP-0043-US-0002-ambush-opening
ticket: EP-0043-US-0002-TK-0005-opening-presentation
title: Карта и кнопка запуска боя
stage: approved
layer: client
depends_on: [EP-0043-US-0002-TK-0003-opening-state, EP-0043-US-0001-TK-0003-dialogue-layout, EP-0043-US-0002-TK-0004-opening-checkpoint]
files_touched: 4
serves: [AC-01, AC-02, AC-03]
created: 2026-11-20T10:58:51Z
revision: 1
---

# Карта и кнопка запуска боя

## Why

D-01 и D-02. Новый старт демо предваряет существующее столкновение. [История](../EP-0043-US-0002-ambush-opening.md).

## Decisions

D-01 и D-02. [Продуктовые решения и исходные ответы](../../Documentation.md#решения-пользователя).

## Assumptions

A-01. [Предположения эпика](../../Documentation.md#технические-предположения).

## Code context

| Файл | Точка интеграции | Разрешённое изменение |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` | `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs:1363` | Основное изменение по шагам тикета. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs` | `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs:103` | Только необходимая интеграция или проверка данных в рамках шагов. |
| `src/DeepSpaceSaga.Client/Localization.cs` | `src/DeepSpaceSaga.Client/Localization.cs:1` | Только необходимая интеграция или проверка данных в рамках шагов. |
| `tests/DeepSpaceSaga.Client.Tests/DemoEp0043Us0002Tk0005Tests.cs` | Новый файл в DeepSpaceSaga.Client.Tests. | Только именованные тесты тикета. |

## Public API after the change

Переиспользовать текущие публичные маршруты и команды. Новые поля проекций определяют тикеты слоя Contracts.

## Implementation steps

Для демо использовать карту и её штатный вход в dialogue modal до стартового запроса. После подтверждённого завершения сцены запустить прежний TutorialBattleScenarioId/TutorialBattleSeed через StartAsync с текущей ревизией крыла. Блокировать повторы в ожидании, сохранить доступный повтор при отказе, соблюдать transition lock и идентичность сессии. Добавить RU/EN строки трёх ключей. Non-demo сохраняет direct-start. Использовать ту же обработку ReadyForAmbush после Continue и назначать из неё исходный модальный экран тактики.

## Out of scope

Новая тактика, баланс, постановка боя и изменение обычного профиля. Любые изменения вне allowlist.

## Invariants

Ровно один запуск боя на принятую команду. Точка интеграции — `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs:1363`.

## Tests

- AC-01 — `DemoMapOpensBeforeBattleRequest`.
- AC-02 — `FinalChoiceLaunchesPreparedBattleOnce`.
- AC-03 — `OrdinaryTutorialStartsAsBefore`.

Будущий запуск из корня DSS через ограничитель дерева процессов 300000 ms. Создать отдельный абсолютный каталог запуска и направить туда stdout/stderr.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj -c Release --filter 'FullyQualifiedName=DeepSpaceSaga.Client.Tests.DemoEp0043Us0002Tk0005Tests.DemoMapOpensBeforeBattleRequest|FullyQualifiedName=DeepSpaceSaga.Client.Tests.DemoEp0043Us0002Tk0005Tests.FinalChoiceLaunchesPreparedBattleOnce|FullyQualifiedName=DeepSpaceSaga.Client.Tests.DemoEp0043Us0002Tk0005Tests.OrdinaryTutorialStartsAsBefore' --results-directory 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/test-results' --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --verify-no-changes --include src/DeepSpaceSaga.Client/UI/SkiaWindow.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs src/DeepSpaceSaga.Client/Localization.cs
dotnet format whitespace tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --verify-no-changes --include tests/DeepSpaceSaga.Client.Tests/DemoEp0043Us0002Tk0005Tests.cs
```

## Definition of Done

- Шаги выполнены в пределах allowlist и критерии AC-01, AC-02, AC-03 покрыты указанными проверками.
- Именованные проверки, сборка и применимое форматирование проходят. Исходное падение записано отдельно, если применимо.
- Поведение на штатных входах, повторе операции и границах истории проверено в применимой части.

## Self-containment check

Code context задаёт разрешённые точки интеграции. Входные решения, предшествующие работы и наблюдаемый результат указаны в истории и depends_on. Статус утверждения относится к декомпозиции по workflow.
