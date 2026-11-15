# Источники и проверки тактического боя

Базовый каталог переносится из [VData/Planning/V4.0](../../../VData/Planning/V4.0/) Markdown. Initiative присоединяется только из runtime pilots.json по точному ordinal ID. Standalone V4 и его Reference сохранены как историческая одновременная модель и не являются oracle текущей инициативной стрельбы и расширений EP-0020.

Из корня DSS:

```powershell
./tools/tactical-battle/Export-Content.ps1 -Check
python tools/tactical-battle/export_references.py --check
```

Первый инструмент проверяет [runtime JSON](../../../src/DeepSpaceSaga.Client/Data/TacticalBattle/), второй — исторические [эталоны V4](../../../VData/Planning/V4.0/Reference/). Export-Content поддерживает абсолютные -OutputDirectory и -PilotInitiativeFile для isolated fixtures. Initiative source строго валидируется до любых записей; -Check ничего не пишет. Самодостаточный PowerShell harness tools/tactical-battle/tests/Export-Content.Tests.ps1 проверяет отрицательные случаи. ContentSync независимо сверяет базовые поля V4 и initiative extension. Геометрические/сборочные primitive fixtures сохраняют назначение; для генерации геометрического эталона используйте Linux/glibc и версию Python из заголовка JSON.

Новая механика проверяется ручными expected cases, ограниченным независимым oracle (гарантированное попадание/промах, фиксированный урон, jam) и интеграционным direct/API сравнением. Oracle не вызывает production Salvo/CheckVictory и не заменяет проверку всей боевой математики. Массовая parity нового правила сравнивает детерминированные пути, а не старые V4 итоги.

Обычный Engine-прогон исключает `Category=LongRunning` и `Category=Balance`. Балансовые EvasionCalibrationTests/DecoyReportTests/AnomalyReportTests запускаются отдельно; их отчёты не объявляются универсальной приёмкой баланса. Для серии запускайте по одному точному FQN класса SeriesParityTests: Dogfight, Raid, Defence, Balanced, Commander — seeds1…1000 каждый. Сохраняйте signatures итогового состояния и RNG. Каждый процесс ограничивается 300000 ms с завершением дерева; точные команды и результаты находятся на [Board EP-0020](../../Board/EP-0020-tactical-battle-mechanics/ExecutionStatus.md). TRX, stdout/stderr и fixtures направляйте абсолютными путями в уникальный D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/; создавайте каталог заранее. Historical результаты EP-0015 остаются в его [FinalValidation](../../Board/EP-0015-tactical-carrier-battle/FinalValidation.md).

См. [контракт боя](../04-Engineering/TacticalBattle.md).
