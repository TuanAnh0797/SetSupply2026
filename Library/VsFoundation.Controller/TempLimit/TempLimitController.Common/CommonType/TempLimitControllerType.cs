namespace VsFoundation.Controller.TempLimit.TempLimitController.Common.CommonType;

public enum eTempLimitProtocolType { ModBus, PCLinkVXSeries, PCLinkSTSeries }
public enum eTempLimitChannel { CH1, CH2, CH3, CH4 }
public enum eTempLimitAlarmType { Off, AbsoluteUpper, AbsoluteLower, UpperDeviation, LowerDeviation }
public enum eTempLimitInputType { K, T, R, B, S, N, J, E, L, JPT100, PT100 }
public enum eTempLimitUnit { DegreeC , DegreeF }
public enum eTempLimitType {VXSeries,STSeries,M74Series }
