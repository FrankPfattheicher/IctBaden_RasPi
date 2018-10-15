﻿using System;
using System.Threading;
using IctBaden.RasPi.Display;
using IctBaden.RasPi.IO;
using IctBaden.RasPi.Sensor;
using IctBaden.RasPi.System;

namespace RasPiSample
{
    internal class Program
    {
        private static void Main()
        {
            Console.WriteLine("Raspi Sample");
            
            Console.WriteLine("1-wire Temperature Sensors");
            var devices = OneWireTemp.GetDevices();
            Console.WriteLine(devices.Count + " device(s) found");
            foreach (var device in devices)
            {
                var temp = OneWireTemp.ReadDeviceTemperature(device);
                Console.WriteLine(device + " = " + temp);
            }

            Console.WriteLine("Digital I/O");
            var io = new DigitalIo();
            if (!io.Initialize())
            {
                Console.WriteLine("Failed to initialize IO");
                return;
            }


            Console.WriteLine("I2C");
            const string deviceName = "/dev/i2c-1";
            var lcd = new CharacterDisplayI2C();
            if (!lcd.Open(deviceName, 0x27))
            {
                Console.WriteLine("Failed to open I2C");
                return;
            }

            lcd.Print($"RasPi {ModelInfo.Revision:X}={ModelInfo.Name}");
            lcd.SetCursor(1, 2);
            lcd.Print("äöüßgjpqyÄÖÜ 0°");

            var oldInp = io.GetInputs();
            while (true)
            {
                var newInp = io.GetInputs();

                if (newInp == oldInp)
                {
                    Thread.Sleep(50);
                    continue;
                }

                Console.WriteLine("Digital I/O");
                Console.WriteLine("Inputs = {0:X8}", newInp);

                oldInp = newInp;
                if ((newInp & 0x0F) == 0x0F)
                {
                    break;
                }

                if (io.GetInput(0))
                {
                    lcd.Backlight = !lcd.Backlight;
                }
                if (io.GetInput(1))
                {
                    for (var ix = 0; ix < io.Outputs; ix++)
                    {
                        Console.WriteLine("Set out {0}", ix);
                        io.SetOutput(ix, true);
                        Thread.Sleep(100);
                    }
                    for (var ix = 0; ix < io.Outputs; ix++)
                    {
                        Console.WriteLine("Reset out {0}", ix);
                        io.SetOutput(ix, false);
                        Thread.Sleep(100);
                    }
                }
                if (io.GetInput(2) && devices.Count > 0)
                {
                    foreach (var device in devices)
                    {
                        var temp = OneWireTemp.ReadDeviceTemperature(device);
                        Console.WriteLine(device + " = " + temp);
                    }
                }

            }

        }



    }

}
