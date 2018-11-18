This is a sample project implementing the Bluetooth LE Google Daydream handheld controller plugin in Unity for connecting a Daydream controller with Windows HoloLens and Windows 10 PCs.  
  
This is the Lite version of the plugin. Some features time-out after a couple of minutes.  
  
The plugin is available on the Unity Asset store at these links:  
  
Lite:    
https://assetstore.unity.com/packages/tools/input-management/google-daydream-controller-plugin-for-hololens-lite-95316  
  
Pro:  
https://assetstore.unity.com/packages/tools/input-management/google-daydream-controller-plugin-for-hololens-pro-95051  


-----------  
    
2018/11/11  
Plugin Testing and Confirmation  
Successful Build and test on HoloLens HRM Plugin version: 1.3.4.0  
    
I will occasionally update my toolset and republish to HoloLens to confirm that everything is still working. This is the toolset used in my latest test:
   
Unity Editor Version: 2017.4.14f1  
(as seen in: ProjectSettings > ProjectVersion.txt)  
  
Mixed Reality Toolkit 2017.4.2.0 (as seen in: Assets\HoloToolkit\MRTKVersion.txt  
https://github.com/Microsoft/MixedRealityToolkit-Unity/releases/tag/2017.4.2.0  
  
Windows SDK 10.0.17763.0 (as seen in: C:\Program Files (x86)\Windows Kits\10\SDKManifest.xml)  
  
Microsoft Visual Studio Community 2017 Version: 15.8.7  
.NET Version 4.7.03056  
(as seen in Visual Studio About)  
  
Windows Home 10 Version: 10.0.17134  
(as seen via Commandline: winver: Version 1803 (OS Build 17134.345)  
Greater than Fall Creator's Update version (1709)  
https://pureinfotech.com/check-windows-10-fall-creators-update-installed/  
  
HoloLens (Windows Insider Program):  
OS 10.0.17763.1000  

-----------  
  
There are several applications I recommend for testing your HRM device's BLE capabilities:  
- Android/iOS – nRF Connect from Nordic Semiconductor:  
  - https://play.google.com/store/apps/details?id=no.nordicsemi.android.mcp&hl=en  
  - https://itunes.apple.com/us/app/nrf-connect/id1054362403?mt=8  
- Microsoft Bluetooth LE Explorer for Windows 10 (and now available for HoloLens also):  
  - https://www.microsoft.com/en-us/store/p/bluetooth-le-explorer/9n0ztkf1qd98  
  
 
