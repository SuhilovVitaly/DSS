# EP-0019 — файлы для исполнения

| Путь | Что это |
|---|---|
| `fonts/DejaVuSans.ttf`, `fonts/DejaVuSans-Bold.ttf` | шрифт боя, поставляется с клиентом (US-0001 TK-0002) |
| `fonts/LICENSE-DejaVu.txt` | лицензия шрифта |
| `battle-data-seed1.txt` | значения снимков движка боя seed 1 (`main` @ `8c6f4e2a`) для ходов 1 и 3: позы, корпус и броня, предложения ИИ, цели и шансы, очередь огня, шаги залпа; на них проверяется вид (DesignSpec §12) |

## SHA-256

```
690243adfefe0ce154b547db6205794bd30ac4277275179517a90994f4980648  fonts/DejaVuSans.ttf
d1c3ff99f1e1ce1827a33efd4dad81f40babda06bff9e43bd7591c86662a287b  fonts/DejaVuSans-Bold.ttf
```

## Восстановление входных файлов 2026-11-08

В исходном коммите f9fcbd45 три файла fonts отсутствовали. Точные TTF восстановлены из [официального пакета Ubuntu 2.37-2build1](https://archive.ubuntu.com/ubuntu/pool/main/f/fonts-dejavu/fonts-dejavu-core_2.37-2build1_all.deb); оба хеша выше совпадают побайтно. LICENSE-DejaVu.txt содержит font stanza copyright этого пакета, без секции упаковки debian/*.

Входной battle-data-seed1.txt всё ещё отсутствует: его описание выше является требованием, не подтверждением наличия файла. Воспроизводимые значения поз, приказов и FireSteps должны быть восстановлены из исходного reference, а не записаны как новый oracle из текущего production. См. ExecutionStatus.md эпика.
