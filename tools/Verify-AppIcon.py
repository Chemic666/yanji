"""Read native Windows icon resources without executing the inspected PE file.
Standard-library only. Used after publishing; no database or UI interaction.
"""
from pathlib import Path
import argparse
import ctypes
from ctypes import wintypes
import hashlib
import json
import struct

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_ICON = ROOT / "src/YanJi/Assets/Icons/YanJi.ico"
SIZES = {16, 24, 32, 48, 64, 128, 256}

def verify(executable, icon=DEFAULT_ICON):
    executable = Path(executable).resolve()
    source = Path(icon).read_bytes()
    reserved, kind, count = struct.unpack_from("<HHH", source)
    assert (reserved, kind, count) == (0, 1, 7), "Expected seven ICO frames"
    expected = {}
    digest = lambda b: hashlib.sha256(b).hexdigest().upper()
    for i in range(count):
        w,h,_,_,_,_,length,offset = struct.unpack_from("<BBBBHHII", source, 6 + 16*i)
        size = (w or 256, h or 256)
        assert offset+length <= len(source)
        expected[size] = digest(source[offset:offset+length])
    assert set(expected) == {(n,n) for n in SIZES}
    api = ctypes.WinDLL("kernel32", use_last_error=True)
    api.LoadLibraryExW.argtypes = [wintypes.LPCWSTR, ctypes.c_void_p, wintypes.DWORD]
    api.LoadLibraryExW.restype = ctypes.c_void_p
    api.FindResourceW.argtypes = [ctypes.c_void_p, ctypes.c_void_p, ctypes.c_void_p]
    api.FindResourceW.restype = ctypes.c_void_p
    api.LoadResource.argtypes = [ctypes.c_void_p, ctypes.c_void_p]
    api.LoadResource.restype = ctypes.c_void_p
    api.SizeofResource.argtypes = [ctypes.c_void_p, ctypes.c_void_p]
    api.SizeofResource.restype = wintypes.DWORD
    api.LockResource.argtypes = [ctypes.c_void_p]
    api.LockResource.restype = ctypes.c_void_p
    api.FreeLibrary.argtypes = [ctypes.c_void_p]
    callback = ctypes.WINFUNCTYPE(wintypes.BOOL, ctypes.c_void_p, ctypes.c_void_p, ctypes.c_void_p, ctypes.c_ssize_t)
    api.EnumResourceNamesW.argtypes = [ctypes.c_void_p, ctypes.c_void_p, callback, ctypes.c_ssize_t]
    module = api.LoadLibraryExW(str(executable), None, 0x2 | 0x20)  # data/image resources only
    if not module: raise ctypes.WinError(ctypes.get_last_error())
    try:
        names=[]
        @callback
        def collect(_module, _type, name, _context):
            names.append(name)
            return True
        if not api.EnumResourceNamesW(module, 14, collect, 0):
            raise ctypes.WinError(ctypes.get_last_error())
        def resource(name, kind):
            ref=api.FindResourceW(module, name, kind)
            if not ref: raise ctypes.WinError(ctypes.get_last_error())
            data=api.LockResource(api.LoadResource(module, ref))
            if not data: raise ctypes.WinError(ctypes.get_last_error())
            return ctypes.string_at(data, api.SizeofResource(module, ref))
        verified=[]
        for name in names:
            group=resource(name,14)
            _,typ,total=struct.unpack_from("<HHH",group)
            assert typ==1
            frames=[]
            for i in range(total):
                w,h,_,_,_,_,length,rid=struct.unpack_from("<BBBBHHIH",group,6+14*i)
                size=(w or 256,h or 256);pixels=resource(rid,3)
                assert len(pixels)==length and expected.get(size)==digest(pixels), "Unexpected native icon frame"
                frames.append({"Width":size[0],"Height":size[1],"Sha256":digest(pixels)})
            assert len(frames)==7 and {(r["Width"],r["Height"]) for r in frames}==set(expected)
            verified.append(frames)
        assert verified
        return {"Status":"PASS","Exe":str(executable),"IcoSha256":digest(source),"NativeGroups":verified}
    finally:
        api.FreeLibrary(module)

if __name__ == "__main__":
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("executable", type=Path)
    parser.add_argument("--icon", type=Path, default=DEFAULT_ICON)
    args=parser.parse_args()
    print(json.dumps(verify(args.executable,args.icon),indent=2))
