---
epic: EP-maintenance
story: EP-maintenance-US-0028-w8-pose-folders
ticket: EP-maintenance-US-0028-TK-0002-prisoner-shoulder
title: Восстановить правое плечо костюмов пленного W8
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
serves: [AC2]
---

# Восстановить правое плечо костюмов пленного W8

[История](../EP-maintenance-US-0028-w8-pose-folders.md)

Правое плечо дорисовано у GREEN, GRAY и IVORY по одному контуру. Направляющая, маска и контрольная сумма GREEN в шаблоне обновлены.

## Проверки

Альфа костюмов совпадает по всему холсту. RGBA на защищённом участке шеи сохранён. Пятнадцать кадров KR4M2V собраны из сохранённых PNG с применением `skinTone` и посадкой атласа в (48, 416).

Пилот LI8M2Q / SURVEYSMALL проверен в позе пленного. Лысая контрольная голова, манекен SMALL и отдельная разметка используют холст 416×747. Все пять ячеек закрывают 60 контрольных пикселей стыка; RGBA защищённой шеи и альфа восстановленного плеча совпали с эталоном. Пилот хранится в рабочем каталоге Antropik. [Сборка с манекеном](D:/DeepSpaceSaga/artifacts/validation/w8-prisoner-mannequin/20261124-li8m2q/mannequin-and-small.png) · [Стык шеи](D:/DeepSpaceSaga/artifacts/validation/w8-prisoner-mannequin/20261124-li8m2q/neck.png).
