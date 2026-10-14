using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

class SyntaxCheck
{
    static int Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (sender, ev) =>
        {
            string path = Path.Combine(@"E:\VS-IDE\MSBuild\Current\Bin\Roslyn", new AssemblyName(ev.Name).Name + ".dll");
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        };
        return Inspect(args[0]);
    }

    static int Inspect(string path)
    {
        var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), new CSharpParseOptions(LanguageVersion.CSharp7_3), path);
        var root = tree.GetCompilationUnitRoot();
        var errors = tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).OrderBy(d => d.Location.SourceSpan.Start).ToArray();
        foreach (var error in errors.Take(12)) Console.WriteLine(error);
        Console.WriteLine("SyntaxErrors=" + errors.Length);
        Console.WriteLine("GlobalStatements=" + root.DescendantNodes().OfType<GlobalStatementSyntax>().Count());
        Console.WriteLine("OpenBraces=" + root.DescendantTokens().Count(t => t.IsKind(SyntaxKind.OpenBraceToken)) + ", CloseBraces=" + root.DescendantTokens().Count(t => t.IsKind(SyntaxKind.CloseBraceToken)));
        var cls = root.DescendantNodes().OfType<ClassDeclarationSyntax>().Single(c => c.Identifier.ValueText == "ImgAcqFIFO");
        foreach (string name in new[] { "CogFlipRotateImInit", "TryReservePreview", "ReleasePreview", "IsCameraGrabbing", "GetCameraFrameLock" })
        {
            var method = cls.Members.OfType<MethodDeclarationSyntax>().SingleOrDefault(m => m.Identifier.ValueText == name);
            Console.WriteLine(name + ": ClassMember=" + (method != null) + ", HasBody=" + (method != null && method.Body != null) + ", Public=" + (method != null && method.Modifiers.Any(t => t.IsKind(SyntaxKind.PublicKeyword))));
        }
        return errors.Length == 0 ? 0 : 1;
    }
}
