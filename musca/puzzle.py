"""A bounded player-choice puzzle following arrival; no motor control here."""

from copy import deepcopy
from dataclasses import asdict

from .bridge import Bridge
from .contracts import ContractError, GateTelemetry, SensorMode
from .interpretation import interpret_gate


class GateWorld:
    """Hidden synthetic reagent properties, independent of the interpreter."""

    def __init__(self, layout: str) -> None:
        if layout not in ('a', 'b'):
            raise ContractError("gate layout must be a or b")
        self._neutral = 'cobalt' if layout == 'a' else 'amber'

    def observe(self, *, tick: int, chemical: bool) -> GateTelemetry:
        if type(chemical) is not bool:
            raise ContractError("chemical permission must be a boolean")
        return GateTelemetry(
            tick, 9, 4,
            self._neutral != 'amber' if chemical else None,
            self._neutral != 'cobalt' if chemical else None,
        )

    def use(self, reagent: str) -> bool:
        if reagent not in ('amber', 'cobalt'):
            raise ContractError("unknown reagent")
        return reagent == self._neutral


class GateSession:
    """Human commands buy evidence or affect the gate, never choose body steps."""

    def __init__(self, mode: SensorMode, layout: str) -> None:
        if not isinstance(mode, SensorMode):
            raise ContractError("gate sensor mode must be a SensorMode")
        self._world = GateWorld(layout)
        self._bridge = Bridge()
        self._mode = mode
        self._layout = layout
        self._cells = 2
        self._scanned = False
        self._tick = 0
        self._outcome = 'pending'
        self._decisions: list[dict] = []
        self._trace = [self._observe()]

    @property
    def finished(self) -> bool:
        return self._outcome != 'pending'

    def _observe(self) -> dict:
        telemetry = self._world.observe(tick=self._tick, chemical=self._scanned)
        events = self._bridge.gate_events(telemetry)
        return {
            'tick': self._tick,
            'events': [asdict(event) for event in events],
            'interpretation': list(interpret_gate(events)),
        }

    def view(self) -> dict:
        """Player-safe view; layout and hidden truth are not included."""
        return deepcopy({
            **self._trace[-1], 'cells_remaining': self._cells,
            'scan_available': self._mode == SensorMode.LIGHT_CHEMICAL and not self._scanned and not self.finished,
            'end_reason': self._outcome,
        })

    def choose(self, command: str) -> bool:
        if self.finished:
            raise ContractError("gate episode already finished")
        known = type(command) is str and command in ('scan', 'amber', 'cobalt', 'leave', 'quit')
        allowed = known and (command != 'scan' or self.view()['scan_available'])
        self._decisions.append({
            'tick': self._tick, 'command': command if known else 'invalid', 'accepted': allowed,
        })
        if not allowed:
            return False
        self._tick += 1
        if command == 'scan':
            self._cells -= 1
            self._scanned = True
            self._trace.append(self._observe())
        elif command in ('amber', 'cobalt'):
            opened = self._world.use(command)
            self._outcome = 'opened' if opened else 'sealed'
            if not opened:
                self._cells = 0
        else:
            self._outcome = 'left' if command == 'leave' else 'user_quit'
        return True

    def result(self) -> dict:
        """Evaluator receipt; layout is diagnostic and not passed to interpretation."""
        return deepcopy({
            'fixture_id': 'gate-v1', 'layout': self._layout,
            'sensor_mode': self._mode.value, 'end_reason': self._outcome,
            'cells_remaining': self._cells, 'scan_used': self._scanned,
            'decision_steps': self._tick, 'decisions': self._decisions, 'trace': self._trace,
        })
