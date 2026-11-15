---
epic: EP-0021-drone-combat-economy
story: EP-0021-US-0004-campaign-sortie
ticket: EP-0021-US-0004-TK-0002-owned-wing-assembly
title: Сборка боя из купленного крыла
stage: approved
layer: engine
depends_on: [EP-0021-US-0004-TK-0001-sortie-request, EP-0021-US-0003-TK-0002-fit-wing]
files_touched: 5
serves: [AC-01, AC-02]
created: 2026-11-09T11:46:33Z
revision: 1
---

# Сборка боя из купленного крыла

## Why

Часть [Вылет кампании собственным крылом](../EP-0021-US-0004-campaign-sortie.md). Приёмка этой части покрывает AC-01, AC-02 на поверхности указанного слоя.

## Decisions

Применить [D-01/D-02 эпика](../../Documentation.md#требования-пользователя-и-рабочие-решения) в пределах истории. Пользователь поручил подготовить все тикеты одним набором без промежуточных подтверждений. `approved` здесь означает автоматический статус карты, а продуктовый просмотр отложен пользователем.

## Assumptions

Применить соответствующие A-01…A-04 и предварительные числа эпика. Проверять свой шаг на контролируемой фикстуре до сквозного прогона US-0007.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleAssembly.cs` | Create и сборка рам/оснащения, строки 14–59. | Изменить только интеграцию, указанную в Implementation steps. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs` | TryCreate, Fire, Exit. Валидация сценарного запуска в строках 35–64. | Изменить только интеграцию, указанную в Implementation steps. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.TacticalBattle.cs` | Start/Exit и ExecuteTactical, строки 48–121. | Изменить только интеграцию, указанную в Implementation steps. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleState.cs` | TacticalDrone, строки 36–57. | Изменить только интеграцию, указанную в Implementation steps. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/CampaignWingAssemblyTests.cs` | Новый файл в рамках тикета. | Именованный класс CampaignWingAssemblyTests и его данные. |

## Public API after the change

Использовать контракты из зависимых тикетов. Новые локальные типы, проекции или действия ограничены Implementation steps.

## Implementation steps

В ветке CampaignWingRevision сверить revision и боеготовность, заморозить позиции сценария и world IDs дронов/пилотов. Допускать от одного до четырёх готовых дронов, каждому сопоставлять уникальный сценарный слот. Эталон использует слот своего пилота, прочие назначения заполняют свободные слоты стабильно по ID. Пустое крыло получает отказ. Собрать дружеские DroneStart из принадлежащих игроку рам, двигателей, брони, пушек, щитов, ловушек, hull, armor, jam, текущие факторы двигателя и charges. Сторону B, seed, движения, инициативу и RNG сохранить. Владелец не передаёт выбранные stats. Сравнить эталонные составы и нормализованную ленту.

## Out of scope

Соседние слои, продуктовые функции вне критериев и правки за пределами Code context. Для соседней интеграции использовать именованные зависимости.

## Invariants

- `TacticalBattleSession.cs:35–64` сейчас принимает сценарий и overlay. `TacticalBattleState.cs:36–57` содержит характеристики дрона и заряды.
- Доступность кампании проверяется на фактическом состоянии крыла, ангарах и каютах; запуск замораживает точную сборку и связи с world ID.
- Отказы и повторы проверяются в точках изменения имущества, рынка, визита и результата, применимых к этому тикету.

## Tests

Проект проверок `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Имена ниже задают будущие методы. У каждого процесса тайм-аут 300000 ms с остановкой всего дерева. Длительные серии запускать по одному точному имени метода.

- `CampaignWingAssemblyTests.Ac01` покрывает **AC-01**. Доступность кампании проверяется на фактическом состоянии крыла, ангарах и каютах; запуск замораживает точную сборку и связи с world ID.
- `CampaignWingAssemblyTests.Ac02` покрывает **AC-02**. Эталонная четвёрка воспроизводит стартовые боевые параметры начального боя. Тот же seed и приказы дают эквивалентный боевой результат при одинаковом состоянии.

Из `D:/DeepSpaceSaga/DSS` в PowerShell:

```powershell
$carrierRun = "D:/DeepSpaceSaga/artifacts/validation/ep-0021/EP-0021-US-0004-TK-0002-owned-wing-assembly-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $carrierRun -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --configuration Release --filter "FullyQualifiedName~.CampaignWingAssemblyTests." --results-directory "$carrierRun" --logger "trx"
dotnet build "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" --configuration Release
git diff --check
```

## Definition of Done

- Изменения ограничены Code context. Каждое действие даёт наблюдаемый результат на своём слое.
- Каждый указанный критерий покрыт именованной проверкой, а результат focused test и build записан с путём артефакта. Исходное падение отделено от новой регрессии.
- Инварианты и совместимость входов проверены. Сверены зависимости, diff и отсутствие лишних файлов.

## Self-containment check

Эпик содержит эталон, цены, маршрут и правила последствий. Здесь заданы границы слоя, входы, ожидаемый результат, зависимости и точка проверки. Расширение allowlist оформляется в Board, если текущая поверхность окажется недостаточной.
