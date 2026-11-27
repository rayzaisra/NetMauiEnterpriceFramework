'use strict';

const fs = require('fs-extra');
const path = require('path');
const crypto = require('crypto');
const pc = require('picocolors');
const { ensureDatabases, buildSolution, seedDatabase, deployProcedures, runDotnet } = require('./db');

const TEMPLATE_DIR = path.join(__dirname, '..', 'templates', 'solution');

const TEXT_EXTENSIONS = new Set([
  '.cs', '.razor', '.csproj', '.sln', '.slnx', '.json', '.html', '.css',
  '.js', '.ts', '.md', '.sql', '.xml', '.config', '.props', '.targets',
  '.yml', '.yaml', '.txt', '.env', '.example', '.gitignore', '.xaml',
  '.http', '.manifest', '.appxmanifest', '.plist', '.resx', '.svg', '.user',
]);

function isTextFile(file) {
  return TEXT_EXTENSIONS.has(path.extname(file).toLowerCase()) || path.basename(file) === '.gitignore';
}

function buildTokens(spec) {
  const appIdName = spec.projectName.toLowerCase().replace(/[^a-z0-9]/g, '') || 'app';
  return {
    __PROJECT__: spec.projectName,
    __ROOT_NAMESPACE__: spec.projectName,
    __DB_NAME__: spec.dbName,
    __AUDIT_DB_NAME__: spec.auditDbName,
    __SQL_SERVER__: spec.server,
    __SQL_USER__: spec.dbUser || '',
    __SQL_PASSWORD__: spec.dbPassword || '',
    __JWT_SECRET__: crypto.randomBytes(64).toString('base64'),
    __ENCRYPTION_PASSPHRASE__: crypto.randomBytes(24).toString('base64'),
    __API_URL__: spec.apiUrl || 'https://localhost:7143',
    __WEB_URL__: spec.webUrl || 'https://localhost:44353',
    __ADMIN_USER__: spec.adminUser,
    __ADMIN_PASSWORD__: spec.adminPassword || '',
    __ADMIN_NAME__: spec.adminName,
    __ADMIN_EMAIL__: spec.adminEmail,
    __BRAND_NAME__: spec.projectName,
    __THEME_STYLE__: spec.themeStyle || 'neo',
    __THEME_PALETTE__: spec.themePalette || 'default',
    __APP_ID__: `com.companyname.${appIdName}.app`,
  };
}

function replaceContent(content, tokens) {
  let result = content;
  for (const [token, value] of Object.entries(tokens)) {
    result = result.split(token).join(value);
  }
  return result;
}

async function replaceTokensInTree(root, tokens) {
  const entries = await fs.readdir(root, { withFileTypes: true });

  for (const entry of entries) {
    const fullPath = path.join(root, entry.name);

    if (entry.isDirectory()) {
      const renamed = path.join(root, replaceContent(entry.name, tokens));
      if (renamed !== fullPath) await fs.move(fullPath, renamed, { overwrite: false });
      await replaceTokensInTree(renamed, tokens);
      continue;
    }

    if (entry.isFile() && isTextFile(fullPath)) {
      const content = await fs.readFile(fullPath, 'utf8');
      const replaced = replaceContent(content, tokens);
      if (replaced !== content) await fs.writeFile(fullPath, replaced, 'utf8');
    }

    const renamed = path.join(root, replaceContent(entry.name, tokens));
    if (renamed !== fullPath) await fs.move(fullPath, renamed, { overwrite: false });
  }
}

async function writeDevelopmentSettings(spec, outputDir) {
  const apiDir = path.join(outputDir, `${spec.projectName}.Api`);
  const quote = (value) => `"${String(value).replace(/"/g, '""')}"`;
  const connection = (database) =>
    `Server=${spec.server};Database=${database};User Id=${spec.dbUser};Password=${quote(spec.dbPassword)};TrustServerCertificate=True;Encrypt=False`;

  const settings = {
    ConnectionStrings: {
      DefaultConnection: connection(spec.dbName),
      AuditLogConnection: connection(spec.auditDbName),
    },
    Seed: {
      Enabled: true,
      AdminUser: spec.adminUser,
      AdminPassword: spec.adminPassword,
      AdminName: spec.adminName,
      AdminEmail: spec.adminEmail,
    },
    Logging: {
      LogLevel: {
        Default: 'Information',
        'Microsoft.AspNetCore': 'Warning',
      },
    },
  };

  await fs.writeFile(
    path.join(apiDir, 'appsettings.Development.json'),
    `${JSON.stringify(settings, null, 2)}\n`,
    'utf8'
  );
}

async function copyLogo(spec, outputDir) {
  if (!spec.logoPath) return;

  const ext = path.extname(spec.logoPath).toLowerCase();
  const targetDir = path.join(outputDir, `${spec.projectName}.Shared`, 'wwwroot', 'images');
  const targetFile = path.join(targetDir, `logo${ext}`);

  await fs.ensureDir(targetDir);
  await fs.copy(spec.logoPath, targetFile, { overwrite: true });
  console.log(pc.gray(`  logo         -> ${path.relative(outputDir, targetFile)}`));
}

async function removeMauiProject(spec, outputDir) {
  const appName = `${spec.projectName}.App`;
  const appDir = path.join(outputDir, appName);
  await fs.remove(appDir);

  const solutionFile = path.join(outputDir, `${spec.projectName}.sln`);
  if (!(await fs.pathExists(solutionFile))) return;

  let content = await fs.readFile(solutionFile, 'utf8');
  const projectPattern = new RegExp(
    `Project\\("\\{[^}]+\\}"\\) = "${appName}", "${appName}\\\\${appName}\\.csproj", "\\{([^}]+)\\}"\\r?\\nEndProject\\r?\\n`,
    'i'
  );
  const match = content.match(projectPattern);
  if (!match) return;

  const guid = match[1];
  content = content.replace(projectPattern, '');
  content = content
    .split(/\r?\n/)
    .filter((line) => !line.includes(guid))
    .join('\n');
  await fs.writeFile(solutionFile, content, 'utf8');
  console.log(pc.gray('  maui         -> removed (--no-maui)'));
}

async function addTestProject(spec, outputDir) {
  const testsName = `${spec.projectName}.Tests`;
  const testsDir = path.join(outputDir, testsName);
  await fs.ensureDir(testsDir);

  await fs.writeFile(
    path.join(testsDir, `${testsName}.csproj`),
    `<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.4" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\\${spec.projectName}.Application\\${spec.projectName}.Application.csproj" />
    <ProjectReference Include="..\\${spec.projectName}.Infrastructure\\${spec.projectName}.Infrastructure.csproj" />
  </ItemGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

</Project>
`,
    'utf8'
  );

  await fs.writeFile(
    path.join(testsDir, 'PasswordHasherTests.cs'),
    `using __PROJECT__.Infrastructure.Identity;

namespace __PROJECT__.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void HashPassword_ThenValidate_Succeeds()
    {
        var hasher = new PasswordHasherService();
        var hash = hasher.HashPassword("Test#12345");

        Assert.True(hasher.ValidatePassword("Test#12345", hash));
    }

    [Fact]
    public void ValidatePassword_WrongPassword_Fails()
    {
        var hasher = new PasswordHasherService();
        var hash = hasher.HashPassword("Test#12345");

        Assert.False(hasher.ValidatePassword("Wrong#12345", hash));
    }
}
`.split('__PROJECT__').join(spec.projectName),
    'utf8'
  );

  await fs.writeFile(
    path.join(testsDir, 'AuthIdleTimeoutTests.cs'),
    `using System.Security.Claims;

using Microsoft.Extensions.Configuration;

using __PROJECT__.Application.DTOs.Auth;
using __PROJECT__.Application.Exceptions;
using __PROJECT__.Application.Interfaces;
using __PROJECT__.Application.Interfaces.Repositories;
using __PROJECT__.Application.Services;
using __PROJECT__.Domain.Entities;

namespace __PROJECT__.Tests;

public class AuthIdleTimeoutTests
{
    [Fact]
    public async Task GetSessionTimeRemainingAsync_ReturnsIdleRemaining()
    {
        var sessionRepo = new FakeSessionRepository(new um_user_app_session
        {
            user_id = "user-1",
            session_id = "session-1",
            last_activity = DateTime.UtcNow.AddMinutes(-5),
        });
        var service = CreateService(sessionRepo: sessionRepo, idleTimeoutMinutes: 15);

        var result = await service.GetSessionTimeRemainingAsync("user-1");

        Assert.InRange(result.IdleRemainingSeconds, 9 * 60, 10 * 60 + 1);
        Assert.Equal(result.IdleRemainingSeconds, result.RemainingSeconds);
        Assert.Equal(15, result.IdleTimeoutMinutes);
    }

    [Fact]
    public async Task RefreshTokenAsync_Throws_WhenIdleExpired()
    {
        var refreshRepo = new FakeRefreshTokenRepository(new um_user_refresh_token
        {
            id = 1,
            user_id = "user-1",
            refresh_token = Hash("refresh-token"),
            expiry_date = DateTime.UtcNow.AddHours(1),
            is_revoked = false,
            remember_me = false,
        });
        var userRepo = new FakeUserRepository(new um_user_app
        {
            user_id = "user-1",
            isActive = true,
            isDeleted = false,
        });
        var flagRepo = new FakeUserFlagRepository(new um_user_app_flag
        {
            user_id = "user-1",
            is_login = true,
            session_id = "session-1",
        });
        var sessionRepo = new FakeSessionRepository(new um_user_app_session
        {
            user_id = "user-1",
            session_id = "session-1",
            last_activity = DateTime.UtcNow.AddMinutes(-30),
        });
        var service = CreateService(
            refreshRepo: refreshRepo,
            userRepo: userRepo,
            flagRepo: flagRepo,
            sessionRepo: sessionRepo,
            idleTimeoutMinutes: 15);

        var ex = await Assert.ThrowsAsync<AuthenticationException>(() => service.RefreshTokenAsync("refresh-token"));

        Assert.Contains("inactivity", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(refreshRepo.StoredToken!.is_revoked);
        Assert.False(flagRepo.Flag!.is_login);
        Assert.Null(flagRepo.Flag.session_id);
        Assert.True(sessionRepo.RemovedUserId == "user-1");
    }

    [Fact]
    public async Task RefreshTokenAsync_Succeeds_WhenIdleActive()
    {
        var refreshRepo = new FakeRefreshTokenRepository(new um_user_refresh_token
        {
            id = 1,
            user_id = "user-1",
            refresh_token = Hash("refresh-token"),
            expiry_date = DateTime.UtcNow.AddHours(1),
            is_revoked = false,
            remember_me = true,
        });
        var userRepo = new FakeUserRepository(new um_user_app
        {
            user_id = "user-1",
            full_name = "Test User",
            isActive = true,
            isDeleted = false,
        });
        var flagRepo = new FakeUserFlagRepository(new um_user_app_flag
        {
            user_id = "user-1",
            is_login = true,
            session_id = "session-1",
        });
        var sessionRepo = new FakeSessionRepository(new um_user_app_session
        {
            user_id = "user-1",
            session_id = "session-1",
            last_activity = DateTime.UtcNow.AddMinutes(-1),
        });
        var groupRepo = new FakeUserGroupRepository(new um_user_group
        {
            user_id = "user-1",
            group_id = "ADMIN",
        });
        var service = CreateService(
            refreshRepo: refreshRepo,
            userRepo: userRepo,
            flagRepo: flagRepo,
            sessionRepo: sessionRepo,
            userGroupRepo: groupRepo,
            idleTimeoutMinutes: 15);

        var result = await service.RefreshTokenAsync("refresh-token");

        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("new-refresh-token", result.RefreshToken);
        Assert.True(refreshRepo.StoredToken!.is_revoked);
        Assert.NotNull(refreshRepo.AddedToken);
        Assert.Equal("user-1", sessionRepo.LastUpsertUserId);
    }

    private static AuthService CreateService(
        FakeRefreshTokenRepository? refreshRepo = null,
        FakeUserRepository? userRepo = null,
        FakeUserFlagRepository? flagRepo = null,
        FakeSessionRepository? sessionRepo = null,
        FakeUserGroupRepository? userGroupRepo = null,
        int idleTimeoutMinutes = 15)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:IdleTimeoutMinutes"] = idleTimeoutMinutes.ToString(),
                ["Jwt:AccessTokenExpirationMinutes"] = "15",
                ["Jwt:RememberMeExpirationDays"] = "30",
                ["Jwt:SessionRefreshTokenHours"] = "12",
            })
            .Build();

        var uow = new FakeUnitOfWork(
            refreshRepo ?? new FakeRefreshTokenRepository(null),
            userRepo ?? new FakeUserRepository(null),
            flagRepo ?? new FakeUserFlagRepository(null),
            sessionRepo ?? new FakeSessionRepository(null),
            userGroupRepo ?? new FakeUserGroupRepository(null));

        return new AuthService(
            uow,
            new FakeJwtService(),
            new FakePasswordHasher(),
            new FakeCurrentUserService(),
            configuration,
            new FakeSessionNotifier());
    }

    private static string Hash(string value)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        return Convert.ToBase64String(bytes);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public FakeUnitOfWork(
            FakeRefreshTokenRepository refreshTokens,
            FakeUserRepository users,
            FakeUserFlagRepository flags,
            FakeSessionRepository sessions,
            FakeUserGroupRepository groups)
        {
            RefreshTokens = refreshTokens;
            UmUserApp = users;
            UmUserAppFlag = flags;
            UmUserAppSession = sessions;
            UmUserGroup = groups;
        }

        public IUmUserAppRepository UmUserApp { get; }
        public IUmUserGroupRepository UmUserGroup { get; }
        public IUmUserAppFlagRepository UmUserAppFlag { get; }
        public IUmUserAppSessionRepository UmUserAppSession { get; }
        public IRefreshTokenRepository RefreshTokens { get; }
        public IUmGroupAppRepository UmGroupApp => throw new NotImplementedException();
        public ITempGroupAppPendingRepository TempGroupAppPending => throw new NotImplementedException();
        public ITempUserAppPendingRepository TempUserAppPending => throw new NotImplementedException();
        public IUmMenuRepository Menus => throw new NotImplementedException();
        public IUmGroupMenuRepository GroupMenus => throw new NotImplementedException();
        public IParameterStagingRepository ParameterStaging => throw new NotImplementedException();
        public IScreenMenuRepository ScreenMenus => throw new NotImplementedException();
        public IPasswordResetTokenRepository PasswordResetTokens => throw new NotImplementedException();
        public IEmailTemplateRepository EmailTemplates => throw new NotImplementedException();
        public IEmailOutboxRepository EmailOutbox => throw new NotImplementedException();
        public ITbNotificationRepository Notifications => throw new NotImplementedException();
        public IRfNotificationTypeRepository NotificationTypes => throw new NotImplementedException();
        public IEmailProviderRepository EmailProviders => throw new NotImplementedException();
        public IRfDefaultValueRepository DefaultValues => throw new NotImplementedException();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
    {
        public FakeRefreshTokenRepository(um_user_refresh_token? token) => StoredToken = token;
        public um_user_refresh_token? StoredToken { get; }
        public um_user_refresh_token? AddedToken { get; private set; }
        public Task<um_user_refresh_token?> GetByTokenHashAsync(string tokenHash)
            => Task.FromResult(StoredToken?.refresh_token == tokenHash ? StoredToken : null);
        public Task AddAsync(um_user_refresh_token entity) { AddedToken = entity; return Task.CompletedTask; }
        public Task UpdateAsync(um_user_refresh_token entity) => Task.CompletedTask;
        public Task RevokeAllUserTokensAsync(string userId) => Task.CompletedTask;
    }

    private sealed class FakeUserRepository : IUmUserAppRepository
    {
        private readonly um_user_app? _user;
        public FakeUserRepository(um_user_app? user) => _user = user;
        public Task<um_user_app?> GetByIdAsync(string userId) => Task.FromResult(_user?.user_id == userId ? _user : null);
        public Task<um_user_app?> FindByIdOrEmailAsync(string identifier, CancellationToken cancellationToken = default) => Task.FromResult<um_user_app?>(null);
        public Task<um_user_app?> GetActiveByIdAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<um_user_app?>(null);
        public Task<bool> ExistsAnyAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> ExistsActiveAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task AddAsync(um_user_app entity) => Task.CompletedTask;
        public Task UpdateAsync(um_user_app entity) => Task.CompletedTask;
    }

    private sealed class FakeUserFlagRepository : IUmUserAppFlagRepository
    {
        public FakeUserFlagRepository(um_user_app_flag? flag) => Flag = flag;
        public um_user_app_flag? Flag { get; }
        public Task<um_user_app_flag?> GetByUserIdAsync(string userId) => Task.FromResult(Flag?.user_id == userId ? Flag : null);
        public Task AddAsync(um_user_app_flag entity) => Task.CompletedTask;
        public Task UpdateAsync(um_user_app_flag entity) => Task.CompletedTask;
    }

    private sealed class FakeSessionRepository : IUmUserAppSessionRepository
    {
        public FakeSessionRepository(um_user_app_session? session) => Session = session;
        public um_user_app_session? Session { get; }
        public string? RemovedUserId { get; private set; }
        public string? LastUpsertUserId { get; private set; }
        public Task<um_user_app_session?> GetByUserIdAsync(string userId) => Task.FromResult(Session?.user_id == userId ? Session : null);
        public Task UpsertAsync(string userId, string? sessionId) { LastUpsertUserId = userId; return Task.CompletedTask; }
        public Task RemoveAsync(string userId) { RemovedUserId = userId; return Task.CompletedTask; }
    }

    private sealed class FakeUserGroupRepository : IUmUserGroupRepository
    {
        private readonly um_user_group? _group;
        public FakeUserGroupRepository(um_user_group? group) => _group = group;
        public Task<um_user_group?> GetByIdAsync(string userId) => Task.FromResult(_group?.user_id == userId ? _group : null);
        public Task<um_user_group?> GetActiveByIdAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<um_user_group?>(null);
        public Task<bool> ExistsActiveByUserAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> ExistsActiveByGroupAsync(string groupId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<List<string>> GetActiveAdministratorUserIdsAsync(CancellationToken cancellationToken = default) => Task.FromResult(new List<string>());
        public Task<string?> GetGroupNameAsync(string groupId) => Task.FromResult<string?>(groupId);
        public Task AddAsync(um_user_group entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(um_user_group entity) => Task.CompletedTask;
    }

    private sealed class FakeJwtService : IJWTService
    {
        public string GenerateAccessToken(um_user_app user, IEnumerable<string> roles, string sessionId) => "access-token";
        public string GenerateRefreshToken() => "new-refresh-token";
        public ClaimsPrincipal? ValidateToken(string token) => null;
        public string? GetUserIdFromToken(string token) => null;
    }

    private sealed class FakePasswordHasher : IPasswordHasherService
    {
        public string HashPassword(string password) => password;
        public bool ValidatePassword(string enteredPassword, string storedHash) => true;
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public string? UserId => "user-1";
        public string? UserName => "user-1";
        public string? Email => null;
        public string? IpAddress => "127.0.0.1";
        public bool IsAuthenticated => true;
    }

    private sealed class FakeSessionNotifier : IUserSessionNotifier
    {
        public Task NotifySessionRevokedAsync(string userId, string? sessionId) => Task.CompletedTask;
        public Task NotifySessionIdleExpiredAsync(string userId, string? sessionId) => Task.CompletedTask;
    }
}
`.split('__PROJECT__').join(spec.projectName),
    'utf8'
  );

  const solutionFile = path.join(outputDir, `${spec.projectName}.sln`);
  await runDotnet(
    ['sln', solutionFile, 'add', path.join(testsName, `${testsName}.csproj`)],
    outputDir,
    { DOTNET_CLI_TELEMETRY_OPTOUT: '1', DOTNET_NOLOGO: '1' }
  );
  console.log(pc.gray(`  tests        -> ${testsName}`));
}

async function addDockerFiles(spec, outputDir) {
  const dockerDir = path.join(outputDir, 'docker');
  await fs.ensureDir(dockerDir);
  const project = spec.projectName;
  const password = spec.dbPassword.replace(/'/g, "''");
  const defaultConnection = `Server=sql,1433;Database=${spec.dbName};User Id=${spec.dbUser};Password=${spec.dbPassword};TrustServerCertificate=True;Encrypt=False`;
  const auditConnection = `Server=sql,1433;Database=${spec.auditDbName};User Id=${spec.dbUser};Password=${spec.dbPassword};TrustServerCertificate=True;Encrypt=False`;

  await fs.writeFile(
    path.join(outputDir, 'Dockerfile.api'),
    `FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "${project}.Api/${project}.Api.csproj"
RUN dotnet publish "${project}.Api/${project}.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "${project}.Api.dll"]
`,
    'utf8'
  );

  await fs.writeFile(
    path.join(outputDir, 'Dockerfile.web'),
    `FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish "${project}.Web/${project}.Web.csproj" -c Release -o /app/publish
RUN sed -i 's#https://localhost:7143#http://localhost:7143#g' /app/publish/wwwroot/appsettings.json

FROM nginx:alpine
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
`,
    'utf8'
  );

  await fs.writeFile(
    path.join(dockerDir, 'nginx.conf'),
    `server {
    listen 80;
    server_name _;
    root /usr/share/nginx/html;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }
}
`,
    'utf8'
  );

  await fs.writeFile(
    path.join(outputDir, '.dockerignore'),
    `**/bin
**/obj
**/node_modules
.git
.vs
`,
    'utf8'
  );

  await fs.writeFile(
    path.join(outputDir, 'docker-compose.yml'),
    `services:
  sql:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: '${password}'
    ports:
      - "1433:1433"
    healthcheck:
      test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -S localhost -U ${spec.dbUser} -P '${password}' -C -Q 'SELECT 1' || exit 1"]
      interval: 10s
      timeout: 5s
      retries: 20

  api:
    build:
      context: .
      dockerfile: Dockerfile.api
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ConnectionStrings__DefaultConnection: '${defaultConnection}'
      ConnectionStrings__AuditLogConnection: '${auditConnection}'
      Seed__Enabled: "true"
      Cors__AllowedOrigins__0: "http://localhost:44353"
    ports:
      - "7143:8080"
    depends_on:
      sql:
        condition: service_healthy

  web:
    build:
      context: .
      dockerfile: Dockerfile.web
    ports:
      - "44353:80"
    depends_on:
      - api
`,
    'utf8'
  );

  console.log(pc.gray('  docker       -> Dockerfile.api, Dockerfile.web, docker-compose.yml'));
}

async function generate(spec, args) {
  const outputDir = path.resolve(process.cwd(), args.outputDir || spec.projectName);
  const cwd = path.resolve(process.cwd());

  if (!(await fs.pathExists(TEMPLATE_DIR))) {
    throw new Error(`Template directory not found: ${TEMPLATE_DIR}`);
  }

  if (outputDir === cwd || cwd.startsWith(outputDir + path.sep)) {
    throw new Error(`Refusing to use a parent/current directory as output: ${outputDir}`);
  }
  if (outputDir === TEMPLATE_DIR || outputDir.startsWith(TEMPLATE_DIR + path.sep)) {
    throw new Error(`Refusing to write inside the template directory: ${outputDir}`);
  }

  if (await fs.pathExists(outputDir)) {
    const entries = await fs.readdir(outputDir);
    if (entries.length > 0) {
      if (!args.force) {
        throw new Error(`Output directory is not empty: ${outputDir}. Use --force to continue.`);
      }
      console.log(pc.yellow('  output       -> cleared (--force)'));
      await fs.emptyDir(outputDir);
    }
  }

  await fs.ensureDir(outputDir);

  console.log(pc.bold('\nGenerating solution...'));
  const excluded = new Set(['bin', 'obj', 'node_modules', '.vs']);
  await fs.copy(TEMPLATE_DIR, outputDir, {
    overwrite: true,
    errorOnExist: false,
    filter: (src) => {
      const relative = path.relative(TEMPLATE_DIR, src);
      if (!relative) return true;
      return !relative.split(path.sep).some((part) => excluded.has(part));
    },
  });
  console.log(pc.gray('  template     -> copied'));

  await replaceTokensInTree(outputDir, buildTokens(spec));
  await writeDevelopmentSettings(spec, outputDir);
  console.log(pc.gray('  tokens       -> replaced'));

  if (!spec.includeMaui) {
    await removeMauiProject(spec, outputDir);
  }

  await copyLogo(spec, outputDir);

  if (spec.includeTests) {
    await addTestProject(spec, outputDir);
  }

  if (spec.includeDocker) {
    await addDockerFiles(spec, outputDir);
  }

  if (!args.skipBuild) {
    console.log(pc.bold('\nBuilding solution...'));
    await buildSolution(spec, outputDir);
    console.log(pc.gray('  build        -> ok'));
  }

  if (spec.includeTests) {
    console.log(pc.bold('\nRunning tests...'));
    const testArgs = ['test', `${spec.projectName}.sln`, '-v:minimal'];
    if (!args.skipBuild) {
      testArgs.push('--no-build');
    }
    await runDotnet(testArgs, outputDir, {
      DOTNET_CLI_TELEMETRY_OPTOUT: '1',
      DOTNET_NOLOGO: '1',
    });
    console.log(pc.gray('  tests        -> ok'));
  }

  if (!args.skipSeed) {
    console.log(pc.bold('\nPreparing databases...'));
    const { mainCreated, auditCreated } = await ensureDatabases(spec);
    console.log(pc.gray(`  main db      -> ${mainCreated ? 'created' : 'exists'}`));
    console.log(pc.gray(`  audit db     -> ${auditCreated ? 'created' : 'exists'}`));

    console.log(pc.bold('\nApplying migrations and seed...'));
    await seedDatabase(spec, outputDir, { noBuild: !args.skipBuild });
    console.log(pc.gray('  seed         -> ok'));

    const procCount = await deployProcedures(spec, outputDir);
    console.log(pc.gray(`  procedures   -> ${procCount} deployed`));
  } else {
    console.log(pc.yellow('  database     -> seed skipped (--skip-seed)'));
  }

  const solutionFile = path.join(outputDir, `${spec.projectName}.sln`);
  if (!(await fs.pathExists(solutionFile))) {
    console.log(pc.yellow('\nTemplate skeleton copied, but the .NET solution is not ready yet.'));
  }

  console.log(pc.bold('\nNext steps'));
  console.log(`  cd ${path.relative(process.cwd(), outputDir)}`);
  console.log(`  dotnet run --project ${spec.projectName}.Api`);
  console.log(`  dotnet run --project ${spec.projectName}.Web`);
  if (spec.includeMaui) {
    console.log(`  dotnet build ${spec.projectName}.App -f net10.0-windows10.0.19041.0`);
  }
}

module.exports = { generate, buildTokens, replaceContent, replaceTokensInTree };
