const fs = require('fs');
const path = require('path');

/**
 * @typedef {object} ProjectGraph
 * @property {Map<string, string>} nameToDir Project name to absolute project directory.
 * @property {Map<string, string[]>} nameToDeps Project name to referenced project names.
 * @property {Map<string, string>} nameToCsprojRel Project name to backend-relative csproj path.
 */

/**
 * Parse solution projects and project references from backend solution.
 * @param {string} backendDir Absolute backend directory.
 * @returns {ProjectGraph} Project maps used by affected discovery.
 */
function parseSolution(backendDir) {
  const slnx = fs.readFileSync(path.join(backendDir, 'SlideGenerator.slnx'), 'utf8');
  const projPaths = [...slnx.matchAll(/<Project\s+Path="([^"]+)"/g)].map(m => m[1]);

  const nameToDir = new Map();
  const nameToDeps = new Map();
  const nameToCsprojRel = new Map();

  for (const rel of projPaths) {
    const normalizedRel = rel.replace(/\\/g, '/');
    const abs = path.resolve(backendDir, normalizedRel);
    const name = path.basename(abs, '.csproj');
    nameToDir.set(name, path.dirname(abs));
    nameToCsprojRel.set(name, normalizedRel);

    let deps = [];
    try {
      const xml = fs.readFileSync(abs, 'utf8');
      deps = [...xml.matchAll(/<ProjectReference\s+Include="([^"]+)"/gi)]
        .map(m => path.basename(m[1].replace(/\\/g, '/'), '.csproj'));
    } catch {}

    nameToDeps.set(name, deps);
  }

  return {
    nameToCsprojRel,
    nameToDeps,
    nameToDir,
  };
}

/**
 * Invert project references into dependency to dependent mapping.
 * @param {Map<string, string[]>} nameToDeps Project name to referenced project names.
 * @returns {Map<string, Set<string>>} Dependency project name to direct dependents.
 */
function buildDependents(nameToDeps) {
  const dependents = new Map();
  for (const [proj, deps] of nameToDeps) {
    for (const dep of deps) {
      if (!dependents.has(dep)) dependents.set(dep, new Set());
      dependents.get(dep).add(proj);
    }
  }
  return dependents;
}

/**
 * Sort projects so dependencies appear before dependents.
 * @param {Map<string, string>} nameToDir Project name to absolute project directory.
 * @param {Map<string, string[]>} nameToDeps Project name to referenced project names.
 * @returns {string[]} Project names in topological order.
 */
function topoSort(nameToDir, nameToDeps) {
  const inDegree = new Map([...nameToDir.keys()].map(n => [n, 0]));
  const adj = new Map([...nameToDir.keys()].map(n => [n, []]));

  for (const [proj, deps] of nameToDeps) {
    for (const dep of deps) {
      if (inDegree.has(dep)) {
        inDegree.set(proj, inDegree.get(proj) + 1);
        adj.get(dep).push(proj);
      }
    }
  }

  const queue = [...inDegree.entries()].filter(([, d]) => d === 0).map(([n]) => n);
  const topo = [];
  while (queue.length) {
    const n = queue.shift();
    topo.push(n);
    for (const child of adj.get(n)) {
      const d = inDegree.get(child) - 1;
      inDegree.set(child, d);
      if (d === 0) queue.push(child);
    }
  }
  return topo;
}

/**
 * Resolve changed file path to owning project by directory containment.
 * @param {string} ws GitHub workspace path.
 * @param {Map<string, string>} nameToDir Project name to absolute project directory.
 * @param {string} file Workspace-relative changed file path.
 * @returns {string|null} Owning project name, or null when file is outside projects.
 */
function owningProject(ws, nameToDir, file) {
  const abs = path.resolve(ws, file);
  for (const [name, dir] of nameToDir) {
    if (abs === dir || abs.startsWith(dir + path.sep)) return name;
  }
  return null;
}

module.exports = {
  buildDependents,
  owningProject,
  parseSolution,
  topoSort,
};
