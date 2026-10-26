---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0002-client-rendering
ticket: EP-0015-US-0002-TK-0001-skiasharp-linux-native
title: "Нативная библиотека SkiaSharp для Linux"
stage: draft
layer: client
depends_on: []
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
