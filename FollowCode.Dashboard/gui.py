import tkinter as tk
from tkinter import ttk
from tkinter import font as tkfont
import json


class ObjectDashboard:
    def __init__(self, root, data_queue, port):
        self.root = root
        self.data_queue = data_queue
        self.root.title(f"FollowCode Dashboard - Port {port}")
        self.root.geometry("960x540")

        # Header
        header = tk.Label(
            root, text="Tracked Objects", font=("Consolas", 14, "bold"), anchor=tk.W
        )
        header.pack(fill=tk.X, padx=10, pady=(10, 0))

        # Text widget for multi-line display
        frame = tk.Frame(root)
        frame.pack(fill=tk.BOTH, expand=True, padx=10, pady=5)

        self.text = tk.Text(
            frame,
            font=("Consolas", 10),
            wrap=tk.WORD,
            state=tk.DISABLED,
            bg="#1e1e1e",
            fg="#d4d4d4",
            insertbackground="white",
            relief=tk.FLAT,
            borderwidth=0,
        )
        self.text.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)

        # Scrollbar
        scrollbar = ttk.Scrollbar(frame, orient=tk.VERTICAL, command=self.text.yview)
        self.text.configure(yscrollcommand=scrollbar.set)
        scrollbar.pack(side=tk.RIGHT, fill=tk.Y)

        # Tag for header style (bold, accent color)
        self.text.tag_configure("header", foreground="#569cd6", font=("Consolas", 10, "bold"))
        self.text.tag_configure("separator", foreground="#3a3a3a")
        self.text.tag_configure("empty", foreground="#808080")

        # Status bar
        self.status = tk.Label(
            root,
            text="Waiting for SDK connection...",
            font=("Consolas", 9),
            anchor=tk.W,
            fg="gray",
        )
        self.status.pack(fill=tk.X, padx=10, pady=(0, 10))

        self._last_keys = set()
        self._poll_queue()

    def _poll_queue(self):
        try:
            while not self.data_queue.empty():
                objects = self.data_queue.get_nowait()
                self._render(objects)
        except Exception:
            pass
        self.root.after(200, self._poll_queue)

    def _render(self, objects):
        current_keys = set()

        # Enable writing
        self.text.configure(state=tk.NORMAL)
        self.text.delete("1.0", tk.END)

        if not objects:
            self.text.insert(tk.END, "(no objects tracked)", "empty")
            self.status.config(text="Cleared - no objects tracked", fg="gray")
            self.text.configure(state=tk.DISABLED)
            return

        for i, obj in enumerate(objects):
            key = obj.get("key", "?")
            data_value = obj.get("data", "")
            updated = obj.get("updatedAt", "")
            current_keys.add(key)

            # Format timestamp
            try:
                dt = updated.replace("T", " ").replace("Z", "")
                if "." in dt:
                    dt = dt[: dt.index(".")]
            except Exception:
                dt = updated

            # Handle both string and dict data
            if isinstance(data_value, str):
                data_str = data_value
            else:
                data_str = json.dumps(data_value, ensure_ascii=False, indent=2)

            # Write header
            self.text.insert(tk.END, f"{key} @ {dt}\n", "header")
            # Write data content
            self.text.insert(tk.END, f"{data_str}\n")
            # Write separator between objects
            if i < len(objects) - 1:
                self.text.insert(tk.END, "─" * 80 + "\n", "separator")

        self.text.configure(state=tk.DISABLED)
        self.text.see("1.0")

        count = len(objects)
        self.status.config(
            text=f"Last update: {count} object(s)", fg="green"
        )
