from __future__ import annotations

import argparse
import csv
import math
from pathlib import Path

import torch
import torch.nn.functional as F


DEFAULT_INPUT = Path(__file__).resolve().parent / "Data" / "CombatArena.csv"
DEFAULT_OUTPUT = Path(__file__).resolve().parent / "elo_ratings.csv"
ELO_LOGIT_SCALE = math.log(10.0) / 400.0


def load_matches(path: Path) -> tuple[list[str], torch.Tensor, torch.Tensor, torch.Tensor, torch.Tensor, torch.Tensor]:
    unit_to_index: dict[str, int] = {}
    side_a: list[int] = []
    side_b: list[int] = []
    count_a: list[float] = []
    count_b: list[float] = []
    targets: list[float] = []

    def index_for(unit_type: str) -> int:
        if unit_type not in unit_to_index:
            unit_to_index[unit_type] = len(unit_to_index)
        return unit_to_index[unit_type]

    with path.open(newline="", encoding="utf-8") as combat_log:
        reader = csv.reader(combat_log)
        for line_number, row in enumerate(reader, start=1):
            if not row:
                continue
            if len(row) != 5:
                raise ValueError(f"Expected 5 columns on line {line_number}, got {len(row)}")

            unit_a, count_a_text, unit_b, count_b_text, result_text = [value.strip() for value in row]
            parsed_count_a = int(count_a_text)
            parsed_count_b = int(count_b_text)
            if parsed_count_a <= 0 or parsed_count_b <= 0:
                raise ValueError(f"Expected positive unit counts on line {line_number}")

            result = int(result_text)
            if result not in (-1, 0, 1):
                raise ValueError(f"Expected result -1, 0, or 1 on line {line_number}, got {result}")

            side_a.append(index_for(unit_a))
            side_b.append(index_for(unit_b))
            count_a.append(math.log(parsed_count_a))
            count_b.append(math.log(parsed_count_b))
            targets.append({1: 1.0, 0: 0.5, -1: 0.0}[result])

    unit_types = [unit_type for unit_type, _index in sorted(unit_to_index.items(), key=lambda item: item[1])]
    return (
        unit_types,
        torch.tensor(side_a, dtype=torch.long),
        torch.tensor(side_b, dtype=torch.long),
        torch.tensor(count_a, dtype=torch.float32),
        torch.tensor(count_b, dtype=torch.float32),
        torch.tensor(targets, dtype=torch.float32),
    )


def train_elo(
    side_a: torch.Tensor,
    side_b: torch.Tensor,
    count_a: torch.Tensor,
    count_b: torch.Tensor,
    targets: torch.Tensor,
    unit_count: int,
    max_iter: int,
    tolerance_grad: float,
    tolerance_change: float,
    center_penalty: float,
) -> tuple[torch.Tensor, torch.Tensor]:
    rating_logits = torch.nn.Parameter(torch.zeros(unit_count, dtype=torch.float32))
    count_importance = torch.nn.Parameter(torch.tensor(0.0, dtype=torch.float32))
    optimizer = torch.optim.LBFGS(
        [rating_logits, count_importance],
        max_iter=max_iter,
        tolerance_grad=tolerance_grad,
        tolerance_change=tolerance_change,
        line_search_fn="strong_wolfe",
    )

    def calculate_loss() -> tuple[torch.Tensor, torch.Tensor, torch.Tensor]:
        score_a = rating_logits[side_a] + count_a * count_importance
        score_b = rating_logits[side_b] + count_b * count_importance
        logits = score_a - score_b
        fit_loss = F.binary_cross_entropy_with_logits(logits, targets)
        elo_offsets = rating_logits / ELO_LOGIT_SCALE
        center_loss = torch.mean(rating_logits.square())
        loss = fit_loss + center_penalty * center_loss
        return loss, fit_loss, center_loss

    def closure() -> torch.Tensor:
        optimizer.zero_grad()
        loss, _fit_loss, _center_loss = calculate_loss()
        loss.backward()
        return loss

    optimizer.step(closure)

    with torch.no_grad():
        loss, fit_loss, center_loss = calculate_loss()
        print(
            f"loss={loss.item():.6f} "
            f"fit_loss={fit_loss.item():.6f} "
            f"center_mse={center_loss.item():.2f} "
            f"count_importance={count_importance.item():.6f}"
        )

    learned_logits = rating_logits.detach()
    ratings = 1500.0 + (learned_logits / ELO_LOGIT_SCALE)
    combat_power = torch.exp(learned_logits) * 60.0
    return ratings, combat_power


def write_ratings(path: Path, unit_types: list[str], ratings: torch.Tensor, combat_power: torch.Tensor) -> None:
    ranked = sorted(
        zip(unit_types, ratings.tolist(), combat_power.tolist(), strict=True),
        key=lambda item: item[1],
        reverse=True,
    )
    with path.open("w", newline="", encoding="utf-8") as output:
        writer = csv.writer(output)
        writer.writerow(["unit_type", "elo", "combat_power"])
        for unit_type, rating, power in ranked:
            writer.writerow([unit_type, f"{rating:.2f}", f"{power:.2f}"])


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Fit Elo ratings from CombatArena simulated combat logs.")
    parser.add_argument("--input", type=Path, default=DEFAULT_INPUT, help=f"Input CSV path. Default: {DEFAULT_INPUT}")
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT, help=f"Output CSV path. Default: {DEFAULT_OUTPUT}")
    parser.add_argument("--max-iter", type=int, default=1000, help="Maximum LBFGS iterations. Default: 1000")
    parser.add_argument(
        "--tolerance-grad",
        type=float,
        default=1e-7,
        help="LBFGS first-order optimality tolerance. Default: 1e-7",
    )
    parser.add_argument(
        "--tolerance-change",
        type=float,
        default=1e-9,
        help="LBFGS function and parameter change tolerance. Default: 1e-9",
    )
    parser.add_argument(
        "--center-penalty",
        type=float,
        default=1e-3,
        help="MSE penalty on rating offsets from 1500. Default: 1e-3",
    )
    return parser.parse_args()


def main() -> None:
    torch.manual_seed(0)
    args = parse_args()

    unit_types, side_a, side_b, count_a, count_b, targets = load_matches(args.input)
    if not unit_types:
        raise ValueError(f"No matches found in {args.input}")

    ratings, combat_power = train_elo(
        side_a=side_a,
        side_b=side_b,
        count_a=count_a,
        count_b=count_b,
        targets=targets,
        unit_count=len(unit_types),
        max_iter=args.max_iter,
        tolerance_grad=args.tolerance_grad,
        tolerance_change=args.tolerance_change,
        center_penalty=args.center_penalty,
    )
    write_ratings(args.output, unit_types, ratings, combat_power)

    print(f"Wrote {len(unit_types)} Elo ratings to {args.output}")


if __name__ == "__main__":
    main()
