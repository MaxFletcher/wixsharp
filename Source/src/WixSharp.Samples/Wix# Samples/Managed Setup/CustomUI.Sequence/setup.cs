//css_dir ..\..\..\;
//css_ref Wix_bin\WixToolset.Dtf.WindowsInstaller.dll;
//css_ref WixSharp.UI.dll;
//css_ref System.Core.dll;
//css_ref System.Xml.dll;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;
using WixSharp;
using WixSharp.CommonTasks;
using WixSharp.Forms;
using WixSharp.UI.Forms;
using WixToolset.Dtf.WindowsInstaller;

public static class Script
{
    static public void Main()
    {
        // optionally modify UAC related settings
        AutoElements.EnableUACRevealer = true;
        AutoElements.UACWarning = "Wait for UAC prompt to appear on the taskbar.";

        var binaries = new Feature("Binaries", "Product binaries", true, false);
        var docs = new Feature("Documentation", "Product documentation (manuals and user guides)", true) { Display = FeatureDisplay.expand };
        var tuts = new Feature("Tutorials", "Product tutorials", false) { Display = FeatureDisplay.expand };
        var manuals = new Feature("Manuals", "Product Manuals", false) { Display = FeatureDisplay.expand };
        var user_manuals = new Feature("User Manuals", "User Manuals", false);
        var dev_manuals = new Feature("Developer Manuals", "Developer Manuals", false);

        docs.Children.Add(tuts);
        tuts.Children.Add(manuals);
        manuals.Children.Add(user_manuals);
        manuals.Children.Add(dev_manuals);

        var project = new ManagedProject("ManagedSetup",
                              new Dir(@"%ProgramFiles%\My Company\My Product",
                                  new File(binaries, @"..\Files\bin\MyApp.exe"),
                                  new Dir("Docs",
                                      new File(docs, "readme.txt"),
                                      new File(tuts, @"..\Files\Docs\tutorial.txt"),
                                      new File(user_manuals, @"..\Files\Docs\Manual.txt"),
                                      new File(dev_manuals, @"..\Files\Docs\DevManual.txt"))));

        project.ManagedUI = new ManagedUI();

        //project.MinimalCustomDrawing = true;

        project.UIInitialized += CheckCompatibility; //will be fired on the embedded UI start
        project.Load += CheckCompatibility;          //will be fired on the MSI start

        //removing all entry dialogs and installdir
        project.ManagedUI.InstallDialogs.Add(Dialogs.Welcome)
                                        // .Add(Dialogs.Licence) // decide if to show (or not) this dialog at runtime
                                        // .Add(Dialogs.Features)
                                        // .Add(Dialogs.SetupType)
                                        // .Add(Dialogs.InstallDir)
                                        .Add(Dialogs.Progress)
                                        .Add(Dialogs.Exit);

        //removing entry dialog
        project.ManagedUI.ModifyDialogs.Add(Dialogs.MaintenanceType)
                                       //.Add(Dialogs.Features)
                                       .Add(Dialogs.Progress)
                                       .Add(Dialogs.Exit);

        project.ManagedUI.Icon = "app.ico";
        project.UILoaded += Project_UILoaded;
        project.AfterInstall += Project_AfterInstall;

        project.MinimalCustomDrawing = true;

        project.GUID = new Guid("6f330b47-2577-43ad-9095-1861ba25889b");

        project.ControlPanelInfo.InstallLocation = "[INSTALLDIR]";

        project.SetNetFxPrerequisite(Condition.Net45_Installed, "Please install .Net 4.5 First");

        // project.PreserveTempFiles = true;

        project.BuildMsi();
    }

    private static void Project_AfterInstall(SetupEventArgs e)
    {
        // Debug.Assert(false);

        var session = e.Session;
        try
        {
            var value = session.ExtractAppData()["SQLSERVER"];
            MessageBox.Show(value);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    static void Project_UILoaded(SetupEventArgs e)
    {
        MessageBox.Show("Project_UILoaded");
        var msiFile = e.Session.Database.FilePath;

        // Simulate analyzing the runtime conditions with the message box.
        // Make a decision to show (or not) Licence dialog.

        // There are two options for skipping dialogs:
        // - by modifying dialog sequence (next two commented lines)

        // if (MessageBox.Show("Do you want to remove 'Licence Dialog'?", "Wix#", MessageBoxButtons.YesNo) == DialogResult.Yes)
        //     e.ManagedUI.CurrentDialog.Shell.Dialogs.Remove(Dialogs.Licence);

        // - by handling OnCurrentDialogChanged event
        e.ManagedUI.OnCurrentDialogChanged += ManagedUIShell_OnCurrentDialogChanged;
    }

    static void ManagedUIShell_OnCurrentDialogChanged(IManagedDialog obj)
    {
        if (obj.GetType() == Dialogs.Licence)
        {
            // Simulate analyzing the runtime conditions with the message box.
            // Make a decision to jump over the dialog in the sequence
            if (MessageBox.Show("Do you want to skip 'Licence Dialog'?", "Wix#", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                obj.Shell.GoNext();
            }
        }
    }

    static void CheckCompatibility(SetupEventArgs e)
    {
        if (e.IsInstalling)
        {
            var conflictingProductCode = "{1D6432B4-E24D-405E-A4AB-D7E6D088C111}";

            if (AppSearch.IsProductInstalled(conflictingProductCode))
            {
                string msg = "Installed '{0}' is incompatible with this product.\n" +
                           "Setup will be aborted."
                               .FormatWith(AppSearch.GetProductName(conflictingProductCode) ?? conflictingProductCode);

                MessageBox.Show(msg, "Setup");
                e.Result = ActionResult.UserExit;
            }
        }
    }
}