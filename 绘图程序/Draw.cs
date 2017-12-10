using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Linq;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;


namespace 绘图程序
{
    public partial class Draw : Form
    {

        DrawTools dt;
        string sType = "Rect";//绘图样式 是矩形

        bool bReSize = false;//是否改变画布大小
        Size DefaultPicSize;//储存原始画布大小，用来新建文件时使用


        int cur_img_num = 0;
        int img_num = 0;                                    //总的图片数量
        List<string> img_src;                               //指定文件夹内所有的图片
        List<string> cur_txt_content = new List<string>(6);  //每个图片对应的图片标签
        Image newbitmap = null;                             //当前显示的图像
        StreamWriter sw;                                    //标定文件


        Point p1 = new Point(0, 0);     //第一次按下时候的坐标
        Point p2 = new Point(0, 0);     //第二次按下时候的坐标
        int mouse_state = 0;        //0 还没有按下  1 第一个点按下，第二个点还没按   

        //初始化
        public Draw()
        {
            InitializeComponent();
            this.panel2.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.Panel_MouseWheel);

            pbImg.Cursor = Cursors.Cross;   //鼠标的样式

            img_src = GetImgCollection("Img\\");//获取所有的图片路径
            img_num = img_src.Count();//图片个数
            cur_img_num = 0;            //当前第几张图片
            if (img_num>0)
                textBox1.Text = img_src[0];
            
            show_gui(0);                //显示第0张图片和标定内容

        }

        //＂窗体加载＂事件处理方法
        private void Form1_Load(object sender, EventArgs e)
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            this.UpdateStyles();
            Bitmap bmp = new Bitmap(pbImg.Width, pbImg.Height);
            Graphics g = Graphics.FromImage(bmp);
            g.FillRectangle(new SolidBrush(pbImg.BackColor), new Rectangle(0, 0, pbImg.Width, pbImg.Height));
            g.Dispose();
            //dt = new DrawTools(this.pbImg.CreateGraphics(), Color.Aquamarine, bmp);//实例化工具类
            DefaultPicSize = pbImg.Size;
            //show_gui(0);
        }

        //界面显示
        public void show_gui(int curpic)
        {
            Bitmap bmpformfile = (Bitmap)Image.FromFile(img_src[curpic]);   //获取打开的文件
            panel2.AutoScrollPosition = new Point(0, 0);                    //将滚动条复位
            pbImg.Size = bmpformfile.Size;                                  //调整绘图区大小为图片大小

            //因为我们初始时的空白画布大小有限，"打开"操作可能引起画板大小改变，所以要将画板重新传入工具类
            Bitmap bmp = new Bitmap(pbImg.Width, pbImg.Height);
            Graphics g = Graphics.FromImage(bmp);
            g.FillRectangle(new SolidBrush(pbImg.BackColor), new Rectangle(0, 0, pbImg.Width, pbImg.Height));//不使用这句话，那么这个bmp的背景就是透明的
            g.DrawImage(bmpformfile, 0, 0, bmpformfile.Width, bmpformfile.Height);//将图片画到画板上
            g.Dispose();//释放画板所占资源
            //不直接使用pbImg.Image = Image.FormFile(ofd.FileName)是因为这样会让图片一直处于打开状态，也就无法保存修改后的图片；详见http://www.wanxin.org/redirect.php?tid=3&goto=lastpost
            bmpformfile.Dispose();//释放图片所占资源
            g = pbImg.CreateGraphics();
            g.DrawImage(bmp, 0, 0);
            g.Dispose();

            dt = new DrawTools(pbImg.CreateGraphics(), Color.Aquamarine, bmp);//实例化工具类
            dt.OrginalImg = bmp;
            dt.DrawTools_Graphics = pbImg.CreateGraphics();
            bmp.Dispose();
            //读取照片对应的txt
            cur_txt_content = get_txt_content(img_src[curpic]);

            //列表上显示之前读取的坐标
            //先清空坐标列表的显示
            listBox1.Items.Clear();
            if (cur_txt_content.Count > 0)
            {
                for (int i = 0; i < cur_txt_content.Count; i++)
                {
                    listBox1.Items.Add(cur_txt_content[i]); //显示坐标列表
                }

            }
            dt.Draw_txt(cur_txt_content);

            //显示当前页码
            string show_label;
            show_label = curpic.ToString() + "/" + img_num.ToString();
            label1.Text = show_label;

        }


        //根据一个图片路径，读取图片文件同名的txt文档，如果不存在则添加新的txt文件
        public List<string> get_txt_content(string a_img_src)
        {
            List<string> mtxt_content = new List<string>();
            string path = a_img_src.Substring(0, a_img_src.Length - 3); //txt文件的路径
            path += "txt";
            //textBox7.Text = path; //显示提取到路径是否是txt
            try
            {
                if (File.Exists(path)) //文件必须存在
                {
                    using (StreamReader sr = new StreamReader(path, Encoding.Default)) //StreamReader读取文件流
                    {
                        while (sr.Peek() >= 0) //对应行有内容才继续
                        {
                            //读取的每行内容
                            mtxt_content.Add(sr.ReadLine());
                        }

                    }
                }
                else
                {
                    textBox1.Text = "Add new file.";
                    //新建一个对应的txt文档
                    FileStream fs1 = new FileStream(path, FileMode.Create, FileAccess.Write);
                    fs1.Close();
                }
            }
            catch (Exception e)
            {
                textBox1.Text = "The process failed: {0}";
            }
            return mtxt_content;
        }

        //pbimg＂鼠标按下＂事件处理方法
        public void pbImg_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (dt != null)
                {
                    dt.startDraw = true;//相当于所选工具被激活，可以开始绘图
                    dt.startPointF = new PointF(e.X, e.Y);
                    
                    p1 = e.Location;    //记录开始的像素位置
                }
            }
        }

        //pbimg＂鼠标移动＂事件处理方法
        public void pbImg_MouseMove(object sender, MouseEventArgs e)
        {
            Thread.Sleep(6);//减少cpu占用率
            mousePostion.Text = e.Location.ToString();
            if (dt.startDraw)
            {
                dt.Draw(e, sType, cur_txt_content);

            }
        }

        //pbimg＂鼠标松开＂事件处理方法
        public void pbImg_MouseUp(object sender, MouseEventArgs e)
        {
            if (dt != null)
            {
                dt.EndDraw();
                p2 = e.Location;    //记录 矩形截止点
                float width = Math.Abs(p1.X - p2.X);//确定矩形的宽
                if (width > 30)
                {
                    //矩形的坐标写入txt中
                    string path = img_src[cur_img_num].Substring(0, img_src[cur_img_num].Length - 3); //txt文件的路径
                    path += "txt";
                    sw = new StreamWriter(path, true, Encoding.Default);//true 追加 false 覆盖
                    string save_pos = p1.X.ToString() + " " + p1.Y.ToString() + " " + p2.X.ToString() + " " + p2.Y.ToString();
                    sw.WriteLine(save_pos);   //逐行输入
                    textBox1.Text = save_pos;
                    sw.Flush();
                    sw.Close();
                    //坐标list显示坐标
                    listBox1.Items.Add(save_pos);
                }
                
            }
            dt.Draw_txt(cur_txt_content);
        }

        //窗体移动最小化等造成的pbimg＂重画＂事件处理方法
        public void pbImg_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.DrawImage(dt.OrginalImg, 0, 0);
            //g.Dispose();切不可使用,这个Graphics是系统传入的变量，不是我们自己创建的，如果dispose就会出错

        }


        //＂颜色改变＂事件处理方法
        public void colorHatch1_ColorChanged(object sender, ColorHatch.ColorChangedEventArgs e)
        {
            dt.DrawColor = e.GetColor;
        }



        //下一张
        public void button1_Click(object sender, EventArgs e)
        {
            if (cur_img_num == img_num - 1)
                cur_img_num = 0;
            else
                cur_img_num++;
            textBox1.Text = img_src[cur_img_num];
            show_gui(cur_img_num);
        }

        //上一张
        public void button2_Click(object sender, EventArgs e)
        {
            if (cur_img_num == 0)
                cur_img_num = img_num - 1;
            else
                cur_img_num--;
            textBox1.Text = img_src[cur_img_num];
            show_gui(cur_img_num);

        }

        public void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        //获取路径内的所有图片
        public static List<string> GetImgCollection(string path)
        {
            string[] imgarray = Directory.GetFiles(path);
            var result = from imgstring in imgarray
                         where imgstring.EndsWith("jpg", StringComparison.OrdinalIgnoreCase) ||
                         imgstring.EndsWith("png", StringComparison.OrdinalIgnoreCase) ||
                         imgstring.EndsWith("bmp", StringComparison.OrdinalIgnoreCase)
                         select imgstring;
            return result.ToList();
        }

        //删除所有按钮
        private void button3_Click(object sender, EventArgs e)
        {
            //清空listbox的显示
            listBox1.Items.Clear();
            //清空当前txt内容，并保存...
            cur_txt_content.Clear();

            string path = img_src[cur_img_num].Substring(0, img_src[cur_img_num].Length - 3); //txt文件的路径
            path += "txt";
            sw = new StreamWriter(path, false, Encoding.Default);//true 追加 false 覆盖
            sw.Flush();
            sw.Close();

            //刷新显示
            show_gui(cur_img_num);
        }

        private void status_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        void Panel_MouseWheel(object sender, MouseEventArgs e)
        {
            dt.Draw_txt(cur_txt_content);
            //show_gui(cur_img_num);
            //textBox1.Text = "wheel!";
        }

    }
}