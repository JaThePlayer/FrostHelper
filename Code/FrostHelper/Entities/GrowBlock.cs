namespace FrostHelper.Entities;

// todo: grow block spikes, which implement IGrowBlockExtendible,
// which allows them to stretch and split to always cover a certain side of the block,
// which you would just place on the main block in the map editor.

[CustomEntity("FrostHelper/GrowBlock")]
public sealed class GrowBlock : Entity {
    private MTexture BaseTexture;
    public Vector2 StartPos;

    public Point BlockSize;

    public bool GiveLiftBoost;
    public float BlockGrowTime;
    public float CollapseTime;
    public int MaxBlocks;
    public readonly Color Tint;

    Vector2[] Nodes;
    List<Vector2> BlockPositions;
    List<Block> Blocks;
    Coroutine? GrowCoroutine;

    public GrowBlock(EntityData data, Vector2 offset) : base(data.Position + offset) {
        string texturePath = data.Attr("texture", "objects/FrostHelper/growBlock/green");
        string flag = data.Attr("flag");

        BaseTexture = GFX.Game[texturePath];
        BlockSize = new(BaseTexture.Width, BaseTexture.Height);

        Nodes = data.NodesOffset(offset);
        BlockGrowTime = data.Float("blockGrowTime", 0.1f);
        CollapseTime = data.Float("vanishTime", 1f);
        GiveLiftBoost = data.Bool("giveLiftBoost", true);
        Tint = data.GetColor("tint", "ffffff");
        MaxBlocks = data.Int("maxBlocks", 0);
        if (MaxBlocks <= 0) {
            MaxBlocks = int.MaxValue;
        }

        Add(new FlagListener(flag, OnFlag, false, false));

        Blocks = new();

        CalculateTargetBlockPositions();
    }

    private void CalculateTargetBlockPositions() {
        BlockPositions = new();

        var start = Position;
        for (int i = 0; i < Nodes.Length; i++) {
            var node = Nodes[i];

            while (start != node) {
                start.X = Calc.Approach(start.X, node.X, BlockSize.X);
                start.Y = Calc.Approach(start.Y, node.Y, BlockSize.Y);

                BlockPositions.Add(start);
            }

            start = node;
        }
    }

    public void OnFlag(bool val) {
        if (val) {
            if (GrowCoroutine is null || GrowCoroutine is { Finished: true }) {
                GrowCoroutine = new Coroutine(GrowRoutine());
                Add(GrowCoroutine);
            }
        } else {
            // flag just got unset - remove all blocks
            //GrowCoroutine?.RemoveSelf();
            //GrowCoroutine = null;

            //CollapseAllBlocks();
        }
    }

    public IEnumerator GrowRoutine() {
        var i = 0;

        var startPos = Position;
        while (i < BlockPositions.Count) {
            var blockPos = BlockPositions[i];

            if (Blocks.Count < MaxBlocks) {
                AddBlock(startPos, startPos, allowStaticMovers: false);
                Blocks[0].AddMoveTween(startPos, blockPos, BlockGrowTime, GiveLiftBoost);
            } else {
                for (int j = 0; j < Blocks.Count; j++) {
                    var block = Blocks[j];

                    block.AddMoveTween(block.Position, BlockPositions[j + i - MaxBlocks + 1], BlockGrowTime, GiveLiftBoost);
                }
            }

            yield return BlockGrowTime;

            i++;
            startPos = blockPos;
        }

        float collapseTime = CollapseTime;
        const float flashTime = 0.75f;
        if (collapseTime > flashTime) {
            yield return collapseTime - flashTime;
            collapseTime = flashTime;
        }

        float t = 0;
        while (t < collapseTime) {
            var percent = t / collapseTime;
            var alpha = 1.5f - (4f * percent) % 1f;
            foreach (var block in Blocks) {
                block.Image.Color = Color.White * alpha;
            }


            t += Engine.DeltaTime;
            yield return Engine.DeltaTime;
        }

        CollapseAllBlocks();
    }

    private void AddInitialBlock() {
        AddBlock(Position, Position, allowStaticMovers: true);
    }

    public override void Added(Scene scene) {
        base.Added(scene);

        AddInitialBlock();
    }


    public void CollapseAllBlocks() {
        var startBlock = Blocks[0];

        for (int i = 1; i < Blocks.Count; i++) {
            Blocks[i].RemoveSelf();
        }

        Blocks.Clear();
        Blocks.Add(startBlock);
        startBlock.Reset(Position);
    }

    public void AddBlock(Vector2 startPos, Vector2 goalPos, bool allowStaticMovers) {
        var block = new Block(startPos.Floor(), BaseTexture, BlockSize, Tint);
        block.AllowStaticMovers = allowStaticMovers;

        block.AddMoveTween(startPos, goalPos, BlockGrowTime, GiveLiftBoost);

        Blocks.Add(block);
        Scene.Add(block);
    }

    public class Block : Solid {
        public Image Image;

        public Block(Vector2 position, MTexture baseTexture, Point size, Color color) : base(position, size.X, size.Y, false) {
            Image = new Image(baseTexture);
            Image.Color = color;

            Add(Image);
        }

        public void Reset(Vector2 pos) {
            MoveToNaive(pos);
            Image.Color = Color.White;
        }

        public Tween? AddMoveTween(Vector2 startPos, Vector2 goalPos, float time, bool liftBoost) {
            if (startPos == goalPos)
                return null;

            var tw = Tween.Create(Tween.TweenMode.Oneshot, Ease.Linear, time, true);
            tw.OnUpdate = (t) => {
                var nextPos = Vector2.Lerp(startPos, goalPos, t.Eased).Floor();
                if (liftBoost) {
                    MoveTo(nextPos);
                } else {
                    MoveTo(nextPos, liftSpeed: default);
                }
            };

            //todo: optimise when the movement ends - if this block moved in the same direction as the last block, then merge the hitboxes and remove this block

            Add(tw);

            return tw;
        }
    }
}
