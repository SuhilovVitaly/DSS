---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0001-headless-build-and-tests
ticket: EP-0015-US-0001-TK-0004-sdk8-client-compilation
title: "Совместимость вызовов SkiaSharp с компилятором SDK8"
stage: draft
acceptance_status: open
execution_status: implemented
layer: client
depends_on: [EP-0015-US-0001-TK-0001-linux-build-baseline]
serves: [AC-01]
files_touched: 2
created: 2026-10-26
---

# Совместимость вызовов SkiaSharp с компилятором SDK8

> Текущий сводный результат, checkpoints и открытые gates: [FinalValidation](../../FinalValidation.md); публикация: [PublicationStatus](../../PublicationStatus.md). Записи отдельных прогонов ниже — история evidence, а не обещание текущего PASS.

## Why

SDK8.0.427 baseline на Ubuntu24.04 и Windows2022 воспроизводит CS0121 в двух gradient calls. C#12 не выбирает между SKColor[]/float[] и SKColorF[]/SKColorSpace для collection expression + null. SDK10 baseline проходит. Нужен отдельный production Client merge unit перед исправлением тестового fixture.

## Code context

| File | Evidence | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/UI/Screens/GameSession/TacticalMapDepthRenderer.cs | DrawEngineFlame, L420 | Явный SKColor[] для прежней пары цветов |
| src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs | trail gradient, L1867 | Явный SKColor[] для прежней пары цветов |

## Implementation steps

Уточнить тип массива цветов без изменения координат, цветов, stops и tile mode. Проверить owning Client build с LangVersion12.0, targeted renderer/trail tests, scoped formatter и diff. CI SDK8 должен убрать именно эти две ошибки; оставшиеся nullable fixture ошибки принадлежат TK-0002.

## Invariants and public API

No API change. Рендер и результаты Engine не меняются. Не повышать SDK requirement и не отключать TreatWarningsAsErrors.

## Definition of Done

Две CS0121 исчезли на исходном SDK8 пути; C#12 owning build и регрессия прошли. Self-review, отдельный commit/push и remote verification записаны. Native визуальная приёмка не заявляется для эквивалентной подсказки типа.

## Execution result 2026-10-26

Два gradient вызова получили явный SKColor[]; цвета, позиции, null stops и tile mode сохранены. C#12 Client build:0 warnings/errors; TacticalMapDepthRendererTests + GameSessionScreenTrailColorTests:20/20; scoped whitespace format и git diff --check PASS. Self-review: diff только2 эквивалентных подсказки типа. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/20261026-sdk8-client/ (client-csharp12-build.log, renderers.log, renderers.trx, format.log).

Команды: dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --no-restore -p:LangVersion=12.0; dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj -c Release --no-restore -p:LangVersion=12.0 --filter FullyQualifiedName~TacticalMapDepthRendererTests|FullyQualifiedName~GameSessionScreenTrailColorTests --logger trx;LogFileName=renderers.trx --results-directory <указанный абсолютный evidence каталог>; dotnet format whitespace DeepSpaceSaga.sln --no-restore --verify-no-changes --include <оба изменённых файла>. Все процессы capped300с. SDK8 CI confirmation следует после push; полная solution пока блокируется fixture изTK-0002. Native layout не заявляется.
