using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Xml;

namespace Consola.Test
{
    internal struct InputType
    {
        public const uint Mouse = 0;
        public const uint Typed = 1;
        public const uint Other = 2;
    }

    [Flags]
    internal enum MFLAGS : uint {
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

    [Flags]
    internal enum TFLAGS : uint
    {
        MOVE          = 0x00000241,
        DOWN          = 0x00000242,
        LIFT          = 0x00000243,
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    internal struct HARDWAREINPUT
    {
        public TFLAGS uMsg;
        public UInt16 wParamL;
        public UInt16 wParamH;
    }




    [StructLayout(LayoutKind.Sequential)]
    internal struct MOUSE_INPUT
    {
        public UInt32     type;
        public MOUSEINPUT data;
        public MOUSE_INPUT( ConTrol.Move f, int x, int y )
            : this()
        {
            type = InputType.Mouse;
            data.dx = (int)(x*ConTrol.SizeOf.SCALE.X);
            data.dy = (int)(y*ConTrol.SizeOf.SCALE.Y);
            data.dw = 0;
            data.dwFlags = (MFLAGS)f;
            data.time = 0;
        }

        public MOUSE_INPUT( ConTrol.Wheels f, int w )
            : this()
        {
            type = InputType.Mouse;
            data.dx = 0;
            data.dy = 0;
            data.dw = w;
            data.dwFlags = (MFLAGS)f;
            data.time = 0;
        }

        public MOUSE_INPUT( ConTrol.Button f ) 
            : this()
        {
            type = InputType.Mouse;
            data.dwFlags = (MFLAGS)( (uint)( f &~ (ConTrol.Button.UP|ConTrol.Button.DOWN) )
                                   * (uint)( 1 + (f & ConTrol.Button.UP ) ) );
            data.dx = 0;
            data.dy = 0;
            data.dw = 0;
            data.time = 0;
        }
    }

    [StructLayout(LayoutKind.Sequential,Size=8)]
    internal struct ScreenScale
    {
        public float X;
        public float Y;
        public ScreenScale( ConTrol.Point screen )
        {
            X = ( 65535.0f / screen.X );
            Y = ( 65535.0f / screen.Y );
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TYPED_INPUT
    {
        public UInt32     type;
        public KEYBDINPUT data;

        public TYPED_INPUT( ConTrol.Keys flags, char key )
            : this()
        {
            type = InputType.Typed;
            data.wVk = (UInt16)key;
            data.wScan = 0;
            data.dwFlags = (KFLAGS)flags;
            data.time = 0;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TOUCH_INPUT
    {
        public UInt32        type;
        public HARDWAREINPUT data;

        public TOUCH_INPUT( TFLAGS flags, int x, int y )
        {
            type = InputType.Other;
            data.uMsg = flags;
            data.wParamL = (ushort)( ConTrol.SizeOf.SCALE.X * x );
            data.wParamH = (ushort)( ConTrol.SizeOf.SCALE.Y * y );
        }
    }

    public class ConTrol
    {
        [StructLayout(LayoutKind.Explicit,Size = 4)]
        public struct Point
        {
            [FieldOffset(0)]
            public UInt32 data;
            [FieldOffset(0)]
            public UInt16 X;
            [FieldOffset(2)]
            public UInt16 Y;

            public Point( int x, int y ) : this()
            {
                X = (ushort)x;
                Y = (ushort)y;
            }

            public static Point operator +( Point This, Point That )
            {
                This.X += That.X;
                This.Y += That.Y;
                return This;
            }

            public static Point operator -( Point This, Point That )
            {
                This.X -= That.X;
                This.Y -= That.Y;
                return This;
            }

            public static implicit operator ValueTuple<short,short>( Point cast )
            {
                return new ValueTuple<short,short>((short)cast.X, (short)cast.Y);
            }

            public static implicit operator Point( ValueTuple<short,short> cast )
            {
                return new Point( cast.Item1, cast.Item2 );
            }
        }

        public struct SizeOf
        {
            internal static readonly UInt32 MOUSE_DATA = (uint)Marshal.SizeOf<MOUSE_INPUT>();
            internal static readonly UInt32 TYPED_DATA = (uint)Marshal.SizeOf<TYPED_INPUT>();
            internal static readonly ScreenScale SCALE = ConTrol.GetScreenScale();
            public static readonly ConTrol.Point Screen = ConTrol.GetScreenSize();
        }

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

        public enum Keys : uint 
        {
            TYPE = 0,
            DOWN = 1,
            UP = KFLAGS.KEYUP
        }

        [DllImport( "User32.dll", EntryPoint = "GetSystemMetrics" )]
        private static extern Int32 GetSystemMetrics( int metricsrequestid );

        [DllImport( "User32.dll", EntryPoint = "SendInput" )]
        private static extern UInt32 SendInput( UInt32 cInputs, IntPtr pInputs, UInt32 cbSize );


        internal static Point GetScreenSize()
        {
            return new Point( GetSystemMetrics(0), GetSystemMetrics(1) );
        }

        internal static ScreenScale GetScreenScale()
        {
            return new ScreenScale( GetScreenSize() );
        }

        public static bool Mouse( Move flags, ValueTuple<short,short> point )
        {
            return Mouse( flags, point.Item1, point.Item2 );
        }

        public static bool Mouse( Move flags, int x, int y )
        {
            MOUSE_INPUT data = new MOUSE_INPUT( flags, x, y );
            unsafe { return SendInput( 1, new IntPtr(&data), SizeOf.MOUSE_DATA ) == 1; }
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
                    return SendInput( count, new IntPtr(ptr), SizeOf.MOUSE_DATA ) == count;
                }
            }
        }

        public static bool Click( Button flags, int atX, int atY )
        {
            uint count = 2;
            MOUSE_INPUT[] click;
            if( ( flags & ( Button.UP | Button.DOWN ) ) == 0 ) {
                click = new MOUSE_INPUT[3] {
                    new MOUSE_INPUT( Move.Absolute, atX, atY ),
                    new MOUSE_INPUT( flags|Button.DOWN ),
                    new MOUSE_INPUT( flags|Button.UP )
                };
                count = 3;
            } else {
                click = new MOUSE_INPUT[2] { 
                    new MOUSE_INPUT( Move.Absolute, atX, atY ),
                    new MOUSE_INPUT( flags )
                };
            } unsafe {
                fixed( MOUSE_INPUT* ptr = &click[0] ) {
                    return SendInput( count, new IntPtr(ptr), SizeOf.MOUSE_DATA ) == count;
                }
            }
        }

        public static bool Click( Button flags, ValueTuple<short,short> ixyps )
        {
            return Click( flags, ixyps.Item1, ixyps.Item2 );
        }

        public static bool Click( Button flags, ConTrol.Point point )
        {
            return Click( flags, point.X, point.Y );
        }

        public static bool Wheel( Wheels flags, int turn )
        {
            MOUSE_INPUT ev = new MOUSE_INPUT( flags, turn );
            unsafe { return SendInput( 1, new IntPtr(&ev), SizeOf.MOUSE_DATA ) == 1; }
        }

        public static bool Wheel( Wheels flags, int turn, int atX, int atY )
        {
            MOUSE_INPUT[] evs = new MOUSE_INPUT[2] {
                new MOUSE_INPUT( ConTrol.Move.Absolute, atX, atY ),
                new MOUSE_INPUT( flags, turn )
            };
            unsafe { fixed( MOUSE_INPUT* ptr = &evs[0] ) {
                return SendInput( 2, new IntPtr(ptr), SizeOf.MOUSE_DATA ) == 2; }
            }
        }

        public static bool Wheel( Wheels flags, int turn, ConTrol.Point point )
        {
            return Wheel( flags, turn, point.X, point.Y );
        }


        public static bool Key( Keys flags, char key )
        {
            uint count;
            TYPED_INPUT[] eingabe;
            if( ( flags & ( Keys.UP | Keys.DOWN ) ) == 0 ) {
                eingabe = new TYPED_INPUT[2] {
                    new TYPED_INPUT( flags, key ),
                    new TYPED_INPUT( flags|Keys.UP, key )
                }; count = 2;
            } else {
                eingabe = new TYPED_INPUT[1] {
                    new TYPED_INPUT( flags & ~Keys.DOWN, key )
                }; count = 1;
            }
            unsafe { fixed( TYPED_INPUT* pt = &eingabe[0] ) {
                return SendInput( count, new IntPtr(pt),
                                  SizeOf.TYPED_DATA ) == count;
            } }
        }

        public static bool Type( string sequence )
        {
            uint count = (uint)(sequence.Length*2);
            char[] text = sequence.ToCharArray();
            TYPED_INPUT[] evs = new TYPED_INPUT[count];
            count = (uint)text.Length;
            for( int i = 0; i < count; ++i ) { int e = i+i;
                char c = text[i];
                evs[e] = new TYPED_INPUT( Keys.TYPE, c );
                evs[e+1] = new TYPED_INPUT( Keys.UP, c );
            } count += count;
            unsafe { fixed( TYPED_INPUT* ptr = &evs[0] ) {
                uint size = (uint)sizeof(TYPED_INPUT);
                return SendInput( count, new IntPtr(ptr), 
                                  SizeOf.TYPED_DATA ) == count;
            } }
        }
    }
}
