shader_type canvas_item;

uniform float time = 0.0;
uniform float breathe = 0.0;
uniform float shimmer = 0.0;
uniform vec2 pan_offset = vec2(0.0);
uniform float zoom = 1.0;

void fragment() {
	vec2 uv = UV;
	vec2 centered = uv - vec2(0.5, 0.5);
	centered /= max(zoom, 0.001);
	uv = centered + vec2(0.5, 0.5) + pan_offset;

	vec2 from_center = uv - vec2(0.5, 0.42);

	uv.x += from_center.x * breathe * 0.008;
	uv.y += from_center.y * breathe * 0.006;

	uv = clamp(uv, vec2(0.002), vec2(0.998));
	vec4 col = texture(TEXTURE, uv);

	float sky = smoothstep(0.08, 0.42, uv.y);
	float water_hint = smoothstep(0.48, 0.62, uv.y) * (1.0 - smoothstep(0.15, 0.85, abs(uv.x - 0.38)));
	float sparkle = sin(time * 1.4 + uv.x * 24.0) * sin(time * 1.1 + uv.y * 18.0);
	sparkle = smoothstep(0.55, 1.0, sparkle);
	col.rgb += vec3(0.85, 0.95, 1.0) * water_hint * shimmer * sparkle * 0.05;
	col.rgb += vec3(0.55, 0.78, 0.95) * sky * shimmer * 0.025;

	COLOR = col;
}
