# Источники и проверки тактического боя V4

Рабочий источник — [VData/Planning/V4.0](../../../VData/Planning/V4.0/). Каталог V4 переносится в клиент из Markdown. Отдельная справочная реализация для сборки, прогона или повторного создания эталона не нужна.

Из корня DSS:

```powershell
./tools/tactical-battle/Export-Content.ps1 -Check
python tools/tactical-battle/export_references.py --check
```

Первый инструмент проверяет [runtime JSON](../../../src/DeepSpaceSaga.Client/Data/TacticalBattle/), второй — [независимые эталоны](../../../VData/Planning/V4.0/Reference/) из опубликованных таблиц. Для генерации геометрического эталона используйте Linux/glibc и версию Python, записанную в заголовке JSON. Таблицы боя сохраняют доступную точность: целые счётчики сравниваются точно, десятичный trace и средние — с опубликованной точностью. Среднее значение не восстанавливает исходную целую сумму.

Обычный тактический Engine-прогон исключает `Category=LongRunning`. Для проверки серии запустите одну тактику за раз: `FullyQualifiedName~SeriesParityTests.Dogfight`, `Raid`, `Defence`, `Balanced`, `Commander`. У каждого прогона свой абсолютный каталог результатов под workspace artifacts. Фикстуры также проверяют пошаговый trace, итоги отдельных боёв и геометрические векторы. Текущие результаты приёмки хранятся на [Board](../../Board/EP-0015-tactical-carrier-battle/FinalValidation.md).

См. [контракт боя](../04-Engineering/TacticalBattle.md).