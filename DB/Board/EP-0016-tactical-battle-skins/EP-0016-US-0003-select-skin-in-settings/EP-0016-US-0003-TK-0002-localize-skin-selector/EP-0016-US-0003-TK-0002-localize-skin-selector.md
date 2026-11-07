---
epic: EP-0016-tactical-battle-skins
story: EP-0016-US-0003-select-skin-in-settings
ticket: EP-0016-US-0003-TK-0002-localize-skin-selector
title: Локализация переключателя
stage: approved
layer: content-data
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0016-US-0003-TK-0001-persist-skin-selection]
files_touched: 3
serves: [US3-AC1, US3-AC3]
created: 2026-11-06T08:02:55Z
revision: 1
---

# Локализация переключателя

## Why

Игроку нужны понятные названия переключателя и штатного оформления в обоих языках игры.

## Decisions

Применяются [D-01–D-04](../../Documentation.md#подтверждённые-требования) и [утверждение истории](../EP-0016-US-0003-select-skin-in-settings.md#решения-и-согласование). Разрешение касается планирования. Статус тикета присвоен по workflow epic-writer.

## Assumptions

A-10. Названия внешних пакетов отображаются из манифеста. Штатный скин и служебные сообщения локализуются клиентом.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | English.json:332–333 содержит настройки языка и масштаба. | Добавить Settings.TacticalBattleSkin, Settings.TacticalBattleSkin.Classic и Settings.TacticalBattleSkin.SaveFailed. |
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | Russian.json:332–333 содержит те же ключи. | Добавить те же ключи и русский перевод. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/LocalizationCoverageTests.cs` | LocalizationCoverageTests.cs:19 проверяет упакованные словари. | Проверить наличие новых ключей и фактический перевод штатного имени. |

## Public API after the change

Новые ключи Settings.TacticalBattleSkin, Settings.TacticalBattleSkin.Classic, Settings.TacticalBattleSkin.SaveFailed. Манифест предоставляет DisplayName внешнего пакета как текст.

## Implementation steps

1. Добавить в оба словаря подпись «Скин пошагового боя» / «TACTICAL BATTLE SKIN», штатное имя «Штатный» / «Classic» и короткую локализованную ошибку записи выбора.
2. Проверить наличие, ненулевые значения и отсутствие нерезолвленных ключей на фактически упакованных JSON.

## Out of scope

Перевод названий пользовательских пакетов и изменение остальных строк.

## Invariants

Localization.Get читает упакованные словари (Localization.cs:12–15, :95–104).

## Tests

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `LocalizationCoverageTests.Battle_skin_settings_labels_are_resolved_in_both_packaged_locales`

Будущие команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms и сохранять его вывод рядом с TRX.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~LocalizationCoverageTests.Battle_skin_settings_labels_are_resolved_in_both_packaged_locales" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0016/<run>/us3-tk2" --logger "trx;LogFileName=focused.trx"
dotnet format whitespace tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --include tests/DeepSpaceSaga.Client.Tests/TacticalBattle/LocalizationCoverageTests.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги внутри allowlist.
- Для US3-AC1, US3-AC3 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены с фактическими результатами. Исходное падение при наличии записано отдельно.
- Соблюдены инварианты и границы изменений. Реальные команды и результат проверок записаны в evidence исполнения.

## Self-containment check

Файлы, ключи и потребитель перечислены. UI подключается в TK-0003.
