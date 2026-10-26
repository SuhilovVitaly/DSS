---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0002-client-rendering
ticket: EP-0015-US-0002-TK-0001-skiasharp-linux-native
title: "Нативная библиотека SkiaSharp для Linux"
stage: draft
execution_status: implemented-local-validation
layer: client
depends_on: [EP-0015-US-0001-TK-0002-test-portability-fixes]
serves: [AC-01]
created: 2026-10-25
---

# Нативная библиотека SkiaSharp для Linux

История: [EP-0015-US-0002-client-rendering](../EP-0015-US-0002-client-rendering.md) · Эпик: [EP-0015](../../Documentation.md)

## Зачем

Сейчас клиент ссылается только на пакет `SkiaSharp` 2.88.8, в котором есть нативная библиотека для Windows и macOS, но нет `libSkiaSharp.so` для Linux. На Ubuntu первая отрисовка падает.

## Шаги

1. В `Directory.Packages.props` добавить `<PackageVersion Include="SkiaSharp.NativeAssets.Linux" Version="2.88.8" />` — версия строго равна версии `SkiaSharp`.
2. В `src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj` добавить `<PackageReference Include="SkiaSharp.NativeAssets.Linux" />`.
3. Вариант `.NoDependencies` не использовать: он требует системный fontconfig и ломает загрузку шрифтов на минимальных образах.
4. Проверить, что `bin/.../runtimes/linux-x64/native/libSkiaSharp.so` появляется после сборки и после `dotnet publish -r linux-x64`.

## Зависимости

Нет.

## Definition of Done

- Клиент на Ubuntu открывает главное меню.
- Сборка на Windows не меняется: в выводе остаётся `runtimes/win-x64/native/libSkiaSharp.dll`, клиент запускается.
- Проверено на Windows и Ubuntu; на Windows поведение не ухудшилось.

## Code context and assumptions

Initial allowlist: Directory.Packages.props (SkiaSharp2.88.8), src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj (PackageReference), tests/DeepSpaceSaga.Client.Tests/SkiaNativeSmokeTests.cs (реальная raster native smoke). Добавить обычный SkiaSharp.NativeAssets.Linux2.88.8 как требует исходный scope. В исходном шаге3 описание NoDependencies/fontconfig было обратным: обычный Linux пакет использует fontconfig; NoDependencies исключает эту зависимость. Это не меняет выбор обычного пакета; libfontconfig1 нужно описать в зависимости Ubuntu. Источник: официальные NuGet package pages.

Проверки: restore/build, фактическое наличие linux-x64 native lib в NuGet/runtime assets, реальное raster рисование черезSKSurface на обеихОС, scoped format/diff. Raster не является доказательством desktop/GPU окна, UI-input или раскладки; эти gates остаются уTK-0003.

## Execution result 2026-10-26

Добавлены CPM version и Client reference SkiaSharp.NativeAssets.Linux2.88.8, строго как SkiaSharp. Restore PASS; build through Client.Tests PASS, новый реальный SKSurface/SKBitmap pixel smoke1/1 наWindows. В Release output присутствуют linux-x64/native/libSkiaSharp.so и win-x64/native/libSkiaSharp.dll; paths/sizes/SHA256 сохранены в native-assets.json. Native publish проверяется вUS-0004-TK-0002. Первый scoped format выявил толькоLF новогоC# файла; файл приведён кCRLF, повторный verifyPASS. Diff checkPASS, self-review: Windows package не заменён, версии согласованы, NoDependencies не добавлен.

Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/20261026-skia-native/ (restore.log, skia.log/skia.trx, native-assets.json, format-final.log). Все command processes capped300с. Linux smoke подтверждается CI послеpush; desktop mainmenu/X11/XWayland и human/GPU gates здесьNOT RUN и не подменяютсяraster.

Источники пакетов: https://www.nuget.org/packages/SkiaSharp.NativeAssets.Linux/2.88.8 ; https://www.nuget.org/packages/SkiaSharp.NativeAssets.Linux.NoDependencies/2.88.8 .
