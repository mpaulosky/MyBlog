# Auth0 Setup Guide

> 🎓 **Training Project** — This guide covers Auth0 integration for MyBlog. Production considerations are called out where relevant.

MyBlog uses Auth0 for authentication and role-based authorization. Two Auth0 applications are required:

1. **Regular Web Application** — handles user login/logout via the Blazor Server app
2. **Machine-to-Machine (M2M) Application** — calls the Auth0 Management API to read and assign user roles

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Create the Regular Web Application](#create-the-regular-web-application)
3. [Create the M2M Application for the Management API](#create-the-m2m-application-for-the-management-api)
4. [Create Author and Admin Roles](#create-author-and-admin-roles)
5. [Local Development Configuration](#local-development-configuration)
6. [Assign Roles to Users](#assign-roles-to-users)
7. [Testing the Setup](#testing-the-setup)
8. [Troubleshooting](#troubleshooting)

---

## Prerequisites

- **.NET 10 SDK** — [Download](https://dotnet.microsoft.com/en-us/download)
- **Auth0 account** — [Sign up free](https://auth0.com/signup) (the free tier is sufficient for this training project)
- A cloned and building copy of the MyBlog repository (see [Getting Started](../README.md#getting-started))

---

## Create the Regular Web Application

This application handles the browser-facing login/logout flow.

### 1. Create the Application

1. Log in to the [Auth0 Dashboard](https://manage.auth0.com/).
2. Go to **Applications → Applications** in the left sidebar.
3. Click **Create Application**.
4. Enter a name — e.g., `MyBlog Web`.
5. Select **Regular Web Applications**.
6. Click **Create**.

### 2. Note Your Credentials

On the application's **Settings** tab, note:

| Setting | Where to find it |
|---|---|
| **Domain** | e.g., `your-tenant.us.auth0.com` |
| **Client ID** | Alphanumeric string |
| **Client Secret** | Click **Show** to reveal |

### 3. Configure Callback and Logout URLs

Still on the **Settings** tab, scroll to **Application URIs**:

| Field | Value |
|---|---|
| **Allowed Callback URLs** | `https://localhost:7172/signin-auth0` |
| **Allowed Logout URLs** | `https://localhost:7172/` |

> **Port 7172** is the HTTPS port defined in `src/Web/Properties/launchSettings.json`.
> If you change it, update these URLs to match.

Click **Save Changes**.

---

## Create the M2M Application for the Management API

MyBlog reads and assigns Auth0 roles via the Management API. A separate M2M application handles this server-to-server call.

> 🎓 **Training note** — In a training project it's acceptable to call the Management API on every request. In production you would cache the M2M access token (it expires after 24 hours by default) and use a library such as `Auth0.ManagementApi` with a token cache.

### 1. Create the M2M Application

1. Go to **Applications → Applications**.
2. Click **Create Application**.
3. Enter a name — e.g., `MyBlog Management API`.
4. Select **Machine to Machine Applications**.
5. Click **Create**.

### 2. Authorize the Management API

After creation, the dashboard shows a prompt to select an API:

1. Select **Auth0 Management API** from the dropdown.
2. Expand the scopes and enable **all of the following**:

   | Scope | Purpose |
   |---|---|
   | `read:users` | Read user profile data |
   | `read:roles` | List available roles |
   | `update:users` | Assign/remove roles on a user |
   | `create:role_members` | Add a user to a role |
   | `delete:role_members` | Remove a user from a role |

3. Click **Authorize**.

### 3. Note Your M2M Credentials

On the M2M application's **Settings** tab, note:

| Setting | Where to find it |
|---|---|
| **Client ID** | Alphanumeric string |
| **Client Secret** | Click **Show** to reveal |

The **Domain** is the same as your Regular Web Application.

---

## Create Author and Admin Roles

1. In the Auth0 Dashboard, go to **User Management → Roles**.
2. Click **Create Role** and create the following two roles:

   | Role Name | Description |
   |---|---|
   | `Author` | Can create, edit, and delete blog posts |
   | `Admin` | Can do everything Author can, plus manage user roles |

   > Role names are **case-sensitive**. Use exactly `Author` and `Admin`.

---

## Local Development Configuration

### appsettings.json (safe to commit)

Add the non-secret Auth0 settings to `src/Web/appsettings.json`. These values are not sensitive:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Auth0": {
    "Domain": "YOUR_AUTH0_DOMAIN",
    "ClientId": "YOUR_CLIENT_ID"
  }
}
```

Replace `YOUR_AUTH0_DOMAIN` and `YOUR_CLIENT_ID` with the values from your Regular Web Application.

### User Secrets (never committed to git)

Secrets such as `ClientSecret` and the M2M credentials must **not** go in `appsettings.json`. Use the .NET User Secrets system instead.

#### Step 1 — Initialize user secrets for the Web project

```bash
cd src/Web
dotnet user-secrets init
```

This adds a `UserSecretsId` to `Web.csproj` and creates a local secrets store tied to that ID.

#### Step 2 — Set each secret

```bash
dotnet user-secrets set "Auth0:ClientSecret"              "YOUR_CLIENT_SECRET"
dotnet user-secrets set "Auth0:ManagementApiDomain"       "YOUR_AUTH0_DOMAIN"
dotnet user-secrets set "Auth0:ManagementApiClientId"     "YOUR_M2M_CLIENT_ID"
dotnet user-secrets set "Auth0:ManagementApiClientSecret" "YOUR_M2M_CLIENT_SECRET"
```

Replace each placeholder with the real value from the Auth0 Dashboard.

#### Verify the secrets are set

```bash
dotnet user-secrets list
```

Expected output (values will differ):

```
Auth0:ClientSecret = ***
Auth0:ManagementApiDomain = your-tenant.us.auth0.com
Auth0:ManagementApiClientId = ***
Auth0:ManagementApiClientSecret = ***
```

> **Where are secrets stored?**
> On Linux/macOS: `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json`
> On Windows: `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`
> These files are local to your machine and never committed to git.

---

## Assign Roles to Users

Before you can test role-based access, assign one of the roles to a user in the Auth0 Dashboard.

1. Go to **User Management → Users**.
2. Click on the user you want to assign a role to.
3. Select the **Roles** tab.
4. Click **Assign Roles**.
5. Select `Author` or `Admin` from the dropdown and click **Assign**.

> You can assign the same user both roles if needed during testing.

---

## Testing the Setup

### 1. Run the application

```bash
cd src/AppHost
dotnet run
```

The Aspire dashboard URL will appear in the console. The Blazor app runs at `https://localhost:7172`.

### 2. Test login

1. Navigate to `https://localhost:7172`.
2. Click the login link.
3. You should be redirected to your Auth0 Universal Login page.
4. Sign in with a test user.
5. After successful login you should be redirected back to `https://localhost:7172`.

### 3. Test role-based access

- Log in with a user that has the `Author` role — blog post management actions should be available.
- Log in with a user that has the `Admin` role — all Author actions plus user role management should be available.
- Log in with a user that has **no role** — protected actions should be hidden or return a 403.

### 4. Common verification checks

| Check | How |
|---|---|
| Login redirect works | Browser reaches Auth0 login page |
| Callback succeeds | Redirected back without errors after login |
| Logout works | Session cleared, redirected to home |
| Role claims present | Inspect `User.Claims` in a debug breakpoint or log them |

---

## Troubleshooting

### `Callback URL mismatch` error from Auth0

**Cause**: The URL that Auth0 redirected back to is not in the **Allowed Callback URLs** list.

**Fix**: Check that `https://localhost:7172/signin-auth0` is listed exactly (including the path) in your Regular Web Application's settings. Make sure the port matches `launchSettings.json`.

---

### `Client authentication failed` / 401 from the Management API

**Cause**: The M2M Client ID or Client Secret is wrong, or the M2M app has not been authorized against the Management API.

**Fix**:
1. Re-check `dotnet user-secrets list` to confirm the secrets are set.
2. In the Auth0 Dashboard, open the M2M application → **APIs** tab and confirm the Management API is listed as authorized with the required scopes.

---

### Roles are not appearing in user claims

**Cause**: By default Auth0 does not include roles in the ID token. You may need an Auth0 Action or Rule to add them.

**Fix**: In the Auth0 Dashboard, go to **Actions → Flows → Login** and add an action that injects roles into the token:

```javascript
exports.onExecutePostLogin = async (event, api) => {
  const namespace = 'https://myblog/';
  if (event.authorization) {
    api.idToken.setCustomClaim(`${namespace}roles`, event.authorization.roles);
    api.accessToken.setCustomClaim(`${namespace}roles`, event.authorization.roles);
  }
};
```

> The namespace must be a URL format (`https://...`). It does not need to resolve anywhere.
> Update the claim-reading code in the application to use the same namespace prefix.

---

### `dotnet user-secrets` command not found

**Cause**: The .NET SDK is not installed or not on `PATH`.

**Fix**: Install the [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download) and confirm with `dotnet --version`.

---

### Application starts but login redirects to wrong port

**Cause**: Running under the `http` profile instead of `https`.

**Fix**: Run with the HTTPS profile explicitly:

```bash
dotnet run --launch-profile https
```

Or run through the AppHost (Aspire), which uses the configured HTTPS URL.

---

## Related Documentation

- [ARCHITECTURE.md](ARCHITECTURE.md) — Solution structure and design decisions
- [SECURITY.md](SECURITY.md) — Security policy
- [Auth0 .NET Quickstart](https://auth0.com/docs/quickstart/webapp/aspnet-core) — Official Auth0 guide for ASP.NET Core
- [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) — Microsoft documentation
