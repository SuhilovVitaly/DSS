# Команды корабля

> **Контракт EP-0013.** EP-0013: keyboard turnHold/speedHold работают независимыми осями; кнопочные циклы остаются отдельными командами и отменяются ручным удержанием. Текущие клавиши и pending/receipt семантика заданы в [ManualManeuver](../../04-Engineering/ManualManeuver.md); отладочный overlay применяется только авторитетной командой из [DebugTuningPanel](../../04-Engineering/DebugTuningPanel.md).

Левая часть тактического экрана группирует команды по собственным модулям.
Она показывает availability, pending/result и модульные damage/disabled/destroyed
сведения; длинные тексты переносятся и прокручиваются. Справа остаются данные
и изображение выбранного объекта. Команда отправляется через session и получает
авторитетное подтверждение. Кнопка/preview не должны сами менять мир.
Правила point aiming, manual countermeasure, reload и выход из aiming заданы
в [PointAimCombat](../../04-Engineering/PointAimCombat.md).

При нехватке места оружие получает приоритет перед Navigation; явно выбранная
группа и ручные сворачивания сохраняются. Заголовки с дальностью и числом reload
остаются доступны. Profile selection принадлежит отдельному owner/module и
сбрасывается при новой сессии. Подготовительный прицел доступен и у временно
неработающего аппарата; блокирующая причина запрещает отправку пуска.
Armor/Structure имеют подписанные полосы и current/max tooltip, critical marker
объясняется при наведении, n/L использует предел собственного корабля из Engine.
Длительности, включая engine times и прогноз подлёта, выражены в real seconds
при текущем K либо последнем K перед паузой. Hover не замещает readiness/reload.

Источники: [GameSession](../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession), [Commands](../../../src/DeepSpaceSaga.Client/Data/Commands).
