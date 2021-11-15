# GearVR-Controller
GearVR-Controller on windows with re-map capabilities.
![gvr](https://user-images.githubusercontent.com/61667570/141807053-7bc3e593-7f60-4601-8946-c3304ba18e28.png)

# How to use:
* Type in the MAC address of your controller and click `Connect`.
* ButtonPad: Click the touchpad at one of the regions.
* TriggerPad: Touch one of the regions and pull the trigger button.
* Trigger: Sends input mapped to trigger unless touchpad is touched.
* To re-map, click the button you want to re-map and type in what you want to re-map it to. Available keys will show up from dropdwon. For the list of available keys you can visit: https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes.
* To launch a program, click the button you want to re-map and click the `Launch` button on the left when a dialog pops up. Additionally, you can type in `Launch:` and the path to the folder/file you want to open.
* If button is mappped to `Toggle`, it can be used to make the touchpad switch between being used as a trackpad or a scroll wheel.
* In scroll wheel mode move your finger around the touchpad. Clockwise to scroll down, counterclockwise to scroll up.
* Speed of the scrolling can be set by clicking the outer ring of `ButtonPad`.
* How much to move to activate the scroll can be set by clicking the outer ring of `TriggerPad`. The lower the number the more you have to move.
* Click on the checkboxes to display repective sensor data.

# Requirements:
* .NET 6

# Future plans:
Non right now, but may do something with the `Gyro`, `Magnet` and `Accel` data for motion control.
* Feel feel to make a pull request if you want to add or improve upon this project.

# Credits
* Jim Yan for reverse engineering the controller. https://github.com/jsyang/gearvr-controller-webbluetooth
* rdady for providing a starting point. https://github.com/rdady/gear-vr-controller-windows
