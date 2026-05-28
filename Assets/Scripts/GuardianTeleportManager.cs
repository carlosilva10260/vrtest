using UnityEngine;

public abstract class GuardianTeleportManager : MonoBehaviour
{
    public abstract Vector3 PredictFinalUserPosition(Vector3 selectedTeleportPos, Vector3 currentForward);
    public abstract Vector3 PredictFinalGuardianCenter(Vector3 selectedTeleportPos, Vector3 currentForward);

    public abstract void RemoveTarget(Transform target);
}