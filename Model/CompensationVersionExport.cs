using System.Collections.Generic;
using OutSystems.ExternalLibraries.SDK;

namespace CompensationExportLibrary.Models
{
    [OSStructure(Description = "One compensation version = one folder in the ZIP")]
    public struct CompensationVersionExport
    {
        [OSStructureField] public string SalaryScaleModelName { get; set; }
        [OSStructureField] public List<CompensationRow> Rows { get; set; }
    }
}
