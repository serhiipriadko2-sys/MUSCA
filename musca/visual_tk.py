"""Tk presentation for the local MUSCA visual puzzle shell."""

from __future__ import annotations

from typing import Any

from .contracts import SensorMode
from .visual import VisualPuzzleController, VisualSnapshot

_BG = "#07131d"
_PANEL = "#0d1f2d"
_PANEL_2 = "#102b3b"
_TEXT = "#eaf7ff"
_MUTED = "#89a9bb"
_CYAN = "#35d7ff"
_AMBER = "#ffb13b"
_BLUE = "#3aa8ff"
_RED = "#ff5c63"
_GREEN = "#48e29d"
_BORDER = "#24485d"


def _require_tk():
    try:
        import tkinter as tk
    except ImportError as exc:  # pragma: no cover - platform capability guard
        raise RuntimeError("Tkinter is not available in this Python installation") from exc
    return tk


def _new_root(tk):
    try:
        return tk.Tk()
    except Exception as exc:  # platform/display capability boundary
        raise RuntimeError("Tk window could not be created on this host") from exc


class VisualPuzzleApp:
    """Tk visual shell. Hidden evaluator state never appears in widgets."""

    def __init__(self, root, controller: VisualPuzzleController) -> None:
        self.tk = _require_tk()
        self.root = root
        self.controller = controller
        self.root.title("ISKRA // MUSCA — visual gate prototype")
        self.root.geometry("1280x760")
        self.root.minsize(980, 640)
        self.root.configure(bg=_BG)
        self.root.protocol("WM_DELETE_WINDOW", self._close)
        self._build_shell()
        self.render()

    def _build_shell(self) -> None:
        tk = self.tk
        header = tk.Frame(self.root, bg=_BG, padx=26, pady=18)
        header.pack(fill="x")
        tk.Label(
            header, text="MUSCA // GATE", bg=_BG, fg=_TEXT,
            font=("Segoe UI", 24, "bold"),
        ).pack(side="left")
        self.stage_label = tk.Label(
            header, text="", bg=_BG, fg=_CYAN, font=("Segoe UI", 11, "bold"),
        )
        self.stage_label.pack(side="right", padx=(12, 0))
        self.mode_label = tk.Label(
            header, text="", bg=_PANEL_2, fg=_TEXT,
            font=("Segoe UI", 10, "bold"), padx=14, pady=7,
        )
        self.mode_label.pack(side="right")
        self.body = tk.Frame(self.root, bg=_BG, padx=24, pady=6)
        self.body.pack(fill="both", expand=True)

        footer = tk.Frame(self.root, bg="#061018", padx=26, pady=12)
        footer.pack(fill="x", side="bottom")
        tk.Label(
            footer,
            text="HUMAN chooses   •   ISKRA interprets   •   MUSCA moves",
            bg="#061018", fg=_MUTED, font=("Segoe UI", 10),
        ).pack(side="left")
        tk.Label(
            footer, text="visual candidate • not GATE-P01 v0.1",
            bg="#061018", fg=_AMBER, font=("Segoe UI", 9, "bold"),
        ).pack(side="right")

    def _clear_body(self) -> None:
        for child in self.body.winfo_children():
            child.destroy()

    def _panel(self, parent, *, pad: int = 18):
        return self.tk.Frame(
            parent, bg=_PANEL, padx=pad, pady=pad,
            highlightbackground=_BORDER, highlightthickness=1,
        )

    def _title(self, parent, text: str, *, color: str = _TEXT, size: int = 18) -> None:
        self.tk.Label(
            parent, text=text, bg=parent.cget("bg"), fg=color,
            font=("Segoe UI", size, "bold"), anchor="w",
        ).pack(fill="x", pady=(0, 12))

    def _button(self, parent, text: str, command, *, color: str = _CYAN, state: str = "normal"):
        return self.tk.Button(
            parent, text=text, command=command, state=state,
            bg=color if state == "normal" else "#29404f", fg="#041017",
            activebackground=_TEXT, activeforeground="#041017",
            disabledforeground="#7e95a3", relief="flat", bd=0,
            font=("Segoe UI", 11, "bold"), padx=18, pady=10,
            cursor="hand2" if state == "normal" else "arrow",
        )

    def render(self) -> None:
        snap = self.controller.snapshot()
        self.mode_label.configure(text=f"MODE: {snap.mode}")
        self.stage_label.configure(text=snap.stage.upper())
        self._clear_body()
        if snap.stage == "navigation":
            self._render_navigation(snap)
        elif snap.stage == "gate":
            self._render_gate(snap)
        else:
            self._render_result(snap)

    def _render_navigation(self, snap: VisualSnapshot) -> None:
        tk = self.tk
        grid = tk.Frame(self.body, bg=_BG)
        grid.pack(fill="both", expand=True)
        grid.columnconfigure(0, weight=3)
        grid.columnconfigure(1, weight=2)
        grid.rowconfigure(0, weight=1)

        left = self._panel(grid)
        left.grid(row=0, column=0, sticky="nsew", padx=(0, 10))
        right = self._panel(grid)
        right.grid(row=0, column=1, sticky="nsew", padx=(10, 0))
        self._title(left, "1 · Подход к источнику", color=_CYAN)
        tk.Label(
            left,
            text="MUSCA движется к более сильному сигналу.\nВы задаёте только смысл: идти, ждать или остановить эпизод.",
            bg=_PANEL, fg=_MUTED, justify="left", anchor="w",
            font=("Segoe UI", 11),
        ).pack(fill="x", pady=(0, 18))

        self._draw_signal_path(left, snap.signal_history)
        self._title(right, "Наблюдения", size=15)
        self._interpretation_box(right, snap.interpretation)
        actions = tk.Frame(right, bg=_PANEL)
        actions.pack(fill="x", side="bottom", pady=(18, 0))
        self._button(actions, "GO · исследовать", lambda: self._nav("go"), color=_CYAN).pack(fill="x", pady=4)
        self._button(actions, "WAIT · наблюдать", lambda: self._nav("wait"), color="#8db7c8").pack(fill="x", pady=4)
        self._button(actions, "QUIT", self._close, color="#607787").pack(fill="x", pady=4)

    def _draw_signal_path(self, parent, values: tuple[int, ...]) -> None:
        tk = self.tk
        canvas = tk.Canvas(parent, bg="#081822", height=270, bd=0, highlightthickness=0)
        canvas.pack(fill="both", expand=True)
        canvas.update_idletasks()
        width = max(canvas.winfo_width(), 600)
        count = max(len(values), 5)
        margin = 55
        usable = max(width - margin * 2, 200)
        y = 130
        canvas.create_line(margin, y, width - margin, y, fill="#264a5e", width=4)
        for index in range(count):
            x = margin + usable * index / max(count - 1, 1)
            value = values[index] if index < len(values) else None
            radius = 17 if value is None else min(34, 12 + value * 2)
            fill = "#183746" if value is None else _AMBER
            outline = "#2f5d70" if value is None else "#ffe7a4"
            canvas.create_oval(x-radius, y-radius, x+radius, y+radius, fill=fill, outline=outline, width=2)
            label = "—" if value is None else str(value)
            canvas.create_text(x, y-62, text=label, fill=_TEXT, font=("Segoe UI", 13, "bold"))
        canvas.create_text(margin, 225, anchor="w", text="СТАРТ", fill=_MUTED, font=("Segoe UI", 10, "bold"))
        canvas.create_text(width-margin, 225, anchor="e", text="ИСТОЧНИК", fill=_CYAN, font=("Segoe UI", 10, "bold"))

    def _interpretation_box(self, parent, statements: tuple[str, ...]) -> None:
        tk = self.tk
        box = tk.Frame(parent, bg="#081822", padx=14, pady=14)
        box.pack(fill="both", expand=True)
        for statement in statements:
            color = _TEXT
            if statement.startswith("[HYP]"):
                color = _AMBER
            elif statement.startswith("[UNKNOWN]"):
                color = _MUTED
            elif statement.startswith("[INTERP]"):
                color = _CYAN
            tk.Label(box, text=statement, bg="#081822", fg=color, justify="left", anchor="w",
                     wraplength=390, font=("Segoe UI", 10)).pack(fill="x", pady=4)

    def _render_gate(self, snap: VisualSnapshot) -> None:
        tk = self.tk
        grid = tk.Frame(self.body, bg=_BG)
        grid.pack(fill="both", expand=True)
        grid.columnconfigure(0, weight=3)
        grid.columnconfigure(1, weight=2)
        grid.rowconfigure(0, weight=1)

        left = self._panel(grid)
        left.grid(row=0, column=0, sticky="nsew", padx=(0, 10))
        right = self._panel(grid)
        right.grid(row=0, column=1, sticky="nsew", padx=(10, 0))
        self._title(left, "2 · Шлюз", color=_CYAN)
        tk.Label(
            left,
            text="Нужно выбрать нейтральный реагент. Свет показывает заряд, но НЕ состав.",
            bg=_PANEL, fg=_TEXT, justify="left", anchor="w",
            font=("Segoe UI", 11, "bold"),
        ).pack(fill="x", pady=(0, 16))

        cards = tk.Frame(left, bg=_PANEL)
        cards.pack(fill="both", expand=True)
        cards.columnconfigure(0, weight=1)
        cards.columnconfigure(1, weight=1)
        self._reagent_card(cards, "ЯНТАРНЫЙ", "amber", 9, _AMBER, lambda: self._gate("amber")).grid(
            row=0, column=0, sticky="nsew", padx=(0, 8)
        )
        self._reagent_card(cards, "КОБАЛЬТОВЫЙ", "cobalt", 4, _BLUE, lambda: self._gate("cobalt")).grid(
            row=0, column=1, sticky="nsew", padx=(8, 0)
        )

        resource = tk.Frame(left, bg="#081822", padx=14, pady=12)
        resource.pack(fill="x", pady=(16, 0))
        tk.Label(resource, text="ЗАПАС", bg="#081822", fg=_MUTED,
                 font=("Segoe UI", 9, "bold")).pack(side="left")
        for index in range(2):
            active = snap.cells_remaining is not None and index < snap.cells_remaining
            tk.Label(resource, text="■", bg="#081822", fg=_CYAN if active else "#314957",
                     font=("Segoe UI", 18, "bold")).pack(side="left", padx=4)
        tk.Label(resource, text=f"{snap.cells_remaining}/2", bg="#081822", fg=_TEXT,
                 font=("Segoe UI", 11, "bold")).pack(side="right")
        self._title(right, "Что известно", size=15)
        self._interpretation_box(right, snap.interpretation)
        info = tk.Label(
            right,
            text="SCAN стоит 1 ячейку и доступен только в light_chemical.\nРеактивный реагент блокирует шлюз и обнуляет запас.",
            bg=_PANEL, fg=_MUTED, justify="left", anchor="w",
            wraplength=400, font=("Segoe UI", 10),
        )
        info.pack(fill="x", pady=(12, 8))
        scan_state = "normal" if snap.scan_available else "disabled"
        self._button(right, "SCAN · химическая проба", lambda: self._gate("scan"),
                     color=_CYAN, state=scan_state).pack(fill="x", pady=4)
        self._button(right, "LEAVE · отказаться", lambda: self._gate("leave"),
                     color="#8db7c8").pack(fill="x", pady=4)
        self._button(right, "QUIT", self._close, color="#607787").pack(fill="x", pady=4)

    def _reagent_card(self, parent, title: str, command: str, charge: int, color: str, action):
        tk = self.tk
        card = tk.Frame(parent, bg="#081822", padx=18, pady=18,
                        highlightbackground=color, highlightthickness=1)
        tk.Label(card, text=title, bg="#081822", fg=color,
                 font=("Segoe UI", 14, "bold")).pack()
        tk.Label(card, text=command, bg="#081822", fg=_MUTED,
                 font=("Consolas", 10)).pack(pady=(2, 14))
        vial = tk.Canvas(card, width=110, height=180, bg="#081822", bd=0, highlightthickness=0)
        vial.pack(pady=4)
        vial.create_rectangle(38, 18, 72, 42, fill="#1b2e38", outline=_TEXT, width=2)
        vial.create_rectangle(28, 42, 82, 148, fill="#0f202a", outline=color, width=3)
        vial.create_rectangle(34, 70, 76, 140, fill=color, outline=color)
        vial.create_text(55, 106, text=str(charge), fill="#051018", font=("Segoe UI", 24, "bold"))
        tk.Label(card, text=f"Световой заряд: {charge}", bg="#081822", fg=_TEXT,
                 font=("Segoe UI", 10)).pack(pady=(4, 12))
        self._button(card, f"ВЫБРАТЬ {command.upper()}", action, color=color).pack(fill="x")
        return card

    def _render_result(self, snap: VisualSnapshot) -> None:
        tk = self.tk
        panel = self._panel(self.body, pad=28)
        panel.pack(fill="both", expand=True)
        navigation, gate = self.controller.results()
        reason = gate.get("end_reason", navigation.get("end_reason", "unknown"))
        titles = {
            "opened": ("ШЛЮЗ ОТКРЫТ", _GREEN, "Нейтральный реагент подтвердился."),
            "sealed": ("ШЛЮЗ ЗАБЛОКИРОВАН", _RED, "Реактивный реагент закрыл проход."),
            "left": ("РЕШЕНИЕ ОТЛОЖЕНО", _AMBER, "Вы сохранили ресурс и отказались от открытия."),
            "user_quit": ("ЭПИЗОД ОСТАНОВЛЕН", _MUTED, "Решение не было подменено автоматикой."),
            "tick_limit": ("ЛИМИТ ТИКОВ", _AMBER, "Источник не достигнут в заданном бюджете."),
        }
        title, color, subtitle = titles.get(reason, ("ЭПИЗОД ЗАВЕРШЁН", _TEXT, reason))
        tk.Label(panel, text=title, bg=_PANEL, fg=color,
                 font=("Segoe UI", 30, "bold")).pack(pady=(60, 14))
        tk.Label(panel, text=subtitle, bg=_PANEL, fg=_TEXT,
                 font=("Segoe UI", 14)).pack(pady=(0, 22))
        cells = gate.get("cells_remaining")
        if cells is not None:
            tk.Label(panel, text=f"Итоговый запас: {cells}/2", bg=_PANEL, fg=_CYAN,
                     font=("Segoe UI", 18, "bold")).pack(pady=8)
        tk.Label(
            panel,
            text="Это инженерный visual prototype. Результат не является научным выводом\nи не входит в frozen GATE-P01 v0.1 без отдельной preregistration.",
            bg=_PANEL, fg=_MUTED, justify="center", font=("Segoe UI", 10),
        ).pack(pady=20)
        self._button(panel, "ЗАКРЫТЬ", self.root.destroy, color=_CYAN).pack(pady=18)

    def _nav(self, command: str) -> None:
        self.controller.navigation_choice(command)
        self.render()

    def _gate(self, command: str) -> None:
        self.controller.gate_choice(command)
        self.render()

    def _close(self) -> None:
        self.controller.quit()
        self.root.destroy()


def run_visual_puzzle(mode: SensorMode, layout: str, *, max_ticks: int = 16) -> tuple[dict[str, Any], dict[str, Any]]:
    """Open the Tk shell and return evaluator receipts after the window closes."""
    tk = _require_tk()
    controller = VisualPuzzleController(mode, layout, max_ticks=max_ticks)
    root = _new_root(tk)
    VisualPuzzleApp(root, controller)
    root.mainloop()
    return controller.results()


def visual_smoke() -> dict[str, object]:
    """Construct and destroy the window without entering mainloop."""
    tk = _require_tk()
    controller = VisualPuzzleController(SensorMode.LIGHT_CHEMICAL, "a")
    root = _new_root(tk)
    root.withdraw()
    VisualPuzzleApp(root, controller)
    root.update_idletasks()
    size = (root.winfo_width(), root.winfo_height())
    root.destroy()
    return {"tk_version": tk.TkVersion, "stage": controller.snapshot().stage, "window_size": size}
