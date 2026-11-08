# EP-0019 — эталонные материалы

Материалы получены 2026-11-08 (кадры пересняты в тот же день с платформенно-независимым текстом, см. [DesignSpec §2 «Шрифт»](../DesignSpec.md#шрифт)) на ветке `main` @ `8c6f4e2a` (Linux, .NET SDK 8.0.425, SkiaSharp 2.88.8). Нормативное значение каждой папки описано в [DesignSpec](../DesignSpec.md).

| Путь | Что это | Нормативность |
|---|---|---|
| `screens/*.png` | Финальные эталонные кадры 1920×1080 (макет 1280×720 × 1.5), без курсора ОС | **golden** для US-0006 |
| `mock-renderer/` | Код, которым построены `screens/` и `cards/` | **геометрия, цвета, тексты, порядок рисования** |
| `cards/C-rows-*.png` | Карточки пилотов (вариант C) в масштабе 3 | справочно, увеличенный вид |
| `fonts/` | DejaVu Sans Regular/Bold и лицензия | файлы для поставки с клиентом |
| `battle-data-seed1.txt` | Значения снимков боя seed 1, по которым сняты кадры | проверка данных до сравнения пикселей |
| `notion/notion-1..3.png` | Исходные картинки дизайна пользователя из Notion (Движение, Стрельба, Результаты) | происхождение дизайна |
| `baseline/*.png` | Текущий интерфейс до EP-0019 на том же бою | сравнение «до/после» |

## SHA-256

```
a8f54fd209839e38f73ee74b6bf5953720e2a89458655bcda05bec0bc6147f8e  screens/1-move.png
00227b601151c744c12feff72bd5f7be16488cbe2dd5e8d58bff964861f0181c  screens/1-move-no-hover.png
03c888f2bbf2cec3f22bcf4520a1b02a30ce3233d8afe5b63e64a18a06958611  screens/2-fire.png
63fb8e227f2852b72876a1feb3ed38cb2c80ecaa9ac1c1f8e2099220daa0347d  screens/2-fire-hover-own.png
b838975012715d33953c87a16216ef4a4f1a85be6b676093f4787634ca84b3ca  screens/3-salvo-flight.png
a765bd02afa9c07725a090804b61eb90a93907444e051ce6cf3cf54172b27b9f  screens/4-salvo-results.png
b9bb72a30b1e30a720b4f91084cc63e4fbcf9f3dd1c92d012c5427a5f6bbe30f  screens/5-outcome.png
690243adfefe0ce154b547db6205794bd30ac4277275179517a90994f4980648  fonts/DejaVuSans.ttf
d1c3ff99f1e1ce1827a33efd4dad81f40babda06bff9e43bd7591c86662a287b  fonts/DejaVuSans-Bold.ttf
```

## Как пересобрать эталон

Макетный рендерер — отдельная консольная программа. Она не входит в решение и не должна попадать в `src/` или `tests/`. Для повторной съёмки:

1. Скопировать `mock-renderer/*.cs` и `mock-renderer/battlemock.csproj.txt` (переименовать в `battlemock.csproj`) во временный каталог `D:/DeepSpaceSaga/artifacts/validation/ep-0019/<run>/battlemock/`. Пути `ProjectReference` в файле проекта указаны относительно `tools/battlemock` в корне DSS; поправить их на абсолютные пути к `src/DeepSpaceSaga.Client` и `src/DeepSpaceSaga.Engine.LocalClient`.
2. Задать `MOCK_FONT_DIR` — абсолютный путь к `fonts/`. Макет грузит шрифт только из этих файлов; без переменной он завершается ошибкой. Системный DejaVu не используется.
3. `dotnet run -c Release -- <корень DSS> <каталог вывода>` — кадры `screens/`.
   `MOCK_CARDS=1` — листы карточек (`cards/`); `MOCK_CURSOR=1` — кадры с нарисованным курсором (для показа, не для golden).
   Третий аргумент `dump` печатает значения снимков (формат `battle-data-seed1.txt`).
4. Если на свежем `main` C# 12 сообщает о неоднозначном `SKShader.CreateLinearGradient([...], null, ...)` в `GameSessionScreen.cs` и `TacticalMapDepthRenderer.cs`, это известная проблема SDK 8.0.4xx; эталон снимался с локальной правкой `new SKColor[] { ... }` в копии исходников.

Код в `mock-renderer/` содержит следы промежуточных итераций (функции `Toolbar`, `IconCard`, `TextCard`, ветку старых карточек в `Cards`). Нормативны только пути, которые выполняются при `RenderAll` и `RenderCards`: `Move`, `Fire`, `Flight`, `Results`, `Outcome`, `Layout`, `TopBar`, `DetailsHead`, `ActionPanel`, `HoverPanel`, `Fit`, `MapBase`, `Trails`, `Units`, `Label`, `EdgeMarker`, `HoverMark`, примитивы и `RowCard` с проекциями `MoveCards`, `FireCards`, `FlightCards`, `ResultCards`, `OutcomeCards`.
