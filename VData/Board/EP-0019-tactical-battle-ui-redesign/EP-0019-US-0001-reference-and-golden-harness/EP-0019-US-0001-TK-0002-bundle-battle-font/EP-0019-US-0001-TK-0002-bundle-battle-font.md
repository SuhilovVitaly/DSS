---
epic: EP-0019-tactical-battle-ui-redesign
story: EP-0019-US-0001-reference-and-golden-harness
ticket: EP-0019-US-0001-TK-0002-bundle-battle-font
title: Поставка шрифта DejaVu Sans и единая растеризация текста
stage: draft
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0019-US-0001-TK-0001-fix-sdk-build-ambiguity]
files_touched: 7
serves: [US1-AC2]
created: 2026-11-08T09:31:05Z
revision: 3
---

# Поставка шрифта DejaVu Sans и единая растеризация текста

## Why

Игра должна выглядеть одинаково на Windows, macOS и Ubuntu. Для этого нужны два условия:

1. **Один и тот же шрифт.** Вид боя задан шрифтом DejaVu Sans из файлов эпика (DesignSpec §2). На Windows и macOS этого шрифта нет, а на Ubuntu системная версия может отличаться. Поэтому шрифт поставляется с клиентом и грузится только из файла.
2. **Одинаковая растеризация.** Skia растрирует глифы через FreeType на Ubuntu, DirectWrite на Windows и CoreText на macOS, и их умолчания по хинтингу и сглаживанию разные. Только хинтинг FreeType по умолчанию даёт до 0,61 % отличающихся пикселей, больше допуска регрессионных снимков (DesignSpec §12.5). Поэтому параметры текста задаются явно и одинаково для всех ОС.

## Decisions

Применяются [D-01–D-09](../../Documentation.md#решения-пользователя). Нормативный вид — [DesignSpec](../../DesignSpec.md). Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-05. Лицензия DejaVu разрешает распространение с программой.

Пакет `SkiaSharp` содержит нативные библиотеки для Windows и macOS, `SkiaSharp.NativeAssets.Linux` уже подключён в `DeepSpaceSaga.Client.csproj:60`. Новые пакеты не нужны.

## Code context

Строки — `main` @ `8c6f4e2a`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Fonts/DejaVuSans.ttf` | нет | Скопировать из `DB/Board/EP-0019-tactical-battle-ui-redesign/Assets/fonts/`. |
| `src/DeepSpaceSaga.Client/UI/Fonts/DejaVuSans-Bold.ttf` | нет | То же. |
| `src/DeepSpaceSaga.Client/UI/Fonts/LICENSE-DejaVu.txt` | нет | То же (csproj :107-108 уже копирует `*.txt`). |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleTypography.cs` | нет | Новый статический класс: `Regular`, `Bold` (`SKTypeface.FromFile(Path.Combine(AppContext.BaseDirectory, "UI", "Fonts", ...))`, только `Path.Combine`, без `\\` и `/` в литералах), `IsBundled`, фабрика `TextPaint(size, bold)` с настройками DesignSpec §2. Запасной вариант — `MenuStyle.TypefaceRegular/Bold` с ошибкой в логе и `IsBundled = false`. |
| `src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj` | :104-108 копирование `UI/Fonts` | Проверить, что новые файлы попадают в build и publish; править только при необходимости. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleTypographyTests.cs` | нет | Новые тесты. |

## Public API after the change

```csharp
internal static class BattleTypography
{
    public static SKTypeface Regular { get; }
    public static SKTypeface Bold { get; }
    public static bool IsBundled { get; }            // оба начертания загружены из поставленных файлов
    public static SKPaint TextPaint(float size, bool bold);
    // IsAntialias = true, SubpixelText = true, LcdRenderText = false,
    // HintingLevel = SKPaintHinting.NoHinting, IsAutohinted = false, IsLinearText = true
}
```

Настройки побуквенно совпадают с `TextPaint` в DesignSpec §2 «Шрифт».

## Implementation steps

1. Положить три файла в `UI/Fonts/`, сверить SHA-256 с `Assets/README.md`.
2. Реализовать `BattleTypography` по образцу `XenonStyle.LoadTypeface` (`UI/Controls/XenonStyle.cs:44-63`). Не использовать `SKTypeface.FromFamilyName`, `SKFontManager.MatchCharacter` и иной системный подбор шрифта.
3. Реализовать `TextPaint` с настройками из Public API. В US-0002–US-0006 весь текст боя рисуется и измеряется только через `TextPaint`; это проверяется в ревью каждого тикета.
4. Тесты:
   - оба начертания загружены из файла (`IsBundled`), имя семейства `DejaVu Sans`;
   - глифы есть для всех значений `TacticalBattle.*` RU и EN и для символов `→ · – − ° %` (по образцу `GlyphCoverageTests`);
   - `TextPaint` возвращает настройки из Public API;
   - ширина строки `"Выполнить залп [Enter]"` через `TextPaint` равна 185,9033 (Regular, 15) и 205,7153 (Bold, 15) с точностью 0,01 — линейные метрики не зависят от ОС;
5. В «Исполнении» записать ОС, на которой прошли тесты. Если доступна вторая ОС из трёх, прогнать `BattleTypographyTests` и там.

## Out of scope

Использование шрифта в отрисовке (US-0002).

## Invariants

Остальные экраны продолжают использовать прежние шрифты и прежние настройки текста.

## Tests

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `BattleTypographyTests.Bundled_dejavu_fonts_load_from_output`
- `BattleTypographyTests.Battle_strings_have_glyphs_in_bundled_font`
- `BattleTypographyTests.Text_paint_uses_platform_neutral_settings`
- `BattleTypographyTests.Text_width_matches_reference_metrics`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~BattleTypographyTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0019/<run>/us1-tk2" --logger "trx;LogFileName=focused.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleTypography.cs --verify-no-changes
dotnet format whitespace tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --include tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleTypographyTests.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US1-AC2 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты; ожидания DesignSpec не изменены.

## Self-containment check

Файлы шрифта берутся из Assets эпика; путь и хеши указаны. Настройки растеризации заданы в Public API и DesignSpec §2 «Шрифт».

## Исполнение

Не начато.
