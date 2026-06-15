extends Sprite2D

func _ready() -> void:
	modulate.a = 0.0

func _process(_delta: float) -> void:
	if modulate.a > 0:
		modulate.a -= 0.005
