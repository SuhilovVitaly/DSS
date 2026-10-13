"""Run the independent sampler on current shipped combat content."""

import argparse

from . import load_game_data, sample_battle


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("scenario", choices=("CombatMukha", "CombatShmel"))
    parser.add_argument("--seed", type=int, default=1)
    parser.add_argument("--runs", type=int, default=1)
    args = parser.parse_args()
    if args.runs < 1:
        parser.error("--runs must be positive")
    data = load_game_data()
    results = [sample_battle(data, args.scenario, args.seed + i) for i in range(args.runs)]
    print(f"source={data.client_root}")
    print(f"scenario={args.scenario} seeds={args.seed}..{args.seed + args.runs - 1}")
    print(f"player_wins={sum(result.winner == 'player' for result in results)}/{args.runs}")
    print(f"hostile_wins={sum(result.winner == 'hostiles' for result in results)}/{args.runs}")
    print(f"timeouts={sum(result.winner == 'timeout' for result in results)}/{args.runs}")


if __name__ == "__main__":
    main()
