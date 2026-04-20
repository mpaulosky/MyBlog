#!/usr/bin/env bash
# Installs the pre-push gate hook from .github/hooks/pre-push into the local Git hooks directory.
# Safe to re-run: skips if already up-to-date, backs up any differing hook before overwriting.

set -e

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
HOOKS_DIR_RAW="$(git -C "$REPO_ROOT" rev-parse --git-path hooks)"
case "$HOOKS_DIR_RAW" in
  /*)
    HOOKS_DIR="$HOOKS_DIR_RAW"
    ;;
  *)
    HOOKS_DIR="$REPO_ROOT/$HOOKS_DIR_RAW"
    ;;
esac

mkdir -p "$HOOKS_DIR"

# Install pre-push hook
PRE_PUSH_SOURCE="$REPO_ROOT/.github/hooks/pre-push"
PRE_PUSH_DEST="$HOOKS_DIR/pre-push"

if [[ ! -f "$PRE_PUSH_SOURCE" ]]; then
  echo "❌  Source hook not found: $PRE_PUSH_SOURCE"
  exit 1
fi

if [[ -f "$PRE_PUSH_DEST" ]] && cmp -s "$PRE_PUSH_SOURCE" "$PRE_PUSH_DEST"; then
  echo "✅  Pre-push hook is already up-to-date."
else
  if [[ -f "$PRE_PUSH_DEST" ]]; then
    BACKUP="$PRE_PUSH_DEST.bak.$(date +%Y%m%d%H%M%S)"
    echo "⚠️   Existing pre-push hook differs — backing up to: $BACKUP"
    cp "$PRE_PUSH_DEST" "$BACKUP"
  fi
  cp "$PRE_PUSH_SOURCE" "$PRE_PUSH_DEST"
  chmod +x "$PRE_PUSH_DEST"
  echo "✅  Pre-push hook installed at $PRE_PUSH_DEST"
fi

# Install post-checkout hook (auto-bootstraps pre-push on clone/checkout)
POST_CHECKOUT_SOURCE="$REPO_ROOT/.github/hooks/post-checkout"
POST_CHECKOUT_DEST="$HOOKS_DIR/post-checkout"

if [[ -f "$POST_CHECKOUT_SOURCE" ]]; then
  if [[ -f "$POST_CHECKOUT_DEST" ]] && cmp -s "$POST_CHECKOUT_SOURCE" "$POST_CHECKOUT_DEST"; then
    echo "✅  Post-checkout hook is already up-to-date."
  else
    if [[ -f "$POST_CHECKOUT_DEST" ]]; then
      BACKUP="$POST_CHECKOUT_DEST.bak.$(date +%Y%m%d%H%M%S)"
      echo "⚠️   Existing post-checkout hook differs — backing up to: $BACKUP"
      cp "$POST_CHECKOUT_DEST" "$BACKUP"
    fi
    cp "$POST_CHECKOUT_SOURCE" "$POST_CHECKOUT_DEST"
    chmod +x "$POST_CHECKOUT_DEST"
    echo "✅  Post-checkout hook installed at $POST_CHECKOUT_DEST"
  fi
fi

echo ""
echo "The hook enforces 5 gates on every 'git push':"
echo "  0. Enforces branch naming — squad/{issue}-{slug} runs all gates;"
echo "       sprint/{N}-{slug} passes Gate 0 and exits (skips feature gates)"
echo "  1. Warns about untracked .razor/.cs source files"
echo "  2. Release build  (dotnet build MyBlog.slnx --configuration Release)"
echo "  3. Unit/arch tests (tests/Architecture.Tests, tests/Unit.Tests)"
echo "  4. Integration tests (tests/Integration.Tests — Docker required)"
echo ""
echo "To skip in an emergency: git push --no-verify"
