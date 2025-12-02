shader_type spatial;

uniform sampler2D texture_albedo : source_color, filter_nearest;

void vertex() {
	VERTEX += NORMAL * 0.001f;
}

void fragment() {
	vec4 color = texture(texture_albedo, UV);
	ALBEDO = color.rgb;
	ALPHA = color.a;
}