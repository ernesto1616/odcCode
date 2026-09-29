using System.Collections.Generic;
using OutSystems.ExternalLibraries.SDK;

namespace CompensationExportLibrary.Models
{
    [OSStructure(Description = "One compensation model/version to append to the five combined reports")]
    public struct CompensationVersionExport
    {
        [OSStructureField] public string SalaryScaleModelName { get; set; }
        [OSStructureField] public List<CompensationRow> Rows { get; set; }
    }
}
