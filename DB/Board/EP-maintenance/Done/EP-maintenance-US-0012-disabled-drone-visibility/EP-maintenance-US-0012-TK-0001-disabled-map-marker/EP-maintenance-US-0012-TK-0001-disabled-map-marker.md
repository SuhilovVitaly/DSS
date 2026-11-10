---
epic: EP-maintenance
story: EP-maintenance-US-0012-disabled-drone-visibility
ticket: EP-maintenance-US-0012-TK-0001-disabled-map-marker
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
files_touched: 2
serves: [AC1]
created: "2026-11-10T14:45:55.523Z"
---

# Обездвиженные дроны на карте

## Why

На присланном кадре у Тага-2 корпус 5/45 и статус Disabled, а силуэт окрашен как активный противник.

## Code context

- `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.cs`
- `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleMapRenderTests.cs`

## Decisions and invariants

Отобразить серый силуэт и красный крест у полос по авторитетному статусу Disabled. Прежний крест уничтожения и имя сохраняются. Формулы повреждений и движения не меняются.

## Tests and Definition of Done

Растровая проверка отличает Disabled от Air с тем же корпусом, фиксирует положение креста и сохраняет возможность наведения. Затронутые PNG-эталоны могут быть обновлены.

## Статус

34/34 целевых теста клиента, включая растровую проверку Disabled, прошли. Лог: D:/DeepSpaceSaga/artifacts/validation/tactical-map-polish/20261110144815363/client-recheck/test.log.
