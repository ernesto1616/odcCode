using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using CompensationExportLibrary.Models;

namespace CompensationExportLibrary
{
    public class CompensationExportService : ICompensationExport
    {
        public byte[] GenerateCompensationZip(List<CompensationVersionExport> versions)
        {
            if (versions == null || versions.Count == 0)
                return Array.Empty<byte>();

            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                // Handle duplicate model names so folders don't collide
                var usedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var version in versions)
                {
                    var folderName = MakeUniqueFolder(
                        SanitizeName(version.SalaryScaleModelName), usedFolders);

                    var csvBytes = BuildCsv(version.Rows ?? new List<CompensationRow>());

                    // Path inside the ZIP: "<ModelName>/<ModelName>.csv"
                    var entryName = $"{folderName}/{folderName}.csv";
                    var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);

                    using var entryStream = entry.Open();
                    entryStream.Write(csvBytes, 0, csvBytes.Length);
                }
            }

            return memoryStream.ToArray();
        }

        private static byte[] BuildCsv(List<CompensationRow> rows)
        {
            var sb = new StringBuilder();

            // Header
            sb.AppendLine(string.Join(",", new[]
            {
                "SalaryScaleModelName","EffectiveDate","Grade","ProposedMinimum",
                "ProposedMidpoint","ProposedMaximum","ScaleRounding","TotalMeritIncrease",
                "StructureAdjustement","MeritElement","CPIInflation"
            }.Select(Escape)));

            // Data rows
            var ci = CultureInfo.InvariantCulture;
            foreach (var r in rows)
            {
                sb.AppendLine(string.Join(",", new[]
                {
                    Escape(r.SalaryScaleModelName),
                    Escape(r.EffectiveDate),
                    Escape(r.Grade),
                    r.ProposedMinimum.ToString(ci),
                    r.ProposedMidpoint.ToString(ci),
                    r.ProposedMaximum.ToString(ci),
                    r.ScaleRounding.ToString(ci),
                    r.TotalMeritIncrease.ToString(ci),
                    r.StructureAdjustement.ToString(ci),
                    r.MeritElement.ToString(ci),
                    r.CPIInflation.ToString(ci)
                }));
            }

            // UTF-8 BOM so Excel opens accents correctly
            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        }

        // Escapes commas, quotes and newlines per CSV spec
        private static string Escape(string field)
        {
            if (string.IsNullOrEmpty(field)) return string.Empty;
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
                return $"\"{field.Replace("\"", "\"\"")}\"";
            return field;
        }

        // Removes characters that are illegal in file/folder names
        private static string SanitizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Unnamed";
            var invalid = Path.GetInvalidFileNameChars();
            var clean = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
            return string.IsNullOrEmpty(clean) ? "Unnamed" : clean;
        }

        // Appends _2, _3... if the same model name appears more than once
        private static string MakeUniqueFolder(string baseName, HashSet<string> used)
        {
            var candidate = baseName;
            var i = 2;
            while (!used.Add(candidate))
            {
                candidate = $"{baseName}_{i}";
                i++;
            }
            return candidate;
        }
    }
}
