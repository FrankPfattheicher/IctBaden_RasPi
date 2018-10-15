using System;
using System.Collections.Generic;
using IctBaden.RasPi.Interop;

namespace IctBaden.RasPi.IO
{
    public class DigitalIo
    {
        public readonly int[] DefaultInputAssignment = { /* GPIO */ 17, 27, 22, 18 };
        public readonly int[] DefaultOutputAssignment = { /* GPIO */ 7, 8, 9, 10, 11, 23, 24, 25 };

        private Dictionary<uint, uint> _ioMode = new Dictionary<uint, uint>();
        private int[] _inputAssignment;
        private int[] _outputAssignment;
        private bool[] _outputValues;

        /// <summary>
        /// GPIO numbers used as digital inputs.
        /// Call Initialize() after changing this.
        /// </summary>
        public int[] InputAssignment
        {
            get => _inputAssignment;
            set => _inputAssignment = value;
        }

        /// <summary>
        /// GPIO numbers used as digital outputs.
        /// Call Initialize() after changing this.
        /// </summary>
        public int[] OutputAssignment
        {
            get => _outputAssignment;
            set
            {
                _outputAssignment = value;
                _outputValues = new bool[_outputAssignment.Length];
                for (var ix = 0; ix < _outputValues.Length; ix++)
                {
                    _outputValues[ix] = false;
                }
            }
        }

        /// <summary>
        /// GPIO numbers and I/O ALT-mode to use with
        /// Call Initialize() after changing this.
        /// </summary>
        public Dictionary<uint, uint> IoMode
        {
            get => _ioMode;
            set => _ioMode = value;
        }

        public int Inputs => _inputAssignment.Length;

        public int Outputs => _outputAssignment.Length;

        public DigitalIo()
        {
            InputAssignment = DefaultInputAssignment;
            OutputAssignment = DefaultOutputAssignment;
        }

        public bool Initialize()
        {
            try
            {
                RawGpio.Initialize();
            } catch (Exception)
            {
                return false;
            }

            foreach (var mode in _ioMode)
            {
                RawGpio.INP_GPIO(mode.Key);
                RawGpio.SET_GPIO_ALT(mode.Key, mode.Value);
            }

            foreach (var input in _inputAssignment)
            {
                RawGpio.INP_GPIO((uint)input);
            }
            
            foreach (var output in _outputAssignment)
            {
                RawGpio.INP_GPIO((uint)output); // must use INP_GPIO before we can use OUT_GPIO
                RawGpio.OUT_GPIO((uint)output);
            }
            
            return true;
        }

        public void SetOutput(int index, bool value)
        {
            if ((index < 0) || (index >= Outputs))
            {
                throw new ArgumentException("Output out of range", "index");
            }
            if (!RawGpio.IsInitialized)
            {
                return;
            }
            if (value)
            {
                RawGpio.GPIO_SET = (uint)(1 << _outputAssignment[index]);
            }
            else
            {
                RawGpio.GPIO_CLR = (uint)(1 << _outputAssignment[index]);
            }
            _outputValues[index] = value;
        }

        public bool GetOutput(int index)
        {
            if ((index < 0) || (index >= Outputs))
            {
                throw new ArgumentException("Output out of range", nameof(index));
            }
            return RawGpio.IsInitialized && _outputValues[index];
        }

        public bool GetInput(int index)
        {
            if ((index < 0) || (index >= Inputs))
            {
                throw new ArgumentException("Input out of range", nameof(index));
            }

            return (RawGpio.GPIO_IN0 & (uint)(1 << _inputAssignment [index])) != 0;
        }

        public ulong GetInputs()
        {
            ulong inputs = 0;
            for (var ix = 0; ix < Inputs; ix++)
            {
                if (GetInput(ix))
                {
                    inputs |= (ulong)1 << ix;
                }
            }
            return inputs;
        }
    }
}
