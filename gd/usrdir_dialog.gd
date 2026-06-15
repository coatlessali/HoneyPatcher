extends FileDialog

func _ready() -> void:
	# Maybe we can come back to this at some point?
	if OS.get_name() == "Android":
		use_native_dialog = true

func _on_select_usrdir_pressed() -> void:
	show()
