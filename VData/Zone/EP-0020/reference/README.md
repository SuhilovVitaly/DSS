# EP-0020 — эталонные материалы

Получены 2026-11-08 на `main` @ `8c6f4e2a` (Linux, .NET SDK 8.0.425, SkiaSharp 2.88.8) макетом EP-0019, расширенным уклонением. Значение каждого кадра — [UiSpec §7](../UiSpec.md#7-эталонные-кадры).

| Путь | Что это |
|---|---|
| `screens/*.png` | Кадры 1920×1080 итерации 09, только новые и изменённые относительно EP-0019; остальные — `../../EP-0019-tactical-battle-ui-redesign/Reference/screens/` |
| `mock-renderer/` | Полный код макета EP-0020 (`battlemock.csproj.txt` → `battlemock.csproj`); `Calib.cs` — предварительная калибровка; `ep0019-to-ep0020-*.diff` — изменения относительно EP-0019 |
| `calibration/` | Результат предварительной калибровки; сюда же US-0003 кладёт `calibration.csv` |

Шрифты — `../../EP-0019-tactical-battle-ui-redesign/Reference/fonts/`.

## SHA-256

```
da1d34ac6d88f7bf70686d5d69a36ae2efe92f8e2812529e8eb1dcfa48dfc3d9  screens/2-fire.png
c8160c0c471884fe505985aa9715f5cf84e925376dc30f90d051027bda44d773  screens/2-fire-hover-own.png
1b39dd6542767127cbfdc556b0f88d10b901b3c789623a5b46be317a7c39fa70  screens/2-fire-evade.png
4a50d743862e532073a2b921f3f7c65b2447d15e38f14d186d10ea4e0ce6b411  screens/2-fire-no-target.png
d0f7f58ed36ba83eb71a2273864c5d30d0d5624cf25f7b604a3cd5efacaee980  screens/2-fire-low-mind.png
c145cf01b54086c0b518cea0e1de995fb5338ef25ef3068078e73812a24d3aab  screens/4-salvo-results-evade.png
```

## Как пересобрать

Как для EP-0019 ([его README](../../EP-0019-tactical-battle-ui-redesign/Reference/README.md#как-пересобрать-эталон)), с кодом из `mock-renderer/` этого эпика и `MOCK_FONT_DIR` на шрифты EP-0019. Кадры уклонения строятся в `RenderAll` на клиентском состоянии: `_evading` (план уклонения или раскрытие), `_mind` (сознание). После реализации US-0002 макет должен брать штраф из `Rules.Evasion`, если константа `EvadeChance` расходится с данными (US-0004 TK-0004).

Предварительная калибровка: `dotnet run -c Release -- <корень DSS> calib <число сидов>`.
