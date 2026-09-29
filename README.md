# odcCode

## Compensation Export Library

An OutSystems external library for generating compensation structure reports in CSV format.

### Overview

This library takes compensation version data and generates a ZIP archive containing 5 CSV files with compensation models and salary scale information.

### Input Structure

The method accepts a list of `CompensationVersionExport` objects, each containing:

- **SalaryScaleModelName**: The fully formatted model name string that includes the base model name and the metadata values:
  - Final CPI
  - Structure Limit (SL)
  - Merit Element (ME)
  - Market (MKT)
  - CPH

**Format:** `{BaseName} Final CPI {CPI} SL {StructureLimit} ME {MeritElement} MKT {Market} CPH {CPH}`

**Example:** `FY26 Afghanistan (AF) Final CPI -3.7 SL 100 ME 3 MKT WBG Net TC 75 CPH 2`

- **Rows**: List of `CompensationRow` objects with compensation details

### Output Structure

The ZIP archive contains exactly 5 CSV files:

1. **Scale Output Report.csv** - All staff compensation records with all versions
2. **ET Scale Output Report.csv** - ET (Expert Track) appointment records
3. **ST Scale Output Report.csv** - ST (Staff Track) appointment records
4. **UC_UA Scale Output Report.csv** - UC/UA scale aggregations
5. **UJ Scale Output Report.csv** - UJ scale (derived from GD grade)

All files include the **Structure Model Name** column as the first column. It contains the complete formatted string from `SalaryScaleModelName`.

### Grade Mappings

The library maintains the following grade transformations:

- **ET Grades**: GA→ET1, GB→ET2, GC→ET3, GD→ET4, GE→EC1, GF→EC2, GG→EC3, GH→EC4
- **ST Grades**: GA→T1, GB→T2, GC→T3, GD→T4, GE→C1, GF→C2, GG→C3, GH→C4
- **UC/UA Grades**: GA-GD (UA band), GE-GH (UC band)
- **Excluded Grades**: G1 (never displayed)

### Special Processing

- ST grades (Consultant/Tracker) are converted from annual to daily or hourly rates based on divisors
- UC/UA reports aggregate min/max from grade bands and calculate midpoint with ceiling rounding
- UJ is derived from the GD grade with specific calculation logic
- All decimal values preserve full precision unless specifically rounded

### Method Calling

Before calling `GenerateCompensationZip`, populate `SalaryScaleModelName` in each `CompensationVersionExport` with the complete formatted value. The library uses that value as-is for the first **Structure Model Name** column in every CSV file.
