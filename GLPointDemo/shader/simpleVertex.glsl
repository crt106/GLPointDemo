in vec3 vPosition;
in vec3 vColor;  
out vec3 pass_Color;

uniform mat4 viewMatrix;

void main(){
  gl_Position = viewMatrix * vec4(vPosition, 1.0);
  pass_Color = vColor;
}