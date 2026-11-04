import logging
import os
from datetime import datetime

from flask import Flask, request, jsonify


STARTED_AT = datetime.now()
BASE_DIR = os.path.dirname(os.path.abspath(__file__))
LOG_DIR = os.path.join(BASE_DIR, "Log")
LOG_FILE_PATH = os.path.join(LOG_DIR, f"{STARTED_AT.strftime('%Y-%m-%d')}.log")


def configure_logging():
    os.makedirs(LOG_DIR, exist_ok=True)

    formatter = logging.Formatter(
        "%(asctime)s [%(levelname)s] %(name)s: %(message)s",
        datefmt="%Y-%m-%d %H:%M:%S",
    )

    file_handler = logging.FileHandler(LOG_FILE_PATH, encoding="utf-8")
    file_handler.setFormatter(formatter)

    stream_handler = logging.StreamHandler()
    stream_handler.setFormatter(formatter)

    root_logger = logging.getLogger()
    root_logger.setLevel(logging.INFO)
    root_logger.handlers.clear()
    root_logger.addHandler(stream_handler)
    root_logger.addHandler(file_handler)

    flask_logger = logging.getLogger("werkzeug")
    flask_logger.setLevel(logging.INFO)
    flask_logger.handlers.clear()
    flask_logger.addHandler(stream_handler)
    flask_logger.addHandler(file_handler)
    flask_logger.propagate = False


configure_logging()

app = Flask(__name__)

true_key = "xglluciception"



state = {
    "p1": False,
    "p2": False
}

@app.route("/")
def home():
    app.logger.info("Home endpoint requested.")
    return "Hello World!"

@app.route("/set_motion_detected", methods=["GET"])
def set_motion_detected():
    value = request.args.get("value", "").lower()
    participant = request.args.get("participant", "").lower()
    key = request.args.get("key", "").lower()

    app.logger.info("set_motion_detected called for participant=%s value=%s", participant, value)

    if key != true_key:
        app.logger.warning("set_motion_detected rejected due to invalid key for participant=%s", participant)
        return "invalid key"
    
    if participant == "p1":
        if value == "true":
            state["p1"] = True
            app.logger.info("Motion state updated: participant=p1 value=true")
            return "p1 value set to true"
        elif value == "false":
            state["p1"] = False
            app.logger.info("Motion state updated: participant=p1 value=false")
            return "p1 value set to false"

    if participant == "p2":
        if value == "true":
            state["p2"] = True
            app.logger.info("Motion state updated: participant=p2 value=true")
            return "p2 value set to true"
        elif value == "false":
            state["p2"] = False
            app.logger.info("Motion state updated: participant=p2 value=false")
            return "p2 value set to false"
    app.logger.warning("set_motion_detected received invalid request: participant=%s value=%s", participant, value)
    return "invalid request"

@app.route("/get_motion_detected", methods=["GET"])
def get_motion_detected():
    key = request.args.get("key", "").lower()
    participant = request.args.get("participant", "").lower()

    app.logger.info("get_motion_detected called for participant=%s", participant)

    if key != true_key:
        app.logger.warning("get_motion_detected rejected due to invalid key for participant=%s", participant)
        return "invalid key"

    if participant == "p1":
        app.logger.info("Returning motion state for participant=p1 value=%s", state["p1"])
        return jsonify({"value": state["p1"]})

    if participant == "p2":
        app.logger.info("Returning motion state for participant=p2 value=%s", state["p2"])
        return jsonify({"value": state["p2"]})

    app.logger.warning("get_motion_detected received invalid request: participant=%s", participant)
    return "invalid request"


if __name__ == "__main__":
    app.logger.info("Starting motion detection server on 0.0.0.0:8080. Logs: %s", LOG_FILE_PATH)
    app.run(host="0.0.0.0", port=8080)
