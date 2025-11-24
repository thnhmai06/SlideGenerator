from flask import Flask, request, jsonify
from flask_cors import CORS

from src.config import CONFIG

app = Flask(__name__)
CORS(app)


# ============== HEALTH CHECK ==============
@app.route("/api/health", methods=["GET"])
def health_check():
    """Health check endpoint"""
    return jsonify({"status": "ok", "message": "API Server is running"}), 200


if __name__ == "__main__":
    # Run server using CONFIG values
    app.run(host=CONFIG.server_host, port=CONFIG.server_port, debug=CONFIG.server_debug)
