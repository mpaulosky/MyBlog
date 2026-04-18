#!/bin/bash
# Installs the pre-push gate hook from .github/hooks/pre-push into the local Git hooks directory.
# Safe to re-run: skips if already up-to-date, backs up any differing hook before overwriting.

set -e

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SOURCE="$REPO_ROOT/.github/hooks/pre-push"
HOOKS_DIR="$(git -C "$REPO_ROOT" rev-parse --git-path hooks)"
DEST="$HOOKS_DIR/pre-push"

if [[ ! -f "$SOURCE" ]]; then
  echo "❌  Source hook not found: $SOURCE"
  exit 1
fi

if [[ -f "$DEST" ]] && cmp -s "$SOURCE" "$DEST"; then
  echo "✅  Pre-push hook is already up-to-date. Nothing to do."
  exit 0
fi

if [[ -f "$DEST" ]]; then
  BACKUP="$DEST.bak.$(date +%Y%m%d%H%M%S)"
  echo "⚠️   Existing hook differs — backing up to: $BACKUP"
  cp "$DEST" "$BACKUP"
fi

cp "$SOURCE" "$DEST"
chmod +x "$DEST"
echo "✅  Pre-push hook installed at $DEST"
echo ""
echo "The hook enforces 5 gates on every 'git push':"
echo "  0. Blocks direct pushes to main/dev"
echo "  1. Warns about untracked .razor/.cs source files"
echo "  2. Release build  (dotnet build MyBlog.slnx --configuration Release)"
echo "  3. Unit/arch tests (tests/Architecture.Tests, tests/Unit.Tests)"
echo "  4. Integration tests (tests/Integration.Tests — Docker required)"
echo ""
echo "To skip in an emergency: git push --no-verify"
