using OutSystems.ExternalLibraries.SDK;

namespace CompensationExportLibrary.Models
{
    [OSStructure(Description = "A single row of the compensation SQL output")]
    public struct CompensationRow
    {
        [OSStructureField] public string SalaryScaleModelName { get; set; }
        [OSStructureField] public string EffectiveDate { get; set; }
        [OSStructureField] public string Grade { get; set; }
        [OSStructureField] public decimal ProposedMinimum { get; set; }
        [OSStructureField] public decimal ProposedMidpoint { get; set; }
        [OSStructureField] public decimal ProposedMaximum { get; set; }
        [OSStructureField] public decimal ScaleRounding { get; set; }
        [OSStructureField] public decimal TotalMeritIncrease { get; set; }
        [OSStructureField] public decimal StructureAdjustement { get; set; }
        [OSStructureField] public decimal MeritElement { get; set; }
        [OSStructureField] public decimal CPIInflation { get; set; }
    }
}
