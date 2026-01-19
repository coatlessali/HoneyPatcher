extends Label

var rng = RandomNumberGenerator.new()

func _ready() -> void:
	var magic_number = rng.randi_range(1, 10)
	text = "HoneyPatcher: Arcade Stage 8 Infinity +B"
	if magic_number == 1:
		text = "HoneyBadger: Arcade Stage 8 Infinity +B"
