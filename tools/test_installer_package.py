"""Packaging input safety; entirely synthetic archives, never user data."""
import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
import zipfile

SCRIPT = Path(__file__).with_name('Build-Installer.py')


class InstallerInputTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.temp_root = SCRIPT.parents[1] / 'artifacts/installer-package-tests'
        cls.temp_root.mkdir(parents=True, exist_ok=True)
        cls.builder = None
        if SCRIPT.exists():
            spec = importlib.util.spec_from_file_location('installer_builder', SCRIPT)
            cls.builder = importlib.util.module_from_spec(spec)
            spec.loader.exec_module(cls.builder)

    def builder_available(self):
        self.assertIsNotNone(self.builder, 'Installer builder has not been implemented')
        return self.builder

    def test_unsafe_windows_names_rejected(self):
        builder = self.builder_available()
        for name in ('../escape', '/absolute', 'C:/escape', 'a\\escape', 'a:b',
                     'a/../escape', 'a/./b', 'a//b', 'NUL.txt', 'a/COM1',
                     'a/file.', 'a/file ', 'a/CONIN$', 'a/COM¹.txt', 'a/\x01file'):
            with self.subTest(name=name), self.assertRaises(ValueError):
                builder.validate_name(name)

    def test_normal_nested_payload_accepted(self):
        builder = self.builder_available()
        self.assertEqual(builder.validate_name('licenses/example/LICENSE.txt'),
                         'licenses/example/LICENSE.txt')

    def test_data_and_debug_files_rejected(self):
        builder = self.builder_available()
        for name in ('yanji.sqlite', 'backups/user.yanjibak', 'test.db-wal',
                     'YanJi.pdb', 'data-rc/file.txt', '.git/config', 'diagnostic.log'):
            with self.subTest(name=name), self.assertRaises(ValueError):
                builder.validate_payload_name(name)

    def make_archive(self, root, names, corrupt=False):
        path = root / 'synthetic.zip'
        manifest = {'Files': [{'File': name, 'Bytes': len(data),
                     'Sha256': hashlib.sha256(data).hexdigest()} for name, data in names]}
        if corrupt:
            manifest['Files'][0]['Sha256'] = '0' * 64
        with zipfile.ZipFile(path, 'w') as archive:
            for name, data in names:
                archive.writestr(name, data)
            archive.writestr('manifest-sha256.json', json.dumps(manifest))
        return path

    def test_case_collision_rejected_before_extract(self):
        builder = self.builder_available()
        with tempfile.TemporaryDirectory(dir=self.temp_root) as tmp:
            path = self.make_archive(Path(tmp), [('A.txt', b'a'), ('a.txt', b'b')])
            with self.assertRaises(ValueError):
                builder.read_verified_archive(path)

    def test_manifest_corruption_rejected(self):
        builder = self.builder_available()
        with tempfile.TemporaryDirectory(dir=self.temp_root) as tmp:
            path = self.make_archive(Path(tmp), [('file.txt', b'a')], corrupt=True)
            with self.assertRaises(ValueError):
                builder.read_verified_archive(path)

    def test_unmanifested_entry_rejected(self):
        builder = self.builder_available()
        with tempfile.TemporaryDirectory(dir=self.temp_root) as tmp:
            path = self.make_archive(Path(tmp), [('file.txt', b'a')])
            with zipfile.ZipFile(path, 'a') as archive:
                archive.writestr('extra.txt', b'b')
            with self.assertRaises(ValueError):
                builder.read_verified_archive(path)

    def test_valid_manifest_and_bytes_accepted(self):
        builder = self.builder_available()
        with tempfile.TemporaryDirectory(dir=self.temp_root) as tmp:
            path = self.make_archive(Path(tmp), [('file.txt', b'a')])
            self.assertEqual(builder.read_verified_archive(path)['file.txt'], b'a')


if __name__ == '__main__':
    unittest.main()
