---
epic: EP-maintenance
story: EP-maintenance-US-0022-code-review-bugs
ticket: EP-maintenance-US-0022-TK-0019-sheet-frame-count
title: Лист спрайтов с числом кадров не 32 проходит проверку
stage: draft
implementation_status: not_started
severity: minor
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC19]
created: "2026-11-15T12:00:00Z"
revision: 1
---

# Лист спрайтов с числом кадров не 32 проходит проверку

[История](../EP-maintenance-US-0022-code-review-bugs.md)

## Ожидаемый результат

Пакет скина с листом корабля, где число кадров не 32, либо отклоняется при загрузке с предупреждением, либо кадр по курсу считается для фактического числа направлений.

## Why

Найдено код-ревью 2026-11-15 на main `113b8506`. Источник: EP-0024-US-0001-TK-0002-tabletop-mode, EP-0024-US-0002-TK-0001-sprite-renderer. Серьёзность: minor. Уверенность: CONFIRMED.

**Дефект.** `ShipSheet` принимает любое `frames`, кратное `columns`, в пределах 1…512. `TacticalShipSprites.Frame()` всегда даёт индекс 0…31 по 32 направлениям, `Draw` берёт `% sheet.Frames`.

**Сценарий.** Пакет с `frames: 16, columns: 8` (512×128) загружается; курс 180° → кадр 24, `24 % 16 = 8` → нос вправо вместо влево. При `frames: 64` используется только половина листа.

## Code context

Строки указаны по `113b8506`.

| Файл | Состояние |
|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleSkinRenderScope.cs:21-32` | Проверка геометрии листа. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalShipSprites.cs:25-29, 51` | `Frame` и выбор кадра. |

## Implementation steps

1. Воспроизвести сценарий тестом из раздела Tests до исправления.
2. Отклонять лист с `frames != 32` (UiSpec EP-0024 §2.1) или считать `k = round((course+90)/(360/frames)) mod frames`.
3. Прогнать тест и соседние тесты затронутых классов.

## Tests

`SkinManifestTests.Invalid_sheet_geometry_is_ignored`: InlineData `(8, 16, 512, 128)` ожидает null (или тест кадра по курсу для 16 направлений).

## Definition of Done

- AC19 выполнен, тест из раздела Tests проходит и падает без исправления.
- В разделе «Статус» записаны команды и результаты проверок.

## Статус

Не начат.
