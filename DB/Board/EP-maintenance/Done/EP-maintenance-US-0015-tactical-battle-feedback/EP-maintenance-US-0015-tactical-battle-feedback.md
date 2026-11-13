---
epic: EP-maintenance
story: EP-maintenance-US-0015-tactical-battle-feedback
title: Читаемость тактического боя и обратная связь интерфейса
stage: approved
implementation_status: implemented
---

# Читаемость тактического боя и обратная связь интерфейса

[Эпик «Исправления и доделки»](../../Documentation.md)

## Запрос

Уточнить размер фишек при зуме, отклик кнопок, подписи пилотов и представление движения, залпа и потерь по замечаниям и снимкам от 13 ноября 2026 года.

## Тикеты и критерии

- [AC1. Масштаб фишек](./EP-maintenance-US-0015-TK-0001-scale-unit-markers/EP-maintenance-US-0015-TK-0001-scale-unit-markers.md). Фишки поля и призраки конечных точек уменьшаются до ⅓ размера при минимальном зуме и достигают прежнего размера при максимальном. Размер текста фиксирован.
- [AC2. Наведение на кнопки](./EP-maintenance-US-0015-TK-0002-hover-battle-buttons/EP-maintenance-US-0015-TK-0002-hover-battle-buttons.md). Доступные кнопки боя подсвечиваются и включают курсор действия. Пустая область и недоступные команды сохраняют обычный курсор.
- [AC3. Число целей рядом с оружием](./EP-maintenance-US-0015-TK-0003-show-target-count/EP-maintenance-US-0015-TK-0003-show-target-count.md). У каждого пилота при планировании стрельбы показывается число доступных целей рядом с названием оружия, в том числе ноль.
- [AC4. Направление торпеды](./EP-maintenance-US-0015-TK-0004-align-torpedo-flight/EP-maintenance-US-0015-TK-0004-align-torpedo-flight.md). Горизонтальный кадр спрайта торпеды поворачивается на фактический угол полёта. Сохраняется цвет стороны.
- [AC5. Маршруты остальных дронов](./EP-maintenance-US-0015-TK-0005-dim-unselected-paths/EP-maintenance-US-0015-TK-0005-dim-unselected-paths.md). В фазе планирования движения линии невыбранных дронов тоньше и прозрачнее и заканчиваются стрелкой по курсу.
- [AC6. Попадание в корму](./EP-maintenance-US-0015-TK-0006-explain-rear-hits/EP-maintenance-US-0015-TK-0006-explain-rear-hits.md). Результат кормового попадания отмечает обход брони. Прямое попадание и кормовое добивание используют существующие боевые правила.
- [AC7. Общий спрайт обломков](./EP-maintenance-US-0015-TK-0007-draw-drone-wrecks/EP-maintenance-US-0015-TK-0007-draw-drone-wrecks.md). На поле уничтоженные дроны обеих сторон используют один серый спрайт и серое имя без показателей брони и корпуса.
- [AC8. Анимация ложной цели](./EP-maintenance-US-0015-TK-0008-animate-decoy-interception/EP-maintenance-US-0015-TK-0008-animate-decoy-interception.md). В Tabletop ловушка отлетает от дрона во время залпа. При промахе после её срабатывания торпеда попадает в ложную цель сбоку от дрона.
- [AC9. Сброс выбора при входе в движение](./EP-maintenance-US-0015-TK-0009-clear-movement-selection/EP-maintenance-US-0015-TK-0009-clear-movement-selection.md). При входе в фазу движения Tabletop ни один дрон игрока не выбран и панель команд закрыта. Щелчок по дрону или Tab снова выбирают пилота.
- [AC10. Панели результатов пилотов до движения](./EP-maintenance-US-0015-TK-0010-retain-pilot-salvo-panels/EP-maintenance-US-0015-TK-0010-retain-pilot-salvo-panels.md). После прилёта торпеды под карточкой стрелявшего пилота открывается его собственный результат. Ранее показанные результаты остаются на месте во время следующих выстрелов и на экране результатов. При переходе к движению они закрываются. Пропуск полёта показывает результаты всех состоявшихся выстрелов.

## Проверки

Сборка Client и Client.Tests, scoped `dotnet format whitespace` и `git diff --check` прошли. Визуально сверены 23 обновлённых эталона Classic и Tabletop. Прошли все 563 теста тактического боя. Прогон записан в `D:/DeepSpaceSaga/artifacts/validation/maintenance-tactical-feedback/20261113-feedback-01/final/tactical.trx`. Ручная проверка в запущенной игре остаётся открытой.
