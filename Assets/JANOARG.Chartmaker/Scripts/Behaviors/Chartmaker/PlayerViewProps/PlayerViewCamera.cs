using System.Collections;
using System.Collections.Generic;
using JANOARG.Shared.Utils;
using UnityEngine;

namespace JANOARG.Chartmaker.Behaviors.Chartmaker.PlayerViewProps
{
    public class PlayerViewCamera : MonoBehaviour
    {

        Material lineMaterial;

        public void Start()
        {
            lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
        }

        public void OnPostRender()
        {
            var playerView = PlayerView.main;
            var camera = playerView.MainCamera;

            if (!lineMaterial || playerView.Manager == null) return;

            var camRot = Quaternion.Euler(playerView.Manager.Camera.CameraRotation);
            var camPos = playerView.Manager.Camera.CameraPivot + camRot * new Vector3(0, 0, -playerView.Manager.Camera.PivotDistance);
            var camNear = camera.nearClipPlane;
            var camFar = Mathf.Lerp(camera.nearClipPlane, camera.farClipPlane, .5f);

            const float TAN_Y_MULT = 0.5773502483918941f;
            const float TAN_X_MULT = TAN_Y_MULT * PlayerView.GAME_FIELD_RATIO;

            GL.PushMatrix();
            lineMaterial.SetPass(0);
            GL.LoadProjectionMatrix(camera.projectionMatrix);
            
            if (playerView.CurrentWorldViewMode == WorldViewMode.Freecam)
            {
                GL.Begin(GL.LINES);
                GL.Color((Color.white - playerView.Manager.PalleteManager.CurrentPallete.BackgroundColor) * new ColorFrag(a: 0.35f));

                GL.Vertex(camPos + camRot * new Vector3( TAN_X_MULT,  TAN_Y_MULT, 1) * camNear);
                GL.Vertex(camPos + camRot * new Vector3( TAN_X_MULT,  TAN_Y_MULT, 1) * camFar);
                GL.Vertex(camPos + camRot * new Vector3( TAN_X_MULT, -TAN_Y_MULT, 1) * camNear);
                GL.Vertex(camPos + camRot * new Vector3( TAN_X_MULT, -TAN_Y_MULT, 1) * camFar);
                GL.Vertex(camPos + camRot * new Vector3(-TAN_X_MULT,  TAN_Y_MULT, 1) * camNear);
                GL.Vertex(camPos + camRot * new Vector3(-TAN_X_MULT,  TAN_Y_MULT, 1) * camFar);
                GL.Vertex(camPos + camRot * new Vector3(-TAN_X_MULT, -TAN_Y_MULT, 1) * camNear);
                GL.Vertex(camPos + camRot * new Vector3(-TAN_X_MULT, -TAN_Y_MULT, 1) * camFar);

                GL.Vertex(camPos + camRot * new Vector3( TAN_X_MULT,  TAN_Y_MULT, 1) * camNear);
                GL.Vertex(camPos + camRot * new Vector3( TAN_X_MULT, -TAN_Y_MULT, 1) * camNear);
                GL.Vertex(camPos + camRot * new Vector3( TAN_X_MULT, -TAN_Y_MULT, 1) * camNear);
                GL.Vertex(camPos + camRot * new Vector3(-TAN_X_MULT, -TAN_Y_MULT, 1) * camNear);
                GL.Vertex(camPos + camRot * new Vector3(-TAN_X_MULT, -TAN_Y_MULT, 1) * camNear);
                GL.Vertex(camPos + camRot * new Vector3(-TAN_X_MULT,  TAN_Y_MULT, 1) * camNear);
                GL.Vertex(camPos + camRot * new Vector3(-TAN_X_MULT,  TAN_Y_MULT, 1) * camNear);
                GL.Vertex(camPos + camRot * new Vector3( TAN_X_MULT,  TAN_Y_MULT, 1) * camNear);
                GL.Vertex(camPos + camRot * new Vector3( TAN_X_MULT,  TAN_Y_MULT, 1) * camFar);
                GL.Vertex(camPos + camRot * new Vector3( TAN_X_MULT, -TAN_Y_MULT, 1) * camFar);
                GL.Vertex(camPos + camRot * new Vector3( TAN_X_MULT, -TAN_Y_MULT, 1) * camFar);
                GL.Vertex(camPos + camRot * new Vector3(-TAN_X_MULT, -TAN_Y_MULT, 1) * camFar);
                GL.Vertex(camPos + camRot * new Vector3(-TAN_X_MULT, -TAN_Y_MULT, 1) * camFar);
                GL.Vertex(camPos + camRot * new Vector3(-TAN_X_MULT,  TAN_Y_MULT, 1) * camFar);
                GL.Vertex(camPos + camRot * new Vector3(-TAN_X_MULT,  TAN_Y_MULT, 1) * camFar);
                GL.Vertex(camPos + camRot * new Vector3( TAN_X_MULT,  TAN_Y_MULT, 1) * camFar);
            }
            
            GL.End();

            GL.PopMatrix();
        }
    }
}
