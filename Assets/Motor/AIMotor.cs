using UnityEngine;

public class AIMotor : BaseMotor
{
    protected override void UpdateMotor()
    {
        // Get the input
        MoveVector = Direction();
        // Send the input to a filter
        MoveVector = state.ProcessMotion(MoveVector);
        RotationQuaternion = state.ProcessRotation(MoveVector);
        // Check if we need to change current state
        state.Transition();
        //Move
        Move();
        Rotate();
    }
    public Vector3 Direction()
    {
        return Vector3.zero;
    }
}
