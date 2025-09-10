from flask import Flask, request
import os, pathlib

log_path = os.environ.get("LOG_FILE", "vstorage/logs.txt")

pathlib.Path(os.path.dirname(log_path)).mkdir(exist_ok=True, parents=True)

app = Flask(__name__)

@app.get("/log")
def get_log():
    if not os.path.exists(log_path):
        return "", 200, {"Content-Type": "text/plain; charset=utf-8"}
    with open(log_path, "r", encoding="utf-8") as f:
        data = f.read()
    return data, 200, {"Content-Type": "text/plain; charset=utf-8"}

@app.post("/log")
def post_log():
    body = request.get_data(as_text=True).strip()

    with open(log_path, "a", encoding="utf-8") as f:
        f.write(body + "\n")
    return body, 200, {"Content-Type": "text/plain; charset=utf-8"}

if __name__ == "__main__":
    app.run(host="0.0.0.0", port=8200)