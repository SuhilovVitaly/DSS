# Прототип

`tabletop-prototype.patch` накладывается на `main` @ `d7895f73` командой `git apply --binary` и даёт клиент, которым сделаны эталонные кадры (итерация 16). Внутри:
- скин прототипа `panzer-general` (поле, панели, листы детального сета `pxd-*`, торпеды, эффекты в `Images/Battle` и `Skins/panzer-general/units`);
- `SkinShipSprites`, `BattleMapRenderer.Templates.cs`, `BattleMapRenderer.FireTemplates.cs`, `BattleMapRenderer.PaintedClouds.cs`;
- правки камеры и рендера карты;
- запекатели (`ZzSpriteBake.*`, включая `ZzDetailBake.cs`) и тест-снимок `ZzPixelSalvoCapture`.

Пересоздать эталонные кадры (три фазы и кадры залпа по 40 ms):

```
DSS_UNITS_DIR=<папка с pxd-*.png и fx-строками> DSS_SHOT_DIR=<папка> dotnet test tests/DeepSpaceSaga.Client.Tests -p:TreatWarningsAsErrors=false --filter FullyQualifiedName~ZzPixelSalvoCapture
```

`DSS_UNITS_DIR` — папка, где рядом лежат `pxd-*.png` и `boom-hit.png`, `boom-kill.png`, `puff.png`, `trail.png` (например, `Assets/skin/assets/units` и `Assets/skin/assets/fx`, скопированные в одну папку). Флаг `-p:TreatWarningsAsErrors=false` нужен из-за предупреждения CS1998 в `GameSessionNavigationTests.cs` на этой базе.

`src/` — те же файлы отдельно, для чтения без наложения патча:

| Файл | Что делает |
|---|---|
| `SkinShipSprites.cs` | Выбор листа и кадра по курсу, привязка к пиксельной сетке, тень, «призрак»; `DrawTorpedo`, `DrawEffect`, `EffectFrames` |
| `BattleMapRenderer.Templates.cs` | Ленты движения, зона досягаемости, скобки, подсказка, плашки юнитов. Там же `DrawScout` — окно «Разведка», вне эпика (D-08) |
| `BattleMapRenderer.FireTemplates.cs` | Сектор, ленты выстрелов и угроз, значки с раскладкой поверх лент, прицелы; полёт торпеды со шлейфом, взрывы (`PendingEffects`), итоги залпа |
| `BattleMapRenderer.PaintedClouds.cs` | Облака жара и пыли |
| `hooks.diff` | Точки подключения в `BattleMapRenderer.cs`, `.Movement.cs`, `.Fire.cs` (в т. ч. взрывы в `DrawResultHighlights` и отлетающие цифры в режиме), `TacticalBattleCamera.cs`, `TacticalBattleScreen.cs` |
| `ZzPixelSalvoCapture.cs` | Тест-снимок трёх фаз и кадров залпа в скине `panzer-general` |

Это справочный код дизайн-итерации, а не готовая реализация. Что переделать — в [../README.md](../README.md#что-брать-из-прототипа).
