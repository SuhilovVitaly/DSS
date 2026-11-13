---
epic: EP-0024-battle-field-redesign
story: EP-0024-US-0011-steel-panels
ticket: EP-0024-US-0011-TK-0001-steel-roles
title: Роли и текстуры стальной темы в пакете
stage: draft
layer: client-data
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0024-US-0001-TK-0001-pack-files]
files_touched: 3
serves: [US11-AC1]
created: 2026-11-12T17:00:00Z
revision: 1
---

# Роли и текстуры стальной темы в пакете

## Why

Цвета, фоны и кнопки панелей задаются ролями пакета `tabletop` (A-17); пакет из материалов уже содержит их, нужно закрепить проверками и контрактом.

## Decisions

Применяются [D-01–D-11](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) §11; всё, что там не названо, остаётся как в Classic на базе. Эталон — [Materials/Reference/panels](../../Materials/Reference/panels). Утверждение — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-17. Роли и картинки — `Materials/Assets/skin/skin.json` и `assets/ui/*.png` (6 градиентов 4×128); загрузчик принимает пакет без предупреждений (проверено на `d7895f73`, review log).

## Code context

Строки — `main` @ `d7895f73`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ShippedSkinTests.cs` | проверки пакетов | Пакет `tabletop`: роли `ui.style` (`steel` = 1), `ui.pilot-column` (`slim` = 1), `ui.panel`, `ui.panel.art`, `ui.panel.bevel`, `ui.button(.art|.primary|.primary.art|.pending|.pending.art)`, `ui.box.target.selected(.art)`, `ui.box.decoy*`, `ui.box.evade*`, `ui.bar.armor`, `ui.bar.hull`, `ui.build.name`, `ui.build.slot.armor`: значения UiSpec §11.2, все `*.art` загружаются; пакет укладывается в лимиты (≤ 32 ресурса). |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SkinManifestTests.cs` | разбор ролей | Разбор метрик `ui.style.steel` и `ui.pilot-column.slim`. |
| `DB/Documentation/04-Engineering/TacticalBattleSkins.md` | таблица ролей | Строки новых ролей: `ui.style`, `ui.pilot-column`, `ui.panel.bevel`, `ui.bar.armor`, `ui.bar.hull`, `ui.build.name`, `ui.build.slot.armor`; что они меняют (UiSpec §11.1–§11.3, §11.10). |

## Public API after the change

—

## Implementation steps

1. Сверить пакет в `src/DeepSpaceSaga.Client/Skins/tabletop` с материалами (US-0001 копирует его целиком).
2. Добавить проверки ролей и картинок.
3. Описать роли в контракте скинов.

## Out of scope

Код отрисовки (TK-0002, TK-0003).

## Invariants

Classic, Contrast, Xenon Star и их роли не меняются.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `ShippedSkinTests`
- `SkinManifestTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~ShippedSkinTests|FullyQualifiedName~SkinManifestTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0024/<run>/us11-tk1" --logger "trx;LogFileName=client.trx"

git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US11-AC1 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Получившийся кадр сверен на глаз с `Materials/Reference/panels` (тот же кадр и вырезка ×2), расхождения записаны в «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Значения ролей — UiSpec §11.2; файлы — в материалах эпика.

## Исполнение

Не начато.
