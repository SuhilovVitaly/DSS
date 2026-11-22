---
epic: EP-0043-demo-ambush-prologue
story: EP-0043-US-0004-prisoner-interrogation
ticket: EP-0043-US-0004-TK-0003-interrogation-text
title: Текст допроса и подписи в RU/EN
stage: approved
layer: client
depends_on: [EP-0043-US-0004-TK-0002-interrogation-state, EP-0043-US-0001-TK-0003-dialogue-layout]
files_touched: 3
serves: [AC-01, AC-02]
created: 2026-11-20T10:58:51Z
revision: 1
---

# Текст допроса и подписи в RU/EN

## Why

D-03 и D-05. Захваченный пират раскрывает происхождение маяка и судьбу пилотов. [История](../EP-0043-US-0004-prisoner-interrogation.md).

## Decisions

D-03 и D-05. [Продуктовые решения и исходные ответы](../../Documentation.md#решения-пользователя).

## Assumptions

A-02 и A-05. [Предположения эпика](../../Documentation.md#технические-предположения).

## Code context

| Файл | Точка интеграции | Разрешённое изменение |
|---|---|---|
| `src/DeepSpaceSaga.Client/Localization.cs` | `src/DeepSpaceSaga.Client/Localization.cs:1` | Основное изменение по шагам тикета. |
| `src/DeepSpaceSaga.Client/UI/Screens/Dialogue/DialogueScreen.cs` | `src/DeepSpaceSaga.Client/UI/Screens/Dialogue/DialogueScreen.cs:123` | Только необходимая интеграция или проверка данных в рамках шагов. |
| `tests/DeepSpaceSaga.Client.Tests/DemoEp0043Us0004Tk0003Tests.cs` | Новый файл в DeepSpaceSaga.Client.Tests. | Только именованные тесты тикета. |

## Public API after the change

Переиспользовать текущие публичные маршруты и команды. Новые поля проекций определяют тикеты слоя Contracts.

## Implementation steps

Добавить русский и английский текст четырёх узлов и действия «Показать на карте» по рабочему тексту сцены, проводить текущую реплику через существующий диалоговый экран. При отсутствии bitmap портрета использовать принятый заглушку портрета для известного пленного и сохранить его имя. Проверить контекст каждого говорящего и порядок текста.

## Out of scope

Общие механики допроса и взаимодействия с пленными. Любые изменения вне allowlist.

## Invariants

Отметка аванпоста появляется только после раскрытия игроку. Точка интеграции — `src/DeepSpaceSaga.Client/Localization.cs:1`.

## Tests

- AC-01 — `InterrogationUsesBothLocalesAndRealSpeaker`.
- AC-02 — `InterrogationUsesBothLocalesAndRealSpeaker`.

Будущий запуск из корня DSS через ограничитель дерева процессов 300000 ms. Создать отдельный абсолютный каталог запуска и направить туда stdout/stderr.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj -c Release --filter 'FullyQualifiedName=DeepSpaceSaga.Client.Tests.DemoEp0043Us0004Tk0003Tests.InterrogationUsesBothLocalesAndRealSpeaker' --results-directory 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/test-results' --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --verify-no-changes --include src/DeepSpaceSaga.Client/Localization.cs src/DeepSpaceSaga.Client/UI/Screens/Dialogue/DialogueScreen.cs
dotnet format whitespace tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --verify-no-changes --include tests/DeepSpaceSaga.Client.Tests/DemoEp0043Us0004Tk0003Tests.cs
```

## Definition of Done

- Шаги выполнены в пределах allowlist и критерии AC-01, AC-02 покрыты указанными проверками.
- Именованные проверки, сборка и применимое форматирование проходят. Исходное падение записано отдельно, если применимо.
- Поведение на штатных входах, повторе операции и границах истории проверено в применимой части.

## Self-containment check

Code context задаёт разрешённые точки интеграции. Входные решения, предшествующие работы и наблюдаемый результат указаны в истории и depends_on. Статус утверждения относится к декомпозиции по workflow.
