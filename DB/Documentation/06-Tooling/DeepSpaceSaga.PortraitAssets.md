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

Переносимые операции `export-tactical` и `prepare-portrait` вызываются через
[Export-TacticalButtonAssets.ps1](../../../tools/Export-TacticalButtonAssets.ps1) и
[prepare.ps1](../../../src/DeepSpaceSaga.Client/Images/Persons/W4/Portraits/character-generator/scripts/prepare.ps1).
Нужны PowerShell7+ и .NET SDK8+. Растеризация использует тот же Skia на Windows/Linux.
prepare определяет repository root, принимает OutputDirectory/PreviewDirectory,
проверяет square input, анатомические диапазоны, outer alpha, сплошную шею
и три воротника до сохранения. Preview по умолчанию создаётся в workspace
artifacts/validation/portrait-preparation; после визуальной проверки удалите
свой preview. Export сохраняет manifest dimensions и optional Install.
Диагностический прогон задаёт outputs только под workspace artifacts;
[Linux prerequisites](Ubuntu.md) и исходные шапки определяют команды.

Источники: [Program.cs](../../../tools/DeepSpaceSaga.PortraitAssets/Program.cs), [PortraitAssetRepository.cs](../../../src/DeepSpaceSaga.Client/Portraits/PortraitAssetRepository.cs).
