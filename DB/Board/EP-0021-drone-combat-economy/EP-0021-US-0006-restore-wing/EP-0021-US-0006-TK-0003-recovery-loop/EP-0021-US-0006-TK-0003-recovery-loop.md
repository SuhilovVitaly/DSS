---
epic: EP-0021-drone-combat-economy
story: EP-0021-US-0006-restore-wing
ticket: EP-0021-US-0006-TK-0003-recovery-loop
title: Сквозная проверка возврата к бою
stage: approved
layer: engine
depends_on: [EP-0021-US-0006-TK-0001-station-services, EP-0021-US-0002-TK-0004-nearby-outfit-route, EP-0021-US-0011-TK-0002-carrier-world-continuity]
files_touched: 1
serves: [AC-02]
created: 2026-11-09T11:46:33Z
revision: 1
---

# Сквозная проверка возврата к бою

## Why

Часть [Ремонт, заряды и замена после вылета](../EP-0021-US-0006-restore-wing.md). Приёмка этой части покрывает AC-02 на поверхности указанного слоя.

## Decisions

Применить [D-01/D-02 эпика](../../Documentation.md#требования-пользователя-и-рабочие-решения) в пределах истории. Пользователь поручил подготовить все тикеты одним набором без промежуточных подтверждений. `approved` здесь означает автоматический статус карты, а продуктовый просмотр отложен пользователем.

## Assumptions

Применить соответствующие A-01…A-04 и предварительные числа эпика. Проверять свой шаг на контролируемой фикстуре до сквозного прогона US-0007.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Engine.Tests/CampaignRecoveryLoopTests.cs` | Новый файл в рамках тикета. | Именованный класс CampaignRecoveryLoopTests и его данные. |

## Public API after the change

Использовать контракты из зависимых тикетов. Новые локальные типы, проекции или действия ограничены Implementation steps.

## Implementation steps

Отработка полной потери, выхода, сохранения, нормальной торговли, закупки и нового вылета. Для ремонта и перезарядки проверить сессионный повтор, повтор после Load, отсутствующее имущество и исчерпание Tokens.

## Out of scope

Соседние слои, продуктовые функции вне критериев и правки за пределами Code context. Для соседней интеграции использовать именованные зависимости.

## Invariants

- `TacticalBattleSession.cs:118–147` фиксирует расход зарядов. Существующий мир использует реальные Tokens и квитанции (`SimulationEngine.TradeExecution.cs:94–110`).
- После полной потери крыло восстанавливается торговлей и покупкой с конечного рынка, а Load и новый бой не чинят дронов.
- Отказы и повторы проверяются в точках изменения имущества, рынка, визита и результата, применимых к этому тикету.

## Tests

Проект проверок `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Имена ниже задают будущие методы. У каждого процесса тайм-аут 300000 ms с остановкой всего дерева. Длительные серии запускать по одному точному имени метода.

- `CampaignRecoveryLoopTests.Ac02` покрывает **AC-02**. После полной потери крыло восстанавливается торговлей и покупкой с конечного рынка, а Load и новый бой не чинят дронов.

Из `D:/DeepSpaceSaga/DSS` в PowerShell:

```powershell
$carrierRun = "D:/DeepSpaceSaga/artifacts/validation/ep-0021/EP-0021-US-0006-TK-0003-recovery-loop-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $carrierRun -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --configuration Release --filter "FullyQualifiedName~.CampaignRecoveryLoopTests." --results-directory "$carrierRun" --logger "trx"
dotnet build "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" --configuration Release
git diff --check
```

## Definition of Done

- Изменения ограничены Code context. Каждое действие даёт наблюдаемый результат на своём слое.
- Каждый указанный критерий покрыт именованной проверкой, а результат focused test и build записан с путём артефакта. Исходное падение отделено от новой регрессии.
- Инварианты и совместимость входов проверены. Сверены зависимости, diff и отсутствие лишних файлов.

## Self-containment check

Эпик содержит эталон, цены, маршрут и правила последствий. Здесь заданы границы слоя, входы, ожидаемый результат, зависимости и точка проверки. Расширение allowlist оформляется в Board, если текущая поверхность окажется недостаточной.
