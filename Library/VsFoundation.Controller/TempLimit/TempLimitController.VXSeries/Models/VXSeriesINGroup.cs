namespace VsFoundation.Controller.TempLimit.TempLimitController.VXSeries.Models;

public class VXSeriesINGroup
{
    /// <summary>
    /// 0900
    /// </summary>
    public eVXSeriesInputType InputType { get; set; } = eVXSeriesInputType.K0;

    /// <summary>
    /// 0901
    /// </summary>
    public eVXSeriesUnit Unit { get; set; } = eVXSeriesUnit.DegreeC;

    /// <summary>
    /// Reference Junction Compensation 0907
    /// </summary>
    public eVXSeriesStatus RJC { get; set; } = eVXSeriesStatus.ON;

    /// <summary>
    /// 0909
    /// </summary>
    public float BIAS { get; set; } = 0;


}
public enum eVXSeriesInputType { K0 = 1, K1 = 2, JO = 3, J1 = 4, E1 = 5, T1 = 5, R0 = 7, B0 = 8, S0 = 9, L1 = 10, N0 = 11, U1 = 12, W0 = 13, PL0 = 14, JPT0 = 20, JPT1 = 21, PT0 = 22, PT1 = 23 }
public enum eVXSeriesUnit { DegreeC = 0, DegreeF = 1 }
public enum eVXSeriesStatus { ON = 1, OFF = 0 }