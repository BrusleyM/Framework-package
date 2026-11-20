import json
import pathlib
import sys
import tempfile
import unittest

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))

import core_ref
from core_ref import CoreRefError


class CoreRefTests(unittest.TestCase):
    def test_development_builds_use_the_develop_branch_of_the_core(self):
        manifest = {"dependencies": {"com.vishnu.framework.core": "1.0.0"}}

        self.assertEqual("develop", core_ref.core_ref(manifest, release=False))

    def test_a_release_uses_the_tag_of_the_core_version_the_package_depends_on(self):
        manifest = {"dependencies": {"com.vishnu.framework.core": "1.2.0"}}

        self.assertEqual("v1.2.0", core_ref.core_ref(manifest, release=True))

    def test_range_markers_and_a_leading_v_are_dropped_from_the_version(self):
        for declared in ("^1.2.0", "~1.2.0", "=1.2.0", "v1.2.0", " 1.2.0 "):
            manifest = {"dependencies": {"com.vishnu.framework.core": declared}}

            self.assertEqual("v1.2.0", core_ref.core_ref(manifest, release=True), declared)

    def test_a_preview_version_keeps_its_label(self):
        manifest = {"dependencies": {"com.vishnu.framework.core": "1.2.0-preview.1"}}

        self.assertEqual("v1.2.0-preview.1", core_ref.core_ref(manifest, release=True))

    def test_a_release_without_a_core_dependency_is_refused(self):
        with self.assertRaises(CoreRefError):
            core_ref.core_ref({"dependencies": {}}, release=True)

    def test_development_builds_do_not_need_the_dependency(self):
        self.assertEqual("develop", core_ref.core_ref({}, release=False))

    def test_the_command_line_prints_the_ref_and_fails_when_a_release_has_no_dependency(self):
        with tempfile.TemporaryDirectory() as folder:
            root = pathlib.Path(folder)
            (root / "package.json").write_text(json.dumps({"dependencies": {"com.vishnu.framework.core": "1.2.0"}}))

            self.assertEqual(0, core_ref.main(["--release"], root))

            (root / "package.json").write_text(json.dumps({"dependencies": {}}))

            self.assertEqual(1, core_ref.main(["--release"], root))


if __name__ == "__main__":
    unittest.main()
