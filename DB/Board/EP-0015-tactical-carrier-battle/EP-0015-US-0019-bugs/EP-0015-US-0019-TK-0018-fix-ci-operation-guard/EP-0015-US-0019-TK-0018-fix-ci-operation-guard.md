---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0019-bugs
ticket: EP-0015-US-0019-TK-0018-fix-ci-operation-guard
stage: done
layer: client
depends_on: [EP-0015-US-0019-TK-0017-keep-trails-during-fire]
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs
---

# Исправить нарушение контракта операций в PR 100

## Контракт AC-18

Guard ProductionTacticalBattleSourceUsesOnlyApprovedOperations проходит для production-кода. Заменить Math.Min/Max/Clamp на PyMath, явно использовать double в делениях и приводить координаты к float на границе Skia. Целочисленный номер позиции краевой метки сохранить через явное усечение.

## Проверка

Motion suite и проверки карты боя. После публикации проверить Linux workflow на вершине PR 100 и устранить обнаруженные связанные ошибки. Источник дефекта — job 127951451189, run 42235370923.

## Исполнение

Локально Motion 1267/1267 и Client 18/18. Операции приведены к контракту, явное усечение индекса сохранено. Журналы `D:/DeepSpaceSaga/artifacts/validation/pr100-ci/20261107-1794040474770/motion.log` и `D:/DeepSpaceSaga/artifacts/validation/pr100-ci/20261107-1794040474770/client.log`. Результат проверки опубликованного CI записывается в `D:/DeepSpaceSaga/artifacts/validation/pr100-ci/20261107-1794040474770/Report.md`.
