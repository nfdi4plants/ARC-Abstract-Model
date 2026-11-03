"""Pack a staged native library as a local wheel using the Python standard library."""

from __future__ import annotations

import argparse
import base64
import csv
import hashlib
import io
from pathlib import Path
import tomllib
import zipfile


REPOSITORY = Path(__file__).resolve().parent.parent


def pack_wheel(package_root: Path, package: str, distribution: str, destination: Path,
               requires_python: str | None = None) -> Path:
    package_root = package_root.resolve(strict=True)
    source = package_root / package
    for required in ("__init__.py", "__init__.pyi", "py.typed"):
        if not (source / required).is_file():
            raise ValueError(f"Missing staged package file: {source / required}")
    destination.mkdir(parents=True, exist_ok=True)
    wheel = destination / f"{package}-0.0.0-py3-none-any.whl"
    dist_info = f"{package}-0.0.0.dist-info"
    project = tomllib.loads(
        (REPOSITORY / "pyproject.toml").read_text(encoding="utf-8")
    )["project"]
    dependency = next(value for value in project["dependencies"] if value.startswith("fable-library=="))
    requires_python = requires_python or project["requires-python"]
    files = {
        filename.relative_to(package_root).as_posix(): filename.read_bytes()
        for filename in sorted(source.rglob("*"))
        if filename.is_file() and "__pycache__" not in filename.parts and filename.suffix != ".pyc"
    }
    files[f"{dist_info}/METADATA"] = (
        f"Metadata-Version: 2.3\nName: {distribution}\nVersion: 0.0.0\n"
        f"Requires-Python: {requires_python}\nRequires-Dist: {dependency}\n"
    ).encode()
    files[f"{dist_info}/WHEEL"] = (
        "Wheel-Version: 1.0\nGenerator: ARC native package verification\n"
        "Root-Is-Purelib: true\nTag: py3-none-any\n"
    ).encode()
    records = io.StringIO(newline="")
    writer = csv.writer(records)
    for filename, data in files.items():
        digest = base64.urlsafe_b64encode(hashlib.sha256(data).digest()).rstrip(b"=").decode()
        writer.writerow([filename, "sha256=" + digest, len(data)])
    writer.writerow([f"{dist_info}/RECORD", "", ""])
    files[f"{dist_info}/RECORD"] = records.getvalue().encode()
    with zipfile.ZipFile(wheel, "w", zipfile.ZIP_DEFLATED) as archive:
        for filename, data in files.items():
            archive.writestr(filename, data)
    return wheel


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("package_root", type=Path)
    parser.add_argument("package")
    parser.add_argument("distribution")
    parser.add_argument("destination", type=Path)
    parser.add_argument("--requires-python")
    args = parser.parse_args()
    print(pack_wheel(args.package_root, args.package, args.distribution, args.destination,
                     args.requires_python))
