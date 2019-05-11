using System;
using System.Collections.Generic;
using UnityEngine;

using Random = UnityEngine.Random;

public class RayMarchingMaster : MonoBehaviour {

    // --- PARAMETERS ---

    public ComputeShader rayMarchingShader;

    public Color groundColor;
    public Color skyColor;

    [Range(1, 1024)]
    public int numberOfMarchingIterations = 256;

    // --- PRIVATE ---

    private RenderTexture targetTexture;

    private new Camera camera;

    // --- EVENTS ---

    private void Awake() {
        camera = GetComponent<Camera>();
    }

    private void OnEnable() {
    }

    private void OnDisable() {
    }

    // called by the camera to do post-processing
    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        Render(destination);
    }

    // --- SETUP ---

    private void SetupScene() {
    }

    // --- RENDERING ---

    private void Render(RenderTexture destination) {
        InitializeRenderTexture();
        SetShaderParameters();

        const float threadsInGroup = 16f;

        int threadGroupsX = Mathf.CeilToInt(Screen.width / threadsInGroup);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / threadsInGroup);
        rayMarchingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

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
        rayMarchingShader.SetTexture(0, "result", targetTexture);
        rayMarchingShader.SetFloat("currentTime", Time.time);
        rayMarchingShader.SetVector("groundColor", groundColor);
        rayMarchingShader.SetVector("skyColor", skyColor);

        rayMarchingShader.SetMatrix("cameraToWorld", camera.cameraToWorldMatrix);
        rayMarchingShader.SetMatrix("cameraInverseProjection", camera.projectionMatrix.inverse);

        rayMarchingShader.SetInt("numberOfMarchingIterations", numberOfMarchingIterations);
    }

    // --- GENERATION ---

    private Color MakePastelColor() {
        return Random.ColorHSV(0, 1, .57f, .6f, .75f, .91f);
    }
}