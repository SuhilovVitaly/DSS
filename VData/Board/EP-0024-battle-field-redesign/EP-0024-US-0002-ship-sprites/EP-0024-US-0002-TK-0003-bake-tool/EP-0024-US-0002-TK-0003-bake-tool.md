---
epic: EP-0024-battle-field-redesign
story: EP-0024-US-0002-ship-sprites
ticket: EP-0024-US-0002-TK-0003-bake-tool
title: Запекатель спрайтов как инструмент тестового проекта
stage: approved
layer: client-tests
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0024-US-0001-TK-0001-pack-files]
files_touched: 3
serves: [US2-AC1]
created: 2026-11-12T13:00:00Z
revision: 1
---

# Запекатель спрайтов как инструмент тестового проекта

## Why

Листы спрайтов получены кодом; при правке моделей их нужно перезапечь (A-12).

## Decisions

Применяются [D-01–D-11](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md); всё, что там не названо, остаётся как в Classic на базе. Материалы — [Materials](../../Materials/README.md). Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-12. Запуск только при заданной переменной `DSS_SHIP_BAKE_DIR`; без неё тест ничего не делает.

## Code context

Строки — `main` @ `d7895f73`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Client.Tests/Tools/ShipSpriteBake/*.cs` | — | Перенести `DB/Board/EP-0024-battle-field-redesign/Materials/Bake/ZzSpriteBake.cs`, `ZzPixelBake.cs`, `ZzTypedBake.cs`, `ZzDetailBake.cs` в один инструмент `ShipSpriteBake` (без тестов Star General, гладких листов и сета 14, которые не нужны пакету); переменная `DSS_SHIP_BAKE_DIR`; пишет 13 листов `pxd-<модель>-<сторона>.png` детального сета (корабли и торпеды). |
| `tools/fx/explosions.py` | — | Скопировать `DB/Board/EP-0024-battle-field-redesign/Materials/Bake/explosions.py` — генератор строк эффектов (Python 3 + Pillow): `python3 tools/fx/explosions.py <папка>`. |
| `DB/Documentation/04-Engineering/TacticalBattleSkins.md` | раздел пакетов | Как перезапечь листы и пересоздать эффекты. |

## Public API after the change

`[Fact, Trait("Category", "Tool")] ShipSpriteBake.Bake()`.

## Implementation steps

1. Перенести код, убрать лишние ветки (гладкие листы, Star General, сет 14), оставить детальные пиксельные листы четырёх моделей и торпеды (`DetailModels()`).
2. Запустить с `DSS_SHIP_BAKE_DIR` и сравнить размеры и число непрозрачных пикселей кадров с листами пакета (допуск ±2 %), записать результат. На машине планирования совпадение было попиксельным.
3. Положить генератор эффектов в `tools/fx/` и проверить, что он воспроизводит 4 строки `assets/fx` (там же — попиксельно).

## Out of scope

Правка моделей.

## Invariants

Без переменной тест проходит мгновенно и не пишет файлов.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `ShipSpriteBake`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~ShipSpriteBake" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0024/<run>/us2-tk3" --logger "trx;LogFileName=client.trx"
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US2-AC1 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Исходники — `DB/Board/EP-0024-battle-field-redesign/Materials/Bake`, параметры моделей — README там же.

## Исполнение

2026-11-12: **BLOCKED, не реализовано**. `Materials/Bake` содержит только README.
`ZzSpriteBake.cs`, `ZzPixelBake.cs`, `ZzTypedBake.cs`, `ZzDetailBake.cs` и
`explosions.py` отсутствуют также в предоставленном `VData` и не найдены в
доступной истории Git. Перенос моделей и проверка перезапекания невозможны
без исходников; копирование PNG не подменяет запекатель. См.
[ExecutionStatus](../../ExecutionStatus.md).


### Текущий статус

BLOCKED только этот тикет: отсутствуют ZzSpriteBake.cs, ZzPixelBake.cs, ZzTypedBake.cs, ZzDetailBake.cs и explosions.py. Пользовательский D-12 разрешил использование готовых PNG и независимые тикеты; D-13 восстановил манифест. Воспроизводимое перезапекание не реализовано и не подменено копированием PNG.
