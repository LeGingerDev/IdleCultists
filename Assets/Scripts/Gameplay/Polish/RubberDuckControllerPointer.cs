using UnityEngine;

namespace LGD.Gameplay.Polish
{
    public class RubberDuckControllerPointer : MonoBehaviour
{
    public RubberDuckController rubberDuckController;

    void Awake()
    {
        rubberDuckController = GetComponentInParent<RubberDuckController>();
    }

    public void Jump(float direction)
    {
        if (rubberDuckController != null)
        {
            rubberDuckController.Jump(direction);
        }
    }

    public void JumpRandomDirection()
    {
        if (rubberDuckController != null)
        {
            rubberDuckController.JumpRandomDirection();
        }
    }
}
}