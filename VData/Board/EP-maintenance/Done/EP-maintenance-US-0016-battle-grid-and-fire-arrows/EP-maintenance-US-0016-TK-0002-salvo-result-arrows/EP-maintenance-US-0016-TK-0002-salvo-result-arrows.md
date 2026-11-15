---
epic: EP-maintenance
story: EP-maintenance-US-0016-battle-grid-and-fire-arrows
ticket: EP-maintenance-US-0016-TK-0002-salvo-result-arrows
title: Стрелки результатов залпа
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC2]
---

# Стрелки результатов залпа

[User story](../EP-maintenance-US-0016-battle-grid-and-fire-arrows.md)

Стрелки результатов в Tabletop тоньше и прозрачнее. Цвета различают попадания сторон и промахи. Цифры урона и подписи исхода на поле компактнее.

## Проверки

TabletopSalvoTests проверяет появление стрелок и подписей по времени попадания. Эталонные кадры показывают различия между попаданиями и промахами.

В Tabletop шрифт плашек попаданий уменьшен с 12,5 до 9,5 логических пикселя, а шрифт остальных плашек результатов — с 10,5 до 9. Обновлены эталоны обычного залпа и залпа с ловушкой.

Итоговый прогон боя и сетки прошёл 590 из 590 проверок. Форматирование и git diff --check прошли. Плашки сверены на растровом кадре. Окно игры не запускалось. Результаты — D:/DeepSpaceSaga/artifacts/validation/maintenance-salvo-badges/20261113-badges-01/final/salvo-badges.trx.

Линия завершённого выстрела и текст результата видны только при наведении на стрелявший дрон. Подпись показывает цель, шанс и подробный исход. После ухода курсора или выхода из показа залпа линия и текст исчезают.

Прошли 50/50 headless-проверок Release, включая эталоны Tabletop. Результаты: D:/DeepSpaceSaga/artifacts/validation/tabletop-salvo-hover/20261113T160239889Z/verify.

Прошли 30/30 headless-проверок в Release, включая показ только при наведении и сравнение эталонов. Результаты: D:/DeepSpaceSaga/artifacts/validation/tabletop-hover-only/20261113T161259013Z/verify.

При попадании над целью появляется отлетающее число урона. Линия завершённого выстрела и подробный исход видны при наведении на стрелявший дрон.

Прошли 48/48 headless-проверок Release, включая эталоны Tabletop. Результаты: D:/DeepSpaceSaga/artifacts/validation/tabletop-floating-damage/20261113T161825398Z/verify.

После залпа панель при наведении на стрелявший дрон показывает шанс попадания и фактический бросок. Для поглощённой торпеды показан бросок поглощения.

Прошли 46/46 клиентских headless-проверок в Release. Результаты: D:/DeepSpaceSaga/artifacts/validation/tabletop-disabled-roll/20261113T163726168Z/verify.
