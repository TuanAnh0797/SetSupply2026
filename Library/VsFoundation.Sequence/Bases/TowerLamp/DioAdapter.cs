namespace VsFoundation.Sequence.Bases.TowerLamp;

public sealed class DioAdapter : IDiscreteOutput
{
    private readonly dynamic _dio;
    public DioAdapter(string name, dynamic dio) { Name = name; _dio = dio; }
    public string Name { get; }
    public bool IsOn => _dio.IsOn();
    public void On() => _dio.On();
    public void Off() => _dio.Off();
}
