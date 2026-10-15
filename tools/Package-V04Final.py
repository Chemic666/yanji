"""Two-step RC-Final packaging. Local-only; preserves all previous outputs.

windows: seal a committed self-contained staging directory and actually extract
its verified ZIP back into the same owned, data-free directory (one runtime copy).
final: after isolated acceptance and a documentation commit, archive source and
review evidence. No build, cleanup, Git mutation or network access is performed.
"""
from pathlib import Path, PurePosixPath
import argparse
import hashlib
import html
import importlib.util
import io
import json
import subprocess
import struct
import zipfile

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'artifacts/v04-rc-final'
VERSION = '0.4.0-rc.final.1'
TAG = 'v' + VERSION
LICENSE_EOL_FIXES = {
    'resources/licenses/CommunityToolkit-MIT.md',
    'resources/licenses/CommunityToolkit-NOTICES.txt',
    'resources/licenses/DotNet-Runtime-MIT.txt',
    'resources/licenses/DotNet-ThirdPartyNotices.txt',
    'resources/licenses/WindowsDesktop-MIT.txt',
}
spec = importlib.util.spec_from_file_location('licenses', ROOT / 'tools/Check-ReleaseLicenses.py')
licenses = importlib.util.module_from_spec(spec)
spec.loader.exec_module(licenses)


def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT).decode('utf-8').strip()


def read(p):
    return json.loads(p.read_text(encoding='utf-8-sig'))


def sha(data):
    return hashlib.sha256(data).hexdigest().upper()


def new(p, data):
    with p.open('xb') as f:
        f.write(data)


def dump(p, value):
    new(p, (json.dumps(value, ensure_ascii=False, indent=2) + '\n').encode())


def files(folder):
    for p in sorted(folder.rglob('*')):
        if p.is_file():
            assert not p.is_symlink() and p.resolve().is_relative_to(folder.resolve())
            yield p.relative_to(folder).as_posix(), p.read_bytes()


def pack(path, entries, code, docs, license_mode=None):
    index = {}
    with zipfile.ZipFile(path, 'x', zipfile.ZIP_DEFLATED, compresslevel=6) as z:
        for key, data in entries:
            assert not key.startswith('/') and '..' not in PurePosixPath(key).parts and '\\' not in key and ':' not in key
            assert key not in index and key != 'manifest-sha256.json'
            index[key] = {'Bytes': len(data), 'Sha256': sha(data)}
            z.writestr(key, data)
        assert len({s.casefold() for s in index}) == len(index)
        z.writestr('manifest-sha256.json', json.dumps({'CodeHead': code, 'DocumentationHead': docs, 'Files': index}, ensure_ascii=False, indent=2).encode())
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None
        closed = json.loads(z.read('manifest-sha256.json'))
        assert set(z.namelist()) == set(closed['Files']) | {'manifest-sha256.json'}
        for key, row in closed['Files'].items():
            data = z.read(key)
            assert row['Bytes'] == len(data) and row['Sha256'] == sha(data)
    if license_mode:
        assert not licenses.check(path, license_mode, require_manifest=True)
    return {'File': path.name, 'Bytes': path.stat().st_size, 'Sha256': sha(path.read_bytes()),
            'Entries': len(index), 'ZipCRC': 'PASS', 'AllEntrySha256': 'PASS',
            'LicenseGate': 'PASS' if license_mode else 'not applicable'}


def windows():
    assert not git('status', '--porcelain'), 'Commit reviewed changes before freezing a binary'
    assert git('branch', '--show-current') == 'codex/v04-rc-final'
    code = git('rev-parse', 'HEAD')
    stage = ART / 'publish'
    assert stage.is_dir() and not licenses.check(stage, 'binary')
    entries = list(files(stage))
    assert not any(k.endswith(('.db', '.sqlite', '.yanjibak', '-wal', '-shm')) or k.startswith('data-') for k, _ in entries)
    release = ROOT / ('releases/YanJi-v' + VERSION + '-' + code[:8])
    release.mkdir(exist_ok=False)
    info = {'Version': VERSION, 'CodeHead': code, 'RecoveryTag': TAG,
            'ExpectedAssembly': VERSION + '+' + code, 'UserDataIncluded': False,
            'DllSha256': sha((stage / 'YanJi.dll').read_bytes())}
    zip_path = release / ('YanJi-v' + VERSION + '-windows-x64.zip')
    archive = pack(zip_path, entries + [('BUILD-INFO.json', json.dumps(info, indent=2).encode())], code, code, 'binary')
    # No extra or user-created files may be overwritten. Exact current bytes
    # must already match the sealed ZIP before the actual extraction proceeds.
    with zipfile.ZipFile(zip_path) as z:
        manifest = json.loads(z.read('manifest-sha256.json'))['Files']
        current = dict(files(stage))
        assert set(current) == set(manifest) - {'BUILD-INFO.json'}
        for key, data in current.items():
            assert sha(data) == manifest[key]['Sha256']
        for name in z.namelist():
            destination = stage.joinpath(*PurePosixPath(name).parts).resolve()
            assert destination.is_relative_to(stage.resolve())
        z.extractall(stage)
        for key, row in manifest.items():
            assert sha((stage / key).read_bytes()) == row['Sha256']
    dump(ART / 'windows-candidate.json', {'CodeHead': code, 'ReleaseDirectory': str(release), 'Archive': archive, 'BuildInfo': info})
    dump(ART / 'zip-extraction.json', {'Status': 'READY FOR FINAL APPROVAL', 'ActualZipExtraction': True,
         'Method': 'verified in-place extraction into this run own data-free staging; no historical files',
         'Directory': str(stage), 'FilesRechecked': len(manifest), 'SelfContainedCopies': 1})
    print(json.dumps({'Release': str(release), 'Archive': archive}, ensure_ascii=False, indent=2))


def final():
    assert not git('status', '--porcelain')
    candidate = read(ART / 'windows-candidate.json')
    code = candidate['CodeHead']; head = git('rev-parse', 'HEAD')
    assert git('rev-parse', TAG + '^{commit}') == code
    assert not git('diff', code, head, '--', 'src', 'resources/knowledge-packages', 'resources/legal', 'Directory.Build.props')
    accepted = read(ART / 'window-accepted/result.json')
    provenance = read(ART / 'window-accepted/provenance.json')
    assert accepted['Status'] == 'PASS' and accepted['Checks'] == 37 and accepted['Samples'] == 9
    assert provenance['Assembly'] == VERSION + '+' + code
    assert provenance['BinarySha256'] == candidate['BuildInfo']['DllSha256']
    assert read(ART / 'isolation-and-restore.json')['Status'] == 'READY FOR FINAL APPROVAL'
    # Check the original screenshot manifest, not only the new archive index.
    screenshots = read(ART / 'window-accepted/screenshots.json')
    assert len(screenshots) == 7
    assert len({r['File'].casefold() for r in screenshots}) == 7
    assert {p.name for p in (ART / 'window-accepted').glob('*.png')} == {r['File'] for r in screenshots}
    gallery = ['<!doctype html><html lang="zh-CN"><meta charset="utf-8"><title>RC-Final 真实 WPF 截图</title>',
               '<style>body{font:16px system-ui;max-width:1100px;margin:32px auto;padding:16px;background:#f5f4ef;color:#24382e}img{width:100%;height:auto}figure{margin:28px 0}figcaption{margin:10px 0}</style>',
               '<h1>RC-Final · 真实 WPF 窗口</h1><p>原生客户端截图；尺寸按截图元数据标注。不是网页模拟效果。物理输入法及其他物理 DPI 尚未验收。</p>']
    for row in screenshots:
        name = row['File']
        assert PurePosixPath(name).name == name and '\\' not in name
        data = (ART / 'window-accepted' / name).read_bytes()
        assert len(data) == row['Bytes'] and sha(data) == row['Sha256']
        assert data[:8] == b'\x89PNG\r\n\x1a\n' and data[12:16] == b'IHDR'
        assert struct.unpack('>II', data[16:24]) == (row['PixelWidth'], row['PixelHeight'])
        assert row['PixelWidth'] == row['Width'] * row['Dpi'] / 96
        assert row['PixelHeight'] == row['Height'] * row['Dpi'] / 96
        caption = f"{name} · {row['Width']}×{row['Height']} DIP · {row['PixelWidth']}×{row['PixelHeight']} px · DPI {row['Dpi']}"
        if name == 'package-conflict-dark-900.png':
            caption += '；历史文件名含900，实际窗口为1200×800，以元数据为准'
        gallery.append(f'<figure><figcaption>{html.escape(caption)}</figcaption><img src="{html.escape(name, quote=True)}" alt="{html.escape(caption, quote=True)}"></figure>')
    gallery.append('</html>')
    new(ART / 'window-accepted/index.html', '\n'.join(gallery).encode())
    dump(ART / 'screenshots-verified.json', {'Status': 'READY FOR FINAL APPROVAL', 'Files': 7,
         'OriginalManifestBytesAndSha256': True, 'PngDimensionsAndDpi': True,
         'GalleryReferences': 'window-accepted/index.html; seven relative PNG paths verified',
         'FilenameCaveat': 'package-conflict-dark-900.png actually 1200x800 DIP; original evidence preserved'})
    release = Path(candidate['ReleaseDirectory'])
    win_path = release / candidate['Archive']['File']
    assert sha(win_path.read_bytes()) == candidate['Archive']['Sha256']
    # A reviewed source-only repair preserves upstream CRLF license bytes that
    # pre-existing Git blobs had normalized to LF. No runtime input may change.
    eol_fixes = git('diff', '--name-only', code, head, '--', 'resources/licenses').splitlines()
    assert set(eol_fixes).issubset(LICENSE_EOL_FIXES)
    with zipfile.ZipFile(win_path) as z:
        for name in eol_fixes:
            original = subprocess.check_output(['git', 'show', code + ':' + name], cwd=ROOT)
            current = (ROOT / name).read_bytes()
            assert current.replace(b'\r\n', b'\n') == original
            assert current == z.read('licenses/' + PurePosixPath(name).name)
    source_bytes = io.BytesIO(subprocess.check_output(['git', 'archive', '--format=zip', head], cwd=ROOT))
    def source():
        with zipfile.ZipFile(source_bytes) as z:
            for key in z.namelist():
                if key.endswith('/'):
                    continue
                assert not key.endswith(('.exe', '.dll', '.pdb', '.sqlite', '.db', '.yanjibak')) and '/bin/' not in key and '/obj/' not in key
                data = z.read(key); assert len(data) < 10 * 1024 * 1024
                yield key, data
    archives = [candidate['Archive']]
    archives.append(pack(release / ('YanJi-v' + VERSION + '-source.zip'), source(), code, head, 'source'))
    def evidence():
        for key, data in files(ART):
            if PurePosixPath(key).parts[0] not in ('publish', 'license-fixtures'):
                yield 'evidence/' + key, data
        for name in ['V04-RCFINAL-DELIVERY.md', 'V04-RCFINAL-VALIDATION.md', 'V04-RCFINAL-PLAN.md',
                     'V04-RCFINAL-MANUAL.md', 'V04-RCFINAL-RELEASE-NOTES.md', 'V04-FINAL-LICENSE-REVIEW.md']:
            yield 'docs/' + name, (ROOT / 'docs' / name).read_bytes()
        for folder in ['docs/legal', 'resources/licenses']:
            for key, data in files(ROOT / folder):
                yield folder + '/' + key, data
        for old in ['artifacts/v04-m4/core-final/core-final.trx', 'artifacts/v04-m4/migration-reconciliation.json',
                    'artifacts/v04-final-approval/runtime-final.json', 'artifacts/v04-final-approval/soak/result.json',
                    'artifacts/v04-final-approval/soak/samples.json']:
            yield 'reused-rc1-evidence/' + old, (ROOT / old).read_bytes()
    archives.append(pack(release / 'YanJi-V0.4-RC-Final-Evidence.zip', evidence(), code, head))
    manifest = {'Status': 'READY FOR FINAL APPROVAL', 'Version': VERSION, 'Branch': git('branch', '--show-current'),
                'CodeHead': code, 'DocumentationHead': head, 'Tag': TAG, 'Assembly': provenance['Assembly'],
                'BinarySha256': provenance['BinarySha256'], 'Archives': archives,
                'PhysicalIME': 'NOT TESTED', 'CleanMachineOffline': 'NOT TESTED', 'RealUserUpgrade': 'NOT TESTED',
                'NoPublicRelease': True, 'RootProjectLicense': 'Apache-2.0; original-code scope', 'SelfContainedCopiesCreated': 1,
                'SourceOnlyLicenseByteRepairs': eol_fixes, 'CompiledSourceAndKnowledgePackagesUnchanged': True}
    dump(release / 'release-manifest.json', manifest)
    sums = '\n'.join(row['Sha256'] + '  ' + row['File'] for row in archives) + '\n'
    new(release / 'SHA256SUMS.txt', sums.encode())
    draft = (ROOT / 'docs/V04-RCFINAL-RELEASE-NOTES.md').read_bytes()
    new(release / 'RELEASE-DRAFT.md', draft + ('\n## 本地已封闭归档校验值\n\n```text\n' + sums + '```\n').encode())
    dump(release / 'space-summary.json', {'ArtifactsBytes': sum(p.stat().st_size for p in ART.rglob('*') if p.is_file()),
         'ReleaseBytes': sum(p.stat().st_size for p in release.rglob('*') if p.is_file()), 'HistoricalMaterialsDeleted': 0,
         'ActiveSelfContainedCopies': 1, 'NoCacheCleanupOrUserDataAccess': True})
    print(json.dumps(manifest, ensure_ascii=False, indent=2))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('stage', choices=['windows', 'final'])
    args = parser.parse_args()
    windows() if args.stage == 'windows' else final()
