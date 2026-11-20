---
epic: EP-maintenance
story: EP-maintenance-US-0025-debug-kill-all
ticket: EP-maintenance-US-0025-TK-0003-console-command
stage: approved
implementation_status: implemented
layer: client
depends_on: [EP-maintenance-US-0025-TK-0002-resolve-enemy-side]
---
# EP-maintenance-US-0025-TK-0003-console-command

Зарегистрировать `kill all` и показать его в `help`. Выполнять через подтверждённую тактическую сессию и обычный транспорт. После команды открывается стандартная карта лута, в том числе в «Начальном бою». Восстановление лута при загрузке должно работать и для вылета без привязки к пиратской встрече.

Файлы реализации: `src/DeepSpaceSaga.Client/UI/DebugConsole/KillAllDebugCommand.cs`, `src/DeepSpaceSaga.Client/UI/DebugConsole/DebugCommandCatalog.cs`.

Проект проверок: `DeepSpaceSaga.Client.Tests`. Интеграционное покрытие: `KillAllOpensLootFromAnActiveEncounter`.
