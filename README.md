# Gear VR Controller for Windows

Use a Samsung Gear VR Controller as a mouse and remap its buttons on Windows. The application connects over Bluetooth Low Energy (BLE), supports motion aiming and touchpad controls, and can remain in the Windows notification area.

## Requirements

- Windows 10 or 11 with a working Bluetooth LE adapter.
- A Samsung Gear VR Controller with charged batteries.
- The .NET 6 Desktop Runtime to run a framework-dependent build.
- To build from source: the .NET 6 SDK with Windows desktop support and access to NuGet.

## Connect a controller

1. Turn on Bluetooth in Windows and wake the controller. Pair it in Windows Bluetooth settings if necessary.
2. Open the app and select **Detectar**. It searches Windows' known BLE devices and nearby advertisements for a Gear VR Controller. If several are found, choose one from the list. You can instead enter a 12-digit Bluetooth address manually.
3. Select **Conectar**. The indicator turns green when controller data arrives. Windows may request pairing confirmation.
4. To connect automatically at launch and retry after the controller sleeps or becomes unavailable, enable **Conectar y reconectar automáticamente**. Each connection attempt times out; **Desconectar** cancels the attempt and stops automatic retries until you connect again.

If detection fails, wake the controller, check the adapter and Windows pairing, and try again. A sleeping controller may not advertise until awakened. If Windows has stale pairing information, remove the device from Bluetooth settings and pair it again. The app can find only devices Windows exposes or whose matching BLE advertisements it receives.

## Motion pointer

Motion aiming is enabled by default. Rotate the controller to move the cursor; adjust **Sensibilidad** from 0.25× to 3×. The app smooths movement between Bluetooth reports. The display checkboxes for gyro and accelerometer readings do not need to be enabled for pointing.

The pointer can reach the full Windows virtual desktop, including monitors to the left of or above the primary monitor. To recenter, point at the screen and hold **Home** for one second while keeping the controller still. The pointer moves to the center of the monitor containing the cursor, and stationary samples are used to reduce gyro drift. A short Home press invokes its assigned action. Rotation is estimated from the gyroscope and gravity; it does not provide absolute position tracking and can drift over time.

Uncheck **Activar apuntado con giroscopio** to move the pointer with the touchpad instead. Touchpad button mappings and scroll mode remain available while motion aiming is enabled.

## Idle disconnect

**Desconectar por inactividad** is enabled by default and disconnects after 5 minutes without touchpad contact or button input. Set the timeout to any whole number from 1 to 1440 minutes, or disable the option. Motion, accelerometer and gyroscope readings, and incoming BLE packets do not count as activity. The timeout can therefore expire while moving the pointer if no button or touchpad input occurs. After an idle disconnect it pauses automatic reconnection until you select **Conectar**; the automatic connection preference is retained for future launches. Disconnecting releases the application's Bluetooth session, but does not guarantee when Windows or the controller will enter its own low-power mode.

## Button mappings

The diagrams show the five touchpad regions, five combinations of a touched region plus the trigger, and the other physical buttons. Select a region or button to edit its assignment; hover over it to see the current assignment. The trigger's own mapping is used when the touchpad is not touched.

The editor accepts keys from its list, mouse buttons, `Toggle`, or `Launch: <path>`. `Toggle` switches the touchpad between pointer and wheel mode. In wheel mode, swipe vertically or horizontally; use **Velocidad de la rueda** to configure its speed. For available keyboard codes, see [Windows virtual-key codes](https://learn.microsoft.com/windows/win32/inputdev/virtual-key-codes).

## Startup and tray

- Enable **Iniciar con Windows, minimizado** to start the app in the notification area when you sign in. Enable the separate automatic connection option if you also want it to connect to the controller.
- Closing or minimizing the window keeps the app running in the notification area. Double-click its icon to reopen it; right-click to see connection status, connect or disconnect, or choose **Salir** to quit.
- If you move the executable, re-enable Windows startup from the new location. Windows user settings can also be stored separately for different executable locations.

## Build

From the repository root on Windows:

```powershell
dotnet restore "GearVR Controller/GearVR Controller.csproj"
dotnet build "GearVR Controller/GearVR Controller.csproj" --configuration Release
```

To create a framework-dependent distribution:

```powershell
dotnet publish "GearVR Controller/GearVR Controller.csproj" --configuration Release --output publish
```

Run `GearVR Controller.exe` from the output directory. A release build still requires the .NET 6 Desktop Runtime unless published as self-contained.

## Verification and limitations

The project has been compiled successfully on Windows. Controller detection was checked against a Gear VR Controller visible to Windows, and motion coordinate calculations were checked with simulated multi-monitor layouts. Bluetooth reconnection, pointer feel, monitor transitions and button behavior should still be verified with the physical controller and the target computer's display arrangement. Performance depends on the controller, Bluetooth adapter, Windows and display configuration.

## AI assistance

The connection and reconnection changes, controller detection, motion pointer, multi-monitor support, interface updates, and accompanying documentation in this set of changes were developed with assistance from OpenAI Codex. Compilation and the checks described above were performed during development; AI assistance does not replace review or testing on the target hardware.

## Credits and license

- Jim Yan's [Gear VR Controller Web Bluetooth work](https://github.com/jsyang/gearvr-controller-webbluetooth) for controller reverse engineering.
- [rdady's Windows controller project](https://github.com/rdady/gear-vr-controller-windows) for a starting point.

See [LICENSE](LICENSE) for the project license.
