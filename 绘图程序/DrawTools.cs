using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;

namespace 绘图程序
{
    /// <summary>
    /// 绘图工具包括直线，矩形，铅笔，圆形，橡皮
    /// </summary>
    class DrawTools
    {
        public Graphics DrawTools_Graphics;
        public Pen p;
        public Image orginalImg;               //原始画布，用来保存已完成的绘图过程
        public Color drawColor = Color.Black;  //绘图颜色
        public Graphics newgraphics;           //画板
        public Image finishingImg;             //中间只有一个框的图片

        /// <summary>
        /// 绘图颜色
        /// </summary>
        public Color DrawColor
        {
            get { return drawColor; }
            set
            {
                drawColor = value;
                p.Color = value;
            }
        }

        /// <summary>
        /// 原始画布
        /// </summary>
        public Image OrginalImg
        {
            get { return orginalImg; }
            set
            {
                finishingImg = (Image)value.Clone();
                orginalImg = (Image)value.Clone();
            }
        }


        /// <summary>
        /// 表示是否开始绘图
        /// </summary>
        public bool startDraw = false;

        /// <summary>
        /// 绘图起点
        /// </summary>
        public PointF startPointF;

        /// 初始化绘图工具
        public DrawTools(Graphics g, Color c, Image img)
        {
            //DrawTools_Graphics = g;
            drawColor = c;
            p = new Pen(c, 2);
            finishingImg = (Image)img.Clone();
            orginalImg = (Image)img.Clone();
            DrawTools_Graphics = Graphics.FromImage(orginalImg);    //画布
        }

        //直接显示之前txt上的画框
        public void Draw_txt(List<string> cur_txt_content)
        {
            if (cur_txt_content.Count > 0)
            {
                for (int i = 0; i < cur_txt_content.Count; i++)
                {
                    //得到坐标
                    Point point1 = new Point(0, 0);
                    Point point2 = new Point(0, 0);
                    string[] ss = cur_txt_content[i].Split(' ');
                    if (ss.Count() == 4)
                    {
                        point1.X = Convert.ToInt32(ss[0]); point1.Y = Convert.ToInt32(ss[1]);
                        point2.X = Convert.ToInt32(ss[2]); point2.Y = Convert.ToInt32(ss[3]);
                    }
                    //textBox1.Text = point1.ToString();
                    float width = Math.Abs(point1.X - point2.X);//确定矩形的宽
                    float heigth = Math.Abs(point1.Y - point2.Y);//确定矩形的高
                    PointF rectStartPointF = point1;
                    if (point2.X < point1.X)
                    {
                        rectStartPointF.X = point2.X;
                    }
                    if (point2.Y < point1.Y)
                    {
                        rectStartPointF.Y = point2.Y;
                    }
                    DrawTools_Graphics.DrawRectangle(p, rectStartPointF.X, rectStartPointF.Y, width, heigth);
                }

            }
            finishingImg = (Image)orginalImg.Clone();

        }

        /// <summary>
        /// 绘制直线，矩形，圆形
        /// </summary>
        /// <param name="e">鼠标参数</param>
        /// <param name="sType">绘图类型</param>
        public void Draw(MouseEventArgs e, string sType, List<string> cur_txt_content)
        {
            if (startDraw)
            {
                //为防止造成图片抖动，防止记录不必要的绘图过程中的痕迹，我们先在中间画板上将图片完成，然后在将绘制好的图片一次性画到目标画板上
                //步骤1实例化中间画板，画布为上一次绘制结束时的画布的副本（如果第一次绘制，那画布就是初始时的画布副本）
                //步骤2按照绘图样式在中间画板上进行绘制
                //步骤3将绘制结束的图片画到中间画布上
                //因为我们最终绘制结束时的图片应该是在鼠标松开时完成，所以鼠标移动中所绘制的图片都只画到中间画布上,但仍需要显示在目标画板上，否则鼠标移动过程中我们就看不到效果。
                //当鼠标松开时，才把最后的那个中间图片画到原始画布上

                Image img = (Image)orginalImg.Clone();//原始img的拷贝
                newgraphics = Graphics.FromImage(img);//以img为画板

                switch (sType)
                {
                    case "Line":
                        {//画直线
                            newgraphics.DrawLine(p, startPointF, new PointF(e.X, e.Y)); break;
                        }
                    case "Rect":
                        {//画矩形
                            float width = Math.Abs(e.X - startPointF.X);//确定矩形的宽
                            float heigth = Math.Abs(e.Y - startPointF.Y);//确定矩形的高
                            PointF rectStartPointF = startPointF;
                            if (e.X < startPointF.X)
                            {
                                rectStartPointF.X = e.X;
                            }
                            if (e.Y < startPointF.Y)
                            {
                                rectStartPointF.Y = e.Y;
                            }
                            newgraphics.DrawRectangle(p, rectStartPointF.X, rectStartPointF.Y, width, heigth);  //画矩形
                            break;
                        }
                    case "Circle":
                        {//画圆形
                            newgraphics.DrawEllipse(p, startPointF.X, startPointF.Y, e.X - startPointF.X, e.Y - startPointF.Y); break;
                        }
                }
                newgraphics.Dispose();//绘图完毕释放中间画板所占资源
                newgraphics = Graphics.FromImage(finishingImg);         //finishingImg为画板
                newgraphics.DrawImage(img, 0, 0);                       //在上面画出了一个框
                newgraphics.Dispose();

                DrawTools_Graphics.DrawImage(img, 0, 0);    //在原始上画出新的一层
                img.Dispose();
            }

        }

        public void EndDraw()
        {
            startDraw = false;
            //为了让完成后的绘图过程保留下来，要将中间图片绘制到原始画布上
            newgraphics = Graphics.FromImage(orginalImg);   //建立原始图片的画布
            newgraphics.DrawImage(finishingImg, 0, 0);      //在其上显示finishingImg
            newgraphics.Dispose();

        }

        /// <summary>
        /// 清除变量，释放内存
        /// </summary>
        public void ClearVar()
        {
            DrawTools_Graphics.Dispose();
            finishingImg.Dispose();
            orginalImg.Dispose();
            p.Dispose();
        }

    }
}
