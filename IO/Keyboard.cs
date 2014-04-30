using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace Common.IO
{
    public static class Keyboard
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(System.Windows.Forms.Keys vKey);

        public static bool IsKeyPressed(Keys keys)
        {
            short state = GetAsyncKeyState(keys);

            return ((state & 0x8000) == 0x8000);
        }
    }
}
