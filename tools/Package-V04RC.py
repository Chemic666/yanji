"""Archive the verified V0.4 RC1 without replacing evidence or historical releases.

Requires the final local code tag, a clean committed tree, and existing isolated
test evidence. Uses only Python's standard library; does not build or delete.
"""
from pathlib import Path, PurePosixPath
import hashlib
import html
import io
import json
import re
import subprocess
import xml.etree.ElementTree as ET
import zipfile

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'artifacts/v04-m4'
TAG = 'v0.4.0-rc.1'
VERSION = '0.4.0-rc.1'


def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT).decode('utf-8').strip()


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(data):
    return hashlib.sha256(data).hexdigest().upper()


def write_new(path, data):
    with path.open('xb') as out:
        out.write(data)


def dump(path, obj):
    write_new(path, (json.dumps(obj, ensure_ascii=False, indent=2) + '\n').encode())


def files(folder):
    for path in sorted(folder.rglob('*')):
        if path.is_file():
            assert not path.is_symlink() and path.resolve().is_relative_to(folder.resolve())
            yield path.relative_to(folder).as_posix(), path.read_bytes()


def main():
    code = git('rev-parse', TAG + '^{commit}')
    head = git('rev-parse', 'HEAD')
    branch = git('branch', '--show-current')
    assert branch == 'codex/v04-m4' and not git('status', '--porcelain')
    assert code == 'e0e710c0ebd209adb84f82efb22886ffa99ef22a'
    assert not git('diff', code, head, '--', 'src', 'tests', 'resources', 'Directory.Build.props')
    assert '<Version>0.4.0-rc.1</Version>' in (ROOT / 'Directory.Build.props').read_text()
    for row in read(ART / 'baseline.json')['M0']:
        assert sha((ROOT / 'docs' / row['Path']).read_bytes()) == row['Sha256']
        git('ls-files', '--error-unmatch', 'docs/' + row['Path'])
    accepted = ART / 'window-accepted'
    result = read(accepted / 'result.json')
    checks = read(accepted / 'checks.json')
    samples = read(accepted / 'timer-samples.json')
    assert result['Status'] == 'PASS' and result['Checks'] == len(checks) == 37
    assert all(c['Status'] == 'PASS' for c in checks)
    assert len(samples) == 9 and all(s['Main'] == s['Mini'] for s in samples)
    prov = read(accepted / 'provenance.json')
    assert prov['Assembly'] == VERSION + '+' + code
    assert prov['BinarySha256'] == sha((ART / 'publish/YanJi.dll').read_bytes())
    ns = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
    counts = ET.parse(ART / 'core-final/core-final.trx').find('.//t:Counters', ns).attrib
    assert (counts['executed'], counts['passed'], counts['failed']) == ('197', '197', '0')
    rec = read(ART / 'migration-reconciliation.json')
    assert len(rec) == 5 and all(r['Status'] == 'PASS' for r in rec)
    assert {r['SourceSchema'] for r in rec} == {1, 2, 3, 4}
    for name in ['default-startup', 'upgraded-startup', 'reopen-startup', 'v031-rollback-startup']:
        assert read(ART / name / 'startup.json')['Status'] == 'PASS'
    assert read(ART / 'reopen-before.json') == read(ART / 'reopen-after.json')
    assert '升级未完成' in (ART / 'failed-startup/startup-failure.txt').read_text(encoding='utf-8-sig')
    assert len(list((ART / 'publish/licenses').glob('*'))) == 7
    release = ROOT / ('releases/YanJi-v' + VERSION + '-' + code[:8])
    review = ART / 'review'
    assert not release.exists() and not review.exists(), 'Preserve existing outputs; investigate partial runs first'
    release.mkdir(parents=True)
    (review / 'screenshots').mkdir(parents=True)
    docs = ['V04-RC-DELIVERY.md', 'V04-RC-VALIDATION.md', 'V04-RC-USER-GUIDE.md',
            'V04-RC-UPGRADE.md', 'V04-RC-LICENSES.md', 'V04-M4-DESIGN.md',
            'V04-M4-PLAN.md', 'V04-M2-TEMPLATE-SOURCES.md']
    for name in docs:
        write_new(review / name, (ROOT / 'docs' / name).read_bytes())
    for name in ['checks.json', 'provenance.json', 'result.json', 'timer-samples.json',
                 'screenshots.json', 'ui-performance.json']:
        write_new(review / name, (accepted / name).read_bytes())
    for name in ['independent-review.md', 'baseline.json', 'migration-reconciliation.json',
                 'dependency-license-audit.json', 'template-audit.json']:
        write_new(review / name, (ART / name).read_bytes())
    images = read(accepted / 'screenshots.json')
    assert len(images) == 7
    hashes = []
    for row in images:
        data = (accepted / row['File']).read_bytes()
        assert row['Sha256'] == sha(data) and row['Bytes'] == len(data)
        assert data[:8] == b'\x89PNG\r\n\x1a\n'
        assert int.from_bytes(data[16:20], 'big') == row['PixelWidth']
        assert int.from_bytes(data[20:24], 'big') == row['PixelHeight']
        write_new(review / 'screenshots' / row['File'], data)
        hashes.append({'File': 'screenshots/' + row['File'], 'Bytes': len(data), 'Sha256': sha(data)})
    dump(review / 'image-hashes.json', hashes)
    page = ['<!doctype html><html lang="zh-CN"><meta charset="utf-8">',
            '<meta name="viewport" content="width=device-width"><title>YanJi V0.4 RC1 审核</title>',
            '<style>body{max-width:1280px;margin:32px auto;padding:0 24px;background:#f6f4ee;color:#29352e;font:16px/1.7 system-ui}img{max-width:100%;border:1px solid #d9ded3}figure{margin:28px 0}code{word-break:break-all}a{color:#456653}</style>',
            '<h1>研迹 V0.4 RC1 · READY FOR RELEASE REVIEW</h1>',
            '<p>本页只索引真实Windows HWND截图，不是网页产品效果。实际200%DPI；所有数据均为隔离合成数据。未执行真实用户升级、公开发布或电源操作。</p>',
            f'<p>代码 <code>{code}</code>；文档 <code>{head}</code>。197项核心回归、37项原生WPF检查、9次运行中计时采样。旧格式回退已由实际V0.3.1打开。</p>',
            '<p><a href="V04-RC-DELIVERY.md">交付说明</a> · <a href="V04-RC-VALIDATION.md">验证与NOT TESTED</a> · <a href="V04-RC-UPGRADE.md">升级与回退</a> · <a href="image-hashes.json">图片哈希</a></p>',
            '<p>原始文件package-conflict-dark-900.png名称与尺寸不符，实际为1200×800；下方按真实元数据展示，原文件不改写。</p>']
    for row in images:
        path = 'screenshots/' + row['File']
        caption = f'{row["File"]} · {row["Width"]}×{row["Height"]} DIP · {row["PixelWidth"]}×{row["PixelHeight"]} px · ' + ('深色' if row['Dark'] else '浅色')
        page.append(f'<figure><figcaption>{html.escape(caption)}</figcaption><img src="{html.escape(path)}" alt="{html.escape(caption)}" loading="lazy"></figure>')
    page.append('</html>')
    write_new(review / 'comparison.html', '\n'.join(page).encode())
    archives = []

    def pack(name, items):
        path = release / name
        manifest = {}
        with zipfile.ZipFile(path, 'x', zipfile.ZIP_DEFLATED, compresslevel=6) as z:
            for key, data in items:
                assert key not in manifest and not key.startswith('/') and '..' not in PurePosixPath(key).parts
                assert key != 'manifest-sha256.json'
                manifest[key] = {'Bytes': len(data), 'Sha256': sha(data)}
                z.writestr(key, data)
            z.writestr('manifest-sha256.json', json.dumps({'CodeHead': code, 'DocumentationHead': head, 'Files': manifest}, ensure_ascii=False, indent=2).encode())
        with zipfile.ZipFile(path) as z:
            assert z.testzip() is None
            closed = json.loads(z.read('manifest-sha256.json'))
            assert set(z.namelist()) == set(closed['Files']) | {'manifest-sha256.json'}
            assert len(z.namelist()) == len(set(z.namelist()))
            for key, row in closed['Files'].items():
                data = z.read(key)
                assert row['Bytes'] == len(data) and row['Sha256'] == sha(data)
            if name.endswith('Review.zip'):
                for row in json.loads(z.read('image-hashes.json')):
                    data = z.read(row['File'])
                    assert row['Bytes'] == len(data) and row['Sha256'] == sha(data)
                for row in json.loads(z.read('screenshots.json')):
                    data = z.read('screenshots/' + row['File'])
                    assert row['Bytes'] == len(data) and row['Sha256'] == sha(data)
                for link in re.findall(r'(?:src|href)="([^"]+)"', z.read('comparison.html').decode()):
                    assert link in z.namelist(), 'Missing HTML dependency: ' + link
        archives.append({'File': name, 'Bytes': path.stat().st_size, 'Sha256': sha(path.read_bytes()),
                         'FilesVerified': len(manifest), 'ZipCRC': 'PASS', 'EachEntrySHA256': 'PASS'})

    def runtime_entries():
        for key, data in files(ART / 'publish'):
            if PurePosixPath(key).parts[0] == 'data-rc-v04':
                continue  # Own startup probe data stays on disk; never ships.
            assert not key.endswith(('.sqlite', '.db', '.yanjibak', '-wal', '-shm'))
            assert not PurePosixPath(key).name.startswith(('Launch-M1', 'Launch-M2', 'Launch-M3'))
            yield key, data
        for name in ['V04-RC-USER-GUIDE.md', 'V04-RC-UPGRADE.md', 'V04-RC-VALIDATION.md', 'V04-RC-LICENSES.md']:
            yield name, (ROOT / 'docs' / name).read_bytes()
        yield 'THIRD-PARTY-NOTICES.md', (ROOT / 'THIRD-PARTY-NOTICES.md').read_bytes()
        yield '开始使用.txt', '研迹V0.4 RC1：完整解压到可写目录，双击YanJi.exe。数据在旁置data-rc-v04，不覆盖旧版。请先阅读V04-RC-USER-GUIDE.md及V04-RC-UPGRADE.md。尚未授权升级真实用户数据或公开发布。\n'.encode()

    pack('YanJi-v' + VERSION + '-windows-x64.zip', runtime_entries())
    source = io.BytesIO(subprocess.check_output(['git', 'archive', '--format=zip', head], cwd=ROOT))

    def source_entries():
        with zipfile.ZipFile(source) as z:
            for key in z.namelist():
                if key.endswith('/'):
                    continue
                assert not key.endswith(('.sqlite', '.db', '.yanjibak', '.dll', '.exe', '.pdb'))
                assert '/bin/' not in key and '/obj/' not in key
                data = z.read(key)
                assert len(data) < 10 * 1024 * 1024
                yield key, data

    pack('YanJi-v' + VERSION + '-source.zip', source_entries())
    pack('YanJi-V0.4-RC1-Review.zip', files(review))

    def evidence_entries():
        for key, data in files(ART):
            if PurePosixPath(key).parts[0] not in ('publish', 'review'):
                yield key, data

    pack('YanJi-V0.4-RC1-Evidence.zip', evidence_entries())
    dump(release / 'release-manifest.json', {'Status': 'READY FOR RELEASE REVIEW', 'Version': VERSION,
         'CodeHead': code, 'DocumentationHead': head, 'Branch': branch, 'RecoveryTag': TAG,
         'Assembly': prov['Assembly'], 'BinarySha256': prov['BinarySha256'], 'Archives': archives,
         'CoreTests': {'Passed': 197, 'Failed': 0}, 'NativeWpf': result, 'M0Preserved': True,
         'RealUserDataAccessed': False, 'NoPublicRelease': True,
         'ManifestSelfHash': 'Entry manifest excludes itself; outer ZIP SHA256 covers the entire archive.'})
    dump(release / 'space-summary.json', {
        'ArtifactsBytes': sum(p.stat().st_size for p in ART.rglob('*') if p.is_file()),
        'ReleaseBytesBeforeSpaceSummary': sum(p.stat().st_size for p in release.rglob('*') if p.is_file()),
        'ActiveSelfContainedPublishCopies': 1, 'HistoricalMaterialsDeleted': 0,
        'RuntimeZipExcludesDataDirectory': True})
    print(json.dumps({'Release': str(release), 'Archives': archives}, ensure_ascii=False, indent=2))


if __name__ == '__main__':
    main()
