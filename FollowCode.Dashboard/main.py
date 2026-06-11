import sys
import threading
from server import app, run_server, data_queue
from gui import ObjectDashboard
import tkinter as tk

DEFAULT_HOST = "0.0.0.0"
DEFAULT_PORT = 42102


def main():
    host = DEFAULT_HOST
    port = DEFAULT_PORT

    if len(sys.argv) > 1:
        try:
            port = int(sys.argv[1])
        except ValueError:
            print(f"Invalid port: {sys.argv[1]}, using default {DEFAULT_PORT}")

    # Start Flask in background thread
    server_thread = threading.Thread(
        target=run_server, args=(host, port), daemon=True
    )
    server_thread.start()
    print(f"[Dashboard] Flask listening on http://{host}:{port}")
    print(f"[Dashboard] SDK target: POST http://localhost:{port}/api/objects")

    # Start Tkinter GUI (must run on main thread)
    root = tk.Tk()
    ObjectDashboard(root, data_queue, port)
    root.mainloop()


if __name__ == "__main__":
    main()
