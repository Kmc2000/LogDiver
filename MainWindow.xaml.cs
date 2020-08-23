using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace LogDiver
{
    /// <summary>
    /// This class is the entire basis for the log diver. It handles building the actual window for it based off of the XAML template, and also handles file I/O.
    /// </summary>
    public partial class MainWindow
    {
        //Dict to hold all of the contents of each log file, indexed by what file it was loaded in from. There's a slight memory hit from this, but it lets me select lines super quickly with convenient filtering via LINQ.
        private readonly Dictionary<string, List<string>>
            _allLines =
                new Dictionary<string, List<string>>(); //Store the lines for later enumeration. Slight memory hit.
        //Dict to store the values of each filter type. Filters here refers to the files of origin for each log, these can either be "true" meaning theyll show up on the search page, or the inverse.
        private readonly Dictionary<string, bool> _filters = new Dictionary<string, bool>();
        private readonly string[] _presetFilters = new string[] {"attack", "game", "telecomms"};
        //Constructor for MainWindow, this is where everything's handled.
        public MainWindow()
        {
            //Open a new file dialogue set to choose folders, we just ask the user to give a directory where all the logs are stored, filtering handles the rest.
            var dialog = new CommonOpenFileDialog {IsFolderPicker = true};
            var result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Cancel)
            {
                Close();
                return;
            }
    
            var logPath = dialog.FileName;
            
            //Alright they've chosen a file folder now build the XAML itself so we're not referring to null refs.
            InitializeComponent();
            //Set up the UI prefabs based off of the newly generated XAML components.

            //This is our tab system, each log file gets its own tab
            TabControl tabby = LogTabs;

            string[] files = Directory.GetFiles(logPath, "*.log");
            //If we were unable to locate any files, just cut off. This could be extended to show a more useful error screen in future.
            if (files.Length <= 0)
            {
                return;
            }
            //Next up: process the log files we've been given.
            foreach (var file in files)
            {
                Console.WriteLine(file);
                var name = Path.GetFileNameWithoutExtension(file);
                var logTab = new TabItem {Header = name, Name = name};
                tabby?.Items.Add(logTab);
                var filter = new CheckBox {Content = name};
                //Set up event handler for the checkboxes.
                filter.Click += FilterTick;
                //Only some filters come preset, this is for ease of use so that you're not getting not entirely useful logs like asset and so on.
                if (_presetFilters.Contains(name))
                {
                    _filters[name] = true;
                    filter.IsChecked = true;
                }
                else
                {
                    _filters[name] = false;
                }
                
                SearchBox.Children.Add(filter);
                
                //Next up, we build the box that'll store the contents of each file in its specific tab.
                var lineBox = new ListView();
                foreach (var line in File.ReadLines(file))
                {
                    if (!_allLines.ContainsKey(name))
                    {
                        _allLines[name] = new List<string>();
                    }

                    _allLines[name].Add(line);
                    lineBox.Items.Add(new ListViewItem
                        {Content = line});
                }
                //Finally, set the tab's content to be the box we just generated.
                logTab.Content = lineBox;
            }
        }

        /// <summary>
        /// Event Handler method that is run when a checkbox is ticked
        /// </summary>
        /// <param name="sender">The object sending this event signal.</param>
        /// <param name="e"></param>
        private void FilterTick(object sender, EventArgs e)
        {
            CheckBox box = (CheckBox) sender;
            box.IsChecked = box.IsChecked != null && (bool) box.IsChecked;
            if (_filters[box.Content.ToString() ?? throw new InvalidOperationException()])
            {
                _filters[box.Content.ToString() ?? throw new InvalidOperationException()] = false;
            }
            else
            {
                _filters[box.Content.ToString() ?? throw new InvalidOperationException()] = true;
            }

            var temp = Search.Text;
            //Messy way of forcing text-box update. This isn't perfect but I can't find a way to do this cleanly yet.
            Search.Text = "";
            Search.Text = temp;

        }
        /// <summary>
        /// Method that is called when the contents of the searchbar changes. This will auto-execute your searches without having to push a button,
        /// which is a lot nicer to use than having to repeatedly click a "search" button next to a searchbar.
        /// </summary>
        /// <param name="sender">The search bar which sent this event</param>
        /// <param name="textChangedEventArgs"></param>
        private void OnSearch(object sender, TextChangedEventArgs textChangedEventArgs)
        {
            var search = Search;
            //Quick query to find what we're looking for.
            Results.Items.Clear();
            Results.Visibility = Visibility.Hidden;
            //Firstly, only select log lines that come from files that aren't being filtered off.
            var relevant = (from entry in _filters where entry.Value select entry.Key).SelectMany(filter => _allLines[filter]).ToList();
            //Next up, we take the output of relevant and perform some more precise searching on it, so that we only get the lines that have a match to our search query.
            IEnumerable<String> results = from line in relevant where line.Contains(search.Text) select line;
            //No results, so hide the results box from view and cut off here to save effort.
            if (search.Text.Length <= 0)
            {
                Results.Visibility = Visibility.Hidden;
                return;
            }
            //Seems we have some results, make the results box visible and add each result to it, so it forms a nice list.
            Results.Visibility = Visibility.Visible;
            foreach (var result in results)
            {
                Results.Items.Add(new ListViewItem {Foreground = (Colourify(result)), Content = new TextBlock {Text = result}});
            }
        }
        
        /// <summary>
        /// Method to colour specific logs based off of keywords, currently hardcoded.
        /// </summary>
        /// <param name="result">The log to be coloured.</param>
        /// <returns></returns>
        private Brush Colourify(String result)
        {
            if (result.Contains("ATTACK"))
            {
                return Brushes.OrangeRed;
            }
            if (result.Contains("ADMIN"))
            {
                return Brushes.Goldenrod;
            }
            if (result.Contains("SAY"))
            {
                return Brushes.DarkGreen;
            }

            if (result.Contains("ACCESS"))
            {
                return Brushes.DodgerBlue;
            }
            if (result.Contains("TCOMMS"))
            {
                return Brushes.Orange;
            }
            //Default case.
            return Brushes.Black;
        }
    }
}
