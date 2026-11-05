---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0011-legacy-removal-prep
title: "Подготовка удаления старого боя: инвентаризация, сохранения, небоевое"
stage: draft
dependencies: [EP-0015-US-0010-battle-end]
---

# Подготовка удаления старого боя: инвентаризация, сохранения, небоевое

## Связь с техническим заданием

Источник правил, данных и эталонов — `VData/Planning/V4.0` (Spec, Data, Reference). Решения и gaps — [Documentation.md](../Documentation.md). Требования ниже — целевой результат, не утверждение о реализации.

## User story

Как разработчик, до удаления прежней боевой системы реального времени (EP-0006…EP-0014) я фиксирую, что именно удаляется и кем используется, сохраняю старые сохранения как фикстуры, переношу небоевое из боевых файлов и перевожу формат сохранения на 20 (Save20, записанные до чистки сценариев в US-0013, — только машина разработки, не поддерживаются), чтобы дальше удалять код сверху вниз компилируемыми срезами.

## Зависимости

[EP-0015-US-0010-battle-end](../EP-0015-US-0010-battle-end/EP-0015-US-0010-battle-end.md)

## Completion evidence

- **AC-01.** `RemovalInventory.md`: каждый символ и файл старого боя во всех слоях (Client, Engine.LocalClient, Engine, Motion, Contracts, контент, инструменты, тесты) → consumers (поиск по символам, тестам, данным, csproj; граф Graphify, если есть) → решение (удалить / почистить / оставить) → тикет удаления → тестовые файлы этого тикета. Решения записаны для: `DefeatScreen`/`Defeat.md`/`SessionOutcomeSnapshot`, `ship.mukha`/`ship.shmel`, `CombatLaser`, `Scanner`, броня и `armorClass` модулей и кораблей (`ScenarioData.cs:482`, `SimulationEngine.cs:268,1427`), `DestroyedModuleLimit` панелей EP-0014, `SimulationSpeed.IsAllowedInCombat`, `TurnHold`/`SpeedHold`/`RuntimeMotion` (чистить, не удалять), Dialogue encounter, `tools/DeepSpaceSaga.EconomyBalance` (`combatRules`), `tools/graphify/rebuild.py`, `tools/DeepSpaceSaga.Performance(.Tests)`, `InternalsVisibleTo DeepSpaceSaga.CombatAnalyzer`. Порядок удаления — **сверху вниз**: Client → Engine.LocalClient → Engine → Motion → Contracts → контент и инструменты.

- **AC-02.** До удаления любого кода в `tests/DeepSpaceSaga.Engine.Tests/Fixtures/Saves/` лежат реальные Save18 и Save19 сценария `Default` (без боя и с активным боем: point combat, контрмеры, hostile, отчёты), снятые текущим writer'ом, с записью, как получены.

- **AC-03.** Клавиши ручного маневрирования EP-0013 (`turnLeft`/`accelerate`/`brake`/`fullSpeed`) и `debugPanel` перенесены из `combat-input.json` в `Data/UI/maneuver-input.json` и `ManeuverInputSettings`; маневрирование работает; `CombatInputSettings` больше их не читает.

- **AC-04.** Сохранение формата **20**: writer не пишет боевых секций; `ScenarioLoader` загружает 18 и 19 через миграцию в 20, которая атомарно отбрасывает боевое состояние (point combat, контрмеры, hostile, reactions, reports, боевые RNG) **и** вычищает из кораблей экземпляры типов модулей по инвентаризации (TorpedoLauncher, CountermeasureLauncher и прочие), их команды и боезапас, а также враждебные объекты старого боя; сохраняет семантику 18→19 (Save18 с полями Save19 отвергается, `ScenarioLoader.cs:93-101`) и проверки каталога/fingerprint (`SimulationEngine.cs:273-277`); повреждённые сохранения отклоняются; фикстуры AC-02 загружаются и продолжаются детерминированно.

Это план будущих наблюдений, а не выполненная проверка. Automated/render, native/input и product/human acceptance фиксируются раздельно.

## Ограничения и gaps

Удаление идёт после того, как новый бой полностью работает (US-0010). Каждый тикет — компилируемый срез (R-09).

## Тикеты

| Ticket | Layer | Story criteria |
|---|---|---|
| [EP-0015-US-0011-TK-0001-removal-inventory](EP-0015-US-0011-TK-0001-removal-inventory/EP-0015-US-0011-TK-0001-removal-inventory.md) | documentation | AC-01 |
| [EP-0015-US-0011-TK-0002-save-fixtures](EP-0015-US-0011-TK-0002-save-fixtures/EP-0015-US-0011-TK-0002-save-fixtures.md) | engine | AC-02 |
| [EP-0015-US-0011-TK-0003-maneuver-input-move](EP-0015-US-0011-TK-0003-maneuver-input-move/EP-0015-US-0011-TK-0003-maneuver-input-move.md) | client | AC-03 |
| [EP-0015-US-0011-TK-0004-save-format-20](EP-0015-US-0011-TK-0004-save-format-20/EP-0015-US-0011-TK-0004-save-format-20.md) | engine | AC-04 |

## Historical planning review

Draft; human approval отсутствует. Решения и сообщения согласования записываются в review log эпика.
