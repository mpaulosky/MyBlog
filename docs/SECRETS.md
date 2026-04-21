# Secrets Management

This document describes how to securely manage secrets for IssueTrackerApp across different environments.

## Overview

The application uses a layered configuration system where secrets are **never** stored in source control:

| Environment | Secret Storage            | Access Method             |
|-------------|---------------------------|---------------------------|
| Development | .NET User Secrets         | `dotnet user-secrets` CLI |
| CI/CD       | GitHub Repository Secrets | Environment variables     |
| Production  | Azure Key Vault           | Managed Identity          |

## Required Secrets

| Secret                         | Description                                     | Required      |
|------------------------------- |-------------------------------------------------|-------------- |
| `Auth0:Domain`                 | Auth0 tenant domain (e.g., `tenant.auth0.com`)  | ✅ Yes        |
| `Auth0:ClientId`               | Auth0 application client ID                     | ✅ Yes        |
| `Auth0:ClientSecret`           | Auth0 application client secret                 | ✅ Yes        |
| `MongoDB:ConnectionString`     | MongoDB Atlas connection string                 | ✅ Production |
| `SendGrid:ApiKey`              | SendGrid API key for email                      | Optional      |
| `BlobStorage:ConnectionString` | Azure Blob Storage connection                   | Optional      |

---

## Development Setup (User Secrets)

### Prerequisites

- .NET SDK 10.0+ installed
- Terminal/PowerShell open at the repository root

### Step 1: Navigate to the Web Project

User secrets are project-specific. You must run commands from the **Web project directory**:

```powershell
# From the repository root (E:\github\IssueTrackerApp)
cd src\Web
```

### Step 2: Verify User Secrets Is Configured

The Web.csproj already has a `UserSecretsId`. Verify it exists:

```powershell
# Should show: <UserSecretsId>issuetracker-web-secrets</UserSecretsId>
Select-String -Path Web.csproj -Pattern "UserSecretsId"
```

### Step 3: Set Your Auth0 Secrets

Get your Auth0 credentials from the [Auth0 Dashboard](https://manage.auth0.com/):

1. Go to **Applications → Applications → Your App**
2. Copy Domain, Client ID, and Client Secret

```powershell
# Still in src\Web directory
dotnet user-secrets set "Auth0:Domain" "YOUR-TENANT.us.auth0.com"
dotnet user-secrets set "Auth0:ClientId" "YOUR_CLIENT_ID_HERE"
dotnet user-secrets set "Auth0:ClientSecret" "YOUR_CLIENT_SECRET_HERE"
```

### Step 4: Set MongoDB Connection (Optional for Local Dev)

For **local development**, .NET Aspire runs MongoDB in a container automatically — no connection string needed!

For **MongoDB Atlas** (cloud database), get your connection string from [MongoDB Atlas](https://cloud.mongodb.com/):

1. Go to **Database → Connect → Drivers**
2. Copy the connection string (replace `<password>` with your actual password)

```powershell
dotnet user-secrets set "MongoDB:ConnectionString" "mongodb+srv://username:password@cluster.mongodb.net/issuetracker-db?retryWrites=true&w=majority"
```

### Step 5: Verify Your Secrets

```powershell
# List all secrets (still in src\Web)
dotnet user-secrets list
```

Expected output:

```text
Auth0:ClientId = abc123...
Auth0:ClientSecret = xyz789...
Auth0:Domain = your-tenant.us.auth0.com
MongoDB:ConnectionString = mongodb+srv://...
```

### Where Are Secrets Stored?

Secrets are stored **outside** the repository in your user profile:

| OS      | Location                                                                           |
|---------|------------------------------------------------------------------------------------|
| Windows | `%APPDATA%\Microsoft\UserSecrets\issuetracker-web-secrets\secrets.json`            |
| macOS   | `~/.microsoft/usersecrets/issuetracker-web-secrets/secrets.json`                   |
| Linux   | `~/.microsoft/usersecrets/issuetracker-web-secrets/secrets.json`                   |

You can view/edit this file directly if needed.

---

## CI/CD Setup (GitHub Actions)

### Required Repository Secrets

Go to **Settings → Secrets and variables → Actions** and add:

| Secret Name                 | Value                                               |
|-----------------------------|-----------------------------------------------------|
| `AUTH0_DOMAIN`              | Your Auth0 tenant domain                            |
| `AUTH0_CLIENT_ID`           | Your Auth0 client ID                                |
| `AUTH0_CLIENT_SECRET`       | Your Auth0 client secret                            |
| `MONGODB_CONNECTION_STRING` | MongoDB Atlas connection string                     |
| `AZURE_CREDENTIALS`         | Azure service principal JSON (for Key Vault access) |

### Workflow Usage

Secrets are injected as environment variables in workflows:

```yaml
env:
  Auth0__Domain: ${{ secrets.AUTH0_DOMAIN }}
  Auth0__ClientId: ${{ secrets.AUTH0_CLIENT_ID }}
  Auth0__ClientSecret: ${{ secrets.AUTH0_CLIENT_SECRET }}
  MongoDB__ConnectionString: ${{ secrets.MONGODB_CONNECTION_STRING }}
```

> **Note:** Use double underscores (`__`) for nested configuration in environment variables.

---

## Production Setup (Azure Key Vault)

### Prerequisites for Azure

- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) installed
- Logged in: `az login`
- An Azure subscription

### Step 1: Create a Resource Group (if needed)

```bash
az group create \
  --name issuetracker-rg \
  --location eastus
```

### Step 2: Create the Key Vault

Key Vault names must be globally unique (3-24 characters, alphanumeric and hyphens only):

```bash
az keyvault create \
  --name issuetracker-kv \
  --resource-group issuetracker-rg \
  --location eastus \
  --sku standard
```

### Step 3: Add Secrets to Key Vault

Azure Key Vault uses `--` as the section separator (translated to `:` by .NET):

```bash
# Auth0 secrets
az keyvault secret set --vault-name issuetracker-kv --name "Auth0--Domain" --value "your-tenant.us.auth0.com"
az keyvault secret set --vault-name issuetracker-kv --name "Auth0--ClientId" --value "your-client-id"
az keyvault secret set --vault-name issuetracker-kv --name "Auth0--ClientSecret" --value "your-client-secret"

# MongoDB Atlas connection string
az keyvault secret set --vault-name issuetracker-kv --name "MongoDB--ConnectionString" --value "mongodb+srv://user:pass@cluster.mongodb.net/issuetracker-db"

# Optional: SendGrid
az keyvault secret set --vault-name issuetracker-kv --name "SendGrid--ApiKey" --value "SG.your-api-key"
```

### Step 4: Deploy Your App (Azure Container Apps Example)

```bash
# Create Container Apps environment
az containerapp env create \
  --name issuetracker-env \
  --resource-group issuetracker-rg \
  --location eastus

# Deploy the app with managed identity enabled
az containerapp create \
  --name issuetracker-app \
  --resource-group issuetracker-rg \
  --environment issuetracker-env \
  --image your-registry/issuetracker:latest \
  --target-port 8080 \
  --ingress external \
  --system-assigned
```

### Step 5: Grant Key Vault Access to Your App

```bash
# Get the app's managed identity principal ID
PRINCIPAL_ID=$(az containerapp identity show \
  --name issuetracker-app \
  --resource-group issuetracker-rg \
  --query principalId -o tsv)

# Grant the identity permission to read secrets
az keyvault set-policy \
  --name issuetracker-kv \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list
```

### Step 6: Configure the App to Use Key Vault

```bash
# Set the Key Vault URI as an environment variable
az containerapp update \
  --name issuetracker-app \
  --resource-group issuetracker-rg \
  --set-env-vars "KeyVault__Uri=https://issuetracker-kv.vault.azure.net/"
```

### Alternative: Azure App Service

For App Service instead of Container Apps:

```bash
# Enable managed identity
az webapp identity assign --name issuetracker-app --resource-group issuetracker-rg

# Get principal ID
PRINCIPAL_ID=$(az webapp identity show --name issuetracker-app --resource-group issuetracker-rg --query principalId -o tsv)

# Grant Key Vault access (same as above)
az keyvault set-policy --name issuetracker-kv --object-id $PRINCIPAL_ID --secret-permissions get list

# Set Key Vault URI in app settings
az webapp config appsettings set --name issuetracker-app --resource-group issuetracker-rg --settings KeyVault__Uri=https://issuetracker-kv.vault.azure.net/
```

---

## SendGrid Setup

SendGrid is used for email notifications (issue assignments, status changes, comments). This section covers how to create a SendGrid account, generate an API key, and configure it for IssueTrackerApp.

### Step 1: Create a SendGrid Account

1. Go to [SendGrid Signup](https://signup.sendgrid.com/)
2. Create a free account (100 emails/day free tier)
3. Complete email verification and account setup

### Step 2: Create a Sender Identity

Before sending emails, you must verify a sender:

1. Go to **Settings → Sender Authentication**
2. Choose one of:
   - **Single Sender Verification** (easiest for dev) — Verify one email address
   - **Domain Authentication** (recommended for production) — Verify your domain via DNS

For Single Sender:

1. Click **Create a Sender**
2. Fill in your details (From Name, From Email, etc.)
3. Click the verification link sent to that email

### Step 3: Generate an API Key

1. Go to **Settings → API Keys**
2. Click **Create API Key**
3. Enter a name: `IssueTrackerApp`
4. Choose **Restricted Access** and enable:
   - **Mail Send → Full Access**
5. Click **Create & View**
6. **Copy the key immediately** — it won't be shown again!

The API key looks like: `SG.xxxxxx.yyyyyy` (starts with `SG.`)

### Step 4: Configure the Secret

#### Development (User Secrets)

```powershell
# From src\Web directory
dotnet user-secrets set "SendGrid:ApiKey" "SG.your-api-key-here"
dotnet user-secrets set "SendGrid:FromEmail" "noreply@yourdomain.com"
dotnet user-secrets set "SendGrid:FromName" "IssueTracker"
```

#### GitHub Actions

Add to repository secrets (**Settings → Secrets → Actions**):

| Secret Name         | Value                        |
|---------------------|------------------------------|
| `SENDGRID_API_KEY`  | `SG.your-api-key`            |
| `SENDGRID_FROM`     | `noreply@yourdomain.com`     |

#### Azure Key Vault

```bash
az keyvault secret set --vault-name issuetracker-kv --name "SendGrid--ApiKey" --value "SG.your-api-key"
az keyvault secret set --vault-name issuetracker-kv --name "SendGrid--FromEmail" --value "noreply@yourdomain.com"
az keyvault secret set --vault-name issuetracker-kv --name "SendGrid--FromName" --value "IssueTracker"
```

### SendGrid Configuration in appsettings.json

The base configuration (no secrets) should look like:

```json
{
  "SendGrid": {
    "ApiKey": "",
    "FromEmail": "",
    "FromName": "IssueTracker"
  }
}
```

### Testing Your SendGrid Setup

After configuring, test email sending:

1. Run the application locally
2. Create an issue and assign it to a user with a valid email
3. Check the SendGrid **Activity Feed** for sent emails
4. Check spam folder if email doesn't arrive

### Troubleshooting SendGrid

| Problem | Solution |
|---------|----------|
| "Unauthorized" error | API key is invalid or missing — check `SendGrid:ApiKey` is set |
| "Forbidden" error | API key doesn't have Mail Send permission — regenerate with correct scopes |
| Emails not arriving | Check sender is verified; check recipient's spam folder |
| "From address not verified" | Complete Sender Authentication in SendGrid dashboard |
| Rate limited | Free tier allows 100/day — upgrade or wait 24 hours |

### SendGrid Best Practices

- **Use a subdomain** for sending (e.g., `mail.yourdomain.com`) to protect your main domain reputation
- **Set up domain authentication** for production to improve deliverability
- **Monitor the Activity Feed** to track bounces and blocks
- **Use templates** for consistent email formatting (SendGrid Dynamic Templates)
- **Don't share API keys** between environments — create separate keys for dev/staging/prod

### Alternative: SMTP Configuration

If you prefer SMTP over the SendGrid API, you can use SendGrid's SMTP relay:

```powershell
dotnet user-secrets set "Email:SmtpHost" "smtp.sendgrid.net"
dotnet user-secrets set "Email:SmtpPort" "587"
dotnet user-secrets set "Email:SmtpUsername" "apikey"
dotnet user-secrets set "Email:SmtpPassword" "SG.your-api-key"
dotnet user-secrets set "Email:FromEmail" "noreply@yourdomain.com"
```

> **Note:** For SMTP, the username is literally the word `apikey`, and the password is your API key.

---

## Configuration Hierarchy

.NET loads configuration in this order (later sources override earlier):

1. `appsettings.json` — Base defaults (empty strings for secrets)
2. `appsettings.{Environment}.json` — Environment-specific non-secrets
3. User Secrets — Development only
4. Azure Key Vault — Non-Development environments
5. Environment Variables — Highest priority (CI/CD, containers)

---

## Security Best Practices

### DO ✅

- Use User Secrets for local development
- Use Azure Key Vault for production secrets
- Use GitHub Repository Secrets for CI/CD
- Enable Managed Identity for Azure services
- Rotate secrets regularly
- Use separate secrets per environment

### DON'T ❌

- Commit secrets to source control
- Use placeholder values that look like real credentials
- Share development secrets across team members
- Store secrets in appsettings.json files
- Log secrets in application logs

---

## Troubleshooting

### "User secrets not found" or secrets not loading

1. **Are you in the right directory?** Run `pwd` — you should be in `src/Web`
2. **Does UserSecretsId exist?** Check Web.csproj for `<UserSecretsId>`
3. **Is ASPNETCORE_ENVIRONMENT set?** User secrets only load in Development mode
4. **List your secrets:** `dotnet user-secrets list` (run from `src/Web`)

### Key Vault not loading in production

1. **Is KeyVault:Uri set?** Check your app's environment variables
2. **Is Managed Identity enabled?** `az containerapp identity show --name your-app --resource-group your-rg`
3. **Does the identity have access?** Check Key Vault access policies in Azure Portal
4. **Are secret names correct?** Use `--` not `:` (e.g., `Auth0--Domain` not `Auth0:Domain`)

### Auth0 authentication failing

1. **Are all three secrets set?** Domain, ClientId, AND ClientSecret are required
2. **Is the callback URL configured?** In Auth0 Dashboard → Application → Allowed Callback URLs, add:
   - Development: `https://localhost:5001/callback`
   - Production: `https://your-domain.com/callback`
3. **Is it a "Regular Web Application"?** Not SPA or Native

### MongoDB connection failing

1. **Is your IP whitelisted?** In MongoDB Atlas → Network Access, add your IP or `0.0.0.0/0` for testing
2. **Is the password URL-encoded?** Special characters like `@` or `#` must be encoded
3. **Is the database name correct?** Check the connection string includes `/issuetracker-db`

---

## Quick Reference

### Commands Summary

```powershell
# Navigate to Web project (required for user-secrets commands)
cd src\Web

# Set a secret
dotnet user-secrets set "Section:Key" "value"

# List all secrets
dotnet user-secrets list

# Remove a secret
dotnet user-secrets remove "Section:Key"

# Clear all secrets
dotnet user-secrets clear
```

### Secret Name Formats

| Context             | Format Example              |
|---------------------|-----------------------------|
| User Secrets / Code | `Auth0:Domain`              |
| Environment Vars    | `Auth0__Domain`             |
| Azure Key Vault     | `Auth0--Domain`             |

---

## Related Documentation

- [Auth0 Setup Guide](../src/Web/Auth/README.md)
- [Azure Key Vault Documentation](https://learn.microsoft.com/azure/key-vault/)
- [.NET User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets)
- [Azure Container Apps](https://learn.microsoft.com/azure/container-apps/)
- [Managed Identity](https://learn.microsoft.com/azure/active-directory/managed-identities-azure-resources/)
