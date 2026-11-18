import json
import pathlib
import sys
import tempfile
import unittest

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))

import check_package
import check_test_results
import create_host_project
import release_tools
from release_tools import ReleaseError, Version, next_version, parse_version


class NextVersionTests(unittest.TestCase):
    def test_current_keeps_the_version_as_it_is(self):
        self.assertEqual("2.0.0-preview.1", str(next_version(parse_version("2.0.0-preview.1"), "current")))

    def test_patch_minor_and_major_reset_the_lower_numbers_and_drop_the_label(self):
        start = parse_version("1.4.7-preview.2")
        self.assertEqual("1.4.8", str(next_version(start, "patch")))
        self.assertEqual("1.5.0", str(next_version(start, "minor")))
        self.assertEqual("2.0.0", str(next_version(start, "major")))

    def test_stable_drops_the_label_of_a_preview(self):
        self.assertEqual("2.0.0", str(next_version(parse_version("2.0.0-preview.3"), "stable")))

    def test_stable_refuses_a_version_that_is_already_stable(self):
        with self.assertRaises(ReleaseError):
            next_version(parse_version("2.0.0"), "stable")

    def test_preview_counts_up_an_existing_preview(self):
        self.assertEqual("2.0.0-preview.2", str(next_version(parse_version("2.0.0-preview.1"), "preview")))

    def test_preview_of_a_stable_version_starts_the_next_patch_as_a_preview(self):
        self.assertEqual("2.0.1-preview.1", str(next_version(parse_version("2.0.0"), "preview")))

    def test_preview_refuses_a_label_that_is_not_preview_n(self):
        with self.assertRaises(ReleaseError):
            next_version(parse_version("2.0.0-beta.1"), "preview")

    def test_unknown_bump_is_refused(self):
        with self.assertRaises(ReleaseError):
            next_version(parse_version("1.0.0"), "huge")

    def test_text_that_is_not_a_semantic_version_is_refused(self):
        for text in ("2.0", "v2.0.0", "2.0.0.1", ""):
            with self.assertRaises(ReleaseError):
                parse_version(text)

    def test_a_version_prints_the_way_it_was_written(self):
        self.assertEqual("2.0.0-preview.1", str(Version(2, 0, 0, "preview.1")))


CHANGELOG = """# Changelog

## [Unreleased]

### Added
- A new step.

## [1.0.0] - 2026-01-01

### Added
- First release.
"""


class ChangelogTests(unittest.TestCase):
    def test_promoting_keeps_an_empty_unreleased_section_above_the_new_release(self):
        promoted = release_tools.promote_unreleased(CHANGELOG, parse_version("1.1.0"), "2026-02-02")
        self.assertIn("## [Unreleased]\n\n## [1.1.0] - 2026-02-02\n\n### Added\n- A new step.", promoted)

    def test_promoting_an_empty_unreleased_section_is_refused(self):
        empty = "# Changelog\n\n## [Unreleased]\n\n## [1.0.0] - 2026-01-01\n- First release.\n"
        with self.assertRaises(ReleaseError):
            release_tools.promote_unreleased(empty, parse_version("1.1.0"), "2026-02-02")

    def test_promoting_a_version_that_already_has_a_section_is_refused(self):
        with self.assertRaises(ReleaseError):
            release_tools.promote_unreleased(CHANGELOG, parse_version("1.0.0"), "2026-02-02")

    def test_notes_are_only_the_section_of_that_version(self):
        self.assertEqual("### Added\n- First release.", release_tools.release_notes(CHANGELOG, parse_version("1.0.0")))

    def test_notes_of_a_missing_version_are_refused(self):
        with self.assertRaises(ReleaseError):
            release_tools.release_notes(CHANGELOG, parse_version("9.9.9"))

    def test_a_changelog_without_unreleased_is_refused(self):
        with self.assertRaises(ReleaseError):
            release_tools.unreleased_body("# Changelog\n\n## [1.0.0] - 2026-01-01\n- x\n")


class PrepareReleaseTests(unittest.TestCase):
    def test_preparing_rewrites_the_manifest_and_the_changelog_together(self):
        with tempfile.TemporaryDirectory() as folder:
            root = pathlib.Path(folder)
            (root / "package.json").write_text(json.dumps({"name": "x", "version": "1.0.0"}))
            (root / "CHANGELOG.md").write_text(CHANGELOG.replace("## [1.0.0] - 2026-01-01", "## [0.9.0] - 2026-01-01"))

            new_version = release_tools.prepare_release(root, "minor", "2026-03-03")

            self.assertEqual("1.1.0", str(new_version))
            self.assertEqual("1.1.0", json.loads((root / "package.json").read_text())["version"])
            self.assertIn("## [1.1.0] - 2026-03-03", (root / "CHANGELOG.md").read_text())

    def test_a_failed_preparation_leaves_both_files_untouched(self):
        with tempfile.TemporaryDirectory() as folder:
            root = pathlib.Path(folder)
            manifest = json.dumps({"name": "x", "version": "1.0.0"})
            changelog = "# Changelog\n\n## [Unreleased]\n\n"
            (root / "package.json").write_text(manifest)
            (root / "CHANGELOG.md").write_text(changelog)

            with self.assertRaises(ReleaseError):
                release_tools.prepare_release(root, "patch", "2026-03-03")

            self.assertEqual(manifest, (root / "package.json").read_text())
            self.assertEqual(changelog, (root / "CHANGELOG.md").read_text())


class PendingReleaseTests(unittest.TestCase):
    def package_with(self, folder, version, changelog):
        root = pathlib.Path(folder)
        (root / "package.json").write_text(json.dumps({"name": "x", "version": version}))
        (root / "CHANGELOG.md").write_text(changelog)
        return root

    def test_a_prepared_version_that_is_not_tagged_is_pending(self):
        with tempfile.TemporaryDirectory() as folder:
            root = self.package_with(folder, "1.0.0", CHANGELOG)

            self.assertEqual("1.0.0", str(release_tools.pending_release(root, {"v0.9.0"})))

    def test_a_version_that_is_already_tagged_is_not_pending(self):
        with tempfile.TemporaryDirectory() as folder:
            root = self.package_with(folder, "1.0.0", CHANGELOG)

            self.assertIsNone(release_tools.pending_release(root, {"v1.0.0"}))

    def test_a_version_without_a_changelog_section_is_not_pending(self):
        with tempfile.TemporaryDirectory() as folder:
            root = self.package_with(folder, "1.1.0", CHANGELOG)

            self.assertIsNone(release_tools.pending_release(root, {"v1.0.0"}))


def write_asset(root, relative, content="", guid=None):
    path = root / relative
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content)
    if guid is not None:
        path.with_name(path.name + ".meta").write_text(f"fileFormatVersion: 2\nguid: {guid}\n")
    return path


class PackageCheckTests(unittest.TestCase):
    def setUp(self):
        self._folder = tempfile.TemporaryDirectory()
        self.root = pathlib.Path(self._folder.name)

    def tearDown(self):
        self._folder.cleanup()

    def test_a_script_without_a_meta_file_is_reported(self):
        (self.root / "Runtime").mkdir()
        (self.root / "Runtime.meta").write_text("guid: " + "a" * 32 + "\n")
        (self.root / "Runtime" / "Step.cs").write_text("")

        self.assertEqual(["Runtime/Step.cs has no .meta file"], check_package.find_missing_metas(self.root))

    def test_hidden_and_tilde_folders_need_no_meta_files(self):
        write_asset(self.root, ".github/workflow.yml")
        write_asset(self.root, "Documentation~/guide.md")

        self.assertEqual([], check_package.find_missing_metas(self.root))

    def test_a_meta_file_without_its_asset_is_reported(self):
        (self.root / "Gone.cs.meta").write_text("guid: " + "b" * 32 + "\n")

        self.assertEqual(["Gone.cs.meta has no asset next to it"], check_package.find_orphan_metas(self.root))

    def test_two_assets_sharing_a_guid_are_reported(self):
        write_asset(self.root, "A.cs", guid="c" * 32)
        write_asset(self.root, "B.cs", guid="c" * 32)

        self.assertEqual(1, len(check_package.find_duplicate_guids(self.root)))

    def test_an_assembly_reference_that_does_not_exist_is_reported(self):
        write_asset(self.root, "One.asmdef", json.dumps({"name": "One", "references": ["Missing"]}), guid="d" * 32)

        problems = check_package.find_unresolved_assembly_references(self.root)

        self.assertEqual(1, len(problems))
        self.assertIn("'Missing'", problems[0])

    def test_references_by_name_by_guid_and_to_known_test_assemblies_resolve(self):
        write_asset(self.root, "Core.asmdef", json.dumps({"name": "Core", "references": []}), guid="e" * 32)
        write_asset(
            self.root,
            "User.asmdef",
            json.dumps({"name": "User", "references": ["Core", "GUID:" + "e" * 32, "UnityEngine.TestRunner"]}),
            guid="f" * 32,
        )

        self.assertEqual([], check_package.find_unresolved_assembly_references(self.root))

    def test_references_into_a_dependency_package_resolve_by_name_and_by_guid(self):
        with tempfile.TemporaryDirectory() as folder:
            dependency_root = pathlib.Path(folder)
            write_asset(dependency_root, "Shared.asmdef", json.dumps({"name": "Shared", "references": []}), guid="1" * 32)
            write_asset(
                self.root,
                "User.asmdef",
                json.dumps({"name": "User", "references": ["Shared", "GUID:" + "1" * 32]}),
                guid="2" * 32,
            )

            self.assertEqual(2, len(check_package.find_unresolved_assembly_references(self.root)))
            self.assertEqual([], check_package.find_unresolved_assembly_references(self.root, [dependency_root]))

    def test_a_manifest_missing_fields_or_with_a_bad_version_is_reported(self):
        (self.root / "package.json").write_text(json.dumps({"name": "x", "version": "one"}))

        problems = check_package.find_manifest_problems(self.root)

        self.assertTrue(any("displayName" in problem for problem in problems))
        self.assertTrue(any("not a semantic version" in problem for problem in problems))

    def test_a_release_check_needs_the_changelog_section_of_the_manifest_version(self):
        (self.root / "CHANGELOG.md").write_text("# Changelog\n\n## [Unreleased]\n\n- x\n")

        self.assertEqual([], check_package.find_changelog_problems(self.root, parse_version("1.0.0"), False))
        self.assertEqual(1, len(check_package.find_changelog_problems(self.root, parse_version("1.0.0"), True)))


class HostProjectTests(unittest.TestCase):
    def test_the_manifest_points_at_the_package_and_makes_its_tests_runnable(self):
        manifest = create_host_project.build_manifest("../..", "17.3.0", "1.6.0")

        self.assertEqual("file:../..", manifest["dependencies"]["com.vishnu.framework.sequencer"])
        self.assertEqual(["com.vishnu.framework.sequencer"], manifest["testables"])
        self.assertNotIn("com.unity.xr.interaction.toolkit", manifest["dependencies"])

    def test_local_dependencies_are_added_as_file_references(self):
        manifest = create_host_project.build_manifest("../..", "17.3.0", "1.6.0", None, {"com.vishnu.framework.core": "../../.core"})

        self.assertEqual("file:../../.core", manifest["dependencies"]["com.vishnu.framework.core"])

    def test_the_interaction_toolkit_is_only_added_when_asked_for(self):
        manifest = create_host_project.build_manifest("../..", "17.3.0", "1.6.0", "3.0.8")

        self.assertEqual("3.0.8", manifest["dependencies"]["com.unity.xr.interaction.toolkit"])

    def test_creating_the_project_writes_the_manifest_and_the_editor_version(self):
        with tempfile.TemporaryDirectory() as folder:
            package = pathlib.Path(folder) / "package"
            project = package / ".host"
            package.mkdir()

            create_host_project.create_host_project(project, package, "6000.3.9f1", "17.3.0", "1.6.0")

            self.assertEqual("m_EditorVersion: 6000.3.9f1\n", (project / "ProjectSettings" / "ProjectVersion.txt").read_text())
            self.assertIn("file:../..", (project / "Packages" / "manifest.json").read_text())
            self.assertTrue((project / "Assets").is_dir())


class TestResultsTests(unittest.TestCase):
    def write_results(self, folder, name, total, failed):
        path = pathlib.Path(folder) / name
        path.write_text(f'<test-run total="{total}" failed="{failed}" passed="{total - failed}"></test-run>')
        return path

    def test_results_with_tests_and_no_failures_pass(self):
        with tempfile.TemporaryDirectory() as folder:
            files = [self.write_results(folder, "editmode-results.xml", 80, 0)]

            self.assertEqual([], check_test_results.find_problems(files))

    def test_a_run_with_failures_is_reported_by_name(self):
        with tempfile.TemporaryDirectory() as folder:
            files = [self.write_results(folder, "playmode-results.xml", 5, 2)]

            self.assertEqual(["playmode-results.xml: 2 of 5 tests failed"], check_test_results.find_problems(files))

    def test_a_run_where_no_tests_ran_is_reported(self):
        with tempfile.TemporaryDirectory() as folder:
            files = [self.write_results(folder, "editmode-results.xml", 0, 0)]

            self.assertEqual(["editmode-results.xml: no tests ran"], check_test_results.find_problems(files))

    def test_missing_result_files_are_reported(self):
        self.assertEqual(["No test result files were found."], check_test_results.find_problems([]))


if __name__ == "__main__":
    unittest.main()
