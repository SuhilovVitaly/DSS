# Проверки — 2026-10-10

## Основная рабочая копия

`D:/DeepSpaceSaga/DSS`, `base-fight`, `02c89d4`. Команда:

```powershell
dotnet test DeepSpaceSaga.sln --no-restore --filter 'FullyQualifiedName!~CorrectnessCorpus' --logger 'trx;LogFileName=review-ep1-4-20261010.trx'
```

| Проект | Passed | Failed |
|---|---:|---:|
| Contracts | 170 | 0 |
| Motion | 141 | 0 |
| Engine | 1661 | 0 |
| Client | 1734 | 1 |
| Performance tooling | 4 | 0 |
| EconomyBalance | 58 | 0 |
| Всего | 3768 | 1 |

Общий exit=1. Failed: `StationMarketDemoContentTests.Every_demo_station_docks_and_trades_via_session` для `SPC-MARKET-INDUSTRIAL`, `NotSupportedException` из async-iterator DisposeAsync после 15 секунд. Изолированный повтор всех пяти вариантов: **5/5 PASS**, `review-ep1-4-client-recheck.trx`. Общий прогон от этого не становится полностью зелёным; см. F05.

Большие `CorrectnessCorpus` тесты намеренно исключены: production не менялся; исторические 4800-world корпуса не выдаются за свежие. TestResults находятся в соответствующих test-project directories; компактная свежая сводка сохранена в `test-summary.json`.

## Отдельная ветка EP-0004

`D:/DeepSpaceSaga/DSS-EP-0004`, `76f500a`. Engine Release, фильтр MapEnvironmentSave / SeededAi / MovingTerritory / SeededEnvironment / SeededAbandoned / AiTrade: **18/18 PASS**. Это focused regression, не весь solution и не интегрированная с base-fight сборка.

Client Release, фильтр EnvironmentFieldRendering / TerritoryRendering / AiMap / Poi: **44/44 PASS** (`review-ep4-client-20261010.trx`). Это автоматическая Client-проверка, не native acceptance.

## Воспроизводимые probes

```powershell
dotnet run --no-restore --project Documentation/04-Engineering/EpicReview20261010/Probes -- D:/DeepSpaceSaga/DSS
```

Новый отдельный проект потребовал `dotnet restore ... --ignore-failed-sources`; первый sandbox restore не мог прочитать пользовательский NuGet.Config, разрешённый повтор прошёл. Production-код и существующие тесты не редактировались.

```text
COLLAPSED_CLUSTER: ACCEPTED; CLUSTER-4-STATION-1/CLUSTER-4-STATION-10; separation=0
UNBOUNDED_CONFIG: ACCEPTED asteroids=2147483647, attempts=64; generation deliberately not executed
UNBOUNDED_CONFIG: ACCEPTED asteroids=24, attempts=2147483647; generation deliberately not executed
CUSTOM_ENGINE: generated; configuredMaxSpeedKmS=1.4; snapshotMaxSpeedKmS=null
```

Эти результаты подтверждают F02–F04. Принятие экстремального config проверено без запуска миллиардов итераций; OOM или время зависания не измерялись.

## Полная матрица экономической приёмки

```powershell
dotnet run --no-restore --project tools/DeepSpaceSaga.EconomyBalance -- D:/DeepSpaceSaga/DSS D:/DeepSpaceSaga/DSS/Documentation/04-Engineering/EpicReview20261010/balance-report.json
```

Exit **1**, `status=violations`, штатная acceptance corpus **24 cases**. 5784 hourly samples, 12840 стратегий: 6784 completed, 4512 rejected, 1544 partial, 0 unknown. Все 24 continuous/save-load hashes совпали. Это подтверждает continuation, но не баланс.

| Нарушение | Количество |
|---|---:|
| route_margin_dominance | 108 |
| short_margin_band | 12 |
| event_did_not_change_leader | 2 |
| route_always_best | 2 |
| long_upgrade_crossover | 1 |
| Всего | 125 |

[Компактные результаты с каждым нарушением](balance-summary.json). Полный исходный JSON (182937091 bytes) сохранён без потерь в [balance-report.json.gz](balance-report.json.gz); его SHA256 указан в summary. Незапакованный оригинал перемещён в `D:/DeepSpaceSaga/review-ep1-4-work/balance-report.json`, чтобы не помещать 183 MB JSON в документационный индекс. Историческое число 141 не является результатом этого запуска.

## Не выполнялось

Новый native/UI playthrough, измерение GPU execution/physical scanout, повтор full solar/cluster/full-map correctness corpus и проверка ещё не существующей объединённой ветки. Исторические EP-0002/3 native PASS и EP-0004 native FAILED процитированы из сохранённых отчётов, а не воспроизведены сегодня.
