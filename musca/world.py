"""A deterministic grid fixture with local sensing and guarded body movement."""

from .contracts import (
    Contact,
    ContractError,
    MotorCommand,
    Probe,
    SensorFrame,
    SensorMode,
)


Position = tuple[int, int]
_STEPS = (
    (MotorCommand.EAST, (1, 0)),
    (MotorCommand.SOUTH, (0, 1)),
    (MotorCommand.WEST, (-1, 0)),
    (MotorCommand.NORTH, (0, -1)),
)
_DELTAS = dict(_STEPS)


class GridWorld:
    """Synthetic world state; controllers receive only the result of ``sense``."""

    def __init__(
        self,
        *,
        width: int = 7,
        height: int = 5,
        start: Position = (1, 2),
        beacon: Position = (5, 2),
        walls: frozenset[Position] = frozenset({(3, 1)}),
        hazards: frozenset[Position] = frozenset({(3, 2)}),
    ) -> None:
        if type(width) is not int or type(height) is not int or min(width, height) < 3:
            raise ContractError("grid dimensions must be integers of at least 3")
        self._width = width
        self._height = height
        self._start = self._validate_position(start, "start")
        self._beacon = self._validate_position(beacon, "beacon")
        self._walls = self._validate_cells(walls, "wall")
        self._hazards = self._validate_cells(hazards, "hazard")
        if self._walls & self._hazards:
            raise ContractError("walls and hazards must not overlap")
        if self._start in self._walls | self._hazards:
            raise ContractError("start must be clear of walls and hazards")
        if self._beacon in self._walls | self._hazards:
            raise ContractError("beacon must be clear of walls and hazards")
        self.reset()

    def _validate_position(self, position: Position, name: str) -> Position:
        if (
            not isinstance(position, tuple)
            or len(position) != 2
            or any(type(value) is not int for value in position)
        ):
            raise ContractError(f"{name} must be a pair of integer coordinates")
        x, y = position
        if not (0 < x < self._width - 1 and 0 < y < self._height - 1):
            raise ContractError(f"{name} must lie inside the blocked grid boundary")
        return position

    def _validate_cells(self, cells: frozenset[Position], name: str) -> frozenset[Position]:
        try:
            return frozenset(self._validate_position(cell, name) for cell in cells)
        except TypeError as exc:
            raise ContractError(f"{name} cells must be an iterable of coordinate pairs") from exc

    def reset(self) -> None:
        self._position = self._start
        self._tick = 0
        self._collisions = 0
        self._collided = False
        self._last_action = MotorCommand.STAY

    def configuration(self) -> dict:
        """Evaluator-only initial configuration, never included in sensor frames."""
        return {
            "width": self._width,
            "height": self._height,
            "start": list(self._start),
            "beacon": list(self._beacon),
            "walls": [list(cell) for cell in sorted(self._walls)],
            "hazards": [list(cell) for cell in sorted(self._hazards)],
        }

    @property
    def tick(self) -> int:
        return self._tick

    @property
    def position(self) -> Position:
        return self._position

    @property
    def contact(self) -> Contact:
        if self._position in self._hazards:
            return Contact.HARM
        if self._position == self._beacon:
            return Contact.BEACON
        return Contact.NONE

    @property
    def collisions(self) -> int:
        return self._collisions

    def _blocked(self, position: Position) -> bool:
        x, y = position
        return (
            not (0 < x < self._width - 1 and 0 < y < self._height - 1)
            or position in self._walls
        )

    def _signal(self, position: Position) -> int:
        distance = abs(position[0] - self._beacon[0]) + abs(position[1] - self._beacon[1])
        return max(0, 10 - distance)

    def sense(self, mode: SensorMode) -> SensorFrame:
        if not isinstance(mode, SensorMode):
            raise ContractError("sensor mode must be a SensorMode value")
        chemical_enabled = mode is SensorMode.LIGHT_CHEMICAL
        probes = []
        x, y = self._position
        for direction, (dx, dy) in _STEPS:
            neighbor = (x + dx, y + dy)
            blocked = self._blocked(neighbor)
            probes.append(Probe(
                direction=direction,
                blocked=blocked,
                signal=0 if blocked else self._signal(neighbor),
                chemical=(neighbor in self._hazards) if chemical_enabled and not blocked else None,
            ))
        return SensorFrame(
            tick=self._tick,
            signal=self._signal(self._position),
            chemical=(self._position in self._hazards) if chemical_enabled else None,
            probes=tuple(probes),
            contact=self.contact,
            collided=self._collided,
            last_action=self._last_action,
        )

    def advance(self, action: MotorCommand, *, allow_motion: bool = True) -> None:
        if not isinstance(action, MotorCommand):
            raise ContractError("action must be a MotorCommand value")
        if type(allow_motion) is not bool:
            raise ContractError("allow_motion must be a boolean")
        self._tick += 1
        self._collided = False
        self._last_action = MotorCommand.STAY
        if not allow_motion or action is MotorCommand.STAY:
            return
        dx, dy = _DELTAS[action]
        target = (self._position[0] + dx, self._position[1] + dy)
        if self._blocked(target):
            self._collided = True
            self._collisions += 1
            return
        self._position = target
        self._last_action = action
