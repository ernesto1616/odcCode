using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;
using CompensationExportLibrary.Models;

namespace CompensationExportLibrary
{
    public class CompensationExportService : ICompensationExport
    {
        private static readonly Dictionary<string, string> EtGradeMap =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["GH"] = "EC4", ["GG"] = "EC3", ["GF"] = "EC2", ["GE"] = "EC1",
                ["GD"] = "ET4", ["GC"] = "ET3", ["GB"] = "ET2", ["GA"] = "ET1"
            };
        private static readonly Dictionary<string, string> StGradeMap =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["GH"] = "C4", ["GG"] = "C3", ["GF"] = "C2", ["GE"] = "C1",
                ["GD"] = "T4", ["GC"] = "T3", ["GB"] = "T2", ["GA"] = "T1"
            };
        private static readonly HashSet<string> GePlusGrades =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "GE", "GF", "GG", "GH" };
        private static readonly HashSet<string> GaToGdGrades =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "GA", "GB", "GC", "GD" };
        private static readonly Dictionary<string, int> GradeOrder =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["G1"] = 0, ["GA"] = 1, ["GB"] = 2, ["GC"] = 3, ["GD"] = 4,
                ["GE"] = 5, ["GF"] = 6, ["GG"] = 7, ["GH"] = 8
            };

        private static readonly string[] StaffHeaders = Headers("Category", "Pay Frequency", "Grade", "Proposed Minimum", "Proposed Midpoint", "Proposed Maximum", "Status");
        private static readonly string[] EtHeaders = Headers("ET Category", "Pay Frequency", "ET Grade", "Proposed Minimum", "Proposed Midpoint", "Proposed Maximum", "ET Status");
        private static readonly string[] StHeaders = Headers("ST Category", "ST Frequency", "ST Grade", "Avg. ST Minimum", "ST Midpoint", "Avg. ST Maximum", "ST Status");
        private static readonly string[] UHeaders = Headers("Category", "Pay Frequency", "U Grade", "UC/UA Minimum", "UC/UA Midpoint", "UC/UA Maximum", "UC/UA Status");
        private static readonly string[] UjHeaders = Headers("UJ Category", "Pay Frequency", "UJ Grade", "UJ Minimum", "UJ Midpoint", "UJ Maximum", "UJ Status");

        private static string[] Headers(string category, string frequency, string grade, string min, string mid, string max, string status)
        {
            return new[] { "Structure Model Name", "Effective Date", "Region", "Country", "Salary Plan", "Pay Type",
                category, frequency, "Grade Group", grade, min, mid, max, "Currency", "Code", status,
                "Scale Rounding", "Total Merit Increase", "Structure Adjustment", "Merit Element", "CPI Inflation", "HQ/CO" };
        }

        public byte[] GenerateCompensationZip(List<CompensationVersionExport> versions)
        {
            if (versions == null || versions.Count == 0)
                throw new ArgumentException("At least one compensation version is required.", nameof(versions));

            var staff = new List<string[]>();
            var et = new List<string[]>();
            var st = new List<string[]>();
            var u = new List<string[]>();
            var uj = new List<string[]>();

            // Build each version independently: U-band extrema and GD selection never cross versions.
            for (var v = 0; v < versions.Count; v++)
            {
                var version = versions[v];
                var rows = version.Rows ?? new List<CompensationRow>();
                var name = ValidateAndGetModelName(version, rows, v + 1);
                foreach (var r in rows.OrderBy(GradeRank))
                {
                    var grade = (r.Grade ?? string.Empty).Trim();
                    if (!grade.Equals("G1", StringComparison.OrdinalIgnoreCase))
                        staff.Add(ReportRow(name, r, "STAFF", r.PayFrequency, r.GradeGroup, r.Grade,
                            D(r.ProposedMinimum), D(r.ProposedMidpoint), D(r.ProposedMaximum)));
                    if (EtGradeMap.TryGetValue(grade, out var etGrade))
                        et.Add(ReportRow(name, r, "ET Appt", "Annual", r.GradeGroup, etGrade,
                            D(r.ProposedMinimum), D(r.ProposedMidpoint), D(r.ProposedMaximum)));
                    if (StGradeMap.TryGetValue(grade, out var stGrade))
                    {
                        var consultant = stGrade.StartsWith("C", StringComparison.OrdinalIgnoreCase);
                        var divisor = consultant ? 260m : 2080m;
                        st.Add(ReportRow(name, r, "ST Appt", consultant ? "Daily" : "Hourly", r.GradeGroup, stGrade,
                            D2(r.ProposedMinimum / divisor), D2(r.ProposedMidpoint / divisor), D2(r.ProposedMaximum / divisor)));
                    }
                }
                AppendBand(u, name, rows, GaToGdGrades, "GA-GD", "UA");
                AppendBand(u, name, rows, GePlusGrades, "GE+", "UC");
                foreach (var gd in rows.Where(r => string.Equals((r.Grade ?? string.Empty).Trim(), "GD", StringComparison.OrdinalIgnoreCase)).Take(1))
                {
                    var value = D(CeilingToMultiple((gd.ProposedMinimum + gd.ProposedMidpoint) / 2m, gd.ScaleRounding));
                    uj.Add(ReportRow(name, gd, "ET APPT", gd.PayFrequency, "GA-GD", "UJ", value, value, value));
                }
            }

            using var output = new MemoryStream();
            using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
            {
                AddWorkbook(archive, "Scale Output Report.xlsx", StaffHeaders, staff);
                AddWorkbook(archive, "ET Scale Output Report.xlsx", EtHeaders, et);
                AddWorkbook(archive, "ST Scale Output Report.xlsx", StHeaders, st);
                AddWorkbook(archive, "UC_UA Scale Output Report.xlsx", UHeaders, u);
                AddWorkbook(archive, "UJ Scale Output Report.xlsx", UjHeaders, uj);
            }
            return output.ToArray();
        }

        private static string ValidateAndGetModelName(CompensationVersionExport version, List<CompensationRow> rows, int index)
        {
            var model = version.SalaryScaleModelName?.Trim();
            if (string.IsNullOrWhiteSpace(model))
                throw new ArgumentException($"Version {index}: SalaryScaleModelName is required.");
            // A version without rows contributes no data to any report.
            if (rows.Count == 0)
                return model;
            var first = rows[0];
            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].CPIInflation != first.CPIInflation || rows[i].MeritElement != first.MeritElement)
                    throw new ArgumentException($"Version {index}, row {i + 1}: CPIInflation and MeritElement must match throughout the version.");
                if (!string.Equals(rows[i].SalaryScaleModelName?.Trim(), model, StringComparison.Ordinal))
                    throw new ArgumentException($"Version {index}, row {i + 1}: SalaryScaleModelName must match the version name.");
            }
            return model;
        }

        private static int GradeRank(CompensationRow row) =>
            GradeOrder.TryGetValue((row.Grade ?? string.Empty).Trim(), out var rank) ? rank : int.MaxValue;

        private static void AppendBand(List<string[]> output, string name, List<CompensationRow> rows,
            HashSet<string> grades, string group, string uGrade)
        {
            var matches = rows.Where(r => grades.Contains((r.Grade ?? string.Empty).Trim())).ToList();
            if (matches.Count == 0) return;
            var template = matches[0]; // Preserve original first-row metadata and rounding unit.
            var min = matches.Min(r => r.ProposedMinimum);
            var max = matches.Max(r => r.ProposedMaximum);
            var mid = CeilingToMultiple((min + max) / 2m, template.ScaleRounding);
            output.Add(ReportRow(name, template, "STAFF", template.PayFrequency, group, uGrade, D(min), D(mid), D(max)));
        }

        private static string[] ReportRow(string name, CompensationRow r, string category, string frequency,
            string group, string grade, string min, string mid, string max)
        {
            return new[] { name, FormatDate(r.EffectiveDate), r.Region, r.Country, r.SalaryPlan, r.PayType,
                category, frequency, group, grade, min, mid, max, r.Currency, r.Code, r.Status,
                D(r.ScaleRounding), P(r.TotalMeritIncrease), P(r.StructureAdjustement), P(r.MeritElement),
                P(r.CPIInflation), r.HCCO };
        }

        private static decimal CeilingToMultiple(decimal value, decimal rounding) =>
            rounding <= 0 ? value : Math.Ceiling(value / rounding) * rounding;
        private static string D(decimal value) => value.ToString("0.############################", CultureInfo.InvariantCulture);
        private static string D2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero).ToString("0.00", CultureInfo.InvariantCulture);
        private static string P(decimal value) => D(value) + "%";
        private static string FormatDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ||
                DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out dt))
                return dt.ToString("M/d/yyyy", CultureInfo.InvariantCulture);
            return value;
        }

        private static void AddWorkbook(ZipArchive outer, string fileName, string[] headers, List<string[]> rows)
        {
            if (rows.Count >= 1048576)
                throw new ArgumentException($"{fileName}: the worksheet exceeds Excel's 1,048,576-row limit.");
            var entry = outer.CreateEntry(fileName, CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            using var workbook = new ZipArchive(entryStream, ZipArchiveMode.Create, leaveOpen: true);
            WritePart(workbook, "[Content_Types].xml",
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
                "</Types>");
            WritePart(workbook, "_rels/.rels",
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                "</Relationships>");
            WritePart(workbook, "xl/workbook.xml",
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                "<sheets><sheet name=\"Report\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
            WritePart(workbook, "xl/_rels/workbook.xml.rels",
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
                "</Relationships>");
            var sheet = workbook.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.Optimal);
            using var stream = sheet.Open();
            using var xml = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new UTF8Encoding(false), CloseOutput = false });
            const string ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            xml.WriteStartDocument();
            xml.WriteStartElement("worksheet", ns);
            xml.WriteStartElement("sheetData", ns);
            WriteRow(xml, headers, 1, ns);
            for (var i = 0; i < rows.Count; i++)
                WriteRow(xml, rows[i], i + 2, ns);
            xml.WriteEndElement();
            xml.WriteEndElement();
            xml.WriteEndDocument();
        }

        private static void WriteRow(XmlWriter xml, string[] cells, int number, string ns)
        {
            xml.WriteStartElement("row", ns);
            xml.WriteAttributeString("r", number.ToString(CultureInfo.InvariantCulture));
            for (var i = 0; i < cells.Length; i++)
            {
                xml.WriteStartElement("c", ns);
                xml.WriteAttributeString("r", ColumnName(i + 1) + number.ToString(CultureInfo.InvariantCulture));
                xml.WriteAttributeString("t", "inlineStr");
                xml.WriteStartElement("is", ns);
                xml.WriteStartElement("t", ns);
                xml.WriteAttributeString("xml", "space", "http://www.w3.org/XML/1998/namespace", "preserve");
                xml.WriteString(CleanXml(cells[i] ?? string.Empty));
                xml.WriteEndElement();
                xml.WriteEndElement();
                xml.WriteEndElement();
            }
            xml.WriteEndElement();
        }

        private static string CleanXml(string value)
        {
            // XML 1.0 rejects several control characters; preserve all legal characters.
            var builder = new StringBuilder(value.Length);
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (char.IsHighSurrogate(c) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                {
                    builder.Append(c).Append(value[++i]);
                }
                else if (XmlConvert.IsXmlChar(c)) builder.Append(c);
            }
            return builder.ToString();
        }

        private static string ColumnName(int index)
        {
            var result = string.Empty;
            while (index > 0)
            {
                index--;
                result = (char)('A' + index % 26) + result;
                index /= 26;
            }
            return result;
        }

        private static void WritePart(ZipArchive archive, string name, string xml)
        {
            var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
            using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
            writer.Write(xml);
        }
    }
}
