"""Composition of the synthetic world, replaceable backend, Bridge and interpreter."""

from dataclasses import asdict
from typing import Mapping

from .bridge import Bridge
from .contracts import (
    Contact, ContractError, Goal, HighLevelIntent, MotorCommand, MuscaBackend,
    SensorimotorTelemetry, SensorMode,
)
from .interpretation import interpret
from .world import GridWorld


class Simulation:
    def __init__(self, *, world: GridWorld, backend: MuscaBackend, sensor_mode: SensorMode) -> None:
        if not isinstance(sensor_mode, SensorMode):
            raise ContractError("sensor_mode must be a SensorMode")
        self.world = world
        self.backend = backend
        self.sensor_mode = sensor_mode
        self.world.reset()
        self.backend.reset()
        self.bridge = Bridge()
        self._faulted = False
        self.decisions: list[dict] = []
        self.trace = [self._frame(MotorCommand.STAY)]

    @property
    def finished(self) -> bool:
        return self.world.contact != Contact.NONE

    def submit(self, payload: Mapping[str, object]) -> HighLevelIntent:
        if self._faulted:
            raise ContractError("episode failed; create a new Simulation")
        try:
            parsed = HighLevelIntent.parse(payload)
            if parsed.sensor_mode != self.sensor_mode:
                raise ContractError("sensor mode is fixed for the episode")
            intent = self.bridge.submit(payload, self.world.tick)
        except ContractError as exc:
            self.bridge.clear()
            # Never retain arbitrary rejected input, which may contain private data.
            self.decisions.append({'tick': self.world.tick, 'accepted': False, 'error': str(exc)})
            raise
        self.decisions.append({'tick': self.world.tick, 'accepted': True, 'intent': asdict(intent)})
        return intent

    def submit_goal(self, goal: Goal, *, ttl: int = 16) -> HighLevelIntent:
        if type(ttl) is not int:
            self.bridge.clear()
            raise ContractError("ttl must be an integer")
        return self.submit({
            'goal': goal, 'sensor_mode': self.sensor_mode,
            'issued_at': self.world.tick, 'expires_at': self.world.tick + ttl,
        })

    def _frame(self, requested: MotorCommand) -> dict:
        sensors = self.world.sense(self.sensor_mode)
        telemetry = self.backend.observe(sensors)
        if not isinstance(telemetry, SensorimotorTelemetry) or telemetry.tick != sensors.tick:
            raise ContractError("backend telemetry must match the current sensor tick")
        events = self.bridge.events(telemetry)
        return {
            'tick': self.world.tick,
            # Evaluator-only data; not passed to Bridge or the interpreter.
            'position': list(self.world.position),
            'requested_action': requested.value,
            'action': sensors.last_action.value,
            'events': [asdict(event) for event in events],
            'interpretation': list(interpret(events)),
        }

    def step(self) -> dict:
        if self._faulted:
            raise ContractError("episode failed; create a new Simulation")
        if self.finished:
            raise RuntimeError("episode already terminated by observed contact")
        start_tick = self.world.tick
        requested = MotorCommand.STAY
        try:
            sensors = self.world.sense(self.sensor_mode)
            modulation = self.bridge.modulation(self.world.tick)
            requested = self.backend.act(sensors, modulation)
            if not isinstance(requested, MotorCommand):
                raise ContractError("backend must return a MotorCommand")
            permitted = modulation.approach_signal and self.world.tick < modulation.valid_until
            self.world.advance(requested, allow_motion=permitted)
            frame = self._frame(requested)
        except Exception as exc:
            # A plugin fault stops this episode; do not continue with old permission or
            # manufacture semantic observations. Keep body actions already executed.
            self.bridge.clear()
            self._faulted = True
            actual = self.world.sense(self.sensor_mode).last_action if self.world.tick > start_tick else MotorCommand.STAY
            self.trace.append({
                'tick': self.world.tick, 'position': list(self.world.position),
                'requested_action': requested.value if isinstance(requested, MotorCommand) else 'invalid',
                'action': actual.value, 'events': [], 'interpretation': [],
                'error': 'backend_contract_error',
            })
            raise ContractError("backend step failed; episode stopped") from exc
        self.trace.append(frame)
        return frame

    def run(self, *, max_ticks: int = 16) -> dict:
        if type(max_ticks) is not int or not 1 <= max_ticks <= 256:
            raise ContractError("max_ticks must be an integer between 1 and 256")
        while self.world.tick < max_ticks and not self.finished:
            self.step()
        return self.result('tick_limit')

    def result(self, end_reason: str) -> dict:
        if self._faulted:
            end_reason = 'backend_contract_error'
        elif self.finished:
            end_reason = self.world.contact.value
        return {
            'world_model': 'integer-grid-v1',
            'world': self.world.configuration(),
            'sensor_mode': self.sensor_mode.value,
            'backend': type(self.backend).__name__,
            'end_reason': end_reason,
            'decisions': list(self.decisions),
            'trace': list(self.trace),
            'metrics': {
                'ticks': self.world.tick,
                'moves': sum(frame['action'] != 'stay' for frame in self.trace[1:]),
                'collisions': self.world.collisions,
                'reached_beacon': self.world.contact == Contact.BEACON,
                'harm_observed': self.world.contact == Contact.HARM,
            },
        }
