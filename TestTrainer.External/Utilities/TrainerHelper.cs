using System.Numerics;

namespace TestTrainer.External.Utilities;

public static class TrainerHelper
{
    /// <summary>
    /// Gives you the new coordinates of a game object, based on wherever the camera of the object is looking at.
    /// </summary>
    /// <param name="currentPosition"></param>
    /// <param name="yaw"></param>
    /// <param name="pitch"></param>
    /// <param name="distance"></param>
    /// <returns></returns>
    public static Vector3 TeleportForward(Vector3 currentPosition, float yaw, float pitch, float distance)
    {
        var forward = new Vector3(
            (float)(-Math.Sin(yaw * Math.PI / 180f) * Math.Cos(pitch * Math.PI / 180f)),
            (float)(Math.Cos(yaw * Math.PI / 180f) * Math.Cos(pitch * Math.PI / 180f)),
            (float)-Math.Sin(pitch * Math.PI / 180f)
        );

        var newPosition = currentPosition + forward * distance;

        newPosition.Z = currentPosition.Z += pitch;

        return newPosition;
    }

    /// <summary>
    /// Gives you the new coordinates of a game object, based on wherever the camera of the object is looking at.
    /// </summary>
    /// <param name="currentPosition"></param>
    /// <param name="yaw"></param>
    /// <param name="pitch"></param>
    /// <param name="distance"></param>
    /// <returns></returns>
    public static Vector3 TeleportBackward(Vector3 currentPosition, float yaw, float pitch, float distance)
    {
        var forward = new Vector3(
            (float)(-Math.Sin(yaw * Math.PI / 180f) * Math.Cos(pitch * Math.PI / 180f)),
            (float)(Math.Cos(yaw * Math.PI / 180f) * Math.Cos(pitch * Math.PI / 180f)),
            (float)-Math.Sin(pitch * Math.PI / 180f)
        );

        var newPosition = currentPosition - forward * distance;

        newPosition.Z = currentPosition.Z -= pitch;

        return newPosition;
    }

    /// <summary>
    /// Gives you the new coordinates of a game object, based on wherever the camera of the object is looking at.
    /// </summary>
    /// <param name="currentPosition"></param>
    /// <param name="yaw"></param>
    /// <param name="distance"></param>
    /// <returns></returns>
    public static Vector3 TeleportForwardWithoutZ(Vector3 currentPosition, float yaw, float distance)
    {
        var forward = new Vector3(
            (float)(-Math.Sin(yaw * Math.PI / 180f) * Math.Cos(-45f * Math.PI / 180f)),
            (float)(Math.Cos(yaw * Math.PI / 180f) * Math.Cos(-45f * Math.PI / 180f)),
            (float)-Math.Sin(-45f * Math.PI / 180f)
        );

        var newPosition = currentPosition + forward * distance;

        newPosition.Z = currentPosition.Z;

        return newPosition;
    }
}