#version 330 core

struct Material {
    vec3 color;
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

in vec3 normal;
in vec3 fragCoord;
out vec4 fragColor;

uniform Material material;
uniform Light light;
uniform vec3 ambientIntensity;
uniform bool blinn;

void main() {
    vec3 toLight = normalize(light.pos - fragCoord);
    
    vec3 ambient = ambientIntensity * material.Ka;
    
    float diffuseCoef = max(dot(toLight, normal), 0);
    vec3 diffuse = light.intensity * material.Kd * diffuseCoef;
    
    vec3 toCamera = normalize(-fragCoord);
    float specularCoef = 0;
    if (diffuseCoef > 0) {
        if (blinn) {
            vec3 halfwayDir = normalize(toLight + toCamera);
            specularCoef = pow(max(dot(halfwayDir, normal), 0), material.p);
        } else {
            vec3 reflectDir = reflect(-toLight, normal);
            specularCoef = pow(max(dot(reflectDir, toCamera), 0), material.p);
        }
    }
    vec3 specular = light.intensity * material.Ks * specularCoef;
    
    float distToLight = length(light.pos - fragCoord);
    float attenuation = (1 + light.attenuation * distToLight * distToLight);
    
    fragColor = vec4(material.color * (ambient + (diffuse + specular) / attenuation), 1);
}
