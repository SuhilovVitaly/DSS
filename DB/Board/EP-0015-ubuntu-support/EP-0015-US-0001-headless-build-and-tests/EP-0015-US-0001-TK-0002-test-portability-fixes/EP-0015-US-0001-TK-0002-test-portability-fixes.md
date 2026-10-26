---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0001-headless-build-and-tests
ticket: EP-0015-US-0001-TK-0002-test-portability-fixes
title: "Исправление непереносимых тестов"
stage: draft
layer: tests
depends_on: [EP-0015-US-0001-TK-0001-linux-build-baseline]
serves: [AC-02, AC-03, AC-04]
created: 2026-10-25
---

# Исправление непереносимых тестов

История: [EP-0015-US-0001-headless-build-and-tests](../EP-0015-US-0001-headless-build-and-tests.md) · Эпик: [EP-0015](../../Documentation.md)

## Зачем

Исправить тесты, которые падают на Linux из-за допущений Windows: регистр путей, разделители, перевод строк, имена исполняемых файлов.

## Шаги

1. Источник списка — таблица из TK-0001.
2. Пути строить только через `Path.Combine`/`Path.Join`; не сравнивать пути как строки с `\`.
3. Если тест сравнивает текст с эталоном — нормализовать `\r\n` → `\n` перед сравнением (в репозитории нет `.gitattributes`, поэтому окончания строк зависят от настроек git у разработчика).
4. Добавить `.gitattributes` с `* text=auto` и явными `binary` для `*.png *.jpg *.otf *.ttf *.trx`.

## Зависимости

[EP-0015-US-0001-TK-0001-linux-build-baseline](../EP-0015-US-0001-TK-0001-linux-build-baseline/EP-0015-US-0001-TK-0001-linux-build-baseline.md)

## Definition of Done

- Все тесты из таблицы TK-0001 с причиной «переносимость» проходят на Linux и на Windows.
- `.gitattributes` добавлен; повторный checkout на Windows и Linux не даёт изменённых файлов в `git status`.
- Проверено на Windows и Ubuntu; на Windows поведение не ухудшилось.
