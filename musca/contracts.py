"""Version 0.1 contracts. Upper intentions have no motor or coordinate fields."""

from dataclasses import dataclass
from enum import StrEnum
from typing import Mapping, Protocol


MAX_INTENT_TICKS = 32
MIN_INTENT_INTERVAL = 4


class ContractError(ValueError):
    """An input does not satisfy the explicit prototype contract."""


class Goal(StrEnum):
    SEEK_SIGNAL = "seek_signal"
    HOLD = "hold"


class SensorMode(StrEnum):
    LIGHT = "light"
    LIGHT_CHEMICAL = "light_chemical"


class MotorCommand(StrEnum):
    EAST = "east"
    SOUTH = "south"
    WEST = "west"
    NORTH = "north"
    STAY = "stay"


class Contact(StrEnum):
    NONE = "none"
    BEACON = "beacon"
    HARM = "harm"


class EventKind(StrEnum):
    SIGNAL = "signal"
    CHEMICAL_WARNING = "chemical_warning"
    COLLISION = "collision"
    BEACON = "beacon"
    HARM = "harm"
    AMBER_CHARGE = "amber_charge"
    COBALT_CHARGE = "cobalt_charge"
    AMBER_REACTIVE = "amber_reactive"
    COBALT_REACTIVE = "cobalt_reactive"


@dataclass(frozen=True, slots=True)
class HighLevelIntent:
    goal: Goal
    sensor_mode: SensorMode
    issued_at: int
    expires_at: int

    def __post_init__(self) -> None:
        if not isinstance(self.goal, Goal) or not isinstance(self.sensor_mode, SensorMode):
            raise ContractError("goal and sensor_mode must be valid enum values")
        if type(self.issued_at) is not int or type(self.expires_at) is not int:
            raise ContractError("intent ticks must be integers, not booleans")
        if self.issued_at < 0 or not 1 <= self.expires_at - self.issued_at <= MAX_INTENT_TICKS:
            raise ContractError("intent lifetime must be 1..32 ticks with a nonnegative start")

    @classmethod
    def parse(cls, payload: Mapping[str, object]) -> "HighLevelIntent":
        expected = {"goal", "sensor_mode", "issued_at", "expires_at"}
        if not isinstance(payload, Mapping) or set(payload) != expected:
            raise ContractError("intent requires exactly goal, sensor_mode, issued_at, expires_at")
        try:
            return cls(
                Goal(payload["goal"]), SensorMode(payload["sensor_mode"]),
                payload["issued_at"], payload["expires_at"],
            )
        except (TypeError, ValueError) as exc:
            raise ContractError("invalid intent values") from exc


@dataclass(frozen=True, slots=True)
class MuscaModulation:
    approach_signal: bool
    avoid_chemical: bool
    valid_until: int


@dataclass(frozen=True, slots=True)
class Probe:
    direction: MotorCommand
    blocked: bool
    signal: int
    chemical: bool | None


@dataclass(frozen=True, slots=True)
class SensorFrame:
    tick: int
    signal: int
    chemical: bool | None
    probes: tuple[Probe, ...]
    contact: Contact
    collided: bool
    last_action: MotorCommand


@dataclass(frozen=True, slots=True)
class SensorimotorTelemetry:
    tick: int
    signal: int
    chemical_warning: bool | None
    contact: Contact
    collided: bool

    def __post_init__(self) -> None:
        if type(self.tick) is not int or self.tick < 0:
            raise ContractError("telemetry tick must be a nonnegative integer")
        if type(self.signal) is not int or self.signal < 0:
            raise ContractError("telemetry signal must be a nonnegative integer")
        if self.chemical_warning is not None and type(self.chemical_warning) is not bool:
            raise ContractError("chemical_warning must be a boolean or None")
        if not isinstance(self.contact, Contact) or type(self.collided) is not bool:
            raise ContractError("telemetry contact or collision value is invalid")


@dataclass(frozen=True, slots=True)
class SemanticEvent:
    tick: int
    kind: EventKind
    value: int | bool | str


@dataclass(frozen=True, slots=True)
class GateTelemetry:
    """Only measured gate properties, never an answer or hidden layout id."""

    tick: int
    amber_charge: int
    cobalt_charge: int
    amber_reactive: bool | None
    cobalt_reactive: bool | None

    def __post_init__(self) -> None:
        for value in (self.tick, self.amber_charge, self.cobalt_charge):
            if type(value) is not int or value < 0:
                raise ContractError("gate measurements must be nonnegative integers")
        for value in (self.amber_reactive, self.cobalt_reactive):
            if value is not None and type(value) is not bool:
                raise ContractError("chemical measurement must be boolean or None")
        if (self.amber_reactive is None) != (self.cobalt_reactive is None):
            raise ContractError("a gate probe measures both reagents together")


class MuscaBackend(Protocol):
    def reset(self) -> None: ...

    def act(self, sensors: SensorFrame, modulation: MuscaModulation) -> MotorCommand: ...

    def observe(self, sensors: SensorFrame) -> SensorimotorTelemetry: ...
