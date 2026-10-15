"""Lightweight source/runtime license gate; no installs, network or extraction."""
from pathlib import Path, PurePosixPath
import argparse
import hashlib
import json
import zipfile

APACHE_SHA = 'CFC7749B96F63BD31C3C42B5C471BF756814053E847C10F3EB003417BC523D30'
NESTED_MIT = {
    'docs/legal/third-party/modern-screenshot-MIT.txt': '1DAAE2DB27DABA18A7E21BB56E12E19B8DE6C1197658921F09BBE550F7106579',
    'docs/legal/third-party/platform-design-skills-MIT.txt': '1126322E2CC8D165ADC4C792EEB195717DE2BCC7B39BE1CE77959D78E87EF685',
}


def digest(data):
    return hashlib.sha256(data).hexdigest().upper()


def check(target, mode, require_manifest=False):
    errors = []
    archive = zipfile.ZipFile(target) if Path(target).is_file() else None
    root = Path(target).resolve()

    def read(name):
        parts = PurePosixPath(name)
        if parts.is_absolute() or '..' in parts.parts or '\\' in name or ':' in name:
            raise ValueError('Unsafe license path: ' + name)
        if archive:
            return archive.read(name)
        path = root.joinpath(*parts.parts)
        if not path.resolve().is_relative_to(root):
            raise ValueError('License path leaves target: ' + name)
        return path.read_bytes()

    def need(name, expected=None):
        try:
            data = read(name)
            if expected and digest(data) != expected:
                errors.append('License text hash differs: ' + name)
            return data
        except (OSError, KeyError, ValueError) as e:
            errors.append('Missing/invalid required file: ' + name + ' (' + str(e) + ')')
            return b''

    try:
        required = {'LICENSE', 'NOTICE', 'COPYRIGHT.md', 'THIRD-PARTY-NOTICES.md'}
        need('LICENSE', APACHE_SHA)
        for name in sorted(required - {'LICENSE'}):
            need(name)
        key = 'resources/legal/third-party-licenses.json' if mode == 'source' else 'third-party-licenses.json'
        required.add(key)
        try:
            inventory = json.loads(need(key))
        except (ValueError, TypeError):
            errors.append('License inventory cannot be parsed')
            return errors
        if inventory.get('ProjectLicense') != 'Apache-2.0' or inventory.get('ProjectLicenseScope') != 'original-code':
            errors.append('Project license scope must be original-code only')
        components = inventory.get('Components', [])
        if not components or any(not isinstance(c.get('License'), str) or not c['License'].strip() or not c.get('Source') or not c.get('Version') for c in components):
            errors.append('Component license/source/version missing')
        for entry in inventory.get('LicenseFiles', []):
            name = entry.get('SourcePath') if mode == 'source' else entry.get('BinaryPath')
            if name:
                required.add(name)
                need(name, entry['Sha256'])
        if mode == 'source':
            for name, expected in NESTED_MIT.items():
                required.add(name)
                need(name, expected)
            upstream = need('.agents/skills/impeccable/UPSTREAM-NOTICE.md')
            if upstream.strip() not in need('NOTICE'):
                errors.append('NOTICE must preserve the upstream Impeccable notice verbatim')
            readme = need('README.md').decode('utf-8-sig')
            if '原创代码' not in readme or '用户数据' not in readme:
                errors.append('README must explain original-code scope and user data')
        else:
            for name in ['START-HERE.md', 'USER-GUIDE.md', 'UPGRADE-ROLLBACK.md', 'KNOWLEDGE-SOURCES.md']:
                required.add(name)
                need(name)
        if archive:
            names = [n for n in archive.namelist() if not n.endswith('/')]
            if len({n.casefold() for n in names}) != len(names):
                errors.append('Duplicate/case-colliding ZIP paths')
            if require_manifest or 'manifest-sha256.json' in names:
                try:
                    entries = json.loads(read('manifest-sha256.json'))['Files']
                    for name in required:
                        if name not in entries:
                            errors.append('Distribution manifest omits license/document: ' + name)
                except (KeyError, ValueError, TypeError):
                    errors.append('Distribution SHA manifest missing/invalid')
        elif require_manifest:
            errors.append('Manifest closure verification requires a ZIP')
    finally:
        if archive:
            archive.close()
    return errors


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('target', type=Path)
    parser.add_argument('--mode', choices=['source', 'binary'], required=True)
    parser.add_argument('--require-manifest', action='store_true')
    args = parser.parse_args()
    failures = check(args.target, args.mode, args.require_manifest)
    print(json.dumps({'Status': 'BLOCKED' if failures else 'READY FOR FINAL APPROVAL', 'Mode': args.mode, 'Errors': failures}, ensure_ascii=False, indent=2))
    raise SystemExit(1 if failures else 0)
