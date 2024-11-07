using Godot;
using System;

public class ParameterEditor : Control
{
	private PlayerController playerController;

	// Nodes for UI elements
	private Button jumpToggleButton;
	private VBoxContainer jumpContainer;

	private Button dashToggleButton;
	private VBoxContainer dashContainer;

	public override void _Ready()
	{
		// Assume PlayerController is available in the scene or assigned
		playerController = GetNode<PlayerController>("/root/PlayerController");

		// Setup UI components for Jump
		jumpToggleButton = GetNode<Button>("VBoxContainer/JumpPanel/JumpToggleButton");
		jumpContainer = GetNode<VBoxContainer>("VBoxContainer/JumpPanel/JumpContainer");
		jumpToggleButton.Connect("pressed", this, nameof(OnJumpTogglePressed));

		// Setup UI components for Dash
		dashToggleButton = GetNode<Button>("VBoxContainer/DashPanel/DashToggleButton");
		dashContainer = GetNode<VBoxContainer>("VBoxContainer/DashPanel/DashContainer");
		dashToggleButton.Connect("pressed", this, nameof(OnDashTogglePressed));

		// Connect parameter controls (e.g., sliders) for Jump and Dash
		HSlider jumpHeightSlider = GetNode<HSlider>("VBoxContainer/JumpPanel/JumpContainer/JumpHeightSlider");
		jumpHeightSlider.Connect("value_changed", this, nameof(OnJumpHeightChanged));

		HSlider dashSpeedSlider = GetNode<HSlider>("VBoxContainer/DashPanel/DashContainer/DashSpeedSlider");
		dashSpeedSlider.Connect("value_changed", this, nameof(OnDashSpeedChanged));
	}

	private void OnJumpTogglePressed()
	{
		// Toggle visibility of the jump parameter container
		jumpContainer.Visible = !jumpContainer.Visible;
	}

	private void OnDashTogglePressed()
	{
		// Toggle visibility of the dash parameter container
		dashContainer.Visible = !dashContainer.Visible;
	}

	private void OnJumpHeightChanged(float value)
	{
		// Update jump height in the Jump ability
		playerController.jump.height = value;
	}

	private void OnDashSpeedChanged(float value)
	{
		// Update dash speed in the Dash ability
		playerController.dash.speed = value;
	}
}
