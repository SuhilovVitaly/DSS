# Перестройка навигационного графа

`tools/graphify/rebuild.py` выполняет offline AST extraction и Markdown structure
для code/docs/Board, включая EP-0013, и объединяет свежие host-agent semantic
fragments. Реальная игра и внешний API не запускаются. Для нового rebuild нужны
Python с установленным Graphify и результаты semantic чтения по текущим hashes.

```powershell
$env:DSS_GRAPH_OUTPUT = 'D:/DeepSpaceSaga/artifacts/validation/graphify/unique-run'
$env:DSS_GRAPH_SEMANTICS = 'D:/DeepSpaceSaga/artifacts/validation/ep0013'
& 'D:/DeepSpaceSaga/.graphify-venv/Scripts/python.exe' tools/graphify/rebuild.py
```

Без output override выбирается уникальный timestamp-каталог под artifacts.
AST cache также располагается там. Семантические `graph-semantic-*.json` должны
содержать nodes/edges/hyperedges и input_sha256; EXTRACTED имеет confidence1,
архитектурные выводы помечаются INFERRED. Абсолютные source paths нормализуются
к корню репозитория; изменившийся input требует повторного чтения.

`--prepare-ast` подготавливает структурный кэш, `--dry-run` проверяет построение,
`--force` разрешает осознанное уменьшение существующего output. Перенос output
не означает переписывание исторического `src/graphify-out`.

Проверяются существование sources, диапазоны L#, endpoints, self-loops и hashes.
Необъявленные AST references и canonical self-loops учитываются в audit sidecars,
а не становятся выдуманными фактами. Полный graph.json и community HTML имеют
разные уровни детализации. Manifest относится к фактически прочитанному рабочему
дереву; параллельные непубликованные изменения должны быть обозначены в Board.
Документы о графе и publication/impact ledgers исключаются из hashes во избежание
циклической зависимости. Кэши/временные scripts удаляются после проверки;
сохраняются только явно требуемые evidence.

Graphify — средство навигации. Ни связность графа, ни INFERRED edge не доказывают
Engine outcome, native FPS, звук, физический ввод или human/product acceptance.
Текущий результат перестройки и residual gates публикуются в
[ExecutionStatus](../../Board/EP-0013-debug-tuning-panel/ExecutionStatus.md).
