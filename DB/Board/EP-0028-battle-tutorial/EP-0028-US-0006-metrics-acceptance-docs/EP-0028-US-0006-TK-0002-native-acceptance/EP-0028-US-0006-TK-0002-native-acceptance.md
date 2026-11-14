---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0006-metrics-acceptance-docs
ticket: EP-0028-US-0006-TK-0002-native-acceptance
title: Нативная приёмка
stage: approved
layer: verification
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0028-US-0006-TK-0001-step-metrics, EP-0028-US-0005-TK-0002-winnable-battle]
implementation_files: 0
files_touched: []
serves: [US-0006/AC-02]
created: 2026-11-14T15:04:43Z
revision: 1
---

# Нативная приёмка

[История](../EP-0028-US-0006-metrics-acceptance-docs.md) · [Эпик](../../Documentation.md)

## Цель

Проверить обучение в настоящем окне игры на меньшем мониторе: RU и EN, подсказки вкл/выкл, журнал H, Tabletop, разбор итога; классифицировать G-01. Production-файлы не меняются; найденные дефекты оформляются тикетами-исправлениями этой истории.

## Decisions

D-01…D-06 проверяются визуально; правила запуска — [AGENTS.md](../../../../Documentation/00-Process/AGENTS.md#game-launches): никогда не на самом большом мониторе; `DSS_VALIDATION_SMALL_MONITOR=1`; изолированный профиль.

## Assumptions

- Изолированный профиль — копия выхода сборки в `D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0006-tk0002-<run>/profile/` без сохранений (чтобы «Новая игра» запускала обучение, EP-0027) и с собственным `Settings.json`.
- Если меньший монитор нельзя подтвердить — только headless-проверки, нативная часть помечается `NOT RUN` с причиной, приёмка остаётся открытой.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| — | Production-код не меняется | Только артефакты в `$RUN` и записи Board этого тикета |

## Public API after the change

No API change.

## Implementation steps

Матрица (каждая ячейка — снимок экрана в `$RUN/shots/` и строка отчёта `$RUN/acceptance.md`):

| # | Язык | Подсказки | Проверка |
|---|---|---|---|
| 1 | RU | вкл | «Новая игра» → бой Tabletop; приветствие; панель не закрывает карточки; подсветка карточки |
| 2 | RU | вкл | T1 Move/Fire/Salvo/Results — реплики по каталогу закрываются действиями; при перетаскивании пути офицер молчит |
| 3 | RU | вкл | H открывает журнал, Esc закрывает журнал, затем Esc — подтверждение выхода (EP-0027) |
| 4 | RU | вкл→выкл | «Подсказки: выкл» → значок; `Settings.json` содержит `tutorialHints: false`; «Подсказки: вкл» возвращает |
| 5 | EN | вкл | Те же шаги 1–2: все строки на английском, помещаются в панель |
| 6 | EN | выкл (из Настроек) | Строка Настроек; бой без реплик; журнал пуст до включения |
| 7 | RU | вкл | Победа чистым Enter; разбор `over.victory`; «На карту» → карта |
| 8 | — | — | G-01: нажать E в Fire на дроне с доступным уклонением — фиксация «работает/не работает» |
| 9 | — | — | Файл метрик `*.tutorial.json` в `$RUN/battle-metrics` (`DSS_VALIDATION_ROOT=$RUN`) |

## Acceptance criteria

1. Все ячейки матрицы имеют результат PASS/FAIL/NOT RUN с причиной и ссылкой на снимок.
2. Каждый FAIL оформлен тикетом-исправлением этой истории (или EP-maintenance для G-01) до перехода к TK-0003.
3. Нативная проверка не выполнялась на самом большом мониторе (раскладка мониторов записана в отчёт).

## Verification signal

Детерминированная проверка: отчёт `$RUN/acceptance.md` с полной матрицей и существующими путями снимков; повторный прогон headless-набора `DeepSpaceSaga.Client.Tests.Tutorial` перед нативной частью.

## Tests

```bash
RUN=D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0006-tk0002-<yyyyMMddTHHmmss>
mkdir -p "$RUN"
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~DeepSpaceSaga.Client.Tests.Tutorial|FullyQualifiedName~TutorialFlowTests" --results-directory "$RUN" --logger "trx;LogFileName=pre-native.trx"
# нативный запуск — только с DSS_VALIDATION_SMALL_MONITOR=1 и DSS_VALIDATION_ROOT=$RUN из изолированного профиля
```

После работы — остановить процессы игры, удалить профиль и временные файлы, оставить отчёт и снимки.

## Out of scope

Изменения кода (только через тикеты-исправления), документация (TK-0003).

## Invariants

- Без запуска на самом большом мониторе.
- Без артефактов в репозитории.

## Known fallout

Нет.

## Definition of Done

- Матрица заполнена; FAIL маршрутизированы; G-01 классифицирован.
- Отчёт и итог записаны в «Журнал» тикета; коммит Board-записей и `git push`.

## Self-containment check

Матрица, профиль, переменные окружения и правила запуска заданы.

## Журнал

2026-11-14T15:04:43Z — тикет создан.
