---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0003-platform-integration
title: "Звук, открытие папок и скрипты на Linux"
stage: draft
dependencies: [EP-0015-US-0002]
---

# Звук, открытие папок и скрипты на Linux

Эпик: [EP-0015 — Поддержка Ubuntu](../Documentation.md)

## User story

Как игрок на Ubuntu, я слышу боевые сигналы тревоги, а кнопка «открыть папку» в панели отладки открывает файловый менеджер. Как разработчик, я могу запускать служебные скрипты репозитория.

## Зависимости

EP-0015-US-0002

## Критерии приёмки

- **AC-01.** Сигналы тревоги в бою звучат на Ubuntu и на Windows с теми же частотой и длительностью, что задаёт `CombatAlertAudio` (частота 37–32767 Гц, длительность 1–250 мс).
- **AC-02.** Если звуковое устройство недоступно, клиент не падает, а один раз пишет причину в лог (поведение `ReportUnavailable` сохраняется).
- **AC-03.** Кнопка открытия папки в панели отладки на Ubuntu открывает каталог через `xdg-open`; при его отсутствии пользователь видит сообщение с путём к каталогу.
- **AC-04.** Скрипты `tools/Export-TacticalButtonAssets.ps1` и `.../character-generator/scripts/prepare.ps1` запускаются через PowerShell 7 (`pwsh`) на Ubuntu, либо их назначение и способ запуска описаны в документации.

## Тикеты

- [EP-0015-US-0003-TK-0001-cross-platform-alert-audio](EP-0015-US-0003-TK-0001-cross-platform-alert-audio/EP-0015-US-0003-TK-0001-cross-platform-alert-audio.md) — Кроссплатформенный звук тревоги
- [EP-0015-US-0003-TK-0002-open-folder-fallback](EP-0015-US-0003-TK-0002-open-folder-fallback/EP-0015-US-0003-TK-0002-open-folder-fallback.md) — Открытие папки на Linux
- [EP-0015-US-0003-TK-0003-powershell-scripts](EP-0015-US-0003-TK-0003-powershell-scripts/EP-0015-US-0003-TK-0003-powershell-scripts.md) — Служебные скрипты PowerShell
