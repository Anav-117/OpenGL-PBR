out vec4 FragColor;

in vec3 Normal;
in vec3 FragPos;
in vec2 TexCoord;

struct Material {
	sampler2D diffuse;
	sampler2D specular;
	sampler2D emission;
	float shininess;
};

struct Light {
	vec3 position;
	vec3 direction; //for directional lighting
	float cutOff; //for spotlights
	float outerCutOff; //for spotlights 

	vec3 ambient;
	vec3 diffuse;
	vec3 specular;

	float AttConstant;  //attenuation
	float AttLinear;
	float AttQuadriatic;
};

uniform Material material;
uniform Light light;
uniform vec3 viewPos;
uniform float time;

void main() {
   //ambient
   vec3 ambient = light.ambient * vec3(texture(material.diffuse, TexCoord));
   
   //normal and light direction
   vec3 norm = normalize(Normal);
   #ifdef DIRECTIONAL
   vec3 lightDir = normalize(-light.direction);
   #endif
   #ifdef POINT
   vec3 lightDir = normalize(light.position - FragPos);
   #endif
   #ifdef SPOTLIGHT
   vec3 lightDir = normalize(light.position - FragPos);
   float theta = dot(lightDir, normalize(-light.direction));
   float epsilon = light.cutOff - light.outerCutOff;
   float intensity = clamp((theta - light.outerCutOff) / epsilon, 0.0, 1.0);
   #endif

   //diffuse
   float diff = max(dot(norm, lightDir), 0.0);
   vec3 diffuse = light.diffuse * diff * (vec3(texture(material.diffuse, TexCoord)));

   //specular
   vec3 viewDir = normalize(viewPos - FragPos);
   vec3 reflectDir = reflect(-lightDir, norm);
   float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
   vec3 specular = light.specular * (spec * vec3(texture(material.specular, TexCoord)));

   //vec3 emission = vec3(0.0f);
   //if (texture(material.specular, TexCoord).r == 0) {
	//	emission = vec3(texture(material.emission, TexCoord));
   //}

   //attenuation
   #if defined POINT || defined SPOTLIGHT
   float distance = length(light.position - FragPos);
   float attenuation = 1.0 / (light.AttConstant + light.AttLinear * distance + light.AttQuadriatic * distance * distance); 
   #else 
   float attenuation = 1.0;
   #endif

   #ifdef SPOTLIGHT
   if (theta > light.cutOff) {
		  vec3 result = (ambient + diffuse + specular) * attenuation;// + emission);
		  FragColor = vec4(result, 1.0f);
   }
   else {
		  vec3 result = (ambient + (diffuse + specular) * intensity) * attenuation;
		  FragColor = vec4(result, 1.0f);
   }
   #else
   vec3 result = (ambient + diffuse + specular) * attenuation;// + emission);
   FragColor = vec4(result, 1.0f);
   #endif
}