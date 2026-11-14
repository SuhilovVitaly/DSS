# Тикеты EP-0028

Порядок строк — порядок исполнения: каждый тикет зависит только от тикетов выше. После каждого тикета — отдельный коммит и `git push` (D-08). Документационный тикет выполняется последним.

| # | Тикет | Слой | Результат | Зависит от |
|---|---|---|---|---|
| 1 | [EP-0028-US-0001-TK-0001-script-format](EP-0028-US-0001-hint-framework/EP-0028-US-0001-TK-0001-script-format/EP-0028-US-0001-TK-0001-script-format.md) | client | Формат `battle-tutorial.json`, модель и валидирующий загрузчик | — |
| 2 | [EP-0028-US-0001-TK-0002-director-core](EP-0028-US-0001-hint-framework/EP-0028-US-0001-TK-0002-director-core/EP-0028-US-0001-TK-0002-director-core.md) | client | Директор шагов: наблюдение кадра, очередь, завершение действием игрока | 1 |
| 3 | [EP-0028-US-0001-TK-0003-battle-signals](EP-0028-US-0001-hint-framework/EP-0028-US-0001-TK-0003-battle-signals/EP-0028-US-0001-TK-0003-battle-signals.md) | client | Контекстные сигналы боя из принятого снимка | 2 |
| 4 | [EP-0028-US-0001-TK-0004-screen-integration](EP-0028-US-0001-hint-framework/EP-0028-US-0001-TK-0004-screen-integration/EP-0028-US-0001-TK-0004-screen-integration.md) | client | Сессия обучения в экране боя только для IsTutorial | 3 |
| 5 | [EP-0028-US-0002-TK-0001-ui-texts](EP-0028-US-0002-comms-panel/EP-0028-US-0002-TK-0001-ui-texts/EP-0028-US-0002-TK-0001-ui-texts.md) | content-data | UI-строки обучения RU/EN и тест паритета | — |
| 6 | [EP-0028-US-0002-TK-0002-comms-panel](EP-0028-US-0002-comms-panel/EP-0028-US-0002-TK-0002-comms-panel/EP-0028-US-0002-TK-0002-comms-panel.md) | client | Панель связи: портрет, имя, реплика, «Понял», «Подсказки: выкл» | 4, 5 |
| 7 | [EP-0028-US-0002-TK-0003-target-highlight](EP-0028-US-0002-comms-panel/EP-0028-US-0002-TK-0003-target-highlight/EP-0028-US-0002-TK-0003-target-highlight.md) | client | Пульсирующая рамка и стрелка по якорям `BattleLayout.Rects` | 6 |
| 8 | [EP-0028-US-0002-TK-0004-quiet-rules](EP-0028-US-0002-comms-panel/EP-0028-US-0002-TK-0004-quiet-rules/EP-0028-US-0002-TK-0004-quiet-rules.md) | client | Правила «не перебивать»: тишина при вводе, интервал, сворачивание | 6 |
| 9 | [EP-0028-US-0002-TK-0005-comms-journal](EP-0028-US-0002-comms-panel/EP-0028-US-0002-TK-0005-comms-journal/EP-0028-US-0002-TK-0005-comms-journal.md) | client | Журнал связи по H | 6 |
| 10 | [EP-0028-US-0003-TK-0001-officer](EP-0028-US-0003-officer-lessons/EP-0028-US-0003-TK-0001-officer/EP-0028-US-0003-TK-0001-officer.md) | content-data | Офицер «Скитальца»: имя RU/EN, портрет, тон, приветствие | 6, 5 |
| 11 | [EP-0028-US-0003-TK-0002-move-lessons](EP-0028-US-0003-officer-lessons/EP-0028-US-0003-TK-0002-move-lessons/EP-0028-US-0003-TK-0002-move-lessons.md) | content-data | Уроки движения T1 и T2 | 10, 7, 8 |
| 12 | [EP-0028-US-0003-TK-0003-fire-lessons](EP-0028-US-0003-officer-lessons/EP-0028-US-0003-TK-0003-fire-lessons/EP-0028-US-0003-TK-0003-fire-lessons.md) | content-data | Уроки стрельбы и залпа | 11 |
| 13 | [EP-0028-US-0003-TK-0004-context-lessons](EP-0028-US-0003-officer-lessons/EP-0028-US-0003-TK-0004-context-lessons/EP-0028-US-0003-TK-0004-context-lessons.md) | content-data | Контекстные уроки: огонь, урон, сознание, аномалия, потеря дрона | 12 |
| 14 | [EP-0028-US-0003-TK-0005-debrief](EP-0028-US-0003-officer-lessons/EP-0028-US-0003-TK-0005-debrief/EP-0028-US-0003-TK-0005-debrief.md) | content-data | Разбор победы/поражения и «На карту» | 13 |
| 15 | [EP-0028-US-0004-TK-0001-hints-preference](EP-0028-US-0004-hints-toggle/EP-0028-US-0004-TK-0001-hints-preference/EP-0028-US-0004-TK-0001-hints-preference.md) | client | `gameSettings.tutorialHints`: чтение и атомарная запись | — |
| 16 | [EP-0028-US-0004-TK-0002-settings-row](EP-0028-US-0004-hints-toggle/EP-0028-US-0004-TK-0002-settings-row/EP-0028-US-0004-TK-0002-settings-row.md) | client | Строка «Подсказки обучения» в Настройках | 15, 5 |
| 17 | [EP-0028-US-0004-TK-0003-battle-toggle](EP-0028-US-0004-hints-toggle/EP-0028-US-0004-TK-0003-battle-toggle/EP-0028-US-0004-TK-0003-battle-toggle.md) | client | Кнопка в бою, свёрнутый значок, сохранение выбора | 15, 6 |
| 18 | [EP-0028-US-0005-TK-0001-forced-tabletop](EP-0028-US-0005-reliable-first-battle/EP-0028-US-0005-TK-0001-forced-tabletop/EP-0028-US-0005-TK-0001-forced-tabletop.md) | client | Бой обучения всегда в Tabletop | 4 |
| 19 | [EP-0028-US-0005-TK-0002-winnable-battle](EP-0028-US-0005-reliable-first-battle/EP-0028-US-0005-TK-0002-winnable-battle/EP-0028-US-0005-TK-0002-winnable-battle.md) | client | Победа и по Enter, и по подсказкам; seed зафиксирован | 18 |
| 20 | [EP-0028-US-0006-TK-0001-step-metrics](EP-0028-US-0006-metrics-acceptance-docs/EP-0028-US-0006-TK-0001-step-metrics/EP-0028-US-0006-TK-0001-step-metrics.md) | client | Локальные метрики шагов | 17, 14 |
| 21 | [EP-0028-US-0006-TK-0002-native-acceptance](EP-0028-US-0006-metrics-acceptance-docs/EP-0028-US-0006-TK-0002-native-acceptance/EP-0028-US-0006-TK-0002-native-acceptance.md) | verification | Нативная приёмка на меньшем мониторе, RU/EN, вкл/выкл | 19, 20 |
| 22 | [EP-0028-US-0006-TK-0003-project-docs](EP-0028-US-0006-metrics-acceptance-docs/EP-0028-US-0006-TK-0003-project-docs/EP-0028-US-0006-TK-0003-project-docs.md) | documentation | Документация проекта и синхронизация Board | 21 |
