namespace Jellyfish.Render;

public interface IRender
{
    void CreateBuffers();
    void OnMapLoad();
    void PreFrame();
    void Frame();
    void Unload();
    
    public bool IsReady { get; set; }
    public bool NeedToRecreateBuffers { get; set; }
}