using FrostHelper.DocGen;
using Xunit.Abstractions;

namespace FrostHelper.Tests.SessionExpressions;

[Collection("FrostHelper")]
public class DocGen(ITestOutputHelper testOutputHelper) {
    [Fact]
    public void GenerateMarkdownDocs() {
        testOutputHelper.WriteLine(MarkdownDocGen.CreateMarkdownDocumentation());
    }
}