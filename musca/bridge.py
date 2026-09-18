"""Translate upper intentions and lower telemetry; never issue motor commands."""

from typing import Mapping

from .contracts import (
    MIN_INTENT_INTERVAL, Contact, ContractError, EventKind, GateTelemetry, Goal,
    HighLevelIntent, MuscaModulation, SemanticEvent, SensorimotorTelemetry, SensorMode,
)


class Bridge:
    def __init__(self) -> None:
        self._intent: HighLevelIntent | None = None
        self._last_seek_tick: int | None = None

    @property
    def intent(self) -> HighLevelIntent | None:
        return self._intent

    def clear(self) -> None:
        self._intent = None

    def submit(self, payload: Mapping[str, object], tick: int) -> HighLevelIntent:
        # Any rejection must revoke prior movement permission, including bad timestamps.
        self.clear()
        if type(tick) is not int or tick < 0:
            raise ContractError("current tick must be a nonnegative integer")
        intent = HighLevelIntent.parse(payload)
        if intent.issued_at != tick:
            raise ContractError("issued_at must equal the current tick")
        if intent.goal == Goal.SEEK_SIGNAL:
            if self._last_seek_tick is not None and tick - self._last_seek_tick < MIN_INTENT_INTERVAL:
                raise ContractError("new movement intentions require at least 4 ticks between them")
            self._last_seek_tick = tick
        self._intent = intent
        return intent

    def modulation(self, tick: int) -> MuscaModulation:
        if type(tick) is not int or tick < 0:
            raise ContractError("current tick must be a nonnegative integer")
        intent = self._intent
        if intent is None or not intent.issued_at <= tick < intent.expires_at:
            return MuscaModulation(False, False, tick)
        return MuscaModulation(
            intent.goal == Goal.SEEK_SIGNAL,
            intent.sensor_mode == SensorMode.LIGHT_CHEMICAL,
            intent.expires_at,
        )

    def events(self, telemetry: SensorimotorTelemetry) -> tuple[SemanticEvent, ...]:
        if not isinstance(telemetry, SensorimotorTelemetry):
            raise ContractError("backend must return SensorimotorTelemetry")
        result = [SemanticEvent(telemetry.tick, EventKind.SIGNAL, telemetry.signal)]
        if telemetry.chemical_warning is True:
            result.append(SemanticEvent(telemetry.tick, EventKind.CHEMICAL_WARNING, True))
        if telemetry.collided:
            result.append(SemanticEvent(telemetry.tick, EventKind.COLLISION, True))
        if telemetry.contact == Contact.HARM:
            result.append(SemanticEvent(telemetry.tick, EventKind.HARM, True))
        elif telemetry.contact == Contact.BEACON:
            result.append(SemanticEvent(telemetry.tick, EventKind.BEACON, True))
        return tuple(result)

    def gate_events(self, telemetry: GateTelemetry) -> tuple[SemanticEvent, ...]:
        if not isinstance(telemetry, GateTelemetry):
            raise ContractError("gate observation must be GateTelemetry")
        result = [
            SemanticEvent(telemetry.tick, EventKind.AMBER_CHARGE, telemetry.amber_charge),
            SemanticEvent(telemetry.tick, EventKind.COBALT_CHARGE, telemetry.cobalt_charge),
        ]
        if telemetry.amber_reactive is not None:
            result.extend((
                SemanticEvent(telemetry.tick, EventKind.AMBER_REACTIVE, telemetry.amber_reactive),
                SemanticEvent(telemetry.tick, EventKind.COBALT_REACTIVE, telemetry.cobalt_reactive),
            ))
        return tuple(result)
