# Graphify: торговля и Солнечная система — 2026-10-10

Граф основной копии: [graph.json](../../src/graphify-out/graph.json), [HTML](../../src/graphify-out/graph.html), [отчёт](../../src/graphify-out/GRAPH_REPORT.md), [проверка](../../src/graphify-out/verification.json). Исходная ветка `base-fight / 02c89d4` плюс исправления F01–F05 и локальная интеграция EP-0004. Это навигация по исходникам и документам, не свидетельство прохождения AC.

AST обновлён для всех C# файлов src/tests/tools (без bin/obj/cache). Документы и Board EP-0001–0004/EP-0007 имеют структурные узлы и ссылки; семантика неизменённых документов переиспользуется только при совпадении прежнего SHA256. Сорок три ключевых документа обработаны тремя Graphify-агентами; выборочное покрытие больших requirements/исторических приложений явно указано в semantic-coverage.json. Остальные новые/изменённые документы имеют структурное покрытие, без выдуманной полной семантической обработки. Непроверенные старые semantic edges удалены.

Корпус: 907 файлов (584 C#, 323 Markdown): tests313, Board246, src247, Documentation77, tools24. Граф: **14935 узлов, 37184 связей, 517 сообществ**. Превышены пороги 500 файлов / 5000 узлов; полный JSON сохранён, HTML автоматически показывает агрегированный обзор 517 сообществ, а не все отдельные символы. Полные input hashes и фактические счётчики в input-evidence.json / counts.json. Бинарные медиа, TestResults, raw JSON evidence, игровые изображения и сторонние рабочие копии не индексируются. Этот файл исключён из собственного hash-набора.

EP-0004 интегрирован в основную рабочую копию и включён в AST/документальный граф вместе с исправлениями. Полный исторический граф этой поставки остаётся в `D:/DeepSpaceSaga/DSS-EP-0004/graphify-out`; это отдельный граф с собственной базой и приёмкой.

Граф строится graphifyy через `D:/DeepSpaceSaga/.graphify-venv/Scripts/python.exe`, последовательно для AST из-за Windows process-pool ограничений. `rebuild.py` в output проверяет manifest hashes и повторяет clustering/export по сохранённой extraction; при изменении исходников нужна новая extraction. Confidence/locations сохраняются. Внешние AST reference nodes без declaration location и объединение нескольких отношений в undirected graph отмечены в diagnostics; исходные направления остаются в extraction.json. Token usage host-агентов не измерен; отсутствие внешнего API не означает нулевую стоимость.

```powershell
Set-Location D:/DeepSpaceSaga/DSS
& D:/DeepSpaceSaga/.graphify-venv/Scripts/python.exe -m graphify query "TradingAndSolarSystem" --graph src/graphify-out/graph.json --budget 1200
```

Граф проверен: нет dangling/self-loop edges, отсутствующих source files или выходящих за границы locations. 777 unresolved edges вынесены в pruned-unresolved-edges.json; 2118 reference nodes не имеют declaration location и не считаются доказанными символами.

Прежние output artifacts сохранены вне репозитория в `D:/DeepSpaceSaga/review-ep1-4-work/previous-graphify-out`, чтобы старые counts/traces не выдавались за результат новой сборки.
