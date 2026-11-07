---
epic: EP-0016-tactical-battle-skins
story: EP-0016-US-0001-skin-pack-folder
ticket: EP-0016-US-0001-TK-0001-skin-manifest
title: Контракт пакета оформления
stage: approved
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: []
files_touched: 2
serves: [US1-AC1]
created: 2026-11-06T08:02:55Z
revision: 2
---

# Контракт пакета оформления

## Why

Агенту нужен стабильный декларативный контракт для изменения дизайна без сборки клиента.

## Decisions

Применяются [D-01–D-04](../../Documentation.md#подтверждённые-требования) и [утверждение истории](../EP-0016-US-0001-skin-pack-folder.md#решения-и-согласование). Разрешение касается планирования. Статус тикета присвоен по workflow epic-writer.

## Assumptions

A-04/A-05 из эпика. Схема v1 использует именованные визуальные роли и частичные переопределения штатного скина.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleSkin.cs` | Новый файл. Цвета и размеры задаются в TacticalBattleMapRenderer.Draw, DrawSalvo и DrawLabels, фон панели — в TacticalBattleScreen.DrawPanel. | Модель, строгий разбор манифеста и типизированный доступ к значениям. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SkinManifestTests.cs` | Новый файл. Доступ к internal уже настроен в DeepSpaceSaga.Client.csproj:92. | Именованные проверки схемы, диапазонов и наследования. |

## Public API after the change

В namespace `DeepSpaceSaga.Client.UI.Screens.TacticalBattle` ввести internal `TacticalBattleSkin` со свойствами `Id`, `DisplayName`, статическим `Classic` и внутренним разбором `Parse(string json)`. Разбор возвращает модель либо `FormatException` с именем поля. Роли адресуются парой `(role, property)`. Типизированное чтение цвета, числа, dash, формы возвращает переданный штатный fallback при отсутствии переопределения. Ссылки на ресурсы доступны каталогу как проверенные относительные имена. Полное описание формата v1 находится в US-0001.

## Implementation steps

1. Объявить штатный ID classic и неизменяемую модель. Разделить ссылку на ресурс и фактическую загрузку.
2. Реализовать проверку версий, форматов идентификаторов, типов, диапазонов и повторных ключей JSON, включая вложенные словари. Проверять относительные пути лексически по одному правилу для Windows и Linux. Ошибка отклоняет весь пакет.
3. Оставить штатные визуальные значения доступными при частичном переопределении. Добавить тесты, которые проверяют точный цвет с альфа-каналом, форму, размеры, шаблон линии, ресурс и отсутствие эффекта от пропуска поля.

## Out of scope

Сканирование файлов, декодирование ресурсов, отрисовка и настройки относятся к следующим тикетам.

## Invariants

Параметры скина адресуют только оформление. Контракт игрового снимка и команд принадлежит TacticalBattleSnapshot.cs:35, :58 и TacticalBattleRequests.cs.

## Tests

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `SkinManifestTests.V1_reads_all_property_types_and_keeps_classic_fallbacks`
- `SkinManifestTests.Invalid_manifest_is_rejected_as_a_unit`

Будущие команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms и сохранять его вывод рядом с TRX.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~SkinManifestTests.V1_reads_all_property_types_and_keeps_classic_fallbacks|FullyQualifiedName~SkinManifestTests.Invalid_manifest_is_rejected_as_a_unit" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0016/<run>/us1-tk1" --logger "trx;LogFileName=focused.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleSkin.cs --verify-no-changes
dotnet format whitespace tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --include tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SkinManifestTests.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги внутри allowlist.
- Для US1-AC1 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены с фактическими результатами. Исходное падение при наличии записано отдельно.
- Соблюдены инварианты и границы изменений. Реальные команды и результат проверок записаны в evidence исполнения.

## Self-containment check

Схема, значения по умолчанию и все ошибки определены в US-0001. Изменения локализованы в модели и её тестах.
