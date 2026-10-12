# Defeat

Экран реализован EP-0008 и открывается по authoritative SessionStatus.Defeated.

- Доступны Load и Main Menu. ClockSpeed0 фиксирован; отмена/закрытие/Escape не возвращают игрока в погибший мир.
- Load открывает существующий файловый выбор. Cancel/Escape возвращают Defeat. Ошибка формата или содержимого локализована, сохраняет исходную сессию и позволяет повторить попытку.
- Успешный Load прежнего save создаёт новую Running-сессию на паузе; старые journal effects не воспроизводятся.
- Save/F5 погибшей сессии запрещён Engine и LocalClient до файловой записи. Старый слот не изменяется при отказе.
- Main Menu завершает работу с текущей сессией обычным переходом.

[Канонический контракт](../../01-Requirements/EngineRequirements.md#phase-three-combat), [Load](Load.md), [Save](Save.md), [проверки](../../04-Engineering/PhaseThreeCombatAcceptance.md). Headless navigation/file tests не заменяют native приёмку; дальнейшие игровые запуски отложены пользователем.
