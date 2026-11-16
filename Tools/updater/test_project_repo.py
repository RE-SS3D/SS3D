#!/usr/bin/env python3
"""Every copy of the project repository (see set_project_repo.py) names the same repository."""
import importlib.util
import shutil
import tempfile
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("set_project_repo", HERE / "set_project_repo.py")
repo = importlib.util.module_from_spec(spec)
spec.loader.exec_module(repo)


class ProjectRepoTests(unittest.TestCase):
    def test_every_copy_names_the_same_repository(self):
        found = repo.read_all()
        self.assertEqual(len(set(found.values())), 1, f"copies disagree: {found}")

    def test_switch_rewrites_every_copy_and_nothing_else(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            for name, _ in repo.COPIES:
                (root / name).parent.mkdir(parents=True, exist_ok=True)
                shutil.copyfile(repo.ROOT / name, root / name)
            original, repo.ROOT = repo.ROOT, root
            try:
                before = {name: (root / name).read_bytes() for name, _ in repo.COPIES}
                old = next(iter(repo.read_all().values()))
                repo.write_all("Example-Org/Example.Repo")
                self.assertEqual(set(repo.read_all().values()), {"Example-Org/Example.Repo"})
                for name, _ in repo.COPIES:
                    restored = (root / name).read_bytes().replace(b"Example-Org/Example.Repo", old.encode())
                    self.assertEqual(restored, before[name], f"{name} changed beyond the repository name")
            finally:
                repo.ROOT = original


if __name__ == "__main__":
    unittest.main()
