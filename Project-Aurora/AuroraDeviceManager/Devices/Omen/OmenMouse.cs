﻿using System.Collections.Concurrent;
using System.Drawing;
using System.Runtime.InteropServices;
using Common.Devices;

namespace AuroraDeviceManager.Devices.Omen
{
    public class OmenMouse : IOmenDevice
    {
        private const string OmenLightingSdkDll = "OmenLightingSDK.dll";
        private IntPtr hMouse = IntPtr.Zero;

        ConcurrentDictionary<int, Color> cachedColors = new ConcurrentDictionary<int, Color>();

        private OmenMouse(IntPtr hMouse)
        {
            this.hMouse = hMouse;
        }

        public static IEnumerable<OmenMouse> GetOmenMice()
        {
            var ptr = OmenLighting_Mouse_OpenByName("Photon");
            if (ptr != IntPtr.Zero)
                yield return new OmenMouse(ptr);
            
            ptr = OmenLighting_Mouse_OpenByName("Daffy2");
            if (ptr != IntPtr.Zero)
                yield return new OmenMouse(ptr);
            
            ptr = OmenLighting_Mouse_OpenByName("Drake2");
            if (ptr != IntPtr.Zero)
                yield return new OmenMouse(ptr);
        }

        private static MouseLightingZone GetMouseLightingZone(DeviceKeys key)
        {
            MouseLightingZone zone = MouseLightingZone.MOUSE_LIGHTING_ZONE_ALL;
            switch (key)
            {
                case DeviceKeys.Peripheral_Logo:
                    return MouseLightingZone.MOUSE_LIGHTING_ZONE_LOGO;
                case DeviceKeys.Peripheral_FrontLight:
                    return MouseLightingZone.MOUSE_LIGHTING_ZONE_LOGO;
                case DeviceKeys.Peripheral_ScrollWheel:
                    return MouseLightingZone.MOUSE_LIGHTING_ZONE_WHEEL;
                default:
                    return zone;
            }
        }

        public enum MouseLightingZone
        {
            MOUSE_LIGHTING_ZONE_ALL = 0,                            /* All zone. Only for set lighting effect. */
            MOUSE_LIGHTING_ZONE_LOGO = 1,                           /* Logo zone */
            MOUSE_LIGHTING_ZONE_WHEEL = 2,                          /* Wheel zone */
        }

        public void SetLights(Dictionary<DeviceKeys, Color> keyColors)
        {
            if (hMouse != IntPtr.Zero)
            {
                foreach (KeyValuePair<DeviceKeys, Color> keyColor in keyColors)
                {
                    switch (keyColor.Key)
                    {
                        case DeviceKeys.Peripheral_Logo:
                        case DeviceKeys.Peripheral_FrontLight:
                        case DeviceKeys.Peripheral_ScrollWheel:
                            SetLight(keyColor.Key, keyColor.Value);
                            break;
                    }
                }
            }
        }

        public void SetLight(DeviceKeys key, Color color)
        {
            if (hMouse != IntPtr.Zero)
            {
                int zone = (int)GetMouseLightingZone(key);
                cachedColors.AddOrUpdate(zone, color, (_, oldValue) => color);
                Task.Run(() =>
                {
                    if (Monitor.TryEnter(this))
                    {
                        try
                        {
                            foreach (var item in cachedColors)
                            {
                                LightingColor c = LightingColor.FromColor(item.Value);
                                int res = OmenLighting_Mouse_SetStatic(hMouse, item.Key, c, IntPtr.Zero);
                                if (res != 0)
                                {
                                    Global.Logger.Error("OMEN Mouse, Set static effect fail: " + res);
                                }

                                Color outColor;
                                cachedColors.TryRemove(item.Key, out outColor);
                            }
                        }
                        catch (Exception exc)
                        {
                            Global.Logger.Error("OMEN Mouse, exception during set lights: " + exc);
                        }
                        finally
                        {
                            Monitor.Exit(this);
                        }
                    }
                });
            }
        }

        public string GetDeviceName()
        {
            return hMouse != IntPtr.Zero ? "Mouse Connected" : string.Empty;
        }

        public void Shutdown()
        {
            try
            {
                Monitor.Enter(this);
                OmenLighting_Mouse_Close(hMouse);
                hMouse = IntPtr.Zero;
            }
            catch (Exception exc)
            {
                Global.Logger.Error("OMEN Mouse, exception during shutdown: " + exc);
            }
            finally
            {
                Monitor.Exit(this);
            }
        }

        [DllImport(OmenLightingSdkDll)]
        static extern IntPtr OmenLighting_Mouse_Open();

        [DllImport(OmenLightingSdkDll)]
        static extern IntPtr OmenLighting_Mouse_OpenByName([MarshalAs(UnmanagedType.LPWStr)] string deviceName);

        [DllImport(OmenLightingSdkDll)]
        static extern void OmenLighting_Mouse_Close(IntPtr hMouse);

        [DllImport(OmenLightingSdkDll)]
        static extern int OmenLighting_Mouse_SetStatic(IntPtr hMouse, int zone, LightingColor color, IntPtr property);
    }
}
