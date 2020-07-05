# Unity-for-Game-Bar
An app that allows a Unity project built for the Universal Windows Platform to be used in the Xbox Game Bar.

This app uses simple code generation to allow Unity games to be used in the Xbox Game Bar.
It was designed for mobile-like games to be played whiles also playing another game, perhaps whiles players are waiting for a game or the next round to start. However, any Unity game will work.

## Guide
### If you are new to building Unity games for UWP
1. Install the **Universal Windows Platform Build Support** module for Unity 2019.4 (other versions of Unity may work).
2. Load the desired Unity project, then go to **File | Build Settings**. Select **Universal Windows Platform** and click **Switch Platform**.
3. When all your assets have been reimported, it is recommended to use the following configuration:
  - Target Device: PC
- Architecture: x64
- Build Type: XAML Project
- Minimum Platform Version: 10.0.18362.0
- Build Configuration: Master
4. Click **Build** and select a folder. We will need access to this folder shortly so remember where it is. The first build will usually take a long time.
5. You can know more about building Unity games for UWP here: https://unity3d.com/partners/microsoft/porting-guides

### For everyone
6. Open the [app](https://github.com/dynamiquel/Unity-for-Game-Bar/releases), and for **Unity UWP Solution**, enter the file path for the solution (.sln) file that Unity created for the UWP project.
7. All other options are for preference. When you are ready, click **Convert Project**.
8. Once the project has been converted, open the solution (.sln) file with **Visual Studio 2019**.
9. Install the **Microsoft.Gaming.XboxGameBar** NuGet package, by going to **Project | Manage NuGet Packages | Browse**, search for 'Microsoft.Gaming.XboxGameBar' and click **Install**. *I wasn't sure how to automatically install the package*.
10. Change the **Solution Configuration** from **Debug** to **Master**, and the **Solution Platform** to **x64**.
11. From here, treat it as a typical UWP app.

## Things you should know
1. The game must work with the Universal Windows Platform to be used in the Xbox Game Bar.
2. Not tested, but it is likely that running the game in the Xbox Game Bar will yield lower frame rates than running normally.
3. Only one instance of the game can exist at any given time. If you attempt to load the game through the Xbox Game Bar as well as traditonally, the game will crash.
4. Other than that, there are zero known limitations of running your game through the Xbox Game Bar.
