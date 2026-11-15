import argparse
import dataclasses
import datetime
import json
import pathlib
import re
import sys

PACKAGE_ROOT = pathlib.Path(__file__).resolve().parents[2]
SEMVER_PATTERN = re.compile(r"^(\d+)\.(\d+)\.(\d+)(?:-([0-9A-Za-z.-]+))?$")
PREVIEW_PATTERN = re.compile(r"^preview\.(\d+)$")
UNRELEASED_HEADING = "## [Unreleased]"
BUMP_KINDS = ("current", "stable", "patch", "minor", "major", "preview")


class ReleaseError(Exception):
    pass


@dataclasses.dataclass(frozen=True)
class Version:
    major: int
    minor: int
    patch: int
    prerelease: str = ""

    def __str__(self):
        core = f"{self.major}.{self.minor}.{self.patch}"
        return f"{core}-{self.prerelease}" if self.prerelease else core

    @property
    def is_prerelease(self):
        return bool(self.prerelease)


def parse_version(text):
    match = SEMVER_PATTERN.match(text)
    if not match:
        raise ReleaseError(f"'{text}' is not a semantic version (x.y.z or x.y.z-label).")
    major, minor, patch, prerelease = match.groups()
    return Version(int(major), int(minor), int(patch), prerelease or "")


def next_version(current, kind):
    if kind == "current":
        return current
    if kind == "stable":
        if not current.is_prerelease:
            raise ReleaseError(f"{current} is already a stable version.")
        return Version(current.major, current.minor, current.patch)
    if kind == "patch":
        return Version(current.major, current.minor, current.patch + 1)
    if kind == "minor":
        return Version(current.major, current.minor + 1, 0)
    if kind == "major":
        return Version(current.major + 1, 0, 0)
    if kind == "preview":
        return next_preview(current)
    raise ReleaseError(f"Unknown bump '{kind}'. Use one of: {', '.join(BUMP_KINDS)}.")


def next_preview(current):
    if not current.is_prerelease:
        return Version(current.major, current.minor, current.patch + 1, "preview.1")
    match = PREVIEW_PATTERN.match(current.prerelease)
    if not match:
        raise ReleaseError(f"Cannot continue the pre-release label '{current.prerelease}'; it is not preview.N.")
    number = int(match.group(1)) + 1
    return Version(current.major, current.minor, current.patch, f"preview.{number}")


def unreleased_body(changelog):
    start = changelog.find(UNRELEASED_HEADING)
    if start < 0:
        raise ReleaseError(f"CHANGELOG.md has no '{UNRELEASED_HEADING}' section.")
    body_start = start + len(UNRELEASED_HEADING)
    next_heading = changelog.find("\n## [", body_start)
    body_end = next_heading if next_heading >= 0 else len(changelog)
    return changelog[body_start:body_end].strip()


def promote_unreleased(changelog, version, release_date):
    if not unreleased_body(changelog):
        raise ReleaseError("The Unreleased section of CHANGELOG.md is empty; there is nothing to release.")
    if f"## [{version}]" in changelog:
        raise ReleaseError(f"CHANGELOG.md already has a section for {version}.")
    replacement = f"{UNRELEASED_HEADING}\n\n## [{version}] - {release_date}"
    return changelog.replace(UNRELEASED_HEADING, replacement, 1)


def release_notes(changelog, version):
    heading = f"## [{version}]"
    start = changelog.find(heading)
    if start < 0:
        raise ReleaseError(f"CHANGELOG.md has no section for {version}.")
    body_start = changelog.find("\n", start) + 1
    next_heading = changelog.find("\n## [", body_start)
    body_end = next_heading if next_heading >= 0 else len(changelog)
    notes = changelog[body_start:body_end].strip()
    if not notes:
        raise ReleaseError(f"The CHANGELOG.md section for {version} is empty.")
    return notes


def read_package_version(package_root):
    package = json.loads((package_root / "package.json").read_text())
    return parse_version(package["version"])


def write_package_version(package_root, version):
    path = package_root / "package.json"
    package = json.loads(path.read_text())
    package["version"] = str(version)
    path.write_text(json.dumps(package, indent=4) + "\n")


def prepare_release(package_root, kind, release_date):
    current = read_package_version(package_root)
    new_version = next_version(current, kind)
    changelog_path = package_root / "CHANGELOG.md"
    changelog = promote_unreleased(changelog_path.read_text(), new_version, release_date)
    write_package_version(package_root, new_version)
    changelog_path.write_text(changelog)
    return new_version


def pending_release(package_root, existing_tags):
    version = read_package_version(package_root)
    if f"v{version}" in existing_tags:
        return None
    changelog = (package_root / "CHANGELOG.md").read_text()
    try:
        release_notes(changelog, version)
    except ReleaseError:
        return None
    return version


def build_parser():
    parser = argparse.ArgumentParser(description="Version and release notes tooling for the package.")
    commands = parser.add_subparsers(dest="command", required=True)
    commands.add_parser("version", help="print the version in package.json")
    commands.add_parser("pending", help="read existing tags from stdin and print the version if it is prepared but not yet tagged")
    prepare = commands.add_parser("prepare", help="bump package.json and promote the changelog's Unreleased section")
    prepare.add_argument("kind", choices=BUMP_KINDS)
    prepare.add_argument("--date", default=datetime.date.today().isoformat())
    notes = commands.add_parser("notes", help="print the changelog section for a version")
    notes.add_argument("version")
    return parser


def main(arguments, package_root=PACKAGE_ROOT):
    parsed = build_parser().parse_args(arguments)
    if parsed.command == "version":
        print(read_package_version(package_root))
    elif parsed.command == "pending":
        version = pending_release(package_root, set(sys.stdin.read().split()))
        if version is not None:
            print(version)
    elif parsed.command == "prepare":
        print(prepare_release(package_root, parsed.kind, parsed.date))
    else:
        print(release_notes((package_root / "CHANGELOG.md").read_text(), parse_version(parsed.version)))


if __name__ == "__main__":
    try:
        main(sys.argv[1:])
    except ReleaseError as problem:
        print(f"::error::{problem}")
        sys.exit(1)
