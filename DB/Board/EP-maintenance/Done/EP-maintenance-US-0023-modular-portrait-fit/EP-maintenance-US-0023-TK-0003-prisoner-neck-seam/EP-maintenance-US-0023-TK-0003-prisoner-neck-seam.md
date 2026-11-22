---
epic: EP-maintenance
story: EP-maintenance-US-0023-modular-portrait-fit
ticket: EP-maintenance-US-0023-TK-0003-prisoner-neck-seam
title: Совместить стык шеи пленного с шаблоном
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
serves: [AC3]
---

# Совместить стык шеи пленного с шаблоном

[История](../EP-maintenance-US-0023-modular-portrait-fit.md)

Проверили сохранённые атласы PR7N2K и KR4M2V. В KR4M2V исправили альфу стыка во всех пяти ячейках по `portraitAlpha`. За пределами `neckProtect` рисунок сохранён.

## Проверки

У PR7N2K изначально было ноль расхождений. У KR4M2V после исправления ноль расхождений вместо 443 на эмоцию. Альфа всех эмоций каждого атласа совпадает с его нейтральной ячейкой. Оба атласа стойки подтверждены по SHA-256. Собраны все 15 сочетаний KR4M2V.
