---
epic: EP-0016-tactical-battle-skins
story: EP-0016-US-0003-select-skin-in-settings
ticket: EP-0016-US-0003-TK-0004-wire-settings-to-battle
title: Связь настроек с реальным экраном боя
stage: done
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0016-US-0003-TK-0003-add-skin-selector, EP-0016-US-0002-TK-0005-theme-information-tabs]
files_touched: 6
serves: [US3-AC1, US3-AC2, US3-AC3, US3-AC4]
created: 2026-11-06T08:02:55Z
revision: 2
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
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` | SkiaWindow.cs:790–831, :1091–1106, :1262–1289, :1510–1527, :1722. Вход F8 перечитывает язык, Start создаёт активный экран, New возвращает в подготовку. | Владение каталогом, подключение выбора в OpenSettingsAsync и CreateTacticalBattleView, освобождение ресурсов. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SkinBattleIntegrationTests.cs` | Новый файл. LocalizationCoverageTests.cs:29 и BattleNavigationTests.cs:20–153 показывают реальные маршруты. | Связать пакет, Settings, F8 и Render. Проверить смену локали, новый бой, выход, fallback и равенство исходов. |
| `src/DeepSpaceSaga.Client/Localization.cs` | Читает язык из стандартного Settings.json. | Использовать тот же путь настроек окна для изолированной сквозной проверки обоих полей. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | Хранит неизменяемый выбранный скин. | Внутренняя диагностика ID для сквозной и нативной проверки. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleNavigationTests.cs` | Fixture создаёт реальное локальное подключение и SkiaWindow без GPU. | Передать изолированный путь к пользовательским настройкам. |
| `tools/DeepSpaceSaga.Performance/TacticalBattleNativeEvidence.cs` | Реальное окно и авторитетная боевая сессия с программным вводом. | Добавить необязательный профиль скинов, проверку Settings и фиксировать фактический ID. |

## Public API after the change

No public API change. Разрешить internal factory/helper внутри SkiaWindow.cs для общих production/test путей создания SettingsScreen и TacticalBattleScreen с уже разрешённым skin. Боевой экран использует конструктор из US-0002-TK-0002.

## Implementation steps

1. Инициализировать каталог из Path.Combine(AppContext.BaseDirectory, "Skins") один раз, диагностика через безопасный callback InterfaceLog. Прочитать предпочтение, определить фактический ID. Каталог живёт дольше созданных экранов и освобождается после завершения их рисования. Уничтожение повторно безопасно.
2. В OpenSettingsAsync передать варианты, локализованный Classic, эффективный ID и callback, принимающий только доступный ID. Публиковать его в памяти после успешной записи через TacticalBattleSkinPreference. При ошибке настройки показывают прежний активный ID.
3. В CreateTacticalBattleView передать разрешённый скин настоящему TacticalBattleScreen. Сохранить `Localization.ApplyLanguage(Localization.ReadLanguageSetting())` при F8 перед созданием подготовки. Пройти F8 → Start, «Новый бой» → Start, выход и повторный вход. При новом запуске без выбранного пакета показать Classic в настройках и в бою.
4. Автоматически пройти реальную папку, JSON, настройки, F8 и Render двух скинов из одной seed/сценарной фикстуры. Через настройки сменить язык и скин в обоих порядках, прочитать оба сохранённых поля и проверить язык на подготовке и в новом активном бою. Сравнить игровые факты, исходы, сохранение мира и данные подсказок. Выход, включая восстановление при потерянном или устаревшем бое, прогнать существующими тестами навигации.
5. На опубликованном клиенте скопировать пакет, запустить, выбрать скин и открыть бой. Пройти движение, огонь, залп, вкладки, итог и подтверждение. Повторить F8 со вторым языком после смены в Settings. Изменить только manifest и ресурс, перезапустить и проверить новый вид. Повторить после удаления пакета. Отдельно зафиксировать платформу, версии, скриншоты по ролям и непроведённые проверки.

## Out of scope

Новый боевой режим, переходы на другие окна, механика торпед и производство художественного дизайна без предоставленного задания.

## Invariants

Активный бой создаётся `CreateTacticalBattleView`. Скин действует внутри Render активного боя, а подготовка, мир и `ShipDestroyedScreen` используют свой штатный рендер. `TacticalBattleFlow.ExitAsync` и возврат камеры и скорости мира сохраняют действующий контракт EP-0015. При одинаковых исходных данных, seed и приказах тема даёт те же игровые факты.

## Tests

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `SkinBattleIntegrationTests.Installed_pack_flows_from_settings_to_real_battle_and_survives_restart`
- `SkinBattleIntegrationTests.Missing_pack_falls_back_and_battle_state_is_skin_independent`
- `BattleNavigationTests.RealBattleAndNewBattleReplaceOneOpaqueModalAndRestoreWorldCameraAndSpeed`
- `BattleNavigationTests.ActiveEscapeRequiresConfirmationAndActualExitPrecedesResume`
- `LocalizationCoverageTests.SavedRussianFromSettingsIsUsedByActualSalvoPanelAndTabs`
- `BattleNavigationTests.ExitClosesTheModalWhenTheEngineNoLongerHasThisBattle`
- `BattleNavigationTests.ExitOfAStaleBattleExitsTheLiveEngineBattleInstead`
- `SkinBattleIntegrationTests.Language_and_skin_survive_settings_changes_and_real_f8_start`

Будущие команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms и сохранять его вывод рядом с TRX.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~LocalizationCoverageTests.SavedRussianFromSettingsIsUsedByActualSalvoPanelAndTabs|FullyQualifiedName~BattleNavigationTests.ExitClosesTheModalWhenTheEngineNoLongerHasThisBattle|FullyQualifiedName~BattleNavigationTests.ExitOfAStaleBattleExitsTheLiveEngineBattleInstead|FullyQualifiedName~SkinBattleIntegrationTests.Language_and_skin_survive_settings_changes_and_real_f8_start" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0016/<run>/us3-tk4" --logger "trx;LogFileName=locale-and-recovery.trx"
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~SkinBattleIntegrationTests.Installed_pack_flows_from_settings_to_real_battle_and_survives_restart|FullyQualifiedName~SkinBattleIntegrationTests.Missing_pack_falls_back_and_battle_state_is_skin_independent|FullyQualifiedName~BattleNavigationTests.RealBattleAndNewBattleReplaceOneOpaqueModalAndRestoreWorldCameraAndSpeed|FullyQualifiedName~BattleNavigationTests.ActiveEscapeRequiresConfirmationAndActualExitPrecedesResume" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0016/<run>/us3-tk4" --logger "trx;LogFileName=focused.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/SkiaWindow.cs --verify-no-changes
dotnet format whitespace tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --include tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SkinBattleIntegrationTests.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги внутри allowlist.
- Для US3-AC1, US3-AC2, US3-AC3, US3-AC4 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены с фактическими результатами. Исходное падение при наличии записано отдельно.
- Соблюдены инварианты и границы изменений. Реальные команды и результат проверок записаны в evidence исполнения.

## Self-containment check

Определены точки подключения настроек и активного экрана, обновление локали при F8, fallback и сквозные проверки. Работа вне allowlist для этой интеграции не требуется.

## Исполнение

SkiaWindow загружает каталог один раз, показывает эффективный выбор, меняет его после успешной записи и передаёт в каждый создаваемый активный бой. Выбор языка и настройки читаются из одного файла. Подготовка использует штатный дизайн. Каталог освобождает ресурсы после деактивации экранов.

- Сборка, scoped whitespace Client, Client.Tests и Performance, staged diff check и самопроверка прошли. Тактический набор и Settings: 250/250 PASS. Три сквозных теста прошли реальный маршрут Settings → F8 → Start, новый бой, повторный вход, перестановку изменений языка и скина, удаление пакета и сравнение полного итога. `D:/DeepSpaceSaga/artifacts/validation/ep-0016/20261106-execution-43bf71/us3-tk4/focused.trx`.
- Release publish PASS. Клиентская DLL и файлы пакета побайтно совпадают с экземпляром, использованным нативным драйвером. `D:/DeepSpaceSaga/artifacts/validation/ep-0016/20261106-execution-43bf71/us3-tk4/publish-binary-verification.json`.
- Нативный OpenGL 1280×720 на Intel Arc 140V: 38/38 выбранный внешний пакет RU, 37/37 обновлённый пакет EN после перезапуска, 37/37 fallback EN после удаления манифеста. Совпали исход, боевые итоги и 224 вызова RNG. В каждом случае проверены новый бой, вкладки, флажки, фазы, подтверждение, выход, F5 и F9. Драйвер обновлён на три действующих флажка и их реальные значения по умолчанию. В двух EN кадр подготовки побайтно одинаков, RGB заменённого PNG подтверждён на поле.
- Нативный ввод был программным через обработчики окна. Физический ввод и отдельная художественная приёмка остаются за рамками этого evidence. Сводка `D:/DeepSpaceSaga/artifacts/validation/ep-0016/20261106-execution-43bf71/native/comparison.json`, проверка пикселей `D:/DeepSpaceSaga/artifacts/validation/ep-0016/20261106-execution-43bf71/native/pixel-comparison.json`.

US3-AC1–AC4 покрыты на реальном пути клиента и в нативном окне.
