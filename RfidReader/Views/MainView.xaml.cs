using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using RfidReader.Model;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace RfidReader.Views
{
    /// <summary>
    /// Interaction logic for View1.xaml
    /// </summary>
    public partial class MainView : UserControl
    {
        public MainView()
        {
            InitializeComponent();

            CmbTheme.SelectedIndex = 0;
            ApplyTheme("MetropolisLight");
            CmbTheme.SelectionChanged += CmbTheme_SelectionChanged;
        }

        private readonly string[] _themeName = {
            "MetropolisLight",
            "MetropolisDark",
            "Win10Light",
            "Office2019White",
            "Office2019Black",
            "Office2019Colorful",
            "Office2019DarkGray",
            "Office2016White",
            "Office2016Black",
            "Office2016Colorful",
            "Office2013LightGray",
            "Office2013DarkGray"
        };

        private void ApplyTheme(string themeName)
        {
            ApplicationThemeHelper.ApplicationThemeName = themeName;
            UpdateLayout();
        }

        private void CmbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyTheme(_themeName[CmbTheme.SelectedIndex]);
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (RdbChinese.IsChecked != null && RdbChinese.IsChecked.Value)
            {
                ChangeUiLanguage("zh-CN");
            }
            else
            {
                ChangeUiLanguage("en");
            }
        }

        private void ChangeUiLanguage(string culture)
        {
            var dictionaryList = new List<ResourceDictionary>();
            foreach (var dictionary in Application.Current.Resources.MergedDictionaries)
            {
                dictionaryList.Add(dictionary);
            }
            var requestedCulture = $"Resources/StringDictionary.{culture}.xaml";
            var resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString.Equals(requestedCulture));
            if (resourceDictionary == null)
            {
                requestedCulture = "Resources/StringDictionary.xaml";
                resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString.Equals(requestedCulture));
            }
            if (resourceDictionary != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(resourceDictionary);
                Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
            }

            Messenger.Default.Send(new Message(MessageType.SwitchLanguage, null));
        }
    }
}
