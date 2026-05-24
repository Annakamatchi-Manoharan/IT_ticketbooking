"""
Flask ML microservice: TF-IDF vectorization + LinearSVC classification for IT ticket categories.
Used by the ASP.NET Engineer Assistant chatbot via POST /api/predict.
"""
from __future__ import annotations

import json
import os
import re
from pathlib import Path

from flask import Flask, jsonify, request
from flask_cors import CORS
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.pipeline import Pipeline
from sklearn.svm import LinearSVC
import joblib

app = Flask(__name__)
CORS(app)

MODEL_DIR = Path(__file__).parent / "models"
MODEL_PATH = MODEL_DIR / "ticket_classifier.joblib"

# Training corpus aligned with ProblemType (Network, Software, Hardware)
TRAINING_SAMPLES = [
    ("vpn not connecting remote access failed", "Network"),
    ("internet down wifi disconnected no browsing", "Network"),
    ("server down database connection timeout host unreachable", "Network"),
    ("network cable unplugged lan issue", "Network"),
    ("browser is slow web app latency", "Software"),
    ("email login failed outlook password expired", "Software"),
    ("install software application setup permission", "Software"),
    ("windows update pending laptop slow performance", "Software"),
    ("how to host application deploy iis", "Software"),
    ("printer offline paper jam cannot print", "Hardware"),
    ("monitor no display keyboard not working", "Hardware"),
    ("laptop overheating fan noise hardware failure", "Hardware"),
]

GUIDANCE = {
    "Network": {
        "possible_causes": [
            "VPN profile or credential mismatch",
            "DNS/firewall blocking required ports",
            "Upstream outage or expired certificates",
        ],
        "fix_steps": [
            "Confirm baseline internet without VPN.",
            "Collect VPN/client logs and error codes.",
            "Test path: ping/traceroute/Test-NetConnection.",
            "Validate firewall rules and split-tunnel policy.",
            "Escalate to network team with timestamps.",
        ],
        "recommended_actions": ["Attach logs", "Note affected users/sites"],
        "best_practices": ["Document rollback plan", "Change during maintenance window"],
    },
    "Software": {
        "possible_causes": [
            "Corrupted user profile or cache",
            "Pending updates or incompatible add-in",
            "Insufficient permissions for install",
        ],
        "fix_steps": [
            "Reproduce on one machine vs many.",
            "Clear cache / safe mode / repair Office.",
            "Check event viewer and application logs.",
            "Reinstall or patch to latest supported build.",
            "Validate with a second account.",
        ],
        "recommended_actions": ["Capture screenshots", "Export event logs"],
        "best_practices": ["Test in pilot group", "Use Software Center packages"],
    },
    "Hardware": {
        "possible_causes": [
            "Driver conflict or firmware drift",
            "Physical connection or power issue",
            "End-of-life device component failure",
        ],
        "fix_steps": [
            "Verify power, cables, and indicator lights.",
            "Remove and re-seat connections.",
            "Update drivers from vendor catalog.",
            "Swap with known-good peripheral if possible.",
            "Raise hardware RMA if failure persists.",
        ],
        "recommended_actions": ["Record asset tag", "Photo of error panel"],
        "best_practices": ["Track warranty status", "Standardize golden images"],
    },
}


def preprocess(text: str) -> str:
    text = text.lower()
    text = re.sub(r"[^a-z0-9\s]", " ", text)
    return re.sub(r"\s+", " ", text).strip()


def build_pipeline() -> Pipeline:
    texts, labels = zip(*TRAINING_SAMPLES)
    pipe = Pipeline([
        ("tfidf", TfidfVectorizer(ngram_range=(1, 2), min_df=1)),
        ("svm", LinearSVC()),
    ])
    pipe.fit(texts, labels)
    return pipe


def load_or_train_model() -> Pipeline:
    MODEL_DIR.mkdir(parents=True, exist_ok=True)
    if MODEL_PATH.exists():
        return joblib.load(MODEL_PATH)
    model = build_pipeline()
    joblib.dump(model, MODEL_PATH)
    return model


model = load_or_train_model()


@app.get("/health")
def health():
    return jsonify({"status": "ok", "model": str(MODEL_PATH.name)})


@app.post("/api/predict")
def predict():
    payload = request.get_json(silent=True) or {}
    text = preprocess(payload.get("text", ""))
    if not text:
        return jsonify({"error": "text is required"}), 400

    label = model.predict([text])[0]
    # Decision function confidence proxy
    try:
        scores = model.decision_function([text])[0]
        import numpy as np
        probs = np.exp(scores) / np.exp(scores).sum()
        confidence = float(probs.max())
    except Exception:
        confidence = 0.75

    guide = GUIDANCE.get(label, GUIDANCE["Software"])
    return jsonify({
        "predicted_category": label,
        "problem_type": label,
        "confidence": round(confidence, 4),
        "possible_causes": guide["possible_causes"],
        "fix_steps": guide["fix_steps"],
        "recommended_actions": guide["recommended_actions"],
        "best_practices": guide["best_practices"],
        "pipeline": "TF-IDF + LinearSVC",
    })


if __name__ == "__main__":
    port = int(os.environ.get("PORT", "5001"))
    app.run(host="0.0.0.0", port=port, debug=False)
