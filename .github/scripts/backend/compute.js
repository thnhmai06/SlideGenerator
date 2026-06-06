const path = require('path');
const { affectedProjectLists, transitiveAffectedProjects } = require('./affected');
const { findBaseRunId } = require('./base-artifacts');
const { shouldRunFullCi, shouldSkip } = require('./ci-mode');
const { changedFiles, commitMessage } = require('./git');
const { buildDependents, parseSolution, topoSort } = require('./project-graph');
const { setFullCi, setPartial, setSkipped } = require('./outputs');

/**
 * Entrypoint used by actions/github-script discover step.
 * Computes CI mode, affected build/test targets, and optional base artifact run id.
 * @param {object} input GitHub Script runtime objects.
 * @param {object} input.core GitHub Actions core API.
 * @param {import('@actions/github').GitHub} input.github Octokit client.
 * @param {object} input.context GitHub Actions context.
 * @returns {Promise<void>}
 */
async function run({ core, github, context }) {
  const ws = process.env.GITHUB_WORKSPACE;
  const backend = path.join(ws, 'backend');
  const event = process.env.EVENT;
  const baseSha = process.env.BASE_SHA || '';
  const headSha = process.env.HEAD_SHA || 'HEAD';
  const hasLabel = process.env.HAS_FULL_CI_LABEL === 'true';

  const message = commitMessage(ws);
  if (shouldSkip(message)) {
    setSkipped(core);
    return;
  }

  if (shouldRunFullCi({ ws, message, event, hasLabel, baseSha, headSha })) {
    setFullCi(core);
    return;
  }

  const graph = parseSolution(backend);
  const dependents = buildDependents(graph.nameToDeps);
  const topo = topoSort(graph.nameToDir, graph.nameToDeps);

  let changedRaw = '';
  try {
    changedRaw = changedFiles(ws, baseSha, headSha);
  } catch {
    setFullCi(core);
    return;
  }

  const visited = transitiveAffectedProjects({
    ws,
    changedRaw,
    nameToDir: graph.nameToDir,
    dependents,
  });

  const affected = affectedProjectLists({
    topo,
    visited,
    nameToDeps: graph.nameToDeps,
    nameToCsprojRel: graph.nameToCsprojRel,
  });
  
  if (affected.affectedProjects.length === 0 && affected.affectedTests.length === 0) {
    setFullCi(core);
    return;
  }

  if (affected.affectedTests.length === 0) {
    setPartial(core, {
      baseRunId: '',
      affectedProjects: affected.affectedProjects,
      affectedTests: [],
    });
    return;
  }

  let baseRunId = '';
  try {
    baseRunId = await findBaseRunId({ github, context, baseSha });
  } catch (e) {
    core.warning(`Failed to inspect base artifacts: ${e.message}`);
  }

  if (!baseRunId) {
    core.warning(`No complete backend-ci artifacts found for base commit ${baseSha}; falling back to full CI.`);
    setFullCi(core);
    return;
  }

  setPartial(core, {
    baseRunId,
    affectedProjects: affected.affectedProjects,
    affectedTests: affected.affectedTests,
  });
}

module.exports = {
  run,
};
