# Боевые доклады и журнал

Engine создаёт факты боевого доклада; Client локализует и показывает их,
согласуя с display-admitted сценой. Новые кадры, повторная доставка, pause
и Load не создают дубликатов одного факта. Reliable sequence/cursor — часть
continuation; receipt и визуальная сцена не должны показывать противоречащие исходы.

Combat report panel позволяет читать собственные боевые события и реакцию
корабля; локализованные роль, имя и текст формируются из подтверждённых DTO.
Подробности health/disabled/destroyed собственных модулей доступны в командных
группах слева. Отчёт не раскрывает скрытую NPC-перезарядку.

Завершение своего reload даёт одно сообщение и один readiness cue после
допуска в UI. Начальная готовность и загруженный завершённый reload молчат;
незавершённый deadline сообщает при достижении. Наличие audio cue в коде
не доказывает физически слышимое воспроизведение — это отдельная UI-проверка.

Источники: [DeepSpaceSaga.Contracts](../../src/DeepSpaceSaga.Contracts), [GameSession](../../src/DeepSpaceSaga.Client/UI/Screens/GameSession), [SimulationEngine.PointCombat.cs](../../src/DeepSpaceSaga.Engine/SimulationEngine.PointCombat.cs).
