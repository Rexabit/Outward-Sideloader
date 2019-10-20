using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using SinAPI;

namespace SideLoader
{
    public class SLGUI : MonoBehaviour
    {
        public SideLoader script;

        public bool ShowGUI = false;

        public Rect m_windowRect = Rect.zero;
        public Vector2 scroll = Vector2.zero;

        private Vector2 m_virtualSize = new Vector2(1920, 1080);
        private Vector2 m_currentSize = Vector2.zero;
        public Matrix4x4 m_scaledMatrix;

        internal void Update()
        {
            if (script.InitDone <= 0)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                ShowGUI = !ShowGUI;
            }

            if (m_currentSize.x != Screen.width || m_currentSize.y != Screen.height)
            {
                m_scaledMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / m_virtualSize.x, Screen.height / m_virtualSize.y, 1));
                m_currentSize = new Vector2(Screen.width, Screen.height);
            }
        }

        internal void OnGUI()
        {
            if (!ShowGUI)
            {
                return;
            }

            Matrix4x4 orig = GUI.matrix;
            GUI.matrix = m_scaledMatrix;

            if (m_windowRect == Rect.zero || m_windowRect == null)
            {
                m_windowRect = new Rect(200, 5, 530, 470);
            }
            else
            {
                m_windowRect = GUI.Window(1273732, m_windowRect, DrawWindow, "SideLoader " + script._base.version.ToString("0.00"));
            }

            GUI.matrix = orig;
        }

        private void DrawWindow(int id)
        {
            
        }
    }
}
