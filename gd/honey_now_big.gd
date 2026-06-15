extends Sprite2D

func _ready() -> void:
	hide()

func _physics_process(_delta: float) -> void:
	if modulate.a > 0.0:
		modulate.a -= 0.025
