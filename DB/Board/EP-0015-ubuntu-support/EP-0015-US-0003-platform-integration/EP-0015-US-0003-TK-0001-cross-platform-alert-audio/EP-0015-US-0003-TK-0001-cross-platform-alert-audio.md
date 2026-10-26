---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0003-platform-integration
ticket: EP-0015-US-0003-TK-0001-cross-platform-alert-audio
title: "Кроссплатформенный звук тревоги"
stage: draft
layer: client
depends_on: []
serves: [AC-01, AC-02]
created: 2026-10-25
---

# Кроссплатформенный звук тревоги

История: [EP-0015-US-0003-platform-integration](../EP-0015-US-0003-platform-integration.md) · Эпик: [EP-0015](../../Documentation.md)

## Зачем

Сейчас `CombatAlertAudio.cs:58` играет тон через `Console.Beep`, который работает только на Windows; на Linux звук отключается с сообщением «Native tone playback requires Windows».

## Шаги

1. Добавить пакет `Silk.NET.OpenAL` 2.21.0 (та же версия, что у остальных пакетов Silk.NET) и `Silk.NET.OpenAL.Soft.Native` для нативной библиотеки OpenAL Soft.
2. Генерировать тон программно: синус 16-бит моно 44100 Гц, длительность из `cue.DurationMs`, огибающая 5 мс на вход и выход, чтобы не было щелчков.
3. Буферы готовить заранее для всех сигналов из конфигурации, воспроизводить без выделения памяти в цикле отрисовки.
4. Сохранить ограничения частоты 37–32767 Гц и длительности 1–250 мс; на Windows тоже перейти на OpenAL, чтобы звук был одинаковым на обеих ОС.
5. Нет устройства или ошибка инициализации → `ReportUnavailable(причина)` один раз, дальше молча.

## Зависимости

Нет.

## Definition of Done

- Сигналы слышны на Ubuntu (PulseAudio/PipeWire) и Windows.
- Тест: генератор тона для 440 Гц / 100 мс выдаёт 4410 сэмплов, первый и последний сэмпл равны нулю.
- Запуск без звукового устройства (например, в контейнере) не роняет клиент.
- Проверено на Windows и Ubuntu; на Windows поведение не ухудшилось.

## Code context and implementation scope

Directory.Packages.props; src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj; UI/CombatAlertAudio.cs; new UI/CombatTone.cs and UI/OpenAlTonePlayer.cs; tests/DeepSpaceSaga.Client.Tests/CombatToneTests.cs. Preserve session epochs, throttling, and one-time unavailable reporting. Worker thread owns the OpenAL context; configured buffers are prepared before queue playback, outside rendering. Nyquist constraint: use 44100 Hz through 22050 Hz tones, 96000 Hz for higher supported frequencies; the required 440/100 fixture remains 4410 samples. Native heard-by-human acceptance remains separately reported. Official binding example: https://github.com/dotnet/Silk.NET/blob/main/examples/CSharp/OpenAL%20Demos/WavePlayer/Program.cs .

## Implementation and validation checkpoint

OpenAL binding 2.21.0, native Soft package 1.23.1 (native package has its own version series, verified against NuGet). Added bounded audio queue (128, DropOldest), dedicated context-owning background thread, cached PCM buffers, epoch cancellation during tones/gaps, and disabled-device draining with one warning. CI workflow scope extended to exercise Linux null-device and unavailable-driver subprocesses.
Evidence D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/20261026-audio/: build.log C#12 zero warnings/errors; audio.trx 28/28 existing timing/epochs plus PCM; native-null.trx 4/4 real native context+buffer initialization; native-no-device.trx 1/1 non-crashing unavailable path; format.log scoped formatter exit0. Human audible Windows/PulseAudio/PipeWire acceptance NOT RUN. Linux native CI pending.
