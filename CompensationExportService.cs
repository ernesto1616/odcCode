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
                ["GA"] = "ET1"
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
                ["GA"] = "T1"
            };

        // Grades that make up the "GE+" and "GA-GD" bands used by the U-scale reports.
        private static readonly HashSet<string> GePlusGrades =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "GE", "GF", "GG", "GH" };

        private static readonly HashSet<string> GaToGdGrades =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "GA", "GB", "GC", "GD" };

        // Grades that must never be displayed in any report (including the master table).
        private static readonly HashSet<string> ExcludedGrades =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "G1" };

        // Canonical ascending display order (lowest grade first, GH last).
        // Lower rank = displayed earlier. Unknown grades sort to the end.
        private static readonly Dictionary<string, int> GradeOrder =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["G1"] = 0,
                ["GA"] = 1,
                ["GB"] = 2,
                ["GC"] = 3,
                ["GD"] = 4,
                ["GE"] = 5,
                ["GF"] = 6,
                ["GG"] = 7,
                ["GH"] = 8
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

                    AddEntry(
                        archive,
                        $"{folderName}/UC_UA Scale Output Report.csv",
                        BuildUcUaCsv(rows));

                    AddEntry(
                        archive,
                        $"{folderName}/UJ Scale Output Report.csv",
                        BuildUjCsv(rows));
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

        private static bool IsExcluded(CompensationRow r)
        {
            return ExcludedGrades.Contains((r.Grade ?? string.Empty).Trim());
        }

        private static int GradeRank(CompensationRow r)
        {
            return GradeOrder.TryGetValue((r.Grade ?? string.Empty).Trim(), out var rank)
                ? rank
                : int.MaxValue;
        }

        // Returns rows sorted in canonical ascending grade order (GH last).
        private static List<CompensationRow> OrderByGrade(IEnumerable<CompensationRow> rows)
        {
            return rows.OrderBy(GradeRank).ToList();
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

            foreach (var r in OrderByGrade(rows))
            {
                if (IsExcluded(r))
                    continue;

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

            foreach (var r in OrderByGrade(rows))
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

            foreach (var r in OrderByGrade(rows))
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

        private static byte[] BuildUcUaCsv(List<CompensationRow> rows)
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
                "U Grade",
                "UC/UA Minimum",
                "UC/UA Midpoint",
                "UC/UA Maximum",
                "Currency",
                "Code",
                "UC/UA Status",
                "Scale Rounding",
                "Total Merit Increase",
                "Structure Adjustment",
                "Merit Element",
                "CPI Inflation",
                "HQ/CO"
            }.Select(Escape)));

            // Ascending order: GA-GD (UA) first, GE+ (UC) last.
            AppendUcUaRow(sb, rows, GaToGdGrades, "GA-GD", "UA");
            AppendUcUaRow(sb, rows, GePlusGrades, "GE+", "UC");

            return WithUtf8Bom(sb.ToString());
        }

        private static void AppendUcUaRow(
            StringBuilder sb,
            List<CompensationRow> rows,
            HashSet<string> grades,
            string gradeGroup,
            string uGrade)
        {
            var groupRows = rows
                .Where(r => grades.Contains((r.Grade ?? string.Empty).Trim()))
                .ToList();

            if (groupRows.Count == 0)
                return;

            // Common fields are copied from the first row in the band.
            var t = groupRows[0];

            var min = groupRows.Min(r => r.ProposedMinimum);
            var max = groupRows.Max(r => r.ProposedMaximum);

            // Midpoint is the average of min and max, then rounded UP to the
            // nearest multiple of Scale Rounding (ceiling behaviour).
            var mid = CeilingToMultiple((min + max) / 2m, t.ScaleRounding);

            sb.AppendLine(string.Join(",", new[]
            {
                Escape(t.SalaryScaleModelName),
                Escape(FormatDate(t.EffectiveDate)),
                Escape(t.Region),
                Escape(t.Country),
                Escape(t.SalaryPlan),
                Escape(t.PayType),
                Escape("STAFF"),
                Escape(t.PayFrequency),
                Escape(gradeGroup),
                Escape(uGrade),
                FormatDecimal(min),
                FormatDecimal(mid),
                FormatDecimal(max),
                Escape(t.Currency),
                Escape(t.Code),
                Escape(t.Status),
                FormatDecimal(t.ScaleRounding),
                FormatDecimal(t.TotalMeritIncrease),
                FormatDecimal(t.StructureAdjustement),
                FormatDecimal(t.MeritElement),
                FormatDecimal(t.CPIInflation),
                Escape(t.HCCO)
            }));
        }

        private static byte[] BuildUjCsv(List<CompensationRow> rows)
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
                "UJ Category",
                "Pay Frequency",
                "Grade Group",
                "UJ Grade",
                "UJ Minimum",
                "UJ Midpoint",
                "UJ Maximum",
                "Currency",
                "Code",
                "UJ Status",
                "Scale Rounding",
                "Total Merit Increase",
                "Structure Adjustment",
                "Merit Element",
                "CPI Inflation",
                "HQ/CO"
            }.Select(Escape)));

            // UJ is derived from the GD row of the structure report.
            // CompensationRow is a value type (struct), so we check for existence
            // via count rather than a null comparison on FirstOrDefault.
            var gdRows = rows
                .Where(r => string.Equals((r.Grade ?? string.Empty).Trim(), "GD", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (gdRows.Count > 0)
            {
                var gd = gdRows[0];

                var raw = (gd.ProposedMinimum + gd.ProposedMidpoint) / 2m;
                var value = CeilingToMultiple(raw, gd.ScaleRounding);

                sb.AppendLine(string.Join(",", new[]
                {
                    Escape(gd.SalaryScaleModelName),
                    Escape(FormatDate(gd.EffectiveDate)),
                    Escape(gd.Region),
                    Escape(gd.Country),
                    Escape(gd.SalaryPlan),
                    Escape(gd.PayType),
                    Escape("ET APPT"),
                    Escape(gd.PayFrequency),
                    Escape("GA-GD"),
                    Escape("UJ"),
                    FormatDecimal(value),
                    FormatDecimal(value),
                    FormatDecimal(value),
                    Escape(gd.Currency),
                    Escape(gd.Code),
                    Escape(gd.Status),
                    FormatDecimal(gd.ScaleRounding),
                    FormatDecimal(gd.TotalMeritIncrease),
                    FormatDecimal(gd.StructureAdjustement),
                    FormatDecimal(gd.MeritElement),
                    FormatDecimal(gd.CPIInflation),
                    Escape(gd.HCCO)
                }));
            }

            return WithUtf8Bom(sb.ToString());
        }

        // Rounds a value UP to the nearest multiple of "rounding" (ceiling behaviour).
        // e.g. value=150, rounding=100 -> 200; value=100 exactly -> 100.
        private static decimal CeilingToMultiple(decimal value, decimal rounding)
        {
            if (rounding <= 0)
                return value;

            return Math.Ceiling(value / rounding) * rounding;
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
