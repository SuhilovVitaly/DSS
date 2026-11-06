---
epic: EP-0016-tactical-battle-skins
story: EP-0016-US-0001-skin-pack-folder
ticket: EP-0016-US-0001-TK-0003-ship-reference-pack
title: Поставка демонстрационного скина
stage: approved
layer: content-data
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0016-US-0001-TK-0002-discover-and-load-packs]
files_touched: 4
serves: [US1-AC2]
created: 2026-11-06T08:02:55Z
revision: 2
---

# Поставка демонстрационного скина

## Why

Дать автору или ИИ-агенту пакет, который можно скопировать, изменить и проверить через обычный запуск игры.

## Decisions

Применяются [D-01–D-04](../../Documentation.md#подтверждённые-требования) и [утверждение истории](../EP-0016-US-0001-skin-pack-folder.md#решения-и-согласование). Разрешение касается планирования. Статус тикета присвоен по workflow epic-writer.

## Assumptions

A-06. Демонстрационный contrast — проверочный дизайн; выбранный пользователем арт-дирекшен можно перенести в другой пакет.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Skins/contrast/skin.json` | Новый файл. Контракт v1 определён в US-0001. | Манифест contrast, по одному выразительному переопределению на каждую семью ролей. |
| `src/DeepSpaceSaga.Client/Skins/contrast/assets/field.png` | Новый файл. US-0001 поддерживает PNG. | Небольшой собственный рисунок для проверки ресурса, пригодный для чтения на фоне сетки. |
| `src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj` | DeepSpaceSaga.Client.csproj:15–17 показывает CopyToOutputDirectory и CopyToPublishDirectory. | Включить только manifest и разрешённые assets из дерева штатных пакетов. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ShippedSkinTests.cs` | Новый файл. Тестовый проект ссылается на Client в csproj:20. | Загрузка реального поставляемого пакета из AppContext.BaseDirectory. |

## Public API after the change

Файлы Skins/contrast/skin.json и Skins/contrast/assets/field.png рядом с executable. Пакет использует только существующую схему v1.

## Implementation steps

1. Создать контрастный пример по карте US-0002. Включить явные переопределения новых деталей EP-0015: точки прогноза, кормового сектора, стрелок наведения и края карты, счётчиков, медицинского плюса, X обломка и флажков. Штатный пакет classic остаётся встроенным.
2. Объявить MSBuild items без двойного включения SDK None/Content. Копировать в build и publish с сохранением структуры. Проверить чистую папку publish отдельной командой ниже.
3. В тесте загрузить пакет по фактическому пути и проверить готовый PNG и роли. После миграции реальные визуальные отличия покрываются US-0002 и US-0003.

## Out of scope

Новый художественный стиль игры, замена базовых артов других окон, изменение пользовательских пакетов.

## Invariants

Публикация включает ровно содержимое поставляемых пакетов и не зависит от текущего каталога. Classic выбирается каталогом по правилам TK-0002.

## Tests

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `ShippedSkinTests.Contrast_manifest_and_field_image_are_present_in_real_output`

Будущие команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms и сохранять его вывод рядом с TRX.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~ShippedSkinTests.Contrast_manifest_and_field_image_are_present_in_real_output" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0016/<run>/us1-tk3" --logger "trx;LogFileName=focused.trx"
dotnet format whitespace tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --include tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ShippedSkinTests.cs --verify-no-changes
git diff --check
```

Из корня также выполнить `dotnet publish src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release -o D:/DeepSpaceSaga/artifacts/validation/ep-0016/<run>/us1-tk3-publish` и проверить наличие manifest/PNG по указанным относительным путям.

## Definition of Done

- Выполнены все шаги внутри allowlist.
- Для US1-AC2 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены с фактическими результатами. Исходное падение при наличии записано отдельно.
- Соблюдены инварианты и границы изменений. Реальные команды и результат проверок записаны в evidence исполнения.

## Self-containment check

Пути, схема, границы packaging и демонстрационные роли указаны в US-0001/US-0002.
