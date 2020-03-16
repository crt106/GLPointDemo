using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GlmNet;

namespace GLPointDemo
{
    /// <summary>
    /// Rotate model using arc-ball method.
    /// 这个类是根据 https://www.cnblogs.com/bitzhuwei/p/arcball_4_all_camera.html 改的
    /// </summary>
    public class ArcBallManipulater
    {
        private MouseEventHandler mouseDownEvent;
        private MouseEventHandler mouseMoveEvent;
        private MouseEventHandler mouseUpEvent;
        private MouseEventHandler mouseWheelEvent;

        /// <summary>
        /// 最终所有的变换结果都反映到这个矩阵上
        /// </summary>
        private mat4 finalMat;

        private UserControl control;

        /// <summary>
        /// 矩阵变化值回调方法
        /// </summary>
        private Action<mat4> callBackAction;

        private vec3 _vectorRight = new vec3(1, 0, 0);
        private vec3 _vectorUp = new vec3(0, 1, 0);
        private vec3 _vectorBack = new vec3(0, 0, 1);
        private float _length, _radiusRadius;

        private vec3 _startPosition, _endPosition, _normalVector = new vec3(0, 1, 0);
        private int _width;
        private int _height;
        private bool mouseDownFlag;

        public float MouseSensitivity { get; set; }

        public MouseButtons BindingMouseButtons { get; set; }
        private MouseButtons lastBindingMouseButtons;

        /// <summary>
        /// Rotate model using arc-ball method.
        /// </summary>
        /// <param name="bindingMouseButtons"></param>
        public ArcBallManipulater(MouseButtons bindingMouseButtons = MouseButtons.Left)
        {
            this.MouseSensitivity = 0.1f;
            this.BindingMouseButtons = bindingMouseButtons;
            this.mouseDownEvent = new MouseEventHandler(control_MouseDown);
            this.mouseMoveEvent = new MouseEventHandler(control_MouseMove);
            this.mouseUpEvent = new MouseEventHandler(control_MouseUp);
            this.mouseWheelEvent = new MouseEventHandler(control_MouseWheel);
        }


        public void Bind(UserControl control, mat4 viewMat, Action<mat4> callback)
        {
            this.finalMat = viewMat;
            this.control = control;
            callBackAction = callback;
            this.control.MouseDown += this.mouseDownEvent;
            this.control.MouseMove += this.mouseMoveEvent;
            this.control.MouseUp += this.mouseUpEvent;
            this.control.MouseWheel += this.mouseWheelEvent;
        }

        public void Unbind()
        {
            this.control.MouseDown -= this.mouseDownEvent;
            this.control.MouseMove -= this.mouseMoveEvent;
            this.control.MouseUp -= this.mouseUpEvent;
            this.control.MouseWheel -= this.mouseWheelEvent;
        }

        void control_MouseWheel(object sender, MouseEventArgs e)
        {
            float scale = 1;
            if (e.Delta > 0)
            {
                scale = 1 + MouseSensitivity;
            }
            else
            {
                scale = 1 - MouseSensitivity;
            }

            finalMat = glm.scale(finalMat, new vec3(scale, scale, scale));
            callBackAction(finalMat);
        }

        void control_MouseDown(object sender, MouseEventArgs e)
        {
            this.lastBindingMouseButtons = this.BindingMouseButtons;
            if ((e.Button & this.lastBindingMouseButtons) != MouseButtons.None)
            {
                this.SetBounds(control.Width, control.Height);
                this._startPosition = GetArcBallPosition(e.X, e.Y);
                mouseDownFlag = true;
            }
        }

        private void SetBounds(int width, int height)
        {
            this._width = width;
            this._height = height;
            _length = width > height ? width : height;
            var rx = (width / 2) / _length;
            var ry = (height / 2) / _length;
            _radiusRadius = (float) (rx * rx + ry * ry);
        }

        void control_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseDownFlag && ((e.Button & this.lastBindingMouseButtons) != MouseButtons.None))
            {
                this._endPosition = GetArcBallPosition(e.X, e.Y);
                var cosAngle = _startPosition.dot(_endPosition) / (_startPosition.length() * _endPosition.length());
                if (cosAngle > 1.0f)
                {
                    cosAngle = 1.0f;
                }
                else if (cosAngle < -1)
                {
                    cosAngle = -1.0f;
                }

                var angle = MouseSensitivity * (float) (Math.Acos(cosAngle) / Math.PI * 180);
                _normalVector = _startPosition.cross(_endPosition).normalize();
                if (!
                    ((_normalVector.x == 0 && _normalVector.y == 0 && _normalVector.z == 0)
                     || float.IsNaN(_normalVector.x) || float.IsNaN(_normalVector.y) || float.IsNaN(_normalVector.z)))
                {
                    _startPosition = _endPosition;

                    mat4 newRotation = glm.rotate(angle, _normalVector);
                    this.finalMat = newRotation * finalMat;
                    callBackAction(this.finalMat);

                    //控制台瞄一眼
                    float[] debugA = this.finalMat.to_array();
                    StringBuilder sb = new StringBuilder();
                    sb.Append("finalMat[");
                    for (int i = 0; i < 16; i++)
                    {
                        sb.Append($"{debugA[i]} ,");
                        if ((i + 1) % 4 == 0)
                        {
                            sb.Append("\n");
                        }
                    }
                }
            }
        }

        private vec3 GetArcBallPosition(int x, int y)
        {
            float rx = (x - _width / 2) / _length;
            float ry = (_height / 2 - y) / _length;
            float zz = _radiusRadius - rx * rx - ry * ry;
            float rz = (zz > 0 ? (float) Math.Sqrt(zz) : 0.0f);
            var result = new vec3(
                rx * _vectorRight.x + ry * _vectorUp.x + rz * _vectorBack.x,
                rx * _vectorRight.y + ry * _vectorUp.y + rz * _vectorBack.y,
                rx * _vectorRight.z + ry * _vectorUp.z + rz * _vectorBack.z
            );
            //var position = new vec3(rx, ry, rz);
            //var matrix = new mat3(_vectorRight, _vectorUp, _vectorBack);
            //result = matrix * position;

            return result;
        }

        void control_MouseUp(object sender, MouseEventArgs e)
        {
            if ((e.Button & this.lastBindingMouseButtons) != MouseButtons.None)
            {
                mouseDownFlag = false;
            }
        }
    }

    /// <summary>
    /// 扩充一下这个博客里用到的数据结构没有的方法
    /// </summary>
    public static class Extention
    {
        public static float length(this vec3 vec)
        {
            return (float) Math.Sqrt(vec.x * vec.x + vec.y * vec.y + vec.z * vec.z);
        }

        public static float dot(this vec3 v1, vec3 v2)
        {
            return v1.x * v2.x + v1.y * v2.y + v1.z * v2.z;
        }

        public static vec3 cross(this vec3 v1, vec3 v2)
        {
            float nx = v1.y * v2.z - v2.y * v1.z;
            float ny = v2.x * v1.z - v1.x * v2.z;
            float nz = v1.x * v2.y - v2.x * v1.y;
            return new vec3(nx, ny, nz);
        }

        public static vec3 normalize(this vec3 vec)
        {
            float total = Math.Abs(vec.x) + Math.Abs(vec.y) + Math.Abs(vec.z);
            return new vec3(vec.x / total, vec.y / total, vec.z / total);
        }
    }
}