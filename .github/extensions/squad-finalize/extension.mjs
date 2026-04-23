// Extension: squad-finalize
// Squad workflow playbook - review, commit, push, PR, cleanup, and sync

import { joinSession } from "@github/copilot-sdk/extension";
import { execSync } from "node:child_process";

async function runCommand(command) {
    try {
        const output = execSync(command, { encoding: "utf8", stdio: ["pipe", "pipe", "pipe"] });
        return { success: true, output };
    } catch (err) {
        return { success: false, error: err.stderr || err.message };
    }
}

const session = await joinSession({
    tools: [
        {
            name: "squad_finalize_run",
            description: "Complete squad workflow: review changes, stage, commit, push, create PR, clean up, and sync with dev",
            inputSchema: {
                type: "object",
                properties: {
                    commitMsg: {
                        type: "string",
                        description: "Commit message (leave empty for auto-generated from branch name)",
                    },
                    baseBranch: {
                        type: "string",
                        description: "Base branch for PR (default: dev)",
                    },
                    deleteBranch: {
                        type: "boolean",
                        description: "Delete local branch after PR creation (default: true)",
                    },
                },
                required: [],
            },
            handler: async (input) => {
                const commitMsg = input.commitMsg || "";
                const baseBranch = input.baseBranch || "dev";
                const deleteBranch = input.deleteBranch !== false;

                // Step 1: Get current branch
                const branchResult = await runCommand("git rev-parse --abbrev-ref HEAD");
                if (!branchResult.success) return `❌ Error getting branch: ${branchResult.error}`;

                const currentBranch = branchResult.output.trim();
                if (currentBranch === "main" || currentBranch === "dev") {
                    return `❌ Cannot finalize on ${currentBranch} branch!`;
                }

                await session.log(`📍 Branch: ${currentBranch}`);

                // Step 2: Review changes
                const diffResult = await runCommand("git diff --stat");
                if (!diffResult.success) return `❌ Error getting diff: ${diffResult.error}`;

                await session.log(`📋 Changes:\n\`\`\`\n${diffResult.output}\`\`\``);

                // Step 3: Stage changes
                const stageResult = await runCommand("git add -A");
                if (!stageResult.success) return `❌ Error staging: ${stageResult.error}`;
                await session.log("📦 Changes staged");

                // Step 4: Generate or use provided commit message
                let finalCommitMsg = commitMsg;
                if (!finalCommitMsg) {
                    finalCommitMsg = currentBranch
                        .replace(/^squad\/\d+-/, "")
                        .replace(/-/g, " ")
                        .replace(/\b\w/g, (l) => l.toUpperCase());
                }

                const commitResult = await runCommand(`git commit -m "${finalCommitMsg}"`);
                if (!commitResult.success) return `❌ Error committing: ${commitResult.error}`;
                await session.log(`💬 Committed: ${finalCommitMsg}`);

                // Step 5: Push changes
                const pushResult = await runCommand(`git push -u origin ${currentBranch}`);
                if (!pushResult.success) return `❌ Error pushing: ${pushResult.error}`;
                await session.log(`🚀 Pushed to origin/${currentBranch}`);

                // Step 6: Create PR
                const prCmd = `gh pr create --base "${baseBranch}" --head "${currentBranch}" --title "${finalCommitMsg}" --body "Automated squad workflow PR"`;
                const prResult = await runCommand(prCmd);

                let prUrl = "";
                if (prResult.success) {
                    const match = prResult.output.match(/https:\/\/github\.com\/[^\s]+/);
                    prUrl = match ? match[0] : "";
                    if (prUrl) {
                        await session.log(`📝 PR created: ${prUrl}`);
                    }
                } else {
                    const checkPrCmd = `gh pr view ${currentBranch} --json url -q '.url'`;
                    const checkResult = await runCommand(checkPrCmd);
                    if (checkResult.success && checkResult.output.trim()) {
                        prUrl = checkResult.output.trim();
                        await session.log(`📝 PR found: ${prUrl}`);
                    } else {
                        await session.log(`⚠️  Could not create/find PR`);
                    }
                }

                // Step 7: Delete local branch
                if (deleteBranch) {
                    const deleteResult = await runCommand(`git branch -D ${currentBranch}`);
                    if (deleteResult.success) {
                        await session.log("🗑️  Local branch deleted");
                    } else {
                        await session.log(`⚠️  Could not delete branch: ${deleteResult.error}`);
                    }
                }

                // Step 8: Checkout dev
                const checkoutResult = await runCommand("git checkout dev");
                if (!checkoutResult.success) return `❌ Error checking out dev: ${checkoutResult.error}`;
                await session.log("🔄 Switched to dev");

                // Step 9: Sync with origin
                const syncResult = await runCommand("git pull origin dev");
                if (!syncResult.success) return `❌ Error syncing: ${syncResult.error}`;
                await session.log("🔁 Synced with origin/dev");

                const summary = `✨ Squad workflow complete!\n\n${prUrl ? `PR: ${prUrl}` : ""}`;
                return summary;
            },
        },
    ],
});
