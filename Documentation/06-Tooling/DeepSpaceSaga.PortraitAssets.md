# Инструмент подготовки портретов

`tools/DeepSpaceSaga.PortraitAssets` готовит и проверяет слои портретов.
Доступные операции и аргументы определяет Program.cs; не используй старый
список вызовов как гарантию интерфейса. Runtime manifest и PNG остаются в
наборе персонажей, авторские исходники и проверки — вне Documentation.

Проверяй размеры, alpha, названия, геометрию и reconstruct/composition по
контракту выбранного W4/M4 набора. Не выдавай визуально похожую голову за
совместимый компонент без проверки совмещения. Скрипты навыков и reference
assembly contract относятся к workflow генерации, не к игровому Engine.
См. [PortraitGenerator](../03-Design/PortraitGenerator.md).

Источники: [Program.cs](../../tools/DeepSpaceSaga.PortraitAssets/Program.cs), [PortraitAssetRepository.cs](../../src/DeepSpaceSaga.Client/Portraits/PortraitAssetRepository.cs).
