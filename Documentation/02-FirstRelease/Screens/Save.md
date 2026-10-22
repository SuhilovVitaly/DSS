# Save

> **Контракт EP-0013.** Текущий контракт EP-0013: writer Save19, явная миграция Save18, сохранение не-каталожного tuning и provenance retimed flight. Сохраняется sampled pose; клавишные удержания и Shift latch не сохраняются. [Подробности](../../04-Engineering/DebugTuningPanel.md#файлы-и-продолжение).

## Current EP-0012 contract — 2026-10-21

Current writer is SaveFormatVersion18. A real Save17 or earlier is rejected atomically without migration/current-world replacement; NewGame scenario version0 is a separate path. Point flights, reload/captured operators, named scatter/dodge RNG, evasion/AI/encounter and fractional motion remainder survive load. Defeated sessions cannot overwrite a slot; error/cancel remains recoverable. Version17/16/15 text below records earlier stages. Existing modal pause, file IO and menu navigation semantics remain applicable. Headless continuation/rejection passed; native physical Save/Load menu acceptance is NOT RUN. [Point-aim combat contract](../../04-Engineering/PointAimCombat.md).

## Актуализация EP-0009 — 2026-10-12

Текущий writer — SaveFormatVersion17: defense3/combat2/hostile2, броня/структура/отключение модулей, режим/готовность/поломка и список активных торпед, очереди ПРО, параметры ИИ и счётчики named RNG. Hull HP и auto-attempt flag не сохраняются. Сохранение Defeated по-прежнему запрещено. Описание Save16/15/11 ниже историческое. [Требования](../../01-Requirements/EngineRequirements.md#phase-four-module-combat) · [проверки](../../04-Engineering/ModuleCombat.md).

## Актуализация EP-0008 — 2026-10-12

На этапе EP-0008 формат16 имел defense payload2, hostile1, combat1; материализованная карта и экономическая continuation сохранялись. Боевой save фиксировал captured параметры, пути, TTL/reload/coast, attempt flags, AI, RNG counters и журнал. В Defeated новый Save/F5 отклонялся до файловой записи, включая гибель при advance. Уже захваченный до гибели immutable save мог завершиться. Отказ не перезаписывал старый слот. Текущий формат17 описан выше; указания Save15/11 ниже — более ранняя история.

[Текущий контракт](../../01-Requirements/EngineRequirements.md#phase-three-combat) · [Проверки и ограничения](../../04-Engineering/PhaseThreeCombatAcceptance.md).


## Сверка реализации — 2026-10-10

Обновление после исправлений: F01–F05 закрыты кодом и регрессиями; EP-0004 включён в текущий рабочий набор. Баланс, 80 FPS и человеческая приёмка остаются открытыми. Текущий статус — `Documentation/04-Engineering/EpicReview20261010/Fixes.md`.

Текущий writer использует SaveFormatVersion=15. Материализованные solarSystem/clusterMap, обе временные шкалы и полная торговая continuation сохраняются. AiMap включён в текущую рабочую копию: Load проверяет и восстанавливает материализованные descriptors без регенерации.

[Текущее устройство](../../04-Engineering/TradingAndSolarSystem.md) · [Ревью, дефекты и остаток](../../04-Engineering/EpicReview20261010/README.md). Датированные записи ниже сохраняют историческое значение; они не являются новым подтверждением готовности.

Статус: экран уже существует в коде.

Код: `src/DeepSpaceSaga.Client/UI/Screens/Save/`.

## Назначение

Модальное окно сохранения поверх игровой сессии. Открывается из `Game Menu`.

## Уже существующая функциональность

- Показывает список save slots.
- Выбранный слот определяет цель операций `Overwrite` и `Delete`.
- Позволяет создать `New Save`.
- Позволяет перезаписать существующий слот.
- Позволяет удалить слот через подтверждение повторным нажатием.
- Фильтрует reserved quicksave slot.
- `Escape` или `Close` закрывает окно и возвращает в предыдущее меню.

## Визуальный стиль

- Окно и кнопки используют `Generic Type A`.
- Список слотов расположен в одной контентной панели; отдельной правой панели нет.
- Фон окна непрозрачный: элементы предыдущего экрана не просвечивают через Save Screen.
- Поле имени всегда видно над списком сохранений.
- Выбор строки подставляет display name слота в поле имени.
- Все действия находятся в одном нижнем ряду: `Close`, `Delete` и единая динамическая кнопка сохранения.
- Для уникального имени динамическая кнопка отображается как `New Save`; при совпадении имени существующего слота — как `Overwrite`.
- Текст `Overwrite` использует оранжевый Xenon-акцент `#FF8404`; `New Save` сохраняет стандартный голубой цвет.
- Совпавший с полем имени слот автоматически выделяется в списке и прокручивается в видимую область; для уникального имени выделение снимается.
- После успешного `New Save` или `Overwrite` окно закрывается.

## Требования первого релиза

- Пока окно открыто, действует modal pause rule.
- Save operations должны выполняться через session/factory boundary, без прямой логики Engine внутри экрана.
- После завершения сохранения список слотов должен обновляться.
- Сохранение экономической сессии должно включать текущие склады, бюджеты и `marketRevision` станций.
- Сохраняются активные события станций, их оставшаяся длительность, `masterSeed` и сгенерированная торговая сеть.
- Сохранение на границе торговой команды не должно приводить к повторному исполнению или тихой переоценке сделки.


## EP-0007 — актуальное дополнение от 2026-10-04

Формат сохранения11 включает DefenseState v1: состояние аппаратов, живые ПР и аналитические маршруты/следы, attempt flags, frozen chance, абсолютные физические deadlines, RNG state/counter и журнал. Выбор активной ПР сохраняется. Эфемерные UI-анимации не являются игровым состоянием. [Технический контракт и приёмка](../../04-Engineering/CountermeasureCombat.md).

## Актуализация EP-0004 — 2026-10-08

Текущий SaveFormatVersion=15; сведения о формате11 выше — историческая поставка EP-0007. Optional AiMap rulesVersion1 сохраняет готовые базы, радиусы, геометрию/anchors/seeds полей и тексты/anchors POI. Сохраняются физические и календарные epochs. Изменение content после save не меняет карту при Load.

[Текущий технический контракт и evidence](../../04-Engineering/AiMapEnvironment.md).
