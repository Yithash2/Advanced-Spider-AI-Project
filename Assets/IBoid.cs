using UnityEngine;

public interface IBoid
{
    public Group CurrentGoup {get; set; }
    public Vector2 Velocity {get;}
    public Vector2 Position {get;}
    public bool IsLost { get;}

    public void DeclaredFound();
    public void DeclaredLost();

    public void RemoveGroup();
    public void AddGroup(Group group2, Color teamColor);
}
