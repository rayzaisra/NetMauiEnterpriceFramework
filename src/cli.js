'use strict';

const fs = require('fs');
const path = require('path');
const pc = require('picocolors');
const { askQuestions, DEFAULTS } = require('./prompts');
const { validateSpec } = require('./validators');
const { generate } = require('./generator');
const pkg = require('../package.json');

function parseArgs(argv) {
  const args = { _: [] };
  const takesValue = new Set([
    '--name', '--db', '--audit-db', '--server', '--user', '--password',
    '--admin-user', '--admin-password', '--admin-name', '--admin-email',
    '--logo', '--config', '--output',
  ]);
  const camel = {
    '--name': 'name',
    '--db': 'db',
    '--audit-db': 'auditDb',
    '--server': 'server',
    '--user': 'user',
    '--password': 'password',
    '--admin-user': 'adminUser',
    '--admin-password': 'adminPassword',
    '--admin-name': 'adminName',
    '--admin-email': 'adminEmail',
    '--logo': 'logo',
    '--config': 'config',
    '--output': 'outputDir',
  };

  for (let i = 0; i < argv.length; i += 1) {
    const arg = argv[i];
    if (takesValue.has(arg)) {
      const value = argv[i + 1];
      if (value === undefined || value.startsWith('--')) {
        throw new Error(`Missing value for ${arg}`);
      }
      args[camel[arg]] = value;
      i += 1;
      continue;
    }
    switch (arg) {
      case '--no-maui': args.noMaui = true; break;
      case '--tests': args.tests = true; break;
      case '--docker': args.docker = true; break;
      case '--skip-build': args.skipBuild = true; break;
      case '--skip-seed': args.skipSeed = true; break;
      case '--force': args.force = true; break;
      case '--dry-run': args.dryRun = true; break;
      case '--yes': args.yes = true; break;
      case '--help': case '-h': args.help = true; break;
      case '--version': case '-v': args.version = true; break;
      default:
        if (arg.startsWith('--')) throw new Error(`Unknown option: ${arg}`);
        args._.push(arg);
    }
  }
  return args;
}

function normalizeSpec(raw, args) {
  const spec = {
    projectName: raw.projectName || raw.name || DEFAULTS.projectName,
    dbName: raw.dbName || raw.db || DEFAULTS.dbName,
    auditDbName: raw.auditDbName || raw.auditDb || `${raw.dbName || raw.db || DEFAULTS.dbName}_log_audit`,
    server: raw.server || DEFAULTS.server,
    authType: raw.authType || 'sql',
    dbUser: raw.dbUser || raw.user || DEFAULTS.dbUser,
    dbPassword: raw.dbPassword || raw.password || '',
    trustServerCertificate: raw.trustServerCertificate !== false,
    adminUser: raw.adminUser || DEFAULTS.adminUser,
    adminPassword: raw.adminPassword || '',
    adminName: raw.adminName || DEFAULTS.adminName,
    adminEmail: raw.adminEmail || DEFAULTS.adminEmail,
    logoPath: raw.logoPath || raw.logo || '',
    includeMaui: raw.includeMaui !== false && !args.noMaui,
    includeTests: Boolean(raw.includeTests || args.tests),
    includeDocker: Boolean(raw.includeDocker || args.docker),
    agentDocs: raw.agentDocs !== false,
  };
  return spec;
}

function printHelp() {
  console.log(`
${pc.bold('RyzCore')} ${pkg.version}

RyzCore generates an enterprise .NET 10 framework with Blazor WASM, MAUI Blazor
Hybrid, MudBlazor, dynamic menus, parameter management, and a separate audit
database.

${pc.bold('Usage')}
  npm create ryzcore@latest
  npx create-ryzcore [options]
  npm install -g create-ryzcore
  create-ryzcore [options]

${pc.bold('Options')}
  --name <name>              Project name
  --db <name>                Main database name
  --audit-db <name>          Audit/log database name
  --server <host,port>       SQL Server endpoint
  --user <user>              SQL login user
  --password <password>      SQL login password
  --admin-user <user>        Admin user id
  --admin-password <pass>    Admin password
  --admin-name <name>        Admin full name
  --admin-email <email>      Admin email
  --logo <path>              Logo PNG/JPG/SVG
  --config <file>            JSON config (non-interactive)
  --output <dir>             Output directory (default: project name)
  --no-maui                  Skip MAUI project generation
  --tests                    Include test project
  --docker                   Include Docker Compose
  --skip-build               Skip dotnet build
  --skip-seed                Skip database seed
  --force                    Overwrite non-empty output directory
  --dry-run                  Validate and print the plan only
  --yes                      Non-interactive (requires config or full args)
  -h, --help                 Show this help
  -v, --version              Show version
`);
}

function printPlan(spec, args) {
  const output = path.resolve(process.cwd(), args.outputDir || spec.projectName);
  console.log(pc.bold('\nRyzCore scaffold plan\n'));
  console.log(`  Project      : ${spec.projectName}`);
  console.log(`  Output       : ${output}`);
  console.log(`  Main DB      : ${spec.dbName}`);
  console.log(`  Audit DB     : ${spec.auditDbName}`);
  console.log(`  SQL Server   : ${spec.server}`);
  console.log(`  Auth         : ${spec.authType}`);
  console.log(`  Admin user   : ${spec.adminUser}`);
  console.log(`  MAUI         : ${spec.includeMaui ? 'yes' : 'no'}`);
  console.log(`  Tests        : ${spec.includeTests ? 'yes' : 'no'}`);
  console.log(`  Docker       : ${spec.includeDocker ? 'yes' : 'no'}`);
  console.log(`  Agent docs   : ${spec.agentDocs ? 'yes' : 'no'}`);
  console.log('');
}

async function loadSpec(args) {
  if (args.config) {
    if (!fs.existsSync(args.config)) {
      throw new Error(`Config file not found: ${args.config}`);
    }
    const config = JSON.parse(
      fs.readFileSync(args.config, 'utf8').replace(/^\uFEFF/, '')
    );
    return { ...config, ...args };
  }
  if (args.yes) {
    return { ...args };
  }
  return askQuestions(args);
}

async function run() {
  const args = parseArgs(process.argv.slice(2));

  if (args.help) {
    printHelp();
    return;
  }
  if (args.version) {
    console.log(pkg.version);
    return;
  }

  try {
    const raw = await loadSpec(args);
    const spec = normalizeSpec(raw, args);
    const errors = validateSpec(spec);

    if (errors.length > 0) {
      console.error(pc.red('Invalid configuration:'));
      for (const error of errors) console.error(`  - ${error}`);
      process.exitCode = 1;
      return;
    }

    printPlan(spec, args);

    if (args.dryRun) {
      console.log(pc.yellow('Dry run: no files or databases were changed.'));
      return;
    }

    await generate(spec, args);
    console.log(pc.green('\nDone.'));
  } catch (error) {
    if (error.code === 'CANCELLED') {
      console.log(pc.yellow('\nCancelled.'));
      return;
    }
    throw error;
  }
}

module.exports = { run, parseArgs, normalizeSpec };
