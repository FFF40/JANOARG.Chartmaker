using System;
using System.Collections;
using System.Collections.Generic;
using JANOARG.Shared.Utils;
using UnityEngine;

namespace JANOARG.Chartmaker.Behaviors.Chartmaker.PlayerViewProps
{
    public class PlayerViewCamera : MonoBehaviour
    {

        public bool DrawCamera { get; internal set; } = true;

        public bool DrawGrid { get; internal set; } = true;

        Material lineMaterial;

        const float BASE_GRID_RANGE = 10;

        float lastCamY = 0;
        float gridRange = 0;
        float gridSpacing = 0;
        float gridMinorAlpha = 0;

        public void Start()
        {
            lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
            lineMaterial.SetInt("_ZTest", 0);
        }

        public void OnPostRender()
        {
            var playerView = PlayerView.main;
            var camera = playerView.MainCamera;

            if (!lineMaterial || playerView.Manager == null) return;

            var palette = playerView.Manager.PalleteManager.CurrentPallete;
            var baseColor = Color.Lerp(Color.white - palette.BackgroundColor, palette.InterfaceColor, 0.5f);

            GL.PushMatrix();
            lineMaterial.SetPass(0);
            GL.LoadProjectionMatrix(camera.projectionMatrix);
            
            if (playerView.CurrentWorldViewMode == WorldViewMode.Freecam)
            {
                if (DrawGrid)
                {
                    Vector3 camPos = camera.transform.position;
                    Color colFar = baseColor * new ColorFrag(a: 0);

                    if (lastCamY != camPos.y) UpdateGridParams();
                    if (gridRange == 0) return;

                    GL.Begin(GL.LINES);

                    // X lines
                    for (float x = Mathf.Ceil((camPos.x - gridRange) / gridSpacing) * gridSpacing; x < camPos.x + gridRange; x += gridSpacing)
                    {
                        float dist = Mathf.Abs(x - camPos.x);
                        float range = gridRange - dist;
                        Color colNear = baseColor * new ColorFrag(a: range / gridRange * GetGridAlpha(x));
                        GL.Color(colNear);
                        GL.Vertex(new Vector3(x, 0, camPos.z));
                        GL.Color(colFar);
                        GL.Vertex(new Vector3(x, 0, camPos.z + gridRange));
                        GL.Color(colNear);
                        GL.Vertex(new Vector3(x, 0, camPos.z));
                        GL.Color(colFar);
                        GL.Vertex(new Vector3(x, 0, camPos.z - gridRange));
                    }

                    // Z lines
                    for (float z = Mathf.Ceil((camPos.z - gridRange) / gridSpacing) * gridSpacing; z < camPos.z + gridRange; z += gridSpacing)
                    {
                        float dist = Mathf.Abs(z - camPos.z);
                        float range = gridRange - dist;
                        Color colNear = baseColor * new ColorFrag(a: range / gridRange * GetGridAlpha(z));
                        GL.Color(colNear);
                        GL.Vertex(new Vector3(camPos.x, 0, z));
                        GL.Color(colFar);
                        GL.Vertex(new Vector3(camPos.x + gridRange, 0, z));
                        GL.Color(colNear);
                        GL.Vertex(new Vector3(camPos.x, 0, z));
                        GL.Color(colFar);
                        GL.Vertex(new Vector3(camPos.x - gridRange, 0, z));
                    }
                }
                if (DrawCamera)
                {
                    var camRot = Quaternion.Euler(playerView.Manager.Camera.CameraRotation);
                    var camPos = playerView.Manager.Camera.CameraPivot + camRot * new Vector3(0, 0, -playerView.Manager.Camera.PivotDistance);
                    var camNear = camera.nearClipPlane;
                    var camFar = PlayerView.BASE_CAMERA_RANGE;

                    const float TAN_Y_MULT = 0.5773502483918941f; // Mathf.Tan(PlayerView.BASE_CAMERA_FOV / 2 * Mathf.Deg2Rad)
                    const float TAN_X_MULT = TAN_Y_MULT * PlayerView.GAME_FIELD_RATIO;

                    GL.Begin(GL.LINES);
                    GL.Color(baseColor * new ColorFrag(a: 0.2f));

                    // Camera projection
                    GL.Vertex(camPos + camRot * new Vector3( TAN_X_MULT,  TAN_Y_MULT, 1) * camNear);
                    GL.Vertex(camPos + camRot * new Vector3( TAN_X_MULT,  TAN_Y_MULT, 1) * camFar);
                    GL.Vertex(camPos + camRot * new Vector3( TAN_X_MULT, -TAN_Y_MULT, 1) * camNear);
                    GL.Vertex(camPos + camRot * new Vector3( TAN_X_MULT, -TAN_Y_MULT, 1) * camFar);
                    GL.Vertex(camPos + camRot * new Vector3(-TAN_X_MULT,  TAN_Y_MULT, 1) * camNear);
                    GL.Vertex(camPos + camRot * new Vector3(-TAN_X_MULT,  TAN_Y_MULT, 1) * camFar);
                    GL.Vertex(camPos + camRot * new Vector3(-TAN_X_MULT, -TAN_Y_MULT, 1) * camNear);
                    GL.Vertex(camPos + camRot * new Vector3(-TAN_X_MULT, -TAN_Y_MULT, 1) * camFar);

                    // Camera near plane
                    GL.Vertex(camPos + camRot * new Vector3( TAN_X_MULT,  TAN_Y_MULT, 1) * camNear);
                    GL.Vertex(camPos + camRot * new Vector3( TAN_X_MULT, -TAN_Y_MULT, 1) * camNear);
                    GL.Vertex(camPos + camRot * new Vector3( TAN_X_MULT, -TAN_Y_MULT, 1) * camNear);
                    GL.Vertex(camPos + camRot * new Vector3(-TAN_X_MULT, -TAN_Y_MULT, 1) * camNear);
                    GL.Vertex(camPos + camRot * new Vector3(-TAN_X_MULT, -TAN_Y_MULT, 1) * camNear);
                    GL.Vertex(camPos + camRot * new Vector3(-TAN_X_MULT,  TAN_Y_MULT, 1) * camNear);
                    GL.Vertex(camPos + camRot * new Vector3(-TAN_X_MULT,  TAN_Y_MULT, 1) * camNear);
                    GL.Vertex(camPos + camRot * new Vector3( TAN_X_MULT,  TAN_Y_MULT, 1) * camNear);

                    // Camera far plane
                    GL.Vertex(camPos + camRot * new Vector3( TAN_X_MULT,  TAN_Y_MULT, 1) * camFar);
                    GL.Vertex(camPos + camRot * new Vector3( TAN_X_MULT, -TAN_Y_MULT, 1) * camFar);
                    GL.Vertex(camPos + camRot * new Vector3( TAN_X_MULT, -TAN_Y_MULT, 1) * camFar);
                    GL.Vertex(camPos + camRot * new Vector3(-TAN_X_MULT, -TAN_Y_MULT, 1) * camFar);
                    GL.Vertex(camPos + camRot * new Vector3(-TAN_X_MULT, -TAN_Y_MULT, 1) * camFar);
                    GL.Vertex(camPos + camRot * new Vector3(-TAN_X_MULT,  TAN_Y_MULT, 1) * camFar);
                    GL.Vertex(camPos + camRot * new Vector3(-TAN_X_MULT,  TAN_Y_MULT, 1) * camFar);
                    GL.Vertex(camPos + camRot * new Vector3( TAN_X_MULT,  TAN_Y_MULT, 1) * camFar);
                }
            }
            
            GL.End();

            GL.PopMatrix();
        }

        void UpdateGridParams()
        {
            lastCamY = transform.position.y;
            gridRange = BASE_GRID_RANGE * lastCamY;
            float log10 = Mathf.Max(0, Mathf.Log10(lastCamY) - 0.25f);
            gridSpacing = Mathf.Pow(10, Mathf.Floor(log10));
            gridMinorAlpha = Mathf.Min((Mathf.Pow(0.1f, (log10 % 1 + 1) % 1) - 0.1f) / 0.9f, 1);
        }

        float GetGridAlpha(float pos)
        {
            return 0.2f * (pos % (gridSpacing * 10) == 0 ? 1 : gridMinorAlpha);
        }
    }
}
