using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;

/// <summary>
/// Keeps the IDE-generated projects aligned with the analyzer shipped by this repository.
/// </summary>
public sealed class UnityAnalyzerProjectPostprocessor : AssetPostprocessor
{
    private const string AnalyzerFileName = "Microsoft.Unity.Analyzers.dll";
    private const string AnalyzerProjectPath = "Assets/Analyzers/Microsoft.Unity.Analyzers.dll";

    public static string OnGeneratedCSProject(string path, string content)
    {
        if (!path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            return content;
        }

        XDocument document;

        try
        {
            document = XDocument.Parse(content);
        }
        catch (Exception)
        {
            // Do not prevent Unity from generating a project if a future project format changes.
            return content;
        }

        XElement root = document.Root;
        if (root == null)
        {
            return content;
        }

        XNamespace xmlNamespace = root.Name.Namespace;
        XElement[] existingAnalyzers = document
            .Descendants(xmlNamespace + "Analyzer")
            .Where(element =>
                string.Equals(
                    Path.GetFileName((string)element.Attribute("Include")),
                    AnalyzerFileName,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            .ToArray();

        foreach (XElement analyzer in existingAnalyzers)
        {
            analyzer.Remove();
        }

        root.Add(
            new XElement(
                xmlNamespace + "ItemGroup",
                new XElement(
                    xmlNamespace + "Analyzer",
                    new XAttribute("Include", AnalyzerProjectPath)
                )
            )
        );

        return document.ToString();
    }
}
