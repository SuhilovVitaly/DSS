---
epic: EP-0024-battle-field-redesign
story: EP-0024-US-0007-salvo-results
ticket: EP-0024-US-0007-TK-0002-torpedo-fx
title: Пиксельная торпеда, шлейф и анимация взрыва
stage: draft
layer: client-ui
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0024-US-0007-TK-0001-salvo, EP-0024-US-0002-TK-0001-sprite-renderer]
files_touched: 4
serves: [US7-AC1, US7-AC2, US7-AC3]
created: 2026-11-12T16:00:00Z
revision: 1
---

# Пиксельная торпеда, шлейф и анимация взрыва

## Why

Пользователь попросил, чтобы в фазе результатов торпеда летела, а взрыв был анимирован в том же пиксельном стиле, что и детальные модели (D-10). Смысл D-06 сохраняется: торпеда летит и взрывается, цифра урона или «промах» отлетает и исчезает.

## Decisions

Применяются [D-01–D-11](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) §1.4, §2.1, §2.1a, §7.1, §7.4; всё, что там не названо, остаётся как в Classic на базе. Материалы — [Materials](../../Materials/README.md): листы `assets/units/pxd-torpedo-*.png`, строки `assets/fx/*.png`, эталоны `Reference/salvo`. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-04, A-09. Эффекты рисуются тем же множителем пикселя и с той же привязкой к сетке, что модели (UiSpec §2.3). Тайминги полёта и удара — существующие (`TacticalShotFlight.Progress`, `ImpactAgeMs`); длительности эффектов задаются метрикой `frame-ms` и на порядок и тайминг залпа не влияют.

## Code context

Строки — `main` @ `d7895f73`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalShipSprites.cs` | отрисовка модели (US-0002-TK-0001) | `DrawTorpedo(canvas, point, directionDeg, own)` по листу `field.unit.sprite.torpedo.<side>`; `DrawEffect(canvas, name, frame, point, alpha)` по `EffectStrip` (US-0001-TK-0002); прототип — `SkinShipSprites.DrawTorpedo`, `DrawEffect`, `EffectFrames`. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.FireTemplates.cs` | ленты, значки, итог залпа (US-0006, TK-0001) | Полёт: пунктир курса, шлейф `trail`, спрайт торпеды (UiSpec §7.1). Удар: выбор эффекта и кадра по `ImpactAgeMs`, отложенная отрисовка поверх моделей. Маркеры цели §7.4 — после окончания взрыва. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.Fire.cs` | :64-72 полёт, :131-155 отлетающие цифры, :214-226 `DrawResultHighlights` | В режиме: прежний полёт не рисовать (его заменяет пиксельный); `DrawFloatingShotResults` вызывать и в режиме; в `DrawResultHighlights` — отрисовать отложенные взрывы вместо колец-вспышек и колец поглощения. Classic без изменений. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/TabletopSalvoTests.cs` | тесты итога (TK-0001) | Пробы UiSpec §10.4, §10.7; кадр взрыва по времени (0, 100, 450, 880, 1000 ms); пропуск анимации без взрывов; Classic без пиксельной торпеды. |

## Public API after the change

`internal static bool TacticalShipSprites.DrawTorpedo(SKCanvas c, SKPoint p, float directionDeg, bool own)`; `internal static bool TacticalShipSprites.DrawEffect(SKCanvas c, string name, int frame, SKPoint p, byte alpha = 255)`; `internal static int TacticalShipSprites.EffectFrames(string name)`.

## Implementation steps

1. Отрисовка торпеды и кадра эффекта по ролям скина (UiSpec §2.1, §2.1a, §2.3), запасной вариант без листа — круг r 4 `#FFF2A0`.
2. Полёт в режиме: пунктир курса, 9 клубов шлейфа, спрайт по направлению; `Layout.Rects["projectile"]`.
3. Удар в режиме: эффект по исходу (`boom-kill`, `boom-hit`, `puff`), точка (`Shot.To` или точка поглощения), кадр `floor(ImpactAgeMs / frame-ms)`; отрисовка отложена до `DrawResultHighlights`, поверх моделей и плашек.
4. Маркеры цели §7.4 — когда взрывы выстрелов по этой цели закончились; при `PlaybackSkipped` — сразу, без взрывов.
5. `DrawFloatingShotResults` — и в режиме; прежние кольца-вспышки и кольца поглощения в режиме не рисуются.
6. Тесты на `3-salvo-flight`, `4-salvo-results`, `4-decoy-results`, `4-anomaly-absorbed`, сверка с `Materials/Reference/salvo/kill-sequence.png` на глаз.

## Out of scope

Ленты и значки итога (TK-0001). Порядок, число и тайминг выстрелов. Правка листов и эффектов (US-0002-TK-0003).

## Invariants

В Classic, Contrast и Xenon Star полёт, вспышки и цифры прежние. Данные выстрелов — из `TacticalSalvoFrame` и проекции результатов.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `TabletopSalvoTests`
- `ResultsPhaseTests`
- `Ep0019ViewTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~TabletopSalvoTests|FullyQualifiedName~ResultsPhaseTests|Category=Ep0019View" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0024/<run>/us7-tk2" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalShipSprites.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.FireTemplates.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.Fire.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US7-AC1, US7-AC2, US7-AC3 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Вид и тайминги — UiSpec §7.1, §7.4; роли и размеры листов — §2.1, §2.1a; файлы — `Materials/Assets/skin`; готовая реализация для справки — `Materials/Prototype/src/BattleMapRenderer.FireTemplates.cs`, `SkinShipSprites.cs`, `hooks.diff`.

## Исполнение

Не начато.
