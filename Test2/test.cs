using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenGLF;
using OpenTK;
using System.Threading;

namespace Test2
{
    class CapicityList<T>
    {
        protected List<T> list = new List<T>();

        public CapicityList() { }

        public CapicityList(int initCapacity)
        {
            capacity = initCapacity;
        }

        protected int capacity = 100;
        public int Capacity
        {
            get { return capacity; }
            set
            {
                if (capacity > 0)
                {
                    capacity = value;
                    Check();
                }
            }
        }

        protected void Check()
        {
            while (list.Count > capacity)
            {
                list.RemoveAt(0);
            }
        }

        public void Push(T item)
        {
            list.Add(item);
            Check();
        }
        
        IEnumerable<T> GetEnumerable() { return list.AsEnumerable(); }

        public T Get(int index) { return list[index]; }

        public int Count
        {
            private set { }
            get { return list.Count; }
        }
    }

    class test
    {
        class MainWindow : Window
        {
            GameObject test;
            Font font;

            OsuListenner listener = new OsuListenner();

            Thread thread = null;

            object lockObj = new object();

            float Prev_Acc = 0.0f;
            float Acc_Scale = 0.5f;
            int Acc_AddCount = 0;

            CapicityList<float> HpRecorder=new CapicityList<float>(100), AccRecorder = new CapicityList<float>(100);

            string info = "";

            bool HpVisiable=true,AccVisiable=true;

            public MainWindow()
            {
                engine.afterDraw += Engine_afterDraw;
            }

            private void Engine_afterDraw()
            {
                Drawing.drawText(new Vector(Width-220, Height-25), Vector.zero, new Vector(1, 1), 0, 220, 50, info ,new Color(255,255,0,125), 15, font);

                lock (lockObj)
                {
                    if (HpVisiable)
                        DrawHpLines();
                    if (AccVisiable)
                        DrawAccLines();
                }
            }

            protected override void OnUpdateFrame(FrameEventArgs e)
            {
                base.OnUpdateFrame(e);
                info =String.Format("f{0}/u{1:F2}ms/r{2:F2}ms",Math.Truncate(1.0f/(UpdateTime + RenderTime)),UpdateTime*1000,RenderTime*1000);
            }

            protected override void OnLoad(EventArgs e)
            {
                base.OnLoad(e);

                Engine.scene = new Scene();
                font = new Font("Assets/OpenSans-Bold.ttf");

                listener.onChangeBeatmapSetId += Listener_onChangeBeatmapSetId;
                listener.onChangeBeatmapId += Listener_onChangeBeatmapId;
                listener.onConnect += Listener_onConnect;
                listener.onConnectLost += Listener_onConnectLost;
                listener.onPlayerFailed += Listener_onPlayerFailed;
                listener.onPlayerPause += Listener_onPlayerPause;
                listener.onUpdateACC += Listener_onUpdateACC;
                listener.onUpdateHP += Listener_onUpdateHP;
                listener.onUpdateTitle += Listener_onUpdateTitle;
                listener.onUpdateDiff += Listener_onUpdateDiff;

                thread = new Thread(() => {
                    listener.Start();
                });

                thread.Start();
            }

            protected override void OnRenderFrame(FrameEventArgs e)
            {
                base.OnRenderFrame(e);
            }

            #region DrawHp/AccLines

            protected void DrawHpLines()
            {
                float Now, Prev;
                for(int i = HpRecorder.Count-1; i >0; i--)
                {
                    Now = HpRecorder.Get(i);
                    Prev = HpRecorder.Get(i - 1);
                    Vector startVec = MapPoint(i / (float)HpRecorder.Capacity, 1-Now / 200.0f);
                    Vector endVec = MapPoint((i - 1) / (float)HpRecorder.Capacity, 1- Prev / 200.0f);

                    Drawing.drawLine(
                        startVec,
                        endVec,
                        2.5f,
                        Color.green
                        );
                }
            }

            protected void DrawAccLines()
            {
                float Now, Prev;
                for (int i = AccRecorder.Count - 1; i > 0; i--)
                {
                    Now = AccRecorder.Get(i);
                    Prev = AccRecorder.Get(i - 1);
                    Drawing.drawLine(
                        MapPoint(i / (float)AccRecorder.Capacity, ((Now/Acc_Scale)/2+0.5f)),
                        MapPoint((i - 1) / (float)AccRecorder.Capacity, (Prev / Acc_Scale) / 2 + 0.5f),
                        2.5f,
                        Color.blue
                        );
                }
            }
#endregion

            public Vector MapPoint(float nX, float nY)
            {
                return new Vector(Width * nX, Height * nY);
            }

            #region OsuListenerCallback
            private void Listener_onChangeBeatmapId(int id)
            {
                Console.WriteLine("beatmapId :" + id);
            }

            private void Listener_onChangeBeatmapSetId(int setId)
            {
                Console.WriteLine("beatmapSetId :" + setId);
            }

            private void Listener_onPlayerPause()
            {
                Console.WriteLine("player is pause or return!");
            }

            private void Listener_onPlayerFailed()
            {
                Console.WriteLine("player is failed!");
            }

            private void Listener_onUpdateDiff(string diffName)
            {
                Console.WriteLine("diff :" + diffName);
            }

            private void Listener_onUpdateTitle(string title)
            {
                Console.WriteLine("title :" + title);
            }

            private void Listener_onUpdateHP(double hp)
            {
                lock (lockObj)
                {
                    HpRecorder.Push(Convert.ToSingle(hp));
                }
                Console.WriteLine("hp:{0:F2},",hp);
            }

            private void Listener_onUpdateACC(double acc)
            {
                float NowAcc = Convert.ToSingle(acc);
                float divc = NowAcc - Prev_Acc;
                Acc_AddCount++;
                Acc_Scale = Convert.ToSingle(Math.Pow(Acc_AddCount,1.01))-1;

                if (Math.Abs(divc) > Acc_Scale)
                    divc = (Math.Sign(divc)<0?-1:1)*Acc_Scale;
                lock (lockObj)
                {
                    AccRecorder.Push(divc);
                }
                Prev_Acc = NowAcc;
                Console.WriteLine("acc:{0:F2},", acc);
            }

            private void Listener_onConnectLost()
            {
                Console.WriteLine("connection lost!");
            }

            private void Listener_onConnect(bool isReconnect)
            {
                Console.WriteLine("connected!");
            }
            #endregion
        }


        static void Main(string[] args)
        {
            
            MainWindow window = new MainWindow();
            window.Size = new System.Drawing.Size(712, 100);
            window.Title = "test";
            window.Run();
        }
    }
}
