using Godot;
using System;

public partial class ParamaterEditor : Node2D
{
	[Export] private VBoxContainer abilitiesContainer;
	[Export] private MarginContainer jumpContainer;
	[Export] private MarginContainer doubleJumpContainer;
	[Export] private MarginContainer wallJumpContainer;
	[Export] private MarginContainer dashContainer;
	[Export] private MarginContainer climbContainer;

	public override void _Ready() {

	}
	
	//// ABILITIES MENU 
	
	public void _on_ability_menu_toggle_button_pressed() {
		// Toggle visibility of the jump parameter container
		abilitiesContainer.Visible = !abilitiesContainer.Visible;
	}
	
	//// JUMP 
	
	public void _on_jump_toggle_button_pressed() {
		// Toggle visibility of the jump parameter container
		jumpContainer.Visible = !jumpContainer.Visible;
	}
	
	public void _on_variable_height_jump_check_button_toggled(bool value) {
		PlayerController.pc.jump.variableHeight = value;
	}
	
	public void _on_jump_height_slider_value_changed(float value) {
		PlayerController.pc.jump.airAcceleration = value;
	}
	
	public void _on_jump_cutoff_slider_value_changed(float value) {
		PlayerController.pc.jump.cutoff = value;
	}
	
	public void _on_jump_gravity_slider_value_changed(float value) {
		PlayerController.pc.gravity = value;
	}

	//// DOUBLE JUMP 
	
	public void _on_double_jump_toggle_button_pressed() {
		// Toggle visibility of the jump parameter container
		doubleJumpContainer.Visible = !doubleJumpContainer.Visible;
	}
	
	public void _on_enable_double_jump_check_button_toggled(bool value) {
		PlayerController.pc.jump.doubleJump = value;
	}
	
	public void _on_dj_air_acceleration_slider_value_changed(float value) {
		PlayerController.pc.jump.djAirAcceleration = value;
	}

	//// DOUBLE JUMP 
	
	public void _on_wall_jump_toggle_button_pressed() {
		// Toggle visibility of the jump parameter container
		wallJumpContainer.Visible = !wallJumpContainer.Visible;
	}
	
	public void _on_enable_wall_friction_check_button_toggled(bool value) {
		PlayerController.pc.wallJump.wallFriction = value;
	}
	
	public void _on_wall_jump_height_slider_value_changed(float value) {
		PlayerController.pc.wallJump.height = value;
	}
	
	public void _on_wall_jump_distance_slider_value_changed(float value) {
		PlayerController.pc.wallJump.distance = value;
	}
	
	public void _on_wall_jump_grip_time_slider_value_changed(float value) {
		PlayerController.pc.wallJump.gripTime = value;
	}
	
	//// DASH
	
	public void _on_dash_toggle_button_pressed() {
		// Toggle visibility of the dash parameter container
		dashContainer.Visible = !dashContainer.Visible;
	}
	
	public void _on_dash_speed_slider_value_changed(float value) {
		PlayerController.pc.dash.speed = value;
	}
	
	public void _on_dash_distance_slider_value_changed(float value) {
		PlayerController.pc.dash.distance = value;
	}
	
	public void _on_dash_speed_cooldown_value_changed(float value) {
		PlayerController.pc.dash.cooldown = value;
	}
	
	//// CLIMB
	
	public void _on_climb_toggle_button_pressed() {
		// Toggle visibility of the dash parameter container
		climbContainer.Visible = !climbContainer.Visible;
	}
	
	public void _on_climb_speed_slider_value_changed(float value) {
		PlayerController.pc.climb.climbSpeed = value;
	}
}
