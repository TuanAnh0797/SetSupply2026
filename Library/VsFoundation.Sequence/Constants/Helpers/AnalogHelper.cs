namespace VsFoundation.Sequence.Constants.Helpers;

public static class AnalogHelper
{
    /// <summary>
    /// Calculate the Analog Value from the Actual Value.
    /// </summary>
    /// <param name="realValue">Actual Value</param>
    /// <param name="realMin">Minimum Actual Value</param>
    /// <param name="realMax">Maximum Actual Value</param>
    /// <param name="analogMin">Minimum Analog Value</param>
    /// <param name="analogMax">Maximum Analog Value</param>
    /// <returns>Corresponding Analog Value</returns>
    public static double RealToAnalog(double realValue,
        double realMin, double realMax, double analogMin, double analogMax)
    {
        var result = ((realValue - realMin) / (realMax - realMin)) * (analogMax - analogMin) + analogMin;
        return result;
    }

    /// <summary>
    /// Calculate the Actual Value from the Analog Value.
    /// </summary>
    /// <param name="analogValue">Analog Value</param>
    /// <param name="analogMin">Minimum Analog Value</param>
    /// <param name="analogMax">Maximum Analog Value</param>
    /// <param name="realMin">Minimum Actual Value</param>
    /// <param name="realMax">Maximum Actual Value</param>
    /// <returns>Corresponding Actual Value</returns>
    public static double AnalogToReal(double analogValue, double analogMin, double analogMax, double realMin, double realMax)
    {
        var x = ((analogValue - analogMin) / (analogMax - analogMin)) * (realMax - realMin) + realMin;
        return Math.Round(x, 2);
    }
    /// <summary>
    /// Calculate the Analog Value from the Actual Value.
    /// </summary>
    /// <param name="realValue">Actual Value</param>
    /// <param name="analogMin">Minimum Analog Value</param>
    /// <param name="analogMax">Maximum Analog Value</param>
    /// <param name="realMin">Minimum Actual Value</param>
    /// <param name="realMax">Maximum Actual Value</param>
    /// <returns>Corresponding Analog Value</returns>
    public static double RealToAnalogDigital(double realValue, double analogMin, double analogMax, double realMin, double realMax)
    {
        return ((realValue - realMin) / (realMax - realMin)) * (analogMax - analogMin) + analogMin;
    }
}
