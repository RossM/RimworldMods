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


def load_matches(path: Path) -> tuple[list[str], torch.Tensor, torch.Tensor, torch.Tensor]:
    unit_to_index: dict[str, int] = {}
    side_a: list[int] = []
    side_b: list[int] = []
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

            unit_a, _count_a, unit_b, _count_b, result_text = [value.strip() for value in row]
            result = int(result_text)
            if result not in (-1, 0, 1):
                raise ValueError(f"Expected result -1, 0, or 1 on line {line_number}, got {result}")

            side_a.append(index_for(unit_a))
            side_b.append(index_for(unit_b))
            targets.append({1: 1.0, 0: 0.5, -1: 0.0}[result])

    unit_types = [unit_type for unit_type, _index in sorted(unit_to_index.items(), key=lambda item: item[1])]
    return (
        unit_types,
        torch.tensor(side_a, dtype=torch.long),
        torch.tensor(side_b, dtype=torch.long),
        torch.tensor(targets, dtype=torch.float32),
    )


def train_elo(
    side_a: torch.Tensor,
    side_b: torch.Tensor,
    targets: torch.Tensor,
    unit_count: int,
    max_iter: int,
    tolerance_grad: float,
    tolerance_change: float,
    center_penalty: float,
) -> torch.Tensor:
    rating_logits = torch.nn.Parameter(torch.zeros(unit_count, dtype=torch.float32))
    optimizer = torch.optim.LBFGS(
        [rating_logits],
        max_iter=max_iter,
        tolerance_grad=tolerance_grad,
        tolerance_change=tolerance_change,
        line_search_fn="strong_wolfe",
    )

    def calculate_loss() -> tuple[torch.Tensor, torch.Tensor, torch.Tensor]:
        logits = rating_logits[side_a] - rating_logits[side_b]
        fit_loss = F.binary_cross_entropy_with_logits(logits, targets)
        elo_offsets = rating_logits / ELO_LOGIT_SCALE
        center_loss = torch.mean(elo_offsets.square())
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
            f"center_mse={center_loss.item():.2f}"
        )

    return 1500.0 + (rating_logits.detach() / ELO_LOGIT_SCALE)


def write_ratings(path: Path, unit_types: list[str], ratings: torch.Tensor) -> None:
    ranked = sorted(zip(unit_types, ratings.tolist(), strict=True), key=lambda item: item[1], reverse=True)
    with path.open("w", newline="", encoding="utf-8") as output:
        writer = csv.writer(output)
        writer.writerow(["unit_type", "elo"])
        for unit_type, rating in ranked:
            writer.writerow([unit_type, f"{rating:.2f}"])


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
        default=1e-6,
        help="MSE penalty on rating offsets from 1500. Default: 1e-6",
    )
    return parser.parse_args()


def main() -> None:
    torch.manual_seed(0)
    args = parse_args()

    unit_types, side_a, side_b, targets = load_matches(args.input)
    if not unit_types:
        raise ValueError(f"No matches found in {args.input}")

    ratings = train_elo(
        side_a=side_a,
        side_b=side_b,
        targets=targets,
        unit_count=len(unit_types),
        max_iter=args.max_iter,
        tolerance_grad=args.tolerance_grad,
        tolerance_change=args.tolerance_change,
        center_penalty=args.center_penalty,
    )
    write_ratings(args.output, unit_types, ratings)

    print(f"Wrote {len(unit_types)} Elo ratings to {args.output}")


if __name__ == "__main__":
    main()
