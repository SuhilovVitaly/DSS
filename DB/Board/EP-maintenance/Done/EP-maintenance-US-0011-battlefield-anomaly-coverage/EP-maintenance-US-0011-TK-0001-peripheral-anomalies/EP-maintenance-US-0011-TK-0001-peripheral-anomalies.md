---
epic: EP-maintenance
story: EP-maintenance-US-0011-battlefield-anomaly-coverage
ticket: EP-maintenance-US-0011-TK-0001-peripheral-anomalies
title: Аномалии по всему полю боя
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: engine
depends_on: []
files_touched: 3
serves: [AC1]
created: "2026-11-10T14:03:31.830Z"
---

# Аномалии по всему полю боя

## Why

Проверка уточнения: 106 целевых тестов движка и повторные 10 тестов генератора прошли. Логи: D:/DeepSpaceSaga/artifacts/validation/tactical-map-polish/20261110144815363/engine/test.log, D:/DeepSpaceSaga/artifacts/validation/tactical-map-polish/20261110144815363/engine-final/test.log.

Уточнение от 2026-11-10. Детерминированная внешняя сетка покрывает дальние сектора поля, включая углы, за пределами районов развёртывания. Существующий ближний слой сохраняется.
Обновляются соответствующий рендер, ввод или генератор, связанные проверки и затронутые растровые эталоны.

Расширить генерацию за пределы центральной полосы. Сохранить размер и состав сгустков и безопасный отступ от стартовых позиций. Добавить периферийные области с сопоставимым шагом; генерация по seed остаётся детерминированной.

## Decisions

Отдельное исправление по запросу пользователя с коммитом и отправкой в текущую ветку.

## Assumptions

Расширение добавляет боковые области к прежнему центральному поясу и сохраняет размеры аномалий.

## Code context

- `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalAnomalyGenerator.cs`
- `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/AnomalyGeneratorTests.cs`
- `DB/Documentation/04-Engineering/TacticalBattle.md` — описание области и шага генерации.

Интеграционная проверка клиента использует обновлённые ожидаемые данные seed 1 в `View/battle-data-seed1.txt`, `View/Ep0019ViewTests.cs`, `PhaseOutcomeTests.cs` и `PilotCardModelTests.cs`. Эти изменения тестовых данных отражают ход боя с новой географией. Растровые эталоны и общая приёмка интерфейса публикуются с US-0010 TK-0005.

## Public API after the change

Форматы команд и сохранений прежние.

## Implementation steps

Изменить указанную точку отрисовки, ввода или генерации, затем проверить соответствующий критерий через существующую фикстуру.

## Out of scope

Изменения боевой формулы, камеры и правил сохранений.

## Invariants

Расширить генерацию за пределы центральной полосы. Сохранить размер и состав сгустков и безопасный отступ от стартовых позиций. Добавить периферийные области с сопоставимым шагом; генерация по seed остаётся детерминированной.

## Tests

`AnomalyGeneratorTests` в Engine.Tests. Результаты под абсолютным каталогом `D:/DeepSpaceSaga/artifacts/validation/maintenance-us-0010-0011/<run>`. Лимит процесса 300000 мс.

## Definition of Done

Критерий подтверждён целевыми проверками, `git diff --check` проходит, статус и результаты записаны перед публикацией.

## Self-containment check

В контексте указаны файлы реализации и тестов.

## Статус

106/106 целевых Engine-тестов, в том числе генерация 1000 seed и контроль независимого RNG. 419/419 тестов тактического клиента прошли с обновлёнными ожиданиями. Новый контрольный бой seed 1: победа на ходу 21 после уничтожения носителя, 40 выстрелов, 20 попаданий. Логи: D:/DeepSpaceSaga/artifacts/validation/maintenance-us-0010-0011/20261110140406919/tk6-engine/test.log и D:/DeepSpaceSaga/artifacts/validation/maintenance-us-0010-0011/20261110140406919/client-final/test.log.
