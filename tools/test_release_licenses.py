"""Risk-focused gate regression tests; synthetic ZIPs only, no application data."""
from pathlib import Path
import importlib.util
import json
import tempfile
import unittest
import zipfile

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('license_gate', ROOT / 'tools/Check-ReleaseLicenses.py')
gate = importlib.util.module_from_spec(spec)
spec.loader.exec_module(gate)


class LicenseGateTests(unittest.TestCase):
    def fixture(self):
        inv = json.loads((ROOT / 'resources/legal/third-party-licenses.json').read_text(encoding='utf-8'))
        names = {'LICENSE', 'NOTICE', 'COPYRIGHT.md', 'THIRD-PARTY-NOTICES.md', 'README.md',
                 'resources/legal/third-party-licenses.json'}
        names.update(f['SourcePath'] for f in inv['LicenseFiles'])
        return {name: (ROOT / name).read_bytes() for name in names}

    def check_zip(self, entries, omitted_manifest=None):
        temp_root = ROOT / 'artifacts/v04-rc-final/license-fixtures'
        temp_root.mkdir(parents=True, exist_ok=True)
        with tempfile.TemporaryDirectory(prefix='case-', dir=temp_root) as tmp:
            p = Path(tmp) / 'source.zip'
            with zipfile.ZipFile(p, 'x') as z:
                for key, data in entries.items():
                    z.writestr(key, data)
                listed = {k: {'Sha256': gate.digest(v), 'Bytes': len(v)} for k, v in entries.items() if k != omitted_manifest}
                z.writestr('manifest-sha256.json', json.dumps({'Files': listed}))
            return gate.check(p, 'source', require_manifest=True)

    def test_current_source_and_closed_zip(self):
        self.assertEqual([], gate.check(ROOT, 'source'))
        self.assertEqual([], self.check_zip(self.fixture()))

    def test_missing_project_license_and_notice(self):
        for name in ['LICENSE', 'NOTICE']:
            with self.subTest(name=name):
                entries = self.fixture(); entries.pop(name)
                self.assertTrue(any(name in e for e in self.check_zip(entries)))

    def test_both_nested_mit_notices_are_mandatory(self):
        for name in gate.NESTED_MIT:
            with self.subTest(name=name):
                entries = self.fixture(); entries.pop(name)
                self.assertTrue(any(name in e for e in self.check_zip(entries)))

    def test_tampered_license_is_rejected(self):
        entries = self.fixture(); entries['LICENSE'] += b'\nAltered legal terms'
        self.assertTrue(any('hash differs' in e for e in self.check_zip(entries)))

    def test_upstream_notice_must_not_be_replaced_by_generic_notice(self):
        entries = self.fixture(); entries['NOTICE'] = b'Generic project NOTICE'
        self.assertTrue(any('verbatim' in e for e in self.check_zip(entries)))

    def test_sha_manifest_must_list_required_license(self):
        self.assertTrue(any('manifest omits' in e for e in self.check_zip(self.fixture(), 'LICENSE')))

    def test_scope_cannot_blanket_relicense_everything(self):
        entries = self.fixture(); key = 'resources/legal/third-party-licenses.json'
        inv = json.loads(entries[key]); inv['ProjectLicenseScope'] = 'all-files'
        entries[key] = json.dumps(inv).encode()
        self.assertTrue(any('original-code' in e for e in self.check_zip(entries)))

    def test_component_license_requires_an_explicit_string(self):
        entries = self.fixture(); key = 'resources/legal/third-party-licenses.json'
        inv = json.loads(entries[key]); inv['Components'][0]['License'] = {'Id': 'MIT'}
        entries[key] = json.dumps(inv).encode()
        self.assertTrue(any('license/source/version' in e for e in self.check_zip(entries)))


if __name__ == '__main__':
    unittest.main(verbosity=2)
