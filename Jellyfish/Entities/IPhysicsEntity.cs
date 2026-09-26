using OpenTK.Mathematics;

namespace Jellyfish.Entities;

public interface IPhysicsEntity
{
    void ResetVelocity();
    void OnPhysicsChanged(Vector3 position, Quaternion rotation);
}