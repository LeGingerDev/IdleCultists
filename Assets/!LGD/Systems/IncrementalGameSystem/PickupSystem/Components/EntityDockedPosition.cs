using UnityEngine;

public class EntityDockedPosition : MonoBehaviour
{
    public EntityController entity;
    public void Assign(EntityController entity)
    {
        this.entity = entity;
    }

    public void Remove()
    {
        this.entity = null;
    }

    public bool IsOccupied()
    {
        return entity != null;
    }
}