# Двигатели (`Modules/Engines/`)

Модуль двигателя: на дрон ставится от 1 до `engineSlots` рамы, у носителя двигателей нет. Вклады двигателей суммируются по порядку списка `engines[]` компоновки и масштабируются массой дрона (Spec/Combat.md §3):

```
k      = referenceMassKg рамы / масса дрона
vmax   = min(maxSpeedCap, minSpeed + Σ speedBonus × множитель × k)
change = Σ speedChange × множитель × k          — изменение скорости за ход
turn   = min(turnCapDeg, Σ turnDeg × множитель × k)
```

Множитель двигателя — 1.0. После повреждения он становится `rules.systems.engineDamagedFactor` (0.5) до конца боя (Spec/Combat.md §6.4).

| Поле | Смысл |
|---|---|
| `name` | название |
| `speedBonus` | вклад в vmax над vmin |
| `speedChange` | вклад в изменение скорости за ход |
| `turnDeg` | вклад в поворот за ход, ° |
| `massKg` | масса, кг |

<!-- table:engines -->
| id | name | speedBonus | speedChange | turnDeg | massKg |
|---|---|---|---|---|---|
| drive.heavy | Тяжёлый маршевый | 32 | 11 | 27.5 | 150 |
| drive.light | Лёгкий маршевый | 20 | 9 | 30 | 70 |
| drive.sprint | Форсажный | 35 | 14 | 25 | 120 |
| drive.standard | Стандартный маршевый | 25 | 10 | 32.5 | 100 |
| drive.vector | С вектором тяги | 18 | 9 | 45 | 110 |
| drive.vector.light | Лёгкий векторный | 15 | 8 | 40 | 85 |
<!-- /table -->
