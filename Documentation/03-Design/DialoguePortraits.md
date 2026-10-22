# Портреты в диалогах

DialoguePortraitComposer использует общие PortraitRenderer/PortraitAssetRepository:
целая голова с шеей и костюм складываются на общем холсте, затем масштабируются
для диалога. Активные наборы — W4/M4. Костюм определяется стабильным person key;
смена реплики и повторное открытие не должны случайно менять внешность.

Явный portraitImage сохраняет личность. Автовыбор головы связан с session seed
и identity; полученный относительный путь входит в сохранение. Изменение состава
PNG не должно менять уже сохранённый конкретный путь. Legacy W/M пути имеют
совместимое отображение в текущих наборах; отсутствующий файл не должен падать.
Композиция производится в памяти, отдельные готовые диалоговые PNG не создаются.

Источники: [DialoguePortraitComposer.cs](../../src/DeepSpaceSaga.Client/UI/Portraits/DialoguePortraitComposer.cs), [PortraitAssetRepository.cs](../../src/DeepSpaceSaga.Client/Portraits/PortraitAssetRepository.cs).
