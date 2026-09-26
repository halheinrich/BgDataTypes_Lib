using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Xml.Linq;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// The declarations that keep the test-support assembly out of products, and
/// the direction of the dependency between it and the library. The builders
/// are for tests — this repository's and every consumer's — and must never
/// reach a product; what stops that from this repository's side is one
/// assembly-level declaration, and these pin that nobody quietly removes it.
/// The BgUiPrimitives_Razor test-support project is the precedent.
/// </summary>
public class TestSupportPostureTests
{
    private static readonly Assembly TestSupport = typeof(TestRecords).Assembly;

    private static XDocument StagedProject(string fileName) =>
        XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Posture", fileName));

    // Unsupported on "browser": in any project that declares the browser
    // platform — BgQuiz's WebAssembly client, Extract's client and every
    // Razor member do — the platform-compatibility analyzer reports a use of
    // this assembly as CA1416, which the house TreatWarningsAsErrors makes a
    // build error.
    //
    // Stated limit: this pins the declaration, not the analyzer's verdict,
    // which belongs to the referencing project's build. A product that
    // declares no browser platform — a plain class library such as the
    // converter — is not stopped by it; the library below references no
    // project, which is this repository's half, and the rest is a
    // consumer-graph question.
    [Fact]
    public void TheTestSupportAssembly_DeclaresItselfUnsupportedInTheBrowser()
    {
        var declaration = Assert.Single(
            TestSupport.GetCustomAttributes<UnsupportedOSPlatformAttribute>());

        Assert.Equal("browser", declaration.PlatformName);
    }

    // Trimmable is a promise about a shipped surface; this assembly makes none.
    [Fact]
    public void TheTestSupportAssembly_DoesNotDeclareItselfTrimmable()
    {
        Assert.DoesNotContain(
            TestSupport.GetCustomAttributes<AssemblyMetadataAttribute>(),
            a => a.Key == "IsTrimmable");
    }

    // The library is the product; it references no project at all — neither
    // another member (it sits below them all) nor its own test support.
    [Fact]
    public void TheLibrary_ReferencesNoProject()
    {
        Assert.Empty(StagedProject("BgDataTypes_Lib.csproj").Descendants("ProjectReference"));
    }

    // The builders see what a producer sees and nothing more: the library
    // names this suite as a friend (it pins internal forms), never the
    // builders, so a builder cannot construct a record a producer could not.
    [Fact]
    public void TheLibrary_GrantsTheTestSupportAssemblyNoInternals()
    {
        var friends = typeof(BgDecisionData).Assembly
            .GetCustomAttributes<InternalsVisibleToAttribute>()
            .Select(a => a.AssemblyName);

        Assert.DoesNotContain(TestSupport.GetName().Name, friends);
    }

    // The test-support project references the library and nothing else: no
    // other project, and no package at all.
    [Fact]
    public void TheTestSupportProject_ReferencesOnlyTheLibrary()
    {
        var project = StagedProject("BgDataTypes_Lib.TestSupport.csproj");

        var reference = Assert.Single(project.Descendants("ProjectReference"));
        Assert.Equal(
            @"..\BgDataTypes_Lib\BgDataTypes_Lib.csproj",
            reference.Attribute("Include")?.Value);
        Assert.Empty(project.Descendants("PackageReference"));
    }

    // Consumers reach it by project reference from their test projects; it is
    // never a package.
    [Fact]
    public void TheTestSupportProject_IsNotPackable()
    {
        var packable = Assert.Single(
            StagedProject("BgDataTypes_Lib.TestSupport.csproj").Descendants("IsPackable"));

        Assert.Equal("false", packable.Value);
    }

    // No test framework rides along into a consumer's test project.
    [Fact]
    public void TheTestSupportAssembly_ReferencesNoTestFramework()
    {
        Assert.DoesNotContain(
            TestSupport.GetReferencedAssemblies(),
            a => a.Name!.StartsWith("xunit", StringComparison.OrdinalIgnoreCase)
                || a.Name.StartsWith("nunit", StringComparison.OrdinalIgnoreCase)
                || a.Name.StartsWith("Microsoft.VisualStudio.TestPlatform", StringComparison.OrdinalIgnoreCase));
    }
}
