# W4: цельный портрет и костюм

По умолчанию `TempCharacterImage` открывает W4. В окне два выбора: персонаж и костюм. Справа — три карточки персонажей; выбор карточки сохраняет текущий костюм. Клик по большому портрету создаёт другое сочетание. Сохранения W4 используют отдельный `temp-character-w4.json`.

Файлы: `src/DeepSpaceSaga.Client/Images/Persons/W4`.

- `Portraits` — три новых прозрачных PNG: голова, все черты лица, причёска и собственная шея нарисованы вместе. Каждое изображение остаётся цельным.
- `Clothes` — три неизменённые копии совместимых костюмов W2.
- `Sources` — три исходных изображения, сгенерированные без референсов.
- `Generated` — примеры, все девять сочетаний и превью окна.

Рабочие PNG имеют размер 1024×1024. Порядок сборки: портрет → костюм. Подготовка ограничена очисткой прозрачного края и равномерным масштабированием/смещением целого изображения к воротнику. Отдельных овалов, лиц, подбородков, волос и шей в W4 нет. Предыдущие W, W1 и W2 сохранены.

Изображения созданы встроенным `image_gen`; запросы записаны в `tools/DeepSpaceSaga.PortraitAssets/w4-prompts.json`. Авторская сборка — `UnifiedPortraitAssets.cs`:

```powershell
dotnet run --project tools/DeepSpaceSaga.PortraitAssets -- w4 src/DeepSpaceSaga.Client/Images/Persons/W4
dotnet run --project tools/DeepSpaceSaga.PortraitAssets -- preview src/DeepSpaceSaga.Client/Images/Persons/W4
```

Игра загружает только рабочие PNG и JSON из W4 рядом с исполняемым файлом. `Sources` и `Generated` не входят в сборку и публикацию.
