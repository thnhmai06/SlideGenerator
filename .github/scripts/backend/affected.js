const { owningProject } = require('./project-graph');

/**
 * Expand changed projects through transitive dependents.
 * @param {object} input Affected calculation inputs.
 * @param {string} input.ws GitHub workspace path.
 * @param {string} input.changedRaw Newline-delimited changed paths.
 * @param {Map<string, string>} input.nameToDir Project name to absolute project directory.
 * @param {Map<string, Set<string>>} input.dependents Dependency project name to dependents.
 * @returns {Set<string>} Changed projects plus all dependent projects.
 */
function transitiveAffectedProjects({ ws, changedRaw, nameToDir, dependents }) {
  const changedProjs = new Set(
    changedRaw.split('\n').filter(Boolean).map(file => owningProject(ws, nameToDir, file)).filter(Boolean)
  );

  const visited = new Set(changedProjs);
  const queue = [...changedProjs];
  while (queue.length) {
    const p = queue.shift();
    for (const dep of (dependents.get(p) || [])) {
      if (!visited.has(dep)) {
        visited.add(dep);
        queue.push(dep);
      }
    }
  }
  return visited;
}

/**
 * Split affected projects into production build targets and test execution targets.
 * @param {object} input Project list calculation inputs.
 * @param {string[]} input.topo Project names in dependency-first order.
 * @param {Set<string>} input.visited Affected production/test project names.
 * @param {Map<string, string[]>} input.nameToDeps Project name to referenced project names.
 * @param {Map<string, string>} input.nameToCsprojRel Project name to backend-relative csproj path.
 * @returns {{affectedProjects: {name: string, path: string}[], affectedTests: {name: string, path: string}[]}}
 * Production projects build when directly affected; test projects run when directly affected or referencing affected code.
 */
function affectedProjectLists({ topo, visited, nameToDeps, nameToCsprojRel }) {
  const affectedProjects = topo.filter(n =>
    !n.endsWith('.Tests') && visited.has(n)
  ).map(n => ({ name: n, path: nameToCsprojRel.get(n) }));

  const affectedTests = topo.filter(n =>
    n.endsWith('.Tests') && (
      visited.has(n) ||
      (nameToDeps.get(n) || []).some(d => visited.has(d))
    )
  ).map(n => ({ name: n, path: nameToCsprojRel.get(n) }));

  return {
    affectedProjects,
    affectedTests,
  };
}

module.exports = {
  affectedProjectLists,
  transitiveAffectedProjects,
};
