shader_type spatial;
render_mode blend_mix, cull_front, unshaded;

uniform vec4 color : source_color = vec4(1);
uniform float size : hint_range(1.0, 10.0, 0.01) = 1.25;

void vertex() {
	VERTEX *= size;
}

void fragment() {
	ALBEDO = color.rgb;
	ALPHA = color.a;
}