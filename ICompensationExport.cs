using System.Collections.Generic;
using OutSystems.ExternalLibraries.SDK;
using CompensationExportLibrary.Models;

namespace CompensationExportLibrary
{
    [OSInterface(Description = "Exports compensation models in Excel workbooks inside one ZIP")]
    public interface ICompensationExport
    {
        [OSAction(
            Description = "Creates one ZIP with five combined XLSX compensation reports",
            ReturnName = "ZipFile",
            ReturnDescription = "The generated ZIP as Binary Data")]
        byte[] GenerateCompensationZip(
            [OSParameter(Description = "One item per selected version")]
            List<CompensationVersionExport> versions);

        [OSAction(
            Description = "Creates one ZIP with the five combined XLSX compensation reports plus the US Scale Output Report",
            ReturnName = "ZipFile",
            ReturnDescription = "The generated ZIP as Binary Data")]
        byte[] GenerateCompensationZipWithUS(
            [OSParameter(Description = "One item per selected version")]
            List<CompensationVersionExport> versions,
            [OSParameter(Description = "US Scale rows, written in the order received (an empty list writes the header only)")]
            List<USScaleRow> usScaleRows);
    }
}
