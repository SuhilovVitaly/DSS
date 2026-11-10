---
epic: EP-0021-drone-combat-economy
story: EP-0021-US-0004-campaign-sortie
ticket: EP-0021-US-0004-TK-0001-sortie-request
title: Контракт вылета кампании
stage: approved
layer: contracts
depends_on: [EP-0021-US-0009-TK-0002-carrier-action-contract]
files_touched: 3
serves: [AC-01]
created: 2026-11-09T11:46:33Z
revision: 1
---

# Контракт вылета кампании

## Why

Часть [Вылет кампании собственным крылом](../EP-0021-US-0004-campaign-sortie.md). Приёмка этой части покрывает AC-01 на поверхности указанного слоя.

## Decisions

Применить [D-01/D-02 эпика](../../Documentation.md#требования-пользователя-и-рабочие-решения) в пределах истории. Пользователь поручил подготовить все тикеты одним набором без промежуточных подтверждений. `approved` здесь означает автоматический статус карты, а продуктовый просмотр отложен пользователем.

## Assumptions

Применить соответствующие A-01…A-04 и предварительные числа эпика. Проверять свой шаг на контролируемой фикстуре до сквозного прогона US-0007.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/TacticalBattleRequests.cs` | StartTacticalBattleRequest и TacticalBattleResponse, строки 6–30 базовой версии. | Изменить только интеграцию, указанную в Implementation steps. |
| `src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs` | Точная сигнатура TacticalBattleSnapshot, текущий snapshot DTO. | Изменить только интеграцию, указанную в Implementation steps. |
| `tests/DeepSpaceSaga.Contracts.Tests/CampaignSortieContractTests.cs` | Новый файл в рамках тикета. | Именованный класс CampaignSortieContractTests и его данные. |

## Public API after the change

Добавить необязательный CampaignWingRevision в StartTacticalBattleRequest и режим в снимок. Null означает тренировку; значение требует scenario initial и отсутствие client loadout. Добавить причины WingNotReady/StaleWing/SortieUnavailable в конец enum. Совместимость сериализации проверить на старых запросах.

## Implementation steps

Добавить необязательный CampaignWingRevision в StartTacticalBattleRequest и режим в снимок. Null означает тренировку; значение требует scenario initial и отсутствие client loadout. Добавить причины WingNotReady/StaleWing/SortieUnavailable в конец enum. Совместимость сериализации проверить на старых запросах.

## Out of scope

Соседние слои, продуктовые функции вне критериев и правки за пределами Code context. Для соседней интеграции использовать именованные зависимости.

## Invariants

- `TacticalBattleSession.cs:35–64` сейчас принимает сценарий и overlay. `TacticalBattleState.cs:36–57` содержит характеристики дрона и заряды.
- Доступность кампании проверяется на фактическом состоянии крыла, ангарах и каютах; запуск замораживает точную сборку и связи с world ID.
- Отказы и повторы проверяются в точках изменения имущества, рынка, визита и результата, применимых к этому тикету.

## Tests

Проект проверок `tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj`. Имена ниже задают будущие методы. У каждого процесса тайм-аут 300000 ms с остановкой всего дерева. Длительные серии запускать по одному точному имени метода.

- `CampaignSortieContractTests.Ac01` покрывает **AC-01**. Доступность кампании проверяется на фактическом состоянии крыла, ангарах и каютах; запуск замораживает точную сборку и связи с world ID.

Из `D:/DeepSpaceSaga/DSS` в PowerShell:

```powershell
$carrierRun = "D:/DeepSpaceSaga/artifacts/validation/ep-0021/EP-0021-US-0004-TK-0001-sortie-request-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $carrierRun -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj" --configuration Release --filter "FullyQualifiedName~.CampaignSortieContractTests." --results-directory "$carrierRun" --logger "trx"
dotnet build "src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj" --configuration Release
git diff --check
```

## Definition of Done

- Изменения ограничены Code context. Каждое действие даёт наблюдаемый результат на своём слое.
- Каждый указанный критерий покрыт именованной проверкой, а результат focused test и build записан с путём артефакта. Исходное падение отделено от новой регрессии.
- Инварианты и совместимость входов проверены. Сверены зависимости, diff и отсутствие лишних файлов.

## Self-containment check

Эпик содержит эталон, цены, маршрут и правила последствий. Здесь заданы границы слоя, входы, ожидаемый результат, зависимости и точка проверки. Расширение allowlist оформляется в Board, если текущая поверхность окажется недостаточной.
