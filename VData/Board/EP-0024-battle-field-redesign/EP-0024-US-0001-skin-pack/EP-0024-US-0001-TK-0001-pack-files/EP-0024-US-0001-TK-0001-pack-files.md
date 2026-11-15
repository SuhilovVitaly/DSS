---
epic: EP-0024-battle-field-redesign
story: EP-0024-US-0001-skin-pack
ticket: EP-0024-US-0001-TK-0001-pack-files
title: Пакет `tabletop` в клиенте и в сборке
stage: done
layer: client-data
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: []
files_touched: 7
serves: [US1-AC1]
created: 2026-11-12T13:00:00Z
revision: 1
---

# Пакет `tabletop` в клиенте и в сборке

## Why

Новый вид поля поставляется отдельным скином (D-02); пакет уже собран в материалах эпика.

## Decisions

Применяются [D-01–D-11](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md); всё, что там не названо, остаётся как в Classic на базе. Материалы — [Materials](../../Materials/README.md). Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-02, A-03. По прямому уточнению пользователя 2026-11-12 отсутствующий
манифест восстанавливается по UiSpec; готовые PNG копируются без изменений.
Scope дополнен `Materials/Assets/skin/skin.json`, записью происхождения пакета
и актуализацией статуса исполнения. Это реконструкция по нормативному тексту,
не восстановление утраченного файла побайтно. Запекатель остаётся отдельным
блокированным тикетом и не блокирует runtime.

## Code context

Строки — `main` @ `d7895f73`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Skins/tabletop/skin.json` | — | Скопировать `DB/Board/EP-0024-battle-field-redesign/Materials/Assets/skin/skin.json`. |
| `src/DeepSpaceSaga.Client/Skins/tabletop/assets/field.png` | — | Скопировать из `DB/Board/EP-0024-battle-field-redesign/Materials/Assets/skin/assets/`. |
| `src/DeepSpaceSaga.Client/Skins/tabletop/assets/units/pxd-*.png` | — | Скопировать 13 листов из `DB/Board/EP-0024-battle-field-redesign/Materials/Assets/skin/assets/units/` (11 кораблей, 2 торпеды). |
| `src/DeepSpaceSaga.Client/Skins/tabletop/assets/fx/*.png` | — | Скопировать 4 строки эффектов из `DB/Board/EP-0024-battle-field-redesign/Materials/Assets/skin/assets/fx/` (`boom-hit`, `boom-kill`, `puff`, `trail`). |
| `src/DeepSpaceSaga.Client/Skins/tabletop/assets/ui/*.png` | — | Скопировать 6 градиентов стальной темы из `DB/Board/EP-0024-battle-field-redesign/Materials/Assets/skin/assets/ui/` (`steel`, `steel-button`, `gold-button`, `decoy-button`, `evade-button`, `off-button`; D-11). |
| `src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj` | :43-61 правила копирования `Skins/contrast`, `Skins/xenon-star` | Правило `None Update` для `Skins/tabletop/skin.json` и `Skins/tabletop/assets/**/*.png` с `CopyToOutputDirectory`/`CopyToPublishDirectory` `PreserveNewest` в безусловной группе (как contrast). |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ShippedSkinTests.cs` | проверки xenon-star и contrast | Новый тест: пакет `tabletop` в выходной папке, `Options`, `Image("field.background")`, `Image("field.unit.sprite.heavy.own")`, `Image("field.unit.sprite.torpedo.own")`, `Image("field.fx.boom-kill")` и его метрики (16 кадров, 55 ms), откат повреждённого пакета. |

## Public API after the change

—

## Implementation steps

1. Скопировать пакет из материалов.
2. Добавить правило копирования в проект.
3. Добавить тест в `ShippedSkinTests`.

## Out of scope

Код режима (TK-0002).

## Invariants

Существующие пакеты и правила копирования не меняются.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `ShippedSkinTests`
- `SkinCatalogTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~ShippedSkinTests|FullyQualifiedName~SkinCatalogTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0024/<run>/us1-tk1" --logger "trx;LogFileName=client.trx"
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US1-AC1 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Файлы пакета лежат в материалах эпика; правило копирования повторяет правило contrast.

## Исполнение

2026-11-12: **BLOCKED, не реализовано**. В обоих предоставленных комплектах
отсутствует `Materials/Assets/skin/skin.json`. Шаг переноса пакета без изменений
и US1-AC1 не выполнены. 24 PNG прошли проверку размеров, CRC и DEFLATE и
побайтно совпадают с материалами пользователя; это не проверка загрузки скина.
Подробности, evidence и отсутствующие файлы — [ExecutionStatus](../../ExecutionStatus.md).


### Продолжение по D-12

Восстановлен манифест по UiSpec, 24 исходных PNG перенесены без изменений;
пакет копируется в build/publish. ShippedSkinTests проверяет реальный output,
все 13 листов, эффекты, 24 ресурса, Options и откат повреждённого эффекта.
`dotnet test ... --filter FullyQualifiedName~ShippedSkinTests|FullyQualifiedName~SkinCatalogTests
--no-restore --results-directory D:/DeepSpaceSaga/artifacts/validation/ep-0024/20261112-implementation-01/us1-tk1`
— 7/7 PASS, включая сборку Client и tests; log `us1-tk1-test.log` в указанном run.
Самопроверка: PNG не изменены, schema v1 и другие пакеты не меняются.

## Дополнение: переданный пользователем манифест

2026-11-12 пользователь передал исходный JSON. Scope дополнения: заменить реконструкцию в Materials и runtime; сохранить входной JSON в Materials/Assets/tabletop-supplied.json, исправив в рабочем манифесте только Markdown-артефакт ключа ui.build.name; сверить ShippedSkinTests с фактическими явно заданными art-ролями, сохранив проверки цветов, fallback, ресурсов и геометрии. Обновить происхождение в Documentation, Materials README и ExecutionStatus. PNG не меняются.

Дополнение проверено: 50/50 ShippedSkinTests, SkinManifestTests, TabletopPanelFrameTests, TabletopCommandPanelTests; build 0 warnings/errors, scoped format и diff check PASS. Materials/runtime пакет побайтно равен; 81 роль, 55 ui.*, 24 PNG. Evidence: `D:/DeepSpaceSaga/artifacts/validation/ep-0024/20261112-implementation-01/supplied-manifest/client.trx`, рядом `supplied-manifest-test.log`, `supplied-manifest-build.log`, `supplied-manifest-format-verify.log`. Самопроверка источника и нормализации пройдена.

Уточнение diff check дополнения: исходная вставка содержала два завершающих пробела, проверка указала их. Удалены отдельной косметической правкой входного JSON без изменения данных; повторный staged diff check PASS.
