using System; using System.Runtime.InteropServices;
namespace SupressIt.Helpers
{
    public static class KillSound
    {
        [DllImport("winmm.dll")] static extern bool PlaySound(string s,IntPtr h,uint f);
        public static void Play()=>PlaySound("SystemExclamation",IntPtr.Zero,0x00010001);
    }
}
