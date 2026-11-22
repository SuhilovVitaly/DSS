---
epic: EP-0043-demo-ambush-prologue
story: EP-0043-US-0002-ambush-opening
title: Доклад о засаде на карте
stage: draft
dependencies: [EP-0043-US-0001-dialogue-context]
created: 2026-11-20T10:58:51Z
source_request: "D-01 и D-02. Новый старт демо предваряет существующее столкновение."
current_review: complete
revision: 1
---

# Доклад о засаде на карте

## Входное техническое задание

D-01 и D-02. Новый старт демо предваряет существующее столкновение. [Решения эпика](../Documentation.md#решения-пользователя).

## User story

Как игрок, при запуске новой демо-игры я сначала вижу свой корабль на карте Солнечной системы и слышу доклад инженера о пропаже сигнала маяка. По кнопке «Далее» читаю доклад оператора сканера о сигнатурах двигателей и засаде. Когда я нажимаю «К бою», начинается подготовленный первый бой.

## Acceptance criteria

- **AC-01.** В новой демо-игре карта видна за диалогом. Инженер докладывает капитану о пропавшем сигнале маяка по курсу. Игровое время стоит во время чтения.
- **AC-02.** По «Далее» оператор сообщает о сигнатурах корабельных двигателей и засаде. Бой начинается один раз по «К бою» с исходными параметрами initial и seed 1 и собственным крылом.
- **AC-03.** Повторное нажатие, ошибка перехода и Continue не дублируют старт. Обычный профиль и сохранения без состояния нового пролога сохраняют прежний путь.

## Completion evidence

Игровой сценарий с наблюдаемыми результатами AC-01…AC-03 и именованные проверки из тикетов.

## Non-goals

Новая тактика, баланс, постановка боя и изменение обычного профиля.

## Dependencies

- [EP-0043-US-0001-dialogue-context](../EP-0043-US-0001-dialogue-context/EP-0043-US-0001-dialogue-context.md)

## Invariants

Ровно один запуск боя на принятую команду.

## Approved ticket map

| Тикет | Результат | Слой | Зависимости | Критерии |
|---|---|---|---|---|
| [EP-0043-US-0002-TK-0001-opening-phase-contract](EP-0043-US-0002-TK-0001-opening-phase-contract/EP-0043-US-0002-TK-0001-opening-phase-contract.md) | Фаза пролога для запуска и Continue | contracts | EP-0043-US-0001-TK-0001-dialogue-contract | AC-02, AC-03 |
| [EP-0043-US-0002-TK-0002-opening-dialogue-data](EP-0043-US-0002-TK-0002-opening-dialogue-data/EP-0043-US-0002-TK-0002-opening-dialogue-data.md) | Два доклада в контенте демо | content-data | EP-0043-US-0001-TK-0001-dialogue-contract | AC-01, AC-02 |
| [EP-0043-US-0002-TK-0003-opening-state](EP-0043-US-0002-TK-0003-opening-state/EP-0043-US-0002-TK-0003-opening-state.md) | Состояние пролога и разрешение на бой | engine | EP-0043-US-0002-TK-0002-opening-dialogue-data, EP-0043-US-0001-TK-0002-speaker-projection, EP-0043-US-0002-TK-0001-opening-phase-contract | AC-01, AC-02, AC-03 |
| [EP-0043-US-0002-TK-0004-opening-checkpoint](EP-0043-US-0002-TK-0004-opening-checkpoint/EP-0043-US-0002-TK-0004-opening-checkpoint.md) | Восстановление начатого пролога | local-client | EP-0043-US-0002-TK-0003-opening-state | AC-03 |
| [EP-0043-US-0002-TK-0005-opening-presentation](EP-0043-US-0002-TK-0005-opening-presentation/EP-0043-US-0002-TK-0005-opening-presentation.md) | Карта и кнопка запуска боя | client | EP-0043-US-0002-TK-0003-opening-state, EP-0043-US-0001-TK-0003-dialogue-layout, EP-0043-US-0002-TK-0004-opening-checkpoint | AC-01, AC-02, AC-03 |

## Decisions and review log

D-01 и D-02. Продуктовые ответы и время их записи приведены в эпике. A-01 содержат технические предположения. Декомпозиция подготовлена по workflow без отдельного пользовательского согласования файлов.

## Gaps and backlog

Контрольные запуски и ручная приёмка входят в критерии соответствующих тикетов.
