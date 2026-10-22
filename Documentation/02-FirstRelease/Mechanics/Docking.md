# Стыковка

Approach, синхронизация и Dock — отдельные действия. Приближение к движущейся
станции не выдаёт автоматически доступ к Trade. Engine проверяет target,
положение/состояние и станционное разрешение. Диалог может предоставлять
разрешённый docking effect; один disabled UI не является защитой.
Пристыкованный корабль следует станции. Undock начинает физическое продолжение
рейса с согласованной voyage/fuel моделью. Базы ИИ дают station_access_denied.
Диалоговые эффекты исполняются транзакционно и не повторяются из-за redraw.

Источники: [DialogueEffectTransaction.cs](../../../src/DeepSpaceSaga.Engine/Dialogue/DialogueEffectTransaction.cs), [StationSecuritySystem.cs](../../../src/DeepSpaceSaga.Engine/Dialogue/StationSecuritySystem.cs).
