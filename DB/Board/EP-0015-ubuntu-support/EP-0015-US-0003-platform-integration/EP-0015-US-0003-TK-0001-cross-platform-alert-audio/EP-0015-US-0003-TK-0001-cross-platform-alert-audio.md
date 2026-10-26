---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0003-platform-integration
ticket: EP-0015-US-0003-TK-0001-cross-platform-alert-audio
title: "Кроссплатформенный звук тревоги"
stage: draft
acceptance_status: open
execution_status: implemented
layer: client
depends_on: []
serves: [AC-01, AC-02]
created: 2026-10-25
---

# Кроссплатформенный звук тревоги

> Текущий сводный результат, checkpoints и открытые gates: [FinalValidation](../../FinalValidation.md); публикация: [PublicationStatus](../../PublicationStatus.md). Записи отдельных прогонов ниже — история evidence, а не обещание текущего PASS.

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

Self-review boundary fix: exactly22050Hz equals Nyquist at44100Hz and a sampled sine would be silent. Switch to96000Hz at22050Hz inclusive; add exact22049/22050 boundary cases. Earlier prose saying through22050 is superseded by this correction; required440/100 sample fixture unchanged.

Nyquist follow-up verified: D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/20261026-audio-nyquist/nyquist.trx6/6, including exact22049/22050 boundaries and native OpenAL Soft initialization; C#12 build zero diagnostics. Change is one threshold and two theory rows; scoped formatting style unchanged.

Linux follow-up40290044041: full Client corpus passed after Unix write-failure correction; 8/8 native layouts now pass with graphics dependencies. Explicit OpenAL-null check failed loading the library: default AL/ALContext.GetApi(false) searches system OpenAL names, whereas the package ships Soft native names. XML API documentation exposes GetApi(bool soft); select soft:true in both APIs to use the bundled library instead of relying on a system install. Evidence in D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/20261026-linux-native-deps/.

Bundled-loader Windows verification: D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/20261026-audio-bundled/bundled-null.trx6/6, bundled-unavailable.trx1/1; C#12 build zero diagnostics. Both AL and ALC explicitly prefer Soft native library names. Final Linux repeat pending CI.

Final orchestration review: the separate CI audio subprocess step lacked the300s outer bound used by the baseline runner (the test itself waits15s). Extend workflow scope to set timeout-minutes:5 on this step; both audio processes now share the cap and Actions terminates the step on expiry. No test or audio behavior changes; validate workflow YAML and exact timeout value. Portable asset validation also receives a5-minute step cap for its build/tool subprocesses.

CI40293853513 still fails native initialization on Ubuntu24 after soft:true: audio-interface.log reports library lookup failure. Earlier inference that preference alone resolves packaged names was insufficient; both names already occur in the default search list. Silk2.21 DefaultPathResolver uses legacy platform/dependency RID fallback and does not find the portable native asset in this framework-dependent test layout. Replace implicit name search with DefaultNativeContext for the absolute bundled path: root library in RID publish, otherwise runtimes/<portable OS-process architecture>/native. AL/ALC core1.1 entry points remain exported by the same Soft binary; both contexts retain worker-thread ownership. Windows native smoke must exercise this same explicit-context path. Evidence20261026-linux-audio-diagnosis/extracted/audio-interface.log. Official exact-version sources: https://github.com/dotnet/Silk.NET/blob/ff35bea3441a6cef8316af6d43f65967946e7ea5/src/Core/Silk.NET.Core/Loader/DefaultPathResolver.cs and src/OpenAL/Silk.NET.OpenAL/ALC/ALContext.cs. Scope adds current Ubuntu/audio documentation and CI ldd diagnostics for the actual bundled library.

Absolute-context Windows checks pass:20261026-audio-absolute/absolute-null.trx6/6; absolute-unavailable.trx1/1; C#12 build0 warnings/errors and scoped format0. This exercises real AL/ALC construction from the packaged native path, not a fake native API. Linux repeat remains pending; the previous preference-only failure is retained rather than relabelled PASS.

Extend the explicit available-device native test to create an independent context and actually play/stop20ms PCM through core AL exports. The dedicated null-driver subprocess has no active game session; do not play in ordinary/full-suite or unavailable-device mode. This verifies that replacing GetApi with a direct context still supports the production playback calls, beyond buffer preparation.

20261026-audio-playback/native-playback.trx passes6/6 with actual native source play/stop; unavailable.trx1/1. C#12 build0 diagnostics, scoped format0. No physical audibility claim.
