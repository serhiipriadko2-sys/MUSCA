"""Scripted interpretation consumes events only, with no access to hidden world state."""

from .contracts import EventKind, SemanticEvent


def interpret(events: tuple[SemanticEvent, ...]) -> tuple[str, ...]:
    statements = []
    kinds = {event.kind for event in events}
    for event in events:
        if event.kind == EventKind.SIGNAL:
            statements.append(f"[FACT] Интенсивность светового сигнала: {event.value}.")
    if EventKind.COLLISION in kinds:
        statements.append("[FACT] Движение заблокировано препятствием.")
    if EventKind.HARM in kinds:
        statements.append("[FACT] Зарегистрирован вред при контакте.")
        statements.append("[INTERP] Этот путь не подтвердился как безопасный; движение остановлено.")
    elif EventKind.BEACON in kinds:
        statements.append("[FACT] Зарегистрирован контакт с источником сигнала.")
    elif EventKind.CHEMICAL_WARNING in kinds:
        statements.append("[FACT] Химический канал обнаружил предупреждающий признак рядом.")
        statements.append("[HYP] Рядом возможна опасность. Предупреждение ещё не доказывает вред.")
    else:
        statements.append("[HYP] Свет может вести к источнику; безопасность пути ещё не установлена.")
    return tuple(statements)


def interpret_gate(events: tuple[SemanticEvent, ...]) -> tuple[str, ...]:
    """Gate explanation sees the same observations as the player, not the world."""
    observed = {event.kind: event.value for event in events}
    statements = []
    for kind, label in ((EventKind.AMBER_CHARGE, 'янтарного'), (EventKind.COBALT_CHARGE, 'кобальтового')):
        if kind in observed:
            statements.append(f"[FACT] Световой заряд {label} реагента: {observed[kind]}.")
    if EventKind.AMBER_REACTIVE not in observed:
        statements.append("[HYP] Более яркий янтарный реагент может подойти. Свет не подтверждает состав.")
        statements.append("[UNKNOWN] Нейтральность реагентов ещё не измерена.")
    else:
        for kind, label in ((EventKind.AMBER_REACTIVE, 'Янтарный'), (EventKind.COBALT_REACTIVE, 'Кобальтовый')):
            if kind in observed:
                state = 'реактивный' if observed[kind] else 'нейтральный'
                statements.append(f"[FACT] {label} реагент: {state}, по химической пробе.")
        if observed[EventKind.AMBER_REACTIVE]:
            statements.append("[INTERP] Гипотеза о пригодности янтарного реагента опровергнута пробой.")
        else:
            statements.append("[INTERP] Проба поддерживает пригодность янтарного реагента по правилу шлюза.")
    return tuple(statements)
