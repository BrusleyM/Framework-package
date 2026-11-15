import argparse
import json
import os
import pathlib

PACKAGE_NAME = "com.vishnu.framework"
INTERACTION_TOOLKIT = "com.unity.xr.interaction.toolkit"


def build_manifest(package_path, urp_version, test_framework_version, interaction_toolkit_version=None):
    dependencies = {
        PACKAGE_NAME: f"file:{package_path}",
        "com.unity.render-pipelines.universal": urp_version,
        "com.unity.test-framework": test_framework_version,
    }
    if interaction_toolkit_version:
        dependencies[INTERACTION_TOOLKIT] = interaction_toolkit_version
    return {"dependencies": dependencies, "testables": [PACKAGE_NAME]}


def create_host_project(project_path, package_root, unity_version, urp_version, test_framework_version,
                        interaction_toolkit_version=None):
    packages_folder = project_path / "Packages"
    settings_folder = project_path / "ProjectSettings"
    (project_path / "Assets").mkdir(parents=True, exist_ok=True)
    packages_folder.mkdir(parents=True, exist_ok=True)
    settings_folder.mkdir(parents=True, exist_ok=True)
    package_path = pathlib.PurePosixPath(os.path.relpath(package_root, packages_folder))
    manifest = build_manifest(package_path, urp_version, test_framework_version, interaction_toolkit_version)
    (packages_folder / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
    (settings_folder / "ProjectVersion.txt").write_text(f"m_EditorVersion: {unity_version}\n")


def main():
    parser = argparse.ArgumentParser(description="Create an empty Unity project that hosts this package for testing.")
    parser.add_argument("project_path", type=pathlib.Path)
    parser.add_argument("--package-root", type=pathlib.Path, default=pathlib.Path(__file__).resolve().parents[2])
    parser.add_argument("--unity", required=True)
    parser.add_argument("--urp", required=True)
    parser.add_argument("--test-framework", required=True)
    parser.add_argument("--xr-interaction-toolkit")
    arguments = parser.parse_args()
    create_host_project(
        arguments.project_path,
        arguments.package_root,
        arguments.unity,
        arguments.urp,
        arguments.test_framework,
        arguments.xr_interaction_toolkit,
    )
    print(f"Host project created at {arguments.project_path}")


if __name__ == "__main__":
    main()
