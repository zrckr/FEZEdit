shader_type spatial;

render_mode unshaded;

uniform bool orthogonal;
uniform float grid_size;

void vertex() {
	// From FLAG_SRGB_VERTEX_COLOR.
	if (!OUTPUT_IS_SRGB) {
		COLOR.rgb = mix(pow((COLOR.rgb + vec3(0.055)) * (1.0 / (1.0 + 0.055)), vec3(2.4)), COLOR.rgb * (1.0 / 12.92), lessThan(COLOR.rgb, vec3(0.04045)));
	}
}

void fragment() {
	ALBEDO = COLOR.rgb;
	vec3 dir = orthogonal ? -vec3(0, 0, 1) : VIEW;
	float angle_fade = abs(dot(dir, NORMAL));
	angle_fade = smoothstep(0.05, 0.2, angle_fade);
	
	vec3 world_pos = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
	vec3 world_normal = (INV_VIEW_MATRIX * vec4(NORMAL, 0.0)).xyz;
	vec3 camera_world_pos = INV_VIEW_MATRIX[3].xyz;
	vec3 camera_world_pos_on_plane = camera_world_pos * (1.0 - world_normal);
	float dist_fade = 1.0 - (distance(world_pos, camera_world_pos_on_plane) / grid_size);
	dist_fade = smoothstep(0.02, 0.3, dist_fade);
	
	ALPHA = COLOR.a * dist_fade * angle_fade;
}