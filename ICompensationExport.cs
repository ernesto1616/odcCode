using System.Collections.Generic;
using OutSystems.ExternalLibraries.SDK;
using CompensationExportLibrary.Models;

namespace CompensationExportLibrary
{
    [OSInterface(Description = "Exports compensation models in five Excel workbooks inside one ZIP")]
    public interface ICompensationExport
    {
        [OSAction(
            Description = "Creates one ZIP with five combined XLSX compensation reports",
            ReturnName = "ZipFile",
            ReturnDescription = "The generated ZIP as Binary Data")]
        byte[] GenerateCompensationZip(
            [OSParameter(Description = "One item per selected version")]
            List<CompensationVersionExport> versions);
    }
}
