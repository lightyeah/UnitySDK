﻿using UnityEngine;
using System.Runtime.InteropServices;
using System;

// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable UnusedMember.Local
// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable RedundantAssignment

/*
 * Device interfaces
 */

namespace ThreeGlasses
{


    //3Glasses C# interfaces
    //3Glasses C#接口类
    public class ThreeGlassesInterfaces
    {
        public const int MAJOR = 6;
        public const int MINOR = 0;
        public const int PATCH = 0;
        public const string MAY = "beta";

        public static string getVersion
        {
            get
            {
                return MAJOR + "." + MINOR + "." + PATCH + MAY;
            }
        }

        public enum LeftOrRight {
            Left = 0,
            Right = 1
        };

        private const string vrLib = "SZVRPlugin";
        private const int UGridNumber = 64;
        private const int VGridNumber = 64;

        static Vector3 LastLeftVector3;
        static Vector3 LastRightVector3;


        //获取数据
        //Get data from SDK
        [DllImport(vrLib)]
        private static extern bool SZVR_GetData(float[] input, float[] output);
        //支付接口
        //Pay app
        [DllImport(vrLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool SZVR_PayApp(string appKey);
        //手柄接口
        [DllImport("SZVRWandPlugin", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool SZVR_GetWandData(
            float[] quaternion,
            float[] position,
            uint[] keyStatus,
            byte[] TriggerValue,
            byte[] Stick ,
            bool right);

        //窗口样式设置
        //Set window style
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLong(IntPtr hwnd, int _nIndex, int dwNewLong);

        //设置窗口位置和大小
        //Set windows position and resolution
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        //获取当前窗口
        //Get current app window
        [DllImport("user32.dll")]
        static extern IntPtr GetActiveWindow();

        //通过窗口句柄，获得窗口大小和位置
        //Get window Position and resolution
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

        private const uint SWP_SHOWWINDOW = 0x0040;
        private const int GWL_STYLE = -16;
        private const int WS_BORDER = 1;

        //内部数据结构：
        //Internal data structure
        //------------------------------------
        #region InternalDataStructure
        //窗口矩形
        //Window Rect
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left; //最左坐标
            public int Top; //最上坐标
            public int Right; //最右坐标
            public int Bottom; //最下坐标
        }
        #endregion
        //------------------------------------

        private static bool checkFloat(float v)
        {
            var status = !float.IsNaN(v);

            if (v > 500.0f || v < -500.0f)
            {
                status = false;
            }
            return status;
        }

        //头盔控制
        //Headset control
        //-------------------------------------
        #region Headset
        //获取头盔的显示屏在window下的位置和分辨率
        //Get the position and resolution of the headset under MS windows OS
        internal static Vector4 GetScreenPosRes()
        {
            var inputs = new float[1];
            inputs[0] = 0;
            var result = new float[4];
            var res = new Vector4();
            if (!SZVR_GetData(inputs, result)) return res;
            res.x = result[0];
            res.y = result[1];
            res.z = result[2];
            res.w = result[3];
            return res;
        }

        #endregion
        //-------------------------------------

        //Unity 公开接口
        //Unity Interfaces
        //-------------------------------------

        #region UnityInterfaces
        //获取头盔旋转信息
        //Get Headset rotation Quaternion
        public static Quaternion GetCameraOrientation()
        {
            var rotation = Quaternion.identity;
            var inputs = new[] {1.0f, 16.7f};

            var result = new float[4];
            if (!SZVR_GetData(inputs, result)) return rotation;
            rotation.x = -result[0];
            rotation.y = -result[1];
            rotation.z = result[2];
            rotation.w = result[3];
            return rotation;
        }

        //获取头盔位置信息
        //Get Headset position
        public static Vector3 GetCameraPosition()
        {
            var position = Vector3.zero;

            var inputs = new float[1];
            inputs[0] = 6;
            var result = new float[3];

            if (!SZVR_GetData(inputs, result)) return position;

            position.x = checkFloat(result[0]) ? result[0] : 0;
            position.y = checkFloat(result[1]) ? result[1] : 0;
            position.z = checkFloat(result[2]) ? result[2] : 0;

            return position;
        }

        ///// <summary>
        ///// 获取头盔FOV. Get headset fov
        ///// </summary>
        ///// <returns></returns>
        //public static float GetFov()
        //{
        //    float[] inputs = new float[1];
        //    inputs[0] = 4;
        //    float[] result = new float[1];
        //    if (SZVR_GetData(inputs, result))
        //    {
        //        return result[0];
        //    }
        //    else
        //    {
        //        return 0;
        //    }
        //}

        /// <summary>
        /// 获取手柄位置和旋转.
        /// Get Wand position and orientaion.
        /// </summary>
        /// <param name="LR"> 左右手 </param>
        /// <param name="position">返回的坐标</param>
        /// <param name="rotation">旋转的四元数</param>
        /// <param name="buttonEvent"></param>
        public static void GetWandPosAndRot(LeftOrRight LR, ref Vector3 position, ref Quaternion rotation,out ThreeGlassesWandButtonEvent.ButtonEvent buttonEvent )
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            buttonEvent = ThreeGlassesWandButtonEvent.ButtonEvent.NoneEvent;

            var quaternion_array = new float[4];
            var position_array = new float[3]; // 0.x 1.y 2.z
            var key_status = new uint[1];
            var trigger_value = new byte[] { 255 };// min:0, max:255
            var stick = new byte[2]; // 0.x 1.y

            if (SZVR_GetWandData(
                quaternion_array,
                position_array,
                key_status,
                trigger_value,
                stick,
                LR == LeftOrRight.Right))
            {
                buttonEvent = new ThreeGlassesWandButtonEvent.ButtonEvent(key_status[0], trigger_value[0], stick, LR);
            }

            if (LR == LeftOrRight.Left)
            {
                position.x = checkFloat(position_array[0]) ? -position_array[0] : LastLeftVector3.x;
                position.y = checkFloat(position_array[1]) ? position_array[1] : LastLeftVector3.y;
                position.z = checkFloat(position_array[2]) ? position_array[2] : LastLeftVector3.z;
                LastLeftVector3 = position;
            }
            else
            {
                position.x = checkFloat(position_array[0]) ? -position_array[0] : LastRightVector3.x;
                position.y = checkFloat(position_array[1]) ? position_array[1] : LastRightVector3.y;
                position.z = checkFloat(position_array[2]) ? position_array[2] : LastRightVector3.z;
                LastRightVector3 = position;
            }

            rotation.x = checkFloat(quaternion_array[2]) ? quaternion_array[2] : 0;
            rotation.y = -(checkFloat(quaternion_array[0]) ? quaternion_array[0] : 0);
            rotation.z = checkFloat(quaternion_array[1]) ? quaternion_array[1] : 0;
            rotation.w = -(checkFloat(quaternion_array[3]) ? quaternion_array[3] : 0);
        }


        /// <summary>
        ///设置当前窗口并移动到头盔中. Set current application window to headset display
        /// </summary>
        public static void SetPositionAndResolution()
        {
            var posRes = GetScreenPosRes();
            Screen.SetResolution((int)posRes[2], (int)posRes[3], false);
            SetWindowLong(GetActiveWindow(), GWL_STYLE, WS_BORDER);
            SetWindowPos(GetActiveWindow(), 0, (int)posRes[0], (int)posRes[1], (int)posRes[2], (int)posRes[3], SWP_SHOWWINDOW);
        }


        /// <summary>
        /// 支付接口. Pay app interfaces.
        /// </summary>
        /// <param name="appKey">app id</param>
        /// <returns></returns>
        public static bool PayApp(string appKey)
        {
            var status = false;
            try
            {
                status = SZVR_PayApp(appKey);
            }
            catch(Exception)
            {
                status = false;
            }
            
            return status;
        }

        //动感座椅控制
        //Chair control
        //------------------------------------
        #region ChairControl

        /// <summary>
        /// 初始化座椅. Init chair from chair.xml file.
        /// </summary>
        public static void IntChair()
        {
            var inputs = new float[2];
            inputs[0] = 5;
            inputs[1] = 0;
            var result = new float[1];
            SZVR_GetData(inputs, result);
        }

        /// <summary>
        /// 设置座椅归零. Set chair to zero position
        /// </summary>
        public static void ChairToZero()
        {
            var inputs = new float[2];
            inputs[0] = 5;
            inputs[1] = 1;
            var result = new float[1];
            SZVR_GetData(inputs, result);
        }

        /// <summary>
        /// 设置座椅上升到终点位置. Set chair to middle height
        /// </summary>
        public static void ChairToMiddle()
        {
            var inputs = new float[2];
            inputs[0] = 5;
            inputs[1] = 2;
            var result = new float[1];
            SZVR_GetData(inputs, result);
        }

        /// <summary>
        /// 设置座椅垂直法向量. Set chair vertical direction
        /// </summary>
        /// <param name="center">中点位置 Center point</param>
        /// <param name="directionY">Y方向向量 Y vector</param>
        /// <param name="time">时间. time</param>
        public static void ChairCenterMove(Vector3 center, Vector3 directionY, int time = 10)
        {
            var inputs = new float[9];
            inputs[0] = 5;
            inputs[1] = 3;
            inputs[2] = center.x;
            inputs[3] = center.y;
            inputs[4] = center.z;
            inputs[5] = directionY.x;
            inputs[6] = directionY.y;
            inputs[7] = directionY.z;
            inputs[8] = time;
            var result = new float[1];
            SZVR_GetData(inputs, result);
        }
        #endregion
        //------------------------------------

        #endregion
        //-------------------------------------
    }
}
