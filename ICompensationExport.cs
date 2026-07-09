using System.Collections.Generic;
using OutSystems.ExternalLibraries.SDK;
using CompensationExportLibrary.Models;

namespace CompensationExportLibrary
{
    [OSInterface(Description = "Exports compensation models to a ZIP with one folder per version")]
    public interface ICompensationExport
    {
        [OSAction(
            Description = "Creates a ZIP where each selected version is a folder containing its CSV files",
            ReturnName = "ZipFile",
            ReturnDescription = "The generated ZIP as Binary Data")]
        byte[] GenerateCompensationZip(
            [OSParameter(Description = "One item per selected version")]
            List<CompensationVersionExport> versions
        );
    }
}
