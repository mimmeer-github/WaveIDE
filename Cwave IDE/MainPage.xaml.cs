using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI;
using System.Collections.ObjectModel;
using muxc = Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Popups;
using Windows.System;
using Windows.Storage; // Added
using Windows.Storage.Search; // Added
using System.Threading.Tasks;
using System.Linq.Expressions; // Added

namespace Cwave_IDE
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static Dictionary<string, string> highlightLanguages = new Dictionary<string, string>();
        public static string filePath;
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async Task LoadFiles(string filePath)
        {
            muxc.TreeViewNode rootNode = new muxc.TreeViewNode() { Content = System.IO.Path.GetFileName(filePath) };
            rootNode.IsExpanded = true;
            Files.RootNodes.Add(await search(rootNode, filePath));
        }

        public async Task<muxc.TreeViewNode> search(muxc.TreeViewNode rootNode, string path)
        {
            try
            {
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(path);

                // Get files
                var fileQuery = folder.CreateFileQuery();
                IReadOnlyList<StorageFile> files = await fileQuery.GetFilesAsync();

                foreach (StorageFile file in files)
                {
                    muxc.TreeViewNode fileNode = new muxc.TreeViewNode() { Content = file.Name };
                    fileNode.SetValue(TagProperty, file.Path); // Store the full path
                    rootNode.Children.Add(fileNode);
                }

                // Get folders
                var folderQuery = folder.CreateFolderQuery();
                IReadOnlyList<StorageFolder> subFolders = await folderQuery.GetFoldersAsync();

                foreach (StorageFolder subFolder in subFolders)
                {
                    muxc.TreeViewNode folderNode = new muxc.TreeViewNode() { Content = subFolder.Name };
                    await search(folderNode, subFolder.Path); // Recursive call
                    folderNode.SetValue(TagProperty, subFolder.Path); // Store the full path
                    rootNode.Children.Add(folderNode);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Storage API Error: " + ex.Message);
                // Handle the error (e.g., show a message to the user)
            }

            return rootNode;
        }

        private async void InitMessageDialogHandler(IUICommand command)
        {
            if ((int)command.Id == 0)
            {
                await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-broadfilesystemaccess"));
            }
        }

        private void ConfigureLanguages()
        {
            // These are only WinRT languages.
            // All CPP-like languages
            highlightLanguages.Add("c", "cpp");
            highlightLanguages.Add("cpp", "cpp");
            highlightLanguages.Add("cxx", "cpp");
            highlightLanguages.Add("java", "cpp");
            // All Web Development languages
            highlightLanguages.Add("html", "html");
            highlightLanguages.Add("js", "js");
            highlightLanguages.Add("css", "css");
            // All .NET languages
            highlightLanguages.Add("cs", "csharp");
            // All WaveIDE Languages / Extensions
            highlightLanguages.Add("wavc", "csharp");
            highlightLanguages.Add("wc", "csharp");
            highlightLanguages.Add("wavec", "csharp");
            highlightLanguages.Add("cwave", "csharp");
            highlightLanguages.Add("cwav", "csharp");
            highlightLanguages.Add("cw", "csharp");
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            string projectDirEnv = "%USERPROFILE%\\Documents\\Wave IDE";
            string projectDir = Environment.ExpandEnvironmentVariables(projectDirEnv);
            muxc.TreeViewNode rootNode = new muxc.TreeViewNode() { Content = "PROJECT NAME" };
            rootNode.IsExpanded = true;
            ConfigureLanguages();
            _ = LoadFiles(projectDir);
        }

        private async void Files_ItemInvoked(muxc.TreeView sender, muxc.TreeViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is muxc.TreeViewNode clickedNode)
            {
                // Check if it's a file (files won't have children in our tree)
                if (!clickedNode.HasChildren)
                {
                    // Get the full path to the file from the Tag property
                    filePath = clickedNode.GetValue(TagProperty) as string;

                    if (!string.IsNullOrEmpty(filePath))
                    {
                        try
                        {
                            // Load the content of the file
                            StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);
                            string fileContent = await FileIO.ReadTextAsync(file);
                            IList<string> fileContents = await FileIO.ReadLinesAsync(file);

                            // Set the text in your editor
                            MyEditor.Editor.SetText(fileContent);
                            try
                            {
                                MyEditor.HighlightingLanguage = highlightLanguages[file.FileType.Remove(0, 1)];
                            } 
                            catch (System.Collections.Generic.KeyNotFoundException)
                            {
                                MyEditor.HighlightingLanguage = "text";
                            }
                            
                            
                        }
                        catch (Exception ex)
                        {
                            // Uh oh, something went wrong! Let's show an error message for now
                            MyEditor.Editor.SetText($"Error loading file:\n{ex.Message}");
                        }
                    }
                }
            }
        }

        private async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // Save button
            var file = await StorageFile.GetFileFromPathAsync(filePath);
            var contents = MyEditor.Editor.GetText(MyEditor.Editor.TextLength);
            string[] lines = contents.Split(Environment.NewLine);
            await FileIO.WriteLinesAsync(file, lines);
        }
    }
}