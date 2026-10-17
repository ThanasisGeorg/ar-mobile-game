#!/usr/bin/env python3
"""
Παράγει ΣΥΝΘΕΤΙΚΑ (ψεύτικα) δεδομένα στη μορφή που γράφει η εφαρμογή, ΜΟΝΟ για να
δοκιμαστεί η διαδικασία ανάλυσης (analyze_ux.py) πριν από τη συλλογή πραγματικών δεδομένων.

Τα δεδομένα αυτά ΔΕΝ πρέπει να χρησιμοποιηθούν ως ευρήματα. Ο φάκελος εξόδου
περιέχει το αρχείο SYNTHETIC_DATA.txt και η αναφορά το επισημαίνει.

Χρήση:
    python generate_synthetic_data.py --out synthetic_data --participants 24
"""

import argparse
import csv
from datetime import datetime, timedelta
from pathlib import Path

import numpy as np

SESSION_COLUMNS = [
    "participant_id", "session_id", "mode", "order_index", "started_at", "device_model", "os", "app_version",
    "outcome", "setup_time_s", "play_time_s", "pause_time_s", "system_pause_time_s", "total_time_s",
    "waves_reached", "total_waves", "base_hp_left", "base_hp_max", "enemies_killed", "enemies_leaked",
    "coins_earned", "coins_spent", "towers_placed", "placement_attempts", "placement_fail_no_coins",
    "placement_fail_invalid", "placement_fail_occupied", "placement_miss_board", "placement_error_rate",
    "time_to_first_tower_s", "mean_placement_interval_s", "taps_total", "taps_ui", "taps_world", "taps_per_min",
    "user_pauses", "camera_pan_gestures", "camera_zoom_gestures", "ar_time_to_first_plane_s",
    "ar_board_placements", "ar_board_width_m", "ar_tracking_losses", "ar_tracking_loss_time_s",
    "ar_device_path_m", "ar_device_rotation_deg", "fps_avg", "fps_1pct_low", "frame_time_ms_avg",
    "battery_start", "battery_end", "questionnaire_completed", "sus_score", "igeq_competence",
    "igeq_immersion", "igeq_flow", "igeq_tension", "igeq_challenge", "igeq_negative_affect",
    "igeq_positive_affect", "c01_fatigue", "c02_discomfort", "c03_presence", "c04_precision", "c05_overview",
]

IGEQ = {
    "competence": ["iGEQ02", "iGEQ09"], "immersion": ["iGEQ01", "iGEQ04"], "flow": ["iGEQ05", "iGEQ10"],
    "tension": ["iGEQ06", "iGEQ08"], "challenge": ["iGEQ12", "iGEQ13"],
    "negative_affect": ["iGEQ03", "iGEQ07"], "positive_affect": ["iGEQ11", "iGEQ14"],
}

# Αυθαίρετες μετατοπίσεις μόνο για να υπάρχει κάτι να ανιχνεύσει η ανάλυση.
AR_SHIFT = {"immersion": 0.8, "flow": 0.4, "positive_affect": 0.4, "competence": -0.2, "tension": 0.3,
            "challenge": 0.3, "negative_affect": 0.0}


def likert(rng, mean, low, high):
    return int(np.clip(round(rng.normal(mean, 0.8)), low, high))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", default="synthetic_data")
    parser.add_argument("--participants", type=int, default=24)
    parser.add_argument("--seed", type=int, default=7)
    args = parser.parse_args()

    rng = np.random.default_rng(args.seed)
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    (out / "SYNTHETIC_DATA.txt").write_text("Synthetic data generated for pipeline testing only.\n")

    sessions, answers, finals = [], [], []
    start = datetime(2026, 1, 1, 10, 0, 0)

    for pid in range(1, args.participants + 1):
        order = ["Classic", "AR"] if pid % 2 == 1 else ["AR", "Classic"]
        skill = rng.normal(0, 1)
        for index, mode in enumerate(order, start=1):
            ar = mode == "AR"
            t0 = start + timedelta(days=pid, minutes=30 * index)
            sid = f"P{pid:03d}_{mode}_{t0:%Y%m%d_%H%M%S}"
            learn = 0.3 if index == 2 else 0.0

            sus_items = {}
            for i in range(1, 11):
                positive = i % 2 == 1
                base = 4.0 - (0.35 if ar else 0.0) + 0.2 * skill + learn
                sus_items[f"SUS{i:02d}"] = likert(rng, base if positive else 6 - base, 1, 5)
            geq_items = {}
            for comp, items in IGEQ.items():
                base = {"tension": 0.8, "negative_affect": 0.7, "challenge": 1.8}.get(comp, 2.3)
                for item in items:
                    geq_items[item] = likert(rng, base + (AR_SHIFT[comp] if ar else 0) + 0.1 * skill, 0, 4)
            custom = {
                "C01_fatigue": likert(rng, 1.9 if ar else 0.6, 0, 4),
                "C02_discomfort": likert(rng, 0.9 if ar else 0.3, 0, 4),
                "C03_presence": likert(rng, 3.0 if ar else 1.2, 0, 4),
                "C04_precision": likert(rng, 2.4 if ar else 3.1, 0, 4),
                "C05_overview": likert(rng, 2.5 if ar else 3.0, 0, 4),
            }

            sus = sum((v - 1) if int(k[3:]) % 2 == 1 else (5 - v) for k, v in sus_items.items()) * 2.5
            igeq = {c: np.mean([geq_items[i] for i in items]) for c, items in IGEQ.items()}

            hp = int(np.clip(round(rng.normal(6 + 2 * skill - (1 if ar else 0), 2.5)), 0, 10))
            outcome = "victory" if hp > 0 else "defeat"
            waves = 10 if hp > 0 else int(rng.integers(5, 10))
            play = waves * 33 + rng.normal(0, 15)
            towers = int(np.clip(rng.normal(14, 3), 4, 30))
            invalid = int(rng.poisson(3.0 if ar else 1.2))
            occupied = int(rng.poisson(0.6))
            missed = int(rng.poisson(1.5 if ar else 0.2))
            nocoin = int(rng.poisson(2))
            attempts = towers + invalid + occupied + missed + nocoin
            taps = attempts + int(rng.integers(5, 15))
            battery = rng.uniform(0.4, 1.0)

            row = {
                "participant_id": pid, "session_id": sid, "mode": mode, "order_index": index,
                "started_at": t0.strftime("%Y-%m-%dT%H:%M:%S"), "device_model": "SyntheticPhone", "os": "Android 14",
                "app_version": "0.1", "outcome": outcome,
                "setup_time_s": round(rng.normal(38 if ar else 14, 8 if ar else 4), 2),
                "play_time_s": round(play, 2), "pause_time_s": 0, "system_pause_time_s": round(rng.exponential(3) if ar else 0, 2),
                "total_time_s": round(play + 40, 2), "waves_reached": waves, "total_waves": 10,
                "base_hp_left": hp, "base_hp_max": 10, "enemies_killed": 90 - (10 - hp), "enemies_leaked": 10 - hp,
                "coins_earned": 900, "coins_spent": towers * 20, "towers_placed": towers, "placement_attempts": attempts,
                "placement_fail_no_coins": nocoin, "placement_fail_invalid": invalid, "placement_fail_occupied": occupied,
                "placement_miss_board": missed,
                "placement_error_rate": round((invalid + occupied + missed) / attempts, 3),
                "time_to_first_tower_s": round(rng.normal(9 if ar else 5, 2), 2),
                "mean_placement_interval_s": round(play / max(1, towers - 1), 2),
                "taps_total": taps, "taps_ui": int(rng.integers(3, 8)), "taps_world": taps - 5,
                "taps_per_min": round(taps / (play / 60), 2), "user_pauses": int(rng.poisson(0.3)),
                "camera_pan_gestures": "" if ar else int(rng.poisson(8)),
                "camera_zoom_gestures": "" if ar else int(rng.poisson(4)),
                "ar_time_to_first_plane_s": round(rng.normal(6, 2), 2) if ar else "",
                "ar_board_placements": int(1 + rng.poisson(0.8)) if ar else "", "ar_board_width_m": 0.9 if ar else "",
                "ar_tracking_losses": int(rng.poisson(0.7)) if ar else "",
                "ar_tracking_loss_time_s": round(rng.exponential(2), 2) if ar else "",
                "ar_device_path_m": round(rng.normal(9, 3), 2) if ar else "",
                "ar_device_rotation_deg": round(rng.normal(2500, 600), 1) if ar else "",
                "fps_avg": round(rng.normal(52 if ar else 59, 3), 2), "fps_1pct_low": round(rng.normal(34 if ar else 50, 5), 2),
                "frame_time_ms_avg": round(1000 / (52 if ar else 59), 2),
                "battery_start": round(battery, 2),
                "battery_end": round(battery - (play / 60) * (0.008 if ar else 0.004), 3),
                "questionnaire_completed": 1, "sus_score": sus,
                **{f"igeq_{k}": round(v, 2) for k, v in igeq.items()},
                **{k.lower(): v for k, v in custom.items()},
            }
            sessions.append(row)

            for item_id, value in {**sus_items, **geq_items, **custom}.items():
                instrument = "SUS" if item_id.startswith("SUS") else "iGEQ" if item_id.startswith("iGEQ") else "CUSTOM"
                answers.append({"participant_id": pid, "session_id": sid, "mode": mode, "order_index": index,
                                "instrument": instrument, "item_id": item_id, "value": value})

        finals.append({
            "participant_id": pid, "completed_at": (start + timedelta(days=pid, hours=2)).strftime("%Y-%m-%dT%H:%M:%S"),
            "first_mode": order[0], "classic_done": 1, "ar_done": 1,
            "f01_preference": int(rng.choice([0, 1, 2], p=[0.35, 0.55, 0.10])),
            "f02_fun": int(rng.choice([0, 1, 2], p=[0.25, 0.65, 0.10])),
            "f03_ease": int(rng.choice([0, 1, 2], p=[0.65, 0.20, 0.15])),
            "f04_immersion": int(rng.choice([0, 1, 2], p=[0.10, 0.85, 0.05])),
            "f05_strategy": int(rng.choice([0, 1, 2], p=[0.60, 0.25, 0.15])),
            "f06_again": int(rng.choice([0, 1, 2], p=[0.40, 0.50, 0.10])),
            "d01_age": int(rng.choice(4, p=[0.5, 0.3, 0.1, 0.1])), "d02_gender": int(rng.choice(3, p=[0.5, 0.45, 0.05])),
            "d03_gaming": int(rng.choice(4)), "d04_ar": int(rng.choice(4, p=[0.4, 0.4, 0.15, 0.05])),
            "d05_td": int(rng.choice(3)),
        })

    def write(name, rows, columns):
        with open(out / name, "w", newline="", encoding="utf-8") as f:
            writer = csv.DictWriter(f, fieldnames=columns)
            writer.writeheader()
            writer.writerows(rows)

    write("sessions.csv", sessions, SESSION_COLUMNS)
    write("answers.csv", answers, list(answers[0].keys()))
    write("final.csv", finals, list(finals[0].keys()))
    print(f"Synthetic data for {args.participants} participants written to {out}")


if __name__ == "__main__":
    main()
