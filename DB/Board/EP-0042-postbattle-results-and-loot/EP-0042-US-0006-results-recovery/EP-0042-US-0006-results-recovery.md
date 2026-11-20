---
epic: EP-0042-postbattle-results-and-loot
story: EP-0042-US-0006-results-recovery
title: Сохранение и загрузка окна результатов
stage: draft
dependencies: [EP-0042-US-0001-results-modal, EP-0042-US-0003-timed-loot, EP-0042-US-0004-parallel-bulk, EP-0042-US-0005-safe-exit]
created: 2026-11-20T10:29:12Z
source_request: "Решения D-0042-02, D-0042-08, D-0042-09"
current_review: complete
revision: 1
---

# Сохранение и загрузка окна результатов

## Входное техническое задание

[D-0042-02, D-0042-08, D-0042-09](../Documentation.md#решения-пользователя).

## User story

Я могу сохранить завершённое состояние окна и продолжить с него после загрузки. Во время операций ручные сохранение и загрузка заблокированы. Повтор после ошибки записи не списывает ресурс второй раз, а старое сохранение лута продолжает открываться.

## Acceptance criteria

- **AC-01.** Пока идут операции, F5/F9 и эквиваленты Save/Load недоступны. Диспетчер и слой сохранения отклоняют обходы, autosave не записывает промежуточные операции. После завершения и подтверждения записи доступ возвращается.
- **AC-02.** Загрузка сохраняет факт открытия модального окна, подтверждённый исход и текущие строки, взятые/оставленные трофеи, провалы, аресты и RNG cursor. Возврат не повторяет бой, добычу или settlement.
- **AC-03.** Старое открытое EP-0036 сохранение восстанавливается в новом окне с исходными остатками; закрытое не переоткрывается. Ошибка durable записи позволяет повторить тот же checkpoint/receipt безопасно; неуспех записи не выдаётся за подтверждённое сохранение.

## Non-goals

Изменение underlying правил боя, экономики, формул добычи или ареста без нового решения. Границы общей поставки и допущения — в [эпике](../Documentation.md#технические-допущения-и-границы).

## Dependencies

- [EP-0042-US-0001-results-modal](../EP-0042-US-0001-results-modal/EP-0042-US-0001-results-modal.md)
- [EP-0042-US-0003-timed-loot](../EP-0042-US-0003-timed-loot/EP-0042-US-0003-timed-loot.md)
- [EP-0042-US-0004-parallel-bulk](../EP-0042-US-0004-parallel-bulk/EP-0042-US-0004-parallel-bulk.md)
- [EP-0042-US-0005-safe-exit](../EP-0042-US-0005-safe-exit/EP-0042-US-0005-safe-exit.md)

## Approved ticket map

Карта готова по workflow, человеческое утверждение отдельной детализации не заявлено.

| Тикет | Layer | Зависит от | Покрытие |
|---|---|---|---|
| [EP-0042-US-0006-TK-0001-save-ledger](EP-0042-US-0006-TK-0001-save-ledger/EP-0042-US-0006-TK-0001-save-ledger.md) Снимок завершённых состояний и совместимость | engine | EP-0042-US-0005-TK-0002-capacity-exit-rule, EP-0042-US-0001-TK-0005-outcome-lifecycle | AC-01, AC-02, AC-03 |
| [EP-0042-US-0006-TK-0002-checkpoint-transport](EP-0042-US-0006-TK-0002-checkpoint-transport/EP-0042-US-0006-TK-0002-checkpoint-transport.md) Долговечный checkpoint и повтор записи | local-client | EP-0042-US-0006-TK-0001-save-ledger | AC-01, AC-02, AC-03 |
| [EP-0042-US-0006-TK-0003-load-routing](EP-0042-US-0006-TK-0003-load-routing/EP-0042-US-0006-TK-0003-load-routing.md) Блокировка сохранения и маршрутизация после Load | client | EP-0042-US-0001-TK-0002-mandatory-route, EP-0042-US-0005-TK-0003-footer-confirmation, EP-0042-US-0006-TK-0002-checkpoint-transport | AC-01, AC-02, AC-03 |

## Gaps and backlog

Реализация и приёмка не начаты. Не добавлять новые продуктовые правила за пределами решений эпика.
