using System;
using System.Threading;
using IctBaden.RasPi.Comm;

namespace IctBaden.RasPi.Display
{
    /// <summary>
    /// SainSmart IIC LCD1602 Module Display
    /// I2C address 0x27
    ///
    /// signal assignment 
    ///        LCD - CHIP
    ///         RS - P0
    ///         RW - P1
    ///         EN - P2
    ///         BL - P3
    ///         D4 - P4
    ///         D5 - P5
    ///         D6 - P6
    ///         D7 - P7
    /// </summary>
    public class CharacterDisplayI2C : ICharacterDisplay
    {
        // ReSharper disable UnusedMember.Local
        private const byte LcdRs = 0x01;
        private const byte LcdRw = 0x02;
        private const byte LcdEn = 0x04;
        private const byte LcdBl = 0x08;
        // ReSharper restore UnusedMember.Local

        private bool _backlight;
        public bool Backlight
        {
            get => _backlight;
            set
            {
                _backlight = value;
                WriteNibble(0);
            }
        }
        public int Lines => 2;
        public int Columns => 16;

        // ReSharper disable once InconsistentNaming
        private readonly I2C i2c;

        public CharacterDisplayI2C()
        {
            _backlight = true;
            i2c = new I2C();
        }

        public bool Open(string deviceName, int address)
        {
            if (!i2c.Open(deviceName, address))
            {
                return false;
            }

            Initialize();
            return true;
        }

        private void Initialize()
        {
            WriteNibble(0x00);
            Thread.Sleep(20);
            WriteNibble(0x30);    // Interface auf 4-Bit setzen
            Thread.Sleep(5);
            WriteNibble(0x30);    // Interface auf 4-Bit setzen
            Thread.Sleep(5);
            WriteNibble(0x30);    // Interface auf 4-Bit setzen
            Thread.Sleep(1);
            WriteNibble(0x20);
            Thread.Sleep(1);

            WriteCmd(0x28);    // 2-zeilig, 5x8-Punkt-Matrix
            WriteCmd(0x06);    // Kursor nach rechts wandernd, kein Display shift
            WriteCmd(0x14);    // Cursor/Display-Shift
            Clear();
            WriteCmd(0x0C);    // Display ein

            // charcter set
            // Ä
            WriteCmd(0x40 + 0);
            WriteData(0x11);
            WriteData(0x0E);
            WriteData(0x11);
            WriteData(0x11);
            WriteData(0x1F);
            WriteData(0x11);
            WriteData(0x11);
            WriteData(0x00);
            // Ö
            WriteData(0x11);
            WriteData(0x0E);
            WriteData(0x11);
            WriteData(0x11);
            WriteData(0x11);
            WriteData(0x11);
            WriteData(0x0E);
            WriteData(0x00);
            // Ü
            WriteData(0x11);
            WriteData(0x00);
            WriteData(0x11);
            WriteData(0x11);
            WriteData(0x11);
            WriteData(0x11);
            WriteData(0x0E);
            WriteData(0x00);
            // g
            WriteData(0x00);
            WriteData(0x00);
            WriteData(0x0F);
            WriteData(0x11);
            WriteData(0x11);
            WriteData(0x0F);
            WriteData(0x01);
            WriteData(0x1E);
            // j
            WriteData(0x02);
            WriteData(0x00);
            WriteData(0x06);
            WriteData(0x02);
            WriteData(0x02);
            WriteData(0x02);
            WriteData(0x12);
            WriteData(0x0C);
            // p
            WriteData(0x00);
            WriteData(0x00);
            WriteData(0x1E);
            WriteData(0x11);
            WriteData(0x11);
            WriteData(0x1E);
            WriteData(0x10);
            WriteData(0x10);
            // q
            WriteData(0x00);
            WriteData(0x00);
            WriteData(0x0F);
            WriteData(0x11);
            WriteData(0x11);
            WriteData(0x0F);
            WriteData(0x01);
            WriteData(0x01);
            // y
            WriteData(0x00);
            WriteData(0x00);
            WriteData(0x11);
            WriteData(0x11);
            WriteData(0x11);
            WriteData(0x0F);
            WriteData(0x01);
            WriteData(0x0E);

            WriteCmd(0x80);
        }

        public void Clear()
        {
            WriteCmd(0x01);    // Display löschen 
            Thread.Sleep(20);
        }

        public void SetContrast(int contrast)
        {
            // not supported
        }

        public void SetCursor(int col, int row)
        {
            if ((col < 1) || (col > Columns))
                throw new ArgumentException("invalid position", "col");
            if ((row < 1) || (row > Lines))
                throw new ArgumentException("invalid position", "row");

            int[] rowOffset = { 0x00, 0x40, 0x14, 0x54 };
            var addr = col - 1 + rowOffset[row - 1];
            WriteCmd((byte)(0x80 | addr));
        }

        public void Print(string text)
        {

            foreach (var txch in text)
            {
                byte ch = (byte)txch;
                switch (txch)
                {
                    case 'Ä': ch = 0x00; break;
                    case 'Ö': ch = 0x01; break;
                    case 'Ü': ch = 0x02; break;
                    case 'g': ch = 0x03; break;
                    case 'j': ch = 0x04; break;
                    case 'p': ch = 0x05; break;
                    case 'q': ch = 0x06; break;
                    case 'y': ch = 0x07; break;

                    case '¥': ch = 0x5C; break;

                    case '`': ch = 0x60; break;

                    case '→': ch = 0x7E; break;
                    case '←': ch = 0x7F; break;

                    case '⋅': ch = 0xA5; break;

                    case '°': ch = 0xDF; break;

                    case 'α': ch = 0xE0; break;
                    case 'ä': ch = 0xE1; break;
                    case 'β': ch = 0xE2; break;
                    case 'ß': ch = 0xE2; break;
                    case 'ε': ch = 0xE3; break;
                    case 'μ': ch = 0xE4; break;
                    case 'δ': ch = 0xE5; break;
                    case '√': ch = 0xE8; break;
                    case '¢': ch = 0xEC; break;
                    case '₵': ch = 0xEC; break;
                    case 'ñ': ch = 0xEE; break;
                    case 'ö': ch = 0xEF; break;

                    case 'Θ': ch = 0xF2; break;
                    case '∞': ch = 0xF3; break;
                    case 'Ω': ch = 0xF4; break;
                    case 'ü': ch = 0xF5; break;
                    case 'Σ': ch = 0xF6; break;
                    case '∑': ch = 0xF6; break;
                    case 'π': ch = 0xF7; break;
                    case '÷': ch = 0xFD; break;
                    case '█': ch = 0xFF; break;
                }
                WriteData(ch);
            }
        }

        private void WriteCmd(byte data)
        {
            WriteNibble((byte)(data & 0xf0));
            WriteNibble((byte)((data << 4) & 0xf0));
        }
        private void WriteData(byte data)
        {
            WriteNibble((byte)((data & 0xf0) | LcdRs));
            WriteNibble((byte)(((data << 4) & 0xf0) | LcdRs));
        }
        private void WriteNibble(byte nibble)
        {
            if (_backlight)
                nibble |= LcdBl;
            i2c.Write(new[] { nibble, (byte)(nibble | LcdEn), nibble });
        }

    }
}

