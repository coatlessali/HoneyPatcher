extends RichTextLabel

func _on_show_log_pressed() -> void:
	if visible:
		hide()
	else:
		show()
