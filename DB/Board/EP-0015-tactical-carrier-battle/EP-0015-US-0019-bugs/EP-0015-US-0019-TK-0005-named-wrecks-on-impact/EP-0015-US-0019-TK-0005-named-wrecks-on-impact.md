---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0019-bugs
ticket: EP-0015-US-0019-TK-0005-named-wrecks-on-impact
stage: approved
layer: engine + client
depends_on: []
files_touched:
  - src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSalvo.cs
  - src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleMapRenderTests.cs
  - tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/SalvoTests.cs
---

# Именованные кресты с момента сбития

## Контракт и критерий AC-05

Крест и имя появляются при прилёте фатального попадания, сохраняются до конца залпа и в движении. Повтор воспроизводит тот же момент. Нефатальные попадания и промахи сохраняют маркер дрона.

## Code context и реализация

Выставить Killed для фатального попадания на авторитетной границе расчёта залпа, сохранив одновременный результат и RNG. Клиент переключает маркер по моменту прилёта. Изменение требует согласованной границы Engine → Client и использует существующий DTO.

## Tests

Фильтры `BattleMapRenderTests|ResultsPhaseTests; SalvoTests` в соответствующих DeepSpaceSaga.Client.Tests и DeepSpaceSaga.Engine.Tests. Проверить изменённые файлы через `git diff --check`.

## Definition of Done

Критерий покрыт проверкой в Dss-Main. Результат проверок записан отдельно от ручной приёмки. Отдельный коммит опубликован в ветке эпика.
