using System;
using System.Runtime.InteropServices;


namespace Consola.Test
{
    internal struct InputType
    {
        public const uint Mouse = 0;
        public const uint Typed = 1;
        public const uint Other = 2;
    }

    [Flags]
    public enum MFLAGS : uint {
        MOVE = 0x0001, //	Movement occurred.
        LEFTDOWN = 0x0002, //	The left button was pressed.
        LEFTUP = 0x0004,   //	The left button was released.
        RIGHTDOWN = 0x0008, //	The right button was pressed.
        RIGHTUP = 0x0010, //	The right button was released.
        MIDDLEDOWN = 0x0020, //	The middle button was pressed.
        MIDDLEUP = 0x0040, //	The middle button was released.
        XDOWN = 0x0080, //	An X button was pressed.
        XUP = 0x0100, //	An X button was released.
        WHEEL = 0x0800, //	The wheel was moved, if the mouse has a wheel. The amount of movement is specified in mouseData.
        HWHEEL = 0x1000, //	The wheel was moved horizontally, if the mouse has a wheel.The amount of movement is specified in mouseData. (Windows XP/2000: This value is not supported.)
        MOVE_NOCOALESCE = 0x2000, //	The WM_MOUSEMOVE messages will not be coalesced.The default behavior is to coalesce WM_MOUSEMOVE messages. (Windows XP/2000: This value is not supported.)
        VIRTUALDESK = 0x4000, //	Maps coordinates to the entire desktop. Must be used with MOUSEEVENTF_ABSOLUTE.
        ABSOLUTE = 0x8000	//The dx and dy members contain normalized absolute coordinates.If the flag is not set, dxand dy contain relative data (the change in position since the last reported position ). This flag can be set, or not set, regardless of what kind of mouse or other pointing device, if any, is connected to the system.For further information about relative mouse motion, see the following Remarks section.
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MOUSEINPUT
    {
        public Int32      dx;
        public Int32      dy;  // hmmm ????
        public Int32      dw;
        public MFLAGS     dwFlags;
        public UInt32     time;
        public UIntPtr    dwExtraInfo; // x64 8byte / x86 4byte
    }

    [Flags]
    internal enum KFLAGS : uint {
        EXTENDEDKEY = 0x0001, // if specified, the scan code was preceded by a prefix byte that has the value 0xE0 (224).
        KEYUP = 0x0002, // If specified, the key is being released. If not specified, the key is being pressed.
        SCANCODE = 0x0008, // If specified, wScan identifies the key and wVk is ignored.
        UNICODE = 0x0004 // If specified, the system synthesizes a VK_PACKET keystroke. The wVk parameter must be zero. This flag can only be combined with the KEYEVENTF_KEYUP flag. For more information, see the Remarks section.
    }
    [StructLayout(LayoutKind.Sequential, Size = 20)]
    internal struct KEYBDINPUT
    {
        public UInt16      wVk;  // ASCII value   (if dwFlgs is UNICODE then must be 0)
        public UInt16      wScan; // Unicode value (only applies when dwFlag is UNICODE)
        public KFLAGS      dwFlags; 
        public UInt32      time; // set 0 to make system fills in tiestamp on its own 
        public UIntPtr     dwExtraInfo;  // x64 8byte / x86 4byte
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    internal struct HARDWAREINPUT
    {
        UInt32 uMsg;
        UInt16 wParamL;
        UInt16 wParamH;
    }


    [StructLayout(LayoutKind.Sequential)]
    internal struct MOUSE_INPUT
    {
        public UInt32     type;
        public MOUSEINPUT data;
        public MOUSE_INPUT( ConTrol.Move f, int x, int y ) : this()
        {
            type = InputType.Mouse;
            data.dx = (int)(x*34.2);
            data.dy = y*61;
            data.dw = 0;
            data.dwFlags = (MFLAGS)f;
            data.time = 0;
        }

        public MOUSE_INPUT( ConTrol.Wheels f, int w ) : this()
        {
            type = InputType.Mouse;
            data.dx = 0;
            data.dy = 0;
            data.dw = w;
            data.dwFlags = (MFLAGS)f;
            data.time = 0;
        }

        public MOUSE_INPUT( ConTrol.Button f ) : this()
        {
            MFLAGS action = (MFLAGS)((uint)(f &~ (ConTrol.Button.UP|ConTrol.Button.DOWN))
                                   * (uint)( 1 + (f & ConTrol.Button.UP ) ));
            type = InputType.Mouse;
            data.dx = 0;
            data.dy = 0;
            data.dw = 0;
            data.dwFlags = action;
            data.time = 0;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TYPED_INPUT
    {
        public UInt32     type;
        public KEYBDINPUT data;

        public TYPED_INPUT( ConTrol.Typed flags, char key ) : this()
        {
            type = InputType.Typed;
            data.wVk = (UInt16)key;
            data.wScan = 0;
            data.dwFlags = (KFLAGS)flags;
            data.time = 0;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CUSTOM_INPUT
    {
        public UInt32        type;
        public HARDWAREINPUT data;
    }

    public class ConTrol
    {
        private static int mX;
        private static int mY;

        public enum Move : uint
        {
            Relative = MFLAGS.MOVE,
            Absolute = MFLAGS.MOVE|MFLAGS.ABSOLUTE,
            VirtDesk = MFLAGS.MOVE|MFLAGS.ABSOLUTE|MFLAGS.VIRTUALDESK
        } 

        public enum Button : uint
        {
            CLICK = 0,
            UP = 0x00000001,
            DOWN = 0x00001000,
            L = MFLAGS.LEFTDOWN,
            R = MFLAGS.RIGHTDOWN,
            M = MFLAGS.MIDDLEDOWN,
            X = MFLAGS.XDOWN,
        }


        public enum Wheels : uint
        {
            WheelV = MFLAGS.WHEEL,
            WheelH = MFLAGS.HWHEEL
        }

        public enum Typed : uint 
        {
            KeyDown = 0,
            KeyUp = KFLAGS.KEYUP
        }

        [DllImport( "User32.dll", EntryPoint = "SendInput" )]
        private static extern UInt32 SendMouse( UInt32 cInputs, IntPtr pInputs, UInt32 cbSize );

        public static bool Mouse( Move flags, int x, int y )
        {
            MOUSE_INPUT data = new MOUSE_INPUT( flags, x, y );
            unsafe { return SendMouse(1, new IntPtr(&data), 40u) == 1; }
        }

        public static bool Click( Button flags )
        {
            uint count = 1;
            MOUSE_INPUT[] data;
            if( (flags & (Button.UP|Button.DOWN)) == 0 ) {
                data = new MOUSE_INPUT[2] {
                    new MOUSE_INPUT( flags|Button.DOWN ),
                    new MOUSE_INPUT( flags|Button.UP )
                };
                count = 2; 
            } else {
                data = new MOUSE_INPUT[1] {
                    new MOUSE_INPUT( flags )
                };
            }
            unsafe {
                fixed( MOUSE_INPUT* ptr = &data[0] ) {
                    uint size = (uint)sizeof(MOUSE_INPUT);
                    return SendMouse(count, new IntPtr(ptr), size) == count;
                }
            }
        }

        public static bool Click( Button flags, int atX, int atY )
        {
            uint count = 2;
            MOUSE_INPUT[] click;
            MOUSE_INPUT move = new MOUSE_INPUT( Move.Absolute, atX, atY );
            if( ( flags & ( Button.UP | Button.DOWN ) ) == 0 ) {
                click = new MOUSE_INPUT[3] { move,
                    new MOUSE_INPUT( flags|Button.DOWN ),
                    new MOUSE_INPUT( flags|Button.UP )
                };
                count = 3;
            } else {
                click = new MOUSE_INPUT[2] { move,
                    new MOUSE_INPUT( flags )
                };
            }
            unsafe {
                fixed( MOUSE_INPUT* ptr = &click[0] ) {
                    uint size = (uint)sizeof(MOUSE_INPUT);
                    return SendMouse(count, new IntPtr(ptr), size) == count;
                }
            }
        }

        public bool Wheel( Wheels flags, int turn )
        {
            MOUSE_INPUT ev = new MOUSE_INPUT( flags, turn );
            unsafe { return SendMouse(1, new IntPtr(&ev), 40u) == 1; }
        }

        public bool Wheel( Wheels flags, int turn, int atX, int atY )
        {
            MOUSE_INPUT[] evs = new MOUSE_INPUT[2]{
                new MOUSE_INPUT( ConTrol.Move.Absolute, atX, atY ),
                new MOUSE_INPUT( flags, turn )
            };
            unsafe { fixed( MOUSE_INPUT* ptr = &evs[0] ) {
                return SendMouse(2, new IntPtr(ptr), 40u) == 2; }
            }
        }

        public bool Type( char key )
        {
            TYPED_INPUT[] evs = new TYPED_INPUT[2] {
                new TYPED_INPUT( Typed.KeyDown, key ),
                new TYPED_INPUT( Typed.KeyUp, key )
            };
            unsafe { fixed( TYPED_INPUT* ptr = &evs[0] ) {
                uint size = (uint)sizeof(TYPED_INPUT);
                return SendMouse( 2, new IntPtr(ptr), size ) == 2;
            } }
        }

        public bool Type( string sequence )
        {
            uint count = (uint)(sequence.Length*2);
            char[] text = sequence.ToCharArray();
            TYPED_INPUT[] evs = new TYPED_INPUT[count];
            count = (uint)text.Length;
            for( int i = 0; i < count; ++i ) { int e = i*2;
                char c = text[i];
                evs[e] = new TYPED_INPUT( Typed.KeyDown, c );
                evs[e+1] = new TYPED_INPUT( Typed.KeyUp, c );
            } count *= 2;
            unsafe { fixed( TYPED_INPUT* ptr = &evs[0] ) {
                uint size = (uint)sizeof(TYPED_INPUT);
                return SendMouse( count, new IntPtr(ptr), size ) == count;
            } }
        }
    }
}
