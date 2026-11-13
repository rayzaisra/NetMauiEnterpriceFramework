'use strict';

const fs = require('fs');
const path = require('path');

const PROJECT_NAME = /^[A-Za-z_][A-Za-z0-9_]*$/;
const DB_NAME = /^[A-Za-z_][A-Za-z0-9_]*$/;
const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

const CSHARP_KEYWORDS = new Set([
  'abstract', 'as', 'base', 'bool', 'break', 'byte', 'case', 'catch', 'char',
  'checked', 'class', 'const', 'continue', 'decimal', 'default', 'delegate',
  'do', 'double', 'else', 'enum', 'event', 'explicit', 'extern', 'false',
  'finally', 'fixed', 'float', 'for', 'foreach', 'goto', 'if', 'implicit',
  'in', 'int', 'interface', 'internal', 'is', 'lock', 'long', 'namespace',
  'new', 'null', 'object', 'operator', 'out', 'override', 'params', 'private',
  'protected', 'public', 'readonly', 'ref', 'return', 'sbyte', 'sealed',
  'short', 'sizeof', 'stackalloc', 'static', 'string', 'struct', 'switch',
  'this', 'throw', 'true', 'try', 'typeof', 'uint', 'ulong', 'unchecked',
  'unsafe', 'ushort', 'using', 'virtual', 'void', 'volatile', 'while',
  'record', 'required', 'file', 'global', 'init', 'scoped',
]);

function validateProjectName(value) {
  if (!value || !PROJECT_NAME.test(value)) {
    return 'Project name must start with a letter/underscore and contain only letters, digits, and underscores.';
  }
  if (CSHARP_KEYWORDS.has(value.toLowerCase())) {
    return `"${value}" is a reserved C# keyword.`;
  }
  if (value.length > 80) {
    return 'Project name is too long.';
  }
  return true;
}

function validateDbName(value) {
  if (!value || !DB_NAME.test(value)) {
    return 'Database name must start with a letter/underscore and contain only letters, digits, and underscores.';
  }
  if (value.length > 120) {
    return 'Database name is too long.';
  }
  return true;
}

function validateEmail(value) {
  if (!value) return true;
  return EMAIL.test(value) || 'Enter a valid email address.';
}

function validatePassword(value) {
  if (!value || value.length < 8) {
    return 'Password must be at least 8 characters.';
  }
  return true;
}

function validateLogo(value) {
  if (!value) return true;
  const ext = path.extname(value).toLowerCase();
  if (!['.png', '.jpg', '.jpeg', '.svg'].includes(ext)) {
    return 'Logo must be PNG, JPG, or SVG.';
  }
  if (!fs.existsSync(value)) {
    return 'Logo file does not exist.';
  }
  const stat = fs.statSync(value);
  if (stat.size > 2 * 1024 * 1024) {
    return 'Logo must be smaller than 2 MB.';
  }
  return true;
}

function validateSpec(spec) {
  const errors = [];

  if (validateProjectName(spec.projectName) !== true) {
    errors.push(`projectName: ${validateProjectName(spec.projectName)}`);
  }
  if (validateDbName(spec.dbName) !== true) {
    errors.push(`dbName: ${validateDbName(spec.dbName)}`);
  }
  if (validateDbName(spec.auditDbName) !== true) {
    errors.push(`auditDbName: ${validateDbName(spec.auditDbName)}`);
  }
  if (spec.dbName && spec.auditDbName && spec.dbName.toLowerCase() === spec.auditDbName.toLowerCase()) {
    errors.push('auditDbName must be different from dbName.');
  }
  if (!spec.server) {
    errors.push('server is required.');
  }
  if (!spec.adminUser) {
    errors.push('adminUser is required.');
  }
  if (validatePassword(spec.adminPassword) !== true) {
    errors.push(`adminPassword: ${validatePassword(spec.adminPassword)}`);
  }
  if (!spec.adminName) {
    errors.push('adminName is required.');
  }
  if (validateEmail(spec.adminEmail) !== true) {
    errors.push(`adminEmail: ${validateEmail(spec.adminEmail)}`);
  }
  if (spec.authType !== 'sql') {
    errors.push('authType: only SQL authentication is supported in the MVP.');
  } else if (!spec.dbUser) {
    errors.push('dbUser is required for SQL authentication.');
  }
  if (validateLogo(spec.logoPath) !== true) {
    errors.push(`logoPath: ${validateLogo(spec.logoPath)}`);
  }

  return errors;
}

module.exports = {
  validateProjectName,
  validateDbName,
  validateEmail,
  validatePassword,
  validateLogo,
  validateSpec,
};
