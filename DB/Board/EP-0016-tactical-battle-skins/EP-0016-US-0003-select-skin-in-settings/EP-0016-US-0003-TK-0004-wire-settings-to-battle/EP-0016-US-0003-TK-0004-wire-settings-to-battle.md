---
epic: EP-0016-tactical-battle-skins
story: EP-0016-US-0003-select-skin-in-settings
ticket: EP-0016-US-0003-TK-0004-wire-settings-to-battle
title: Связь настроек с реальным экраном боя
stage: approved
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0016-US-0003-TK-0003-add-skin-selector, EP-0016-US-0002-TK-0005-theme-information-tabs]
files_touched: 2
serves: [US3-AC1, US3-AC2, US3-AC3, US3-AC4]
created: 2026-11-06T08:02:55Z
revision: 1
---

# Связь настроек с реальным экраном боя

## Why

Связать настройки с настоящим боем и проверить путь внедрения нового дизайна пакетными файлами.

## Decisions

Применяются [D-01–D-04](../../Documentation.md#подтверждённые-требования) и [утверждение истории](../EP-0016-US-0003-select-skin-in-settings.md#решения-и-согласование). Разрешение касается планирования. Статус тикета присвоен по workflow epic-writer.

## Assumptions

A-03/A-04/A-09. Пакеты обнаруживаются при запуске клиента, а выбранный ID используется при каждом создании активного окна боя. Удаление пакета проверяется после следующего запуска.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` | SkiaWindow.cs:100, :798, :1078–1081, :1236–1263, :1372, :1725–1762. Точки старта, настройки, создание боя и Dispose. | Владение каталогом, разрешение сохранённого ID, подключение обоих production entry points и освобождение ресурсов. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SkinBattleIntegrationTests.cs` | Новый файл. BattleNavigationTests.cs:193 использует реальную боевую fixture, SettingsScreenTests.cs:46 растровую поверхность. | Связать реальную папку, выбор и Render, покрыть новый выбор, рестарт, fallback и сравнение игрового результата. |

## Public API after the change

No public API change. Разрешить internal factory/helper внутри SkiaWindow.cs для общих production/test путей создания SettingsScreen и TacticalBattleScreen с уже разрешённым skin. Боевой экран использует конструктор из US-0002-TK-0002.

## Implementation steps

1. Инициализировать каталог из Path.Combine(AppContext.BaseDirectory, "Skins") один раз, диагностика через безопасный callback InterfaceLog. Прочитать предпочтение, определить фактический ID. Каталог живёт дольше созданных экранов и освобождается после завершения их рисования. Уничтожение повторно безопасно.
2. В OpenSettingsAsync передать варианты, локализованный Classic, эффективный ID и callback, принимающий только доступный ID. Публиковать его в памяти после успешной записи через TacticalBattleSkinPreference. При ошибке настройки показывают прежний активный ID.
3. В CreateTacticalBattleView передать разрешённый скин настоящему TacticalBattleScreen. Setup и мир остаются вне боевого scope. Пройти вход F8, новый бой, выход и повторный вход. При новом запуске без выбранного пакета показать Classic в настройках и в бою.
4. Автоматически пройти реальную папку и JSON, SettingsScreen с callback, создание настоящего боевого экрана и Render двух скинов из одной seed/сценарной фикстуры. Проверить исходы, отсутствие изменения сохранения мира и данные подсказок, а не только изменение цвета.
5. Провести нативную проверку на доступной платформе на опубликованном клиенте: скопировать пакет, запустить, выбрать, открыть бой, пройти движение, огонь, эффект, вкладки, итог и подтверждение. Изменить только manifest и ресурс пакета, перезапустить и увидеть новый дизайн. Повторить после удаления пакета. Отдельно зафиксировать платформу, версии, скриншоты по ролям и непроведённые проверки.

## Out of scope

Новый боевой режим, переходы на другие окна, механика торпед и производство художественного дизайна без предоставленного задания.

## Invariants

Активный бой создаётся CreateTacticalBattleView (SkiaWindow.cs:798, :1080). Основной экран настроек уже доступен через OpenSettingsAsync. Одинаковые seed и приказы дают одинаковое игровое состояние.

## Tests

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `SkinBattleIntegrationTests.Installed_pack_flows_from_settings_to_real_battle_and_survives_restart`
- `SkinBattleIntegrationTests.Missing_pack_falls_back_and_battle_state_is_skin_independent`
- `BattleNavigationTests.RealBattleAndNewBattleReplaceOneOpaqueModalAndRestoreWorldCameraAndSpeed`
- `BattleNavigationTests.ActiveEscapeRequiresConfirmationAndActualExitPrecedesResume`

Будущие команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms и сохранять его вывод рядом с TRX.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~SkinBattleIntegrationTests.Installed_pack_flows_from_settings_to_real_battle_and_survives_restart|FullyQualifiedName~SkinBattleIntegrationTests.Missing_pack_falls_back_and_battle_state_is_skin_independent|FullyQualifiedName~BattleNavigationTests.RealBattleAndNewBattleReplaceOneOpaqueModalAndRestoreWorldCameraAndSpeed|FullyQualifiedName~BattleNavigationTests.ActiveEscapeRequiresConfirmationAndActualExitPrecedesResume" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0016/<run>/us3-tk4" --logger "trx;LogFileName=focused.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/SkiaWindow.cs --verify-no-changes
dotnet format whitespace tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --include tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SkinBattleIntegrationTests.cs --verify-no-changes
git diff --check
```

Регрессии из реального маршрута: BattleNavigationTests.RealBattleAndNewBattleReplaceOneOpaqueModalAndRestoreWorldCameraAndSpeed; BattleNavigationTests.ActiveEscapeRequiresConfirmationAndActualExitPrecedesResume.

## Definition of Done

- Выполнены все шаги внутри allowlist.
- Для US3-AC1, US3-AC2, US3-AC3, US3-AC4 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены с фактическими результатами. Исходное падение при наличии записано отдельно.
- Соблюдены инварианты и границы изменений. Реальные команды и результат проверок записаны в evidence исполнения.

## Self-containment check

Определены обе production точки подключения, источник данных, fallback и end-to-end сценарий. Работа вне allowlist для этой интеграции не требуется.
