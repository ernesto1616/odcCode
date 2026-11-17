using OutSystems.ExternalLibraries.SDK;

namespace CompensationExportLibrary.Models
{
    [OSStructure(Description = "One row of the US Scale Output Report (Non Staff grades)")]
    public struct USScaleRow
    {
        [OSStructureField] public string StructureModelName { get; set; }
        [OSStructureField] public string EffectiveDate { get; set; }
        [OSStructureField] public string Region { get; set; }
        [OSStructureField] public string Country { get; set; }
        [OSStructureField] public string SalaryPlan { get; set; }
        [OSStructureField] public string PayType { get; set; }
        [OSStructureField] public string Category { get; set; }
        [OSStructureField] public string PayFrequency { get; set; }
        [OSStructureField] public string GradeGroup { get; set; }
        [OSStructureField] public string Grade { get; set; }
        [OSStructureField] public decimal AvgMinimum { get; set; }
        [OSStructureField] public decimal Midpoint { get; set; }
        [OSStructureField] public decimal AvgMaximum { get; set; }
        [OSStructureField] public string Currency { get; set; }
        [OSStructureField] public string Code { get; set; }
        [OSStructureField] public string Status { get; set; }
        [OSStructureField] public decimal ScaleRounding { get; set; }
        [OSStructureField] public decimal TotalMeritIncrease { get; set; }
        [OSStructureField] public decimal StructureAdjustment { get; set; }
        [OSStructureField] public decimal MeritElement { get; set; }
        [OSStructureField] public decimal CPIInflation { get; set; }
        [OSStructureField] public string HQCO { get; set; }
        [OSStructureField] public bool HasMinimum { get; set; }
        [OSStructureField] public bool HasMidpoint { get; set; }
        [OSStructureField] public bool HasMaximum { get; set; }
    }
}
