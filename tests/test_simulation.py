"""Acceptance tests for the synthetic fixture, not scientific comparisons."""

import unittest
from dataclasses import replace

from musca.backends import ScriptedBackend
from musca.contracts import (
    Contact, ContractError, Goal, MotorCommand, SensorMode, SensorimotorTelemetry,
)
from musca.simulation import Simulation
from musca.world import GridWorld


class StationaryBackend:
    def reset(self):
        pass

    def act(self, sensors, modulation):
        return MotorCommand.STAY

    def observe(self, sensors):
        return SensorimotorTelemetry(
            sensors.tick, sensors.signal, None, sensors.contact, sensors.collided,
        )


class DisobedientBackend(StationaryBackend):
    def act(self, sensors, modulation):
        return MotorCommand.EAST


def make_simulation(mode=SensorMode.LIGHT, backend=None, world=None):
    return Simulation(
        world=world if world is not None else GridWorld(),
        backend=backend if backend is not None else ScriptedBackend(),
        sensor_mode=mode,
    )


class SimulationTests(unittest.TestCase):
    def test_stale_telemetry_is_rejected_and_episode_stays_stopped(self):
        class StaleBackend(ScriptedBackend):
            def observe(self, sensors):
                return replace(super().observe(sensors), tick=0)

        sim = make_simulation(backend=StaleBackend())
        sim.submit_goal(Goal.SEEK_SIGNAL)
        with self.assertRaises(ContractError):
            sim.step()
        self.assertEqual(sim.world.tick, 1)  # Body already moved before bad feedback arrived.
        self.assertEqual(sim.world.position, (2, 2))
        with self.assertRaises(ContractError):
            sim.submit_goal(Goal.SEEK_SIGNAL)
        with self.assertRaises(ContractError):
            sim.step()
        result = sim.result('interrupted')
        self.assertEqual(result['end_reason'], 'backend_contract_error')
        self.assertEqual(result['metrics']['moves'], 1)
        self.assertEqual(result['trace'][-1]['events'], [])
        self.assertEqual(sim.world.position, (2, 2))

    def test_default_light_observes_harm_before_source(self):
        sim = make_simulation()
        sim.submit_goal(Goal.SEEK_SIGNAL)
        result = sim.run(max_ticks=16)
        self.assertEqual(result['end_reason'], 'harm')
        self.assertEqual(result['metrics']['ticks'], 2)
        self.assertEqual(result['trace'][-1]['position'], [3, 2])
        self.assertEqual(result['metrics']['collisions'], 0)
        self.assertFalse(result['metrics']['reached_beacon'])
        self.assertNotIn('chemical_warning', [
            event['kind'] for frame in result['trace'] for event in frame['events']
        ])

    def test_extra_channel_permits_detour_in_same_world(self):
        sim = make_simulation(SensorMode.LIGHT_CHEMICAL)
        sim.submit_goal(Goal.SEEK_SIGNAL)
        result = sim.run(max_ticks=16)
        self.assertEqual(result['end_reason'], 'beacon')
        self.assertEqual(result['metrics']['ticks'], 6)
        self.assertEqual(result['trace'][-1]['position'], [5, 2])
        self.assertEqual(result['metrics']['collisions'], 0)
        self.assertTrue(any(
            e['kind'] == 'chemical_warning' for f in result['trace'] for e in f['events']
        ))
        self.assertFalse(any(
            e['kind'] == 'harm' for f in result['trace'] for e in f['events']
        ))

    def test_player_can_stop_after_new_warning(self):
        sim = make_simulation(SensorMode.LIGHT_CHEMICAL)
        sim.submit_goal(Goal.SEEK_SIGNAL)
        frame = sim.step()
        self.assertIn('chemical_warning', [e['kind'] for e in frame['events']])
        position = sim.world.position
        sim.submit_goal(Goal.HOLD)
        sim.run(max_ticks=6)
        self.assertEqual(sim.world.position, position)
        self.assertEqual(sim.world.contact, Contact.NONE)

    def test_interchangeable_backend_cannot_be_moved_by_runner(self):
        sim = make_simulation(backend=StationaryBackend())
        sim.submit_goal(Goal.SEEK_SIGNAL)
        result = sim.run(max_ticks=8)
        self.assertEqual(sim.world.position, (1, 2))
        self.assertFalse(result['metrics']['reached_beacon'])
        self.assertEqual(result['metrics']['moves'], 0)

    def test_executor_stops_even_disobedient_backend_at_expiry(self):
        sim = make_simulation(backend=DisobedientBackend(), world=GridWorld(hazards=frozenset()))
        sim.submit_goal(Goal.SEEK_SIGNAL, ttl=1)
        sim.step()
        self.assertEqual(sim.world.position, (2, 2))
        sim.step()
        self.assertEqual(sim.world.position, (2, 2))
        self.assertEqual(sim.trace[-1]['action'], 'stay')

    def test_no_intent_prevents_disobedient_backend_motion(self):
        sim = make_simulation(backend=DisobedientBackend())
        sim.run(max_ticks=4)
        self.assertEqual(sim.world.position, (1, 2))

    def test_bad_intent_clears_old_motion_permission(self):
        sim = make_simulation(backend=DisobedientBackend())
        sim.submit_goal(Goal.SEEK_SIGNAL)
        with self.assertRaises(ContractError):
            sim.submit({'goal': 'seek_signal', 'dx': 1})
        sim.step()
        self.assertEqual(sim.world.position, (1, 2))
        self.assertFalse(sim.decisions[-1]['accepted'])

    def test_mode_cannot_be_changed_mid_episode(self):
        sim = make_simulation()
        sim.submit_goal(Goal.SEEK_SIGNAL)
        with self.assertRaises(ContractError):
            sim.submit({
                'goal': 'seek_signal', 'sensor_mode': 'light_chemical',
                'issued_at': 0, 'expires_at': 8,
            })
        sim.step()
        self.assertEqual(sim.world.position, (1, 2))

    def test_same_local_observations_have_same_actions_and_interpretation(self):
        # These worlds differ at an unobservable hazard, beyond the one-cell probes.
        a = make_simulation(world=GridWorld(hazards=frozenset({(3, 2)})))
        b = make_simulation(world=GridWorld(hazards=frozenset({(4, 1)})))
        self.assertEqual(a.trace[0]['events'], b.trace[0]['events'])
        self.assertEqual(a.trace[0]['interpretation'], b.trace[0]['interpretation'])
        for sim in (a, b):
            sim.submit_goal(Goal.SEEK_SIGNAL)
        self.assertEqual(a.step()['action'], b.step()['action'])

    def test_reusing_world_and_backend_resets_episode_state(self):
        backend, world = ScriptedBackend(), GridWorld()
        results = []
        for mode in (SensorMode.LIGHT, SensorMode.LIGHT_CHEMICAL, SensorMode.LIGHT):
            sim = make_simulation(mode, backend, world)
            sim.submit_goal(Goal.SEEK_SIGNAL)
            results.append(sim.run(max_ticks=16))
        self.assertEqual(results[0], results[2])
        self.assertNotEqual(results[0]['trace'], results[1]['trace'])

    def test_terminal_episode_cannot_keep_moving(self):
        sim = make_simulation()
        sim.submit_goal(Goal.SEEK_SIGNAL)
        sim.run(max_ticks=16)
        with self.assertRaises(RuntimeError):
            sim.step()
        self.assertEqual(sim.world.position, (3, 2))

    def test_world_blocks_collision_and_suppresses_unpermitted_motor(self):
        world = GridWorld()
        world.advance(MotorCommand.WEST)
        self.assertEqual(world.position, (1, 2))
        self.assertTrue(world.sense(SensorMode.LIGHT).collided)
        self.assertEqual(world.collisions, 1)
        world.advance(MotorCommand.EAST, allow_motion=False)
        self.assertEqual(world.position, (1, 2))
        self.assertEqual(world.sense(SensorMode.LIGHT).last_action, MotorCommand.STAY)

    def test_blocked_probe_hides_signal_and_chemistry(self):
        world = GridWorld()
        west = next(p for p in world.sense(SensorMode.LIGHT_CHEMICAL).probes if p.direction == MotorCommand.WEST)
        self.assertTrue(west.blocked)
        self.assertEqual(west.signal, 0)
        self.assertIsNone(west.chemical)

    def test_invalid_backend_motor_is_rejected_without_motion(self):
        class InvalidBackend(StationaryBackend):
            def act(self, sensors, modulation):
                return 'east'

        sim = make_simulation(backend=InvalidBackend())
        sim.submit_goal(Goal.SEEK_SIGNAL)
        with self.assertRaises(ContractError):
            sim.step()
        self.assertEqual(sim.world.tick, 0)
        self.assertEqual(sim.world.position, (1, 2))

    def test_run_budget_is_bounded(self):
        for value in (0, -1, True, 1.5, 257):
            with self.subTest(value=value), self.assertRaises(ContractError):
                make_simulation().run(max_ticks=value)


if __name__ == '__main__':
    unittest.main()
