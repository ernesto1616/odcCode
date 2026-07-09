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
        private static readonly Dictionary<string, string> EtGradeMap =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["GH"] = "EC4",
                ["GG"] = "EC3",
                ["GF"] = "EC2",
                ["GE"] = "EC1",
                ["GD"] = "ET4",
                ["GC"] = "ET3",
                ["GB"] = "ET2",
                ["GA"] = "ET1",
                ["G1"] = "ET0"
            };

        private static readonly Dictionary<string, string> StGradeMap =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["GH"] = "C4",
                ["GG"] = "C3",
                ["GF"] = "C2",
                ["GE"] = "C1",
                ["GD"] = "T4",
                ["GC"] = "T3",
                ["GB"] = "T2",
                ["GA"] = "T1",
                ["G1"] = "T0"
            };

        public byte[] GenerateCompensationZip(List<CompensationVersionExport> versions)
        {
            if (versions == null || versions.Count == 0)
                return Array.Empty<byte>();

            using var memoryStream = new MemoryStream();

            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var usedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var version in versions)
                {
                    var folderName = MakeUniqueFolder(
                        SanitizeName(version.SalaryScaleModelName),
                        usedFolders);

                    var rows = version.Rows ?? new List<CompensationRow>();

                    AddEntry(
                        archive,
                        $"{folderName}/Scale Output Report.csv",
                        BuildStaffCsv(rows));

                    AddEntry(
                        archive,
                        $"{folderName}/ET Scale Output Report.csv",
                        BuildEtCsv(rows));

                    AddEntry(
                        archive,
                        $"{folderName}/ST Scale Output Report.csv",
                        BuildStCsv(rows));
                }
            }

            return memoryStream.ToArray();
        }

        private static void AddEntry(ZipArchive archive, string entryName, byte[] content)
        {
            var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            entryStream.Write(content, 0, content.Length);
        }

        private static byte[] BuildStaffCsv(List<CompensationRow> rows)
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Join(",", new[]
            {
                "Structure Model Name",
                "Effective Date",
                "Region",
                "Country",
                "Salary Plan",
                "Pay Type",
                "Category",
                "Pay Frequency",
                "Grade Group",
                "Grade",
                "Proposed Minimum",
                "Proposed Midpoint",
                "Proposed Maximum",
                "Currency",
                "Code",
                "Status",
                "Scale Rounding",
                "Total Merit Increase",
                "Structure Adjustment",
                "Merit Element",
                "CPI Inflation",
                "HQ/CO"
            }.Select(Escape)));

            foreach (var r in rows)
            {
                sb.AppendLine(string.Join(",", new[]
                {
                    Escape(r.SalaryScaleModelName),
                    Escape(FormatDate(r.EffectiveDate)),
                    Escape(r.Region),
                    Escape(r.Country),
                    Escape(r.SalaryPlan),
                    Escape(r.PayType),
                    Escape("STAFF"),
                    Escape(r.PayFrequency),
                    Escape(r.GradeGroup),
                    Escape(r.Grade),
                    FormatDecimal(r.ProposedMinimum),
                    FormatDecimal(r.ProposedMidpoint),
                    FormatDecimal(r.ProposedMaximum),
                    Escape(r.Currency),
                    Escape(r.Code),
                    Escape(r.Status),
                    FormatDecimal(r.ScaleRounding),
                    FormatDecimal(r.TotalMeritIncrease),
                    FormatDecimal(r.StructureAdjustement),
                    FormatDecimal(r.MeritElement),
                    FormatDecimal(r.CPIInflation),
                    Escape(r.HCCO)
                }));
            }

            return WithUtf8Bom(sb.ToString());
        }

        private static byte[] BuildEtCsv(List<CompensationRow> rows)
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Join(",", new[]
            {
                "Structure Model Name",
                "Effective Date",
                "Region",
                "Country",
                "Salary Plan",
                "Pay Type",
                "ET Category",
                "Pay Frequency",
                "Grade Group",
                "ET Grade",
                "Proposed Minimum",
                "Proposed Midpoint",
                "Proposed Maximum",
                "Currency",
                "Code",
                "ET Status",
                "Scale Rounding",
                "Total Merit Increase",
                "Structure Adjustment",
                "Merit Element",
                "CPI Inflation",
                "HQ/CO"
            }.Select(Escape)));

            foreach (var r in rows)
            {
                if (!EtGradeMap.TryGetValue((r.Grade ?? string.Empty).Trim(), out var etGrade))
                    continue;

                sb.AppendLine(string.Join(",", new[]
                {
                    Escape(r.SalaryScaleModelName),
                    Escape(FormatDate(r.EffectiveDate)),
                    Escape(r.Region),
                    Escape(r.Country),
                    Escape(r.SalaryPlan),
                    Escape(r.PayType),
                    Escape("ET Appt"),
                    Escape("Annual"),
                    Escape(r.GradeGroup),
                    Escape(etGrade),
                    FormatDecimal(r.ProposedMinimum),
                    FormatDecimal(r.ProposedMidpoint),
                    FormatDecimal(r.ProposedMaximum),
                    Escape(r.Currency),
                    Escape(r.Code),
                    Escape(r.Status),
                    FormatDecimal(r.ScaleRounding),
                    FormatDecimal(r.TotalMeritIncrease),
                    FormatDecimal(r.StructureAdjustement),
                    FormatDecimal(r.MeritElement),
                    FormatDecimal(r.CPIInflation),
                    Escape(r.HCCO)
                }));
            }

            return WithUtf8Bom(sb.ToString());
        }

        private static byte[] BuildStCsv(List<CompensationRow> rows)
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Join(",", new[]
            {
                "Structure Model Name",
                "Effective Date",
                "Region",
                "Country",
                "Salary Plan",
                "Pay Type",
                "ST Category",
                "ST Frequency",
                "Grade Group",
                "ST Grade",
                "Avg. ST Minimum",
                "ST Midpoint",
                "Avg. ST Maximum",
                "Currency",
                "Code",
                "ST Status",
                "Scale Rounding",
                "Total Merit Increase",
                "Structure Adjustment",
                "Merit Element",
                "CPI Inflation",
                "HQ/CO"
            }.Select(Escape)));

            foreach (var r in rows)
            {
                if (!StGradeMap.TryGetValue((r.Grade ?? string.Empty).Trim(), out var stGrade))
                    continue;

                var isConsultant = stGrade.StartsWith("C", StringComparison.OrdinalIgnoreCase);
                var divisor = isConsultant ? 260m : 2080m;
                var stFrequency = isConsultant ? "Daily" : "Hourly";

                var stMin = divisor == 0 ? 0 : r.ProposedMinimum / divisor;
                var stMid = divisor == 0 ? 0 : r.ProposedMidpoint / divisor;
                var stMax = divisor == 0 ? 0 : r.ProposedMaximum / divisor;

                sb.AppendLine(string.Join(",", new[]
                {
                    Escape(r.SalaryScaleModelName),
                    Escape(FormatDate(r.EffectiveDate)),
                    Escape(r.Region),
                    Escape(r.Country),
                    Escape(r.SalaryPlan),
                    Escape(r.PayType),
                    Escape("ST Appt"),
                    Escape(stFrequency),
                    Escape(r.GradeGroup),
                    Escape(stGrade),
                    FormatDecimal2(stMin),
                    FormatDecimal2(stMid),
                    FormatDecimal2(stMax),
                    Escape(r.Currency),
                    Escape(r.Code),
                    Escape(r.Status),
                    FormatDecimal(r.ScaleRounding),
                    FormatDecimal(r.TotalMeritIncrease),
                    FormatDecimal(r.StructureAdjustement),
                    FormatDecimal(r.MeritElement),
                    FormatDecimal(r.CPIInflation),
                    Escape(r.HCCO)
                }));
            }

            return WithUtf8Bom(sb.ToString());
        }

        private static string FormatDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ||
                DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out dt))
            {
                return dt.ToString("M/d/yyyy", CultureInfo.InvariantCulture);
            }

            return value;
        }

        private static string FormatDecimal(decimal value)
        {
            return value.ToString("0.############################", CultureInfo.InvariantCulture);
        }

        private static string FormatDecimal2(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero)
                .ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static byte[] WithUtf8Bom(string content)
        {
            return Encoding.UTF8.GetPreamble()
                .Concat(Encoding.UTF8.GetBytes(content))
                .ToArray();
        }

        private static string Escape(string field)
        {
            if (string.IsNullOrEmpty(field))
                return string.Empty;

            if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
                return $"\"{field.Replace("\"", "\"\"")}\"";

            return field;
        }

        private static string SanitizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Unnamed";

            var invalid = Path.GetInvalidFileNameChars();
            var clean = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();

            return string.IsNullOrEmpty(clean) ? "Unnamed" : clean;
        }

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
