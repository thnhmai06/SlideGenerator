from flask import Flask, request, jsonify
from flask_cors import CORS

from .services.download_manager import download_manager
from .services.data_manager import data_manager
from .config import config

app = Flask(__name__)
CORS(app)  # Enable CORS so frontend can call API

# ============== DOWNLOAD API ==============


@app.route("/api/download/create", methods=["POST"])
def create_download():
    """
    Create image download task

    Body:
        {
            "url": "https://...",
            "save_dir": "downloads" (optional)
        }
    """
    try:
        data: dict = request.get_json()
        url = data.get("url")
        save_dir = data.get("save_dir")  # None = use config default

        if not url:
            return jsonify({"error": "URL is required"}), 400

        task_id = download_manager.create_task(url, save_dir)
        return jsonify({"success": True, "task_id": task_id}), 201

    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/download/status/<task_id>", methods=["GET"])
def get_download_status(task_id):
    """Get download task status"""
    try:
        status = download_manager.get_status(task_id)

        if status is None:
            return jsonify({"error": "Task does not exist"}), 404

        return jsonify(status), 200

    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/download/list", methods=["GET"])
def list_downloads():
    """Get list of all download tasks"""
    try:
        tasks = download_manager.get_all_tasks()
        return jsonify({"tasks": tasks}), 200

    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/download/cancel/<task_id>", methods=["DELETE"])
def cancel_download(task_id):
    """Cancel download task"""
    try:
        success = download_manager.cancel_task(task_id)

        if not success:
            return jsonify({"error": "Task does not exist"}), 404

        return jsonify({"success": True}), 200

    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/download/pause/<task_id>", methods=["POST"])
def pause_download(task_id):
    """Pause download task"""
    try:
        success = download_manager.pause_task(task_id)

        if not success:
            return jsonify({"error": "Task does not exist or cannot be paused"}), 400

        return jsonify({"success": True}), 200

    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/download/resume/<task_id>", methods=["POST"])
def resume_download(task_id):
    """Resume paused download task"""
    try:
        success = download_manager.resume_task(task_id)

        if not success:
            return jsonify({"error": "Task does not exist or cannot be resumed"}), 400

        return jsonify({"success": True}), 200

    except Exception as e:
        return jsonify({"error": str(e)}), 500


# ============== DATA MANAGER API ==============


@app.route("/api/data/load", methods=["POST"])
def load_file():
    """
    Load CSV/Excel file into system

    Body:
        {
            "file_path": "/path/to/file.xlsx",
            "file_id": "my_file" (optional)
        }
    """
    try:
        data: dict = request.get_json()
        file_path = data.get("file_path")
        file_id = data.get("file_id")

        if not file_path:
            return jsonify({"error": "file_path is required"}), 400

        result = data_manager.load_file(file_path, file_id)

        return jsonify(result), 201

    except FileNotFoundError as e:
        return jsonify({"error": str(e)}), 404
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/data/unload/<file_id>", methods=["DELETE"])
def unload_file(file_id):
    """Unload file from system"""
    try:
        success = data_manager.unload_file(file_id)

        if not success:
            return jsonify({"error": "File does not exist"}), 404

        return jsonify({"success": True}), 200

    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/data/files", methods=["GET"])
def list_files():
    """Get list of all loaded files"""
    try:
        files = data_manager.get_loaded_files()
        return jsonify({"files": files}), 200

    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/data/<file_id>/sheets", methods=["GET"])
def get_sheets(file_id):
    """Get list of sheets in file"""
    try:
        sheets = data_manager.get_sheet_ids(file_id)

        if sheets is None:
            return jsonify({"error": "File does not exist"}), 404

        return jsonify({"sheets": sheets}), 200

    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/data/<file_id>/sheets/<sheet_id>/columns", methods=["GET"])
def get_columns(file_id, sheet_id):
    """Get list of columns in sheet"""
    try:
        columns = data_manager.get_columns(file_id, sheet_id)

        if columns is None:
            return jsonify({"error": "File or sheet does not exist"}), 404

        return jsonify({"columns": columns}), 200

    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/data/<file_id>/sheets/<sheet_id>/info", methods=["GET"])
def get_sheet_info(file_id, sheet_id):
    """Get detailed information of sheet"""
    try:
        info = data_manager.get_sheet_info(file_id, sheet_id)

        if info is None:
            return jsonify({"error": "File or sheet does not exist"}), 404

        return jsonify(info), 200

    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/data/<file_id>/sheets/<sheet_id>/data", methods=["GET"])
def get_sheet_data(file_id, sheet_id):
    """
    Get sheet data with pagination

    Query params:
        offset: Starting row index (default: 0)
        limit: Row limit (optional, default: all remaining rows)
    """
    try:
        offset = request.args.get("offset", default=0, type=int)
        limit = request.args.get("limit", type=int)

        data = data_manager.get_data(file_id, sheet_id, offset, limit)

        if data is None:
            return jsonify({"error": "File or sheet does not exist"}), 404

        return jsonify(data), 200

    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route(
    "/api/data/<file_id>/sheets/<sheet_id>/rows/<int:row_index>", methods=["GET"]
)
def get_sheet_row(file_id, sheet_id, row_index):
    """
    Get a specific row by index

    Path params:
        row_index: Row index (1-based, 1 = first data row after header)
    """
    try:
        row_data = data_manager.get_row(file_id, sheet_id, row_index)

        if row_data is None:
            return jsonify({"error": "File, sheet, or row does not exist"}), 404

        return jsonify({"row_index": row_index, "data": row_data}), 200

    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/download/queue", methods=["GET"])
def get_queue_info():
    """Get download queue information"""
    try:
        queue_info = download_manager.get_queue_info()
        return jsonify(queue_info), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


# ============== CONFIG API ==============


@app.route("/api/config", methods=["GET"])
def get_config():
    """Get all configuration values (all sections)"""
    try:
        return jsonify(config.get_all()), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/config/<section>", methods=["GET"])
def get_config_section(section):
    """Get specific configuration section"""
    try:
        section_data = config.get_section(section)
        if not section_data:
            return jsonify(
                {"error": f"Configuration section '{section}' not found"}
            ), 404
        return jsonify(section_data), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/config/<section>", methods=["PUT"])
def update_config_section(section):
    """Update configuration values in a section

    Body:
        {
            "key1": "value1",
            "key2": value2,
            ...
        }
    """
    try:
        data: dict = request.get_json()
        if not data:
            return jsonify({"error": "No configuration provided"}), 400

        config.update_section(section, data)
        return jsonify({"success": True, "config": config.get_section(section)}), 200
    except KeyError as e:
        return jsonify({"error": str(e)}), 400
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/config/<section>/<key>", methods=["GET"])
def get_config_value(section, key):
    """Get specific configuration value from a section"""
    try:
        value = config.get(section, key)
        if value is None:
            return jsonify({"error": f"Configuration '{section}.{key}' not found"}), 404
        return jsonify({key: value}), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/config/<section>/<key>", methods=["PUT"])
def set_config_value(section, key):
    """Set specific configuration value in a section

    Body:
        {"value": <new_value>}
    """
    try:
        data: dict = request.get_json()
        if "value" not in data:
            return jsonify({"error": "'value' field is required"}), 400

        config.set(section, key, data["value"])
        return jsonify({"success": True, key: config.get(section, key)}), 200
    except KeyError as e:
        return jsonify({"error": str(e)}), 400
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/config/reset", methods=["POST"])
def reset_config():
    """Reset configuration to default values"""
    try:
        config.reset_to_defaults()
        return jsonify({"success": True, "config": config.get_all()}), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/config/save", methods=["POST"])
def save_config():
    """Save current configuration to TOML file"""
    try:
        success = config.save_to_file()
        if success:
            return jsonify(
                {"success": True, "message": "Configuration saved to TOML file"}
            ), 200
        else:
            return jsonify({"error": "Failed to save configuration"}), 500
    except Exception as e:
        return jsonify({"error": str(e)}), 500


# ============== HEALTH CHECK ==============


@app.route("/api/health", methods=["GET"])
def health_check():
    """Health check endpoint"""
    return jsonify({"status": "ok", "message": "IPC API Server is running"}), 200


if __name__ == "__main__":
    # Run server using config values
    app.run(host=config.host, port=config.port, debug=config.debug)
