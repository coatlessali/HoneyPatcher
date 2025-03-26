extends Label

func _ready() -> void:
	pass
func _process(delta: float) -> void:
	pass
func _on_select_usrdir_mouse_entered() -> void:
	text = "Set the path of your USRDIR for the PS3 version of Sonic the Fighters. (Do this before installing mods.)"
func _on_restore_usrdir_mouse_entered() -> void:
	text = "Restore your USRDIR to a vanilla state."
func _on_psarc_mouse_entered() -> void:
	text = "Opens your mods folder."
func _on_install_mouse_entered() -> void:
	text = "Install all mods present in the \"mods\" folder. (Priority is alphanumerical order.)"
func _on_unpack_mouse_entered() -> void:
	text = "Unpack all game files into a HoneyPatcher compatible package."
func _on_gen_patches_mouse_entered() -> void:
	text = "Generate patches for rom_*.bin. See GitHub wiki for more details."
