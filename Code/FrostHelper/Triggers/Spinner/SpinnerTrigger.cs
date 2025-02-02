using FrostHelper.Components;
using FrostHelper.Helpers;
using FrostHelper.SessionExpressions;

namespace FrostHelper.Triggers.Spinner;

[Tracked(true)]
internal abstract class SpinnerTrigger : Trigger {
    private readonly ConditionHelper.Condition _filter, _cacheFilter;
    private readonly bool _oncePerSpinner;
    
    private readonly Vector2? _firstNode;
    private SpinnerTrigger? _nextTrigger;
    private readonly float _nextTriggerDelay;
    
    private List<Entity> _cachedSpinners;
    
    
    protected SpinnerTrigger(EntityData data, Vector2 offset) : base(data, offset)
    {
        _filter = data.GetCondition(ExpressionContext, "filter", "");
        _cacheFilter = data.GetCondition(ExpressionContext, "cacheFilter", "");
        _oncePerSpinner = data.Bool("oncePerSpinner");
        
        _firstNode = data.FirstNodeNullable(offset);
        _nextTriggerDelay = data.Float("nextTriggerDelay");
    }
    
    public override void Awake(Scene scene) {
        base.Awake(scene);

        var session = SceneAs<Level>().Session;
        
        if (!_cacheFilter.Empty) {
            _cachedSpinners = Scene.Tracker.SafeGetEntities<CustomSpinner>()
                .Where(x => _cacheFilter.Check(session, x))
                .ToList();
        } else {
            _cachedSpinners = Scene.Tracker.SafeGetEntities<CustomSpinner>();
            
            // If _oncePerSpinner is set, we'll be mutating the list, make sure we don't destroy the tracker.
            if (_oncePerSpinner)
                _cachedSpinners = _cachedSpinners.ToList();
        }

        if (_firstNode is { }) {
            _nextTrigger = Scene.CollideFirst<SpinnerTrigger>(_firstNode.Value);
        }
    }
    
    public override void OnEnter(Player player) {
        base.OnEnter(player);
        
        var session = SceneAs<Level>().Session;
        var spinners = _cachedSpinners;

        for (int i = spinners.Count - 1; i >= 0; i--)
        {
            var spinner = (CustomSpinner)spinners[i];
            
            if (!HandleSpinner(session, spinner, fromExternalSource: false))
                continue;

            if (_oncePerSpinner)
                spinners.RemoveAt(i);
        }
    }

    protected abstract void ChangeSpinner(Session session, CustomSpinner spinner, bool fromExternalSource);
    
    private bool HandleSpinner(Session session, CustomSpinner spinner, bool fromExternalSource) {
        if (spinner.Scene is null)
            return false;
        
        if (!_filter.Check(session, spinner))
            return false;
        
        if (fromExternalSource) {
            // The spinner got activated externally, so it didn't go past the cache/oncePerSpinner checks yet.
            // While these options are unlikely to be used together, we'll still support it,
            // even if these normally perf-saving methods are actually slower in this usecase.
            if (!_cacheFilter.Empty || _oncePerSpinner) {
                // In either one of these cases, the spinner needs to be in-cache:
                // - !_cacheFilter.Empty -> To show that it passed the cache filter
                // - _oncePerSpinner -> To show we haven't changed it yet.
                var cacheIdx = _cachedSpinners.IndexOf(spinner);
                if (cacheIdx < 0)
                    return false;
                
                if (_oncePerSpinner)
                    _cachedSpinners.RemoveAt(cacheIdx);
            }
        }
        
        ChangeSpinner(session, spinner, fromExternalSource);

        if (_nextTrigger is { }) {
            if (_nextTriggerDelay <= 0f)
                _nextTrigger.HandleSpinner(session, spinner, fromExternalSource: this != _nextTrigger);
            else
                Add(new Coroutine(DelayedActivateSpinner(session, spinner)));
        }

        return true;
    }

    private IEnumerator DelayedActivateSpinner(Session session, CustomSpinner spinner) {
        yield return _nextTriggerDelay;
        _nextTrigger?.HandleSpinner(session, spinner, fromExternalSource: this != _nextTrigger);
    }
    
    
    #region Session Expressions
    private static ExpressionContext ExpressionContext { get; } = EntityContext.Default.CloneWith(
        new() {
            ["s"] = new SpinnerCondition(),
            ["attachGroup"] = new SpinnerAttachGroupCondition(),
            ["directory"] = new SpinnerDirectoryCondition(),
            ["origDirectory"] = new SpinnerOrigDirectoryCondition(),
            ["playerDist"] = new SpinnerPlayerDistCondition(),
            ["animFrame"] = new SpinnerAnimFrameCondition(),
            ["playerCollides"] = new SpinnerPlayerCollidesCondition(),
        }, 
        new()
    );
    
    private sealed class SpinnerCondition : EntityCondition<CustomSpinner> {
        protected override object GetValue(Session session, CustomSpinner entity) {
            return entity;
        }

        protected internal override Type ReturnType => typeof(CustomSpinner);
    }
    
    private sealed class SpinnerAttachGroupCondition : EntityCondition<CustomSpinner> {
        protected override object GetValue(Session session, CustomSpinner entity) {
            return entity.AttachGroup;
        }

        protected internal override Type ReturnType => typeof(int);
    }
    
    private sealed class SpinnerDirectoryCondition : EntityCondition<CustomSpinner> {
        protected override object GetValue(Session session, CustomSpinner entity) {
            return entity.SpriteSource.OrigDirectoryString;
        }

        protected internal override Type ReturnType => typeof(string);
    }
    
    private sealed class SpinnerOrigDirectoryCondition : EntityCondition<CustomSpinner> {
        protected override object GetValue(Session session, CustomSpinner entity) {
            return entity.directory;
        }

        protected internal override Type ReturnType => typeof(string);
    }
    
    private sealed class SpinnerPlayerDistCondition : EntityCondition<CustomSpinner> {
        protected override object GetValue(Session session, CustomSpinner entity) {
            var player = entity.Scene.Tracker.SafeGetEntity<Player>();
            if (player is null)
                return float.MaxValue;
            
            return Vector2.Distance(player.Center, entity.Position);
        }

        protected internal override Type ReturnType => typeof(float);
    }
    
    private sealed class SpinnerPlayerCollidesCondition : EntityCondition<CustomSpinner> {
        protected override object GetValue(Session session, CustomSpinner entity) {
            var player = entity.Scene.Tracker.SafeGetEntity<Player>();
            if (player is null)
                return false;
            
            entity.CreateCollider();
            
            return entity.Collider?.Collide(player) ?? false;
        }

        protected internal override Type ReturnType => typeof(bool);
    }
    
    private sealed class SpinnerAnimFrameCondition : EntityCondition<CustomSpinner> {
        protected override object GetValue(Session session, CustomSpinner entity) {
            if (entity._images is [AnimatedImage first, ..]) {
                return first.GetAnimFrame(entity.controller.TimeUnpaused);
            }

            return 0;
        }

        protected internal override Type ReturnType => typeof(int);
    }
    #endregion
}