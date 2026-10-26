# Женский набор W4

Набор хранится в Images/Persons/W4. `parts.json` и
`portrait-style.json` задают состав и визуальный/координатный контракт.
PNG не следует подменять изображением с другой геометрией шеи/костюма.
Полный head-and-neck слой должен совмещаться с costume без runtime подгонки.
Пути конкретных голов стабильны и могут храниться в сохранениях.

Client.csproj требует manifests W4/M4 при сборке и копирует runtime PNG/JSON,
исключая авторские Sources/Generated. Размещение файлов в другом наборе не
меняет явно сохранённый portraitImage. Общие правила — [PortraitGenerator](PortraitGenerator.md).

Подготовка головы поддерживается переносимым pwsh7+/Skia workflow;
prepare.ps1 принимает gender и явные output/preview directories. Проверки
alpha, непрерывности шеи и трёх воротников обязательны до сохранения.
[Инструмент и команды](../06-Tooling/DeepSpaceSaga.PortraitAssets.md).

Источники: [parts.json](../../../src/DeepSpaceSaga.Client/Images/Persons/W4/parts.json), [portrait-style.json](../../../src/DeepSpaceSaga.Client/Images/Persons/W4/portrait-style.json), [DeepSpaceSaga.Client.csproj](../../../src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj).
