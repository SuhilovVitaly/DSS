---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0019-bugs
ticket: EP-0015-US-0019-TK-0005-named-wrecks-on-impact
stage: done
layer: contracts + engine + client
depends_on: []
files_touched:
  - src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs
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

Добавить необязательный признак LethalImpact к DTO выстрела, сохранив значение Killed как итога залпа. Engine помечает первое попадание, которое обнуляет корпус в порядке проигрывания. Клиент переключает маркер по моменту прилёта. Изменение проходит через Contracts, Engine и Client одним тикетом, чтобы опубликовать согласованную цепочку данных и два регрессионных теста.

## Tests

Фильтры `BattleMapRenderTests|ResultsPhaseTests; SalvoTests` в соответствующих DeepSpaceSaga.Client.Tests и DeepSpaceSaga.Engine.Tests. Проверить изменённые файлы через `git diff --check`.

## Definition of Done

Критерий покрыт проверкой в Dss-Main. Результат проверок записан отдельно от ручной приёмки. Отдельный коммит опубликован в ветке эпика.

## Исполнение

Коммит `95d06b43` опубликован в ветке эпика. 34/34 целевых тестов Client и 19/19 тестов залпа и контрактов из Engine.Tests. Проверено, что крест с именем появляется на фатальном попадании, пока залп продолжается. Тест ActualSalvoWaitsForArrivalAndSkipReplayContinueNeverSendAnotherShot продолжает падать на границе завершения повтора, как зафиксировано в US-0018. Эталонные тесты Engine запущены с временным перенаправлением поиска исходных эталонов в Dss-Main. Ручная приёмка открыта.
