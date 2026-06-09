extends Sprite2D

@export var progress : RichTextLabel
var rng = RandomNumberGenerator.new()

func _ready() -> void:
	var magic_number = rng.randi_range(1, 69420)
	if magic_number == 67:
		watermark()

func watermark() -> void:
	if OS.get_name() == "Windows":
		progress.append_text("[W] Could not validate license. Please go to settings and activate HoneyPatcher.")
		visible = true
