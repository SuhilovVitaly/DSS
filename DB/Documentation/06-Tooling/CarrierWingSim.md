# Источники и проверки тактического боя

Базовый каталог переносится из исторических Markdown V4 (`VData/Planning/V4.0`, отсутствующих в текущем checkout). Initiative присоединяется только из runtime pilots.json по точному ordinal ID. Standalone V4 и его Reference сохранены как историческая одновременная модель и не являются oracle текущей инициативной стрельбы и расширений EP-0020.

Legacy export-команды требуют восстановленного исторического V4 source tree; отсутствие этого дерева не означает ошибку runtime-каталога. Из корня DSS после предоставления источников:

```powershell
./tools/tactical-battle/Export-Content.ps1 -Check
python tools/tactical-battle/export_references.py --check
```

Первый инструмент проверяет [runtime JSON](../../../src/DeepSpaceSaga.Client/Data/TacticalBattle/), второй — исторические эталоны V4 (копия для тестов: [Fixtures/V4](../../../tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/Fixtures/V4/)). Export-Content поддерживает абсолютные -OutputDirectory и -PilotInitiativeFile для isolated fixtures. Initiative source строго валидируется до любых записей; -Check ничего не пишет. Самодостаточный PowerShell harness tools/tactical-battle/tests/Export-Content.Tests.ps1 проверяет отрицательные случаи. ContentSync независимо сверяет базовые поля V4 и initiative extension. Геометрические/сборочные primitive fixtures сохраняют назначение; для генерации геометрического эталона используйте Linux/glibc и версию Python из заголовка JSON.

Новая механика проверяется ручными expected cases, ограниченным независимым oracle (гарантированное попадание/промах, фиксированный урон, jam) и интеграционным direct/API сравнением. Oracle не вызывает production Salvo/CheckVictory и не заменяет проверку всей боевой математики. Массовая parity нового правила сравнивает детерминированные пути, а не старые V4 итоги.

Обычный Engine-прогон исключает `Category=LongRunning` и `Category=Balance`. Балансовые EvasionCalibrationTests/DecoyReportTests/AnomalyReportTests запускаются отдельно; их отчёты не объявляются универсальной приёмкой баланса. Для серии запускайте по одному точному FQN класса SeriesParityTests: Dogfight, Raid, Defence, Balanced, Commander — seeds1…1000 каждый. Сохраняйте signatures итогового состояния и RNG. Каждый процесс ограничивается 300000 ms с завершением дерева; точные команды и результаты находятся на [Board EP-0020](../../Board/EP-0020-tactical-battle-mechanics/ExecutionStatus.md). TRX, stdout/stderr и fixtures направляйте абсолютными путями в уникальный D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/; создавайте каталог заранее. Historical результаты EP-0015 остаются в его [FinalValidation](../../Board/EP-0015-tactical-carrier-battle/FinalValidation.md).

См. [контракт боя](../04-Engineering/TacticalBattle.md).

Для ограниченного прогона калибровки Evasion каждый penalty10…40 имеет20 непересекающихся theory rows по50 seeds (1…50,51…100,…951…1000); внутри каждого остаются все пять тактик и сравнение direct/observed. Decoy имеет10 rows по100 seeds и пять тактик. Полный corpus требует всех rows, а не одного фильтра. Проверяйте состав через --list-tests и сохраняйте матрицу ID→результат. CSV калибровки содержит доли отдельного диапазона: их простое среднее не является общей долей всего penalty. Для общей калибровки нужны исходные числители/знаменатели или отдельная агрегация. SeriesParity и Anomaly сохраняют полные1000 seeds на тактику.
