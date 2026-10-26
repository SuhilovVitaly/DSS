#!/usr/bin/env bash
# Rebase the desktop entry after extraction. Pass --install to add it to your menu.
set -euo pipefail
[[ ${1:---generate-only} == --generate-only || ${1:-} == --install ]] || { printf 'Use --generate-only or --install\n' >&2; exit 2; }
root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
python3 - "$root" "${1:---generate-only}" <<'PY'
from pathlib import Path
import os
import sys
root = Path(sys.argv[1])
def quote(value):
    return '"' + str(value).replace('\\', '\\\\').replace('"', '\\"').replace('`', '\\`').replace('$', '\\$').replace('%', '%%') + '"'
entry = '[Desktop Entry]\nType=Application\nName=Deep Space Saga\nComment=Space strategy game\nExec=' + quote(root / 'DeepSpaceSaga.Client') + '\nIcon=' + str(root / 'deepspacesaga.svg') + '\nTerminal=false\nCategories=Game;StrategyGame;\n'
if sys.argv[2] == '--install':
    base = os.environ.get('XDG_DATA_HOME', '')
    destination = (Path(base) if base and Path(base).is_absolute() else Path.home() / '.local/share') / 'applications/deepspacesaga.desktop'
else:
    destination = root / 'deepspacesaga.desktop'
destination.parent.mkdir(parents=True, exist_ok=True)
destination.write_text(entry, encoding='utf-8')
print(destination)
PY
