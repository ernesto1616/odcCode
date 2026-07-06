using System.Collections.Generic;
using OutSystems.ExternalLibraries.SDK;

namespace CompensationExportLibrary.Models
{
    [OSStructure(Description = "One compensation version = one folder in the ZIP")]
    public struct CompensationVersionExport
    {
        // Used to name the folder inside the ZIP
        [OSStructureField] public string SalaryScaleModelName { get; set; }

        // The full SQL output (all grade rows) for this version
        [OSStructureField] public List<CompensationRow> Rows { get; set; }
    }
}
