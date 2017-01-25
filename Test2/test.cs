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
            #region 定义区
            GameObject test;
            Font font;

            Frame currentFrame = new Frame();

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

            ModsInfo mod = new ModsInfo();

            int combo = 0;
            const int BigBreakComboLimit= 200,IgnoredComboLimit=20;

            CapicityList<Frame> frameRecorder = new CapicityList<Frame>(100);

            string info = "";

            string beatmapTitle = "";
            string beatmapDiff = "";

            bool HpVisiable=true,AccVisiable=true;

#endregion

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
                    string.Format("{0}[{1}]",beatmapTitle,beatmapDiff),
                    new Color(255, 255, 0, 125), 15, font);

                    Drawing.drawText(new Vector(0, Height - 50), Vector.zero, new Vector(1, 1), 0, Width, 50,
                    string.Format("Hp:{0}\tMods:{1}", frameRecorder.Count == 0 ? "??" : (Math.Truncate(frameRecorder.Get(frameRecorder.Count - 1).hp)).ToString(),mod.Name),
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
                listener.onChangeMods += Listener_onChangeMods;
                listener.onUpdateCombo += Listener_onUpdateCombo;
                listener.onUpdateFinish += Listener_onUpdateFinish;

                thread = new Thread(() => {
                    listener.Start();
                });

                thread.Start();
            }

            protected override void OnRenderFrame(FrameEventArgs e)
            {
                base.OnRenderFrame(e);
            }

            #region DrawHp/AccLines/ComboBreak

            protected void DrawHpLines()
            {
                float Now, Prev;
                for(int i = frameRecorder.Count-1; i >0; i--)
                {
                    Now = frameRecorder.Get(i).hp;
                    Prev = frameRecorder.Get(i - 1).hp;
                    Vector startVec = MapPoint(i / (float)frameRecorder.Capacity, 1-Now / 200.0f);
                    Vector endVec = MapPoint((i - 1) / (float)frameRecorder.Capacity, 1- Prev / 200.0f);

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
                int comboIndex = 0;

                for (int i = frameRecorder.Count - 1; i > 0; i--)
                {
                    Now = frameRecorder.Get(i).acc;
                    Prev = frameRecorder.Get(i - 1).acc;
                    Drawing.drawLine(
                        MapPoint(i / (float)frameRecorder.Capacity, ((Now/Acc_Scale)/2+0.5f)),
                        MapPoint((i - 1) / (float)frameRecorder.Capacity, (Prev / Acc_Scale) / 2 + 0.5f),
                        2.5f,
                        Color.blue
                        );

                    if (frameRecorder.Get(i).combo < frameRecorder.Get(i - 1).combo)
                    {
                        //符合条件,绘制comboBreak
                        Drawing.drawCircle(MapPoint(i / (float)frameRecorder.Capacity, ((Now / Acc_Scale) / 2 + 0.5f)), 5, true, 5, Color.red);
                    }
                }
            }
#endregion

            public Vector MapPoint(float nX, float nY)
            {
                return new Vector(Width * nX, Height * nY);
            }

            #region OsuListenerCallback

            private void Listener_onUpdateFinish()
            {
                if (!isVision)
                {
                    if(currentFrame.hp==0&&currentFrame.acc==0)
                        frameRecorder.Clear();
                    return;
                }
                frameRecorder.Push(currentFrame);
                currentFrame = new Frame();
                /*
                Console.WriteLine("count {0}:{1}\t{2}\t{3}",
                    frameRecorder.Count,
                    frameRecorder.Get(frameRecorder.Count-1).hp,
                    frameRecorder.Get(frameRecorder.Count - 1).acc,
                    frameRecorder.Get(frameRecorder.Count - 1).combo);
                    */
            }

            private void Listener_onUpdateCombo(int combo)
            {
                lock(lockObj){
                    currentFrame.combo = combo;
                }
            }

            private void Listener_onChangeMods(int mods)
            {
                this.mod.Mod = (ModsInfo.Mods)mods;
                Console.WriteLine("Mods : " + this.mod.ShortName);
            }

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
                beatmapDiff = diffName;
            }

            private void Listener_onUpdateTitle(string title)
            {
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
                        currentFrame.hp = hpf;
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
                    currentFrame.acc=(divc * val);
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
