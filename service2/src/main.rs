use actix_web::{App, HttpResponse, HttpServer, Responder, get};
use std::fs::OpenOptions;
use std::{env, fs, io::Write, path::Path};
use sysinfo::System;
use time::{OffsetDateTime, macros::format_description};

#[get("/status")]
async fn status() -> impl Responder {
    let storage = env::var("STORAGE_URL").unwrap_or_else(|_| "http://storage:7070".to_string());
    let vpath = env::var("VSTORAGE_PATH").unwrap_or_else(|_| "/data/vstorage/log.txt".to_string());

    let record = format!(
        "Timestamp2: {}: uptime {:.2} hours, free disk in root: {} MBytes",
        get_now_utc_iso(),
        get_uptime(),
        get_root_space()
    );

    if let Some(dir) = Path::new(&vpath).parent() {
        let _ = fs::create_dir_all(dir);
    }

    if let Ok(mut f) = OpenOptions::new().create(true).append(true).open(&vpath) {
        let _ = writeln!(f, "{}", record);
    }

    let client = awc::Client::default();
    let body = record.clone();

    let _ = client
        .post(format!("{}/log", storage))
        .insert_header(("Content-Type", "text/plain"))
        .send_body(body)
        .await;

    HttpResponse::Ok()
        .content_type("text/plain")
        .body(format!("{}\n", record))
}

#[actix_web::main]
async fn main() -> std::io::Result<()> {
    HttpServer::new(|| App::new().service(status))
        .bind(("0.0.0.0", 8080))?
        .run()
        .await
}

fn get_now_utc_iso() -> String {
    let format = format_description!("[year]-[month]-[day]T[hour]:[minute]:[second]Z");
    OffsetDateTime::now_utc().format(&format).unwrap()
}

fn get_uptime() -> f64 {
    (System::uptime() as f64) / 3600.0
}

fn get_root_space() -> u64 {
    let root = if cfg!(windows) { "C:\\" } else { "/" };
    fs2::available_space(root)
        .map(|b| b / (1024 * 1024))
        .unwrap_or(0)
}
