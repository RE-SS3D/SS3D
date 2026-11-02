"""Run with python3 -B Tools/updater/test_update_linux.py. No network, game or real install."""

import contextlib
import copy
import fcntl
import importlib.util
import io
import json
from pathlib import Path
import shutil
import stat
import tempfile
import unittest
from unittest import mock
import zipfile

spec = importlib.util.spec_from_file_location(
    "updater", Path(__file__).resolve().parents[2] / "Builds/update_ss3d.py")
updater = importlib.util.module_from_spec(spec)
spec.loader.exec_module(updater)


class UpdaterTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="ss3d-linux-update-tests-")
        self.addCleanup(self.temp.cleanup)
        self.base = Path(self.temp.name)
        self.root = self.base / "installation with spaces"
        self.new = self.base / "payload/SS3D-Linux-Client-v0.3.0"
        for name in ("Game/SS3D-Client", "Game/UnityPlayer.so", "Game/SS3D-Client_Data/globalgamemanagers",
                     "Game/Config/permissions.txt", "Game/Config/network.json", "README.txt", "TESTERS.md",
                     "update_ss3d.py",
                     "Game/Data/StationTemplates/ministation.json", "Game/Data/StationTemplates/edited.json"):
            self.write(self.new, name, "new release")
        self.write(self.new, "Game/UnityCrashHandler64", "native helper")
        (self.new / "Game/UnityCrashHandler64").chmod(0o755)
        self.archive = self.base / "release.zip"
        with zipfile.ZipFile(self.archive, "w") as archive:
            for path in self.new.rglob("*"):
                if path.is_file():
                    archive.write(path, path.relative_to(self.new.parent))
        # The installed copy predates the main menu and still has the retired launch scripts.
        for name in ("Game/SS3D-Client", "start_ss3d_host.sh", "start_ss3d_client.sh", "README.txt", "TESTERS.md",
                     "update_ss3d.py", "update_ss3d.sh", "Game/Config/network.json", "Game/Data/StationTemplates/ministation.json",
                     "Game/Data/StationTemplates/edited.json", "Game/Data/StationTemplates/my station.json",
                     "Game/Data/Exports/map.zip", "Game/Data/ServerMeta/permissions.json", "Game/Logs/player.log"):
            self.write(self.root, name, "local data")
        self.metadata = {"tag": "v0.2.0", "defaults": {
            "Game/Data/StationTemplates/ministation.json": updater.sha256(
                self.root / "Game/Data/StationTemplates/ministation.json"),
            "Game/Data/StationTemplates/edited.json": "original hash"}}
        self.write(self.root, ".ss3d-release.json", json.dumps(self.metadata))
        self.release = {"tag_name": "v0.3.0", "draft": False, "prerelease": False, "assets": [{
            "name": "SS3D-Linux-Client-v0.3.0.zip", "digest": "sha256:" + updater.sha256(self.archive),
            "browser_download_url": "https://github.com/henkhooft/SS3D/releases/download/v0.3.0/SS3D-Linux-Client-v0.3.0.zip"}]}
        self.latest = self.patch("latest_release", return_value=self.release)
        self.closed = self.patch("assert_closed")
        self.download = self.patch("download", side_effect=lambda url, dest: shutil.copyfile(self.archive, dest))

    def patch(self, name, **kwargs):
        patcher = mock.patch.object(updater, name, **kwargs)
        result = patcher.start()
        self.addCleanup(patcher.stop)
        return result

    @staticmethod
    def write(root, name, value):
        path = root / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(value)

    def update(self, **kwargs):
        with contextlib.redirect_stdout(io.StringIO()):
            updater.update(self.root, **kwargs)

    def assert_original(self):
        self.assertEqual((self.root / "Game/SS3D-Client").read_text(), "local data")
        self.assertEqual(updater.metadata(self.root)["tag"], "v0.2.0")

    def test_install_preserves_data_refreshes_defaults_and_keeps_runnable_backup(self):
        self.update()
        self.assertEqual((self.root / "Game/SS3D-Client").read_text(), "new release")
        self.assertEqual((self.root / "Game/Data/StationTemplates/ministation.json").read_text(), "new release")
        for name in ("Game/Data/StationTemplates/edited.json", "Game/Data/StationTemplates/my station.json",
                     "Game/Data/Exports/map.zip", "Game/Data/ServerMeta/permissions.json",
                     "Game/Logs/player.log", "Game/Config/network.json"):
            self.assertEqual((self.root / name).read_text(), "local data", name)
        self.assertEqual(updater.metadata(self.root)["tag"], "v0.3.0")
        for name in ("Game/SS3D-Client", "Game/UnityCrashHandler64"):
            self.assertTrue((self.root / name).stat().st_mode & stat.S_IXUSR, name)
        for name in updater.RETIRED:
            self.assertFalse((self.root / name).exists(), name)
        backups = list((self.root / ".ss3d-backups").iterdir())
        self.assertEqual(len(backups), 1)
        for name in ("Game/SS3D-Client", "start_ss3d_host.sh", "start_ss3d_client.sh", "update_ss3d.py"):
            self.assertEqual((backups[0] / name).read_text(), "local data")

    def test_updater_replaces_itself_but_keeps_shell_launcher(self):
        self.update()
        self.assertEqual((self.root / "update_ss3d.py").read_text(), "new release")
        self.assertEqual((self.root / "update_ss3d.sh").read_text(), "local data")

    def test_only_the_previous_installation_is_kept_as_backup(self):
        for old in ("20260101-000000-aaaaaaaa", "20260201-000000-bbbbbbbb"):
            self.write(self.root / ".ss3d-backups" / old, "Game/SS3D-Client", "older install")
        self.update()
        backups = list((self.root / ".ss3d-backups").iterdir())
        self.assertEqual(len(backups), 1)
        self.assertEqual((backups[0] / "Game/SS3D-Client").read_text(), "local data")

    def test_check_only_and_same_or_newer_version_do_not_download(self):
        self.update(check_only=True)
        self.assert_original()
        for tag in ("v0.3.0", "v9.0.0"):
            self.metadata["tag"] = tag
            self.write(self.root, ".ss3d-release.json", json.dumps(self.metadata))
            self.update()
        self.download.assert_not_called()

    def test_old_download_without_metadata_preserves_map_collisions(self):
        (self.root / ".ss3d-release.json").unlink()
        self.update()
        self.assertEqual((self.root / "Game/Data/StationTemplates/ministation.json").read_text(), "local data")

    def test_network_or_hash_failure_keeps_original(self):
        self.download.side_effect = OSError("Network unavailable")
        with self.assertRaisesRegex(OSError, "Network unavailable"):
            self.update()
        self.assert_original()
        self.download.side_effect = lambda url, dest: shutil.copyfile(self.archive, dest)
        self.release["assets"][0]["digest"] = "sha256:" + "0" * 64
        with self.assertRaisesRegex(RuntimeError, "SHA-256 mismatch"):
            self.update()
        self.assert_original()

    def test_missing_linux_asset_digest_or_wrong_url_keeps_original(self):
        original = copy.deepcopy(self.release["assets"])
        for asset in ([], [{**original[0], "digest": None}],
                      [{**original[0], "name": "SS3D-Linux-Server-v0.3.0.zip"}],
                      [{**original[0], "browser_download_url": "https://example.invalid/other.zip"}]):
            with self.subTest(asset=asset):
                self.release["assets"] = asset
                with self.assertRaises(RuntimeError):
                    self.update()
                self.assert_original()
        self.download.assert_not_called()

    def test_process_present_or_started_during_download_prevents_swap(self):
        for effects in ([RuntimeError("Close all SS3D")], [None, RuntimeError("Close all SS3D")]):
            self.closed.side_effect = effects
            with self.assertRaisesRegex(RuntimeError, "Close all SS3D"):
                self.update()
            self.assert_original()

    def test_concurrent_updater_is_refused(self):
        with (self.root / ".ss3d-update.lock").open("a") as lock:
            fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
            with self.assertRaisesRegex(RuntimeError, "Another updater"):
                self.update()
        self.download.assert_not_called()

    def test_failure_during_swap_restores_original_and_launchers(self):
        rename = Path.rename
        failed = False

        def fail_once(source, target):
            nonlocal failed
            if not failed and "extract" in source.parts and source.name == "README.txt":
                failed = True
                raise OSError("Simulated disk failure")
            return rename(source, target)

        with mock.patch.object(Path, "rename", fail_once):
            with self.assertRaisesRegex(OSError, "Simulated disk failure"):
                self.update()
        self.assert_original()
        self.assertEqual((self.root / "start_ss3d_host.sh").read_text(), "local data")
        self.assertTrue(list(self.root.glob(".ss3d-update-*")))  # Retain recovery payload.

    def test_symlink_in_local_data_is_refused(self):
        (self.root / "Game/Data/outside").symlink_to(self.new, target_is_directory=True)
        with self.assertRaisesRegex(RuntimeError, "Remove links"):
            self.update()
        self.assert_original()

    def test_unsafe_symlink_or_incomplete_archive_is_refused(self):
        for name, mode in (("../../escape", 0), ("/absolute", 0),
                           ("SS3D-Linux-Client-v0.3.0/Game/../../escape", 0),
                           ("SS3D-Linux-Client-v0.3.0/Game/link", stat.S_IFLNK | 0o777),
                           ("SS3D-Linux-Client-v0.3.0/Game/missing-other-files", 0)):
            with self.subTest(name=name):
                archive_path = self.base / "unsafe.zip"
                with zipfile.ZipFile(archive_path, "w") as archive:
                    entry = zipfile.ZipInfo(name)
                    entry.external_attr = mode << 16
                    archive.writestr(entry, "payload")
                with tempfile.TemporaryDirectory(dir=self.base) as dest:
                    with self.assertRaisesRegex(RuntimeError, "Unsafe|Incomplete"):
                        updater.expand_release(archive_path, Path(dest), "v0.3.0")

    def test_wait_for_exit_returns_once_the_process_is_gone_and_times_out_otherwise(self):
        with mock.patch.object(updater.Path, "exists", side_effect=[True, True, False]), \
                mock.patch.object(updater.time, "sleep") as sleep:
            updater.wait_for_exit(4242)
        self.assertEqual(sleep.call_count, 2)
        with mock.patch.object(updater.Path, "exists", return_value=True), \
                mock.patch.object(updater.time, "sleep"), \
                mock.patch.object(updater.time, "monotonic", side_effect=[0.0, 0.0, 61.0]):
            with self.assertRaisesRegex(RuntimeError, "did not close"):
                updater.wait_for_exit(4242)

    def test_restart_relaunches_the_client_without_arguments_even_after_a_failed_update(self):
        self.latest.side_effect = OSError("offline")
        with mock.patch.object(updater.subprocess, "Popen") as popen, \
                mock.patch.object(updater, "wait_for_exit") as wait, \
                mock.patch.object(updater, "__file__", str(self.root / "update_ss3d.py")), \
                mock.patch.object(updater.sys, "argv", ["update_ss3d.py", "--wait-pid", "77", "--restart"]), \
                contextlib.redirect_stderr(io.StringIO()):
            self.assertEqual(updater.main(), 1)
        wait.assert_called_once_with(77)
        args, kwargs = popen.call_args
        self.assertEqual(args[0], [str(self.root.resolve() / "Game/SS3D-Client")])
        self.assertEqual(kwargs["cwd"], self.root.resolve() / "Game")
        self.assertTrue(kwargs["start_new_session"])

    def test_without_restart_the_updater_never_launches_the_game(self):
        self.latest.side_effect = OSError("offline")
        with mock.patch.object(updater.subprocess, "Popen") as popen, \
                mock.patch.object(updater, "__file__", str(self.root / "update_ss3d.py")), \
                mock.patch.object(updater.sys, "argv", ["update_ss3d.py"]), \
                contextlib.redirect_stderr(io.StringIO()):
            self.assertEqual(updater.main(), 1)
        popen.assert_not_called()


class ReleaseSelectionTests(unittest.TestCase):
    def test_process_detection_reads_names_without_signalling_processes(self):
        with tempfile.TemporaryDirectory(prefix="ss3d-process-fixture-") as folder:
            comm = Path(folder) / "comm"
            with mock.patch.object(updater.Path, "glob", return_value=[comm]):
                for name in ("SS3D-Client", "SS3D.x86_64"):
                    comm.write_text(name + "\n")
                    with self.assertRaisesRegex(RuntimeError, "Close all SS3D"):
                        updater.assert_closed()
                comm.write_text("unrelated-process\n")
                updater.assert_closed()

    def test_pagination_stable_only_numeric_sort_and_promotion(self):
        stable = {"draft": False, "prerelease": False, "tag_name": "v0.9.0"}
        candidate = {"draft": False, "prerelease": True, "tag_name": "v0.10.0"}
        page = [dict(stable, tag_name="develop-nightly") for _ in range(100)]
        rest = [stable, candidate, dict(stable, draft=True, tag_name="v9.0.0")]
        responses = [io.BytesIO(json.dumps(p).encode()) for p in (page, rest)]
        with mock.patch.object(updater.urllib.request, "urlopen", side_effect=responses) as get:
            self.assertEqual(updater.latest_release()["tag_name"], "v0.9.0")
            self.assertIn("page=2", get.call_args.args[0].full_url)
        candidate["prerelease"] = False
        with mock.patch.object(updater.urllib.request, "urlopen", return_value=io.BytesIO(json.dumps(rest).encode())):
            self.assertEqual(updater.latest_release()["tag_name"], "v0.10.0")
        candidate["prerelease"] = True
        with mock.patch.object(updater.urllib.request, "urlopen", return_value=io.BytesIO(json.dumps([candidate]).encode())):
            with self.assertRaisesRegex(RuntimeError, "No stable numbered"):
                updater.latest_release()


if __name__ == "__main__":
    unittest.main()
