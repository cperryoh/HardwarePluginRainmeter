using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Lifetime;
using System.Threading;
using System.Threading.Tasks;
using OpenHardwareMonitor.Hardware;
using Rainmeter;

// Overview: This is a blank canvas on which to build your plugin.

// Note: GetString, ExecuteBang and an unnamed function for use as a section variable
// have been commented out. If you need GetString, ExecuteBang, and/or section variables 
// and you have read what they are used for from the SDK docs, uncomment the function(s)
// and/or add a function name to use for the section variable function(s). 
// Otherwise leave them commented out (or get rid of them)!

namespace HardwarePlugin
{
    class Measure
    {
        static public implicit operator Measure(IntPtr data)
        {
            return (Measure)GCHandle.FromIntPtr(data).Target;
        }
        public void setTarget(IHardware hardware, ISensor sensor)
        {
            this.hardware = hardware;
            this.sensor = sensor;
        }
        public double readSensor()
        {
            double sensorValue = 0;
            if (useCPULoad)
            {
                sensorValue = Plugin.getCpuUseage();
            }
            else if (hardware != null && sensor != null)
            {
                hardware.Update();
                sensorValue = (double)sensor.Value;
            }
            return sensorValue;
        }
        IHardware hardware;
        ISensor sensor;
        public SensorType sensorType;
        public bool useCPULoad = false;
        public IntPtr buffer;
        public Computer computer;
    }
    public class Plugin
    {
        static void init(Measure measure, API api)
        {
            measure.computer = new Computer();
            measure.computer.CPUEnabled = true;
            measure.computer.GPUEnabled = true;
            measure.computer.Open();
            String sensorType = api.ReadString("type", "tempature").ToLower();
            String hardwareName = api.ReadString("hardware", "gpu").ToLower();
            HardwareType hardwareType = (hardwareName.Equals("gpu") ? HardwareType.GpuNvidia : HardwareType.CPU);


            String sensorName = null;
            if (hardwareType == HardwareType.GpuNvidia)
                sensorName = "GPU Core";
            if (sensorType.Equals("tempature"))
            {
                sensorName = (hardwareType == HardwareType.GpuNvidia) ? "GPU Core" : "CPU Package";
                measure.sensorType = SensorType.Temperature;
            }
            else
            {
                measure.sensorType = SensorType.Load;
                if (hardwareType == HardwareType.CPU)
                {
                    measure.useCPULoad = true;
                    return;
                }

            }
            foreach (IHardware hardware in measure.computer.Hardware)
            {
                hardware.Update();
                if (hardware.HardwareType == hardwareType)
                {
                    foreach (ISensor sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == measure.sensorType && sensor.Name.Equals(sensorName))
                        {
                            measure.setTarget(hardware, sensor);
                            return;
                        }
                    }
                }
            }
        }
        [DllExport]
        public static void Initialize(ref IntPtr data, IntPtr rm)
        {

            data = GCHandle.ToIntPtr(GCHandle.Alloc(new Measure()));

            Rainmeter.API api = (Rainmeter.API)rm;
            Measure measure = (Measure)data;
            init(measure, api);
        }
        [DllExport]
        public static void Finalize(IntPtr data)
        {
            Measure measure = (Measure)data;
            if (measure.buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(measure.buffer);
            }
            measure.computer.Close();
            cpuCounter.Close();
            GCHandle.FromIntPtr(data).Free();
        }

        static PerformanceCounter cpuCounter = new PerformanceCounter(
        "Processor",
    "% Idle Time",
    "_Total");
        static bool clearedInital = false;
        public static double getCpuUseage()
        {
            double cpuUseage = 100 - cpuCounter.NextValue();
            if (cpuUseage != 100)
                clearedInital = true;
            if (!clearedInital)
                cpuUseage = 0;
            Debug.WriteLine(cpuUseage);
            return cpuUseage;
        }

        [DllExport]
        public static void Reload(IntPtr data, IntPtr rm, ref double maxValue)
        {
            Measure measure = (Measure)data;
            Rainmeter.API api = (Rainmeter.API)rm;
            init(measure, api);
        }

        [DllExport]
        public static double Update(IntPtr data)
        {
            Measure measure = (Measure)data;
            double outputValue = measure.readSensor();
            return (int)(0.5 + outputValue);
        }
        /*[DllExport]
        public static IntPtr GetString(IntPtr data)
        {
            Measure measure = (Measure)data;
            if (measure.buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(measure.buffer);
                measure.buffer = IntPtr.Zero;
            }

            return Marshal.StringToHGlobalUni(measure.data);
        }*/

        //[DllExport]
        //public static void ExecuteBang(IntPtr data, [MarshalAs(UnmanagedType.LPWStr)]String args)
        //{
        //    Measure measure = (Measure)data;
        //}


    }
}
