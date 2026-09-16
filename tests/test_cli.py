"""Fresh-process checks for user decisions, deterministic results and receipts."""

import hashlib
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest


ROOT = Path(__file__).resolve().parent.parent


def invoke(*arguments, input_text=''):
    return subprocess.run(
        [sys.executable, '-m', 'musca', *arguments], cwd=ROOT,
        input=input_text, capture_output=True, encoding='utf-8', timeout=15,
    )


class CliTests(unittest.TestCase):
    def test_fresh_processes_reproduce_results_in_both_modes(self):
        for mode, outcome in (('light', 'harm'), ('light_chemical', 'beacon')):
            with self.subTest(mode=mode):
                a = invoke('--demo', '--json', '--mode', mode)
                b = invoke('--demo', '--json', '--mode', mode)
                self.assertEqual(a.returncode, 0, a.stderr)
                self.assertEqual(b.returncode, 0, b.stderr)
                first, second = json.loads(a.stdout), json.loads(b.stdout)
                self.assertEqual(first['result'], second['result'])
                self.assertEqual(first['result']['end_reason'], outcome)
                self.assertEqual(first['kind'], 'engineering_demo')
                for name, digest in first['provenance']['source_sha256'].items():
                    self.assertEqual(hashlib.sha256((ROOT / name).read_bytes()).hexdigest(), digest)

    def test_terminal_human_can_stop_after_warning(self):
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / 'decision.json'
            completed = invoke('--mode', 'light_chemical', '--output', str(output), input_text='go\nwait\nquit\n')
            self.assertEqual(completed.returncode, 0, completed.stderr)
            result = json.loads(output.read_text(encoding='utf-8'))['result']
            self.assertEqual(result['end_reason'], 'user_quit')
            self.assertEqual(result['trace'][-1]['position'], [2, 2])
            self.assertEqual(result['metrics']['moves'], 1)
            self.assertEqual([d['intent']['goal'] for d in result['decisions']], ['seek_signal', 'hold'])
            self.assertIn('[HYP]', completed.stdout)
            self.assertNotIn('[3, 2]', completed.stdout)  # Hidden hazard coordinates are evaluator-only.

    def test_continuing_warning_does_not_loop_without_progress(self):
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / 'continue.json'
            completed = invoke('--mode', 'light_chemical', '--output', str(output), input_text='go\ngo\ngo\n')
            self.assertEqual(completed.returncode, 0, completed.stderr)
            result = json.loads(output.read_text(encoding='utf-8'))['result']
            self.assertEqual(result['end_reason'], 'beacon')
            self.assertEqual(result['metrics']['ticks'], 6)

    def test_receipt_refuses_overwrite(self):
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / 'existing.json'
            output.write_text('preserve this', encoding='utf-8')
            completed = invoke('--demo', '--output', str(output))
            self.assertEqual(completed.returncode, 2)
            self.assertEqual(output.read_text(encoding='utf-8'), 'preserve this')

    def test_budget_and_output_options_reject_invalid_values(self):
        for arguments in (('--ticks', '0'), ('--ticks', '257'), ('--ticks', 'nan'), ('--json',), ('--mode', 'omniscient')):
            with self.subTest(arguments=arguments):
                completed = invoke(*arguments)
                self.assertEqual(completed.returncode, 2)

    def test_end_of_input_stops_without_traceback(self):
        completed = invoke()
        self.assertEqual(completed.returncode, 0, completed.stderr)
        self.assertIn('user_quit', completed.stdout)
        self.assertNotIn('Traceback', completed.stderr)

    def test_unknown_human_command_does_not_move_world(self):
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / 'unknown.json'
            completed = invoke('--output', str(output), input_text='east\nquit\n')
            self.assertEqual(completed.returncode, 0, completed.stderr)
            result = json.loads(output.read_text(encoding='utf-8'))['result']
            self.assertEqual(result['metrics']['ticks'], 0)
            self.assertEqual(result['decisions'], [])


if __name__ == '__main__':
    unittest.main()
