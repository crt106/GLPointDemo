using System;
using System.Collections.Generic;
using System.Windows.Forms;
using GlmNet;
using SharpGL;
using SharpGL.Shaders;
using SharpGL.VertexBuffers;

namespace GLPointDemo
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// 假设你有个链表来存储你的某种数据结构的点位 二维数组也行 后续放进GL里都会转换成一维数组
        /// </summary>
        public static List<Point> SumPoints = new List<Point>();

        public struct Point
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }

            public Point(float x, float y, float z)
            {
                X = x;
                Y = y;
                Z = z;
            }
        }

        /// <summary>
        /// 把点位归一化处理进行显示 我是这样想的 因为在不做投影情况下 openGL中XYZ轴范围是 [-1,1]
        /// </summary>
        public float[] pointData
        {
            get
            {
                float[] outA = new float[SumPoints.Count * 3];
                float maxX = 0f, maxY = 0f, maxZ = 0f;
                foreach (var p in SumPoints)
                {
                    var aX = Math.Abs(p.X);
                    var aY = Math.Abs(p.Y);
                    var aZ = Math.Abs(p.Z);
                    maxX = aX > maxX ? aX : maxX;
                    maxY = aY > maxY ? aY : maxY;
                    maxZ = aZ > maxZ ? aZ : maxZ;
                }

                for (int i = 0; i < SumPoints.Count; i++)
                {
                    var p = SumPoints[i];
                    outA[3 * i] = p.X / maxX;
                    outA[3 * i + 1] = p.Y / maxY;
                    outA[3 * i + 2] = p.Z / maxZ;
                }

                return outA;
            }
        }

        #region 全局控制对象

        private static OpenGL gl = null;
        private ShaderProgram mainGlProgram = null;
        private ShaderProgram axisGlProgram = null;
        private ArcBallManipulater arcBallManipulater = new ArcBallManipulater();

        /// <summary>
        /// 视角矩阵 所有的旋转平移操作实际上都是改变它
        /// 在<see cref="arcBallManipulater"/> 类中计算好的变换矩阵通过委托回调修改其值
        /// </summary>
        public static mat4 viewMatrix;

        #endregion

        #region 静态量和shader

        private const uint index_in_vPosition = 0;
        private const uint index_in_vColor = 1;

        private const string vertexShaderSource =
            "in vec3 vPosition;\nin vec3 vColor;  \nout vec3 pass_Color;\n\nuniform mat4 viewMatrix;\n\nvoid main(){\n  gl_Position = viewMatrix*vec4(vPosition, 1.0);\n  pass_Color = vColor;\n}";

        private const string simpleFragmentShaderSource =
            "precision mediump float;\nout vec4 out_Color;\nvoid main(){\n  out_Color = vec4(1.0,1.0,0.0,1.0);\n}";

        private const string colorFragmentShaderSource =
            "precision mediump float;\nin vec3 pass_Color;\nout vec4 out_Color;\nvoid main(void) {\nout_Color = vec4(pass_Color, 1.0);\n}";

        #endregion

        public Form1()
        {
            InitializeComponent();
            SumPoints.Add(new Point(125f, 103f, 75f));
            SumPoints.Add(new Point(115f, 126f, 75f));
            SumPoints.Add(new Point(145f, 156f, 75f));
            SumPoints.Add(new Point(-156f, 166f, 75f));
            SumPoints.Add(new Point(113f, 106f, 75f));
            SumPoints.Add(new Point(110f, 66f, 75f));
            SumPoints.Add(new Point(-189f, 72f, 75f));
            SumPoints.Add(new Point(112f, 43f, 13f));
            SumPoints.Add(new Point(109f, -98f, 32f));
            SumPoints.Add(new Point(134f, -167f, -75f));
            SumPoints.Add(new Point(155f, 24f, -311f));
            SumPoints.Add(new Point(-90f, -123f, 123f));
            SumPoints.Add(new Point(30f, -167f, 56f));
            SumPoints.Add(new Point(-122f, 43f, 123f));
            SumPoints.Add(new Point(48f, 167f, 774f));
        }

        private void openGLControl1_OpenGLInitialized(object sender, EventArgs e)
        {
            gl = openGLControl1.OpenGL;
            gl.ClearColor(0.4f, 0.6f, 0.9f, 0.0f);
            gl.LineWidth(1.5f);
            gl.PointSize(6.0f);

            //

            viewMatrix = new mat4(1.0f);
            /**
             * 使用sharpGL封装过的方式创建openGL Program   不用执行编译shader到链接一系列操作
             * 两program 他们实际上是共用变换矩阵 emmm 可能会有一些小问题
             */

            mainGlProgram = new ShaderProgram();
            mainGlProgram.Create(gl, vertexShaderSource, simpleFragmentShaderSource, null);
            mainGlProgram.BindAttributeLocation(gl, index_in_vPosition, "vPosition");
            mainGlProgram.AssertValid(gl);

            axisGlProgram = new ShaderProgram();
            axisGlProgram.Create(gl, vertexShaderSource, colorFragmentShaderSource, null);
            axisGlProgram.BindAttributeLocation(gl, index_in_vPosition, "vPosition");
            axisGlProgram.BindAttributeLocation(gl, index_in_vColor, "vColor");
            axisGlProgram.AssertValid(gl);


            arcBallManipulater.Bind(openGLControl1, viewMatrix, new Action<mat4>((mat4 => { viewMatrix = mat4; })));
        }

        private void openGLControl1_OpenGLDraw(object sender, SharpGL.RenderEventArgs args)
        {
            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT | OpenGL.GL_STENCIL_BUFFER_BIT);
            drawCoorAxis();
            drawPoints();
        }


        private void drawPoints()
        {
            if (mainGlProgram != null)
            {
                mainGlProgram.Bind(gl);
                mainGlProgram.SetUniformMatrix4(gl, "viewMatrix", viewMatrix.to_array());


                // 同样数组对象不用调用glVertexAttribPointer,可以用它封装好的方式
                var vertexBufferArray = new VertexBufferArray();
                vertexBufferArray.Create(gl);
                vertexBufferArray.Bind(gl);


                var vertexDataBuffer = new VertexBuffer();
                vertexDataBuffer.Create(gl);
                vertexDataBuffer.Bind(gl);
                vertexDataBuffer.SetData(gl, index_in_vPosition, pointData, false, 3);

                //  Draw the square.
                gl.DrawArrays(OpenGL.GL_POINTS, 0, SumPoints.Count);

                //  Unbind our vertex array and shader.
                vertexDataBuffer.Unbind(gl);
                vertexBufferArray.Unbind(gl);
                mainGlProgram.Unbind(gl);
            }
        }


        float[] axisData =
        {
            0.9f, 0.0f, 0.0f,
            -0.9f, 0.0f, 0.0f,
            0.0f, 0.9f, 0.0f,
            0.0f, -0.9f, 0.0f,
            0.0f, 0.0f, 0.9f,
            0.0f, 0.0f, -0.9f
        };

        float[] axisColor =
        {
            0.2f, 1.0f, 0.10f,
            0.3f, 1.0f, 0.20f,
            1.0f, 0.0f, 0.30f,
            1.0f, 0.0f, 0.0f,
            0.40f, 0.20f, 1.0f,
            0.60f, 0.10f, 1.0f,
        };

        /// <summary>
        /// 把坐标轴画上 绿X 红Y 蓝Z
        /// </summary>
        private void drawCoorAxis()
        {
            if (axisGlProgram != null)
            {
                axisGlProgram.Bind(gl);
                axisGlProgram.SetUniformMatrix4(gl, "viewMatrix", viewMatrix.to_array());


                var vertexBufferArray = new VertexBufferArray();
                vertexBufferArray.Create(gl);
                vertexBufferArray.Bind(gl);

                var vertexDataBuffer = new VertexBuffer();
                vertexDataBuffer.Create(gl);
                vertexDataBuffer.Bind(gl);
                vertexDataBuffer.SetData(gl, index_in_vPosition, axisData, false, 3);

                var colorDataBuffer = new VertexBuffer();
                colorDataBuffer.Create(gl);
                colorDataBuffer.Bind(gl);
                colorDataBuffer.SetData(gl, index_in_vColor, axisColor, false, 3);

                //  Draw the square.
                gl.DrawArrays(OpenGL.GL_LINES, 0, 6);

                //  Unbind our vertex array and shader.
                vertexDataBuffer.Unbind(gl);
                colorDataBuffer.Unbind(gl);
                vertexBufferArray.Unbind(gl);
                axisGlProgram.Unbind(gl);
            }
        }
    }
}