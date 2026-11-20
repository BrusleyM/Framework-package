import argparse
import json
import pathlib
import sys

PACKAGE_ROOT = pathlib.Path(__file__).resolve().parents[2]
CORE_PACKAGE = "com.vishnu.framework.core"
DEVELOPMENT_REF = "develop"


class CoreRefError(Exception):
    pass


def core_version(manifest):
    version = manifest.get("requiresCore", "")
    version = version.strip().lstrip("^~=v")
    if not version:
        raise CoreRefError(f"package.json does not declare requiresCore, the {CORE_PACKAGE} version this package needs.")
    return version


def core_ref(manifest, release):
    if release:
        return f"v{core_version(manifest)}"
    return DEVELOPMENT_REF


def main(arguments, package_root=PACKAGE_ROOT):
    parser = argparse.ArgumentParser(description="Print the ref of the core package to check out.")
    parser.add_argument("--release", action="store_true", help="use the released tag the package.json depends on instead of the development branch")
    options = parser.parse_args(arguments)
    manifest = json.loads((pathlib.Path(package_root) / "package.json").read_text(encoding="utf-8"))
    try:
        print(core_ref(manifest, options.release))
    except CoreRefError as failure:
        print(f"::error::{failure}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
