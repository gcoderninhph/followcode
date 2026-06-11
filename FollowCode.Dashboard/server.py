from flask import Flask, request, jsonify
from queue import Queue

app = Flask(__name__)
data_queue = Queue()
latest_objects = []


@app.route("/api/objects", methods=["POST"])
def receive_objects():
    global latest_objects
    objects = request.get_json()
    if objects is not None:
        latest_objects = objects
        data_queue.put(objects)
        return jsonify({"status": "ok", "count": len(objects)})
    return jsonify({"status": "error", "message": "invalid json"}), 400


@app.route("/api/objects", methods=["GET"])
def get_objects():
    # For polling fallback
    return jsonify(latest_objects)


def run_server(host, port):
    app.run(host=host, port=port, debug=False, use_reloader=False)
