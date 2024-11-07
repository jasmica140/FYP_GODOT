using Godot;

public abstract class Ability 
{
	protected PlayerController player;

	public Ability(PlayerController playerController)
	{
		player = playerController;
	}

	public abstract void Activate();
	public virtual void Deactivate() { }
	
	//public abstract void displayStats();
}
