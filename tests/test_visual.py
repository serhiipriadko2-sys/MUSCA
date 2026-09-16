"""Pure state-machine tests for the visual shell; no GUI window is opened here."""

import unittest

from musca.contracts import SensorMode
from musca.visual import VisualPuzzleController


class VisualControllerTests(unittest.TestCase):
    def test_initial_snapshot_contains_only_player_safe_state(self):
        controller = VisualPuzzleController(SensorMode.LIGHT, "a")
        snapshot = controller.snapshot()
        self.assertEqual(snapshot.stage, "navigation")
        self.assertEqual(snapshot.signal, 6)
        self.assertEqual(snapshot.signal_history, (6,))
        self.assertFalse(hasattr(snapshot, "layout"))
        self.assertNotIn("layout", repr(snapshot).lower())

    def test_go_reaches_gate_without_exposing_layout(self):
        a = VisualPuzzleController(SensorMode.LIGHT, "a")
        b = VisualPuzzleController(SensorMode.LIGHT, "b")
        self.assertTrue(a.navigation_choice("go"))
        self.assertTrue(b.navigation_choice("go"))
        self.assertEqual(a.snapshot(), b.snapshot())
        self.assertEqual(a.snapshot().stage, "gate")
        self.assertEqual(a.snapshot().signal_history, (6, 7, 8, 9, 10))

    def test_light_mode_rejects_scan_without_changing_state(self):
        controller = VisualPuzzleController(SensorMode.LIGHT, "a")
        controller.navigation_choice("go")
        before = controller.snapshot()
        self.assertFalse(controller.gate_choice("scan"))
        after = controller.snapshot()
        self.assertEqual(after.cells_remaining, before.cells_remaining)
        self.assertEqual(after.stage, "gate")
        self.assertIn("недоступно", " ".join(after.interpretation))

    def test_wrong_light_choice_seals_gate(self):
        controller = VisualPuzzleController(SensorMode.LIGHT, "a")
        controller.navigation_choice("go")
        self.assertTrue(controller.gate_choice("amber"))
        self.assertEqual(controller.snapshot().stage, "result")
        navigation, gate = controller.results()
        self.assertEqual(navigation["end_reason"], "beacon")
        self.assertEqual(gate["end_reason"], "sealed")
        self.assertEqual(gate["cells_remaining"], 0)

    def test_chemical_scan_costs_one_and_supports_correct_choice(self):
        controller = VisualPuzzleController(SensorMode.LIGHT_CHEMICAL, "a")
        controller.navigation_choice("go")
        self.assertTrue(controller.snapshot().scan_available)
        self.assertTrue(controller.gate_choice("scan"))
        snapshot = controller.snapshot()
        self.assertEqual(snapshot.cells_remaining, 1)
        self.assertFalse(snapshot.scan_available)
        self.assertIn("опровергнута", " ".join(snapshot.interpretation))
        self.assertTrue(controller.gate_choice("cobalt"))
        _, gate = controller.results()
        self.assertEqual(gate["end_reason"], "opened")
        self.assertEqual(gate["cells_remaining"], 1)

    def test_wait_advances_time_without_movement_then_go_can_resume(self):
        controller = VisualPuzzleController(SensorMode.LIGHT, "a")
        self.assertTrue(controller.navigation_choice("wait"))
        self.assertEqual(controller.snapshot().tick, 4)
        self.assertEqual(controller.simulation.result("tick_limit")["metrics"]["moves"], 0)
        self.assertTrue(controller.navigation_choice("go"))
        self.assertEqual(controller.snapshot().stage, "gate")

    def test_quit_before_gate_never_creates_gate_success(self):
        controller = VisualPuzzleController(SensorMode.LIGHT_CHEMICAL, "b")
        controller.quit()
        navigation, gate = controller.results()
        self.assertEqual(controller.snapshot().stage, "result")
        self.assertEqual(navigation["end_reason"], "user_quit")
        self.assertEqual(gate["end_reason"], "not_reached")

    def test_invalid_navigation_choice_is_non_mutating(self):
        controller = VisualPuzzleController(SensorMode.LIGHT, "a")
        before = controller.snapshot()
        self.assertFalse(controller.navigation_choice("east"))
        after = controller.snapshot()
        self.assertEqual(after.tick, before.tick)
        self.assertEqual(after.signal_history, before.signal_history)
        self.assertIn("Неизвестное действие", " ".join(after.interpretation))


if __name__ == "__main__":
    unittest.main()
