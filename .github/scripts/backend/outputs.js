const {FAST_RUNNERS, FULL_RUNNERS} = require('./constants');

/**
 * @typedef {object} OutputValues
 * @property {'true'|'false'} shouldRun Whether verify the job should run.
 * @property {'true'|'false'} fullCi Whether verify should run full suite.
 * @property {string} baseRunId Base workflow run id for artifact reuse.
 * @property {string} affectedProjects JSON array for production build targets.
 * @property {string} affectedTests JSON array for test/coverage targets.
 * @property {{name: string, runner: string}[]} runners Matrix runner entries.
 */

/**
 * Write all discovery outputs in one place to keep workflow_call inputs stable.
 * @param {object} core GitHub Actions core API from actions/github-script.
 * @param {OutputValues} values Output values.
 * @returns {void}
 */
function setCommon(core, values) {
    core.setOutput('should-run', values.shouldRun);
    core.setOutput('full-ci', values.fullCi);
    core.setOutput('base-run-id', values.baseRunId);
    core.setOutput('affected-projects', values.affectedProjects);
    core.setOutput('affected-tests', values.affectedTests);
    core.setOutput('runners', JSON.stringify(values.runners));
}

/**
 * Emit outputs for explicit [skip ci].
 * @param {object} core GitHub Actions core API from actions/github-script.
 * @returns {void}
 */
function setSkipped(core) {
    setCommon(core, {
        shouldRun: 'false',
        fullCi: 'false',
        baseRunId: '',
        affectedProjects: '[]',
        affectedTests: '[]',
        runners: FAST_RUNNERS,
    });
}

/**
 * Emit outputs for full CI fallback or forced full CI.
 * @param {object} core GitHub Actions core API from actions/github-script.
 * @returns {void}
 */
function setFullCi(core) {
    setCommon(core, {
        shouldRun: 'true',
        fullCi: 'true',
        baseRunId: '',
        affectedProjects: '[]',
        affectedTests: '[]',
        runners: FULL_RUNNERS,
    });
}

/**
 * Emit outputs for partial CI.
 * @param {object} core GitHub Actions core API from actions/github-script.
 * @param {object} input Partial CI outputs.
 * @param {string} input.baseRunId Base workflow run id for artifact reuse.
 * @param {{name: string, path: string}[]} input.affectedProjects Production build targets.
 * @param {{name: string, path: string}[]} input.affectedTests Test/coverage targets.
 * @returns {void}
 */
function setPartial(core, {baseRunId, affectedProjects, affectedTests}) {
    setCommon(core, {
        shouldRun: 'true',
        fullCi: 'false',
        baseRunId,
        affectedProjects: JSON.stringify(affectedProjects),
        affectedTests: JSON.stringify(affectedTests),
        runners: FAST_RUNNERS,
    });
}

module.exports = {
    setFullCi,
    setPartial,
    setSkipped,
};
