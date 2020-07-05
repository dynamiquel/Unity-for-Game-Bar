using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using Path = System.IO.Path;

namespace UnityUWPConfigurator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void browseUwpSolutionButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".sln";
            dlg.Filter = "Visual Studio Solutions (.sln)|*.sln";

            bool? result = dlg.ShowDialog();

            if (result == true)
                uwpSolutionLocationTextBox.Text = dlg.FileName;
        }

        private void convertButton_Click(object sender, RoutedEventArgs e)
        {
            var uwpSolutionFileLocation = uwpSolutionLocationTextBox.Text;

            if (string.IsNullOrWhiteSpace(uwpSolutionFileLocation) && !Path.IsPathRooted(uwpSolutionFileLocation))
            {
                MessageBox.Show("A valid Unity UWP Solution file must be provided.\nIt is the .sln file created by Unity when building for the Universal Windows Platform.", "Solution missing", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }    

            var uwpSolutionDirectory = Path.GetDirectoryName(uwpSolutionFileLocation);

            var projectName = GetProjectName(uwpSolutionFileLocation);
            var namespaceName = GetNamespaceName(projectName);
            var projectDirectory = Path.Combine(uwpSolutionDirectory, projectName);

            var displayName = gameDisplayNameTextBox.Text;
            var displayDescription = descriptionTextBox.Text;

            int startWidth;
            int startHeight;
            int minWidth;
            int minHeight;

            try
            {
                startWidth = int.Parse(widthTextBox.Text);
                startHeight = int.Parse(heightTextBox.Text);
                minWidth = int.Parse(minWidthTextBox.Text);
                minHeight = int.Parse(minHeightTextBox.Text);
            }
            catch (Exception)
            {
                MessageBox.Show("Invalid input for one of the size fields. Whole numbers only.", "Invalid input", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            bool resizable = resizableCheckBox.IsChecked ?? false;

            try
            {
                AddGameBarPackage(uwpSolutionDirectory, projectName);
            }
            catch (Exception)
            {
                MessageBox.Show("An error occured when adding the Game Bar package.", "Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                ModifyAppManifest(uwpSolutionDirectory, projectName, true, displayName, displayDescription, startWidth, startHeight, minWidth, minHeight, resizable);
            }
            catch (Exception)
            {
                MessageBox.Show("An error occured when modifying the project's appxmanifest.", "Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                ModifyAppH(projectDirectory);
            }
            catch (Exception)
            {
                MessageBox.Show("An error occured when modifying the project's App.xaml.h file.", "Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                ModifyAppCpp(projectDirectory);
            }
            catch (Exception)
            {
                MessageBox.Show("An error occured when modifying the project's App.xaml.cpp file.", "Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show("Game Bar widget successfully attached to the Unity project.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string GetProjectName(string uwpSolutionFileLocation)
        {
            string projectName = Path.GetFileNameWithoutExtension(uwpSolutionFileLocation);
            projectName.Replace(' ', '_');
            return projectName;
        }

        private string GetNamespaceName(string projectName)
        {
            return projectName.Replace(' ', '_');
        }

        // Adds the required Game Bar NuGet package to the project.
        private void AddGameBarPackage(string uwpSolutionDirectory, string projectName)
        {
            string projectFilePath = Path.Combine(uwpSolutionDirectory, projectName, $"{projectName}.vcxproj");

            string contents = File.ReadAllText(projectFilePath);

            contents = contents.Replace("</ImportGroup>", 
  @"  <Import Project=""..\packages\Microsoft.Gaming.XboxGameBar.5.3.200605002\build\native\Microsoft.Gaming.XboxGameBar.targets"" Condition=""Exists('..\packages\Microsoft.Gaming.XboxGameBar.5.3.200605002\build\native\Microsoft.Gaming.XboxGameBar.targets')""/>
  </ImportGroup>
  <Target Name=""EnsureNuGetPackageBuildImports"" BeforeTargets=""PrepareForBuild"" >
    <PropertyGroup>
      <ErrorText> This project references NuGet package(s) that are missing on this computer.Use NuGet Package Restore to download them.For more information, see http://go.microsoft.com/fwlink/?LinkID=322105. The missing file is {0}.</ErrorText>
    </PropertyGroup>
    <Error Condition=""!Exists('..\packages\Microsoft.Gaming.XboxGameBar.5.3.200605002\build\native\Microsoft.Gaming.XboxGameBar.targets')"" Text=""$([System.String]::Format('$(ErrorText)', '..\packages\Microsoft.Gaming.XboxGameBar.5.3.200605002\build\native\Microsoft.Gaming.XboxGameBar.targets'))""/>
  </Target>");

            File.WriteAllText(projectFilePath, contents);
        }

        private void ModifyAppManifest(string uwpSolutionDirectory, string projectName, bool gameBarOnly, string displayName, string displayDescription, int startWidth, int startHeight, int minWidth, int minHeight, bool resizable)
        {
            string manifest = Path.Combine(uwpSolutionDirectory, projectName, "Package.appxmanifest");

            string contents = File.ReadAllText(manifest);

            contents = contents.Replace("</uap:VisualElements>",
                @$"</uap:VisualElements>
      <Extensions>
        <uap3:Extension Category=""windows.appExtension"">
          <uap3:AppExtension Name=""microsoft.gameBarUIExtension""
                             Id=""UnityPlayerWidget""
                             DisplayName=""{displayName}""
                             Description=""{displayDescription}""
                             PublicFolder=""GameBar"">
            <uap3:Properties>
              <GameBarWidget Type=""Standard"">
                <HomeMenuVisible>true</HomeMenuVisible>
                <PinningSupported>true</PinningSupported>
                <Window>
                  <Size>
                    <Height>{startHeight}</Height>
                    <Width>{startWidth}</Width>
                    <MinHeight>{minHeight}</MinHeight>
                    <MinWidth>{minWidth}</MinWidth>
                  </Size>
                  <ResizeSupported>
                    <Horizontal>{resizable}</Horizontal>
                    <Vertical>{resizable}</Vertical>
                  </ResizeSupported>
                </Window>
              </GameBarWidget>
            </uap3:Properties>
          </uap3:AppExtension>
        </uap3:Extension>
      </Extensions>");

            contents = contents.Replace("</Package>", @"
  <Extensions>
    <!-- Enlighten COM on where to find Metadata Based Marshaling (MBM) data for the Game Bar private types 
       <Path> is a required element (by VS) and has to point to a binary in the package, but it's not used when the class id is 00000355-0000-0000-C000-000000000046 (MBM). Due to that we just put the Microsoft.Gaming.XboxGameBar.winmd here. -->
    <Extension Category=""windows.activatableClass.proxyStub"">
      <ProxyStub ClassId=""00000355-0000-0000-C000-000000000046"">
        <Path>Microsoft.Gaming.XboxGameBar.winmd</Path>

        <!-- include when using SDK version 5.1+-->
        <Interface Name=""Microsoft.Gaming.XboxGameBar.Private.IXboxGameBarWidgetAuthHost"" InterfaceId=""DC263529-B12F-469E-BB35-B94069F5B15A"" />
        <Interface Name=""Microsoft.Gaming.XboxGameBar.Private.IXboxGameBarWidgetControlHost"" InterfaceId=""C309CAC7-8435-4082-8F37-784523747047"" />
        <Interface Name=""Microsoft.Gaming.XboxGameBar.Private.IXboxGameBarNavigationKeyCombo"" InterfaceId=""5EEA3DBF-09BB-42A5-B491-CF561E33C172"" />
        <Interface Name=""Microsoft.Gaming.XboxGameBar.Private.IXboxGameBarWidgetActivatedEventArgsPrivate"" InterfaceId=""782535A7-9407-4572-BFCB-316B4086F102"" />
        <Interface Name=""Microsoft.Gaming.XboxGameBar.Private.IXboxGameBarWidgetHost"" InterfaceId=""5D12BC93-212B-4B9F-9091-76B73BF56525"" />
        <Interface Name=""Microsoft.Gaming.XboxGameBar.Private.IXboxGameBarWidgetPrivate"" InterfaceId=""22ABA97F-FB0F-4439-9BDD-2C67B2D5AA8F"" />

        <!-- include when using SDK version 5.3+-->
        <Interface Name=""Microsoft.Gaming.XboxGameBar.Private.IXboxGameBarWidgetHost2"" InterfaceId=""28717C8B-D8E8-47A8-AF47-A1D5263BAE9B"" />
        <Interface Name=""Microsoft.Gaming.XboxGameBar.Private.IXboxGameBarWidgetPrivate2"" InterfaceId=""B2F7DB8C-7540-48DA-9B46-4E60CE0D9DEB"" />

      </ProxyStub>
    </Extension>
  </Extensions>
</Package>");

            File.WriteAllText(manifest, contents);
        }

        private void ModifyAppH(string projectDirectory)
        {
            string appH = Path.Combine(projectDirectory, $"App.xaml.h");
            string contents = File.ReadAllText(appH);

            contents = contents.Replace("};",
                @"  
        void UnityPlayerWidgetWindowClosedHandler(Platform::Object^ sender, Windows::UI::Core::CoreWindowEventArgs^ e);
        Windows::Foundation::EventRegistrationToken m_unityPlayerWidgetWindowClosedHandlerToken{};
        Microsoft::Gaming::XboxGameBar::XboxGameBarWidget^ m_unityPlayerWidget{ nullptr };
        void OnNavigationFailed(Platform::Object^ sender, Windows::UI::Xaml::Navigation::NavigationFailedEventArgs^ e);
    };");

            File.WriteAllText(appH, contents);
        }

        private void ModifyAppCpp(string projectDirectory)
        {
            string appCpp = Path.Combine(projectDirectory, $"App.xaml.cpp");
            string contents = File.ReadAllText(appCpp);

            string toReplace = @"void App::OnActivated(IActivatedEventArgs^ args)
{
    String^ appArgs = """";

    if (args->Kind == ActivationKind::Protocol)
    {
        auto eventArgs = safe_cast<ProtocolActivatedEventArgs^>(args);
        m_SplashScreen = eventArgs->SplashScreen;
        appArgs += String::Concat(""Uri="", eventArgs->Uri->AbsoluteUri);
    }

    InitializeUnity(appArgs);
}";
            var regex = new Regex(Regex.Escape("InitializeUnity(appArgs);"));
            contents = regex.Replace(contents,
                @"Microsoft::Gaming::XboxGameBar::XboxGameBarWidgetActivatedEventArgs^ widgetArgs = nullptr;
    if (args->Kind == ActivationKind::Protocol)
    {
        auto protocolArgs = dynamic_cast<IProtocolActivatedEventArgs^>(args);
        if (protocolArgs)
        {
            // If scheme name is ms-gamebarwidget, Xbox Game Bar is activating us.
            const wchar_t* scheme = protocolArgs->Uri->SchemeName->Data();
            if (0 == wcscmp(scheme, L""ms-gamebarwidget""))
            {
                widgetArgs = dynamic_cast<Microsoft::Gaming::XboxGameBar::XboxGameBarWidgetActivatedEventArgs^>(args);
            }
        }
    }
    if (widgetArgs)
    {
        //
        // Activation Notes:
        //
        //    If IsLaunchActivation is true, this is Game Bar launching a new instance
        // of our widget. This means we have a NEW CoreWindow with corresponding UI
        // dispatcher, and we MUST create and hold onto a new XboxGameBarWidget.
        //
        // Otherwise this is a subsequent activation coming from Game Bar. We MUST
        // continue to hold the XboxGameBarWidget created during initial activation
        // and ignore this repeat activation, or just observe the URI command here and act 
        // accordingly.  It is ok to perform a navigate on the root frame to switch 
        // views/pages if needed.  Game Bar lets us control the URI for sending widget to
        // widget commands or receiving a command from another non-widget process. 
        //
        // Important Cleanup Notes:
        //    When our widget is closed--by Game Bar or us calling XboxGameBarWidget.Close()-,
        // the CoreWindow will get a closed event.  We can register for Window.Closed
        // event to know when our partucular widget has shutdown, and cleanup accordingly.
        //
        // NOTE: If a widget's CoreWindow is the LAST CoreWindow being closed for the process
        // then we won't get the Window.Closed event.  However, we will get the OnSuspending
        // call and can use that for cleanup.
        //
        if (widgetArgs->IsLaunchActivation)
        {
            auto rootFrame = ref new Frame();
            rootFrame->NavigationFailed += ref new Windows::UI::Xaml::Navigation::NavigationFailedEventHandler(this, &App::OnNavigationFailed);
            Window::Current->Content = rootFrame;

            // Create Game Bar widget object which bootstraps the connection with Game Bar
            m_unityPlayerWidget = ref new Microsoft::Gaming::XboxGameBar::XboxGameBarWidget(
                widgetArgs,
                Window::Current->CoreWindow,
                rootFrame);
            rootFrame->Navigate(TypeName(MainPage::typeid), nullptr);

            m_unityPlayerWidgetWindowClosedHandlerToken = Window::Current->Closed +=
                ref new WindowClosedEventHandler(this, &App::UnityPlayerWidgetWindowClosedHandler);

            Window::Current->Activate();
        }
    }", 1);

            contents += @"
/// <summary>
/// Invoked when Navigation to a certain page fails
/// </summary>
/// <param name=""sender"">The Frame which failed navigation</param>
/// <param name=""e"">Details about the navigation failure</param>
void App::OnNavigationFailed(Platform::Object ^ sender, Windows::UI::Xaml::Navigation::NavigationFailedEventArgs ^ e)
{
    throw ref new FailureException(""Failed to load Page "" + e->SourcePageType.Name);
}

void App::UnityPlayerWidgetWindowClosedHandler(Platform::Object^ /*sender*/, Windows::UI::Core::CoreWindowEventArgs^ /*e*/)
{
    m_unityPlayerWidget = nullptr;
    Window::Current->Closed -= m_unityPlayerWidgetWindowClosedHandlerToken;
}";

            File.WriteAllText(appCpp, contents);
        }

        private void numberTextBox_PreviewInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
                e.Handled = true;
            }
            catch (Exception)
            {

            }
        }
    }
}
