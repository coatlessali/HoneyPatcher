extends Label
@export var stf : Sprite2D
@export var fv : Sprite2D
@export var vf2 : Sprite2D
@export var omg : Sprite2D
@export var daytona : Sprite2D
@export var hp : Node2D

@export var daytona_background : Sprite2D
@export var description : Label
@export var log : RichTextLabel
@export var title : Label
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
			white()
			daytona_desc(false)
			daytona_background.hide()
		"fv":
			stf.hide()
			fv.show()
			vf2.hide()
			omg.hide()
			daytona.hide()
			white()
			daytona_desc(false)
			daytona_background.hide()
		"vf2":
			stf.hide()
			fv.hide()
			vf2.show()
			omg.hide()
			daytona.hide()
			white()
			daytona_desc(false)
			daytona_background.hide()
		"omg":
			stf.hide()
			fv.hide()
			vf2.hide()
			omg.show()
			daytona.hide()
			white()
			daytona_desc(false)
			daytona_background.hide()
		"daytona":
			stf.hide()
			fv.hide()
			vf2.hide()
			omg.hide()
			daytona.show()
			black()
			daytona_desc(true)
			daytona_background.show()

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
	match id:
		0:
			stf.show()
			fv.hide()
			vf2.hide()
			omg.hide()
			daytona.hide()
			daytona_background.hide()
			white()
			stfa.play()
			daytona_desc(false)
		2:
			stf.hide()
			fv.show()
			vf2.hide()
			omg.hide()
			daytona.hide()
			daytona_background.hide()
			white()
			fva.play()
			daytona_desc(false)
		1:
			stf.hide()
			fv.hide()
			vf2.show()
			omg.hide()
			daytona.hide()
			daytona_background.hide()
			white()
			vf2a.play()
			daytona_desc(false)
		3:
			stf.hide()
			fv.hide()
			vf2.hide()
			omg.show()
			daytona.hide()
			daytona_background.hide()
			white()
			omga.play()
			daytona_desc(false)
		4:
			stf.hide()
			fv.hide()
			vf2.hide()
			omg.hide()
			daytona.show()
			daytona_background.show()
			black()
			daytonaa.play()
			daytona_desc(true)

func daytona_desc(flag):
	if flag:
		box_left.show()
		box_right.show()
		box.show()
	else:
		box_left.hide()
		box_right.hide()
		box.hide()

func white() -> void:
	description.remove_theme_color_override("font_color")
	title.remove_theme_color_override("font_color")
	log.remove_theme_color_override("default_color")
	pass

func black() -> void:
	#description.add_theme_color_override("font_color", Color.BLACK)
	#title.add_theme_color_override("font_color", Color.BLACK)
	#log.add_theme_color_override("default_color", Color.BLACK)
	pass
