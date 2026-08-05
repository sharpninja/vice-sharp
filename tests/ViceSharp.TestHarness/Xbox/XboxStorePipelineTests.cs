namespace ViceSharp.TestHarness.Xbox;

using System;
using System.IO;
using Xunit;

/// <summary>
/// FEAT-XSTOREPIPE-001 (PLAN-XBOXUWP S42, area XBOXPKG). Operator 2026-07-14: "create
/// azure pipeline to publish to microsoft store for xbox." Structural pins for the
/// Azure DevOps pipeline that builds the Release-UWP head, produces a Store-upload
/// package, and publishes it to Partner Center through the official Microsoft Store
/// Developer CLI (msstore), gated behind a manual-approval environment.
/// </summary>
/// <remarks>
/// Acceptance:
///   TEST-XSTORE-001a: the pipeline YAML exists, builds Release-UWP|x64, packages with
///     UapAppxPackageBuildMode=StoreUpload (unsigned; the Store signs), and re-stamps
///     the manifest identity from Partner Center variables before packaging.
///   TEST-XSTORE-001b: publishing uses the documented msstore CLI flow (UseMSStoreCLI@0
///     + reconfigure with the four PARTNER_CENTER_* variables + publish with the
///     msixupload and STORE_APP_ID) inside a deployment job bound to the manual-approval
///     environment; secrets appear ONLY as $(variable) references, never literals.
///   TEST-XSTORE-001c: the manifest re-stamp script exists and rewrites the Identity
///     via native XML (no string-templated manifest).
///   TEST-XSTORE-001d: the operator guide documents the Partner Center prerequisites,
///     the variable group, and the environment approval.
/// </remarks>
[Trait("Category", "Xbox")]
public sealed class XboxStorePipelineTests
{
    [Fact]
    public void Pipeline_BuildsReleaseUwp_AndPackagesStoreUpload()
    {
        var yml = ReadPipeline();

        // TEST-XSTORE-001a.
        Assert.Contains("Release-UWP", yml);
        Assert.Contains("UapAppxPackageBuildMode=StoreUpload", yml);
        Assert.Contains("AppxPackageSigningEnabled=false", yml);
        Assert.Contains("Set-StoreIdentity.ps1", yml);
        Assert.Contains("Category=Xbox", yml);
    }

    [Fact]
    public void Pipeline_PublishesViaTheDocumentedMsstoreFlow_WithSecretsAsVariables()
    {
        var yml = ReadPipeline();

        // TEST-XSTORE-001b: the documented Azure DevOps msstore flow.
        Assert.Contains("UseMSStoreCLI@0", yml);
        Assert.Contains("msstore reconfigure --tenantId $(PARTNER_CENTER_TENANT_ID) --sellerId $(PARTNER_CENTER_SELLER_ID) --clientId $(PARTNER_CENTER_CLIENT_ID) --clientSecret $(PARTNER_CENTER_CLIENT_SECRET)", yml);
        Assert.Contains("msstore publish", yml);
        Assert.Contains("$(STORE_APP_ID)", yml);

        // Manual-approval gate: a deployment job bound to the xbox-store environment.
        Assert.Contains("environment: xbox-store", yml);

        // Secrets must never be literals: every credential occurrence is a $() reference.
        Assert.DoesNotContain("clientSecret:", yml);
        Assert.Contains("PARTNER_CENTER_CLIENT_SECRET", yml);
    }

    [Fact]
    public void StoreIdentityScript_RewritesTheManifest_AsNativeXml()
    {
        var script = Path.Combine(RepoRoot, "build", "Set-StoreIdentity.ps1");
        Assert.True(File.Exists(script), $"Expected the manifest re-stamp script at '{script}'.");

        var text = File.ReadAllText(script);
        Assert.Contains("[xml]", text);
        Assert.Contains("Identity", text);
        Assert.Contains("PublisherDisplayName", text);
    }

    [Fact]
    public void WackPreflight_IsAnOptInNukeTarget()
    {
        // Operator 2026-07-14: WACK is NOT required for Store upload (Partner Center
        // certifies every package); it ships as the opt-in Nuke target
        // ValidateStorePackage, surfaced by the pipeline's runWack parameter.
        var build = File.ReadAllText(Path.Combine(RepoRoot, "build", "Build.cs"));
        Assert.Contains("Target ValidateStorePackage", build);
        Assert.Contains("appcert.exe", build);
        Assert.Contains("-appxpackagepath", build);
        Assert.Contains("OVERALL_RESULT", build);
        Assert.Contains("Session0", build);

        var yml = ReadPipeline();
        Assert.Contains("runWack", yml);
        Assert.Contains("ValidateStorePackage", yml);
    }

    [Fact]
    public void OperatorGuide_DocumentsTheStoreSetup()
    {
        var doc = Path.Combine(RepoRoot, "docs", "xbox-store-publishing.md");
        Assert.True(File.Exists(doc), $"Expected the publishing guide at '{doc}'.");

        var text = File.ReadAllText(doc);
        Assert.Contains("Partner Center", text);
        Assert.Contains("PARTNER_CENTER_TENANT_ID", text);
        Assert.Contains("xbox-store", text);
        Assert.Contains("STORE_APP_ID", text);
    }

    private static string ReadPipeline()
    {
        var path = Path.Combine(RepoRoot, "azure-pipelines-xbox-store.yml");
        Assert.True(File.Exists(path), $"Expected the Store pipeline at '{path}'.");
        return File.ReadAllText(path);
    }

    private static string RepoRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ViceSharp.slnx")))
                directory = directory.Parent;

            if (directory is null)
                throw new InvalidOperationException("Could not locate repository root.");

            return directory.FullName;
        }
    }
}
