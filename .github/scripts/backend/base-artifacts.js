const { REQUIRED_BASE_ARTIFACTS } = require('./constants');

/**
 * Find latest successful backend-ci run for base commit that has all reusable artifacts.
 * @param {object} input GitHub API inputs.
 * @param {import('@actions/github').GitHub} input.github Octokit client from actions/github-script.
 * @param {object} input.context GitHub Actions context from actions/github-script.
 * @param {string} input.baseSha Base commit SHA.
 * @returns {Promise<string>} Workflow run id, or empty string when artifacts are unavailable.
 */
async function findBaseRunId({ github, context, baseSha }) {
  const runs = await github.paginate(github.rest.actions.listWorkflowRuns, {
    owner: context.repo.owner,
    repo: context.repo.repo,
    workflow_id: 'backend-ci.yml',
    head_sha: baseSha,
    status: 'success',
    per_page: 50,
  });

  for (const run of runs) {
    if (run.id === context.runId) continue;

    const artifacts = await github.paginate(github.rest.actions.listWorkflowRunArtifacts, {
      owner: context.repo.owner,
      repo: context.repo.repo,
      run_id: run.id,
      per_page: 100,
    });

    const names = new Set(artifacts.filter(a => !a.expired).map(a => a.name));
    if (REQUIRED_BASE_ARTIFACTS.every(name => names.has(name))) {
      return String(run.id);
    }
  }

  return '';
}

module.exports = {
  findBaseRunId,
};
