using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Video.FFMPEG;
using DevExpress.XtraEditors.Controls;

namespace MonitDemo
{
    public partial class FormCameraCapture : Form
    {
        //用来操作摄像头
        private readonly VideoCaptureDevice _camera = null;
        //用来把每一帧图像编码到视频文件
        private readonly VideoFileWriter _videoOutPut = new VideoFileWriter();

        private Bitmap bmp = new Bitmap(1, 1);

        public FormCameraCapture()
        {
            InitializeComponent();

            var devs = new FilterInfoCollection(FilterCategory.VideoInputDevice); //获取摄像头列表
            _camera = new VideoCaptureDevice(devs[0].MonikerString); //实例化设备控制类(我选了第1个)

            var cameraSetupList = new List<CameraSetup>();
            int index = 0;
            foreach (var resolution in _camera.VideoCapabilities)
            {
                var cameraSetup = new CameraSetup
                    {
                        AverageFrameRate = resolution.AverageFrameRate.ToString(),
                        BitCount = resolution.BitCount.ToString(),
                        MaximumFrameRate = resolution.MaximumFrameRate.ToString(),
                        Height = resolution.FrameSize.Height.ToString(),
                        Width = resolution.FrameSize.Width.ToString(),
                        Index = index
                    };
                cameraSetup.Value = string.Format("{0}_{1}_{2}_{3}_{4}", cameraSetup.AverageFrameRate,
                                                  cameraSetup.BitCount, cameraSetup.MaximumFrameRate, cameraSetup.Height,
                                                  cameraSetup.Width);
                cameraSetupList.Add(cameraSetup);
                index++;
            }
            
            lupSetup.Properties.DataSource = cameraSetupList;
            lupSetup.Properties.Columns.Clear();
            LookUpColumnInfoCollection coll = lupSetup.Properties.Columns;
            coll.Add(new LookUpColumnInfo("AverageFrameRate", "AverageFrameRate", 0));
            coll.Add(new LookUpColumnInfo("BitCount", "BitCount", 1));
            coll.Add(new LookUpColumnInfo("MaximumFrameRate", "MaximumFrameRate", 2));
            coll.Add(new LookUpColumnInfo("Height", "Height", 3));
            coll.Add(new LookUpColumnInfo("Width", "Width", 4));
            lupSetup.Properties.DisplayMember = "Value";
            lupSetup.Properties.ValueMember = "Index";
            lupSetup.Properties.BestFit();

            btnStop.Enabled = false;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if(lupSetup.EditValue == null)
                return;

            btnStart.Enabled = false;
            lupSetup.Enabled = false;
            btnStop.Enabled = true;
            _index = (int)lupSetup.EditValue;
            //配置录像参数(宽,高,帧率,比特率等参数)VideoCapabilities这个属性会返回摄像头支持哪些配置,从这里面选一个赋值接即可,我选了第1个
            _camera.VideoResolution = _camera.VideoCapabilities[_index];
            //设置回调,aforge会不断从这个回调推出图像数据
            _camera.NewFrame += Camera_NewFrame;

            videoSourcePlayer1.VideoSource = _camera;
            videoSourcePlayer1.Start();
            _camera.Start();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = true;
            lupSetup.Enabled = true;
            btnStop.Enabled = false;
            if (_camera != null)
            {
                _camera.Stop();
                //关闭录像文件,如果忘了不关闭,将会得到一个损坏的文件,无法播放
                _videoOutPut.Close();
                videoSourcePlayer1.Stop();
            }
        }

        private int _index = 1;
        private void btnStartRecord_Click(object sender, EventArgs e)
        {
            _videoOutPut.Open(string.Format("E:/VIDEO_{0}.MP4", _index),
                 _camera.VideoResolution.FrameSize.Width,
                 _camera.VideoResolution.FrameSize.Height,
                 _camera.VideoResolution.AverageFrameRate,
                 VideoCodec.MPEG4,
                 _camera.VideoResolution.BitCount);
        }

        private void btnStopRecord_Click(object sender, EventArgs e)
        {
            _videoOutPut.Close();
        }

        private void Camera_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            _videoOutPut.WriteVideoFrame(eventArgs.Frame);
            lock (bmp)
            {
                //释放上一个缓存
                bmp.Dispose();
                //保存一份缓存
                bmp = eventArgs.Frame.Clone() as Bitmap;
            }
        }
    }

    public class CameraSetup
    {
        public string AverageFrameRate { get; set; }
        public string BitCount { get; set; }
        public string MaximumFrameRate { get; set; }
        public string Height { get; set; }
        public string Width { get; set; }
        public string Value { get; set; }
        public int Index { get; set; }
    }
}