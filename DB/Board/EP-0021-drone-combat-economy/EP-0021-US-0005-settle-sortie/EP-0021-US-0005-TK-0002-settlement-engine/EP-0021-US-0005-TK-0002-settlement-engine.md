---
epic: EP-0021-drone-combat-economy
story: EP-0021-US-0005-settle-sortie
ticket: EP-0021-US-0005-TK-0002-settlement-engine
title: Однократное списание при выходе
stage: approved
layer: engine
depends_on: [EP-0021-US-0004-TK-0002-owned-wing-assembly, EP-0021-US-0005-TK-0001-sortie-result]
files_touched: 5
serves: [AC-01]
created: 2026-11-09T11:46:33Z
revision: 1
---

# Однократное списание при выходе

## Why

Часть [Потери и экономический итог вылета](../EP-0021-US-0005-settle-sortie.md). Приёмка этой части покрывает AC-01 на поверхности указанного слоя.

## Decisions

Применить [D-01/D-02 эпика](../../Documentation.md#требования-пользователя-и-рабочие-решения) в пределах истории. Пользователь поручил подготовить все тикеты одним набором без промежуточных подтверждений. `approved` здесь означает автоматический статус карты, а продуктовый просмотр отложен пользователем.

## Assumptions

Применить соответствующие A-01…A-04 и предварительные числа эпика. Проверять свой шаг на контролируемой фикстуре до сквозного прогона US-0007.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.TacticalBattle.cs` | Start/Exit и ExecuteTactical, строки 48–121. | Изменить только интеграцию, указанную в Implementation steps. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs` | TryCreate, Fire, Exit. Валидация сценарного запуска в строках 35–64. | Изменить только интеграцию, указанную в Implementation steps. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.CarrierWing.cs` | Поверхность из Grounding истории. `SimulationEngine.TacticalBattle.cs:81–94` закрывает временную сессию, `TacticalBattleState.cs:20–25,36–57` различает повреждения и заряды. | Изменить только интеграцию, указанную в Implementation steps. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.SortieSettlement.cs` | Новый файл в рамках тикета. | Добавить только локальный тип или данные, описанные в Implementation steps. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/CampaignSettlementTests.cs` | Новый файл в рамках тикета. | Именованный класс CampaignSettlementTests и его данные. |

## Public API after the change

Использовать контракты из зависимых тикетов. Новые локальные типы, проекции или действия ограничены Implementation steps.

## Implementation steps

На Exit финализировать под world lock по SortieId, используя реальное состояние каждой своей машины. Destroyed удаляет корпус и установленные модули, оставляет ангар; disabled возвращается ремонтопригодным; реальные charges сохраняются. Пилоты возвращаются, носитель остаётся на карте; abort считает уже понесённое без награды. Ошибка до commit оставляет сессию для повтора. Публикацию режима завершить после US-0011. При повреждениях сохранить hull/armor, jam и факторы двигателя в persistent record; стоимость списания по имеющейся basis без мнимого списания Tokens.

## Out of scope

Соседние слои, продуктовые функции вне критериев и правки за пределами Code context. Для соседней интеграции использовать именованные зависимости.

## Invariants

- `SimulationEngine.TacticalBattle.cs:81–94` закрывает временную сессию, `TacticalBattleState.cs:20–25,36–57` различает повреждения и заряды.
- Исход и досрочный выход считают фактические потери и заряды один раз, тренировочные бои не затрагивают мир.
- Отказы и повторы проверяются в точках изменения имущества, рынка, визита и результата, применимых к этому тикету.

## Tests

Проект проверок `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Имена ниже задают будущие методы. У каждого процесса тайм-аут 300000 ms с остановкой всего дерева. Длительные серии запускать по одному точному имени метода.

- `CampaignSettlementTests.Ac01` покрывает **AC-01**. Исход и досрочный выход считают фактические потери и заряды один раз, тренировочные бои не затрагивают мир.

Из `D:/DeepSpaceSaga/DSS` в PowerShell:

```powershell
$carrierRun = "D:/DeepSpaceSaga/artifacts/validation/ep-0021/EP-0021-US-0005-TK-0002-settlement-engine-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $carrierRun -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --configuration Release --filter "FullyQualifiedName~.CampaignSettlementTests." --results-directory "$carrierRun" --logger "trx"
dotnet build "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" --configuration Release
git diff --check
```

## Definition of Done

- Изменения ограничены Code context. Каждое действие даёт наблюдаемый результат на своём слое.
- Каждый указанный критерий покрыт именованной проверкой, а результат focused test и build записан с путём артефакта. Исходное падение отделено от новой регрессии.
- Инварианты и совместимость входов проверены. Сверены зависимости, diff и отсутствие лишних файлов.

## Self-containment check

Эпик содержит эталон, цены, маршрут и правила последствий. Здесь заданы границы слоя, входы, ожидаемый результат, зависимости и точка проверки. Расширение allowlist оформляется в Board, если текущая поверхность окажется недостаточной.
