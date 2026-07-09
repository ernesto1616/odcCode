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

        [OSStructureField] public string Region { get; set; }
        [OSStructureField] public string Country { get; set; }
        [OSStructureField] public string SalaryPlan { get; set; }
        [OSStructureField] public string PayType { get; set; }
        [OSStructureField] public string PayFrequency { get; set; }
        [OSStructureField] public string GradeGroup { get; set; }
        [OSStructureField] public string Currency { get; set; }
        [OSStructureField] public string Code { get; set; }
        [OSStructureField] public string Status { get; set; }
        [OSStructureField] public string HCCO { get; set; }
    }
}
