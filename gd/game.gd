extends Label
@export var stf : Sprite2D
@export var fv : Sprite2D
@export var vf2 : Sprite2D
@export var omg : Sprite2D
@export var daytona : Sprite2D
@export var hp : Node2D

@export var daytona_background : Sprite2D
@export var box_left : Sprite2D
@export var box : Sprite2D
@export var box_right : Sprite2D

@export var stfa : AudioStreamPlayer
@export var vf2a : AudioStreamPlayer
@export var fva : AudioStreamPlayer
@export var omga : AudioStreamPlayer
@export var daytonaa : AudioStreamPlayer

var time = 0.0

func _ready() -> void:
	# dirty hack to prevent it loading the default of stf
	await get_tree().create_timer(0.1).timeout
	match hp.game:
		"stf":
			stf.show()
			fv.hide()
			vf2.hide()
			omg.hide()
			daytona.hide()
			box.modulate.a = 0.0
			box_right.modulate.a = 0.0
			box_left.modulate.a = 0.0
			daytona_background.modulate.a = 0.0
		"fv":
			stf.hide()
			fv.show()
			vf2.hide()
			omg.hide()
			daytona.hide()
			box.modulate.a = 0.0
			box_right.modulate.a = 0.0
			box_left.modulate.a = 0.0
			daytona_background.modulate.a = 0.0
		"vf2":
			stf.hide()
			fv.hide()
			vf2.show()
			omg.hide()
			daytona.hide()
			box.modulate.a = 0.0
			box_right.modulate.a = 0.0
			box_left.modulate.a = 0.0
			daytona_background.modulate.a = 0.0
		"omg":
			stf.hide()
			fv.hide()
			vf2.hide()
			omg.show()
			daytona.hide()
			box.modulate.a = 0.0
			box_right.modulate.a = 0.0
			box_left.modulate.a = 0.0
			daytona_background.modulate.a = 0.0
		"daytona":
			stf.hide()
			fv.hide()
			vf2.hide()
			omg.hide()
			daytona.show()
			daytona_desc(true)
			box.modulate.a = 1.0
			box_right.modulate.a = 1.0
			box_left.modulate.a = 1.0
			daytona_background.modulate.a = 1.0

func _process(delta: float) -> void:
	# rotation
	time += delta
	var rot = sin(time/2) * 0.25 # 0.25 is the scale of the object
	
	stf.scale.x = rot
	fv.scale.x = rot
	vf2.scale.x = rot
	omg.scale.x = rot
	daytona.scale.x = rot

func _on_popup_menu_id_pressed(id: int) -> void:
	stf.hide()
	fv.hide()
	vf2.hide()
	omg.hide()
	daytona.hide()
	match id:
		0:
			stf.show()
			stfa.play()
			daytona_desc(false)
		2:
			fv.show()
			fva.play()
			daytona_desc(false)
		1:
			vf2.show()
			vf2a.play()
			daytona_desc(false)
		3:
			omg.show()
			omga.play()
			daytona_desc(false)
		4:
			daytona.show()
			daytonaa.play()
			daytona_desc(true)

func daytona_desc(flag):
	var target_alpha = 1.0 if flag else 0.0
	var tween = create_tween()
	tween.set_parallel(true)
	tween.tween_property(box_left, "modulate:a", target_alpha, 0.5)
	tween.tween_property(box_right, "modulate:a", target_alpha, 0.5)
	tween.tween_property(box, "modulate:a", target_alpha, 0.5)
	tween.tween_property(daytona_background, "modulate:a", target_alpha, 0.5)
