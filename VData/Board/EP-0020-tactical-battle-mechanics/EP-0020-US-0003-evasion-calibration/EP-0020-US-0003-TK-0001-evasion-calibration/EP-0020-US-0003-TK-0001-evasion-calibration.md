---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0003-evasion-calibration
ticket: EP-0020-US-0003-TK-0001-evasion-calibration
title: Прогон калибровки и отчёт
stage: draft
layer: tooling
test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0020-US-0002-TK-0005-evasion-regression]
files_touched: 1
serves: [US3-AC1, US3-AC2, US3-AC3]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Прогон калибровки и отчёт

## Why

Пользователь поставил −20 (D-12), зная, что по предварительной оценке это R ≈ 0,64–0,68, а критерий D-04 даёт около −30. Отчёт на движке с уклонением нужен, чтобы при необходимости пересмотреть число.

## Decisions

Применяются [D-01–D-13](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Уклонение ИИ меняет ход боёв, поэтому числа отчёта могут отличаться от предварительной оценки.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/EvasionCalibrationTests.cs` | нет | Тест с `Trait("Category","Balance")`: прогон, метрика, отчёт `calibration.csv` в `DSS_VALIDATION_ROOT/ep0020-calibration/`. |

## Public API after the change

No API change.

## Implementation steps

1. Реализовать прогон с подменой `EvasionPenalty` в правилах каталога (без записи на диск).
2. Посчитать R по US3-AC1 для каждого штрафа; сохранить таблицу.
3. Перенести таблицу в «Исполнение»; записать R(20) и штраф, ближайший к 0,50, в review log эпика как информацию для пользователя.

## Out of scope

Изменение данных и других параметров баланса.

## Invariants

Обычный прогон тестов не запускает калибровку (категория `Balance`).

## Tests

Проект `tests/DeepSpaceSaga.Engine.Tests`. Именованные проверки:

- `EvasionCalibrationTests.Report_expected_damage_ratio_for_penalties`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~EvasionCalibrationTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us3-tk1" --logger "trx;LogFileName=engine.trx"
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US3-AC1, US3-AC2, US3-AC3 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Метрика и содержание отчёта заданы.

## Исполнение

Не начато.
