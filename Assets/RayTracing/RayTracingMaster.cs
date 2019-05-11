using System;
using System.Collections.Generic;
using UnityEngine;

using Random = UnityEngine.Random;

public class RayTracingMaster : MonoBehaviour {
    // --- TYPES ---
    [Serializable]
    private struct Sphere {
        public Vector3 position;
        public float radius;
        public float floatingOffset;
        public float floatingMagnitude;

        public Vector3 albedo;
    }

    // --- PARAMETERS ---

    public ComputeShader rayTracingShader;

    [Range(0, 200)]
    public uint numberOfSpheres = 100;
    [Range(1f, 100f)]
    public float placementRadius = 10f;
    public Vector2 sphereRadiusRange = new Vector2(0f, 5f);

    public Vector2 floatingMagnitudeRange = new Vector2(1f, 3f);

    // --- PRIVATE ---

    private RenderTexture targetTexture;
    private ComputeBuffer worldSpheres;
    private int worldSpheresCount;
    private int worldSpheresStride;

    private new Camera camera;

    // --- EVENTS ---

    private void Awake() {
        camera = GetComponent<Camera>();
    }

    private void OnEnable() {
        SetupScene();
    }

    private void OnDisable() {
        worldSpheres.Release();
    }

    // called by the camera to do post-processing
    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        Render(destination);
    }

    // --- SETUP ---

    private void SetupScene() {
        var generatedSpheres = new List<Sphere>();

        // generate spheres
        for (int i = 0; i < numberOfSpheres; i++) {
            var sphere = new Sphere();

            // generate position and radius
            sphere.radius = sphereRadiusRange.x + Random.value * (sphereRadiusRange.y - sphereRadiusRange.x);

            var randomPosition = Random.insideUnitCircle * placementRadius;
            sphere.position = new Vector3(randomPosition.x, sphere.radius, randomPosition.y);

            if (Random.value < .66f) {
                sphere.floatingOffset = Random.value * 2;
                sphere.floatingMagnitude = floatingMagnitudeRange.x + Random.value * (floatingMagnitudeRange.y - floatingMagnitudeRange.x);

                // scale by sphere radius
                sphere.floatingMagnitude = sphereRadiusRange.y / sphere.radius * sphere.floatingMagnitude;
            } else {
                sphere.floatingOffset = 0f;
                sphere.floatingMagnitude = 0f;
            }

            // throw it away if it intersects with any other
            var intersecting = false;
            foreach (var other in generatedSpheres) {
                float minDist = sphere.radius + other.radius;

                if (Vector3.SqrMagnitude(sphere.position - other.position) < minDist * minDist) {
                    intersecting = true;
                }
            }

            if (intersecting) {
                continue;
            }

            // generate color
            var albedo = Random.ColorHSV(0, 1, .7f, .8f, .4f, .9f);
            sphere.albedo = new Vector3(albedo.r, albedo.g, albedo.b);

            generatedSpheres.Add(sphere);
        }

        worldSpheresCount = generatedSpheres.Count;
        worldSpheresStride = sizeof(float) * 9;

        worldSpheres = new ComputeBuffer(worldSpheresCount, worldSpheresStride);
        worldSpheres.SetData(generatedSpheres);
    }

    // --- RENDERING ---

    private void Render(RenderTexture destination) {
        InitializeRenderTexture();
        SetShaderParameters();

        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        rayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        // render (blit) the texture to the destination texture (screen)
        Graphics.Blit(targetTexture, destination);
    }

    private void InitializeRenderTexture() {
        if (!targetTexture || targetTexture.width != Screen.width || targetTexture.height != Screen.height) {
            if (targetTexture)
                targetTexture.Release();

            // get a render target for ray tracing
            targetTexture = new RenderTexture(
                Screen.width,
                Screen.height,
                0,
                RenderTextureFormat.ARGBFloat,
                RenderTextureReadWrite.Linear
            );

            targetTexture.enableRandomWrite = true;
            targetTexture.Create();
        }
    }
    private void SetShaderParameters() {
        rayTracingShader.SetTexture(0, "result", targetTexture);
        rayTracingShader.SetFloat("currentTime", Time.time);

        rayTracingShader.SetBuffer(0, "worldSpheres", worldSpheres);
        rayTracingShader.SetInt("worldSpheresCount", worldSpheresCount);
        rayTracingShader.SetInt("worldSpheresStride", worldSpheresStride);

        rayTracingShader.SetMatrix("cameraToWorld", camera.cameraToWorldMatrix);
        rayTracingShader.SetMatrix("cameraInverseProjection", camera.projectionMatrix.inverse);
    }
}