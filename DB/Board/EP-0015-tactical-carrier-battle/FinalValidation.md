# EP-0015 — итоговая и нативная проверка

Заготовка для US-0016 (протокол нативной проверки) и итогового review эпика. Automated, native и human/product приёмка фиксируются раздельно; каждый пункт — PASS / FAIL / NOT RUN с причиной.
## Замеры Release — PASS

Windows VDI, .NET 8.0.26. Полный Engine turn (планирование, движение, залп), 2 029 шагов: p95 0,4454 мс; max 1,5797 мс. Прогретый aimAt, 1 000 вызовов: p95 0,0130 мс; max 0,2442 мс. Полная серия из 1 000 боёв: 1,1292 с. Отрисовка и flush настоящего TacticalBattleScreen в CPU Skia 1920×1080: 1 400 кадров на семи состояниях; худший p95 4,4156 мс, максимум 9,4828 мс. Это не замер swap, GPU исполнения или дисплея.

Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-tactical-release-performance-01/performance.json.
