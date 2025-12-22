extends Label
@export var stf : Sprite2D
@export var fv : Sprite2D
@export var vf2 : Sprite2D
@export var omg : Sprite2D
@export var hp : Node2D

@export var stfa : AudioStreamPlayer
@export var vf2a : AudioStreamPlayer
@export var fva : AudioStreamPlayer
@export var omga : AudioStreamPlayer

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
		"fv":
			stf.hide()
			fv.show()
			vf2.hide()
			omg.hide()
		"vf2":
			stf.hide()
			fv.hide()
			vf2.show()
			omg.hide()
		"omg":
			stf.hide()
			fv.hide()
			vf2.hide()
			omg.show()

func _process(delta: float) -> void:
	# rotation
	time += delta
	var rot = sin(time/2) * 0.25 # 0.25 is the scale of the object
	
	stf.scale.x = rot
	fv.scale.x = rot
	vf2.scale.x = rot
	omg.scale.x = rot

func _on_popup_menu_id_pressed(id: int) -> void:
	match id:
		0:
			stf.show()
			fv.hide()
			vf2.hide()
			omg.hide()
			stfa.play()
		2:
			stf.hide()
			fv.show()
			vf2.hide()
			omg.hide()
			fva.play()
		1:
			stf.hide()
			fv.hide()
			vf2.show()
			omg.hide()
			vf2a.play()
		3:
			stf.hide()
			fv.hide()
			vf2.hide()
			omg.show()
			omga.play()
