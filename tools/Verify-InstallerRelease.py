"""Read-only provenance verification for the fixed, already accepted Beta installer."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[1]


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT)


def require(condition, message):
    if not condition:
        raise ValueError(message)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--setup', required=True, type=Path)
    parser.add_argument('--output', type=Path)
    parser.add_argument('--require-clean', action='store_true')
    args = parser.parse_args()
    metadata = read(ROOT / 'installer/RELEASE-ATTACHMENTS.json')
    frozen = ROOT / 'installer/verified-beta1'
    snapshot = read(frozen / 'SNAPSHOT.json')
    expected = metadata['SetupSha256']
    require(sha(args.setup.read_bytes()) == expected, 'Approved installer SHA-256 mismatch')
    require(args.setup.stat().st_size == metadata['SetupBytes'], 'Approved installer size mismatch')
    record = read(frozen / 'installer-validation.json')
    require(record['SetupSha256'].lower() == expected and snapshot['SetupSha256'] == expected, 'Installer/evidence mismatch')
    require(record['Status'] == 'PASS' and len(record['Checks']) == 75 and all(x['Status'] == 'PASS' for x in record['Checks']), 'Final 75 checks do not all pass')
    require(not record['TestProcessElevated'] and not record['RealLearningDataAccessed'], 'Isolation/non-administrator evidence mismatch')
    require(sha((frozen / 'installer-validation.json').read_bytes()) == metadata['RawValidationSha256'], 'Raw validation record changed')
    build = read(frozen / 'build-result.json')
    require(build['SetupSha256'] == expected and not build['ApplicationRecompiled'], 'Build evidence mismatch')
    head = git('rev-parse', 'HEAD').decode().strip()
    base = metadata['OriginalPublicMain']
    build_commit = metadata['BuildInputCommit']
    for ancestor in [base, build_commit]:
        require(subprocess.run(['git', 'merge-base', '--is-ancestor', ancestor, head], cwd=ROOT).returncode == 0, 'Original history/build input commit is not an ancestor')
    document_differences = []
    for name, expected_input in snapshot['BuildInputs'].items():
        require(sha((frozen / 'build-inputs' / name).read_bytes()) == expected_input, 'Frozen build input changed: ' + name)
        require(sha(git('show', build_commit + ':' + name)) == expected_input, 'Git build input does not match original bytes: ' + name)
        if sha((ROOT / name).read_bytes()) != expected_input:
            require(name.endswith('.md'), 'Executable build input changed after acceptance: ' + name)
            document_differences.append(name)
    for name, row in snapshot['EvidenceFiles'].items():
        raw = (frozen / name).read_bytes()
        require(len(raw) == row['Bytes'] and sha(raw) == row['Sha256'], 'Archived evidence changed: ' + name)
    require(subprocess.run(['git', 'diff', '--quiet', base, head, '--', 'src', 'resources', 'Directory.Build.props', 'Directory.Packages.props'], cwd=ROOT).returncode == 0, 'Application source/resources/build configuration changed')
    identity = set(git('log', base + '..' + head, '--format=%ae%n%ce').decode().splitlines())
    require(identity == {'182623705+Chemic666@users.noreply.github.com'}, 'New commit identity differs from existing public noreply identity')
    dirty = bool(git('status', '--porcelain').strip())
    if args.require_clean:
        require(not dirty, 'Release integration worktree is not clean')
    result = {'Status': 'PASS', 'SetupSha256': expected, 'SetupBytes': args.setup.stat().st_size,
              'OriginalPublicMain': base, 'BuildInputCommit': build_commit, 'IntegrationCommit': head,
              'RawValidationSha256': metadata['RawValidationSha256'], 'OriginalInstallerChecks': 75,
              'InstallerChecksRerun': False, 'InstallerRebuilt': False,
              'VerifiedBuildInputs': len(snapshot['BuildInputs']), 'VerifiedEvidenceFiles': len(snapshot['EvidenceFiles']),
              'CurrentDocumentationDiffersFromFrozenBuild': sorted(document_differences),
              'WorktreeDirty': dirty, 'OriginalHistoryPreserved': True, 'ApplicationSourceUnchanged': True,
              'ExistingFullSourceZipIncludesInstaller': False, 'RemoteWritesPerformedByThisTool': False}
    if args.output:
        args.output.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps(result, ensure_ascii=False, indent=2))


if __name__ == '__main__':
    main()
