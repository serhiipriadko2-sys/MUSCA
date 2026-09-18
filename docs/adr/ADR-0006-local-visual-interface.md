# ADR-0006: локальный визуальный интерфейс первого игрового среза

## Status

Accepted for the local exploratory visual prototype. This ADR does **not** select
the final game engine and does not modify the frozen GATE-P01 v0.1 participant build.

## Context

Терминальный «Шлюз» доказал исполняемость механики, но текстовый поток плохо
передаёт пространственный смысл, изменение сигнала, ресурс наблюдения и последствия
выбора. Пользователь явно запросил визуальный интерфейс после первой human session.

В текущем Windows/Python 3.14.6 уже доступен Tk 8.6. Поэтому визуальный слой можно
реализовать без новой package dependency, не устанавливая Unity и не меняя
существующие контракты Simulation, Bridge, GateSession или MuscaBackend.

GATE-P01 v0.1 уже начал сбор данных на frozen terminal snapshot. Замена интерфейса
в середине выборки создала бы новую экспериментальную версию и смешала условия.

## Decision

Добавить отдельный Tk visual shell поверх существующей игровой модели.

- `VisualPuzzleController` содержит player-safe state machine и тестируется без GUI.
- Tk widgets получают только player-safe snapshot и `GateSession.view()`.
- hidden layout не входит в визуальное состояние и не рисуется.
- человек по-прежнему выбирает только семантические действия `go/wait/quit`,
  `scan/amber/cobalt/leave/quit`.
- покадровые направления остаются собственностью `MuscaBackend`.
- визуальный запуск оформляется флагом `--visual --puzzle`.
- evaluator receipt сохраняет layout только после/вне player-facing UI так же, как CLI.

Интерфейс оформляется как тёмный sci-fi HUD: рост сигнала виден как путь,
у шлюза реагенты представлены карточками, ресурс — двумя ячейками, химическая
проба — отдельным действием. FACT/HYP/UNKNOWN/INTERP сохраняются явно.

Visual shell имеет статус exploratory candidate for the next gameplay iteration.
Он не используется для оставшихся участников GATE-P01 v0.1 без новой preregistration.

## Alternatives

| Вариант | Решение |
| --- | --- |
| Оставить только terminal UI | Сохраняет простоту, но уже недостаточен для понимания сцены. |
| HTML/browser shell | Хорош для будущего удалённого теста, но добавляет HTTP/browser contract и отдельную security surface. |
| Unity сейчас | Сильнее для 3D vertical slice, но Unity ещё не установлен и выбор engine требует отдельного ADR/dependency gate. |
| Tkinter / stdlib | Выбран как обратимый bridge от terminal prototype к визуальному UX без новой зависимости. |

## Consequences / price

Плюс: можно проверять композицию интерфейса и понятность механики уже на текущем
коде. Цена: Tk — не игровой движок, ограничен для анимации, 3D, audio pipeline и
кроссплатформенной production-сборки. Наличие Tk на одном Windows-host не доказывает
готовность Linux/macOS или packaged distribution.

Новый UI сам является experimental factor. Его пользовательские результаты нельзя
объединять с GATE-P01 v0.1. Для human test visual версии нужен новый manifest/version.

## Tests

- controller не содержит поля hidden layout в player snapshot;
- два layout дают одинаковое визуальное состояние до разрешённой химической пробы;
- light mode не может выполнить scan;
- scan стоит одну ячейку и не выбирает реагент;
- правильный/неправильный выбор сохраняют исходные последствия GateSession;
- quit не превращается в успех;
- GUI создаётся локально на Tk 8.6 и может быть закрыт без traceback;
- полный существующий regression suite остаётся зелёным.

## Diff scope

`musca/visual.py`, `musca/visual_tk.py`, минимальная интеграция CLI,
visual/controller tests, этот ADR и отдельная visual-документация. Research manifests,
participant data, frozen pilot ZIP, Bridge contracts и scientific lineage не меняются.

## Rollback

Удалить visual branch/commit или перестать использовать `--visual`. Терминальный
`--puzzle` остаётся рабочей спецификацией поведения. Никаких миграций данных нет.

## ΔDΩΛ

∆ — добавлен обратимый визуальный presentation layer без новой зависимости.
D — explicit user need, Tk 8.6 host capability, existing GateSession contracts and tests.
Ω — высокая для локальной инженерной реализуемости; usability visual версии ещё UNKNOWN.
Λ — пересмотреть при новом human pilot visual версии или выборе production game engine.
