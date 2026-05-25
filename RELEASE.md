# Release Process

MyBlog uses **manual semantic versioning** with post-release backmerging to keep `dev` and `main` in sync.

## Release Workflow

### 1. Feature Development

Features merge to `dev` via squad PRs (branch: `squad/{issue}-{slug}`).

### 2. Release Preparation

When ready for release, create a release PR:

```bash
git checkout dev
git pull origin dev
git checkout -b release/vX.Y.Z
# Make any final version/changelog updates if needed
git commit -m "chore: bump version vX.Y.Z"
git push origin release/vX.Y.Z
```

### 3. Release PR and Merge

Open a PR from `release/vX.Y.Z` → `main`. The PR title must include a semver signal:

- Use `+semver: minor` for feature releases
- Use `+semver: patch` for bug fix releases
- Example title: `chore(release): v1.7.0 — MongoDB ObjectId Migration & Cache Hardening +semver: minor`

After approval, merge the PR (squash or regular merge).

### 4. Manual Tag Creation

After merge to `main`, manually create the git tag and push it:

```bash
git tag v1.7.0 <commit-hash>
git push origin refs/tags/v1.7.0
```

**Note:** Use `refs/tags/vX.Y.Z` syntax to bypass pre-push hook constraints on protected branches (see issue #387).

### 5. GitHub Release

Create a GitHub Release from the tag using the CLI:

```bash
gh release create v1.7.0 --notes "Release notes here"
```

### 6. Post-Release Backmerge (Required)

After release, merge `main` back into `dev` to prevent ancestry drift:

```bash
git checkout dev
git pull origin dev
git merge --no-ff main -m "backmerge: main → dev after v1.7.0 release"
git push origin dev
```

Or create a backmerge PR for review: `backmerge/sprint-N-main-to-dev`.

## Version History

| Version | Sprint | Release Date | Highlights |
| --------- | -------- | ------------- | ------------ |
| v1.7.0 | 20 | 2026-06-06 | MongoDB ObjectId Migration & Cache Hardening |
| v1.6.0 | 19 | 2026-05-23 | Markdown Editor & Categories |
| v1.5.2 | 18 | 2026-05-23 | Bug fixes |
| v1.5.1 | 18 | 2026-05-08 | MongoDB Dev Commands |
| v1.5.0 | 18 | 2026-05-08 | MongoDB Refactoring |
| v1.4.0 | 17 | 2026-05-08 | Board Automation |

## Key Points

- **Manual tagging:** No automatic CI version bumping; the team owns the version number
- **Backmerge required:** Every release must backmerge `main` → `dev` to keep branches in sync
- **Post-squash step:** The backmerge prevents orphaned commits and ensures clean history
- **Protected branches:** Use `git push origin refs/tags/vX.Y.Z` if tag push is blocked by pre-push hook
