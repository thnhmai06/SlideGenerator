const {execSync} = require('child_process');

/**
 * Run a git command in the repository workspace and return trimmed stdout.
 * @param {string} ws GitHub workspace path.
 * @param {string} command Git command to execute.
 * @returns {string} Trimmed stdout.
 */
function exec(ws, command) {
    return execSync(command, {cwd: ws, encoding: 'utf8'}).trim();
}

/**
 * Read the latest commit message for skip/full CI directives.
 * @param {string} ws GitHub workspace path.
 * @returns {string} Commit message body.
 */
function commitMessage(ws) {
    return exec(ws, 'git log -1 --pretty=%B');
}

/**
 * List changed files between base/head, falling back to previous commit diff.
 * @param {string} ws GitHub workspace path.
 * @param {string} baseSha Base commit SHA.
 * @param {string} headSha Head commit SHA.
 * @returns {string} Newline-delimited changed paths.
 */
function changedFiles(ws, baseSha, headSha) {
    return exec(ws, `git diff --name-only ${baseSha}...${headSha}`);
}

module.exports = {
    changedFiles,
    commitMessage,
};
