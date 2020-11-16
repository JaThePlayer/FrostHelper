using Celeste;
using Monocle;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod;

namespace FrostHelper
{
    public class NoDashArea : Entity
    {
        Color color = Color.Red * 0.15f;
        Color color2 = Color.Red * 0.25f;
        PlayerCollider pc;
        bool colliding;

        public static float[] speeds = new float[]
		{
		    12f,
		    20f,
		    40f
		};

        public float Flash;

        public float Solidify;

        public bool Flashing => Flash > 0f;

        private List<Vector2> particles;

        public Vector2? Node;

        bool fastMoving;

        public NoDashArea(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            fastMoving = data.Bool("fastMoving", false);
            Collider = new Hitbox(data.Width, data.Height);
            Add(pc = new PlayerCollider(new Action<Player>(OnPlayer), null, null));
            Add(new DisplacementRenderHook(new Action(RenderDisplacement)));
            float num = 0;
            particles = new List<Vector2>();
            while (num < base.Width * base.Height / 16f)
            {
                this.particles.Add(new Vector2(Calc.Random.NextFloat(base.Width - 1f), Calc.Random.NextFloat(base.Height - 1f)));
                num++;
            }
            Node = data.FirstNodeNullable(new Vector2?(offset));
            if (Node != null)
            {
                Vector2 start = this.Position;
                Vector2 end = Node.Value;
                float num2 = Vector2.Distance(start, end) / 12f;
                bool flag2 = fastMoving;
                if (flag2)
                {
                    num2 /= 3f;
                }
                Tween tween = Tween.Create(Tween.TweenMode.YoyoLooping, Ease.SineInOut, num2, true);
                tween.OnUpdate = delegate (Tween t)
                {
                    bool collidable = this.Collidable;
                    if (collidable)
                    {
                        Position = (Vector2.Lerp(start, end, t.Eased));
                    }
                    else
                    {
                        Position = (Vector2.Lerp(start, end, t.Eased));
                    }
                };
                base.Add(tween);
            }
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            /*
            if (scene.Tracker.GetEntity<NoDashAreaRenderer>() == null)
            {
                NoDashAreaRenderer renderer;
                scene.Add(renderer = new NoDashAreaRenderer());
                renderer.Track(this, scene);
            } else
            {
                scene.Tracker.GetEntity<NoDashAreaRenderer>().Track(this, scene);
            } */
            
        }

        public override void Removed(Scene scene)
        {
            base.Removed(scene);
            //scene.Tracker.GetEntity<NoDashAreaRenderer>().Untrack(this);
        }

        public void OnPlayer(Player player)
        {
            Player_dashCooldownTimer.SetValue(player, Engine.DeltaTime + 0.1f);
        }

        private FieldInfo Player_dashCooldownTimer = typeof(Player).GetField("dashCooldownTimer", BindingFlags.NonPublic | BindingFlags.Instance);

        public override void Update()
        {
            base.Update();
            foreach (Player player in SceneAs<Level>().Tracker.GetEntities<Player>())
            {
                colliding = pc.Check(player);
            }
            if (colliding && Input.Dash.Pressed)
            {
                Solidify = 1f;
                Flash = 1f;
            }
            bool flag3 = this.Solidify > 0f;
            if (flag3)
            {
                Solidify = Calc.Approach(this.Solidify, 0f, Engine.DeltaTime);
            }
            if (Flashing)
                Flash = Calc.Approach(this.Flash, 0f, Engine.DeltaTime * 4f);
            int num = speeds.Length;
            float height = base.Height;
            int i = 0;
            int count = this.particles.Count;
            while (i < count)
            {
                Vector2 value = this.particles[i] + Vector2.UnitY * speeds[i % num] * Engine.DeltaTime;
                value.Y %= height - 1f;
                this.particles[i] = value;
                i++;
            }
            
        }

        public override void Render()
        {
            Color color = Color.White * 0.5f;
            Draw.Rect(Collider, Color.Red * 0.25f);
            foreach (Vector2 value in particles)
            {
                Draw.Pixel.Draw(Position + value, Vector2.Zero, color);
            }
            if (Flashing)
            {
                Draw.Rect(Collider, Color.White * Flash * 0.25f);
            }
        }

        public void RenderDisplacement()
        {
            Draw.Rect(X, Y, Width, Height, new Color(0.5f, 0.5f, 0.8f, 1f));
        }

    }

    /*
    [Tracked(false)]
    public class NoDashAreaRenderer : Entity
    {
        public NoDashAreaRenderer()
        {
            this.list = new List<NoDashArea>();
            this.edges = new List<Edge>();
            base.Tag = (Tags.Global | Tags.TransitionUpdate);
            base.Depth = 0;
            base.Add(new CustomBloom(new Action(this.OnRenderBloom)));
        }

        public void Track(NoDashArea block, Scene scene)
        {
            this.list.Add(block);
            bool flag = this.tiles == null;
            if (flag)
            {
                this.levelTileBounds = (scene as Level).TileBounds;
                this.tiles = new VirtualMap<bool>(this.levelTileBounds.Width, this.levelTileBounds.Height, false);
            }
            int num = (int)block.X / 8;
            while ((float)num < block.Right / 8f)
            {
                int num2 = (int)block.Y / 8;
                while ((float)num2 < block.Bottom / 8f)
                {
                    this.tiles[num - this.levelTileBounds.X, num2 - this.levelTileBounds.Y] = true;
                    num2++;
                }
                num++;
            }
            this.dirty = true;
        }

        public void Untrack(NoDashArea block)
        {
            this.list.Remove(block);
            bool flag = this.list.Count <= 0;
            if (flag)
            {
                this.tiles = null;
            }
            else
            {
                int num = (int)block.X / 8;
                while ((float)num < block.Right / 8f)
                {
                    int num2 = (int)block.Y / 8;
                    while ((float)num2 < block.Bottom / 8f)
                    {
                        this.tiles[num - this.levelTileBounds.X, num2 - this.levelTileBounds.Y] = false;
                        num2++;
                    }
                    num++;
                }
            }
            this.dirty = true;
        }

        public override void Update()
        {
            bool flag = this.dirty;
            if (flag)
            {
                this.RebuildEdges();
            }
            this.UpdateEdges();
        }

        public void UpdateEdges()
        {
            Camera camera = (base.Scene as Level).Camera;
            Rectangle rectangle = new Rectangle((int)camera.Left - 4, (int)camera.Top - 4, (int)(camera.Right - camera.Left) + 8, (int)(camera.Bottom - camera.Top) + 8);
            for (int i = 0; i < this.edges.Count; i++)
            {
                bool visible = this.edges[i].Visible;
                if (visible)
                {
                    bool flag = base.Scene.OnInterval(0.25f, (float)i * 0.01f) && !this.edges[i].InView(ref rectangle);
                    if (flag)
                    {
                        this.edges[i].Visible = false;
                    }
                }
                else
                {
                    bool flag2 = base.Scene.OnInterval(0.05f, (float)i * 0.01f) && this.edges[i].InView(ref rectangle);
                    if (flag2)
                    {
                        this.edges[i].Visible = true;
                    }
                }
                bool flag3 = this.edges[i].Visible && (base.Scene.OnInterval(0.05f, (float)i * 0.01f) || this.edges[i].Wave == null);
                if (flag3)
                {
                    this.edges[i].UpdateWave(base.Scene.TimeActive * 3f);
                }
            }
        }

        private void RebuildEdges()
        {
            this.dirty = false;
            this.edges.Clear();
            bool flag = this.list.Count > 0;
            if (flag)
            {
                Level level = base.Scene as Level;
                int left = level.TileBounds.Left;
                int top = level.TileBounds.Top;
                int right = level.TileBounds.Right;
                int bottom = level.TileBounds.Bottom;
                Point[] array = new Point[]
                {
                    new Point(0, -1),
                    new Point(0, 1),
                    new Point(-1, 0),
                    new Point(1, 0)
                };
                foreach (NoDashArea seekerBarrier in this.list)
                {
                    int num = (int)seekerBarrier.X / 8;
                    while ((float)num < seekerBarrier.Right / 8f)
                    {
                        int num2 = (int)seekerBarrier.Y / 8;
                        while ((float)num2 < seekerBarrier.Bottom / 8f)
                        {
                            foreach (Point point in array)
                            {
                                Point point2 = new Point(-point.Y, point.X);
                                bool flag2 = !this.Inside(num + point.X, num2 + point.Y) && (!this.Inside(num - point2.X, num2 - point2.Y) || this.Inside(num + point.X - point2.X, num2 + point.Y - point2.Y));
                                if (flag2)
                                {
                                    Point point3 = new Point(num, num2);
                                    Point point4 = new Point(num + point2.X, num2 + point2.Y);
                                    Vector2 value = new Vector2(4f) + new Vector2((float)(point.X - point2.X), (float)(point.Y - point2.Y)) * 4f;
                                    while (this.Inside(point4.X, point4.Y) && !this.Inside(point4.X + point.X, point4.Y + point.Y))
                                    {
                                        point4.X += point2.X;
                                        point4.Y += point2.Y;
                                    }
                                    Vector2 a = new Vector2((float)point3.X, (float)point3.Y) * 8f + value - seekerBarrier.Position;
                                    Vector2 b = new Vector2((float)point4.X, (float)point4.Y) * 8f + value - seekerBarrier.Position;
                                    this.edges.Add(new Edge(seekerBarrier, a, b));
                                }
                            }
                            num2++;
                        }
                        num++;
                    }
                }
            }
        }

        private bool Inside(int tx, int ty)
        {
            return this.tiles[tx - this.levelTileBounds.X, ty - this.levelTileBounds.Y];
        }

        private void OnRenderBloom()
        {
            Camera camera = (base.Scene as Level).Camera;
            Rectangle rectangle = new Rectangle((int)camera.Left, (int)camera.Top, (int)(camera.Right - camera.Left), (int)(camera.Bottom - camera.Top));
            foreach (NoDashArea seekerBarrier in this.list)
            {
                bool flag = !seekerBarrier.Visible;
                if (!flag)
                {
                    Draw.Rect(seekerBarrier.X, seekerBarrier.Y, seekerBarrier.Width, seekerBarrier.Height, Color.White);
                }
            }
            foreach (Edge edge in edges)
            {
                bool flag2 = !edge.Visible;
                if (!flag2)
                {
                    Vector2 value = edge.Parent.Position + edge.A;
                    Vector2 vector = edge.Parent.Position + edge.B;
                    int num = 0;
                    while ((float)num <= edge.Length)
                    {
                        Vector2 vector2 = value + edge.Normal * (float)num;
                        Draw.Line(vector2, vector2 + edge.Perpendicular * edge.Wave[num], Color.White);
                        num++;
                    }
                }
            }
        }

        public override void Render()
        {
            bool flag = this.list.Count <= 0;
            if (!flag)
            {
                Color color = Color.Red * 0.15f;
                Color value = Color.Red * 0.25f;
                foreach (NoDashArea seekerBarrier in this.list)
                {
                    bool flag2 = !seekerBarrier.Visible;
                    if (!flag2)
                    {
                        Draw.Rect(seekerBarrier.Collider, color);
                    }
                }
                bool flag3 = this.edges.Count > 0;
                if (flag3)
                {
                    foreach (Edge edge in this.edges)
                    {
                        bool flag4 = !edge.Visible;
                        if (!flag4)
                        {
                            Vector2 value2 = edge.Parent.Position + edge.A;
                            Vector2 vector = edge.Parent.Position + edge.B;
                            Color color2 = Color.Lerp(value, Color.White, edge.Parent.Flash);
                            int num = 0;
                            while ((float)num <= edge.Length)
                            {
                                Vector2 vector2 = value2 + edge.Normal * (float)num;
                                Draw.Line(vector2, vector2 + edge.Perpendicular * edge.Wave[num], color);
                                num++;
                            }
                        }
                    }
                }
            }
        }

        private List<NoDashArea> list;

        private List<Edge> edges;

        private VirtualMap<bool> tiles;

        private Rectangle levelTileBounds;

        private bool dirty;

        public class Edge
        {
            public Edge(NoDashArea parent, Vector2 a, Vector2 b)
            {
                this.Parent = parent;
                this.Visible = true;
                this.A = a;
                this.B = b;
                this.Min = new Vector2(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
                this.Max = new Vector2(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
                this.Normal = (b - a).SafeNormalize();
                this.Perpendicular = -this.Normal.Perpendicular();
                this.Length = (a - b).Length();
            }

            public void UpdateWave(float time)
            {
                bool flag = this.Wave == null || (float)this.Wave.Length <= this.Length;
                if (flag)
                {
                    this.Wave = new float[(int)this.Length + 2];
                }
                int num = 0;
                while ((float)num <= this.Length)
                {
                    this.Wave[num] = this.GetWaveAt(time, (float)num, this.Length);
                    num++;
                }
            }

            private float GetWaveAt(float offset, float along, float length)
            {
                bool flag = along <= 1f || along >= length - 1f;
                float result;
                if (flag)
                {
                    result = 0f;
                }
                else
                {
                    bool flag2 = this.Parent.Solidify >= 1f;
                    if (flag2)
                    {
                        result = 0f;
                    }
                    else
                    {
                        float num = offset + along * 0.25f;
                        float num2 = (float)(Math.Sin((double)num) * 2.0 + Math.Sin((double)(num * 0.25f)));
                        result = (1f + num2 * Ease.SineInOut(Calc.YoYo(along / length))) * (1f - this.Parent.Solidify);
                    }
                }
                return result;
            }

            public bool InView(ref Rectangle view)
            {
                return (float)view.Left < this.Parent.X + this.Max.X && (float)view.Right > this.Parent.X + this.Min.X && (float)view.Top < this.Parent.Y + this.Max.Y && (float)view.Bottom > this.Parent.Y + this.Min.Y;
            }

            public NoDashArea Parent;

            public bool Visible;

            public Vector2 A;

            public Vector2 B;

            public Vector2 Min;

            public Vector2 Max;

            public Vector2 Normal;

            public Vector2 Perpendicular;

            public float[] Wave;

            public float Length;
        }
    } */
}
