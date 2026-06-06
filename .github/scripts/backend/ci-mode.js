const { ZERO_SHA } = require('./constants');
const { changedFiles } = require('./git');

/**
 * Check commit directive that disables backend CI.
 * @param {string} message Latest commit message.
 * @returns {boolean} True when CI should stop before matrix creation.
 */
function shouldSkip(message) {
  return message.startsWith('[skip ci]');
}

/**
 * Decide whether partial affected CI is unsafe or unnecessary.
 * @param {object} input Decision inputs.
 * @param {string} input.ws GitHub workspace path.
 * @param {string} input.message Latest commit message.
 * @param {string} input.event GitHub event name.
 * @param {boolean} input.hasLabel Whether pull request has full-ci label.
 * @param {string} input.baseSha Base commit SHA.
 * @param {string} input.headSha Head commit SHA.
 * @returns {boolean} True when full CI should run.
 */
function shouldRunFullCi({ ws, message, event, hasLabel, baseSha, headSha }) {
  if (
    message.startsWith('[full ci]') ||
    message.startsWith('[full]') ||
    event === 'release' ||
    hasLabel ||
    !baseSha ||
    baseSha === ZERO_SHA
  ) {
    return true;
  }

  let diff = '';
  try {
    diff = changedFiles(ws, baseSha, headSha);
  } catch {
    return true;
  }

  return /^\.github\//m.test(diff) ||
    /^backend\/(Directory\.(Build|Packages)\.props|global\.json|nuget\.config|[^/]+\.slnx)$/m.test(diff);
}

module.exports = {
  shouldRunFullCi,
  shouldSkip,
};
