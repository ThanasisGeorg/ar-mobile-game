#!/usr/bin/env python3
"""
Στατιστική ανάλυση της μελέτης UX: AR vs Κλασικό Tower Defense.

Διαβάζει τα CSV που εξάγει η εφαρμογή (φάκελος UXStudy στη συσκευή) και παράγει:
  - report.md          αναφορά με περιγραφικά στατιστικά και ελέγχους υποθέσεων
  - summary_stats.csv  πίνακας με όλα τα αποτελέσματα (για πίνακες της εργασίας)
  - paired_data.csv    ένα ζεύγος τιμών (Classic, AR) ανά συμμετέχοντα και μετρική
  - figures/*.png      γραφήματα

Σχεδιασμός: within-subjects (κάθε συμμετέχων παίζει και τις δύο εκδόσεις) με
αντιστάθμιση σειράς (counterbalancing). Για κάθε μετρική γίνεται paired t-test και
Wilcoxon signed-rank test, υπολογίζεται το μέγεθος επίδρασης (Cohen's dz, rank-biserial r),
και διορθώνονται τα p-values ανά οικογένεια μετρικών με τη μέθοδο Holm.

Χρήση:
    python analyze_ux.py --data <φάκελος με τα CSV> --out results
"""

import argparse
import os
import sys
from pathlib import Path

import numpy as np
import pandas as pd
from scipy import stats

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402

MODES = ["Classic", "AR"]
MODE_LABELS = {"Classic": "Κλασικό", "AR": "AR"}
MODE_COLORS = {"Classic": "#3B6FB6", "AR": "#E08A1E"}

# (στήλη, ετικέτα, οικογένεια, καλύτερο όταν "higher"/"lower"/None)
METRICS = [
    ("sus_score", "SUS (0-100)", "Ευχρηστία", "higher"),
    ("igeq_competence", "iGEQ Ικανότητα", "Εμπειρία παιχνιδιού (iGEQ)", "higher"),
    ("igeq_immersion", "iGEQ Αισθητηριακή εμβύθιση", "Εμπειρία παιχνιδιού (iGEQ)", "higher"),
    ("igeq_flow", "iGEQ Ροή (flow)", "Εμπειρία παιχνιδιού (iGEQ)", "higher"),
    ("igeq_tension", "iGEQ Ένταση/Ενόχληση", "Εμπειρία παιχνιδιού (iGEQ)", "lower"),
    ("igeq_challenge", "iGEQ Πρόκληση", "Εμπειρία παιχνιδιού (iGEQ)", None),
    ("igeq_negative_affect", "iGEQ Αρνητικό συναίσθημα", "Εμπειρία παιχνιδιού (iGEQ)", "lower"),
    ("igeq_positive_affect", "iGEQ Θετικό συναίσθημα", "Εμπειρία παιχνιδιού (iGEQ)", "higher"),
    ("c01_fatigue", "Σωματική κόπωση", "Ειδικές ερωτήσεις", "lower"),
    ("c02_discomfort", "Ζάλη / οπτική δυσφορία", "Ειδικές ερωτήσεις", "lower"),
    ("c03_presence", "Αίσθηση παρουσίας", "Ειδικές ερωτήσεις", "higher"),
    ("c04_precision", "Ευκολία ακριβούς τοποθέτησης", "Ειδικές ερωτήσεις", "higher"),
    ("c05_overview", "Εποπτεία πεδίου", "Ειδικές ερωτήσεις", "higher"),
    ("setup_time_s", "Χρόνος προετοιμασίας (s)", "Αντικειμενικές μετρικές", "lower"),
    ("play_time_s", "Χρόνος παιχνιδιού (s)", "Αντικειμενικές μετρικές", None),
    ("waves_reached", "Κύματα που έφτασε", "Αντικειμενικές μετρικές", "higher"),
    ("base_hp_left", "Ζωή βάσης στο τέλος", "Αντικειμενικές μετρικές", "higher"),
    ("enemies_leaked", "Εχθροί που πέρασαν", "Αντικειμενικές μετρικές", "lower"),
    ("towers_placed", "Πύργοι που χτίστηκαν", "Αντικειμενικές μετρικές", None),
    ("placement_error_rate", "Ποσοστό λαθών τοποθέτησης", "Αντικειμενικές μετρικές", "lower"),
    ("time_to_first_tower_s", "Χρόνος έως 1ο πύργο (s)", "Αντικειμενικές μετρικές", "lower"),
    ("mean_placement_interval_s", "Μέσο διάστημα τοποθετήσεων (s)", "Αντικειμενικές μετρικές", None),
    ("taps_per_min", "Αγγίγματα ανά λεπτό", "Αντικειμενικές μετρικές", None),
    ("user_pauses", "Παύσεις από τον χρήστη", "Αντικειμενικές μετρικές", None),
    ("fps_avg", "Μέσο FPS", "Απόδοση συσκευής", "higher"),
    ("fps_1pct_low", "FPS 1% low", "Απόδοση συσκευής", "higher"),
    ("battery_drain_per_min", "Κατανάλωση μπαταρίας (%/λεπτό)", "Απόδοση συσκευής", "lower"),
]

FINAL_QUESTIONS = [
    ("f01_preference", "Συνολική προτίμηση"),
    ("f02_fun", "Πιο διασκεδαστική"),
    ("f03_ease", "Πιο εύκολη στον χειρισμό"),
    ("f04_immersion", "Πιο εμβυθιστική"),
    ("f05_strategy", "Καλύτερος σχεδιασμός στρατηγικής"),
    ("f06_again", "Θα την έπαιζα ξανά"),
]
FINAL_CHOICES = {0: "Κλασική", 1: "AR", 2: "Καμία διαφορά"}

DEMOGRAPHICS = [
    ("d01_age", "Ηλικία", {0: "18-24", 1: "25-34", 2: "35-44", 3: "45+"}),
    ("d02_gender", "Φύλο", {0: "Άνδρας", 1: "Γυναίκα", 2: "Άλλο/Δεν απαντώ"}),
    ("d03_gaming", "Συχνότητα mobile gaming", {0: "Ποτέ", 1: "Μηνιαία", 2: "Εβδομαδιαία", 3: "Καθημερινά"}),
    ("d04_ar", "Εμπειρία με AR", {0: "Καμία", 1: "Λίγη", 2: "Αρκετή", 3: "Πολλή"}),
    ("d05_td", "Εμπειρία με Tower Defense", {0: "Ποτέ", 1: "Λίγες φορές", 2: "Συχνά"}),
]

SUS_ITEMS = [f"SUS{i:02d}" for i in range(1, 11)]


# ---------------------------------------------------------------------------
# Φόρτωση δεδομένων
# ---------------------------------------------------------------------------

def read_csv_family(folder: Path, base: str) -> pd.DataFrame:
    """Διαβάζει το base.csv και τυχόν εκδόσεις base_v*.csv (αν άλλαξαν οι στήλες)."""
    frames = []
    for path in sorted(folder.glob(f"{base}*.csv")):
        if path.stem == base or path.stem.startswith(base + "_v"):
            frames.append(pd.read_csv(path))
    if not frames:
        return pd.DataFrame()
    return pd.concat(frames, ignore_index=True, sort=False)


def load_sessions(folder: Path) -> pd.DataFrame:
    df = read_csv_family(folder, "sessions")
    if df.empty:
        sys.exit(f"Δεν βρέθηκε sessions.csv στο {folder}")

    df["mode"] = df["mode"].astype(str)
    df = df[df["mode"].isin(MODES)].copy()

    if {"battery_start", "battery_end", "total_time_s"}.issubset(df.columns):
        minutes = df["total_time_s"] / 60.0
        drain = (df["battery_start"] - df["battery_end"]) * 100.0
        df["battery_drain_per_min"] = np.where(minutes > 0.5, drain / minutes, np.nan)

    # Αν ένας συμμετέχων έπαιξε μια έκδοση πολλές φορές, κρατάμε τη συνεδρία με
    # ερωτηματολόγιο (αλλιώς την τελευταία).
    df["started_at"] = pd.to_datetime(df["started_at"], errors="coerce")
    df["_has_q"] = df.get("questionnaire_completed", 0).fillna(0).astype(int)
    df = df.sort_values(["participant_id", "mode", "_has_q", "started_at"])
    df = df.groupby(["participant_id", "mode"], as_index=False).tail(1)
    return df.drop(columns="_has_q")


def paired(df: pd.DataFrame, column: str) -> pd.DataFrame:
    if column not in df.columns:
        return pd.DataFrame(columns=MODES)
    wide = df.pivot(index="participant_id", columns="mode", values=column)
    wide = wide.reindex(columns=MODES)
    return wide.apply(pd.to_numeric, errors="coerce").dropna()


# ---------------------------------------------------------------------------
# Στατιστικά
# ---------------------------------------------------------------------------

def holm(pvalues):
    p = np.asarray(pvalues, dtype=float)
    adjusted = np.full_like(p, np.nan)
    valid = ~np.isnan(p)
    pv = p[valid]
    order = np.argsort(pv)
    m = len(pv)
    adj = np.empty(m)
    running = 0.0
    for rank, idx in enumerate(order):
        running = max(running, (m - rank) * pv[idx])
        adj[idx] = min(1.0, running)
    adjusted[valid] = adj
    return adjusted


def compare(pairs: pd.DataFrame):
    n = len(pairs)
    result = {"n": n}
    for mode in MODES:
        values = pairs[mode] if n else pd.Series(dtype=float)
        result[f"{mode}_mean"] = values.mean()
        result[f"{mode}_sd"] = values.std(ddof=1)
        result[f"{mode}_median"] = values.median()

    if n < 3:
        return result

    diff = pairs["AR"] - pairs["Classic"]
    result["diff_mean"] = diff.mean()
    sd = diff.std(ddof=1)

    if sd > 0:
        t = stats.ttest_rel(pairs["AR"], pairs["Classic"])
        result["t"] = t.statistic
        result["t_p"] = t.pvalue
        result["cohen_dz"] = diff.mean() / sd
        ci = stats.t.interval(0.95, n - 1, loc=diff.mean(), scale=sd / np.sqrt(n))
        result["diff_ci_low"], result["diff_ci_high"] = ci
        if n >= 3:
            result["shapiro_p"] = stats.shapiro(diff).pvalue

    nonzero = diff[diff != 0]
    if len(nonzero) >= 1:
        try:
            w = stats.wilcoxon(pairs["AR"], pairs["Classic"], zero_method="wilcox")
            result["wilcoxon_W"] = w.statistic
            result["wilcoxon_p"] = w.pvalue
            ranks = stats.rankdata(np.abs(nonzero))
            r_plus = ranks[nonzero > 0].sum()
            r_minus = ranks[nonzero < 0].sum()
            result["rank_biserial_r"] = (r_plus - r_minus) / ranks.sum()
        except ValueError:
            pass

    # Προτεινόμενος έλεγχος: t-test αν οι διαφορές είναι κανονικές, αλλιώς Wilcoxon.
    normal = result.get("shapiro_p", 0) > 0.05
    result["test"] = "paired t" if normal and "t_p" in result else "Wilcoxon"
    result["p"] = result.get("t_p") if result["test"] == "paired t" else result.get("wilcoxon_p")
    return result


def cronbach_alpha(items: pd.DataFrame) -> float:
    items = items.dropna()
    k = items.shape[1]
    if k < 2 or len(items) < 3:
        return np.nan
    total_var = items.sum(axis=1).var(ddof=1)
    if total_var == 0:
        return np.nan
    return k / (k - 1) * (1 - items.var(ddof=1).sum() / total_var)


def sus_adjective(score: float) -> str:
    # Bangor, Kortum & Miller (2009); μέσος όρος αναφοράς ≈ 68 (Sauro, 2011).
    if np.isnan(score):
        return "-"
    if score >= 85.5:
        return "Άριστη (Excellent)"
    if score >= 71.4:
        return "Καλή (Good)"
    if score >= 50.9:
        return "Αποδεκτή (OK)"
    return "Χαμηλή (Poor)"


def interpret_effect(value: float, kind: str = "dz") -> str:
    """Cohen (1988): dz 0.2 / 0.5 / 0.8, r 0.1 / 0.3 / 0.5."""
    if value is None or np.isnan(value):
        return "-"
    small, medium, large = (0.2, 0.5, 0.8) if kind == "dz" else (0.1, 0.3, 0.5)
    a = abs(value)
    if a < small:
        return "αμελητέα"
    if a < medium:
        return "μικρή"
    if a < large:
        return "μέτρια"
    return "μεγάλη"


def fmt(value, digits=2):
    if value is None or (isinstance(value, float) and np.isnan(value)):
        return "-"
    return f"{value:.{digits}f}"


def fmt_p(p):
    if p is None or np.isnan(p):
        return "-"
    return "< .001" if p < 0.001 else f"{p:.3f}"


# ---------------------------------------------------------------------------
# Γραφήματα
# ---------------------------------------------------------------------------

def plot_paired(pairs: pd.DataFrame, title: str, path: Path):
    fig, ax = plt.subplots(figsize=(5, 4.5))
    data = [pairs[m].values for m in MODES]
    box = ax.boxplot(data, widths=0.45, patch_artist=True, showmeans=True,
                     meanprops=dict(marker="D", markerfacecolor="white", markeredgecolor="black"))
    for patch, mode in zip(box["boxes"], MODES):
        patch.set_facecolor(MODE_COLORS[mode])
        patch.set_alpha(0.55)
    for _, row in pairs.iterrows():
        ax.plot([1, 2], [row["Classic"], row["AR"]], color="grey", alpha=0.35, linewidth=1)
    ax.set_xticks([1, 2], [MODE_LABELS[m] for m in MODES])
    ax.set_title(title)
    ax.grid(axis="y", alpha=0.3)
    fig.tight_layout()
    fig.savefig(path, dpi=150)
    plt.close(fig)


def plot_igeq_profile(df: pd.DataFrame, path: Path):
    components = [m for m in METRICS if m[0].startswith("igeq_")]
    labels = [c[1].replace("iGEQ ", "") for c in components]
    x = np.arange(len(components))
    fig, ax = plt.subplots(figsize=(9, 4.5))
    for i, mode in enumerate(MODES):
        means, errors = [], []
        for col, *_ in components:
            values = pd.to_numeric(df.loc[df["mode"] == mode, col], errors="coerce").dropna()
            means.append(values.mean())
            errors.append(values.std(ddof=1) / np.sqrt(len(values)) if len(values) > 1 else 0)
        ax.bar(x + (i - 0.5) * 0.38, means, 0.38, yerr=errors, capsize=3,
               label=MODE_LABELS[mode], color=MODE_COLORS[mode], alpha=0.85)
    ax.set_xticks(x, labels, rotation=25, ha="right")
    ax.set_ylim(0, 4)
    ax.set_ylabel("Μέση τιμή (0-4) ± SE")
    ax.set_title("Προφίλ iGEQ ανά έκδοση")
    ax.legend()
    ax.grid(axis="y", alpha=0.3)
    fig.tight_layout()
    fig.savefig(path, dpi=150)
    plt.close(fig)


def plot_preferences(final: pd.DataFrame, path: Path):
    rows = []
    for col, label in FINAL_QUESTIONS:
        if col not in final.columns:
            continue
        counts = final[col].map(FINAL_CHOICES).value_counts()
        rows.append((label, [counts.get(FINAL_CHOICES[k], 0) for k in (0, 1, 2)]))
    if not rows:
        return
    fig, ax = plt.subplots(figsize=(9, 0.6 * len(rows) + 1.5))
    colors = [MODE_COLORS["Classic"], MODE_COLORS["AR"], "#9AA0A6"]
    for i, (label, counts) in enumerate(rows):
        left = 0
        total = max(1, sum(counts))
        for c, color, name in zip(counts, colors, FINAL_CHOICES.values()):
            ax.barh(i, c / total * 100, left=left, color=color, label=name if i == 0 else None)
            if c:
                ax.text(left + c / total * 50, i, str(c), ha="center", va="center", color="white", fontsize=9)
            left += c / total * 100
    ax.set_yticks(range(len(rows)), [r[0] for r in rows])
    ax.invert_yaxis()
    ax.set_xlabel("% συμμετεχόντων")
    ax.set_xlim(0, 100)
    ax.set_title("Τελική σύγκριση των δύο εκδόσεων")
    ax.legend(loc="upper center", bbox_to_anchor=(0.5, -0.18), ncol=3, fontsize=9, frameon=False)
    fig.tight_layout()
    fig.savefig(path, dpi=150)
    plt.close(fig)


# ---------------------------------------------------------------------------
# Κύρια ροή
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--data", required=True, help="Φάκελος με sessions.csv, answers.csv, final.csv")
    parser.add_argument("--out", default="results", help="Φάκελος εξόδου")
    args = parser.parse_args()

    data_dir = Path(args.data)
    out_dir = Path(args.out)
    fig_dir = out_dir / "figures"
    fig_dir.mkdir(parents=True, exist_ok=True)

    sessions = load_sessions(data_dir)
    answers = read_csv_family(data_dir, "answers")
    final = read_csv_family(data_dir, "final")
    if not final.empty:
        final = final.sort_values("completed_at").groupby("participant_id", as_index=False).tail(1)

    complete_ids = sessions.groupby("participant_id")["mode"].nunique()
    complete_ids = complete_ids[complete_ids == 2].index
    synthetic = (data_dir / "SYNTHETIC_DATA.txt").exists()

    lines = []
    w = lines.append
    w("# Αποτελέσματα μελέτης UX: AR vs Κλασικό Tower Defense\n")
    if synthetic:
        w("> **ΠΡΟΣΟΧΗ: τα δεδομένα είναι ΣΥΝΘΕΤΙΚΑ** (generate_synthetic_data.py) και χρησιμοποιούνται μόνο "
          "για έλεγχο της διαδικασίας ανάλυσης. Δεν αποτελούν ερευνητικά ευρήματα.\n")
    w(f"- Συνεδρίες: {len(sessions)}  ")
    w(f"- Συμμετέχοντες με ολοκληρωμένες και τις δύο εκδόσεις: **{len(complete_ids)}**  ")
    if "order_index" in sessions.columns:
        first = sessions[sessions["order_index"] == 1]["mode"].value_counts()
        w(f"- Σειρά (αντιστάθμιση): Κλασικό πρώτα = {first.get('Classic', 0)}, AR πρώτα = {first.get('AR', 0)}  ")
    if "outcome" in sessions.columns:
        outcome = sessions.groupby(["mode", "outcome"]).size().unstack(fill_value=0)
        w("\n**Έκβαση παιχνιδιού ανά έκδοση**\n")
        w(outcome.to_markdown())
    w("")

    # Δημογραφικά
    if not final.empty:
        w("## Δημογραφικά στοιχεία\n")
        for col, label, mapping in DEMOGRAPHICS:
            if col in final.columns:
                counts = final[col].map(mapping).value_counts()
                w(f"- **{label}:** " + ", ".join(f"{k} ({v})" for k, v in counts.items()))
        w("")

    # Αξιοπιστία SUS
    if not answers.empty:
        w("## Αξιοπιστία κλίμακας SUS (Cronbach's α)\n")
        for mode in MODES:
            sub = answers[(answers["mode"] == mode) & (answers["instrument"] == "SUS")]
            if sub.empty:
                continue
            wide = sub.pivot_table(index="session_id", columns="item_id", values="value", aggfunc="last")
            wide = wide.reindex(columns=SUS_ITEMS)
            # Αντιστροφή των αρνητικά διατυπωμένων (ζυγών) ερωτήσεων
            for item in SUS_ITEMS[1::2]:
                wide[item] = 6 - wide[item]
            w(f"- {MODE_LABELS[mode]}: α = {fmt(cronbach_alpha(wide))} (n = {len(wide.dropna())})")
        w("")

    # Σύγκριση μετρικών
    results = []
    paired_rows = []
    for column, label, family, better in METRICS:
        pairs = paired(sessions[sessions["participant_id"].isin(complete_ids)], column)
        if pairs.empty:
            continue
        res = compare(pairs)
        res.update(metric=column, label=label, family=family, better=better)
        results.append(res)
        for pid, row in pairs.iterrows():
            paired_rows.append({"participant_id": pid, "metric": column, "Classic": row["Classic"], "AR": row["AR"]})

    table = pd.DataFrame(results)
    if not table.empty:
        table["p_holm"] = np.nan
        for family, idx in table.groupby("family").groups.items():
            table.loc[idx, "p_holm"] = holm(table.loc[idx, "p"].astype(float).values)
        table.to_csv(out_dir / "summary_stats.csv", index=False)
        pd.DataFrame(paired_rows).to_csv(out_dir / "paired_data.csv", index=False)

        for family, group in table.groupby("family", sort=False):
            w(f"## {family}\n")
            w("| Μετρική | n | Κλασικό M (SD) | AR M (SD) | Διαφορά AR−Κλ. [95% CI] | Έλεγχος | p | p (Holm) | Επίδραση |")
            w("|---|---|---|---|---|---|---|---|---|")
            for _, r in group.iterrows():
                effect = r.get("cohen_dz") if r.get("test") == "paired t" else r.get("rank_biserial_r")
                effect_name = "dz" if r.get("test") == "paired t" else "r"
                ci = f"{fmt(r.get('diff_mean'))} [{fmt(r.get('diff_ci_low'))}, {fmt(r.get('diff_ci_high'))}]"
                w(f"| {r['label']} | {r['n']} | {fmt(r['Classic_mean'])} ({fmt(r['Classic_sd'])}) | "
                  f"{fmt(r['AR_mean'])} ({fmt(r['AR_sd'])}) | {ci} | {r.get('test', '-')} | "
                  f"{fmt_p(r.get('p'))} | {fmt_p(r.get('p_holm'))} | "
                  f"{effect_name} = {fmt(effect)} ({interpret_effect(effect, effect_name)}) |")
            w("")

        sus = table[table["metric"] == "sus_score"]
        if not sus.empty:
            s = sus.iloc[0]
            w(f"**Ερμηνεία SUS:** Κλασικό {fmt(s['Classic_mean'], 1)} → {sus_adjective(s['Classic_mean'])}, "
              f"AR {fmt(s['AR_mean'], 1)} → {sus_adjective(s['AR_mean'])} (μέσος όρος αναφοράς ≈ 68).\n")

    # Επίδραση σειράς (carry-over): σύγκριση της διαφοράς AR−Κλασικό μεταξύ των δύο ομάδων σειράς
    if "order_index" in sessions.columns and len(complete_ids) >= 4:
        w("## Έλεγχος επίδρασης σειράς\n")
        w("Σύγκριση της διαφοράς (AR − Κλασικό) μεταξύ όσων ξεκίνησαν με Κλασικό και όσων ξεκίνησαν με AR "
          "(Welch t-test). Σημαντικό αποτέλεσμα υποδηλώνει αλληλεπίδραση έκδοσης × σειράς (π.χ. επίδραση μάθησης).\n")
        firsts = sessions[sessions["order_index"] == 1].set_index("participant_id")["mode"]
        w("| Μετρική | Δ (Κλασικό πρώτα) | Δ (AR πρώτα) | p |")
        w("|---|---|---|---|")
        for column in ["sus_score", "igeq_positive_affect", "igeq_competence", "base_hp_left", "placement_error_rate"]:
            pairs = paired(sessions[sessions["participant_id"].isin(complete_ids)], column)
            if pairs.empty:
                continue
            diff = pairs["AR"] - pairs["Classic"]
            group = firsts.reindex(diff.index)
            a, b = diff[group == "Classic"], diff[group == "AR"]
            p = stats.ttest_ind(a, b, equal_var=False).pvalue if len(a) > 1 and len(b) > 1 else np.nan
            w(f"| {column} | {fmt(a.mean())} | {fmt(b.mean())} | {fmt_p(p)} |")
        w("")

    # Τελικό ερωτηματολόγιο
    if not final.empty:
        w("## Τελική σύγκριση (προτιμήσεις)\n")
        w("Διωνυμικός έλεγχος (Κλασικό vs AR, χωρίς τις απαντήσεις «Καμία διαφορά»).\n")
        w("| Ερώτηση | Κλασικό | AR | Καμία διαφορά | p (διωνυμικός) |")
        w("|---|---|---|---|---|")
        for col, label in FINAL_QUESTIONS:
            if col not in final.columns:
                continue
            values = pd.to_numeric(final[col], errors="coerce").dropna().astype(int)
            c, a, nd = (values == 0).sum(), (values == 1).sum(), (values == 2).sum()
            p = stats.binomtest(int(a), int(a + c), 0.5).pvalue if a + c > 0 else np.nan
            w(f"| {label} | {c} | {a} | {nd} | {fmt_p(p)} |")
        w("")
        plot_preferences(final, fig_dir / "preferences.png")
        w("![Προτιμήσεις](figures/preferences.png)\n")

    # Γραφήματα
    if not table.empty:
        w("## Γραφήματα\n")
        for column in ["sus_score", "igeq_positive_affect", "igeq_immersion", "igeq_flow", "c01_fatigue",
                       "c03_presence", "placement_error_rate", "setup_time_s", "base_hp_left", "fps_avg"]:
            pairs = paired(sessions[sessions["participant_id"].isin(complete_ids)], column)
            if len(pairs) < 2:
                continue
            label = next(m[1] for m in METRICS if m[0] == column)
            plot_paired(pairs, label, fig_dir / f"{column}.png")
            w(f"![{label}](figures/{column}.png)")
        plot_igeq_profile(sessions[sessions["participant_id"].isin(complete_ids)], fig_dir / "igeq_profile.png")
        w("\n![iGEQ](figures/igeq_profile.png)\n")

    w("---\n")
    w("Σημειώσεις: Τα p-values διορθώνονται με Holm ανά οικογένεια μετρικών. Ο προτεινόμενος έλεγχος "
      "επιλέγεται με βάση τον έλεγχο Shapiro-Wilk των διαφορών (p > .05 → paired t-test, αλλιώς Wilcoxon). "
      "Μέγεθος επίδρασης: Cohen's dz για t-test, rank-biserial r για Wilcoxon.")

    report = out_dir / "report.md"
    report.write_text("\n".join(lines), encoding="utf-8")
    print(f"Η αναφορά γράφτηκε στο {report}")


if __name__ == "__main__":
    main()
