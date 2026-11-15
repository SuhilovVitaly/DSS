---
epic: EP-0021-drone-combat-economy
story: EP-0021-US-0012-campaign-entry
ticket: EP-0021-US-0012-TK-0001-campaign-launch
title: Отдельный запуск с карты
stage: approved
layer: client
depends_on: [EP-0021-US-0004-TK-0002-owned-wing-assembly, EP-0021-US-0005-TK-0002-settlement-engine, EP-0021-US-0011-TK-0002-carrier-world-continuity]
files_touched: 5
serves: [AC-01]
created: 2026-11-09T11:46:33Z
revision: 1
---

# Отдельный запуск с карты

## Why

Часть [Вход в вылет из кампании и отдельная тренировка](../EP-0021-US-0012-campaign-entry.md). Приёмка этой части покрывает AC-01 на поверхности указанного слоя.

## Decisions

Применить [D-01/D-02 эпика](../../Documentation.md#требования-пользователя-и-рабочие-решения) в пределах истории. Пользователь поручил подготовить все тикеты одним набором без промежуточных подтверждений. `approved` здесь означает автоматический статус карты, а продуктовый просмотр отложен пользователем.

## Assumptions

Применить соответствующие A-01…A-04 и предварительные числа эпика. Проверять свой шаг на контролируемой фикстуре до сквозного прогона US-0007.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs` | Текущий вход F8 и закрытие, строки 116–140. | Изменить только интеграцию, указанную в Implementation steps. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` | Поверхность из Grounding истории. `TacticalBattleFlow.cs:116–140` обслуживает текущий вход и выход, `GameSessionHandle.TacticalBattle.cs:27–34` передаёт запросы. | Изменить только интеграцию, указанную в Implementation steps. |
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | Действующий русский ресурс локализации. | Изменить только интеграцию, указанную в Implementation steps. |
| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | Действующий английский ресурс локализации. | Изменить только интеграцию, указанную в Implementation steps. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/CampaignLaunchTests.cs` | Новый файл в рамках тикета. | Именованный класс CampaignLaunchTests и его данные. |

## Public API after the change

Использовать контракты из зависимых тикетов. Новые локальные типы, проекции или действия ограничены Implementation steps.

## Implementation steps

Добавить отдельное действие «Боевой вылет» на карте, видимую готовность, подтверждение ставок и режим. Отправлять текущую revision через существующий GameSessionHandle, сохранять цикл pause/exit. F8 отправляет прежний запрос.

## Out of scope

Соседние слои, продуктовые функции вне критериев и правки за пределами Code context. Для соседней интеграции использовать именованные зависимости.

## Invariants

- `TacticalBattleFlow.cs:116–140` обслуживает текущий вход и выход, `GameSessionHandle.TacticalBattle.cs:27–34` передаёт запросы.
- Отдельный вход с карты передаёт revision крыла, предупреждает о последствиях и отображает отказ или вход в бой. F8 продолжает запускать тренировку.
- Отказы и повторы проверяются в точках изменения имущества, рынка, визита и результата, применимых к этому тикету.

## Tests

Проект проверок `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Имена ниже задают будущие методы. У каждого процесса тайм-аут 300000 ms с остановкой всего дерева. Длительные серии запускать по одному точному имени метода.

- `CampaignLaunchTests.Ac01` покрывает **AC-01**. Отдельный вход с карты передаёт revision крыла, предупреждает о последствиях и отображает отказ или вход в бой. F8 продолжает запускать тренировку.

Из `D:/DeepSpaceSaga/DSS` в PowerShell:

```powershell
$carrierRun = "D:/DeepSpaceSaga/artifacts/validation/ep-0021/EP-0021-US-0012-TK-0001-campaign-launch-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $carrierRun -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --configuration Release --filter "FullyQualifiedName~.CampaignLaunchTests." --results-directory "$carrierRun" --logger "trx"
dotnet build "src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" --configuration Release
git diff --check
```

## Definition of Done

- Изменения ограничены Code context. Каждое действие даёт наблюдаемый результат на своём слое.
- Каждый указанный критерий покрыт именованной проверкой, а результат focused test и build записан с путём артефакта. Исходное падение отделено от новой регрессии.
- Инварианты и совместимость входов проверены. Сверены зависимости, diff и отсутствие лишних файлов.

## Self-containment check

Эпик содержит эталон, цены, маршрут и правила последствий. Здесь заданы границы слоя, входы, ожидаемый результат, зависимости и точка проверки. Расширение allowlist оформляется в Board, если текущая поверхность окажется недостаточной.
