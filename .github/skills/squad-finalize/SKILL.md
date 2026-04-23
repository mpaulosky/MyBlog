---
name: squad-finalize
description: "Complete squad branch workflow: review changes, stage, commit, push, create PR, delete branch, checkout dev, and sync. Use when: finalizing a squad branch feature, ready to merge work, need to clean up local branch and sync."
argument-hint: "optional: --msg 'custom message' --base dev --no-delete --auto"
---

# Squad Finalize Skill

Automate the squad branch workflow: review changes, stage, commit, push, create PR, clean up, and sync with dev.

## Workflow

### 1. Validate Context
- Check current branch is not `main` or `dev` (safety check)
- Verify git is available and we're in a repository
- Confirm user is ready to proceed

### 2. Review Changes
- Run `git diff --stat` to show summary of changes
- Present changes to user for review
- Ask user to confirm proceeding

### 3. Stage and Commit
- Run `git add -A` to stage all changes
- Generate commit message from branch name (e.g., `squad/80-reorganize-test-projects` → `Reorganize Test Projects`)
- Allow user to provide custom commit message if desired
- Run `git commit -m "<message>"` with co-author trailer

### 4. Push to Remote
- Run `git push -u origin <current-branch>`
- Wait for push to complete
- Report success or failure

### 5. Create Pull Request
- Run `gh pr create --base dev --head <current-branch> --title "<commit-message>" --body "Automated squad workflow PR"`
- Extract PR URL from output
- Report PR creation success
- If PR already exists, fetch and display existing PR URL

### 6. Cleanup
- Ask user if they want to delete the local branch (default: yes)
- If confirmed, run `git branch -D <current-branch>`
- Report branch deletion

### 7. Sync with Dev
- Run `git checkout dev`
- Run `git pull origin dev`
- Report sync complete

### 8. Summary
- Display full workflow summary with PR link
- Show completion status

## Safety Checks

- ❌ Abort if on `main` or `dev` branch
- ❌ Abort if no changes to commit
- ⚠️ Warn if PR creation fails but don't abort
- ⚠️ Warn if branch deletion fails but don't abort

## Parameters

Users can provide:
- `--msg "custom message"` - Custom commit message (overrides auto-generated)
- `--base <branch>` - Base branch for PR (default: dev)
- `--no-delete` - Skip local branch deletion
- `--auto` - Skip confirmations (expert mode)

## Example Invocations

```
/skill squad-finalize
```

```
/skill squad-finalize --msg "Fix login validation" --base main
```

```
/skill squad-finalize --auto --no-delete
```

## Implementation Notes

- Extract branch name from `git rev-parse --abbrev-ref HEAD`
- Auto-generate commit message by:
  1. Remove `squad/\d+-` prefix
  2. Replace hyphens with spaces
  3. Title-case the result
- Use `gh pr create` and `gh pr view` for PR operations
- All git commands run with error handling
- Each step is logged to the CLI timeline for visibility
- **ALWAYS include co-author trailer**: `Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>`

## Step-by-Step Execution

### Step 1: Validate Context
1. Run `git rev-parse --abbrev-ref HEAD` to get current branch
2. If branch is `main` or `dev`, abort with error
3. Confirm user is ready to proceed

### Step 2: Review Changes
1. Run `git diff --stat` to show file changes
2. Display the diff summary to user
3. Ask: "Proceed with finalizing these changes?" (Y/n)

### Step 3: Stage & Commit
1. Run `git add -A` to stage all changes
2. Generate commit message from branch name or use `--msg` parameter
3. Format: remove prefix → spaces for hyphens → title-case
4. Run `git commit -m "<message>"` with co-author trailer
5. If commit fails, abort with error

### Step 4: Push to Remote
1. Run `git push -u origin <current-branch>`
2. Wait for push to complete
3. Show success message

### Step 5: Create Pull Request
1. Run `gh pr create --base dev --head <branch> --title "<message>" --body "Automated squad workflow PR"`
2. Extract PR URL from output using regex: `https://github\.com/[^\s]+`
3. If creation fails, check if PR exists: `gh pr view <branch> --json url -q '.url'`
4. Report PR URL or warn if not created

### Step 6: Cleanup (if not `--no-delete`)
1. Ask: "Delete local branch `<branch>`?" (Y/n)
2. If yes, run `git branch -D <current-branch>`
3. Warn if deletion fails but don't abort

### Step 7: Sync with Dev
1. Run `git checkout dev`
2. Run `git pull origin dev`
3. Show success message

### Step 8: Summary
Display:
```
✨ Squad workflow complete!
PR: <url>
Branch: Deleted
Status: Ready for review
```
