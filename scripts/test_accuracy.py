#!/usr/bin/env python3
"""
Test VecNotes semantic search accuracy using search-test.txt entries.

Usage:
  python test_accuracy.py
  python test_accuracy.py ../search-test.txt
  python test_accuracy.py --base-url http://localhost:5111 --limit 10
  python test_accuracy.py --null-threshold 0.5
"""

import argparse
import json
import re
import sys
import urllib.error
import urllib.parse
import urllib.request
from datetime import datetime
from pathlib import Path
from typing import NamedTuple

CATEGORY_ORDER = ["direct", "semantic", "precision", "conceptual", "thematic", "ambiguous", "emotional", "null"]


class TestCase(NamedTuple):
    query: str
    raw_expected: str
    category: str
    is_null: bool
    is_any_of: bool
    expected_ids: list
    excluded_ids: set


class Result(NamedTuple):
    case: TestCase
    passed: bool
    top_note_ids: list
    top_scores: list
    hit_count: int
    detail: str


def log(msg: str) -> None:
    ts = datetime.now().strftime("%H:%M:%S")
    print(f"[{ts}] {msg}", flush=True)


def parse_expected(raw: str, category: str):
    if raw.lower().startswith("none"):
        return True, False, [], set()

    is_any_of = " or " in raw.lower() or category == "ambiguous"

    excluded_ids = {int(m) for m in re.findall(r"\bnot\s+(\d+)", raw)}
    all_ints = [int(m) for m in re.findall(r"\d+", raw)]
    expected_ids = [n for n in all_ints if n not in excluded_ids]

    return False, is_any_of, expected_ids, excluded_ids


def parse_test_file(path: Path) -> list:
    cases = []
    for lineno, line in enumerate(path.read_text().splitlines(), 1):
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        parts = line.split(" | ", maxsplit=2)
        if len(parts) != 3:
            print(f"Warning: skipping malformed line {lineno}: {line!r}", file=sys.stderr)
            continue
        query, raw_expected, category = (p.strip() for p in parts)
        category = category.lower()
        is_null, is_any_of, expected_ids, excluded_ids = parse_expected(raw_expected, category)
        cases.append(TestCase(query, raw_expected, category, is_null, is_any_of, expected_ids, excluded_ids))
    return cases


def search(base_url: str, query: str, limit: int):
    url = f"{base_url}/notes/search?q={urllib.parse.quote(query)}&limit={limit}"
    try:
        with urllib.request.urlopen(url, timeout=15) as resp:
            return json.loads(resp.read())
    except urllib.error.HTTPError as e:
        body = e.read().decode(errors="replace")
        log(f"  HTTP {e.code}: {body[:200]}")
        return None
    except urllib.error.URLError as e:
        log(f"  Connection error: {e.reason}")
        return None
    except Exception as e:
        log(f"  Unexpected error: {e}")
        return None


def evaluate(case: TestCase, results: list, null_threshold: float) -> Result:
    sorted_results = sorted(results, key=lambda r: r["score"], reverse=True)
    top_note_ids = [r["noteId"] for r in sorted_results]
    top_scores = [r["score"] for r in sorted_results]
    result_id_set = set(top_note_ids)

    if case.is_null:
        max_score = max(top_scores, default=0.0)
        passed = max_score < null_threshold
        detail = f"max_score={max_score:.3f} {'<' if passed else '>='} {null_threshold:.3f}"
        return Result(case, passed, top_note_ids, top_scores, 0, detail)

    if case.category == "thematic":
        found = [n for n in case.expected_ids if n in result_id_set]
        hit_count = len(found)
        total = len(case.expected_ids)
        passed = hit_count >= 1
        found_str = ",".join(f"{n:02d}" for n in found) if found else "none"
        detail = f"{hit_count}/{total} expected found ({found_str})"
        return Result(case, passed, top_note_ids, top_scores, hit_count, detail)

    if case.category == "ambiguous":
        any_match = any(n in result_id_set for n in case.expected_ids)
        top_is_excluded = bool(top_note_ids) and top_note_ids[0] in case.excluded_ids
        passed = any_match and not top_is_excluded
        hit_count = sum(1 for n in case.expected_ids if n in result_id_set)
        if any_match and top_is_excluded:
            detail = f"found expected but top result={top_note_ids[0]:02d} is excluded"
        elif any_match:
            matched = next(n for n in case.expected_ids if n in result_id_set)
            score = top_scores[top_note_ids.index(matched)]
            detail = f"note {matched:02d}, score={score:.3f}"
        else:
            detail = f"none of {[f'{n:02d}' for n in case.expected_ids]} in results"
        return Result(case, passed, top_note_ids, top_scores, hit_count, detail)

    # direct, semantic, precision, conceptual, emotional
    found = next((n for n in case.expected_ids if n in result_id_set), None)
    passed = found is not None
    if found is not None:
        score = top_scores[top_note_ids.index(found)]
        detail = f"note {found:02d}, score={score:.3f}"
    else:
        detail = f"none of {[f'{n:02d}' for n in case.expected_ids]} in top {len(sorted_results)} results"
    return Result(case, passed, top_note_ids, top_scores, 1 if passed else 0, detail)


def log_result(i: int, total: int, result: Result) -> None:
    case = result.case
    q = case.query if len(case.query) <= 50 else case.query[:47] + "..."
    status = "PASS" if result.passed else "FAIL"
    cat = case.category.ljust(10)
    index = f"[{i:02d}/{total:02d}]"
    log(f"{index} {cat} | {q!r} → {status} ({result.detail})")


def print_summary(results: list) -> None:
    by_category: dict = {cat: [] for cat in CATEGORY_ORDER}
    for r in results:
        cat = r.case.category
        if cat not in by_category:
            by_category[cat] = []
        by_category[cat].append(r)

    print()
    header = f"{'Category':<12}  {'Tests':>5}  {'Pass':>4}  {'Fail':>4}  {'Accuracy':>8}"
    sep = f"{'':->12}  {'':->5}  {'':->4}  {'':->4}  {'':->8}"
    print(header)
    print(sep)

    total_tests = total_pass = total_fail = 0
    for cat in CATEGORY_ORDER:
        cat_results = by_category.get(cat, [])
        if not cat_results:
            continue
        n = len(cat_results)
        p = sum(1 for r in cat_results if r.passed)
        f = n - p
        acc = p / n * 100
        print(f"{cat:<12}  {n:>5}  {p:>4}  {f:>4}  {acc:>7.1f}%")
        total_tests += n
        total_pass += p
        total_fail += f

    print(sep)
    total_acc = total_pass / total_tests * 100 if total_tests else 0.0
    print(f"{'TOTAL':<12}  {total_tests:>5}  {total_pass:>4}  {total_fail:>4}  {total_acc:>7.1f}%")

    api_errors = sum(1 for r in results if r.detail == "API error")
    if api_errors:
        print(f"\nNote: {api_errors} test(s) failed due to API errors")


def probe(base_url: str) -> bool:
    url = f"{base_url}/notes/search?q=test&limit=1"
    try:
        with urllib.request.urlopen(url, timeout=5) as resp:
            resp.read()
            return True
    except Exception as e:
        log(f"API probe failed: {e}")
        return False


def main() -> None:
    parser = argparse.ArgumentParser(description="Test VecNotes semantic search accuracy")
    parser.add_argument("test_file", nargs="?", default="./search-test.txt")
    parser.add_argument("--base-url", default="http://localhost:5111")
    parser.add_argument("--limit", type=int, default=10)
    parser.add_argument("--null-threshold", type=float, default=0.5,
                        help="Max score for null tests to PASS (default: 0.5)")
    args = parser.parse_args()

    test_path = Path(args.test_file)
    if not test_path.exists():
        print(f"Error: test file not found: {test_path}", file=sys.stderr)
        sys.exit(1)

    cases = parse_test_file(test_path)
    total = len(cases)
    log(f"Loaded {total} test cases from {test_path}")
    log(f"API: {args.base_url} | limit={args.limit} | null_threshold={args.null_threshold}")

    log("Probing API...")
    if not probe(args.base_url):
        print(f"Error: cannot reach API at {args.base_url}", file=sys.stderr)
        sys.exit(1)
    log("API reachable. Starting tests.")

    results = []
    for i, case in enumerate(cases, 1):
        api_results = search(args.base_url, case.query, args.limit)
        if api_results is None:
            result = Result(case, False, [], [], 0, "API error")
        else:
            result = evaluate(case, api_results, args.null_threshold)
        results.append(result)
        log_result(i, total, result)

    print_summary(results)

    failed = sum(1 for r in results if not r.passed)
    sys.exit(0 if failed == 0 else 1)


if __name__ == "__main__":
    main()
