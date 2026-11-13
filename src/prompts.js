'use strict';

const prompts = require('prompts');
const {
  validateProjectName,
  validateDbName,
  validateEmail,
  validatePassword,
  validateLogo,
} = require('./validators');

const DEFAULTS = {
  projectName: 'RyzCore',
  dbName: 'ryzcore_db',
  auditDbName: 'ryzcore_log_audit',
  server: 'localhost,1433',
  authType: 'sql',
  dbUser: 'sa',
  adminUser: 'admin',
  adminName: 'Administrator',
  adminEmail: 'admin@example.com',
};

async function askQuestions(args = {}) {
  const answers = await prompts(
    [
      {
        type: 'text',
        name: 'projectName',
        message: 'Project name',
        initial: args.name || DEFAULTS.projectName,
        validate: validateProjectName,
      },
      {
        type: 'text',
        name: 'dbName',
        message: 'Main database name',
        initial: args.db || DEFAULTS.dbName,
        validate: validateDbName,
      },
      {
        type: 'text',
        name: 'auditDbName',
        message: 'Audit/log database name',
        initial: args.auditDb || DEFAULTS.auditDbName,
        validate: validateDbName,
      },
      {
        type: 'text',
        name: 'server',
        message: 'SQL Server (host,port)',
        initial: args.server || DEFAULTS.server,
        validate: (value) => (value ? true : 'Server is required.'),
      },
      {
        type: 'text',
        name: 'dbUser',
        message: 'SQL login user',
        initial: args.user || DEFAULTS.dbUser,
        validate: (value) => (value ? true : 'DB user is required.'),
      },
      {
        type: 'password',
        name: 'dbPassword',
        message: 'SQL login password',
        validate: (value) => (value ? true : 'DB password is required.'),
      },
      {
        type: 'text',
        name: 'adminUser',
        message: 'Admin user id',
        initial: args.adminUser || DEFAULTS.adminUser,
        validate: (value) => (value ? true : 'Admin user is required.'),
      },
      {
        type: 'password',
        name: 'adminPassword',
        message: 'Admin password',
        validate: validatePassword,
      },
      {
        type: 'password',
        name: 'adminPasswordConfirm',
        message: 'Confirm admin password',
        validate: (value, values) =>
          !values || typeof values.adminPassword !== 'string' || value === values.adminPassword
            ? true
            : 'Passwords do not match.',
      },
      {
        type: 'text',
        name: 'adminName',
        message: 'Admin full name',
        initial: args.adminName || DEFAULTS.adminName,
        validate: (value) => (value ? true : 'Admin full name is required.'),
      },
      {
        type: 'text',
        name: 'adminEmail',
        message: 'Admin email',
        initial: args.adminEmail || DEFAULTS.adminEmail,
        validate: validateEmail,
      },
      {
        type: 'text',
        name: 'logoPath',
        message: 'Logo path (PNG/JPG/SVG, optional)',
        initial: args.logo || '',
        validate: validateLogo,
      },
      {
        type: 'confirm',
        name: 'includeMaui',
        message: 'Generate .NET MAUI Blazor Hybrid project?',
        initial: !args.noMaui,
      },
      {
        type: 'confirm',
        name: 'includeTests',
        message: 'Generate test project?',
        initial: Boolean(args.tests),
      },
      {
        type: 'confirm',
        name: 'includeDocker',
        message: 'Generate Docker Compose (SQL Server + API + Web)?',
        initial: Boolean(args.docker),
      },
      {
        type: 'confirm',
        name: 'agentDocs',
        message: 'Generate agent-ready docs (AGENTS.md, docs/agent)?',
        initial: true,
      },
    ],
    {
      onCancel: () => {
        const error = new Error('Installation cancelled.');
        error.code = 'CANCELLED';
        throw error;
      },
    }
  );

  const { adminPasswordConfirm, ...rest } = answers;

  return {
    ...rest,
    authType: 'sql',
    logoPath: rest.logoPath || '',
    trustServerCertificate: true,
    includeMaui: Boolean(rest.includeMaui),
    includeTests: Boolean(rest.includeTests),
    includeDocker: Boolean(rest.includeDocker),
    agentDocs: Boolean(rest.agentDocs),
  };
}

module.exports = { askQuestions, DEFAULTS };
