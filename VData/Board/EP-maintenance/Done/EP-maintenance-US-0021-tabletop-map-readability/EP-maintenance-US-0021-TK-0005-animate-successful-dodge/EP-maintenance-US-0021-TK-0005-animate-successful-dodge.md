---
epic: EP-maintenance
story: EP-maintenance-US-0021-tabletop-map-readability
ticket: EP-maintenance-US-0021-TK-0005-animate-successful-dodge
title: Анимация успешного уклонения
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC5]
---

# Анимация успешного уклонения

[История](../EP-maintenance-US-0021-tabletop-map-readability.md)

При промахе по уклоняющемуся дрону Tabletop плавно сдвигает дрон поперёк траектории. Торпеда пролетает за точку прицеливания примерно на два с половиной корпуса и исчезает. Дрон возвращается в исходную точку к концу показа выстрела. Анимация использует рассчитанный исход и не меняет позу движка. Попадание, абсорбция, ложная цель и пропуск показа используют штатное представление.

В Tabletop фиолетовое кольцо обозначает приказ на этапе планирования. Во время показа залпа и на экране результатов кольцо вокруг уклоняющегося дрона скрыто.

## Проверки

Прошёл 51 headless-тест Client в Release, включая последовательные кадры уклонения, пролёт на 2,5 корпуса, возврат дрона, пропуск показа и визуальные эталоны. Результаты в `D:/DeepSpaceSaga/artifacts/validation/tabletop-dodge/20261113-01/dodge.trx`.

Прошёл 41 headless-тест Client в Release. Проверено, что кольцо видно при планировании и скрыто во время уклонения и после залпа. Результаты в `D:/DeepSpaceSaga/artifacts/validation/tabletop-dodge-ring/20261113-01/dodge-ring.trx`.
