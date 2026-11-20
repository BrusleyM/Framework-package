import argparse
import json
import os
import pathlib
import shutil

PACKAGE_NAME = "com.vishnu.framework.sequencer"
INTERACTION_TOOLKIT = "com.unity.xr.interaction.toolkit"


def build_manifest(package_path, urp_version, test_framework_version, interaction_toolkit_version=None, local_dependencies=None):
    dependencies = {
        PACKAGE_NAME: f"file:{package_path}",
        "com.unity.render-pipelines.universal": urp_version,
        "com.unity.test-framework": test_framework_version,
    }
    for name, path in (local_dependencies or {}).items():
        dependencies[name] = f"file:{path}"
    if interaction_toolkit_version:
        dependencies[INTERACTION_TOOLKIT] = interaction_toolkit_version
    return {"dependencies": dependencies, "testables": [PACKAGE_NAME]}


def create_host_project(project_path, package_root, unity_version, urp_version, test_framework_version,
                        interaction_toolkit_version=None, dependency_roots=None):
    packages_folder = project_path / "Packages"
    settings_folder = project_path / "ProjectSettings"
    (project_path / "Assets").mkdir(parents=True, exist_ok=True)
    packages_folder.mkdir(parents=True, exist_ok=True)
    settings_folder.mkdir(parents=True, exist_ok=True)
    package_path = pathlib.PurePosixPath(os.path.relpath(package_root, packages_folder))
    local_dependencies = {
        name: pathlib.PurePosixPath(os.path.relpath(root, packages_folder))
        for name, root in (dependency_roots or {}).items()
    }
    manifest = build_manifest(package_path, urp_version, test_framework_version, interaction_toolkit_version, local_dependencies)
    (packages_folder / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
    (settings_folder / "ProjectVersion.txt").write_text(f"m_EditorVersion: {unity_version}\n")


def import_samples(project_path, package_root):
    manifest = json.loads((pathlib.Path(package_root) / "package.json").read_text())
    imported = []
    for sample in manifest.get("samples", []):
        destination = project_path / "Assets" / "Samples" / manifest["displayName"] / manifest["version"] / sample["displayName"]
        shutil.copytree(pathlib.Path(package_root) / sample["path"], destination)
        imported.append(destination)
    return imported


def main():
    parser = argparse.ArgumentParser(description="Create an empty Unity project that hosts this package for testing.")
    parser.add_argument("project_path", type=pathlib.Path)
    parser.add_argument("--package-root", type=pathlib.Path, default=pathlib.Path(__file__).resolve().parents[2])
    parser.add_argument("--unity", required=True)
    parser.add_argument("--urp", required=True)
    parser.add_argument("--test-framework", required=True)
    parser.add_argument("--xr-interaction-toolkit")
    parser.add_argument("--dependency", action="append", default=[], metavar="NAME=PATH")
    parser.add_argument("--import-samples", action="store_true", help="copy the package samples into the host project like the Package Manager does, so their tests run")
    arguments = parser.parse_args()
    dependency_roots = {name: pathlib.Path(path).resolve() for name, path in (item.split("=", 1) for item in arguments.dependency)}
    create_host_project(
        arguments.project_path,
        arguments.package_root,
        arguments.unity,
        arguments.urp,
        arguments.test_framework,
        arguments.xr_interaction_toolkit,
        dependency_roots,
    )
    if arguments.import_samples:
        for destination in import_samples(arguments.project_path, arguments.package_root):
            print(f"Imported sample into {destination}")
    print(f"Host project created at {arguments.project_path}")


if __name__ == "__main__":
    main()
