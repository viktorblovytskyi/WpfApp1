using System.Windows;
using System.Windows.Media;

namespace DesktopApplication.Controls
{
    public class DrawingVisualHost : FrameworkElement
    {
        private readonly VisualCollection _children;
        private DrawingVisual? _visual;

        public DrawingVisualHost()
        {
            _children = new VisualCollection(this);
        }

        protected override int VisualChildrenCount => _children.Count;

        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index >= _children.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            return _children[index];
        }

        public DrawingContext GetDrawingContext()
        {
            if (_visual != null)
            {
                _children.Remove(_visual);
            }

            _visual = new DrawingVisual();
            _children.Add(_visual);

            return _visual.RenderOpen();
        }

        public void Clear()
        {
            _children.Clear();
            _visual = null;
        }
    }
}
