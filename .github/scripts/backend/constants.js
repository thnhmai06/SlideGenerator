/**
 * Fast matrix used by partial CI. Must stay aligned with base artifact names.
 * @type {{name: string, runner: string}[]}
 */
const FAST_RUNNERS = [
    {name: 'windows-x64', runner: 'windows-latest'},
    {name: 'linux-x64', runner: 'ubuntu-24.04'},
    {name: 'macos-arm64', runner: 'macos-26'},
];

/**
 * Full matrix used when change scope or missing artifacts require complete CI.
 * @type {{name: string, runner: string}[]}
 */
const FULL_RUNNERS = [
    {name: 'windows-x64', runner: 'windows-latest'},
    {name: 'windows-arm64', runner: 'windows-11-arm'},
    {name: 'linux-x64', runner: 'ubuntu-24.04'},
    {name: 'linux-arm64', runner: 'ubuntu-24.04-arm'},
    {name: 'macos-x64', runner: 'macos-26-intel'},
    {name: 'macos-arm64', runner: 'macos-26'},
];

/**
 * Artifacts required from a base commit before partial CI can reuse prior results.
 * @type {string[]}
 */
const REQUIRED_BASE_ARTIFACTS = [
    'test-results-windows-x64',
    'test-results-linux-x64',
    'test-results-macos-arm64',
    'coverage-linux-x64',
];

/**
 * GitHub zero SHA used for branch creation and unavailable base revisions.
 * @type {string}
 */
const ZERO_SHA = '0000000000000000000000000000000000000000';

module.exports = {
    FAST_RUNNERS,
    FULL_RUNNERS,
    REQUIRED_BASE_ARTIFACTS,
    ZERO_SHA,
};
