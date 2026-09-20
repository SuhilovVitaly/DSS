# Подготовка портретных деталей

`DeepSpaceSaga.PortraitAssets` — утилита для разработки графического набора, а не часть работающей игры. Она выделяет слои из подготовленных исходников, подгоняет детали к овалам, записывает каталог и создаёт обзоры для проверки. Утилита использует модели и рендерер клиента, чтобы проверочные изображения соответствовали игре. Клиент не ссылается на утилиту и не запускает её.

Игра читает готовые PNG и JSON из `Images/Persons/W/PortraitGenerator` рядом со своим exe. Рабочий набор, исходники и тестовый эталон хранятся в `src/DeepSpaceSaga.Client/Images/Persons/W/PortraitGenerator`; для запуска и обычной сборки клиента утилита не нужна.

Из корня DSS:

```powershell
dotnet run --project tools/DeepSpaceSaga.PortraitAssets -- validate src/DeepSpaceSaga.Client/Images/Persons/W/PortraitGenerator
dotnet run --project tools/DeepSpaceSaga.PortraitAssets -- features src/DeepSpaceSaga.Client/Images/Persons/W/PortraitGenerator
```

`validate` проверяет каталог и изображения, `features` заново извлекает 40 деталей из существующих исходников. Полный список команд и описание набора — в `Documentation/03-Design/PortraitGenerator.md`. Исходники, обзоры и эталон исключены из поставки клиента.
