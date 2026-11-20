import json
import pathlib
import re
import sys

import release_tools

PACKAGE_ROOT = release_tools.PACKAGE_ROOT
EXTERNAL_ASSEMBLIES = {
    "UnityEngine.TestRunner",
    "UnityEditor.TestRunner",
    "Unity.XR.Interaction.Toolkit",
}
GUID_PATTERN = re.compile(r"^guid: ([0-9a-f]{32})\s*$", re.MULTILINE)
REQUIRED_MANIFEST_FIELDS = ("name", "version", "displayName", "description", "unity", "license")


def is_unity_ignored(relative_path):
    return any(part.startswith(".") or part.endswith("~") for part in relative_path.parts)


def imported_assets(package_root):
    for path in sorted(package_root.rglob("*")):
        relative = path.relative_to(package_root)
        if relative.parts and not is_unity_ignored(relative) and path.suffix != ".meta":
            yield path


def find_missing_metas(package_root):
    return [
        f"{path.relative_to(package_root)} has no .meta file"
        for path in imported_assets(package_root)
        if not path.with_name(path.name + ".meta").exists()
    ]


def find_orphan_metas(package_root):
    problems = []
    for meta in sorted(package_root.rglob("*.meta")):
        relative = meta.relative_to(package_root)
        asset = meta.with_name(meta.name[: -len(".meta")])
        if not is_unity_ignored(relative) and not asset.exists():
            problems.append(f"{relative} has no asset next to it")
    return problems


def find_duplicate_guids(package_root):
    owners = {}
    for meta in sorted(package_root.rglob("*.meta")):
        if is_unity_ignored(meta.relative_to(package_root)):
            continue
        match = GUID_PATTERN.search(meta.read_text())
        if match:
            owners.setdefault(match.group(1), []).append(str(meta.relative_to(package_root)))
    return [f"guid {guid} is used by {', '.join(paths)}" for guid, paths in owners.items() if len(paths) > 1]


def read_assemblies(package_root):
    assemblies = {}
    guids = set()
    for definition in package_root.rglob("*.asmdef"):
        if is_unity_ignored(definition.relative_to(package_root)):
            continue
        assemblies[json.loads(definition.read_text())["name"]] = definition
        meta = definition.with_name(definition.name + ".meta")
        match = GUID_PATTERN.search(meta.read_text()) if meta.exists() else None
        if match:
            guids.add(match.group(1))
    return assemblies, guids


def find_unresolved_assembly_references(package_root, dependency_roots=()):
    assemblies, guids = read_assemblies(package_root)
    known_names = set(assemblies)
    known_guids = set(guids)
    for dependency_root in dependency_roots:
        dependency_assemblies, dependency_guids = read_assemblies(dependency_root)
        known_names.update(dependency_assemblies)
        known_guids.update(dependency_guids)

    problems = []
    for name, definition in assemblies.items():
        for reference in json.loads(definition.read_text()).get("references", []):
            if reference.startswith("GUID:"):
                resolved = reference[len("GUID:"):] in known_guids
            else:
                resolved = reference in known_names or reference in EXTERNAL_ASSEMBLIES
            if not resolved:
                problems.append(f"{definition.relative_to(package_root)} references '{reference}', which does not exist")
    return problems


def find_manifest_problems(package_root):
    path = package_root / "package.json"
    try:
        manifest = json.loads(path.read_text())
    except (OSError, json.JSONDecodeError) as failure:
        return [f"package.json cannot be read: {failure}"]

    problems = [f"package.json is missing '{field}'" for field in REQUIRED_MANIFEST_FIELDS if not manifest.get(field)]
    try:
        release_tools.parse_version(manifest.get("version", ""))
    except release_tools.ReleaseError as problem:
        problems.append(str(problem))
    return problems


def find_sample_problems(package_root):
    manifest = json.loads((package_root / "package.json").read_text())
    problems = []
    listed = set()
    for index, sample in enumerate(manifest.get("samples", [])):
        for field in ("displayName", "description", "path"):
            if not sample.get(field):
                problems.append(f"package.json samples entry {index} has no {field}")
        path = sample.get("path")
        if path:
            listed.add(path)
            if not (package_root / path).is_dir():
                problems.append(f"package.json sample path {path} is not a folder")
    samples_root = package_root / "Samples~"
    if samples_root.is_dir():
        for folder in sorted(samples_root.iterdir()):
            if folder.is_dir() and f"Samples~/{folder.name}" not in listed:
                problems.append(f"Samples~/{folder.name} is not listed in the package.json samples")
    if (package_root / "Samples").exists():
        problems.append("Samples/ is imported into every project that installs the package: move it to Samples~ and list it in package.json")
    return problems


def find_changelog_problems(package_root, version, require_release_section):
    changelog = (package_root / "CHANGELOG.md").read_text()
    if release_tools.UNRELEASED_HEADING not in changelog:
        return [f"CHANGELOG.md has no '{release_tools.UNRELEASED_HEADING}' section"]
    if not require_release_section:
        return []
    try:
        release_tools.release_notes(changelog, version)
    except release_tools.ReleaseError as problem:
        return [str(problem)]
    return []


def collect_problems(package_root, require_release_section=False, dependency_roots=()):
    problems = find_manifest_problems(package_root)
    problems += find_missing_metas(package_root)
    problems += find_orphan_metas(package_root)
    problems += find_duplicate_guids(package_root)
    problems += find_unresolved_assembly_references(package_root, dependency_roots)
    problems += find_sample_problems(package_root)
    if not problems:
        version = release_tools.read_package_version(package_root)
        problems += find_changelog_problems(package_root, version, require_release_section)
    return problems


if __name__ == "__main__":
    arguments = sys.argv[1:]
    require_release_section = "--release" in arguments
    dependency_roots = [pathlib.Path(path) for flag, path in zip(arguments, arguments[1:]) if flag == "--dependency-root"]
    found = collect_problems(PACKAGE_ROOT, require_release_section, dependency_roots)
    for problem in found:
        print(f"::error::{problem}")
    print(f"{len(found)} problem(s) found." if found else "Package checks passed.")
    sys.exit(1 if found else 0)
