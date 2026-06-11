import tkinter as tk
from tkinter import ttk
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

        # Treeview table
        columns = ("Key", "Data", "Updated")
        self.tree = ttk.Treeview(root, columns=columns, show="headings", height=20)
        self.tree.heading("Key", text="Key")
        self.tree.heading("Data", text="Data")
        self.tree.heading("Updated", text="Updated")
        self.tree.column("Key", width=140, minwidth=100)
        self.tree.column("Data", width=520, minwidth=200)
        self.tree.column("Updated", width=260, minwidth=160)
        self.tree.pack(fill=tk.BOTH, expand=True, padx=10, pady=5)

        # Scrollbar
        scrollbar = ttk.Scrollbar(root, orient=tk.VERTICAL, command=self.tree.yview)
        self.tree.configure(yscrollcommand=scrollbar.set)
        scrollbar.pack(side=tk.RIGHT, fill=tk.Y)

        # Status bar
        self.status = tk.Label(
            root,
            text="Waiting for SDK connection...",
            font=("Consolas", 9),
            anchor=tk.W,
            fg="gray",
        )
        self.status.pack(fill=tk.X, padx=10, pady=(0, 10))

        # Start polling the queue
        self._object_map = {}
        self._poll_queue()

    def _poll_queue(self):
        try:
            while not self.data_queue.empty():
                objects = self.data_queue.get_nowait()
                self._update_table(objects)
        except Exception:
            pass
        self.root.after(200, self._poll_queue)

    def _update_table(self, objects):
        current_keys = set()

        for obj in objects:
            key = obj.get("key", "?")
            data_value = obj.get("data", "")

            # Handle both string (ToString) and dict (JSON) data
            if isinstance(data_value, str):
                data_str = data_value
            else:
                data_str = json.dumps(data_value, ensure_ascii=False)

            updated = obj.get("updatedAt", "")

            # Format timestamp for display
            try:
                dt = updated.replace("T", " ").replace("Z", "")
                if "." in dt:
                    dt = dt[: dt.index(".")]
            except Exception:
                dt = updated
            current_keys.add(key)

            if key in self._object_map:
                # Update existing row
                item_id = self._object_map[key]
                self.tree.item(item_id, values=(key, data_str, dt))
            else:
                # Insert new row
                item_id = self.tree.insert("", tk.END, values=(key, data_str, dt))
                self._object_map[key] = item_id

        # Remove objects that are no longer being tracked
        removed = set(self._object_map.keys()) - current_keys
        for key in removed:
            self.tree.delete(self._object_map[key])
            del self._object_map[key]

        count = len(objects)
        if count > 0:
            self.status.config(
                text=f"Last update: {count} object(s) received", fg="green"
            )
        else:
            self.status.config(text="Cleared - no objects tracked", fg="gray")
