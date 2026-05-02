"""HTTP bridge between Unity and the LangGraph learning pipeline."""

from __future__ import annotations

from typing import List

from fastapi import FastAPI
from pydantic import BaseModel, Field

from graph_flow import (
    get_stored_profile,
    list_users,
    record_match,
    run_learning,
    sanitize_user_id,
)

app = FastAPI(title="B-Tier Hoops opponent learning", version="1.1.0")


class TelemetrySample(BaseModel):
    t: float
    px: float
    py: float
    vx: float
    vy: float
    ballOwner: int = Field(0, description="0 loose, 1 player, 2 AI")
    playerCharging: bool = False


class TelemetryIn(BaseModel):
    userId: str = "guest"
    displayName: str = ""
    samples: List[TelemetrySample]


class MatchIn(BaseModel):
    userId: str
    won: bool


@app.post("/telemetry")
def ingest(body: TelemetryIn):
    uid = sanitize_user_id(body.userId)
    payload = [s.model_dump() for s in body.samples]
    profile = run_learning(payload, uid, body.displayName.strip())
    return {"ok": True, "profile": profile}


@app.get("/profile")
def profile(userId: str = "guest"):
    return get_stored_profile(sanitize_user_id(userId))


@app.get("/leaderboard")
def leaderboard(limit: int = 25):
    return {"entries": list_users(limit)}


@app.post("/match")
def match_result(body: MatchIn):
    record_match(body.userId, body.won)
    return {"ok": True}


@app.get("/health")
def health():
    return {"status": "ok"}


def main():
    import uvicorn

    uvicorn.run("app:app", host="127.0.0.1", port=8765, reload=False)


if __name__ == "__main__":
    main()
