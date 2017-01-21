using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenGLF;
using OpenTK;
using System.Threading;
using osu_database_reader;

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

        public void Clear() { list.Clear(); }

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

            OsuDb osuDataBase;
            bool ableDataBase = false;

            bool isVision = true;

            OsuListenner listener = new OsuListenner();

            Thread thread = null;

            volatile float passTime = 0;

            object lockObj = new object();

            float Prev_Acc = 0.0f;
            float Acc_Scale = 0.5f;
            int Acc_AddCount = 0;

            CapicityList<float> HpRecorder=new CapicityList<float>(100), AccRecorder = new CapicityList<float>(100);

            string info = "";

            string beatmapTitle = "";
            string beatmapDiff = "";

            bool HpVisiable=true,AccVisiable=true;

            public MainWindow()
            {
                engine.afterDraw += Engine_afterDraw;

                Console.WriteLine("loading osu!.db");
                try
                {
                    osuDataBase = OsuDb.Read(@"g:\osu!\osu!.db");
                    Console.WriteLine("loading database finished! db version {0},beatmap caches count {1}", osuDataBase.OsuVersion, osuDataBase.Beatmaps.Count);
                    ableDataBase = true;
                }
                catch (Exception e) { Console.WriteLine("load failed! "+e.Message); }
            }

            private void Engine_afterDraw()
            {
                if (!isVision)
                    return;
                //Drawing.drawText(new Vector(Width-220, Height-25), Vector.zero, new Vector(1, 1), 0, 220, 50, info ,new Color(255,255,0,125), 15, font);

                lock (lockObj)
                {

                    Drawing.drawText(new Vector(0, Height - 25), Vector.zero, new Vector(1, 1), 0, Width, 50,
                    string.Format("{1}[{2}]\tHP:{0}",HpRecorder.Count==0?"??":(Math.Truncate(HpRecorder.Get(HpRecorder.Count - 1))).ToString(), beatmapTitle, beatmapDiff),
                    new Color(255, 255, 0, 125), 15, font);

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
                passTime+=Convert.ToSingle(UpdateTime + RenderTime);
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
                if (ableDataBase)
                {
                    ThreadPool.QueueUserWorkItem(state =>
                    {
                        var list = osuDataBase.Beatmaps;
                        Parallel.For(0, list.Count, (i, LoopState) =>
                        {
                            if (list[i].BeatmapId == id)
                            {
                                Listener_onUpdateTitle(list[i].ArtistUnicode.Trim().Length==0?list[i].Artist: list[i].ArtistUnicode + " - " + (list[i].TitleUnicode.Trim().Length == 0 ? list[i].Title : list[i].TitleUnicode));
                                Listener_onUpdateDiff(list[i].Difficulty);
                                LoopState.Break();
                            }
                        });
                    }, id);
                }
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
                /*
                if (beatmapDiff.Trim() == diffName.Trim())
                    return;

                Console.WriteLine("diff :" + diffName); */
                beatmapDiff = diffName;
            }

            private void Listener_onUpdateTitle(string title)
            {
                /*
                if (beatmapTitle == title)
                    return;

                Console.WriteLine("title :" + title);*/
                beatmapTitle = title;
            }

            float prev_hp = 0;
            private void Listener_onUpdateHP(double hp)
            {
                float hpf = Convert.ToSingle(hp);

                if (Math.Abs(hpf - prev_hp) < 0.001)
                {
                    if (passTime > 1&&isVision)
                    {
                        isVision = false;
                        HpRecorder.Clear();
                        AccRecorder.Clear();
                        Console.WriteLine("Hide");
                    }
                }
                else
                {
                    //HP改变，说明在游戏中
                    passTime = 0;//刷新累积
                    isVision = true;
                    lock (lockObj)
                    {
                        HpRecorder.Push(hpf);
                        prev_hp = hpf;
                    }
                }
            }

            private void Listener_onUpdateACC(double acc)
            {
                float NowAcc = Convert.ToSingle(acc);
                float divc = NowAcc - Prev_Acc;
                Acc_AddCount++;

                float val = 1 - Convert.ToSingle(Math.Exp(-Math.Abs(divc * 2)));
                if (divc < 0) val = -val;

                lock (lockObj)
                {
                    AccRecorder.Push(divc * val);
                }
                Prev_Acc = NowAcc;
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
