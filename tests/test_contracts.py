"""Acceptance tests for the upper/lower control boundary, without a world."""

from dataclasses import FrozenInstanceError
import unittest

from musca.bridge import Bridge
from musca.contracts import (
    Contact,
    ContractError,
    EventKind,
    Goal,
    HighLevelIntent,
    SensorMode,
    SensorimotorTelemetry,
)


def payload(**changes):
    result = {
        "goal": "seek_signal",
        "sensor_mode": "light",
        "issued_at": 0,
        "expires_at": 16,
    }
    result.update(changes)
    return result


def invalid_payloads():
    cases = [
        ("not a mapping: none", None),
        ("not a mapping: list", list(payload().items())),
        ("not a mapping: string", "seek_signal"),
        ("unknown goal", payload(goal="turn_left")),
        ("unknown sensor", payload(sensor_mode="omniscient")),
        ("list goal", payload(goal=[])),
        ("object goal", payload(goal={})),
        ("list sensor", payload(sensor_mode=[])),
        ("negative start", payload(issued_at=-1)),
        ("zero lifetime", payload(expires_at=0)),
        ("negative end", payload(expires_at=-1)),
        ("too long", payload(expires_at=33)),
    ]
    for field in payload():
        missing = payload()
        del missing[field]
        cases.append((f"missing {field}", missing))
    for field, value in (
        ("motor", "east"),
        ("direction", "north"),
        ("coordinates", [1, 2]),
        ("target_x", 2),
        ("action", "stay"),
        ("unknown", None),
    ):
        cases.append((f"extra {field}", payload(**{field: value})))
    for field in ("issued_at", "expires_at"):
        for value in (True, False, 1.0, "1", None, []):
            cases.append((f"{field} is {value!r}", payload(**{field: value})))
    return cases


class IntentParsingTests(unittest.TestCase):
    def test_invalid_input_matrix(self):
        for label, candidate in invalid_payloads():
            with self.subTest(label=label):
                with self.assertRaises(ContractError):
                    HighLevelIntent.parse(candidate)

    def test_minimum_and_maximum_lifetimes_are_valid(self):
        for lifetime in (1, 32):
            with self.subTest(lifetime=lifetime):
                intent = HighLevelIntent.parse(
                    payload(issued_at=7, expires_at=7 + lifetime)
                )
                self.assertEqual(intent.goal, Goal.SEEK_SIGNAL)
                self.assertEqual(intent.sensor_mode, SensorMode.LIGHT)
                self.assertEqual(intent.expires_at - intent.issued_at, lifetime)

    def test_constructor_does_not_bypass_enum_or_tick_validation(self):
        with self.assertRaises(ContractError):
            HighLevelIntent("seek_signal", SensorMode.LIGHT, 0, 8)
        with self.assertRaises(ContractError):
            HighLevelIntent(Goal.SEEK_SIGNAL, SensorMode.LIGHT, True, 8)
        with self.assertRaises(ContractError):
            HighLevelIntent(Goal.SEEK_SIGNAL, SensorMode.LIGHT, 0, 33)


class BridgeIntentTests(unittest.TestCase):
    def assert_stopped(self, bridge, tick):
        self.assertFalse(bridge.modulation(tick).approach_signal)

    def test_invalid_submission_clears_previous_intent(self):
        for label, candidate in invalid_payloads():
            with self.subTest(label=label):
                bridge = Bridge()
                bridge.submit(payload(), tick=0)
                self.assertTrue(bridge.modulation(0).approach_signal)
                with self.assertRaises(ContractError):
                    bridge.submit(candidate, tick=0)
                self.assertIsNone(bridge.intent)
                self.assert_stopped(bridge, 0)

    def test_submit_requires_payload_even_for_valid_intent_object(self):
        bridge = Bridge()
        intent = HighLevelIntent.parse(payload())
        with self.assertRaises(ContractError):
            bridge.submit(intent, tick=0)
        self.assertIsNone(bridge.intent)

    def test_invalid_current_tick_clears_previous_intent(self):
        for tick in (-1, True, False, 0.0, "0", None, []):
            with self.subTest(tick=tick):
                bridge = Bridge()
                bridge.submit(payload(), tick=0)
                with self.assertRaises(ContractError):
                    bridge.submit(payload(), tick=tick)
                self.assertIsNone(bridge.intent)
                self.assert_stopped(bridge, 0)

    def test_delayed_future_and_expired_submissions_fail_closed(self):
        for issued_at, expires_at in ((3, 10), (5, 10), (0, 3)):
            with self.subTest(issued_at=issued_at, expires_at=expires_at):
                bridge = Bridge()
                bridge.submit(payload(), tick=0)
                with self.assertRaises(ContractError):
                    bridge.submit(
                        payload(issued_at=issued_at, expires_at=expires_at), tick=4
                    )
                self.assertIsNone(bridge.intent)
                self.assert_stopped(bridge, 4)

    def test_exclusive_expiration_and_inactive_bridge(self):
        bridge = Bridge()
        self.assert_stopped(bridge, 0)
        bridge.submit(payload(expires_at=32), tick=0)
        self.assertTrue(bridge.modulation(0).approach_signal)
        self.assertTrue(bridge.modulation(31).approach_signal)
        self.assertEqual(bridge.modulation(31).valid_until, 32)
        self.assert_stopped(bridge, 32)
        self.assert_stopped(bridge, 33)

    def test_sensor_mode_maps_to_chemical_avoidance(self):
        for mode, avoid in (("light", False), ("light_chemical", True)):
            with self.subTest(mode=mode):
                bridge = Bridge()
                bridge.submit(payload(sensor_mode=mode), tick=0)
                modulation = bridge.modulation(0)
                self.assertTrue(modulation.approach_signal)
                self.assertEqual(modulation.avoid_chemical, avoid)

    def test_seek_rate_limit_is_inclusive_at_four_ticks(self):
        bridge = Bridge()
        bridge.submit(payload(), tick=0)
        with self.assertRaises(ContractError):
            bridge.submit(payload(issued_at=3), tick=3)
        self.assertIsNone(bridge.intent)
        self.assert_stopped(bridge, 3)
        bridge.submit(payload(issued_at=4), tick=4)
        self.assertTrue(bridge.modulation(4).approach_signal)

    def test_hold_stops_immediately_without_resetting_seek_rate_history(self):
        bridge = Bridge()
        bridge.submit(payload(), tick=0)
        held = bridge.submit(payload(goal="hold", issued_at=1), tick=1)
        self.assertEqual(held.goal, Goal.HOLD)
        self.assert_stopped(bridge, 1)
        with self.assertRaises(ContractError):
            bridge.submit(payload(issued_at=2), tick=2)
        self.assert_stopped(bridge, 2)
        bridge.submit(payload(issued_at=4), tick=4)
        self.assertTrue(bridge.modulation(4).approach_signal)

    def test_clear_stops_but_does_not_reset_seek_rate_history(self):
        bridge = Bridge()
        bridge.submit(payload(), tick=0)
        bridge.clear()
        self.assertIsNone(bridge.intent)
        self.assert_stopped(bridge, 0)
        with self.assertRaises(ContractError):
            bridge.submit(payload(issued_at=1), tick=1)
        bridge.submit(payload(issued_at=4), tick=4)
        self.assertTrue(bridge.modulation(4).approach_signal)

    def test_accepted_intent_is_immutable_and_detached_from_input(self):
        bridge = Bridge()
        original = payload()
        accepted = bridge.submit(original, tick=0)
        original["goal"] = "hold"
        original["expires_at"] = 0
        self.assertEqual(bridge.intent, accepted)
        self.assertEqual(accepted.goal, Goal.SEEK_SIGNAL)
        self.assertEqual(accepted.expires_at, 16)
        self.assertTrue(bridge.modulation(0).approach_signal)
        with self.assertRaises(FrozenInstanceError):
            accepted.goal = Goal.HOLD


class BridgeEventTests(unittest.TestCase):
    def test_malformed_telemetry_cannot_become_a_fact(self):
        valid = dict(tick=0, signal=4, chemical_warning=None, contact=Contact.NONE, collided=False)
        invalid_fields = (
            ('tick', True), ('tick', -1), ('signal', '4'), ('signal', True),
            ('signal', -1), ('chemical_warning', 'false'), ('chemical_warning', 1),
            ('contact', 'beacon'), ('contact', None), ('collided', 'false'),
        )
        for field, value in invalid_fields:
            with self.subTest(field=field, value=value), self.assertRaises(ContractError):
                SensorimotorTelemetry(**(valid | {field: value}))

    def test_bridge_rejects_objects_outside_telemetry_contract(self):
        class LooseTelemetry:
            tick, signal, chemical_warning, contact, collided = 0, 10, None, Contact.BEACON, False

        with self.assertRaises(ContractError):
            Bridge().events(LooseTelemetry())

    def telemetry(self, *, warning=None, contact=Contact.NONE, collided=False):
        return SensorimotorTelemetry(
            tick=7,
            signal=4,
            chemical_warning=warning,
            contact=contact,
            collided=collided,
        )

    def test_signal_event_exists_without_contact_or_warning(self):
        bridge = Bridge()
        for warning in (None, False):
            with self.subTest(warning=warning):
                events = bridge.events(self.telemetry(warning=warning))
                self.assertIsInstance(events, tuple)
                self.assertEqual([event.kind for event in events], [EventKind.SIGNAL])
                self.assertEqual(events[0].tick, 7)
                self.assertEqual(events[0].value, 4)

    def test_warning_is_observation_not_harm(self):
        events = Bridge().events(self.telemetry(warning=True))
        self.assertEqual(
            {event.kind for event in events},
            {EventKind.SIGNAL, EventKind.CHEMICAL_WARNING},
        )
        self.assertTrue(all(event.tick == 7 for event in events))

    def test_contact_events_require_observed_contact(self):
        for contact, kind in ((Contact.BEACON, EventKind.BEACON), (Contact.HARM, EventKind.HARM)):
            with self.subTest(contact=contact):
                events = Bridge().events(self.telemetry(contact=contact))
                self.assertEqual({event.kind for event in events}, {EventKind.SIGNAL, kind})
                self.assertEqual(len(events), 2)

    def test_collision_is_independent_of_chemical_warning_and_harm(self):
        events = Bridge().events(self.telemetry(collided=True))
        self.assertEqual(
            {event.kind for event in events}, {EventKind.SIGNAL, EventKind.COLLISION}
        )

    def test_cooccurring_observations_are_preserved_once_each(self):
        events = Bridge().events(
            self.telemetry(warning=True, contact=Contact.HARM, collided=True)
        )
        self.assertEqual(
            {event.kind for event in events},
            {EventKind.SIGNAL, EventKind.CHEMICAL_WARNING, EventKind.COLLISION, EventKind.HARM},
        )
        self.assertEqual(len(events), 4)


if __name__ == "__main__":
    unittest.main()
