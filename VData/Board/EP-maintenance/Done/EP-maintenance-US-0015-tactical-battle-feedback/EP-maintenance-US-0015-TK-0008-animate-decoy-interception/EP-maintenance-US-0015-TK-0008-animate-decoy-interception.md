---
epic: EP-maintenance
story: EP-maintenance-US-0015-tactical-battle-feedback
ticket: EP-maintenance-US-0015-TK-0008-animate-decoy-interception
title: Анимация перехвата торпеды ложной целью
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC8]
---

# Анимация перехвата торпеды ложной целью

[История](../EP-maintenance-US-0015-tactical-battle-feedback.md)

## Критерии

- В воспроизведении залпа Tabletop ложная цель вылетает от дрона, когда к нему приближается торпеда. Это видно и у вражеских дронов.
- При промахе после срабатывания ловушки торпеда изгибает траекторию, а вспышка появляется в точке встречи с ложной целью сбоку от дрона.
- При попадании торпеда достигает дрона. При абсорбции вспышка остаётся в тепловой аномалии. Пропуск анимации сразу открывает результаты.
- Движок продолжает определять шанс, урон и списание зарядов. При планировании вражеский приказ скрыт.

## Проверки

Проверка TabletopSalvoTests использует фактический выстрел по вражескому дрону с ловушкой и сравнивает положение ложной цели, торпеды и вспышки в последовательных кадрах.

Прошли 58 headless-тестов Release, включая эталоны Tabletop. Просмотрены кадры подлёта и вспышки. Результаты в `D:/DeepSpaceSaga/artifacts/validation/tabletop-decoy/20261113-decoy-01/verified/decoy.trx`.
