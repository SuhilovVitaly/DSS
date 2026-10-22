# Портретные наборы и композиция

Runtime использует manifests parts.json и portrait-style.json рядом с PNG.
Голова, шея и костюм имеют общий координатный контракт: нельзя независимо
центрировать или обрезать компоненты после подготовки. Альфа, collar overlap,
якоря и размеры проверяются по конкретному набору, а не по общей анатомической схеме.

[W4](PortraitGeneratorW4.md) и [M4](PortraitGeneratorM4.md) используются в диалогах.
PortraitAssetRepository загружает набор, renderer составляет выбранные слои,
TempCharacterImageScreen предоставляет рабочую UI-поверхность генератора.
Для новых компонентов используй установленный character-generator и его
assembly contract; рабочая копия инструкции находится рядом с portrait assets.
Не дублируй численные socket coordinates в нескольких документах.

Исходные staging PNG и QA composites не являются дополнительными runtime
наборами. Генерация, экспорт и проверка ассетов описаны в
[PortraitAssets](../06-Tooling/DeepSpaceSaga.PortraitAssets.md).

Источники: [PortraitAssetRepository.cs](../../src/DeepSpaceSaga.Client/Portraits/PortraitAssetRepository.cs), [PortraitRenderer.cs](../../src/DeepSpaceSaga.Client/Portraits/PortraitRenderer.cs), [TempCharacterImageScreen.cs](../../src/DeepSpaceSaga.Client/UI/Screens/TempCharacterImage/TempCharacterImageScreen.cs), [SKILL.md](../../src/DeepSpaceSaga.Client/Images/Persons/W4/Portraits/character-generator/SKILL.md).
