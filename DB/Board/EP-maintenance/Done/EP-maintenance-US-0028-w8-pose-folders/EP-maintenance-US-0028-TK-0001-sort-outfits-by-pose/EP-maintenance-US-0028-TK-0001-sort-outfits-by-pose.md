---
epic: EP-maintenance
story: EP-maintenance-US-0028-w8-pose-folders
ticket: EP-maintenance-US-0028-TK-0001-sort-outfits-by-pose
title: Разложить костюмы W8 по позам
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
serves: [AC1]
---

# Разложить костюмы W8 по позам

[История](../EP-maintenance-US-0028-w8-pose-folders.md)

Перенесены GRAY, GREEN и IVORY. Компоновщик Antropik загружает спрайт по пути выбранной позы из `pose.json → outfits`. Для будущих тел пленного создан каталог `Prisoner`. [Порядок сборки W8](../../../../../Documentation/06-Tooling/ThreeQuarterCharacterSkinTone.md).

## Проверки

Содержимое трёх PNG сохранено побайтно. Сравнение 195 кадров до и после переноса (13 доступных портретов, 3 костюма, 5 эмоций) дало попиксельное совпадение. Атлас AL7Q2B отсутствовал уже до переноса. Проверки выполнены в автономном компоновщике.

После удаления костюмов в обоих реестрах остался SATIN. Для новых ID промпт требует две именные маски кожи, по одной на позу. Семь производственных PNG и README пленного перенесены в `DSS-Images/Characters/W8/Antropik/prisoner/Templates/`. Эталоны и проверочные маски перечислены в производственном `template.json`. Хеши перенесённых PNG сохранены. В клиентском каталоге остались `pose.json` и `skin-mask-satin.png`. Удалены маски GRAY и IVORY и отдельная копия альфа-канала эталона. Проверены пути и формат масок, 30 автономных сборок SATIN и валидатор портрета SN7L4D.
