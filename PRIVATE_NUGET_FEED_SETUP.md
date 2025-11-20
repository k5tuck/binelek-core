# Private NuGet Feed Setup - GitHub Packages

**Platform:** Binelek
**Feed Provider:** GitHub Packages
**Feed Type:** Private (requires authentication)

---

## Overview

All Binelek shared libraries (Binah.Core, Binah.Contracts, Binah.Infrastructure, Binah.Domain) are published to **GitHub Packages** as a private NuGet feed.

**Feed URL:**
```
https://nuget.pkg.github.com/k5tuck/index.json
```

---

## One-Time Setup (For Developers)

### Step 1: Create GitHub Personal Access Token (PAT)

1. Go to: https://github.com/settings/tokens
2. Click **"Generate new token (classic)"**
3. Give it a name: `Binelek NuGet Feed`
4. Select scopes:
   - ✅ `read:packages` - Read packages from GitHub Packages
   - ✅ `write:packages` - Publish packages to GitHub Packages (if you'll be publishing)
   - ✅ `repo` - Access private repositories (if using private repos)
5. Click **"Generate token"**
6. **Copy the token immediately** (you won't see it again)

### Step 2: Add GitHub Packages as NuGet Source

**Option A: Command Line (Recommended)**

```bash
# Add the GitHub Packages feed
dotnet nuget add source https://nuget.pkg.github.com/k5tuck/index.json \
  --name github \
  --username YOUR_GITHUB_USERNAME \
  --password YOUR_GITHUB_PAT \
  --store-password-in-clear-text

# Verify it was added
dotnet nuget list source
```

**Option B: Visual Studio (Windows)**

1. Tools → NuGet Package Manager → Package Manager Settings
2. Package Sources → Click **+** to add new source
3. Name: `GitHub Packages`
4. Source: `https://nuget.pkg.github.com/k5tuck/index.json`
5. Username: Your GitHub username
6. Password: Your GitHub PAT
7. Click **Update** → **OK**

**Option C: NuGet.Config (Per-Project)**

Create `NuGet.Config` in your solution folder:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="github" value="https://nuget.pkg.github.com/k5tuck/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <github>
      <add key="Username" value="YOUR_GITHUB_USERNAME" />
      <add key="ClearTextPassword" value="YOUR_GITHUB_PAT" />
    </github>
  </packageSourceCredentials>
</configuration>
```

⚠️ **Important:** Add `NuGet.Config` to `.gitignore` to avoid committing your PAT!

---

## Installing Binelek Packages

Once configured, install packages normally:

```bash
# Install all Binelek shared libraries
dotnet add package Binah.Core --source github
dotnet add package Binah.Contracts --source github
dotnet add package Binah.Infrastructure --source github
dotnet add package Binah.Domain --source github
```

Or let NuGet auto-discover:

```bash
dotnet restore
```

---

## Publishing Packages (For Maintainers)

### Automatic Publishing (Recommended)

1. **Create a release in binelek-shared repository:**
   ```bash
   # Tag and push
   git tag v1.0.0
   git push origin v1.0.0
   ```

2. **Create GitHub Release:**
   - Go to: https://github.com/k5tuck/binelek-shared/releases/new
   - Choose tag: `v1.0.0`
   - Title: `Release 1.0.0`
   - Description: Release notes
   - Click **"Publish release"**

3. **GitHub Actions automatically:**
   - Builds all libraries
   - Runs tests
   - Packs NuGet packages
   - Publishes to GitHub Packages

### Manual Publishing

```bash
# Navigate to binelek-shared
cd binelek-shared

# Pack all projects
dotnet pack --configuration Release -p:PackageVersion=1.0.0

# Publish to GitHub Packages
dotnet nuget push ./nupkgs/*.nupkg \
  --source https://nuget.pkg.github.com/k5tuck/index.json \
  --api-key YOUR_GITHUB_PAT \
  --skip-duplicate
```

---

## Verifying Package Installation

After installing, verify the package is available:

```bash
# List installed packages
dotnet list package

# Should show something like:
# > Binah.Core    1.0.0
# > Binah.Contracts    1.0.0
```

In your code:

```csharp
using Binah.Core;
using Binah.Contracts.DTOs;

// If IntelliSense shows these namespaces, packages are working correctly
```

---

## Troubleshooting

### Issue: "Unable to load the service index"

**Cause:** GitHub Packages source not configured or invalid credentials

**Solution:**
```bash
# Remove existing source
dotnet nuget remove source github

# Re-add with correct credentials
dotnet nuget add source https://nuget.pkg.github.com/k5tuck/index.json \
  --name github \
  --username YOUR_GITHUB_USERNAME \
  --password YOUR_GITHUB_PAT \
  --store-password-in-clear-text
```

### Issue: "401 Unauthorized"

**Cause:** Invalid or expired GitHub PAT

**Solution:**
1. Generate a new PAT (see Step 1 above)
2. Update the source:
   ```bash
   dotnet nuget update source github \
     --username YOUR_GITHUB_USERNAME \
     --password NEW_GITHUB_PAT \
     --store-password-in-clear-text
   ```

### Issue: "Package 'Binah.Core' is not found"

**Cause:** Package not yet published or version doesn't exist

**Solution:**
1. Check available versions:
   ```bash
   dotnet nuget search Binah.Core --source github
   ```

2. Verify package exists on GitHub:
   - Go to: https://github.com/k5tuck?tab=packages
   - Look for `Binah.Core`, `Binah.Contracts`, etc.

### Issue: "NU1101: Unable to find package"

**Cause:** Package source priority issue

**Solution:**
```bash
# Explicitly specify source
dotnet add package Binah.Core --source github --version 1.0.0
```

Or update `NuGet.Config` to prioritize GitHub Packages:

```xml
<packageSources>
  <clear />
  <add key="github" value="https://nuget.pkg.github.com/k5tuck/index.json" protocolVersion="3" />
  <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
</packageSources>
```

---

## CI/CD Integration

### GitHub Actions

GitHub Actions automatically has access to `GITHUB_TOKEN` with package permissions:

```yaml
- name: Restore packages
  run: dotnet restore
  env:
    NUGET_AUTH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

### Azure DevOps

```yaml
- task: NuGetAuthenticate@0
  inputs:
    nuGetServiceConnections: 'GitHub Packages'

- task: DotNetCoreCLI@2
  inputs:
    command: 'restore'
    feedsToUse: 'config'
    nugetConfigPath: 'NuGet.Config'
```

### Docker Builds

Pass GitHub PAT as build argument:

```dockerfile
ARG GITHUB_PAT
RUN dotnet nuget add source https://nuget.pkg.github.com/k5tuck/index.json \
    --name github \
    --username k5tuck \
    --password ${GITHUB_PAT} \
    --store-password-in-clear-text \
    && dotnet restore
```

Build with:
```bash
docker build --build-arg GITHUB_PAT=your_pat_here .
```

---

## Package Versioning Strategy

**Semantic Versioning (SemVer):**
- Format: `MAJOR.MINOR.PATCH`
- Example: `1.2.3`

**Version Increments:**
- `MAJOR` (1.0.0 → 2.0.0) - Breaking changes
- `MINOR` (1.0.0 → 1.1.0) - New features (backward compatible)
- `PATCH` (1.0.0 → 1.0.1) - Bug fixes

**Pre-release Versions:**
- Alpha: `1.0.0-alpha.1`
- Beta: `1.0.0-beta.1`
- RC: `1.0.0-rc.1`

---

## Available Packages

| Package | Description | Latest Version |
|---------|-------------|----------------|
| **Binah.Core** | Core utilities, exceptions, middleware | TBD |
| **Binah.Contracts** | DTOs, events, interfaces | TBD |
| **Binah.Infrastructure** | DbContext, repositories, Kafka base classes | TBD |
| **Binah.Domain** | Domain models, value objects | TBD |

---

## Security Best Practices

1. **Never commit PATs to source control**
   - Add `NuGet.Config` to `.gitignore`
   - Use environment variables in CI/CD

2. **Use read-only tokens for consuming packages**
   - Only use `write:packages` scope for publishing

3. **Rotate tokens regularly**
   - Regenerate PATs every 90 days
   - Update in all environments

4. **Use organization secrets in CI/CD**
   - Store PATs as GitHub organization secrets
   - Reference via `${{ secrets.GITHUB_TOKEN }}`

---

## Alternative: Public NuGet.org

If you decide to make packages public:

1. Get NuGet API Key from https://www.nuget.org/account/apikeys
2. Add to GitHub Secrets: `NUGET_API_KEY`
3. Uncomment in `.github/workflows/ci-cd.yml`:
   ```yaml
   - name: Publish to NuGet.org
     run: dotnet nuget push ./nupkgs/*.nupkg \
       --source https://api.nuget.org/v3/index.json \
       --api-key ${{ secrets.NUGET_API_KEY }}
   ```

---

## Support

**Issues with packages?**
- GitHub Issues: https://github.com/k5tuck/binelek-shared/issues
- Contact: Platform team

**Documentation:**
- GitHub Packages Docs: https://docs.github.com/en/packages
- NuGet CLI Reference: https://learn.microsoft.com/en-us/nuget/reference/dotnet-commands

---

**Last Updated:** 2025-11-20
**Maintained By:** Binelek Platform Team
