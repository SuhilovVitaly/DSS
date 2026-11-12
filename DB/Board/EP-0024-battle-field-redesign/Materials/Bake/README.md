# Запекатель спрайтов и генератор эффектов

Четыре файла — части одного класса тестов `ZzSpriteBake`:

| Файл | Что внутри |
|---|---|
| `ZzSpriteBake.cs` | Основа: модель тяжёлого корабля в духе Tetrarch (`Ship()`), примитивы (`Loft`, `Cylinder`, `Box`, `Wedge`, `Disc`), программный растеризатор с буфером глубины, сглаженные нормали, суперсэмплинг 4×4, камера под углом `asin(0,62)` |
| `ZzPixelBake.cs` | Пиксельная обработка `RenderPixel`: ступенчатые палитры `own`, `enemy` и `disabled` с дизерингом 2×2, блики, светлая кромка, линии перекрытий, контур; модель носителя `CarrierShip()` |
| `ZzTypedBake.cs` | Модели `LightShip()`, `HeavyShip()`, `TagShip()` и тест сета 14 `BakeTyped` (пакету не нужен, модели — основа детальных) |
| `ZzDetailBake.cs` | Детальный сет (D-10): `LightShipDetailed()`, `HeavyShipDetailed()`, `TagShipDetailed()`, `CarrierShipDetailed()`, `Torpedo()` и тест `BakeDetailed`, который пишет листы `pxd-<модель>-<сторона>.png` |

Перезапекание листов пакета. Скопировать четыре `.cs` в `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/` и выполнить:

```
DSS_DETAIL_DIR=<папка> dotnet test tests/DeepSpaceSaga.Client.Tests -p:TreatWarningsAsErrors=false --filter FullyQualifiedName~ZzSpriteBake.BakeDetailed
```

Тест без переменной ничего не делает. `DSS_DETAIL_SHOWCASE=<папка>` дополнительно пишет полосы для превью вращения. Параметры — `DetailModels()`:

| Модель | Пикселей на единицу модели | Ячейка | Палитры |
|---|---|---|---|
| light | 20 | 64 | own, enemy, disabled |
| heavy | 23 | 64 | own, enemy, disabled |
| thug | 22 | 64 | own, enemy, disabled |
| carrier | 26 | 96 | own, enemy |
| torpedo | 22 | 32 | own, enemy |

Эффекты — `explosions.py` (Python 3 + Pillow, без numpy): `python3 -I explosions.py <папка>` пишет `boom-hit.png`, `boom-kill.png`, `puff.png`, `trail.png`. Огонь — ступенчатая палитра белый → жёлтый → оранжевый → красный с дизерингом 4×4, дым серый, искры с короткими хвостами, у `boom-kill` ударная волна в первых кадрах.

Проверено на чистом `d7895f73`: оба генератора воспроизводят файлы `Assets/skin/assets/units` и `Assets/skin/assets/fx` попиксельно.
