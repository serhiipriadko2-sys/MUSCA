"""Run the local terminal prototype: ``py -3.14 -m musca --demo``."""

import argparse
from contextlib import ExitStack
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import platform
import subprocess
import sys

from .backends import ScriptedBackend
from .contracts import ContractError, Goal, MAX_INTENT_TICKS, SensorMode
from .simulation import Simulation
from .puzzle import GateSession
from .world import GridWorld


def _tick_budget(value: str) -> int:
    try:
        parsed = int(value)
    except ValueError as exc:
        raise argparse.ArgumentTypeError("ticks must be an integer from 1 to 256") from exc
    if not 1 <= parsed <= 256:
        raise argparse.ArgumentTypeError("ticks must be an integer from 1 to 256")
    return parsed


def _provenance() -> dict:
    root = Path(__file__).resolve().parent.parent

    def git(*args: str) -> str | None:
        try:
            completed = subprocess.run(
                ['git', *args], cwd=root, capture_output=True, text=True,
                encoding='utf-8', timeout=5, check=False,
            )
        except (OSError, subprocess.TimeoutExpired):
            return None
        return completed.stdout.strip() if completed.returncode == 0 else None

    state = git('status', '--porcelain')
    return {
        'created_at': datetime.now(timezone.utc).isoformat(),
        'python': platform.python_version(),
        'platform': platform.system(),
        'base_commit': git('rev-parse', 'HEAD'),
        'worktree_dirty': None if state is None else bool(state),
        'source_sha256': {
            path.relative_to(root).as_posix(): hashlib.sha256(path.read_bytes()).hexdigest()
            for path in sorted((root / 'musca').glob('*.py'))
        },
    }


def _show(frame: dict) -> None:
    print(f"\nТик {frame['tick']}")
    for statement in frame['interpretation']:
        print(statement)


def _interactive(simulation: Simulation, max_ticks: int) -> dict:
    print("ISKRA // MUSCA: технический сценарий, без LLM и биологической модели.")
    print("go — исследовать; wait — остановиться и наблюдать; quit — завершить.")
    print("Режим восприятия фиксирован на эпизод. Направления выбирает нижний контроллер.")
    _show(simulation.trace[0])
    while simulation.world.tick < max_ticks and not simulation.finished:
        try:
            choice = input("\nРешение [go/wait/quit]: ").strip().lower()
        except (EOFError, KeyboardInterrupt):
            print("\nВвод завершён; эпизод остановлен.")
            return simulation.result('user_quit')
        if choice == 'quit':
            return simulation.result('user_quit')
        if choice not in {'go', 'wait'}:
            print("Выберите go, wait или quit. Мир не продвинулся.")
            continue
        try:
            if choice == 'wait':
                simulation.submit_goal(Goal.HOLD, ttl=4)
            elif not simulation.bridge.modulation(simulation.world.tick).approach_signal:
                simulation.submit_goal(Goal.SEEK_SIGNAL, ttl=MAX_INTENT_TICKS)
            # Continuing a still-valid intention does not submit a replacement motor hint.
        except ContractError as exc:
            print(f"Намерение отклонено: {exc}. Движение остановлено; можно выбрать wait.")
            continue
        previous_kinds = {event['kind'] for event in simulation.trace[-1]['events']}
        for _ in range(min(4, max_ticks - simulation.world.tick)):
            frame = simulation.step()
            _show(frame)
            kinds = {event['kind'] for event in frame['events']}
            newly_warned = 'chemical_warning' in kinds and 'chemical_warning' not in previous_kinds
            if simulation.finished or newly_warned:
                break
            previous_kinds = kinds
    return simulation.result('tick_limit')


def _gate_interactive(gate: GateSession) -> dict:
    print("\nШЛЮЗ. Нужно открыть проход нейтральным реагентом.")
    print("Есть янтарный (amber) и кобальтовый (cobalt) реагенты. Свет показывает заряд, не состав.")
    print("Запас: 2 ячейки. Химическая проба scan стоит 1 ячейку; доступна только химическому режиму.")
    print("Нейтральный откроет шлюз с остатком запаса. Реактивный заблокирует шлюз и обнулит запас.")
    print("leave — отказаться и сохранить остаток; quit — завершить ввод.")
    while not gate.finished:
        view = gate.view()
        print(f"\nОсталось ячеек: {view['cells_remaining']}.")
        for statement in view['interpretation']:
            print(statement)
        try:
            command = input("\nВыбор [scan/amber/cobalt/leave/quit]: ").strip().lower()
        except (EOFError, KeyboardInterrupt):
            print("\nВвод завершён; решение не подменяется автоматическим выбором.")
            command = 'quit'
        if not gate.choose(command):
            print("Действие недоступно или неизвестно. Запас и наблюдения не изменились.")
    result = gate.result()
    outcomes = {
        'opened': 'Шлюз открыт. Экспедиция завершена.',
        'sealed': 'Реактивный реагент заблокировал шлюз. Экспедиция не прошла.',
        'left': 'Вы отказались от открытия и сохранили остаток запаса.',
        'user_quit': 'Эпизод остановлен пользователем.',
    }
    print(outcomes[result['end_reason']])
    print(f"Итоговый запас: {result['cells_remaining']} яч.")
    return result


def main(argv: list[str] | None = None) -> int:
    if hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8')
    parser = argparse.ArgumentParser(description="Local synthetic MUSCA prototype; no research claim.")
    parser.add_argument('--mode', choices=[mode.value for mode in SensorMode], default='light')
    parser.add_argument('--demo', action='store_true', help='use a predefined semantic seek_signal intention')
    parser.add_argument('--puzzle', action='store_true', help='play the terminal gate puzzle after a safe approach')
    parser.add_argument('--layout', choices=['a', 'b'], help='gate fixture variant, assigned by the facilitator')
    parser.add_argument('--json', action='store_true', help='print a machine-readable demo receipt')
    parser.add_argument('--ticks', type=_tick_budget, default=16, help='total episode budget, 1..256')
    parser.add_argument('--output', type=Path, help='save receipt to a new file; never overwrite')
    args = parser.parse_args(argv)
    if args.json and not args.demo:
        parser.error('--json requires --demo')
    if args.puzzle and args.demo:
        parser.error('--puzzle requires human input; it cannot be combined with --demo')
    if args.layout and not args.puzzle:
        parser.error('--layout requires --puzzle')
    with ExitStack() as resources:
        output = None
        if args.output is not None:
            try:
                args.output.parent.mkdir(parents=True, exist_ok=True)
                output = resources.enter_context(args.output.open('x', encoding='utf-8', newline='\n'))
            except OSError as exc:
                print(f"Cannot reserve a new receipt file: {exc}", file=sys.stderr)
                return 2
        return _run_episode(args, output)


def _run_episode(args: argparse.Namespace, output) -> int:
    world = GridWorld(hazards=frozenset()) if args.puzzle else GridWorld()
    simulation = Simulation(world=world, backend=ScriptedBackend(), sensor_mode=SensorMode(args.mode))
    try:
        if args.demo:
            simulation.submit_goal(Goal.SEEK_SIGNAL, ttl=min(args.ticks, MAX_INTENT_TICKS))
            result = simulation.run(max_ticks=args.ticks)
        else:
            result = _interactive(simulation, args.ticks)
    except ContractError:
        result = simulation.result('backend_contract_error')
    receipt = {
        'receipt_version': 1,
        'kind': 'engineering_demo',
        'fixture_id': 'detour-v1',
        'config': {'max_ticks': args.ticks, 'input_mode': 'predefined_intent' if args.demo else 'human'},
        'result': result,
        'provenance': _provenance(),
        'limits': 'Synthetic fixture. Not a connectome experiment or a player-interest result.',
    }
    if args.puzzle:
        gate_result = {'end_reason': 'not_reached'}
        if result['end_reason'] == 'beacon':
            gate_result = _gate_interactive(GateSession(SensorMode(args.mode), args.layout or 'a'))
        receipt.update({
            'kind': 'game_puzzle', 'fixture_id': 'gate-v1',
            'navigation': result, 'result': gate_result,
        })
        receipt['config']['layout'] = args.layout or 'a'
    encoded = json.dumps(receipt, ensure_ascii=False, indent=2, sort_keys=True) + '\n'
    if output is not None:
        try:
            output.write(encoded)
            output.flush()
        except OSError as exc:
            print(f"Cannot finish receipt; retain the partial file for diagnosis: {exc}", file=sys.stderr)
            return 2
    if args.json:
        print(encoded, end='')
    else:
        if args.demo:
            print("Техническая демонстрация с заранее заданным намерением; не научный опыт.")
            for frame in result['trace']:
                _show(frame)
        print(f"\nЗавершение навигации: {result['end_reason']}; шагов: {result['metrics']['ticks']}; "
              f"столкновений: {result['metrics']['collisions']}.")
        if args.puzzle:
            print(f"Итог шлюза: {receipt['result']['end_reason']}.")
        if args.output:
            print(f"Квитанция: {args.output}")
    return 1 if result['end_reason'] == 'backend_contract_error' else 0


if __name__ == '__main__':
    raise SystemExit(main())
