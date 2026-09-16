"""Local scripted controller; there is no access to the world or its map."""

from .contracts import (
    Contact,
    MotorCommand,
    MuscaModulation,
    SensorFrame,
    SensorimotorTelemetry,
)


_ORDER = {
    MotorCommand.EAST: 0,
    MotorCommand.SOUTH: 1,
    MotorCommand.WEST: 2,
    MotorCommand.NORTH: 3,
}
_REVERSE = {
    MotorCommand.EAST: MotorCommand.WEST,
    MotorCommand.SOUTH: MotorCommand.NORTH,
    MotorCommand.WEST: MotorCommand.EAST,
    MotorCommand.NORTH: MotorCommand.SOUTH,
}


class ScriptedBackend:
    """A greedy local rule for the fixture, not a general route planner."""

    def reset(self) -> None:
        """No hidden episode state: the last applied action arrives in sensors."""

    def act(self, sensors: SensorFrame, modulation: MuscaModulation) -> MotorCommand:
        if (
            not modulation.approach_signal
            or sensors.tick >= modulation.valid_until
            or sensors.contact is not Contact.NONE
        ):
            return MotorCommand.STAY
        candidates = [
            probe for probe in sensors.probes
            if not probe.blocked
            and probe.direction in _ORDER
            and not (modulation.avoid_chemical and probe.chemical is True)
        ]
        if not candidates:
            return MotorCommand.STAY
        reverse = _REVERSE.get(sensors.last_action)
        alternatives = [probe for probe in candidates if probe.direction is not reverse]
        if alternatives:
            candidates = alternatives
        # Lower signal remains eligible, permitting a local detour.
        chosen = max(candidates, key=lambda probe: (probe.signal, -_ORDER[probe.direction]))
        return chosen.direction

    def observe(self, sensors: SensorFrame) -> SensorimotorTelemetry:
        warning = None
        if sensors.chemical is not None:
            warning = sensors.chemical or any(
                probe.chemical is True and not probe.blocked for probe in sensors.probes
            )
        return SensorimotorTelemetry(
            tick=sensors.tick,
            signal=sensors.signal,
            chemical_warning=warning,
            contact=sensors.contact,
            collided=sensors.collided,
        )
