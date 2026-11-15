---
epic: EP-0024-battle-field-redesign
story: EP-0024-US-0001-skin-pack
ticket: EP-0024-US-0001-TK-0002-tabletop-mode
title: Признак режима и доступ к листам спрайтов
stage: done
layer: client-ui
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0024-US-0001-TK-0001-pack-files]
files_touched: 4
serves: [US1-AC2, US1-AC3]
created: 2026-11-12T13:00:00Z
revision: 1
---

# Признак режима и доступ к листам спрайтов

## Why

Рендер поля должен узнавать режим по скину и получать листы, не зная ID пакета (A-01).

## Decisions

Применяются [D-01–D-11](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md); всё, что там не названо, остаётся как в Classic на базе. Материалы — [Materials](../../Materials/README.md). Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-01, A-02. Листы — роли `field.unit.sprite.<light|heavy|thug|carrier|torpedo>.<own|enemy|disabled>` (у carrier и torpedo без disabled); `columns` = 8, `frames` = 32; размер ячейки = ширина листа / columns. Эффекты — роли `field.fx.<boom-hit|boom-kill|puff|trail>`: строка кадров, ячейка = высота строки, метрики `frames` и `frame-ms` (UiSpec §2.1a).

## Code context

Строки — `main` @ `d7895f73`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleSkinRenderScope.cs` | :7-80 доступ к ролям текущего скина | `Tabletop` (метрика `field.style.tabletop` = 1), `ShipSheet(string model, string side)` → `(SKImage Image, int Columns, int Frames, int Cell)?` и `EffectStrip(string name)` → `(SKImage Image, int Frames, int Cell, float FrameMs)?`. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleSkin.cs` | :39-48 `Image`, `Metric` | Без изменения схемы; при необходимости вспомогательный метод чтения метрики. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SkinManifestTests.cs` | разбор ролей и лимитов | Тесты разбора `field.style` и ролей листов; лимит 32 ресурса и пикселей пакета не превышен. |
| `DB/Documentation/04-Engineering/TacticalBattleSkins.md` | таблица поверхностей | Строки `field.style`, `field.unit.sprite.*` и `field.fx.*`. |

## Public API after the change

`internal static bool TacticalBattleSkinRenderScope.Tabletop`; `internal static ShipSheet? TacticalBattleSkinRenderScope.ShipSheet(string model, string side)`; `internal readonly record struct ShipSheet(SKImage Image, int Columns, int Frames, int Cell)`; `internal static EffectStrip? TacticalBattleSkinRenderScope.EffectStrip(string name)`; `internal readonly record struct EffectStrip(SKImage Image, int Frames, int Cell, float FrameMs)`.

## Implementation steps

1. Добавить признак режима, выбор листа и строки эффекта.
2. Проверить ширину и высоту листа (`Cell × Columns`, `Cell × Frames/Columns`); у строки эффекта ширина = `Cell × Frames`; при несовпадении лист или строка не используются.
3. Тесты разбора и выбора; описать роли в контракте скинов.

## Out of scope

Отрисовка спрайтов (US-0002).

## Invariants

В Classic `Tabletop` ложен без чтения ресурсов.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `SkinManifestTests`
- `ShippedSkinTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~SkinManifestTests|FullyQualifiedName~ShippedSkinTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0024/<run>/us1-tk2" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleSkinRenderScope.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US1-AC2, US1-AC3 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Имена ролей и метрик — UiSpec §1.1, §2.1; черновой манифест в материалах проходит загрузчик.

## Исполнение

Не начато.


### Результат us1-tk2

Режим задаётся метрикой; листы и эффекты читаются из каталога с проверкой геометрии и таймингов, fallback сохранён.

Проверки: 43/43 PASS; сборка Client/tests выполнена тестовой командой. Scoped whitespace и diff check PASS. Evidence: `D:/DeepSpaceSaga/artifacts/validation/ep-0024/20261112-implementation-01/us1-tk2/client.trx`, `us1-tk2-test.log`, `us1-tk2-format.log` в том же run. Самопроверка diff выполнена; это не независимое review. Native/product gates отдельно в ExecutionStatus.
