using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using UglyToad.PdfPig;

namespace RagWorkspace.Api.Services;

public sealed record ExtractedDocument(string Text, IReadOnlyDictionary<int, string> Pages);

public sealed class DocumentProcessingService
{
    public async Task<ExtractedDocument> ExtractAsync(string fileName, Stream stream, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".txt" or ".md" or ".csv" => new ExtractedDocument(await ReadTextAsync(stream, cancellationToken), new Dictionary<int, string>()),
            ".pdf" => ExtractPdf(stream),
            ".docx" => ExtractDocx(stream),
            _ => throw new InvalidDataException("Supported document types are PDF, DOCX, TXT, MD, and CSV.")
        };
    }

    public static string Hash(string content) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));

    public static IReadOnlyList<string> Chunk(string text, int size = 1000, int overlap = 100)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];
        var normalized = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        var chunks = new List<string>();
        for (var start = 0; start < normalized.Length; start += Math.Max(1, size - overlap))
        {
            var length = Math.Min(size, normalized.Length - start);
            chunks.Add(normalized.Substring(start, length));
            if (start + length >= normalized.Length) break;
        }
        return chunks;
    }

    private static async Task<string> ReadTextAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: false);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static ExtractedDocument ExtractPdf(Stream stream)
    {
        using var document = PdfDocument.Open(stream);
        var pages = document.GetPages().ToDictionary(page => page.Number, page => page.Text);
        return new ExtractedDocument(string.Join(Environment.NewLine, pages.Values), pages);
    }

    private static ExtractedDocument ExtractDocx(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        var entry = archive.GetEntry("word/document.xml")
            ?? throw new InvalidDataException("The DOCX package does not contain document.xml.");
        using var entryStream = entry.Open();
        var xml = XDocument.Load(entryStream);
        XNamespace word = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var text = string.Join(Environment.NewLine, xml.Descendants(word + "t").Select(node => node.Value));
        return new ExtractedDocument(text, new Dictionary<int, string>());
    }
}
