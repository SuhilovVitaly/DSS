---
epic: EP-0043-demo-ambush-prologue
story: EP-0043-US-0004-prisoner-interrogation
title: Допрос и отметка аванпоста
stage: draft
dependencies: [EP-0043-US-0001-dialogue-context, EP-0043-US-0003-mandatory-capture]
created: 2026-11-20T10:58:51Z
source_request: "D-03 и D-05. Захваченный пират раскрывает происхождение маяка и судьбу пилотов."
current_review: complete
revision: 1
---

# Допрос и отметка аванпоста

## Входное техническое задание

D-03 и D-05. Захваченный пират раскрывает происхождение маяка и судьбу пилотов. [Решения эпика](../Documentation.md#решения-пользователя).

## User story

Как капитан, после завершения окна трофеев я допрашиваю захваченного пирата. Узнаю, что маяк сняли с корабля, сканировавшего аномалии, и использовали для засады. Я спрашиваю о пилотах, и пленный предлагает показать на карте аванпост, где их держат.

## Acceptance criteria

- **AC-01.** Допрос появляется после успешного завершения результатов. В кадре тот пленный, которого игра зафиксировала при аресте, с его именем и портретом.
- **AC-02.** Реплики идут в согласованном порядке. Знание об аванпосте и цель поиска пилотов появляются только после реплики пленного о месте их заключения.
- **AC-03.** После Continue сцена восстанавливает прогресс с фактическим пленным. Повтор подтверждения не добавляет цель и отметку дважды.

## Completion evidence

Игровой сценарий с наблюдаемыми результатами AC-01…AC-03 и именованные проверки из тикетов.

## Non-goals

Общие механики допроса и взаимодействия с пленными.

## Dependencies

- [EP-0043-US-0001-dialogue-context](../EP-0043-US-0001-dialogue-context/EP-0043-US-0001-dialogue-context.md)
- [EP-0043-US-0003-mandatory-capture](../EP-0043-US-0003-mandatory-capture/EP-0043-US-0003-mandatory-capture.md)

## Invariants

Отметка аванпоста появляется только после раскрытия игроку.

## Approved ticket map

| Тикет | Результат | Слой | Зависимости | Критерии |
|---|---|---|---|---|
| [EP-0043-US-0004-TK-0001-interrogation-data](EP-0043-US-0004-TK-0001-interrogation-data/EP-0043-US-0004-TK-0001-interrogation-data.md) | Последовательность реплик допроса | content-data | EP-0043-US-0003-TK-0003-capture-exit-gate | AC-01, AC-02 |
| [EP-0043-US-0004-TK-0002-interrogation-state](EP-0043-US-0004-TK-0002-interrogation-state/EP-0043-US-0004-TK-0002-interrogation-state.md) | Привязка допроса и открытие цели | engine | EP-0043-US-0004-TK-0001-interrogation-data, EP-0043-US-0003-TK-0003-capture-exit-gate, EP-0043-US-0002-TK-0004-opening-checkpoint | AC-01, AC-02, AC-03 |
| [EP-0043-US-0004-TK-0003-interrogation-text](EP-0043-US-0004-TK-0003-interrogation-text/EP-0043-US-0004-TK-0003-interrogation-text.md) | Текст допроса и подписи в RU/EN | client | EP-0043-US-0004-TK-0002-interrogation-state, EP-0043-US-0001-TK-0003-dialogue-layout | AC-01, AC-02 |

## Decisions and review log

D-03 и D-05. Продуктовые ответы и время их записи приведены в эпике. A-02 и A-05 содержат технические предположения. Декомпозиция подготовлена по workflow без отдельного пользовательского согласования файлов.

## Gaps and backlog

Сведение с дальнейшей цепочкой EP-0039 учитывается в границах эпика.
