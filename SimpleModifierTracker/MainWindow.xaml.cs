using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SimpleModifierTracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Modifier> changed;

        private ListBox ModifiersListBox;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            ModifiersListBox = AttackModifiersListBox;
            FileInfo fi = new FileInfo("save.txt");
            if (fi.Exists)
            {
                LoadData(fi);
            }
            else
            {
                Modifier i = new Modifier(5, "Strength Modifier");
                ModifiersListBox.Items.Add(i);
                i = new Modifier(3, "Proficiency Bonus");
                ModifiersListBox.Items.Add(i);
            }

            tabControl1.SelectionChanged += new SelectionChangedEventHandler(tabControl1_SelectionChanged);
            ModifiersListBox.SelectionChanged += new SelectionChangedEventHandler(ModifiersListBox_SelectionChanged);
            AttackModifiersListBox.MouseDoubleClick += new MouseButtonEventHandler(ModifiersListBox_MouseDoubleClick);
            DamageModifiersListBox.MouseDoubleClick += new MouseButtonEventHandler(ModifiersListBox_MouseDoubleClick);

            changed = new List<SimpleModifierTracker.Modifier>();
            
            CalculateMod();
        }

        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            FileInfo fi = new FileInfo("save.txt");
            if (fi.Exists)
            {
                switch (MessageBox.Show("Override Save Data?","File Exists", MessageBoxButton.YesNoCancel ))
                {
                    case MessageBoxResult.Cancel:
                        e.Cancel = true;
                        break;
                    case MessageBoxResult.No:
                        e.Cancel = false;
                        return;
                    case MessageBoxResult.None:
                        e.Cancel = true;
                        break;
                    case MessageBoxResult.OK:
                        e.Cancel = false;
                        break;
                    case MessageBoxResult.Yes:
                        e.Cancel = false;
                       break;
                    default:
                        break;
                }
            }
            if (!SaveData(fi))
                MessageBox.Show("Save Fail", "Save Fail");
        }

        private bool LoadData(FileInfo fi)
        {
            string line;
            StreamReader sr = new StreamReader(fi.OpenRead());
            do
            {
                int amt = 0;
                string[] split;
                char[] delim = { '(', ')' };
                line = sr.ReadLine();
                if (line.Length > 0)
                {
                    split = line.Split(delim);
                    if (split[0].Trim() == "Attack Modifiers")
                        ModifiersListBox = AttackModifiersListBox;
                    else if (split[0].Trim() == "Damage Modifiers")
                        ModifiersListBox = DamageModifiersListBox;
                    else
                    {
                        bool result = false;
                        try
                        {

                            switch (split[1][0])
                            {
                                case '+':
                                    result = int.TryParse(split[1].Substring(1, split[1].Length - 1), out amt);
                                    break;
                                case '-':
                                    result = int.TryParse(split[1], out(amt));
                                    break;
                                default:
                                    break;
                            }
                        }
                        catch
                        {
                            ModifiersListBox.Items.Clear();
                            return false;
                        }
                        if (result)
                        {
                            ModifiersListBox.Items.Add(new Modifier(amt, split[0].Trim()));
                            if (split.Length > 2)
                            {
                                if (split[2].Trim().Length > 0)
                                {
                                    ModifiersListBox.SelectedItems.Add(ModifiersListBox.Items[ModifiersListBox.Items.Count - 1]);
                                }
                            }
                        }
                        else
                            return false;
                    }
                }
            } while (!sr.EndOfStream);
            sr.Close();
            return true;
        }

        private bool SaveData(FileInfo fi)
        {
            if (AttackModifiersListBox.Items.Count == 0 && DamageModifiersListBox.Items.Count == 0)
                return false;
            FileInfo temp = new FileInfo(DateTime.Now.Ticks.ToString());
            StreamWriter sr;
            try
            {
                
                sr = new StreamWriter(temp.OpenWrite());
            }
            catch
            {
                return false;
            }
            try 
            {
                
                if (AttackModifiersListBox.Items.Count > 0)
                {
                    sr.WriteLine("Attack Modifiers");
                    foreach (Modifier item in AttackModifiersListBox.Items)
                    {
                        if (item.IsSelected)
                        {
                            sr.WriteLine(item.Content + " *");
                        }
                        else
                            sr.WriteLine(item.Content);
                    }
                }
                if (DamageModifiersListBox.Items.Count > 0)
                {
                    sr.WriteLine("Damage Modifiers");
                    foreach (Modifier item in DamageModifiersListBox.Items)
                    {
                        if (item.IsSelected)
                        {
                            sr.WriteLine(item.Content + " *");
                        }
                        else
                            sr.WriteLine(item.Content);
                    }
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                sr.Close();
            }
            if (fi.Exists)
            {
                fi.Delete();
            }
            temp.MoveTo(fi.Name);
            return true;

        }

        void ModifiersListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (changed.Count > 0)
            {
                foreach (Modifier item in changed)
                {
                    ModifiersListBox.Items.Remove(item);
                }
                changed.Clear();
            }
        }

        private void ModifiersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            changed = new List<SimpleModifierTracker.Modifier>();
            foreach (Modifier item in e.AddedItems)
            {
                changed.Add(item);
            }
            foreach (Modifier item in e.RemovedItems)
            {
                changed.Add(item);
            }
            CalculateMod();
            e.Handled = true;
        }

        private void CalculateMod()
        {
            switch (tabControl1.SelectedIndex)
            {
                case 0:
                    ModifiersListBox = AttackModifiersListBox;
                    break;
                case 1:
                    ModifiersListBox = DamageModifiersListBox;
                    break;
                default:
                    break;
            }
            int mod = 0;
            foreach (Modifier item in ModifiersListBox.SelectedItems)
            {
                mod += item.Amount;
            }
            if (mod < 0)
                CurrentModifierLabel.Content = mod.ToString();
            else
                CurrentModifierLabel.Content = "+" + mod.ToString();
        }

        private void AddModifierButton_Click(object sender, RoutedEventArgs e)
        {
            int amount;
            if (DescriptionBox.Text.Length > 0 && Int32.TryParse(AmountBox.Text, out amount))
            {
                Modifier i = new Modifier(amount,DescriptionBox.Text);
                ModifiersListBox.Items.Add(i);
            }
        }

        private void tabControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            CalculateMod();
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SaveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
            sfd.DefaultExt = ".txt|Text File";
            sfd.ShowDialog();
            
        }


        private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }




    }

    public class Modifier : ListBoxItem
    {
        public int Amount { get; set; }
        public Modifier(int amount, string desc)
        {
            this.Amount = amount;
            if (amount < 0)
                this.Content = desc + " (" + amount.ToString() + ")";
            else
                this.Content = desc + " (+" + amount.ToString() + ")";

        }
    }

}
