# Release Process

## Automatic Versioning (v1.0.0+)

The MyBlog project now uses **pure semantic versioning** with automatic GitVersion integration.

### How It Works
1. **Main branch commits** → GitVersion calculates next patch version
2. **CI workflow** → Automatically creates and pushes git tag (e.g., `v1.0.1`)
3. **GitHub Release** → Create release from the tag (Aragorn's workflow)

### Version Scheme
- **Main branch**: Increments `Patch` (1.0.0 → 1.0.1 → 1.0.2...)
- **Dev branch**: Increments `Minor` with alpha label (1.1.0-alpha.X)
- **Feature branches**: Uses branch name as pre-release label

### Release Checklist
1. Merge feature PR to dev → gets alpha tag
2. Merge dev to main → gets release tag (v1.0.X)
3. GitHub Actions creates and pushes the tag automatically
4. (Manual) Create GitHub Release using the tag

### Legacy Sprint Tags
- `v1.0.0-sprint1`, `v1.0.0-sprint2`, `v1.0.0-sprint3` are archived
- `v1.0.0` is the first pure-semver release
- No manual sprint tags going forward
