"""LangGraph + LangChain Runnable: aggregate player telemetry and emit AI tuning multipliers."""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any, Dict, List, TypedDict

import statistics
from langchain_core.runnables import RunnableLambda
from langgraph.graph import END, START, StateGraph

DATA_DIR = Path(__file__).resolve().parent / "data"
STATE_PATH = DATA_DIR / "player_model.json"


class GraphState(TypedDict, total=False):
    samples: List[Dict[str, Any]]
    profile: Dict[str, Any]
    debugFeatures: Dict[str, Any]


def _ensure_data() -> None:
    DATA_DIR.mkdir(parents=True, exist_ok=True)


def _default_profile() -> Dict[str, Any]:
    return {
        "defenseStandoffMultiplier": 1.0,
        "defenseSpeedMultiplier": 1.0,
        "jumpBlockBonus": 0.0,
        "stealCooldownMultiplier": 1.0,
        "reactionDelayMultiplier": 1.0,
        "summary": "",
    }


def _load_disk() -> Dict[str, Any]:
    _ensure_data()
    if not STATE_PATH.exists():
        return {
            "sampleCount": 0,
            "avgAbsVxBallEma": 0.0,
            "chargeRateEma": 0.0,
            "stdevVxEma": 0.0,
            "profile": _default_profile(),
        }
    with open(STATE_PATH, "r", encoding="utf-8") as f:
        return json.load(f)


def _save_disk(data: Dict[str, Any]) -> None:
    _ensure_data()
    with open(STATE_PATH, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2)


def _extract_features(samples: List[Dict[str, Any]]) -> Dict[str, float]:
    if not samples:
        return {}
    vx = [float(s.get("vx", 0)) for s in samples]
    charging = [1.0 if s.get("playerCharging") else 0.0 for s in samples]
    ball_owner = [int(s.get("ballOwner", 0)) for s in samples]
    with_ball_idx = [i for i, b in enumerate(ball_owner) if b == 1]
    vx_ball = [abs(vx[i]) for i in with_ball_idx] if with_ball_idx else [0.0]
    avg_abs_vx_ball = sum(vx_ball) / max(1, len(vx_ball))
    charge_rate = sum(charging) / len(charging)
    stdev_vx = statistics.pstdev(vx) if len(vx) > 1 else 0.0
    return {
        "avgAbsVxBall": avg_abs_vx_ball,
        "chargeRate": charge_rate,
        "stdevVx": stdev_vx,
        "n": float(len(samples)),
    }


def _adjust_from_features(feat: Dict[str, float]) -> Dict[str, Any]:
    """Heuristic counter-strategy from movement habits (no LLM required for gameplay)."""
    p = _default_profile()
    if not feat:
        return p

    avg_v = feat.get("avgAbsVxBall", 0.0)
    charge = feat.get("chargeRate", 0.0)
    stdev = feat.get("stdevVx", 0.0)

    drive_score = min(1.0, avg_v / 5.0)
    p["defenseStandoffMultiplier"] = 1.0 - 0.25 * drive_score
    p["defenseSpeedMultiplier"] = 1.0 + 0.2 * drive_score

    p["jumpBlockBonus"] = min(0.35, charge * 0.5)

    erratic = min(1.0, stdev / 4.0)
    p["reactionDelayMultiplier"] = 1.0 - 0.15 * erratic
    p["stealCooldownMultiplier"] = 1.0 - 0.2 * erratic

    p["defenseStandoffMultiplier"] = max(0.65, min(1.15, p["defenseStandoffMultiplier"]))
    p["defenseSpeedMultiplier"] = max(0.9, min(1.25, p["defenseSpeedMultiplier"]))
    p["reactionDelayMultiplier"] = max(0.75, min(1.05, p["reactionDelayMultiplier"]))
    p["stealCooldownMultiplier"] = max(0.75, min(1.05, p["stealCooldownMultiplier"]))

    p["summary"] = (
        f"drive={drive_score:.2f} charge={charge:.2f} vx_std={stdev:.2f}"
    )
    return p


def merge_and_infer(state: GraphState) -> GraphState:
    samples = state.get("samples") or []
    disk = _load_disk()
    feat_batch = _extract_features(samples)

    alpha = min(0.35, 0.05 + len(samples) / 5000.0)
    if feat_batch:
        def ema(key: str, new_val: float) -> float:
            old = float(disk.get(key, new_val))
            return old * (1.0 - alpha) + new_val * alpha

        disk["avgAbsVxBallEma"] = ema("avgAbsVxBallEma", feat_batch["avgAbsVxBall"])
        disk["chargeRateEma"] = ema("chargeRateEma", feat_batch["chargeRate"])
        disk["stdevVxEma"] = ema("stdevVxEma", feat_batch["stdevVx"])

    disk["sampleCount"] = int(disk.get("sampleCount", 0)) + len(samples)

    ema_feat = {
        "avgAbsVxBall": float(disk.get("avgAbsVxBallEma", 0.0)),
        "chargeRate": float(disk.get("chargeRateEma", 0.0)),
        "stdevVx": float(disk.get("stdevVxEma", 0.0)),
        "n": float(len(samples)),
    }
    profile = _adjust_from_features(ema_feat)
    disk["profile"] = profile
    _save_disk(disk)

    return {"profile": profile, "debugFeatures": ema_feat}


# LangChain Runnable wraps the pure merge step for composition/extension (e.g. add an LLM node later).
learn_runnable: RunnableLambda = RunnableLambda(merge_and_infer)


def _learn_node(state: GraphState) -> GraphState:
    return learn_runnable.invoke(state)


def build_graph():
    workflow = StateGraph(GraphState)
    workflow.add_node("learn", _learn_node)
    workflow.add_edge(START, "learn")
    workflow.add_edge("learn", END)
    return workflow.compile()


_compiled = None


def run_learning(samples: List[Dict[str, Any]]) -> Dict[str, Any]:
    global _compiled
    if _compiled is None:
        _compiled = build_graph()
    out = _compiled.invoke({"samples": samples})
    return out.get("profile", _default_profile())


def get_stored_profile() -> Dict[str, Any]:
    disk = _load_disk()
    return disk.get("profile", _default_profile())
