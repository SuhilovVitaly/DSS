# W8 · Производство портретов для двух поз

Выполни заказ на указанное число персонажей. Для каждого создай одну игровую куклу с общей внешностью и тоном кожи, двумя ракурсами головы и пятью эмоциями в каждом. Готовый персонаж должен собираться с любым зарегистрированным костюмом своей позы без индивидуального смещения, растяжения или дорисовки тела.

Для нового W8 общий полутон всей открытой кожи — **sRGB `#C98E6E` (RGB 201, 142, 110)**, взятый из настроек EV4N8S. Он един для портретов, шеи и тела в обоих ракурсах, на теле Medium во всех эмоциях. В PNG сохраняй светотень и блики; в `settings.json` записывай этот же полутон.

## Экспорт готового портрета

**Выбранный PNG из `%USERPROFILE%/.codex/generated_images` или файл, указанный пользователем, задаёт каждый пиксель соответствующей игровой ячейки.** Скопируй файл в `raw/<поза>/<эмоция>.png` без пересохранения. Запиши путь оригинала и его SHA-256. Для готового листа сохрани оригинал с хешем и координаты каждой ячейки. Извлекай ячейки строго по этим координатам.

1. Подбери на neutral один равномерный масштаб и целочисленный перенос для каждой позы. Используй эту регистрацию для всех пяти исходников позы с одинаковым кадрированием. Запиши `registration.json` в формате `{"standing":[scale,x,y],"prisoner":[scale,x,y]}`. Координаты задают левый верхний угол уменьшенного PNG на полном холсте тела. Размеры округляй после умножения обеих сторон на один коэффициент.
2. Уменьши **весь** выбранный PNG за один проход фильтром BOX с учётом альфы. Вставь результат на прозрачную ячейку прямым копированием RGBA. Сохрани весь выбранный рисунок, включая форму волос, уха, носа, подбородка и полупрозрачную кромку. Запрещены повторная обрезка при экспорте, очистка или порог альфы, ретушь, тонирование, замена черт из neutral, наложение масок, перекрытие волос телом и добавление пикселей шеи или одежды в атлас.
3. Перечитай игровой PNG и сравни каждую ячейку целиком с независимым уменьшением её оригинала. Допуск по каждому из четырёх каналов равен нулю, включая прозрачные поля. Разница хотя бы в одном пикселе означает ошибку экспорта.

Указания о срезе по образцу Рады, свете на шее, масках мимики и контрольных точках относятся к **рисованию и выбору новых исходников**. Если выбранный пользователем PNG при таком экспорте не проходит примерку, покажи прямую копию в игровом размере и конкретное расхождение до публикации. Изменение рисунка или общего стыка требует отдельного решения.

## До начала партии

Работай из `D:/DeepSpaceSaga/DSS-Images/Characters/W8/Antropik` проектным Python. Источники геометрии — [манифест стойки](../../../../DSS-Images/Characters/W8/Antropik/three-quarter-right/pose.json) и [манифест пленного](../../../src/DeepSpaceSaga.Client/Images/Persons/W8/Clothes/Prisoner/Templates/pose.json). Цвет и порядок наложения слоёв описаны в [контракте skinTone](ThreeQuarterCharacterSkinTone.md).

| Поза | Холст тела | Ячейка | Вставка | Игровой атлас, RGBA8 sRGB |
|---|---|---|---|---|
| Три четверти вправо | 282×747 | 144×190 | (69, 0) | `threequarter.png`, 720×190 |
| Пленный | 416×747 | 136×144 | (48, 416) | `prisoner.png`, 680×144 |

1. Составь список заказанных внешностей и уникальных ID из шести символов `A–Z0–9`. Проверь каждый ID в партии, в клиентском `W8/Portraits`, в `pose.json → characters`, в `work/provenance.json` и в рабочей папке исходников. Существующий ID изменяй только при явно заказанной ревизии. В путях имя персонажа строчное.
2. Для каждого ID используй отдельный рабочий каталог `work/imagegen/<id>/`. Здесь сохраняй фактические запросы в `prompts.md`, оригиналы в `raw/standing/` и `raw/prisoner/`, подложки, регистрацию и контрольные маски. Зарезервируй по одному файлу на эмоцию в каждой папке исходников. Если эмоции получены листом, сохрани также оригинал листа.
3. Перед серией проверь рабочий эталон `work/outfit/GREEN.uncut.png` для примерки стойки. Сверь ключи `outfits` двух манифестов. Проверь каждое зарегистрированное тело, направляющие и маски его позы. При пустом каталоге костюмов проверяй геометрию головы на рабочих эталонах обеих поз. Примерку с одеждой выполни после регистрации первого комплекта Medium. Костюм поставляет для каждой позы собственный `Templates/skin-mask-<id>.png` в режиме L. При производстве портрета загрузи и проверь эти маски; в папку персонажа записываются только атласы и `settings.json`. Зафиксируй хеши общих ресурсов и ранее готовых атласов для проверки в конце партии.

## Масштаб головы на стандартном теле

В стойке приложи [манекен SMALL с головой без волос](../../../src/DeepSpaceSaga.Client/Images/Persons/W8/Templates/STANDING-MANNEQUIN.png) целиком как PROPORTION ONLY вместе с крупной подложкой. Его тело построено из SMALL-BUILD, а [контур головы без волос](../../../src/DeepSpaceSaga.Client/Images/Persons/W8/Templates/HEAD-CONSTRUCTION.png) задаёт художественный масштаб. На холсте 282×747 анатомическая макушка находится на y=14 (допуск 3 px), подбородок на y=111 (допуск 2 px). Высота головы 98±3 px, ширина 73±3 px; обе величины включают крайние пиксели. Это около 7,5 высот головы от макушки до подошвы манекена. Размер ячейки 144×190 описывает упаковку атласа.

До генерации мимики обведи на собственном рисунке neutral анатомическую голову в ракурсе три четверти единым силуэтом от затылка до наиболее выступающей точки лица, от анатомической макушки до подбородка. Под волосами восстанови форму черепа по линии лба, виску и уху. Сохрани разметку как `standing-head-construction.png` (L, 282×747, белая голова на чёрном фоне, волосы и шея исключены). Наложи эту разметку поверх исходника при 1:1. Затем из Antropik запусти [проверку размеров](../../../../DSS-Images/Characters/W8/Antropik/tools/check_standing_proportions.py) проектным Python: `python tools/check_standing_proportions.py --head-mask work/imagegen/<id>/standing-head-construction.png`. При отклонении исправь единый масштаб и положение всех слоёв neutral, повторно проверь ухо, подбородок и открытый стык шеи. Если единым переносом и масштабом совпадения добиться нельзя, новый neutral верни на этап рисунка. Для уже выбранного портрета действуй по разделу [«Экспорт готового портрета»](#экспорт-готового-портрета). Для остальных эмоций сохрани его регистрацию.

Для проверки масштаба наложи neutral на силуэт [SMALL-BUILD](../../../../DSS-Images/Characters/W8/Antropik/three-quarter-right/Templates/SMALL-BUILD.png) в исходных координатах. Покажи результат рядом с целым манекеном в масштабе 1:1. Посадку уха, челюсти и шеи отдельно проверь на GREEN. Используй одинаковый холст и опору. Высокая причёска может выступать за разметку головы в пределах ячейки. Для пленного используй [эталонную голову без волос](../../../src/DeepSpaceSaga.Client/Images/Persons/W8/Templates/PRISONER-BALD-HEAD.png), [эталонное тело](../../../src/DeepSpaceSaga.Client/Images/Persons/W8/Templates/REFERENCE-BODY.png) и опоры челюсти: наклон и перспективное сокращение меняют проекцию головы.

## Сначала две нейтральные головы

Для обеих поз и всех пяти эмоций бери образцы художественного оформления из [Portraits-Master](../../../src/DeepSpaceSaga.Client/Images/Persons/W8/Portraits-Master). При каждом запуске выбирай образцы из текущего содержимого папки по характеру рисовки лица. Передавай их генератору с пометкой `STYLE ONLY`. Это правило действует для любой эмоции независимо от имён файлов. Сверяй характер контура, переходы света и тени на коже, группировку прядей и количество деталей, различимых в игровом масштабе. Черты лица, возраст и причёску бери из заказа. Если приложено фото, используй его для уточнения внешности. Лицо, уши и открытая шея в обеих позах используют общий с телом полутон EV4N8S `#C98E6E`. Ракурс, масштаб и посадку в каждой позе бери из её подложки и манифеста.

К запросу на нейтральную голову каждой позы добавь общий блок.

```text
W8 semi-realistic hand-painted game portrait of <appearance>, natural adult proportions.
Use the art samples selected from the Portraits-Master folder (STYLE ONLY) for the warm fine outlines, soft
painted skin shadows, restrained highlights and grouped hair strands. Draw the
requested facial features and hairstyle. Use the shared W8 skin midtone sRGB #C98E6E
for the face and ears in both poses and for any visible neck in the prisoner pose. Preserve painted shadows and highlights. Use
the pose underlay for camera angle, head scale and neck placement. Calm expression,
closed lips.
Transparent antialiased cutout. Keep clothing, backdrop, labels and accessories out
of the portrait. Avoid photographic pores, glossy 3D, exaggerated eyes and rim light.
```

**Стойка.** Возьми ростовой манекен с головой для масштаба и `work/outfit/GREEN.uncut.png` с верхней точкой шеи y=78 для стыка. К чистому манекену приложи отдельную копию с [разметкой](../../../src/DeepSpaceSaga.Client/Images/Persons/W8/Templates/STANDING-LANDMARKS.png) в (0, 0). Бирюзовые точки показывают суставы, золотые — направление лица, включая ухо, глаза и подбородок, розовая линия — стык слоёв. Координаты бери из `landmarks`, `headLandmarks` и `neckSeam` манифеста стойки. Если поза пленного уже готова, приложи её neutral как образец внешности. Вырежи (40, 0, 242, 260), увеличь ×5 методом NEAREST и сохрани подложку на фоне `#E8E2D4` как `underlay.png`. Приложи изолированную голову Рады из раздела [«Срез лица в стойке»](#срез-лица-в-стойке) как `CUT ONLY`. Из цельного `work/outfit/GREEN.uncut.png` вырежи тот же прямоугольник в том же масштабе и приложи отдельно как `NECK LIGHT`. На образце должны быть видны задний край за ухом, кожа под всей челюстью и оба края воротника. Приложи [примерку Лены в SURVEYSMALL](../../../../DSS-Images/Characters/W8/Antropik/work/imagegen/li8m2q/standing/neutral-nape-fit/LI8M2Q-SURVEYSMALL.png) как `NAPE CONTOUR ONLY` для перехода из-под ближнего уха к воротнику. Для первой примерки сохрани также этот участок заказанного костюма, если он уже готов.

```text
Use the unmarked bald standing mannequin for the upright camera and anatomical scale. Its POSE ONLY
overlay locates the eyes, ear and chin. Keep all landmarks out of the art. Match the existing
identity across poses. The CUT ONLY reference shows where the head cutout ends. Use the Portraits-Master samples for drawing style only.
Use the complete PROPORTION ONLY mannequin to set anatomical head size relative to the body.
At final game scale the hairless head is 98 px high and 73 px wide, with crown at y=14 and chin at y=111.
Keep hair volume separate from skull size. Use the close-up for painting detail.
Draw the requested person facing screen-right. Match skull scale, ear and chin
height to the underlay. The body supplies the entire standing neck, including the skin directly below the jaw
(SN7L4D “After”). Use NAPE CONTOUR ONLY for the exposed neck on the body.
For every new character and all five standing expressions, use Rada’s isolated standing head (RD7K4V, CUT ONLY).
Keep the visible earlobe intact. Trace this character’s own lower jaw beneath the cheek,
follow the rounded underside of the chin and continue up its front edge. Keep the shading
on the face side of that line. The exposed neck below the antialiased edge is transparent
on the face layer. Use the actual body crop labelled NECK LIGHT to match the local hue,
midtone and direction of light on the lower cheek to the exposed neck. Turn the jaw
with a broad change of plane inside the cheek. Keep the deepest accent local to the
angle by the ear and ease it towards the chin. A dark stroke tracing the whole cut
makes the face look pasted on. Judge the shadow with the supplied neck visible in place.
Export the isolated head with that same shading and a clean antialiased edge.
Separate locks crossing the collar from the skin.
```

Привяжи голову один раз переносом и равномерным масштабом. На сборке neutral с рабочим эталоном GREEN отметь нижнюю точку подбородка, мочку ближнего уха и фиксированную точку края воротника GREEN. За исходную высоту подбородка возьми опору y=111. Разница просвета `y воротника − y подбородка` — **до 2 игровых пикселей**. Сверь масштаб черепа и высоту уха. Проверь оба края кожи от челюсти до воротника. При 1:1 и ×4 проследи левый на экране контур от затылка до передней кромки воротника по образцу `NAPE CONTOUR ONLY`. Если излом виден уже на цельном теле, исправление относится к общей шее. Перед выпуском используй [проверку подложки стойки](ThreeQuarterOutfitCodexPrompt.md#цвет-и-геометрия-стойки). Диапазон столбцов `neckSeam` определяет разделение слоёв. Ширину шеи определяет рисунок тела на каждой строке.

При создании нового исходника, если после проверки высоты головы кожа выступает у ворота, исправь нижнюю кромку кожи, сохранив челюсть и боковые края общего тела. На полном холсте сделай маску `standing-neck-check.png` (L). Включи нижнюю шею, оба видимых края и полосу сразу за каждым из них. Исключи реальные перекрытия лица и прядей. Проверь маску поверх тела и зафиксируй до генерации эмоций.

### Срез лица в стойке

Для каждого нового персонажа W8 во всех пяти эмоциях стойки (`neutral`, `pain`, `anger`, `joy`, `pleasure`) нижняя граница головы проходит по линии **скула–подбородок, как у Рады**. Вся шея принадлежит телу. При генерации приложи [исправленную изолированную голову Рады (RD7K4V)](../../../../DSS-Images/Characters/W8/Antropik/work/imagegen/rd7k4v/standing-neutral-fit.png) как `CUT ONLY`. Форму уха, челюсти и подбородка рисуй по внешности нового персонажа.

На крупном neutral разметь нижний край лица по образцу Рады. Сохрани видимую мочку уха целиком. От уха веди срез по естественному нижнему контуру челюсти под щекой, плавно огибая подбородок до его переднего края. Сохрани светотень внутри этого контура. В самом исходном PNG сразу под сглаженной кромкой челюсти начинается прозрачность. Сохрани пересекающие эту линию пряди волос. Даже короткая полоска кожи шеи под подбородком — повод переделать рисунок до выбора мастера.

**Приёмка до генерации мимики.** Сопоставь сохранённый слой лица с изолированной головой Рады при 1:1 по мочке уха, нижнему контуру челюсти и округлению подбородка. На светлом и тёмном фоне проверь полноту кромки и прозрачность ниже неё. Затем собери сохранённый neutral на зарегистрированном теле с открытой шеей и общим `#C98E6E` по [правилам тонирования](ThreeQuarterCharacterSkinTone.md). До выпуска подходящей одежды используй рабочий эталон позы для проверки стыка. Осмотри кадр по [проверке света под челюстью](ThreeQuarterCharacterSkinTone.md#проверка-света-под-челюстью-в-стойке). Если новый рисунок захватывает горло или теряет часть челюсти, запроси исправленную генерацию до выбора мастера. Если анатомическая кромка на месте, а голова выглядит наклеенной, попроси генератор поправить светотень нижней щеки на подложенной шее, сохрани посадку и снова примерь neutral. К генерации мимики переходи после приёмки геометрии и света.

Для остальных четырёх эмоций рисуй тот же край neutral. Осмотри все пять изолированных исходников и затем все пять ячеек атласа на контрастном фоне. Проследи границу от мочки уха до подбородка в каждой эмоции. Шею и ворот тела показывай только на отдельной примерке.

**Пленный.** Используй [манекен SMALL с головой без волос](../../../src/DeepSpaceSaga.Client/Images/Persons/W8/Templates/PRISONER-MANNEQUIN.png) на холсте 416×747 и [прозрачную разметку](../../../src/DeepSpaceSaga.Client/Images/Persons/W8/Templates/PRISONER-LANDMARKS.png) в (0, 0). Из чистого манекена вырежи `(41, 412, 211, 581)` и увеличь ×5 NEAREST на том же фоне. Передай крупный план для рисования, а копию с разметкой — как POSE ONLY. Бирюзовые точки обозначают суставы, золотые — ориентиры головы, розовые — скрытые под головой опоры верхнего среза шеи. Сверь наклон по макушке, линии глаз, ближнему уху и подбородку. Индивидуальную форму лица и объём волос бери из заказа. Для отдельной проверки стыка наложи голову на `referenceBody` из [производственного шаблона](../../../../DSS-Images/Characters/W8/Antropik/prisoner/Templates/template.json).

```text
Use the clean bald mannequin close-up for the bowed pose and the POSE ONLY overlay for orientation.
The unmarked full mannequin fixes head-to-body scale. Keep the landmark colours and lines out of the art.
If the standing neutral already exists, use it for identity. Otherwise use the supplied character description or identity reference. Use Portraits-Master for style.
Draw the requested person with head bowed deeply down-left, crown visible.
Match camera, foreshortening, skull scale and placement at the collar and raised
shoulder. The near ear sits above and right of the near eye. The bridge partly hides
the far eye. The nose tip is left of the chin. Preserve the full near cheekbone,
jaw angle and rounded chin. Trace the visible chin-to-cheek contour against the garment.
Where the jaw overlaps cloth, end the cutout exactly on that anatomical contour:
all pixels outside its antialiased edge must be transparent. Keep a crisp painted edge
on the face side. Upper throat may connect only over exposed body skin shared by all
registered prisoner outfits. Never paint neck skin or a cast shadow on the garment. Keep nape locks attached and skin behind the ear clean. Trace this
character’s own silhouette. Leave collar, sleeve and shoulder on the body layer.
```

Сверь обе головы по носу, скулам, подбородку, уху, линии роста волос и приметам. По исходному рисунку пленного проследи скулу до угла челюсти, шею под подбородком и пряди за ухом. Зарегистрируй голову по черепу, уху, подбородку и верхнему срезу шеи. На светлом и тёмном фоне проверь собственный непрерывный контур, затем примерь его на все тела из `outfits` пленного с `skinTone`. Разметь на зарегистрированном исходном neutral собственную линию подбородок–скула. Проверь её на каждом зарегистрированном теле. При генерации запроси прозрачность до контура лица там, где под ним видна ткань. Сохрани естественное сглаживание в граничном пикселе и ясный рисунок со стороны щеки. В зоне под челюстью над тканью допустимы отдельные пиксели с альфой 1 из 255, если на светлом и тёмном фоне отсутствует видимый выступ или ореол. Короткий переход шеи разрешён только там, где все костюмы позы показывают открытую кожу. Край у ткани не растушёвывай. Для проверки исходника разметь на полном холсте маску L `prisoner-jaw-clear.png`: белым отметь зону сразу за сглаженным краем под челюстью над тканью, включая место возможного выступа шеи. Сверь разметку с неизменённым исходником, исключи реальные пряди и обязательное перекрытие верхнего среза тела. Не выводи контрольную маску из готового альфа-канала. Для всех пяти эмоций используй одну разметку. У левого на экране плеча виден естественный переход в рукав. Ступенька под челюстью, прямая срезанная скула, прорези в волосах или отдельные пиксели на затылке требуют повторной генерации до выбора мастера. Сохрани принятый прозрачный слой нейтральной головы на полном холсте 416×747 как `prisoner-neutral-fit.png`. Сверь его с прямым уменьшением исходника. Примерку с телом сохраняй отдельно. Присваивать голове альфу чужого `portraitAlpha`, вычитать из неё силуэт тела или достраивать шею растяжением крайнего пикселя запрещено.

## Четыре изменения мимики на каждую позу

Если все пять рисунков эмоций уже выбраны, переходи к [прямой упаковке в атлас](#прямая-упаковка-в-атлас). Для создания недостающих эмоций начинай с принятого neutral **этой позы**. Общий порядок ячеек слева направо — `neutral, pain, anger, joy, pleasure`. Сохрани тот же источник света, положение головы, причёску, нос, ухо, подбородок и кожу у стыка. Можно получить четыре варианта одним листом на позу.

```text
Edit the accepted neutral of this pose. Use art samples from the Portraits-Master folder
for the drawing style of every expression. Lock hair, nose, ears, silhouette, pose,
neck, skin tone and lighting. In every standing expression, preserve Rada’s cheek-to-chin cut and the shading on the face side. Leave all neck skin to the body. Change brows, lids and mouth as coherent features.
Pain: inner brows raised and drawn together, squint, tense or parted lips.
Anger: brows lowered towards the bridge, narrowed eyes, tight lips.
Joy: raised cheeks, broad smile, upper teeth read as one warm light shape.
Pleasure: relaxed brows, half-closed lids, soft closed smile.
```

При рисовании новой эмоции сверь её с neutral по переносице, кончику носа, уху и челюсти. На полном холсте каждой позы зафиксируй две маски L в рабочем каталоге: `standing-expression-mask.png` и `standing-nose-lock.png`, для пленного — те же имена с префиксом `prisoner-`. Белым отметь соответственно области изменения и неподвижный нос. Используй разметку в запросе на рисование и при проверке исходника. Каждый полученный PNG содержит собственное полное выражение. При несовместимом ракурсе переделай исходник эмоции.

В масштабе 1:1 поставь сохранённую ячейку рядом с выбранными образцами рисовки лица из `Portraits-Master`. Проверь толщину линии века и брови, мягкость тени на щеке и способ рисовать волосы. Затем при 1:1 и ×4 NEAREST проверь каждую сохранённую ячейку. Брови одиночные, веки сходятся в уголках и охватывают радужку с учётом ракурса. Переносица, кончик и ноздри читаются как один нос. У рта одна линия смыкания или одно открытие, пропорциональные губы и согласованные уголки. Улыбка, прищур и напряжение читаются и в наклоне головы. Небольшие различия пикселей носа, светотени и сглаженного контура между исходниками эмоций допустимы, если при смене кадров в масштабе 1:1 голова сохраняет посадку, нос сохраняет форму и лицо узнаваемо. Побайтно сравнивай каждую ячейку с уменьшенным исходником именно этой эмоции.

## Контрольные точки стойки

Для нового портрета зафиксируй на neutral три ориентира в координатах холста 282×747: нижний край мочки видимого уха, переход под ухом к челюсти и нижнюю точку подбородка. Покажи их художнику на подложке и отметь те же ориентиры на исходном слое лица до добавления волос. Точка внутри мочки — (117, 79), перекрытие за ухом — (123, 90), под углом челюсти — (130, 99), у подбородка — (152, 111). Проверь перекрытие лица и шеи тела у угла челюсти. Положение подбородка оцени по его собственной округлой кромке в пределах допуска высоты. Контрольные точки открытой шеи — (125, 104) и (140, 117). Форма мочки и подбородка индивидуальна; оцени её визуально вместе с попаданием в опоры. Для пяти эмоций сохраняй регистрацию neutral и вставку полной ячейки в (69, 0).

Посадку «После» проверяй на собранном персонаже. Верх шеи за ухом достигает y=78 при альфе ≥128. Для каждого нового персонажа укажи ID в `neckSeam.fullNeckUnderlayCharacters` манифеста позы. При примерке наложи голову на полную шею тела. Сохрани прозрачность выбранного мастера при экспорте.

## Прямая упаковка в атлас

Вход для упаковки — десять выбранных PNG в RGBA8 sRGB, по пять на позу. Если для рисования использовались слои, маски или подложки, оставь их в рабочем каталоге. Для каждого входа запиши в `raw-sha256.json` SHA-256 оригинала под ключом вида `standing/neutral`. Для листа используй хеш PNG ячейки, предварительно сверив её RGBA с прямой вырезкой из сохранённого оригинала. Сверяй хеши до упаковки. Несовместимый формат или непрозрачный фон устраняй на этапе получения исходника до его выбора.

Собирай кандидат напрямую из `raw/`. Старый `tools/add_character.py` включает общую шею в стойку и перекрывает задние волосы. Его результат расходится с прямым экспортом. Задай `W8_CHARACTER` и запускай следующий блок проектным Python из `Antropik`. Сначала он записывает оба атласа в рабочий каталог кандидата, затем заново открывает исходники и проверяет все десять ячеек. Для проверки опубликованной папки вызови ту же функцию `verify` с её путём.

```python
import hashlib
import json
import math
import os
import re
from pathlib import Path

import numpy as np
from PIL import Image

cid = os.environ["W8_CHARACTER"]
assert re.fullmatch(r"[A-Z0-9]{6}", cid)
work = Path("work/imagegen") / cid.lower()
dst = work / "candidate" / cid.lower()
registration = json.loads((work / "registration.json").read_text(encoding="utf-8"))
hashes = json.loads((work / "raw-sha256.json").read_text(encoding="utf-8"))
emotions = ("neutral", "pain", "anger", "joy", "pleasure")
poses = {
    "standing": ("threequarter.png", (144, 190), (69, 0)),
    "prisoner": ("prisoner.png", (136, 144), (48, 416)),
}

def original_cell(pose, emotion):
    key = f"{pose}/{emotion}"
    path = work / "raw" / (key + ".png")
    assert hashlib.sha256(path.read_bytes()).hexdigest() == hashes[key].lower(), key
    _, size, origin = poses[pose]
    scale, x, y = registration[pose]
    assert math.isfinite(scale) and scale > 0 and type(x) is int and type(y) is int
    with Image.open(path) as im:
        assert im.mode == "RGBA", path
        scaled = im.resize((round(im.width * scale), round(im.height * scale)), Image.Resampling.BOX)
    # Pillow resizes RGBA through premultiplied alpha. Paste without a mask copies RGBA once.
    left, top = x - origin[0], y - origin[1]
    bounds = scaled.getchannel("A").getbbox()
    assert bounds and 0 <= left + bounds[0] and left + bounds[2] <= size[0], key
    assert 0 <= top + bounds[1] and top + bounds[3] <= size[1], key
    cell = Image.new("RGBA", size)
    cell.paste(scaled, (left, top))
    return cell

def verify(folder):
    for pose, (filename, (w, h), _) in poses.items():
        with Image.open(folder / filename) as atlas:
            assert atlas.mode == "RGBA" and atlas.size == (w * len(emotions), h)
            for i, emotion in enumerate(emotions):
                actual = atlas.crop((i*w, 0, (i+1)*w, h))
                expected = original_cell(pose, emotion)
                changed = np.any(np.asarray(actual) != np.asarray(expected), axis=2)
                assert not changed.any(), (pose, emotion, int(changed.sum()))

dst.mkdir(parents=True, exist_ok=True)
for pose, (filename, (w, h), _) in poses.items():
    atlas = Image.new("RGBA", (w * len(emotions), h))
    for i, emotion in enumerate(emotions):
        atlas.paste(original_cell(pose, emotion), (i*w, 0))
    atlas.save(dst / filename)
(dst / "settings.json").write_text('{"skinTone":"#C98E6E"}\n', encoding="utf-8")
verify(dst)
assert {f.name for f in dst.iterdir()} == {"threequarter.png", "prisoner.png", "settings.json"}
```

## Приёмка и выдача основного комплекта

Перед выдачей перечитай оба реестра костюмов. Если комплект Medium уже опубликован после примерки на манекенах, при приёмке первого портрета выполни все 10 сборок по шагу 1 для каждого такого комплекта, включая открытый крой. Для открытого кроя осмотри границу ткани и кожи под челюстью и передними прядями. Если зарегистрированных костюмов пока нет, укажи результат примерки на шаблонах и статус «совместная примерка ожидает первого костюма». Первый выпуск одежды закрывает эту проверку по опубликованным PNG.

1. Примерь все десять перечитанных ячеек на эталонах и на каждом зарегистрированном костюме соответствующей позы. Проверь маски кожи из обоих манифестов и применяй тонирование только в сборке. Для стойки используй штатный `Pose.portrait_underlay(body, character)` и `dss3q.over`, для пленного — полное тело с ячейкой поверх. Осмотри каждую эмоцию при 1:1 и ×4 на светлом и тёмном фоне. Проверь посадку, просвет под челюстью, ухо, края волос, ворот и отсутствие ткани в портрете.
2. Отдельно проверь геометрию стыка и соответствие масштабу по манифестам обеих поз. Допуск **251–255** проверяй на слое, который закрывает стык в соответствующей позе. В стойке со срезом по челюсти проверь перекрытие головы и тела у `jawAngle`, а непосредственно над `neckSeam` проверь альфу полной шеи тела в каждом столбце. Для ранее упакованных атласов с верхней шеей эти пиксели проверяются в портрете. У пленного проверь все 60 пикселей верхнего среза тела по `neckProtect`, `portraitAlpha` и линии между `upperNeckFront` и `upperNeckBack`, а также прозрачность размеченной зоны `prisoner-jaw-clear.png` под челюстью над тканью. Если старая схема упаковки требует добавить шею из GREEN, перекрасить лицо или закрыть прядь пикселями тела в самой ячейке, покажи конфликт на примерке. Публикуй только при совпадении с исходниками и успешной примерке.
3. После приёмки нового ID перенеси целиком проверенную папку кандидата в клиентский каталог на том же диске. Команда ниже завершается ошибкой, если целевая папка уже существует. После прерывания сверь рабочие исходники и запись в `imagegenCharacters` и продолжи тот же ID с незавершённого этапа. Если папка уже опубликована, проверь её содержимое и хеши. Ревизия существующего персонажа требует отдельного запроса и сохранения предыдущего комплекта.
4. При публикации добавь новый ID в `three-quarter-right/pose.json → characters` и `neckSeam.fullNeckUnderlayCharacters`, а ссылки на фактические исходники и регистрацию запиши в `work/provenance.json`, сохранив существующие записи. Проверь клиентский путь, хеши трёх файлов, вызови `verify` для клиентской папки и выполни штатный валидатор. Засчитывай персонажа только после успешной проверки обеих поз. По партии укажи число заказанных и готовых ID, причину каждого пропуска. Сверь сохранность ранее готовых ассетов. Временные матрицы держи в `D:/DeepSpaceSaga/artifacts/validation/<task>/<run>/` и удаляй после проверки. Итоговые картинки сохраняй и показывай по запросу, вне игровой папки. В `Portraits/<id>/` остаются ровно два атласа и один JSON.

```powershell
$py = 'D:/DeepSpaceSaga/DSS-Images/tools/modular-character-pilot/.venv/Scripts/python.exe'
$idFolder = $env:W8_CHARACTER.ToLowerInvariant()
$candidates = Join-Path (Get-Location) "work/imagegen/$idFolder/candidate"
$clientRoot = 'D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/Images/Persons/W8/Portraits'
$sourceDir = Join-Path $candidates $idFolder
$gameDir = Join-Path $clientRoot $idFolder
$hashes = Get-ChildItem -LiteralPath $sourceDir -File | Get-FileHash
[IO.Directory]::Move($sourceDir, $gameDir)
foreach ($entry in $hashes) {
    $published = Join-Path $gameDir ([IO.Path]::GetFileName($entry.Path))
    if ((Get-FileHash -LiteralPath $published).Hash -ne $entry.Hash) { throw "Расхождение в $published" }
}
& $py tools/dss3q.py validate --pose three-quarter-right --character $env:W8_CHARACTER
if ($LASTEXITCODE -ne 0) { throw 'Ошибка проверки опубликованной стойки' }
```
