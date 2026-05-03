"""LangGraph + LangChain Runnable: per-user telemetry → AI tuning + playstyle label."""

from __future__ import annotations

import json
import re
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Dict, List, TypedDict

import statistics
from langchain_core.runnables import RunnableLambda
from langgraph.graph import END, START, StateGraph

DATA_DIR = Path(__file__).resolve().parent / "data"
USERS_DIR = DATA_DIR / "users"
LEGACY_PATH = DATA_DIR / "player_model.json"


class GraphState(TypedDict, total=False):
    samples: List[Dict[str, Any]]
    userId: str
    displayName: str
    profile: Dict[str, Any]
    debugFeatures: Dict[str, Any]


def sanitize_user_id(raw: str) -> str:
    s = (raw or "guest").strip().lower()
    s = re.sub(r"[^a-z0-9_-]+", "_", s)
    s = re.sub(r"_+", "_", s).strip("_")
    if not s:
        s = "guest"
    if len(s) > 64:
        s = s[:64]
    return s


def _ensure_data() -> None:
    DATA_DIR.mkdir(parents=True, exist_ok=True)
    USERS_DIR.mkdir(parents=True, exist_ok=True)


def migrate_legacy_if_needed() -> None:
    """One-time: old single-file model → users/guest.json."""
    _ensure_data()
    if not LEGACY_PATH.exists():
        return
    if any(USERS_DIR.glob("*.json")):
        return
    try:
        data = json.loads(LEGACY_PATH.read_text(encoding="utf-8"))
        if not isinstance(data, dict):
            return
        data["userId"] = "guest"
        data.setdefault("displayName", "Guest")
        data.setdefault("wins", 0)
        data.setdefault("losses", 0)
        if "playerType" not in data:
            data["playerType"] = classify_playstyle_from_disk(data)
        (USERS_DIR / "guest.json").write_text(json.dumps(data, indent=2), encoding="utf-8")
        LEGACY_PATH.rename(LEGACY_PATH.with_suffix(".migrated.bak"))
    except OSError:
        pass


def _default_profile() -> Dict[str, Any]:
    return {
        "defenseStandoffMultiplier": 1.0,
        "defenseSpeedMultiplier": 1.0,
        "jumpBlockBonus": 0.0,
        "stealCooldownMultiplier": 1.0,
        "reactionDelayMultiplier": 1.0,
        "summary": "",
        "playerType": "All-Around",
    }


def _default_user_record(user_id: str) -> Dict[str, Any]:
    return {
        "userId": user_id,
        "displayName": user_id,
        "sampleCount": 0,
        "wins": 0,
        "losses": 0,
        "avgAbsVxBallEma": 0.0,
        "chargeRateEma": 0.0,
        "stdevVxEma": 0.0,
        "profile": _default_profile(),
        "playerType": "New",
        "updatedAt": "",
    }


def _user_path(user_id: str) -> Path:
    return USERS_DIR / f"{user_id}.json"


def _load_user(user_id: str) -> Dict[str, Any]:
    migrate_legacy_if_needed()
    _ensure_data()
    user_id = sanitize_user_id(user_id)
    path = _user_path(user_id)
    if not path.exists():
        return _default_user_record(user_id)
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
        if not isinstance(data, dict):
            return _default_user_record(user_id)
        data.setdefault("userId", user_id)
        data.setdefault("displayName", user_id)
        data.setdefault("wins", 0)
        data.setdefault("losses", 0)
        data.setdefault("sampleCount", 0)
        data.setdefault("profile", _default_profile())
        return data
    except (OSError, json.JSONDecodeError):
        return _default_user_record(user_id)


def _save_user(user_id: str, data: Dict[str, Any]) -> None:
    _ensure_data()
    user_id = sanitize_user_id(user_id)
    data["userId"] = user_id
    _user_path(user_id).write_text(json.dumps(data, indent=2), encoding="utf-8")


def classify_playstyle_from_disk(disk: Dict[str, Any]) -> str:
    """Label playstyle from smoothed telemetry (ball-speed, shot-charging, lateral variance)."""
    d = float(disk.get("avgAbsVxBallEma", 0.0))
    c = float(disk.get("chargeRateEma", 0.0))
    s = float(disk.get("stdevVxEma", 0.0))
    drive = min(1.0, d / 5.0)
    erratic = min(1.0, s / 4.0)

    primary = ""
    if erratic >= 0.72:
        primary = "Chaos Agent"
    elif drive >= 0.58:
        primary = "Rim Driver"
    elif drive >= 0.42 and erratic >= 0.38:
        primary = "Pace Pusher"
    elif drive >= 0.48 and erratic <= 0.30:
        primary = "Straight-Line Driver"
    elif c >= 0.28 and drive <= 0.36:
        primary = "Spot-Up Sniper"
    elif c >= 0.24 and 0.26 <= drive <= 0.52:
        primary = "Off-the-Dribble Shooter"
    elif drive <= 0.22 and c >= 0.16:
        primary = "Catch-and-Shoot"
    elif drive <= 0.28 and erratic <= 0.30 and c <= 0.12:
        primary = "Floor General"
    elif drive <= 0.34 and erratic >= 0.52:
        primary = "Shift Hunter"
    elif 0.32 <= drive < 0.48 and 0.28 <= erratic < 0.52:
        primary = "Stop-and-Go"

    if not primary:
        primary = "All-Around"

    modifiers: List[str] = []
    if primary != "Chaos Agent" and primary != "Shift Hunter" and erratic >= 0.55:
        modifiers.append("Unpredictable")
    if primary in ("Rim Driver", "Straight-Line Driver", "Pace Pusher") and c >= 0.18:
        modifiers.append("Shot-Ready")
    if primary == "All-Around" and erratic >= 0.40 and drive >= 0.35:
        modifiers.append("Scrappy")

    if modifiers:
        return f"{primary} / {modifiers[0]}"
    return primary


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

    p["summary"] = f"drive={drive_score:.2f} charge={charge:.2f} vx_std={stdev:.2f}"
    return p


def merge_and_infer(state: GraphState) -> GraphState:
    samples = state.get("samples") or []
    user_id = sanitize_user_id(state.get("userId", "guest"))
    display_name = (state.get("displayName") or "").strip()

    disk = _load_user(user_id)
    if display_name:
        disk["displayName"] = display_name[:48]

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
    ptype = classify_playstyle_from_disk(disk)
    disk["playerType"] = ptype
    profile["playerType"] = ptype
    disk["profile"] = profile
    disk["updatedAt"] = datetime.now(timezone.utc).isoformat()
    _save_user(user_id, disk)

    return {"profile": profile, "debugFeatures": ema_feat}


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


def run_learning(
    samples: List[Dict[str, Any]], user_id: str, display_name: str = ""
) -> Dict[str, Any]:
    global _compiled
    if _compiled is None:
        _compiled = build_graph()
    migrate_legacy_if_needed()
    out = _compiled.invoke(
        {
            "samples": samples,
            "userId": user_id,
            "displayName": display_name,
        }
    )
    return out.get("profile", _default_profile())


def get_stored_profile(user_id: str = "guest") -> Dict[str, Any]:
    migrate_legacy_if_needed()
    disk = _load_user(user_id)
    prof = disk.get("profile", _default_profile())
    prof = dict(prof)
    prof["playerType"] = disk.get("playerType", prof.get("playerType", "All-Around"))
    return prof


def record_match(user_id: str, won: bool) -> None:
    migrate_legacy_if_needed()
    user_id = sanitize_user_id(user_id)
    disk = _load_user(user_id)
    if won:
        disk["wins"] = int(disk.get("wins", 0)) + 1
    else:
        disk["losses"] = int(disk.get("losses", 0)) + 1
    disk["updatedAt"] = datetime.now(timezone.utc).isoformat()
    _save_user(user_id, disk)


def list_users(limit: int = 30) -> List[Dict[str, Any]]:
    migrate_legacy_if_needed()
    _ensure_data()
    rows: List[Dict[str, Any]] = []
    for path in USERS_DIR.glob("*.json"):
        try:
            d = json.loads(path.read_text(encoding="utf-8"))
            if not isinstance(d, dict):
                continue
            uid = d.get("userId", path.stem)
            rows.append(
                {
                    "userId": str(uid),
                    "displayName": str(d.get("displayName", uid)),
                    "wins": int(d.get("wins", 0)),
                    "losses": int(d.get("losses", 0)),
                    "sampleCount": int(d.get("sampleCount", 0)),
                    "playerType": str(d.get("playerType", "Unknown")),
                }
            )
        except (OSError, json.JSONDecodeError):
            continue
    rows.sort(key=lambda r: (r["wins"], r["sampleCount"], r["displayName"]), reverse=True)
    return rows[: max(1, min(limit, 100))]
