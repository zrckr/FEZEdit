shader_type spatial;
render_mode unshaded;

uniform sampler2D texture_albedo : source_color;
uniform float texture_scale : hint_range(0.1, 10.0) = 1.0;
uniform vec4 albedo_tint : source_color = vec4(1.0, 1.0, 1.0, 1.0);

varying vec3 vertex_local;
varying vec3 camera_dir_local;

void vertex() {
    vertex_local = VERTEX;
    vec3 camera_pos_world = INV_VIEW_MATRIX[3].xyz;
    vec3 object_pos_world = MODEL_MATRIX[3].xyz;
    vec3 camera_dir_world = normalize(camera_pos_world - object_pos_world);
    camera_dir_local = normalize((inverse(MODEL_MATRIX) * vec4(camera_dir_world, 0.0)).xyz);
}

void fragment() {
    // Create a camera-facing coordinate system
    vec3 forward = normalize(camera_dir_local);
    vec3 right = normalize(cross(vec3(0.0, 1.0, 0.0), forward));
    vec3 up = cross(forward, right);
    
    // Project vertex position onto the camera-facing plane
    vec2 uv = vec2(
        dot(vertex_local, right),
        dot(vertex_local, up)
    ) * texture_scale * vec2(1.0, -1.0) + 0.5;
    
    vec4 texture_color = texture(texture_albedo, uv);
    ALBEDO = texture_color.rgb * albedo_tint.rgb;
}