"""Build a local installer from the immutable, published Beta ZIP. No network/Git writes."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import shutil
import subprocess
import zipfile

ROOT = Path(__file__).resolve().parents[1]
BASE = 'dc1038e62b8046584d7d7dd5f48f17275ce98c44'
VERSION = '0.4.0-beta.1'
ZIP_NAME = f'YanJi-v{VERSION}-windows-x64.zip'
ZIP_SHA = 'b0bec5ba1fb85984b44c6ca726bc7492116298b6acc5e4c5fc06de5e2e2f576b'


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def validate_name(name):
    parts = name.split('/')
    reserved = {'con', 'prn', 'aux', 'nul', 'conin$', 'conout$'}
    if not name or '\\' in name or any(ord(c) < 32 or c in ':*?"<>|{};' for c in name):
        raise ValueError('Unsafe Windows archive name')
    for part in parts:
        stem = part.split('.')[0].casefold()
        if part in ('', '.', '..') or part.endswith((' ', '.')) or stem in reserved or re.fullmatch(r'(com|lpt)[1-9¹²³]', stem):
            raise ValueError('Unsafe Windows archive component')
    return name


def validate_payload_name(name):
    validate_name(name)
    lower = name.lower()
    if re.search(r'\.(db|db3|sqlite|sqlite3)(-|$)', lower) or lower.endswith(('.yanjibak', '.pdb', '.log', '.dmp', '.key', '.pfx')):
        raise ValueError('Data, debug or sensitive file in installer payload')
    if any(p.startswith(('data-', '.git')) or p in ('backups', 'testresults', 'artifacts') for p in lower.split('/')):
        raise ValueError('User/test data directory in installer payload')


def read_verified_archive(path):
    with zipfile.ZipFile(path) as archive:
        infos = archive.infolist()
        if len(infos) > 5000 or sum(i.file_size for i in infos) > 600 * 1024 * 1024:
            raise ValueError('Archive exceeds fixed Beta payload limits')
        entries = {}
        seen = set()
        for info in infos:
            if info.is_dir():
                validate_name(info.filename.rstrip('/'))
                continue
            validate_payload_name(info.filename)
            if info.filename.casefold() in seen or (info.external_attr >> 16) & 0o170000 == 0o120000:
                raise ValueError('Duplicate/case-colliding path or symlink')
            seen.add(info.filename.casefold())
            entries[info.filename] = archive.read(info)  # Includes ZIP CRC verification.
    rows = json.loads(entries['manifest-sha256.json'])['Files']
    if isinstance(rows, list):
        if len({r['File'] for r in rows}) != len(rows):
            raise ValueError('Duplicate manifest entry')
        rows = {r['File']: r for r in rows}
    if set(rows) != set(entries) - {'manifest-sha256.json'}:
        raise ValueError('Manifest does not close over the entire payload')
    for name, row in rows.items():
        if row['Bytes'] != len(entries[name]) or row['Sha256'].lower() != sha(entries[name]):
            raise ValueError('Manifest content mismatch: ' + name)
    return entries


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--archive', required=True, type=Path)
    parser.add_argument('--iscc', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path, help='New directory; never overwrite a prior build')
    args = parser.parse_args()
    compiler_license = args.iscc.resolve().parent / 'License.txt'
    if not args.iscc.is_file() or not compiler_license.is_file():
        raise ValueError('ISCC.exe and its original License.txt are required')
    if sha(args.archive.read_bytes()) != ZIP_SHA:
        raise ValueError('Only the exact published Beta runtime ZIP is accepted')
    entries = read_verified_archive(args.archive)
    before = {n: sha(b) for n, b in entries.items() if n.lower().endswith(('.exe', '.dll', '.json'))}
    args.output.mkdir(parents=True, exist_ok=False)
    output = args.output.resolve()
    payload = output / 'payload'
    payload.mkdir()
    docs = {'USER-GUIDE.md': 'USER-GUIDE.md', 'START-HERE.md': 'USER-GUIDE.md',
            'UPGRADE-ROLLBACK.md': 'UPGRADE-ROLLBACK.md', 'RELEASE-NOTES.md': 'RELEASE-V040.md',
            'RELEASE-V040.md': 'RELEASE-V040.md',
            'INSTALLATION.md': 'INSTALLATION.md'}
    for target, source in docs.items():
        entries[target] = (ROOT / 'docs' / source).read_bytes()
    entries['YanJi.ico'] = (ROOT / 'src/YanJi/Assets/Icons/YanJi.ico').read_bytes()
    entries['INSTALLER-INFO.txt'] = (
        '研迹 YanJi 0.4.0 Beta 1 · Windows 11 x64\r\n\r\n'
        '仅为当前用户安装，无需管理员权限或另装 .NET SDK/运行时。\r\n'
        '程序：%LOCALAPPDATA%\\Programs\\YanJi\r\n'
        '学习数据与备份：%LOCALAPPDATA%\\YanJi\r\n\r\n'
        '安装器不读取、迁移或修改学习数据；卸载保留数据库和备份。\r\n'
        '重装前请先结束计时并退出研迹。旧版本数据库不会自动迁移。\r\n'
        '安装完成后请从开始菜单启动，也可选择创建桌面快捷方式。\r\n\r\n'
        '应用和本安装器尚未代码签名，SmartScreen 可能警告。\r\n'
        '请确认文件来自 github.com/Chemic666/yanji 的 Release，勿关闭系统安全功能。\r\n'
        'SHA-256 为可选完整性核对，不是代码签名或安全认证。\r\n\r\n'
        '本版本为 Beta，实机输入法、其他 DPI、休眠、干净机离线等仍有未测项。\r\n'
        '原创代码 Apache-2.0；第三方和素材声明随程序保留。\r\n'
        '完整安装说明见 INSTALLATION.md，兼容限制见 RELEASE-NOTES.md。\r\n'
    ).encode('utf-8-sig')
    entries['licenses/InnoSetup-LICENSE.txt'] = compiler_license.read_bytes()
    inputs = {p.relative_to(ROOT).as_posix(): sha(p.read_bytes()) for p in
              [ROOT / 'installer/YanJi.iss', ROOT / 'installer/ChineseSimplified.isl',
               Path(__file__).resolve(), ROOT / 'README.md',
               ROOT / 'src/YanJi/Assets/Icons/YanJi.ico',
               *[ROOT / 'docs' / n for n in set(docs.values())]]}
    provenance = {'Version': VERSION, 'BaseCommit': BASE, 'RuntimeZipSha256': ZIP_SHA,
                  'ApplicationRecompiled': False, 'SourceChangesSha256': inputs,
                  'CompilerVersionEvidence': 'compiler.log (ISCC banner)',
                  'CompilerSha256': sha(args.iscc.read_bytes()), 'RemoteWrites': False}
    entries['INSTALLER-PROVENANCE.json'] = json.dumps(provenance, indent=2).encode()
    # The old application build proof remains byte-identical; payload manifest reflects new docs.
    del entries['manifest-sha256.json']
    manifest = {'Format': 'YanJi installer payload v1', 'BaseCommit': BASE,
                'RuntimeZipSha256': ZIP_SHA, 'Files': {n: {'Bytes': len(b), 'Sha256': sha(b)} for n, b in sorted(entries.items())}}
    entries['manifest-sha256.json'] = json.dumps(manifest, ensure_ascii=False, indent=2).encode('utf-8')
    binary_count = 0
    for name, raw in entries.items():
        validate_payload_name(name)
        if name.lower().endswith(('.exe', '.dll')):
            if before[name] != sha(raw):
                raise ValueError('Application/runtime binary changed: ' + name)
            binary_count += 1
        path = payload.joinpath(*name.split('/'))
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(raw)
    spec = importlib.util.spec_from_file_location('license_gate', ROOT / 'tools/Check-ReleaseLicenses.py')
    gate = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(gate)
    failures = gate.check(payload, 'binary')
    if failures:
        raise ValueError('License gate: ' + '; '.join(failures))
    file_list = output / 'payload-files.iss'
    lines = []
    for name in sorted(entries):
        parent = str(Path(name).parent).replace('/', '\\')
        destination = '{app}' if parent == '.' else '{app}\\' + parent
        source = name.replace('/', '\\')
        lines.append(f'Source: "{{#PayloadDir}}\\{source}"; DestDir: "{destination}"; Flags: ignoreversion')
    file_list.write_text('\n'.join(lines) + '\n', encoding='utf-8-sig')
    command = [str(args.iscc.resolve()), f'/DPayloadDir={payload}', f'/DPayloadList={file_list}',
               f'/DArtifactDir={output}', str(ROOT / 'installer/YanJi.iss')]
    result = subprocess.run(command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    (output / 'compiler.log').write_bytes(result.stdout)
    if result.returncode:
        print(result.stdout.decode(errors='replace'))
        raise SystemExit(result.returncode)
    setup = output / f'YanJi-v{VERSION}-Setup.exe'
    shutil.copyfile(args.archive, output / ZIP_NAME)
    result = {'Status': 'BUILD PASS - runtime validation still required', **provenance,
              'PayloadFiles': len(entries), 'IdenticalApplicationRuntimeBinaries': binary_count,
              'LicenseGate': 'PASS', 'SetupBytes': setup.stat().st_size, 'SetupSha256': sha(setup.read_bytes()),
              'PortableZipSha256': sha((output / ZIP_NAME).read_bytes())}
    (output / 'build-result.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    (output / 'SHA256SUMS-INSTALLER.txt').write_text(
        f'{result["SetupSha256"]}  {setup.name}\n{ZIP_SHA}  {ZIP_NAME}\n', encoding='ascii')
    print(json.dumps(result, indent=2))


if __name__ == '__main__':
    main()
