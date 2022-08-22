using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows;

namespace Consola.Test
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct Area
    {
        public static readonly Area Zero = new Area();
        public static readonly Area Empty = new Area(-1,-1,-1,-1);

        [FieldOffset(0)]
        public UInt64 data;
        [FieldOffset(0)]
        public ConTrol.Point Point;
        [FieldOffset(4)]
        public ConTrol.Point Size;

        public Area( ConTrol.Point size ) : this()
        {
            Size.data = size.data;
        }

        public Area( int w, int h ) : this()
        {
            Size.X = (ushort)w;
            Size.Y = (ushort)h;
        }

        public Area( int x, int y, int w, int h )
            : this(w, h)
        {
            Point.X = (ushort)x;
            Point.Y = (ushort)y;
        }

        public Area( ConTrol.Point pos, ConTrol.Point siz ) : this()
        {
            Point.data = pos.data;
            Size.data = siz.data;
        }

        public Area At( ConTrol.Point position )
        {
            return new Area(position, Size);
        }

        public ConTrol.Point Center {
            get {
                ConTrol.Point p = Point;
                p.X += (ushort)( Size.X / 2 );
                p.Y += (ushort)( Size.Y / 2 );
                return p;
            }
            set {
                Point.X = (ushort)( value.X - Size.X / 2 );
                Point.Y = (ushort)( value.Y - Size.Y / 2 );
            }
        }
    }

    /// <summary>
    /// Test Suite base clase for GUI tests on window based applications.
    /// </summary>
    /// <typeparam name="T">T should be any class which provides access
    /// to the AUTs accessible GUI controls and/or its internal api.
    /// 
    /// - for a WindowsForms applications the MainForm class would be
    ///   appropriate for example. 
    /// 
    /// - for a WPF application the MainWindow class. 
    /// 
    /// - other solutions like classes which offer access to the AUT
    ///   via some kind of remote protocol may be practicable,.. 
    /// 
    /// If using a Form or Window class accessing it from another thread
    /// will get a little bit tricky (most controls deny being accessed
    /// from another thread. just primitive value variable can be used)..
    /// </typeparam>
    public abstract class Suite<T> : Test where T : class
    {
        // class which provides access to the AUT's internal api
        protected T Aut;

        // screen coordinates of the AUTs mainwindow rectangle
        protected Area Win;

        // must implement retrieving screencoordinates of the AUT window
        abstract protected Area GetWindowArea();

        // must implement retrieving distinct, testrelevant locations on the AUT.
        // like getting a Control element by name: 'descriptor' then could be
        // interpreted as string and the function then shall use return global screen
        // coordinates of the named control then - Control Bounds properties as
        // local coordinate rectangles are accessible from other threads also.
        // but global bounds are not accessible from other threads. GetTranslated()
        // (already pre-implemented) can be used inside implementation then for
        // doing translation of the requested control elements local coordinates
        // to needed global bounds then.
        abstract protected Area GetScreenArea( object descriptor );


        // must implement retreiving global coordinates of items on a menu strip
        // it should be implemented so accessing menus and as well submenu items
        // (via using '.' separated 'paths' as menupath parameter for exampe. )
        abstract protected ConTrol.Point GetMenuPoint( string menupath );

        /// <summary>
        /// GetTranslated(ConTrol.Point local) returns the given local coordinate
        /// translated to global (screen) coordinates </summary>
        /// <param name="local">local control coordinates</param>
        /// <returns>global screen coordinates</returns>
        protected ConTrol.Point GetTranslated( ConTrol.Point local )
        {
            return local + Win.Point;
        }

        /// <summary>
        /// GetXButton() returns global screen coordinate of the Windows 'X' button
        /// - which (for regular cases) should close the application when clicked
        /// </summary>
        /// <returns>clickable point in global coordinates</returns>
        virtual protected ConTrol.Point GetXButton()
        {
            Win = GetWindowArea();
            return new ConTrol.Point( ( Win.Point.X+Win.Size.X)-20, Win.Point.Y-10 );
        }

        public Suite( T aut, bool logall, bool logxml )
            : base(logall, logxml)
        {
            Aut = aut;
            Win = GetWindowArea();
        }

        protected override void OnCleanUp()
        {
            // auto close the AUT on test end by cllicking 'X'
            ConTrol.Click( ConTrol.Button.L, GetXButton() );
        }
    }

    public class Runner<A,T>
    where T
        : Suite<A>
    where A
        : class
    {
        private Suite<A> tst;
        private Thread   run;
        private int      prc;
        private ParameterizedThreadStart runner;

        public Runner( T suite )
        : this(suite, ApartmentState.STA)
        {
        }

        public Runner( T suite, ApartmentState StateOfTheAut )
        {
            prc = -1;
            tst = suite;
            runner = new ParameterizedThreadStart(testrun);
            run = new Thread(runner);
            run.SetApartmentState(StateOfTheAut);
        }

        public Runner<A, T> Start()
        {
            if( Completed ) Reset();
            run.Start(tst);
            return this;
        }

        private void testrun( object arg )
        {
            prc = Thread.GetCurrentProcessorId();
            Suite<A> tst = arg as Suite<A>;
            if( tst != null )
                tst.Run();
        }

        public void Abort()
        {
            if( run.ThreadState == ThreadState.Running ) {
                run.Abort();
                Thread.Sleep(10);
                prc = -1;
            }
        }

        public int ProcessorId()
        {
            return prc;
        }

        public void Reset()
        {
            ApartmentState ap = run.GetApartmentState();
            if( run.ThreadState == ThreadState.Running ) {
                run.Abort();
                Thread.Sleep(10);
            }
            if( run.ThreadState == ThreadState.Stopped ) {
                run = new Thread(runner);
                run.SetApartmentState(ap);
                prc = -1;
            }
        }

        public bool Completed {
            get { return run.ThreadState == ThreadState.Stopped; }
        }
        public Suite<A> GetResult()
        {
            while( run.ThreadState == ThreadState.Running ) {
                Thread.Sleep(100);
            }
            return tst;
        }

        public static Runner<A,T> CreateTestRunner( T SuiteInstance )
        {
            return CreateTestRunner(SuiteInstance, ApartmentState.STA);
        }

        public static Runner<A,T> CreateTestRunner( T SuiteInstance, ApartmentState StateOfTheAut )
        {
            return new Runner<A, T>(SuiteInstance, StateOfTheAut);
        }
    }
}
