using System.Windows.Forms;

namespace SDUI.Controls
{
    public class DoubleBufferedControl : UserControl
    {
        public DoubleBufferedControl()
        {
            SetStyle(
                 ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.EnableNotifyMessage,
            true
        );
        }

        protected override void OnNotifyMessage(Message m)
        {
            // Filter out WM_ERASEBKGND (0x14) to reduce flicker
            if (m.Msg != 0x14)
            {
                base.OnNotifyMessage(m);
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;

                return cp;
            }
        }
    }
}
