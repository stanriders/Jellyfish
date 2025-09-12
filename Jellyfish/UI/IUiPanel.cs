namespace Jellyfish.UI;

public interface IUiPanel
{
    void Frame(double timeElapsed);
    void Unload();
}