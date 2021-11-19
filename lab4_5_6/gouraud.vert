#version 330 core

struct Material {
    vec3 Ka;
    vec3 Kd;
    vec3 Ks;
    float p;
};

struct Light {
    vec3 intensity;
    vec3 pos;
    float attenuation;
};

layout (location = 0) in vec3 position;
layout (location = 1) in vec3 inColor;
layout (location = 2) in vec3 inNormal;

out vec3 normal;
out vec3 color;

uniform mat4 proj;
uniform mat4 view;
uniform mat4 model;

uniform Material material;
uniform Light light;
uniform vec3 ambientIntensity;

uniform bool animate;
uniform uint curTime;

void main()
{
    mat4 viewmodel = view * model;
    normal = normalize(vec3(viewmodel * vec4(inNormal, 0.0)));
    gl_Position = proj * viewmodel * vec4(position, 1.0);
    
    vec3 vertCoord = vec3(viewmodel * vec4(position, 1.0));
    vec3 toLight = normalize(light.pos - vertCoord);
    
    vec3 ambient = ambientIntensity * material.Ka;
    
    float diffuseCoef = max(dot(toLight, normal), 0);
    vec3 diffuse = light.intensity * material.Kd * diffuseCoef;
    
    vec3 toCamera = normalize(-vertCoord);
    vec3 reflectDir = reflect(-toLight, normal);
    float specularCoef = 0;
    if (diffuseCoef > 1e-6) {
        specularCoef = pow(max(dot(reflectDir, toCamera), 0), material.p);
    }
     vec3 specular = light.intensity * material.Ks * specularCoef;
    
    float distToLight = length(light.pos - vertCoord);
    float attenuation = (1 + light.attenuation * distToLight * distToLight);
    
    if (animate) {
        color = sin(inColor + curTime / 3000000.0) / 2 + 0.5;
    } else {
        color = inColor;
    }
    color = color * (ambient + (diffuse + specular) / attenuation);
}