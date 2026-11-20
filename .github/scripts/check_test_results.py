import pathlib
import re
import sys
import xml.etree.ElementTree as ElementTree


def read_totals(results_file):
    try:
        root = ElementTree.parse(results_file).getroot()
    except ElementTree.ParseError:
        return None
    return int(root.get("total", 0)), int(root.get("failed", 0))


def find_problems(results_files):
    if not results_files:
        return ["No test result files were found."]
    problems = []
    for results_file in results_files:
        totals = read_totals(results_file)
        if totals is None:
            problems.append(f"{results_file.name}: could not be read as test results")
            continue
        total, failed = totals
        if total == 0:
            problems.append(f"{results_file.name}: no tests ran")
        elif failed:
            problems.append(f"{results_file.name}: {failed} of {total} tests failed")
    return problems


def main(results_folder):
    results_files = sorted(pathlib.Path(results_folder).glob("*-results.xml"))
    problems = find_problems(results_files)
    for results_file in results_files:
        totals = read_totals(results_file)
        if totals is not None:
            print(f"{results_file.name}: {totals[0]} tests, {totals[1]} failed")
    for problem in problems:
        print(f"::error::{problem}")
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1]))
