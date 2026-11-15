---
epic: EP-maintenance
story: EP-maintenance-US-0015-tactical-battle-feedback
ticket: EP-maintenance-US-0015-TK-0006-explain-rear-hits
title: Попадание в корму
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC6]
---

# Попадание в корму

[User story](../EP-maintenance-US-0015-tactical-battle-feedback.md)

## Критерий

Результат кормового попадания отмечает обход брони. Прямое попадание и кормовое добивание используют существующие боевые правила.

## Проверки

TabletopSalvoTests, ResultsPhaseTests.

## Исполнение

На скриншоте Таг-1 имеет 50 брони и 0 корпуса. Снимок не определяет причину конкретного попадания. TacticalBattleSalvo направляет кормовой урон напрямую в корпус, а успешный RearKill снимает весь оставшийся корпус. Клиент показывает фактически нанесённый урон и пояснение о кормовом попадании.
