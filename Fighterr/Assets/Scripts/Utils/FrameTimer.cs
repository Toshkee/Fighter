namespace SamuraiFighter.Utils
{
    public class FrameTimer
    {
        private int _frames;

        public int Frames => _frames;
        public bool IsRunning => _frames > 0;

        public void Set(int frames) => _frames = frames;

        public bool Tick()
        {
            if (_frames <= 0) return false;
            _frames--;
            return _frames == 0;
        }

        public void Clear() => _frames = 0;
    }
}
