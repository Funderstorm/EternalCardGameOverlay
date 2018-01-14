using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Tesseract;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace EternalDraftOverlay
{

    public partial class Overlay : Form
    {
        BackgroundWorker bw = new BackgroundWorker();

        private Dictionary<string, string> cardRankings;

        float scalingFactor = 8f;

        public static Overlay Instance { get; private set; }


        private Card[] Cards;
        private TesseractEngine ocrEngine;
        private Graphics formGraphics;
        private IntPtr EternalWindowPointer;
        private Point RescanButtonLocation;

        public Overlay(Dictionary<string, string> dict)
        {
            Instance = this;
            cardRankings = dict;
            scalingFactor = 4f;

            InitializeComponent();

            ocrEngine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);

            var p = Process.GetProcessesByName("Eternal").FirstOrDefault();
            if (p != null)
            {
                EternalWindowPointer = p.MainWindowHandle;

                RepositionForm();

                IntPtr hhook = SetWinEventHook(EVENT_SYSTEM_CAPTURESTART, EVENT_SYSTEM_MINIMIZEEND, IntPtr.Zero, EternalWindowDelegate, Convert.ToUInt32(p.Id), 0, WINEVENT_OUTOFCONTEXT);
            }

            RescanButtonLocation = new Point(50, 75);
            //MaximizeEverything();
            InitializeCardBoundaries();

            SetFormTransparent(this.Handle);
            SetTheLayeredWindowAttribute();
            formGraphics = this.CreateGraphics();

            ScanPage();
        }

        private void InitializeCardBoundaries()
        {
            RECT rect = new RECT();
            GetWindowRect(EternalWindowPointer, ref rect);
            var width = rect.Right - rect.Left;

            (int x, int y) cardFrameStartingLocation;

            (int x, int y) distanceToRanking;
            (int x, int y) distanceToCardName;

            (int down, int right) distanceBetweenCards;
            (int x, int y) cardNameSize;
            (int x, int y) totalCardSize;
            

            switch (width)
            {
                case 1286:
                    distanceToRanking = (45, 40);
                    distanceToCardName = (12, 134);
                    cardNameSize = (112, 10);
                    distanceBetweenCards = (234, 165);
                    totalCardSize = (134, 216);
                    cardFrameStartingLocation = (215, 77);
                    break;
                case 1926:
                    distanceToRanking = (0, 0);;
                    distanceToCardName = (0, 0);
                    distanceBetweenCards = (316, 223);
                    cardNameSize = (152, 16);
                    totalCardSize = (184, 288);
                    cardFrameStartingLocation = (388, 100);
                    break;
                case 160:
                    throw new Exception("Cant init card boundaries bc program is minimized");
                    break;
                default:
                    throw new Exception("Unsupported Window Size");
            }

            Cards = new Card[12];
            int k = 0;
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    // the location of the ranking graphic to display on top of the card
                    var rankingToDisplayX = cardFrameStartingLocation.x + (i * distanceBetweenCards.right) + distanceToRanking.x;
                    var rankingToDisplayY = cardFrameStartingLocation.y + (j * distanceBetweenCards.down) + distanceToRanking.y;

                    // the location of the card's text box
                    var textboxboundPositionX = cardFrameStartingLocation.x + (i * distanceBetweenCards.right) + distanceToCardName.x;
                    var textboxboundPositionY = cardFrameStartingLocation.y + (j * distanceBetweenCards.down) + distanceToCardName.y;

                    var textboxRectangle = new Rectangle(textboxboundPositionX, textboxboundPositionY, cardNameSize.x, cardNameSize.y);
                    var wholeCardRectangle = new Rectangle(
                        cardFrameStartingLocation.x + (i * distanceBetweenCards.right),
                        cardFrameStartingLocation.y + (j * distanceBetweenCards.down), 
                        totalCardSize.x, 
                        totalCardSize.y);

                    Cards[k] = new Card(string.Empty, new Point(rankingToDisplayX, rankingToDisplayY), textboxRectangle, wholeCardRectangle);
                    k++;
                }
            }
        }

        const uint EVENT_MIN = 0x00000001;
        const uint EVENT_MAX = 0x7FFFFFFF;
        const uint EVENT_SYSTEM_MOVESIZESTART = 0x000A;
        const uint EVENT_SYSTEM_MOVESIZEEND = 0x000B;
        const uint EVENT_SYSTEM_MINIMIZESTART = 0x0016;
        const uint EVENT_SYSTEM_MINIMIZEEND = 0x0017;

        const uint EVENT_OBJECT_INVOKED = 0x8013;
        const uint EVENT_SYSTEM_CAPTURESTART = 0x0008;

        const uint WINEVENT_OUTOFCONTEXT = 0;
        static WinEventDelegate EternalWindowDelegate = new WinEventDelegate(EternalWindowMessage);
        static void EternalWindowMessage(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            switch (eventType)
            {
                case EVENT_SYSTEM_MOVESIZEEND:
                    Instance.RepositionForm();
                    break;
                case EVENT_SYSTEM_MINIMIZESTART:
                    break;
                case EVENT_SYSTEM_MINIMIZEEND:
                    break;
                case EVENT_SYSTEM_CAPTURESTART:
                    if (Instance.CardIsSelected())
                        Instance.ClearAndRedrawPage();
                    if (Instance.RescanSelected())
                        Instance.ClearAndRedrawPage();
                    break;
            }
        }

        public bool CardIsSelected()
        {
            var mouse = Cursor.Position;

            foreach(var card in Cards)
            {
                if (card.WholeCardBounding.Contains(mouse.X - this.Location.X, mouse.Y - this.Location.Y))
                    return true;
            }

            return false;
        }

        public bool RescanSelected()
        {
            var mouse = Cursor.Position;

            if (new Rectangle(RescanButtonLocation, EternalDraftOverlay.Properties.Resources.RescanButton.Size).Contains(mouse.X - this.Location.X, mouse.Y - this.Location.Y))
                return true;

            return false;
        }

        public void ClearAndRedrawPage()
        {
            var blankImage = new Bitmap(85, 55, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(blankImage))
            {
                g.FillRectangle(Brushes.White, new Rectangle(0, 0, 85, 55));
            }

            for (int i = 0; i < 12; i++)
                formGraphics.DrawImage(blankImage, new Point(Cards[i].RankLocation.X, Cards[i].RankLocation.Y));

            // 500 is too little
            // 750 seems good
            // 1000 is a little much
            // wait here so game can render the next set of cards, maybe introduce a way to check up on it to see if we should proceed instead of guessing?
            System.Threading.Thread.Sleep(750);

            ScanPage();
        }

        public void ScanPage()
        {
            BackgroundWorker tmpBw = new BackgroundWorker();
            tmpBw.DoWork += new DoWorkEventHandler(bw_DEBUG_OCR);

            this.bw = tmpBw;

            this.bw.RunWorkerAsync();
        }

        public void RepositionForm()
        {
            RECT rect = new RECT();
            GetWindowRect(EternalWindowPointer, ref rect);

            this.Location = new Point(rect.Left, rect.Top);
            this.Size = new Size(rect.Right - rect.Left, rect.Bottom - rect.Top);

            SendMessage(this.Handle, WM_SYSCOMMAND, (UIntPtr)myWParam, (IntPtr)myLparam);
        }

        private void bw_DEBUG_OCR(object sender, DoWorkEventArgs e)
        {
            var watch = Stopwatch.StartNew();
            string processedTextResults = "";
            string preProcessedTextResults = "";
            int hits = 0;

            BackgroundWorker worker = sender as BackgroundWorker;

            Process[] processes = Process.GetProcessesByName("Eternal");

            Process p = processes.FirstOrDefault();
            IntPtr windowHandle;
            if (p != null)
            {
                windowHandle = p.MainWindowHandle;

                // difference is 20 ms between these two function
                //Image img22 = CaptureWindow(windowHandle);
                Pix img = CaptureWindowPix(windowHandle);

                img = img.ConvertRGBToGray(0.40f, 0.34f, 0.26f);

                //img = img.BinarizeOtsuAdaptiveThreshold(img.Width / 5, img.Height / 5, 10, 10, 0.1f);
                // img = img.BinarizeSauvolaTiled();
                //img = img.INVERT

                img = img.Scale(scalingFactor, scalingFactor);
                //img = img.BinarizeOtsuAdaptiveThreshold(img.Width / 5, img.Height / 5, 10, 10, 0.1f);

                //img = img.UNSHARPMASK
                //img = img.BinarizeOtsuAdaptiveThreshold(2000, 2000, 0, 0, 0.0f);
                //img = img.SELECTBYSIZE // removeNoise

                //var dpiX = 300;
                //var dpiY = 300;

                //Bitmap screenshotBitmap = PixConverter.ToBitmap(img);
                //screenshotBitmap.SetResolution(dpiX, dpiY);

                //DEBUG_PrintImage(img, "manualPreProcessing", watch);

                for (int i = 0; i < 12; i++)
                {
                    //formGraphics.DrawRectangle(new Pen(new SolidBrush(Color.Pink)), Cards[i].WholeCardBounding);
                    //formGraphics.DrawRectangle(new Pen(new SolidBrush(Color.Aqua)), Cards[i].TextboxBounding);
                    //formGraphics.DrawRectangle(new Pen(new SolidBrush(Color.Bisque)), Cards[i].RankLocation.X, Cards[i].RankLocation.Y, 10, 10);

                    Rect textbox_Scaled = new Rect(
                        Cards[i].TextboxBounding.X * (int)scalingFactor,
                        Cards[i].TextboxBounding.Y * (int)scalingFactor,
                        Cards[i].TextboxBounding.Width * (int)scalingFactor,
                        Cards[i].TextboxBounding.Height * (int)scalingFactor);

                    using (Page processedImage = ocrEngine.Process(img, textbox_Scaled))
                    {
                        DEBUG_PrintImage(processedImage.GetThresholdedImage(), "lower rez (individual)" + i, watch);

                        var text = processedImage.GetText();
                        preProcessedTextResults += text.Replace("\n", "") + Environment.NewLine;
                        text = CleanText(text);
                        processedTextResults += text + Environment.NewLine;
                        if (cardRankings.ContainsKey(text))
                        {
                            Cards[i].Rank = cardRankings[text];
                            hits++;
                        }
                        else if (!String.IsNullOrEmpty(text))
                        { 
                            List<SymSpell.suggestItem> suggestions = null;
                            suggestions = SymSpell.Lookup(text, "", SymSpell.editDistanceMax);
                            if (suggestions.Count > 0)
                            {
                                Cards[i].Rank = cardRankings[suggestions.First().term];
                                hits++;
                            }
                            else
                                Cards[i].Rank = "U";
                        }
                        else
                        {
                            Cards[i].Rank = string.Empty;
                        }
                    }
                }

                RenderRankings();
            }

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            OutputTestResults(elapsedMs, processedTextResults, hits, preProcessedTextResults);
        }

        private string CleanText(string input)
        {
            input = input.ToLower();

            var sb = new System.Text.StringBuilder();
            var acceptableCharacters = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
            foreach (var c in input)
            {
                if (Array.IndexOf(acceptableCharacters, c) >= 0)
                    sb.Append(c);
            }

            return sb.ToString();
        }

        private void RenderRankings()
        {
            for (int i = 0; i < 12; i++)
                DrawRanking(Cards[i].Rank, Cards[i].RankLocation.X, Cards[i].RankLocation.Y);
        }

        private void DrawRanking(string rank, int x, int y)
        {
            formGraphics.DrawImage(EternalDraftOverlay.Properties.Resources.RescanButton, RescanButtonLocation);
            Image img;
            switch (rank)
            {
                case "S":
                    img = EternalDraftOverlay.Properties.Resources.Ranking_S_Final;
                    break;
                case "A":
                    img = EternalDraftOverlay.Properties.Resources.Ranking_A_Final;
                    break;
                case "A+":
                    img = EternalDraftOverlay.Properties.Resources.Ranking_AP_Final;
                    break;
                case "A-":
                    img = EternalDraftOverlay.Properties.Resources.Ranking_AM_Final;
                    break; ;
                case "B":
                    img = EternalDraftOverlay.Properties.Resources.Ranking_B_Final;
                    break;
                case "B+":
                    img = EternalDraftOverlay.Properties.Resources.Ranking_BP_Final;
                    break;
                case "B-":
                    img = EternalDraftOverlay.Properties.Resources.Ranking_BM_Final;
                    break;
                case "C":
                    img = EternalDraftOverlay.Properties.Resources.Ranking_C_Final;
                    break;
                case "C+":
                    img = EternalDraftOverlay.Properties.Resources.Ranking_CP_Final;
                    break;
                case "C-":
                    img = EternalDraftOverlay.Properties.Resources.Ranking_CM_Final;
                    break;
                case "D":
                    img = EternalDraftOverlay.Properties.Resources.Ranking_D_Final;
                    break;
                case "F":
                    img = EternalDraftOverlay.Properties.Resources.Ranking_F_Final;
                    break;
                case "U":
                    img = EternalDraftOverlay.Properties.Resources.Ranking_U_Final;
                    break;
                default:
                    img = null;
                    break;
            }

            

            if (img != null)
                formGraphics.DrawImage(img, new Point(x, y));
            else if (!String.IsNullOrEmpty(rank))
            {
                formGraphics.DrawString(rank, new Font("Arial", 32), new SolidBrush(Color.Red), new Point(x, y));
            }
        }

        private void OutputTestResults(long time, string results, int hits, string preproccessedResults = null)
        {
            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(@"../../PerformanceTestData/Results_" + DateTime.Now.Ticks + "_" + hits + "_12.txt", true))
            {
                string output = "Executed Time: " + DateTime.Now + Environment.NewLine;
                output += "Notes: " + "no binarize and yes resolution" + Environment.NewLine;
                output += "Time: " + time + " ms or " + ((float)time / 1000f).ToString() + " s Accuracy: " + hits + " / 12" + Environment.NewLine;
                output += "Results:" + Environment.NewLine;
                output += results + Environment.NewLine;
                output += preproccessedResults ?? "" + Environment.NewLine;

                file.Write(output);
                Console.Write(output);
            }
        }

        private void DEBUG_PrintImage(Pix image, string name, Stopwatch watch)
        {
            watch.Stop();

            image.Save(@"../../PerformanceTestData/" + name + ".bmp");

            watch.Start();
        }

        private void DEBUG_PrintImage(Bitmap image, string name, Stopwatch watch)
        {
            watch.Stop();

            image.Save(@"../../PerformanceTestData/" + name + ".bmp");

            watch.Start();
        }

        private void DEBUG_PrintImage(Image image, string name, Stopwatch watch)
        {
            watch.Stop();

            image.Save(@"../../PerformanceTestData/" + name + ".bmp");

            watch.Start();
        }

        
        /// <summary>
        /// Creates an Image object containing a screen shot of a specific window
        /// </summary>
        /// <param name="handle">The handle to the window. (In windows forms, this is obtained by the Handle property)</param>
        /// <returns></returns>
        public Image CaptureWindow(IntPtr handle)
        {
            // get te hDC of the target window
            IntPtr hdcSrc = User32.GetWindowDC(handle);
            // get the size
            User32.RECT windowRect = new User32.RECT();
            User32.GetWindowRect(handle, ref windowRect);
            int width = windowRect.right - windowRect.left;
            int height = windowRect.bottom - windowRect.top;
            // create a device context we can copy to
            IntPtr hdcDest = GDI32.CreateCompatibleDC(hdcSrc);
            // create a bitmap we can copy it to,
            // using GetDeviceCaps to get the width/height
            IntPtr hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc, width, height);
            // select the bitmap object
            IntPtr hOld = GDI32.SelectObject(hdcDest, hBitmap);
            // bitblt over
            GDI32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, GDI32.SRCCOPY);
            // restore selection
            GDI32.SelectObject(hdcDest, hOld);
            // clean up 
            GDI32.DeleteDC(hdcDest);
            User32.ReleaseDC(handle, hdcSrc);
            // get a .NET image object for it
            Image img = Image.FromHbitmap(hBitmap);
            // free up the Bitmap object
            GDI32.DeleteObject(hBitmap);
            return img;
        }

        public Pix CaptureWindowPix(IntPtr handle)
        {
            // get te hDC of the target window
            IntPtr hdcSrc = User32.GetWindowDC(handle);
            // get the size
            User32.RECT windowRect = new User32.RECT();
            User32.GetWindowRect(handle, ref windowRect);
            int width = windowRect.right - windowRect.left;
            int height = windowRect.bottom - windowRect.top;
            // create a device context we can copy to
            IntPtr hdcDest = GDI32.CreateCompatibleDC(hdcSrc);
            // create a bitmap we can copy it to,
            // using GetDeviceCaps to get the width/height
            IntPtr hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc, width, height);
            // select the bitmap object
            IntPtr hOld = GDI32.SelectObject(hdcDest, hBitmap);
            // bitblt over
            GDI32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, GDI32.SRCCOPY);
            // restore selection
            GDI32.SelectObject(hdcDest, hOld);
            // clean up 
            GDI32.DeleteDC(hdcDest);
            User32.ReleaseDC(handle, hdcSrc);
            // get a .NET image object for it
            //Image img = Image.FromHbitmap(hBitmap);

            int bitsPerPixel = ((int)PixelFormat.Format32bppArgb & 0xff00) >> 8;
            int bytesPerPixel = (bitsPerPixel + 7) / 8;
            int stride = 4 * ((width * bytesPerPixel + 3) / 4);

            //Bitmap intermediate = new Bitmap(width, height, stride, PixelFormat.Format32bppArgb, hBitmap);
            //Pix img = PixConverter.ToPix(intermediate);

            Bitmap orig = Image.FromHbitmap(hBitmap);
            Bitmap clone = new Bitmap(orig.Width, orig.Height, PixelFormat.Format32bppArgb);

            using (Graphics gr = Graphics.FromImage(clone))
            {
                gr.DrawImage(orig, new Rectangle(0, 0, clone.Width, clone.Height));
            }
            orig.Dispose();

            Pix img = PixConverter.ToPix(clone);

            //Pix img = PixConverter.ToPix(Image.FromHbitmap(hBitmap));
            // free up the Bitmap object
            GDI32.DeleteObject(hBitmap);
            return img;
        }

        #region Helper Functions

        private void MaximizeEverything()
        {
            this.Location = getTopLeft();
            this.Size = getFullScreensSize();

            SendMessage(this.Handle, WM_SYSCOMMAND, (UIntPtr)myWParam, (IntPtr)myLparam);
        }

        /// <summary>
        /// Make the form (specified by its handle) a window that supports transparency.
        /// </summary>
        /// <param name="Handle">The window to make transparency supporting</param>
        public void SetFormTransparent(IntPtr Handle)
        {
            oldWindowLong = GetWindowLong(Handle, (int)GetWindowLongConst.GWL_EXSTYLE);
            SetWindowLong(Handle, (int)GetWindowLongConst.GWL_EXSTYLE, Convert.ToInt32(oldWindowLong | (uint)WindowStyles.WS_EX_LAYERED | (uint)WindowStyles.WS_EX_TRANSPARENT));
        }

        /// <summary>
        /// Make the form (specified by its handle) a normal type of window (doesn't support transparency).
        /// </summary>
        /// <param name="Handle">The Window to make normal</param>
        public void SetFormNormal(IntPtr Handle)
        {
            SetWindowLong(Handle, (int)GetWindowLongConst.GWL_EXSTYLE, Convert.ToInt32(oldWindowLong | (uint)WindowStyles.WS_EX_LAYERED));
        }

        /// <summary>
        /// Makes the form change White to Transparent and clickthrough-able
        /// Can be modified to make the form translucent (with different opacities) and change the Transparency Color.
        /// </summary>
        public void SetTheLayeredWindowAttribute()
        {
            uint transparentColor = 0xffffffff;

            SetLayeredWindowAttributes(this.Handle, transparentColor, 125, 0x2);

            this.TransparencyKey = Color.White;
        }

        /// <summary>
        /// Finds the Size of all computer screens combined (assumes screens are left to right, not above and below).
        /// </summary>
        /// <returns>The width and height of all screens combined</returns>
        public static Size getFullScreensSize()
        {
            int height = int.MinValue;
            int width = 0;

            foreach (Screen screen in System.Windows.Forms.Screen.AllScreens)
            {
                //take largest height
                height = Math.Max(screen.WorkingArea.Height, height);

                width += screen.Bounds.Width;
            }

            return new Size(width, height);
        }

        /// <summary>
        /// Finds the top left pixel position (with multiple screens this is often not 0,0)
        /// </summary>
        /// <returns>Position of top left pixel</returns>
        public static Point getTopLeft()
        {
            int minX = int.MaxValue;
            int minY = int.MaxValue;

            foreach (Screen screen in System.Windows.Forms.Screen.AllScreens)
            {
                minX = Math.Min(screen.WorkingArea.Left, minX);
                minY = Math.Min(screen.WorkingArea.Top, minY);
            }

            return new Point(minX, minY);
        }
        #endregion

        #region Windows Code

        delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        /// <summary>
        /// Helper class containing User32 API functions
        /// </summary>
        private class User32
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct RECT
            {
                public int left;
                public int top;
                public int right;
                public int bottom;
            }
            [DllImport("user32.dll")]
            public static extern IntPtr GetDesktopWindow();
            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowDC(IntPtr hWnd);
            [DllImport("user32.dll")]
            public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);
            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowRect(IntPtr hWnd, ref RECT rect);
        }

        /// <summary>
        /// Helper class containing Gdi32 API functions
        /// </summary>
        private class GDI32
        {

            public const int SRCCOPY = 0x00CC0020; // BitBlt dwRop parameter
            [DllImport("gdi32.dll")]
            public static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest,
                int nWidth, int nHeight, IntPtr hObjectSource,
                int nXSrc, int nYSrc, int dwRop);
            [DllImport("gdi32.dll")]
            public static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth,
                int nHeight);
            [DllImport("gdi32.dll")]
            public static extern IntPtr CreateCompatibleDC(IntPtr hDC);
            [DllImport("gdi32.dll")]
            public static extern bool DeleteDC(IntPtr hDC);
            [DllImport("gdi32.dll")]
            public static extern bool DeleteObject(IntPtr hObject);
            [DllImport("gdi32.dll")]
            public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

        [DllImport("user32.dll")]
        static extern int SendMessage(IntPtr hWnd, uint wMsg, UIntPtr wParam, IntPtr lParam); //used for maximizing the screen

        const int WM_SYSCOMMAND = 0x0112; //used for maximizing the screen.
        const int myWParam = 0xf120; //used for maximizing the screen.
        const int myLparam = 0x5073d; //used for maximizing the screen.

        int oldWindowLong;

        [Flags]
        enum WindowStyles : uint
        {
            WS_OVERLAPPED = 0x00000000,
            WS_POPUP = 0x80000000,
            WS_CHILD = 0x40000000,
            WS_MINIMIZE = 0x20000000,
            WS_VISIBLE = 0x10000000,
            WS_DISABLED = 0x08000000,
            WS_CLIPSIBLINGS = 0x04000000,
            WS_CLIPCHILDREN = 0x02000000,
            WS_MAXIMIZE = 0x01000000,
            WS_BORDER = 0x00800000,
            WS_DLGFRAME = 0x00400000,
            WS_VSCROLL = 0x00200000,
            WS_HSCROLL = 0x00100000,
            WS_SYSMENU = 0x00080000,
            WS_THICKFRAME = 0x00040000,
            WS_GROUP = 0x00020000,
            WS_TABSTOP = 0x00010000,

            WS_MINIMIZEBOX = 0x00020000,
            WS_MAXIMIZEBOX = 0x00010000,

            WS_CAPTION = WS_BORDER | WS_DLGFRAME,
            WS_TILED = WS_OVERLAPPED,
            WS_ICONIC = WS_MINIMIZE,
            WS_SIZEBOX = WS_THICKFRAME,
            WS_TILEDWINDOW = WS_OVERLAPPEDWINDOW,

            WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,
            WS_POPUPWINDOW = WS_POPUP | WS_BORDER | WS_SYSMENU,
            WS_CHILDWINDOW = WS_CHILD,

            //Extended Window Styles

            WS_EX_DLGMODALFRAME = 0x00000001,
            WS_EX_NOPARENTNOTIFY = 0x00000004,
            WS_EX_TOPMOST = 0x00000008,
            WS_EX_ACCEPTFILES = 0x00000010,
            WS_EX_TRANSPARENT = 0x00000020,

            //#if(WINVER >= 0x0400)

            WS_EX_MDICHILD = 0x00000040,
            WS_EX_TOOLWINDOW = 0x00000080,
            WS_EX_WINDOWEDGE = 0x00000100,
            WS_EX_CLIENTEDGE = 0x00000200,
            WS_EX_CONTEXTHELP = 0x00000400,

            WS_EX_RIGHT = 0x00001000,
            WS_EX_LEFT = 0x00000000,
            WS_EX_RTLREADING = 0x00002000,
            WS_EX_LTRREADING = 0x00000000,
            WS_EX_LEFTSCROLLBAR = 0x00004000,
            WS_EX_RIGHTSCROLLBAR = 0x00000000,

            WS_EX_CONTROLPARENT = 0x00010000,
            WS_EX_STATICEDGE = 0x00020000,
            WS_EX_APPWINDOW = 0x00040000,

            WS_EX_OVERLAPPEDWINDOW = (WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE),
            WS_EX_PALETTEWINDOW = (WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST),
            //#endif /* WINVER >= 0x0400 */

            //#if(WIN32WINNT >= 0x0500)

            WS_EX_LAYERED = 0x00080000,
            //#endif /* WIN32WINNT >= 0x0500 */

            //#if(WINVER >= 0x0500)

            WS_EX_NOINHERITLAYOUT = 0x00100000, // Disable inheritence of mirroring by children
            WS_EX_LAYOUTRTL = 0x00400000, // Right to left mirroring
            //#endif /* WINVER >= 0x0500 */

            //#if(WIN32WINNT >= 0x0500)

            WS_EX_COMPOSITED = 0x02000000,
            WS_EX_NOACTIVATE = 0x08000000
            //#endif /* WIN32WINNT >= 0x0500 */

        }

        public enum GetWindowLongConst
        {
            GWL_WNDPROC = (-4),
            GWL_HINSTANCE = (-6),
            GWL_HWNDPARENT = (-8),
            GWL_STYLE = (-16),
            GWL_EXSTYLE = (-20),
            GWL_USERDATA = (-21),
            GWL_ID = (-12)
        }

        public enum LWA
        {
            ColorKey = 0x1,
            Alpha = 0x2,
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);


        #endregion
    }
}