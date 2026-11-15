import pathlib
import re
import sys
import xml.etree.ElementTree as ElementTree


def read_totals(results_file):
    root = ElementTree.parse(results_file).getroot()
    return int(root.get("total", 0)), int(root.get("failed", 0))


def find_problems(results_files):
    if not results_files:
        return ["No test result files were found."]
    problems = []
    for results_file in results_files:
        total, failed = read_totals(results_file)
        if total == 0:
            problems.append(f"{results_file.name}: no tests ran")
        elif failed:
            problems.append(f"{results_file.name}: {failed} of {total} tests failed")
    return problems


def main(results_folder):
    results_files = sorted(pathlib.Path(results_folder).glob("*-results.xml"))
    problems = find_problems(results_files)
    for results_file in results_files:
        total, failed = read_totals(results_file)
        print(f"{results_file.name}: {total} tests, {failed} failed")
    for problem in problems:
        print(f"::error::{problem}")
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1]))
