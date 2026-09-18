"""Player decisions, information boundaries and terminal gate outcomes."""

import json
from pathlib import Path
import tempfile
import unittest

from musca.bridge import Bridge
from musca.contracts import ContractError, GateTelemetry, Goal, SensorMode
from musca.puzzle import GateSession
from musca.simulation import Simulation
from musca.world import GridWorld
from test_cli import invoke
from test_simulation import StationaryBackend


class GateTests(unittest.TestCase):
    def test_gate_telemetry_rejects_malformed_values(self):
        values = [True, -1, '1', 1.5]
        for index in range(3):
            for invalid in values:
                fields = [0, 9, 4, None, None]
                fields[index] = invalid
                with self.subTest(index=index, invalid=invalid), self.assertRaises(ContractError):
                    GateTelemetry(*fields)
        for chemistry in [('false', False), (1, False), (True, None)]:
            with self.subTest(chemistry=chemistry), self.assertRaises(ContractError):
                GateTelemetry(0, 9, 4, *chemistry)
        with self.assertRaises(ContractError):
            Bridge().gate_events({'amber_reactive': False})

    def test_stationary_backend_cannot_reach_gate(self):
        simulation = Simulation(world=GridWorld(hazards=frozenset()), backend=StationaryBackend(), sensor_mode=SensorMode.LIGHT)
        simulation.submit_goal(Goal.SEEK_SIGNAL)
        result = simulation.run(max_ticks=16)
        self.assertEqual(result['end_reason'], 'tick_limit')
        self.assertFalse(result['metrics']['reached_beacon'])
        self.assertEqual(result['metrics']['moves'], 0)

    def test_unobserved_layouts_have_identical_player_views(self):
        for mode in SensorMode:
            a, b = GateSession(mode, 'a'), GateSession(mode, 'b')
            self.assertEqual(a.view(), b.view())
            if mode == SensorMode.LIGHT:
                self.assertFalse(a.choose('scan'))
                self.assertFalse(b.choose('scan'))
                self.assertEqual(a.view(), b.view())
                self.assertEqual(a.result()['cells_remaining'], 2)

    def test_choices_have_world_consequences_in_both_layouts(self):
        for layout, correct in [('a', 'cobalt'), ('b', 'amber')]:
            for choice in ['amber', 'cobalt']:
                with self.subTest(layout=layout, choice=choice):
                    gate = GateSession(SensorMode.LIGHT, layout)
                    self.assertTrue(gate.choose(choice))
                    self.assertEqual(gate.result()['end_reason'], 'opened' if choice == correct else 'sealed')
                    self.assertEqual(gate.result()['cells_remaining'], 2 if choice == correct else 0)
                    with self.assertRaises(ContractError):
                        gate.choose('scan')

    def test_scan_has_cost_and_revises_hypothesis_without_deciding(self):
        gate = GateSession(SensorMode.LIGHT_CHEMICAL, 'a')
        initial = gate.view()
        self.assertTrue(gate.choose('scan'))
        self.assertFalse(gate.finished)
        self.assertIn('опровергнута', '\n'.join(gate.view()['interpretation']))
        self.assertEqual(gate.result()['cells_remaining'], 1)
        self.assertNotEqual(initial, gate.view())
        self.assertFalse(gate.choose('scan'))
        self.assertEqual(gate.result()['cells_remaining'], 1)
        gate.choose('cobalt')
        self.assertEqual(gate.result()['end_reason'], 'opened')
        self.assertEqual(gate.result()['cells_remaining'], 1)

    def test_scan_in_second_layout_supports_other_choice(self):
        gate = GateSession(SensorMode.LIGHT_CHEMICAL, 'b')
        gate.choose('scan')
        self.assertNotIn('опровергнута', '\n'.join(gate.view()['interpretation']))
        gate.choose('amber')
        self.assertEqual(gate.result()['end_reason'], 'opened')

    def test_rejected_input_never_retains_raw_text_or_changes_state(self):
        gate = GateSession(SensorMode.LIGHT, 'a')
        before = gate.view()
        self.assertFalse(gate.choose('arbitrary personal text'))
        self.assertEqual(before, gate.view())
        self.assertNotIn('arbitrary personal text', json.dumps(gate.result()))
        self.assertEqual(gate.result()['decisions'][-1]['command'], 'invalid')

    def test_leave_and_quit_are_distinct_and_preserve_resource(self):
        for command, reason in [('leave', 'left'), ('quit', 'user_quit')]:
            gate = GateSession(SensorMode.LIGHT_CHEMICAL, 'b')
            gate.choose('scan')
            gate.choose(command)
            self.assertEqual(gate.result()['end_reason'], reason)
            self.assertEqual(gate.result()['cells_remaining'], 1)

    def test_view_and_receipt_cannot_mutate_session(self):
        gate = GateSession(SensorMode.LIGHT, 'a')
        gate.view()['events'].clear()
        gate.result()['trace'].clear()
        self.assertTrue(gate.view()['events'])
        self.assertEqual(len(gate.result()['trace']), 1)

    def test_invalid_configuration_rejected(self):
        for mode, layout in [('light', 'a'), (SensorMode.LIGHT, 'unknown')]:
            with self.assertRaises(ContractError):
                GateSession(mode, layout)


class PuzzleCliTests(unittest.TestCase):
    def run_puzzle(self, *args, commands):
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / 'puzzle.json'
            process = invoke('--puzzle', '--output', str(output), *args, input_text=commands)
            self.assertEqual(process.returncode, 0, process.stderr)
            return process.stdout, json.loads(output.read_text(encoding='utf-8'))

    def test_both_modes_reach_gate_and_complete(self):
        for mode in ['light', 'light_chemical']:
            screen, receipt = self.run_puzzle('--mode', mode, commands='go\ncobalt\n')
            self.assertEqual(receipt['kind'], 'game_puzzle')
            self.assertEqual(receipt['navigation']['end_reason'], 'beacon')
            self.assertEqual(receipt['navigation']['metrics']['ticks'], 4)
            self.assertEqual(receipt['result']['end_reason'], 'opened')
            self.assertEqual(receipt['result']['cells_remaining'], 2)
            self.assertNotIn('layout', screen)

    def test_chemical_loop_records_human_choice_after_evidence(self):
        screen, receipt = self.run_puzzle('--mode', 'light_chemical', commands='go\nscan\ncobalt\n')
        self.assertIn('опровергнута', screen)
        self.assertEqual(receipt['result']['cells_remaining'], 1)
        self.assertEqual([d['command'] for d in receipt['result']['decisions']], ['scan', 'cobalt'])
        self.assertEqual(receipt['config']['input_mode'], 'human')

    def test_both_layouts_reproduce_in_fresh_processes(self):
        for layout, choice in [('a', 'cobalt'), ('b', 'amber')]:
            args = ('--mode', 'light_chemical', '--layout', layout)
            commands = f'go\nscan\n{choice}\n'
            first_screen, first = self.run_puzzle(*args, commands=commands)
            _, second = self.run_puzzle(*args, commands=commands)
            self.assertEqual(first['navigation'], second['navigation'])
            self.assertEqual(first['result'], second['result'])
            self.assertEqual(first['result']['end_reason'], 'opened')
            self.assertIn('Итоговый запас: 1', first_screen)

    def test_light_cannot_read_chemistry_and_wrong_choice_seals_gate(self):
        screen, receipt = self.run_puzzle(commands='go\nscan\namber\n')
        self.assertEqual(receipt['result']['end_reason'], 'sealed')
        self.assertFalse(receipt['result']['scan_used'])
        self.assertNotIn('опровергнута', screen)

    def test_quit_before_gate_does_not_create_puzzle_success(self):
        _, receipt = self.run_puzzle(commands='quit\n')
        self.assertEqual(receipt['result']['end_reason'], 'not_reached')
        self.assertEqual(receipt['navigation']['end_reason'], 'user_quit')

    def test_tick_limit_does_not_open_gate(self):
        _, receipt = self.run_puzzle('--ticks', '1', commands='go\n')
        self.assertEqual(receipt['result']['end_reason'], 'not_reached')
        self.assertEqual(receipt['navigation']['end_reason'], 'tick_limit')

    def test_eof_at_gate_is_not_success(self):
        _, receipt = self.run_puzzle(commands='go\n')
        self.assertEqual(receipt['result']['end_reason'], 'user_quit')

    def test_puzzle_options_are_not_silently_ignored(self):
        for args in [('--puzzle', '--demo'), ('--puzzle', '--json'), ('--layout', 'b')]:
            self.assertEqual(invoke(*args).returncode, 2)


if __name__ == '__main__':
    unittest.main()
