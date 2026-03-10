namespace VsFoundation.Sequence.Sequences.Plasma.IndexPusher.Controller.Interfaces;

public interface IPusherCylinder
{
    void Up(int lane);
    void Down(int lane);
    bool IsUp(int lane);
    bool IsDown(int lane);
}
