# Навигационный граф

Поддерживаемый offline rebuild находится в tools/graphify/rebuild.py;
новый результат выводится в уникальный workspace artifacts/validation/graphify
либо абсолютный DSS_GRAPH_OUTPUT, вне Documentation. Исторический src/graphify-out
не заменяется автоматически.
Скрипт поддерживает --prepare-ast, --dry-run и --force; последнее разрешает
намеренное уменьшение графа, а не скрывает потерю coverage.
Зависимости берутся из локального окружения Graphify, не загружаются игрой.

Граф помогает находить владельцев API и связи документов/кода. Его metadata,
input hashes и warnings определяют свежесть. После изменения документации
старый граф не следует выдавать за актуальный источник требований.
Факты подтверждаются по исходным файлам; navigation/inferred edges не являются
доказательством runtime поведения, native FPS или продуктовой приёмки.
Большие extracts, manifests, кеши, HTML и результаты запусков остаются вне
Documentation. Для исследования не требуется запуск игры или внешний API.

Источники: [rebuild.py](../../tools/graphify/rebuild.py).
