namespace FrostHelper.Helpers;

internal sealed class SliderAccessor {
    private readonly string _counterName;
    private Session.Slider? _slider;
    private Session? _lastSession;
    
    public SliderAccessor(string name) {
        _counterName = name;
    }

    public Session.Slider GetObj(Session session) {
        if (session != _lastSession) {
            _slider = session.GetSliderObject(_counterName);
            _lastSession = session;
        } else {
            _slider ??= session.GetSliderObject(_counterName);
        }

        return _slider;
    }
    
    public float Get(Session session) {
        return GetObj(session).Value;
    }
    
    public void Set(Session session, float value) {
        GetObj(session).Value = value;
    }
}