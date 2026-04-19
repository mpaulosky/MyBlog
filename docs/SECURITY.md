# Security Policy

## Supported Versions

The following versions of MyBlog are currently supported with security updates:

| Version | Supported          |
| ------- | ------------------ |
| 0.1.x   | :white_check_mark: |
| < 0.1   | :x:                |

**Note:** This is an early-stage project. Security updates will be provided for the latest 0.1.x release. Once the project reaches 1.0, we will maintain security support for the current major version and one previous major version.

## Security Features

MyBlog implements the following security measures:

### Authentication & Authorization

- **Auth0 integration** - Enterprise-grade authentication and identity management
- **Role-based access control (RBAC)** - Author and Admin roles enforced on all protected routes
- **Admin authorization enforcement** - User management and other admin-only UI functionality are guarded via the `Admin` role
- **Principle of least privilege** - Management API scopes limited to required operations only

### Data Protection

- **CSRF protection** - `UseAntiforgery()` middleware enabled in ASP.NET Core pipeline
- **HTTPS enforcement** - Redirected on all requests; HSTS enabled in production
- **Blazor automatic HTML encoding** - Razor components automatically encode output to prevent injection attacks

### API Security

- **Auth0 Management API secrets protection** - M2M credentials stored only in user secrets or environment variables; never committed to source control
- **No raw secret logging** - Secrets never logged or echoed, even in debug paths
- **Error handling** - Auth0 errors wrapped in Result objects for consistent handling; user-facing sanitization of error details depends on the calling layer

### MongoDB Security

- **Connection security** - MongoDB connection managed through Aspire service container; credentials stored in environment configuration
- **Typed query API** - EF Core MongoDB access uses LINQ and strongly typed operations rather than string-concatenated query language
- **User authorization** - All blog post operations guard against unauthorized access via authorization policies

## Auth0 Secrets Management Policy

**CRITICAL: Auth0 Management API secrets must NEVER appear in source code or committed config files.**

### Local Development

After creating an M2M (Machine-to-Machine) application in Auth0, store secrets using .NET User Secrets:

```bash
dotnet user-secrets set "Auth0:ManagementApiDomain"       "your-tenant.us.auth0.com"
dotnet user-secrets set "Auth0:ManagementApiClientId"     "YOUR_M2M_CLIENT_ID"
dotnet user-secrets set "Auth0:ManagementApiClientSecret" "YOUR_M2M_CLIENT_SECRET"
```

Secrets are stored locally at `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json` (Linux/macOS) and `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json` (Windows), and are never committed to git.

### CI/CD Environments

- GitHub Actions secrets: `AUTH0_MANAGEMENT_CLIENT_ID`, `AUTH0_MANAGEMENT_CLIENT_SECRET` (configured via repository settings)
- Pass secrets to workflow steps via `${{ secrets.AUTH0_MANAGEMENT_CLIENT_ID }}`
- Never log, echo, or expose secrets in workflow logs, even conditionally

### Configuration Files

- **Safe to commit:** `appsettings.json` with non-secret Auth0 settings (Domain, ClientId only)
- **Never commit:** `ClientSecret`, M2M credentials, or any sensitive configuration
- See `.squad/skills/auth0-management-security/SKILL.md` and `docs/AUTH0_SETUP.md` for implementation details

---

## Reporting a Vulnerability

If you discover a security vulnerability in MyBlog, please report it responsibly:

### How to Report

**Email:** <matthew.paulosky@outlook.com>  
**Subject:** [SECURITY] MyBlog Vulnerability Report

**Please do NOT open a public GitHub issue for security vulnerabilities.**

### What to Include

When reporting a security vulnerability, please include:

1. **Description** - Clear description of the vulnerability
2. **Impact** - Potential security impact and severity
3. **Steps to Reproduce** - Detailed steps to reproduce the vulnerability
4. **Affected Versions** - Which versions are affected
5. **Suggested Fix** - If you have ideas for mitigation (optional)
6. **Your Contact Info** - How we can reach you for follow-up

### Response Timeline

- **Initial Response:** Within 48 hours of report submission
- **Status Update:** Within 7 days with assessment and timeline
- **Fix Timeline:**
  - Critical vulnerabilities: Within 7 days
  - High severity: Within 14 days
  - Medium/Low severity: Within 30 days

### Disclosure Policy

- We will work with you to understand and validate the vulnerability
- We will develop and test a fix before public disclosure
- We will credit you in the security advisory (unless you prefer anonymity)
- We request that you do not publicly disclose the vulnerability until we have released a fix

### Security Advisories

Security updates will be published:

- In the [GitHub Security Advisories](https://github.com/mpaulosky/MyBlog/security/advisories)
- In the project [CHANGELOG.md](../CHANGELOG.md) (if one exists)
- In release notes for security-related releases

## Security Best Practices for Contributors

When contributing to MyBlog, please follow these security guidelines:

### Code Review

- All code changes require review before merging
- Security-sensitive changes require additional scrutiny
- Never commit secrets, API keys, or passwords

### Testing

- Add security-focused tests for authorization checks
- Test boundary conditions and edge cases
- Verify user isolation in integration tests

### Dependencies

- Keep NuGet packages up to date
- Review dependency security advisories
- Use `dotnet list package --vulnerable` to check for known vulnerabilities

### Secrets Management

- Use **User Secrets** for local development (`dotnet user-secrets`)
- Use **Environment Variables** for production
- Store all sensitive configuration outside of source control

### Data Validation

- Validate all user input at the domain model level (e.g., `ArgumentException.ThrowIfNullOrWhiteSpace`)
- Sanitize data before rendering in Blazor components (Blazor does this automatically)

## Known Security Considerations

### Current Limitations

- **No Auth0 rate limiting caching** - Every role query or assignment hits the Auth0 Management API (adequate for current scale; plan caching for production)
- **No structured audit logging** - Admin role operations are not currently logged; planned for future release
- **No input length limits** - Blog post title and content fields lack length constraints
- **No rate limiting** - Consider implementing API rate limiting for production

### Recommendations for Production

1. **Use HTTPS** - Enable HTTPS and HSTS
2. **Secure connection strings** - Use Azure Key Vault or similar
3. **Enable logging** - Add security event logging
4. **Rate limiting** - Implement API rate limiting
5. **Regular updates** - Keep .NET and dependencies updated
6. **Security headers** - Add security headers (CSP, X-Frame-Options, etc.)
7. **Monitor dependencies** - Use GitHub Dependabot for security alerts

## Related Documentation

- **[AUTH0_SETUP.md](AUTH0_SETUP.md)** - Complete Auth0 configuration guide for developers
- **[.squad/skills/auth0-management-security/SKILL.md](../.squad/skills/auth0-management-security/SKILL.md)** - Auth0 secrets management and security patterns
- **[CONTRIBUTING.md](../CONTRIBUTING.md)** - Contributor workflow and code review process

## Security Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [ASP.NET Core Security Best Practices](https://learn.microsoft.com/aspnet/core/security/)
- [Blazor Security](https://learn.microsoft.com/aspnet/core/blazor/security/)
- [Auth0 Security Best Practices](https://auth0.com/docs/secure/security-guidance)
- [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)

---

Thank you for helping keep MyBlog secure!
