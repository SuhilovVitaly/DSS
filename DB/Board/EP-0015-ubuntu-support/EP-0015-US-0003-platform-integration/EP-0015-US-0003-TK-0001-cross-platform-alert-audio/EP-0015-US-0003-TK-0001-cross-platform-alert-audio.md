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
