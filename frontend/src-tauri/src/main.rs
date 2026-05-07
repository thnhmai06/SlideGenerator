#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use serde_json::Value;

#[tauri::command]
async fn backend_request(method: String, _params: Option<Value>) -> Result<Value, String> {
    Err(format!(
        "backend_request is not wired yet in Tauri runtime (method: {method})"
    ))
}

#[tauri::command]
async fn restart_backend() -> bool {
    false
}

#[tauri::command]
fn is_portable() -> bool {
    std::env::var("PORTABLE_EXECUTABLE_DIR").is_ok()
}

#[tauri::command]
fn set_tray_locale(_locale: String) {}

#[tauri::command]
fn log_renderer(level: String, message: String, source: Option<String>) {
    if let Some(src) = source {
        println!("[{level}] [{src}] {message}");
    } else {
        println!("[{level}] {message}");
    }
}

fn main() {
    tauri::Builder::default()
        .plugin(tauri_plugin_dialog::init())
        .plugin(tauri_plugin_fs::init())
        .plugin(tauri_plugin_log::Builder::default().build())
        .plugin(tauri_plugin_opener::init())
        .plugin(tauri_plugin_os::init())
        .plugin(tauri_plugin_process::init())
        .plugin(tauri_plugin_shell::init())
        .plugin(tauri_plugin_updater::Builder::new().build())
        .invoke_handler(tauri::generate_handler![
            backend_request,
            restart_backend,
            is_portable,
            set_tray_locale,
            log_renderer
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
