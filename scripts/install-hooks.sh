#!/bin/bash

# Script to install git hooks for MyBlog project
# Run this after cloning the repository to set up local development hooks

set -e

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
HOOKS_DIR="$REPO_ROOT/.git/hooks"
PRE_PUSH_HOOK="$HOOKS_DIR/pre-push"

echo "Installing pre-push hook..."

# Create the pre-push hook
cat > "$PRE_PUSH_HOOK" << 'HOOK_CONTENT'
#!/bin/bash

# Pre-push hook: Runs build and tests before allowing push
# Ensures broken code doesn't reach GitHub

# Skip if running in CI (CI already validates)
if [ "$CI" = "true" ]; then
  exit 0
fi

echo "🔍 Pre-push gate: Running build and tests..."
echo ""

# Run build
echo "▶️  Building solution (dotnet build MyBlog.slnx --no-incremental -c Release)..."
if ! dotnet build MyBlog.slnx --no-incremental -c Release; then
  echo ""
  echo "❌ Build FAILED. Push aborted."
  echo "💡 Fix build errors, commit, and try again."
  echo "⚠️  To skip this check (emergency only): git push --no-verify"
  exit 1
fi

echo ""
echo "✅ Build passed"
echo ""

# Run tests
echo "▶️  Running tests (dotnet test MyBlog.slnx --no-build -c Release)..."
if ! dotnet test MyBlog.slnx --no-build -c Release; then
  echo ""
  echo "❌ Tests FAILED. Push aborted."
  echo "💡 Fix failing tests, commit, and try again."
  echo "⚠️  To skip this check (emergency only): git push --no-verify"
  exit 1
fi

echo ""
echo "✅ All tests passed"
echo ""
echo "🚀 Push allowed - build and tests successful!"
echo ""
HOOK_CONTENT

# Make it executable
chmod +x "$PRE_PUSH_HOOK"

echo "✅ Pre-push hook installed at .git/hooks/pre-push"
echo ""
echo "The hook will:"
echo "  1. Build the solution (dotnet build MyBlog.slnx)"
echo "  2. Run all tests (dotnet test MyBlog.slnx)"
echo "  3. Abort push if either fails"
echo ""
echo "To bypass in emergencies: git push --no-verify"
echo ""
