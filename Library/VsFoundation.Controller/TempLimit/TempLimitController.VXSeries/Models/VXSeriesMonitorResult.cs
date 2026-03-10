
namespace VsFoundation.Controller.TempLimit.TempLimitController.VXSeries.Models;

public class VXSeriesMonitorResult
{
    /// <summary>
    /// Current temperature - 0000
    /// 
    /// </summary>
    public float CPV { get; set; } = 0;

    /// <summary>
    /// Current set temperature - 0001
    /// </summary>
    public float CSV { get; set; } = 0;

    /// <summary>
    /// Final set temperature - 0002
    /// </summary>
    public float TSV { get; set; } = 0;

    /// <summary>
    /// Dot point position - 0003
    /// </summary>
    public int DPP { get; set; } = 0;


    /// <summary>
    /// 0004
    /// </summary>
    public eVXSeriesUnit Unit { get; set; } = eVXSeriesUnit.DegreeC;


    /// <summary>
    /// 0009
    /// </summary>
    public eVXSeriesSVNumber SVNumber { get; set; } = eVXSeriesSVNumber.SV1;

    /// <summary>
    /// 0020
    /// </summary>
    public eVXSeriesStatus Alarm1Status { get; set; } = eVXSeriesStatus.OFF;

    /// <summary>
    /// 0021
    /// </summary>
    public eVXSeriesStatus Alarm2Status { get; set; } = eVXSeriesStatus.OFF;

    /// <summary>
    /// 0022
    /// </summary>
    public eVXSeriesStatus Alarm3Status { get; set; } = eVXSeriesStatus.OFF;

    /// <summary>
    /// 0023
    /// </summary>
    public eVXSeriesStatus Alarm4Status { get; set; } = eVXSeriesStatus.OFF;

}

