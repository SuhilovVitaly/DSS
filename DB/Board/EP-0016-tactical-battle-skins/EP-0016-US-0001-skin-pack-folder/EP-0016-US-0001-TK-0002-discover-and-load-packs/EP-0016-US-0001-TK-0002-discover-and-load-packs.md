---
epic: EP-0016-tactical-battle-skins
story: EP-0016-US-0001-skin-pack-folder
ticket: EP-0016-US-0001-TK-0002-discover-and-load-packs
title: Обнаружение и проверка пакетов
stage: done
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0016-US-0001-TK-0001-skin-manifest]
files_touched: 3
serves: [US1-AC1, US1-AC2, US1-AC3]
created: 2026-11-06T08:02:55Z
revision: 2
---

# Обнаружение и проверка пакетов

## Why

Папка скинов должна быть рабочей границей установки пользовательского оформления.

## Decisions

Применяются [D-01–D-04](../../Documentation.md#подтверждённые-требования) и [утверждение истории](../EP-0016-US-0001-skin-pack-folder.md#решения-и-согласование). Разрешение касается планирования. Статус тикета присвоен по workflow epic-writer.

## Assumptions

A-04/A-05. Каталог строится один раз при запуске окна клиента. Classic встроен и доступен при ошибках файловой системы.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleSkinCatalog.cs` | Новый файл. SkiaWindow.SettingsFilePath и Localization.ReadLanguageSetting разрешают Settings.json относительно AppContext.BaseDirectory. | Обнаружение, диагностика, загрузка ресурсов и владение их временем жизни. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleSkin.cs` | Модель и Parse из US-0001-TK-0001. | При необходимости передача ссылок и проверенных ресурсов каталогу. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SkinCatalogTests.cs` | Новый файл. Для тестов использовать каталог во временной директории процесса с уборкой. | Настоящие файлы manifest/PNG, плохие данные и независимость от порядка каталогов. |

## Public API after the change

В том же namespace internal `TacticalBattleSkinCatalog : IDisposable` с `Load(string root, Action<string>? warning = null)`, `Options` как неизменяемый список `(Id, DisplayName)`, `GetOrClassic(string? id)` и `TryGet(string id, out TacticalBattleSkin skin)`. Ключи сравниваются ordinal. В разрешённом скине предоставить типизированное получение загруженных `SKImage` и `SKTypeface` по роли/свойству, собственник ресурсов — каталог.

## Implementation steps

1. Сканировать только непосредственные подпапки root с skin.json. Выявлять все конфликтующие пакеты по ID до публикации. Зарезервированный classic отдавать только из встроенного определения. Список начинается с Classic, далее сортируется по ID ordinal.
2. Проверить файлы и форматы ресурсов по ограничениям US-0001. Отвергать символические ссылки и reparse points на пути от корня сканирования до файла, проверять принадлежность файла пакету. Загружать только ссылки из манифеста. При ошибке освободить уже открытые ресурсы этого пакета.
3. Диагностировать папку, роль или файл и причину. Ошибка одного пакета даёт остальным пройти. Если root недоступен, вернуть каталог с Classic. Запустить тесты с другим текущим каталогом и заведомо отличающимся PNG.

## Out of scope

UI, установка пакета в publish и загрузка пакетов по сети.

## Invariants

Обнаружение выполняется перед использованием, рендеру передаются готовые значения. Ошибки скина изолированы от загрузки игрового мира (TacticalBattle.md:12 описывает отдельную границу каталога боя).

## Tests

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `SkinCatalogTests.Catalog_discovers_a_pack_and_loads_assets_from_its_folder`
- `SkinCatalogTests.Bad_packs_and_duplicate_ids_leave_other_packs_and_classic_usable`
- `SkinCatalogTests.Catalog_rejects_escape_and_reparse_paths`

Будущие команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms и сохранять его вывод рядом с TRX.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~SkinCatalogTests.Catalog_discovers_a_pack_and_loads_assets_from_its_folder|FullyQualifiedName~SkinCatalogTests.Bad_packs_and_duplicate_ids_leave_other_packs_and_classic_usable|FullyQualifiedName~SkinCatalogTests.Catalog_rejects_escape_and_reparse_paths" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0016/<run>/us1-tk2" --logger "trx;LogFileName=focused.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleSkinCatalog.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleSkin.cs --verify-no-changes
dotnet format whitespace tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --include tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SkinCatalogTests.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги внутри allowlist.
- Для US1-AC1, US1-AC2, US1-AC3 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены с фактическими результатами. Исходное падение при наличии записано отдельно.
- Соблюдены инварианты и границы изменений. Реальные команды и результат проверок записаны в evidence исполнения.

## Self-containment check

Модель задаётся предыдущим тикетом, точка подключения окна описана US-0003. Каждый путь и порядок обработки заданы в US-0001.

## Исполнение

Каталог сканирует один уровень, разрешает роли и декодирует PNG/TTF/OTF до публикации пакета. Classic доступен первым, конфликтующие ID отклоняются, готовые ресурсы принадлежат каталогу. При ошибке отдельного пакета сохраняются остальные. Проверены локальные файлы, границы и ссылки, диагностика, частичный override и отказ на неправильном Unicode. Зарезервированные имена устройств Windows запрещены на обеих платформах.

Сборка и `dotnet test --no-restore --filter "FullyQualifiedName~SkinCatalogTests|FullyQualifiedName~SkinManifestTests"` — 37/37 PASS. Scoped whitespace Client и Client.Tests, самопроверка и staged diff check прошли. MSB9008 относится к устаревшему restore graph ModApi. TRX: `D:/DeepSpaceSaga/artifacts/validation/ep-0016/20261106-execution-43bf71/us1-tk2/focused.trx`.

Проверено US1-AC1 и US1-AC3. Поставка contrast и подключение сканирования к запуску приложения проверяются отдельно.
