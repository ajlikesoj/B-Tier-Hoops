"""HTTP bridge between Unity and the LangGraph learning pipeline."""

from __future__ import annotations

from typing import List

from fastapi import FastAPI
from pydantic import BaseModel, Field

from graph_flow import get_stored_profile, run_learning

app = FastAPI(title="B-Tier Hoops opponent learning", version="1.0.0")


class TelemetrySample(BaseModel):
    t: float
    px: float
    py: float
    vx: float
    vy: float
    ballOwner: int = Field(0, description="0 loose, 1 player, 2 AI")
    playerCharging: bool = False


class TelemetryIn(BaseModel):
    samples: List[TelemetrySample]


@app.post("/telemetry")
def ingest(body: TelemetryIn):
    payload = [s.model_dump() for s in body.samples]
    profile = run_learning(payload)
    return {"ok": True, "profile": profile}


@app.get("/profile")
def profile():
    return get_stored_profile()


@app.get("/health")
def health():
    return {"status": "ok"}


def main():
    import uvicorn

    uvicorn.run("app:app", host="127.0.0.1", port=8765, reload=False)


if __name__ == "__main__":
    main()
