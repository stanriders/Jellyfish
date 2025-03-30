using OpenTK.Mathematics;

namespace Jellyfish.Entities;

public interface IPhysicsEntity
{
    void ResetVelocity();
    void OnPhysicsPositionChanged(Vector3 position);
    void OnPhysicsRotationChanged(Quaternion rotation);
}