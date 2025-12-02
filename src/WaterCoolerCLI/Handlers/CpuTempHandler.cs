using WaterCoolerCLI.Common;

namespace WaterCoolerCLI.Handlers;

public static class CpuTempHandler
{
    private const string HwmonDir = "/sys/class/hwmon";
    private const string StartInfoFileName = "/bin/sh";
    private const string StartInfoArguments = "-c \"lsmod | grep k10temp\"";

    public static bool CanReadCpuTemperature
    {
        get
        {
            try
            {
                // Check if k10temp module is loaded (optional, for diagnostics)
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = StartInfoFileName,
                        Arguments = StartInfoArguments,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string lsmodOutput = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                if (!lsmodOutput.Contains("k10temp"))
                {
                    return false;
                }

                if (!Directory.Exists(HwmonDir))
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }

            return true;
        }
    }

    public static bool GetCpuTemperature(out (double? PackageTempC, string PackageLabel)temp)
    {
        temp = (null, null);

        string[] hwmonDirs;
        
        try
        {
            hwmonDirs = Directory.GetDirectories(HwmonDir, "hwmon*");
        }
        catch (Exception ex)
        {
            LogUtil.Error(nameof(CpuTempHandler), ex.Message);
            return false;
        }

        foreach (var hwmonDirPath in hwmonDirs)
        {
            // Look for k10temp device (usually hwmon0 or similar)
            try
            {
                var nameFiles = Directory.GetFiles(hwmonDirPath, "name");
                if (!nameFiles.Any(f => File.ReadAllText(f).Trim() == "k10temp")) continue;
            }
            catch(Exception ex)
            {
                LogUtil.Error(nameof(CpuTempHandler),ex.Message);
                continue;
            }

            string [] tempInputFiles;
            try
            {
                tempInputFiles = Directory.GetFiles(hwmonDirPath, "temp*_input");
            }
            catch (Exception ex)
            {
                LogUtil.Error(nameof(CpuTempHandler), ex.Message);
                continue;
            }

            foreach (var inputFile in tempInputFiles)
            {
                try
                {
                    string inputName = Path.GetFileNameWithoutExtension(inputFile);
                    string labelFile = Path.Combine(Path.GetDirectoryName(inputFile)!, inputName.Replace("_input", "_label"));

                    if (File.Exists(labelFile))
                    {
                        string label = File.ReadAllText(labelFile).Trim();
                        string tempStr = File.ReadAllText(inputFile).Trim();

                        if (int.TryParse(tempStr, out int tempMilli))
                        {
                            double tempC = tempMilli / 1000.0;

                            switch (label)
                            {
                                // Prioritize Tctl for package temp
                                case "Tctl":
                                    temp.PackageTempC = tempC;
                                    temp.PackageLabel = label;
                                    break;
                                // Fallback to Tdie if no Tctl
                                case "Tdie" when temp.PackageTempC.HasValue is false:
                                    temp.PackageTempC = tempC;
                                    temp.PackageLabel = label;
                                    break;
                            }
                        }
                    }
                    else
                    {
                        LogUtil.Error(nameof(CpuTempHandler),$"No label found {inputName} {labelFile}");
                    }
                }
                catch(Exception ex)
                {
                    LogUtil.Error(nameof(CpuTempHandler),ex.Message);
                }
            }    
        }

        return temp.PackageTempC.HasValue;
    }
}